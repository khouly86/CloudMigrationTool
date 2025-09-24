FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/CloudMigrationTool.Web/CloudMigrationTool.Web.csproj", "src/CloudMigrationTool.Web/"]
COPY ["src/CloudMigrationTool.Core/CloudMigrationTool.Core.csproj", "src/CloudMigrationTool.Core/"]
COPY ["src/CloudMigrationTool.Infrastructure/CloudMigrationTool.Infrastructure.csproj", "src/CloudMigrationTool.Infrastructure/"]
COPY ["src/CloudMigrationTool.Services/CloudMigrationTool.Services.csproj", "src/CloudMigrationTool.Services/"]

RUN dotnet restore "src/CloudMigrationTool.Web/CloudMigrationTool.Web.csproj"
COPY . .
WORKDIR "/src/src/CloudMigrationTool.Web"
RUN dotnet build "CloudMigrationTool.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CloudMigrationTool.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

ENTRYPOINT ["dotnet", "CloudMigrationTool.Web.dll"]
