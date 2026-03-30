using ERP.API.Common;
using ERP.Application;
using ERP.Domain.Constants;
using ERP.Infrastructure;
using ERP.Infrastructure.Authentication;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = ResolveContentRootPath()
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.Configure<TenantResolutionOptions>(builder.Configuration.GetSection(TenantResolutionOptions.SectionName));
builder.Services.Configure<EmailCampaignOptions>(builder.Configuration.GetSection(EmailCampaignOptions.SectionName));

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.MultipartBodyLengthLimit = 10 * 1024 * 1024); // 10 MB
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 10 * 1024 * 1024); // 10 MB

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ERPv2 API",
        Version = "v1",
        Description = "ERP foundation API with Clean Architecture + CQRS"
    });

    options.OperationFilter<EndpointSummaryOperationFilter>();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<PlatformEmailCampaignProcessor>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (jwtOptions.Key.Length < 32)
{
    throw new InvalidOperationException("JWT key must be at least 32 characters.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(AppRoles.Admin));
    options.AddPolicy("TierUserOrAdmin", policy => policy.RequireRole(AppRoles.Admin, AppRoles.Tier1, AppRoles.Tier2, AppRoles.Tier3));
    options.AddPolicy("PlatformAdmin", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole(AppRoles.Admin) && !context.User.HasClaim(c => c.Type == "tenant_id")));
});

var app = builder.Build();
var securityOptions = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();

await EnsureDatabaseMigratedAsync(app);
await DevelopmentDataSeeder.SeedAsync(app);
await SubscriptionRoleSynchronization.ApplyAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseCors("DevCors");
app.UseRateLimiter();
app.UseDefaultFiles();
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
    await next();
});

if (securityOptions.EnforceAuthorization)
{
    app.UseAuthentication();
}

app.UseMiddleware<TenantResolutionMiddleware>();

if (securityOptions.EnforceAuthorization)
{
    app.UseAuthorization();
}

app.UseMiddleware<ActivityLoggingMiddleware>();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    utc = DateTime.UtcNow,
    authorizationEnforced = securityOptions.EnforceAuthorization
})).AllowAnonymous();

var indexPath = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "index.html");
if (File.Exists(indexPath))
{
    app.MapFallbackToFile("index.html");
}
else
{
    app.MapGet("/", () => Results.Ok(new
    {
        service = "ERPv2 API",
        environment = app.Environment.EnvironmentName,
        docs = app.Environment.IsDevelopment() ? "/swagger" : null
    })).AllowAnonymous();
}

app.Run();

static string ResolveContentRootPath()
{
    var executableDirectory = ResolveExecutableDirectory();
    if (File.Exists(Path.Combine(executableDirectory, "appsettings.json")))
    {
        return executableDirectory;
    }

    var currentDirectory = Directory.GetCurrentDirectory();
    if (File.Exists(Path.Combine(currentDirectory, "appsettings.json")))
    {
        return currentDirectory;
    }

    var baseDirectory = AppContext.BaseDirectory;
    if (File.Exists(Path.Combine(baseDirectory, "appsettings.json")))
    {
        return baseDirectory;
    }

    return currentDirectory;
}

static string ResolveExecutableDirectory()
{
    var processPath = Environment.ProcessPath;
    if (!string.IsNullOrWhiteSpace(processPath))
    {
        var processDirectory = Path.GetDirectoryName(processPath);
        if (!string.IsNullOrWhiteSpace(processDirectory))
        {
            return processDirectory;
        }
    }

    return AppContext.BaseDirectory;
}

static async Task EnsureDatabaseMigratedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
    var providerName = dbContext.Database.ProviderName ?? string.Empty;
    if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        await dbContext.Database.EnsureCreatedAsync();
        return;
    }

    await dbContext.Database.MigrateAsync();
}
