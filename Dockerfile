# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy every .csproj first so Docker can cache the restore layer independently
# of source changes.
COPY src/core/TemplateMs.Domain/TemplateMs.Domain.csproj                          src/core/TemplateMs.Domain/
COPY src/core/TemplateMs.Application/TemplateMs.Application.csproj                src/core/TemplateMs.Application/
COPY src/infrastructure/TemplateMs.Sql/TemplateMs.Sql.csproj                      src/infrastructure/TemplateMs.Sql/
COPY src/infrastructure/TemplateMs.Infrastructure/TemplateMs.Infrastructure.csproj src/infrastructure/TemplateMs.Infrastructure/
COPY src/presentation/TemplateMs.Api/TemplateMs.Api.csproj                         src/presentation/TemplateMs.Api/

RUN dotnet restore src/presentation/TemplateMs.Api/TemplateMs.Api.csproj

# Copy full source and publish a self-contained-ready Release build
COPY . .
RUN dotnet publish src/presentation/TemplateMs.Api/TemplateMs.Api.csproj \
	--configuration Release \
	--no-restore \
	--output /app/publish

# ── Runtime stage ─────────────────────────────────────────────────────────────
# aspnet:9.0 includes ICU libraries required because InvariantGlobalization=false
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Run as non-root for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

# ASP.NET Core 9 defaults to port 8080 inside containers over plain HTTP.
# Override connection strings and AMQ settings via environment variables at
# runtime (see README for the full list of keys).
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "TemplateMs.Api.dll"]
