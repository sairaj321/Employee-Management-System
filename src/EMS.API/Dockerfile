# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy solution and project files for caching restore
COPY EMS.sln ./
COPY src/EMS.Domain/EMS.Domain.csproj src/EMS.Domain/
COPY src/EMS.Application/EMS.Application.csproj src/EMS.Application/
COPY src/EMS.Infrastructure/EMS.Infrastructure.csproj src/EMS.Infrastructure/
COPY src/EMS.API/EMS.API.csproj src/EMS.API/
COPY src/EMS.Tests/EMS.Tests.csproj src/EMS.Tests/

RUN dotnet restore

# Copy all source and build
COPY . .
WORKDIR /app/src/EMS.API
RUN dotnet publish -c Release -o /app/out

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "EMS.API.dll"]
