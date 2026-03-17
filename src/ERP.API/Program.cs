using ERP.API.Common;
using ERP.Application;
using ERP.Domain.Constants;
using ERP.Infrastructure;
using ERP.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.Configure<TenantResolutionOptions>(builder.Configuration.GetSection(TenantResolutionOptions.SectionName));

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

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

await DevelopmentDataSeeder.SeedAsync(app);
await SubscriptionRoleSynchronization.ApplyAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseCors("DevCors");

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
app.MapGet("/", () => Results.Ok(new
{
    service = "ERPv2 API",
    environment = app.Environment.EnvironmentName,
    docs = app.Environment.IsDevelopment() ? "/swagger" : null
})).AllowAnonymous();
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    utc = DateTime.UtcNow,
    authorizationEnforced = securityOptions.EnforceAuthorization
})).AllowAnonymous();

app.Run();
