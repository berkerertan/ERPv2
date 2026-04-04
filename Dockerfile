FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for better layer caching.
COPY src/ERP.API/ERP.API.csproj src/ERP.API/
COPY src/ERP.Application/ERP.Application.csproj src/ERP.Application/
COPY src/ERP.Domain/ERP.Domain.csproj src/ERP.Domain/
COPY src/ERP.Infrastructure/ERP.Infrastructure.csproj src/ERP.Infrastructure/

RUN dotnet restore src/ERP.API/ERP.API.csproj

# Copy full source and publish API.
COPY . .
RUN dotnet publish src/ERP.API/ERP.API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_URLS=http://+:10000

EXPOSE 10000

# Render provides PORT at runtime. Fall back to 10000 if missing.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-10000} dotnet ERP.API.dll"]
