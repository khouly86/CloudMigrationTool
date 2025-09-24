# PowerShell Script to Create Program.cs and Configuration Files
# Step 9: Create application startup and configuration

param(
    [string]$SolutionName = "CloudMigrationTool"
)

Write-Host "Creating Program.cs and Configuration Files..." -ForegroundColor Green

# Ensure we're in the solution directory
if (!(Test-Path "$SolutionName.sln")) {
    Write-Error "Solution file not found. Please run previous setup scripts first."
    exit 1
}

Write-Host "Creating Program.cs..." -ForegroundColor Cyan

# Program.cs for .NET 6+ minimal hosting model
@"
using CloudMigrationTool.Infrastructure.Data;
using CloudMigrationTool.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllersWithViews();

// Add application services
builder.Services.AddApplicationServices(builder.Configuration);

// Configure Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContext<ApplicationDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Configure routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add health check endpoint
app.MapHealthChecks("/health");

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        context.Database.EnsureCreated();
        Log.Information("Database initialized successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the database");
    }
}

Log.Information("Cloud Migration Tool started successfully");

app.Run();
"@ | Out-File -FilePath "src\CloudMigrationTool.Web\Program.cs" -Force -Encoding UTF8

Write-Host "Creating appsettings.json..." -ForegroundColor Cyan

# appsettings.json
@"
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CloudMigrationToolDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ]
  },
  "ApplicationSettings": {
    "ApplicationName": "Cloud Migration Tool",
    "Version": "1.0.0",
    "MaxConcurrentMigrations": 3,
    "DefaultMigrationTimeout": 480,
    "EnableDetailedErrorLogging": true,
    "AutoRefreshInterval": 30,
    "ExchangeSettings": {
      "DefaultBatchSize": 100,
      "MaxRetryAttempts": 3,
      "RetryDelaySeconds": 5
    },
    "ActiveDirectorySettings": {
      "DefaultPageSize": 1000,
      "ConnectionTimeoutSeconds": 30,
      "EnableGroupMembershipSync": true
    },
    "SecuritySettings": {
      "EncryptConnectionStrings": true,
      "RequireHttps": true,
      "SessionTimeoutMinutes": 60
    }
  },
  "AllowedHosts": "*"
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Web\appsettings.json" -Force -Encoding UTF8

Write-Host "Creating appsettings.Development.json..." -ForegroundColor Cyan

# appsettings.Development.json
@"
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CloudMigrationToolDb_Dev;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "ApplicationSettings": {
    "EnableDetailedErrorLogging": true,
    "AutoRefreshInterval": 10,
    "SecuritySettings": {
      "RequireHttps": false
    }
  },
  "DetailedErrors": true
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Web\appsettings.Development.json" -Force -Encoding UTF8

Write-Host "Creating launchSettings.json..." -ForegroundColor Cyan

# Create Properties folder if it doesn't exist
if (!(Test-Path "src\CloudMigrationTool.Web\Properties")) {
    New-Item -ItemType Directory -Path "src\CloudMigrationTool.Web\Properties" -Force
}

# launchSettings.json
@"
{
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:63840",
      "sslPort": 44330
    }
  },
  "profiles": {
    "CloudMigrationTool.Web": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:7167;http://localhost:5167",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Web\Properties\launchSettings.json" -Force -Encoding UTF8

Write-Host "Creating Docker support files..." -ForegroundColor Cyan

# Dockerfile
@"
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
"@ | Out-File -FilePath "Dockerfile" -Encoding UTF8

# Docker Compose
@"
version: '3.8'

services:
  cloudmigrationtool-web:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:80"
      - "8081:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ConnectionStrings__DefaultConnection=Server=sql-server;Database=CloudMigrationToolDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true
    depends_on:
      - sql-server
    volumes:
      - ./logs:/app/logs

  sql-server:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
      - MSSQL_PID=Express
    ports:
      - "1433:1433"
    volumes:
      - sql-data:/var/opt/mssql

volumes:
  sql-data:
"@ | Out-File -FilePath "docker-compose.yml" -Encoding UTF8

Write-Host "Creating README.md..." -ForegroundColor Cyan

# README.md
@"
# Cloud Migration Tool

A comprehensive .NET application for migrating on-premises environments to Microsoft cloud services including Active Directory to Entra ID and Exchange Server to Exchange Online.

## Features

- **Active Directory Migration**: Migrate users, groups, and organizational units from on-premises AD to Entra ID (Azure AD)
- **Exchange Migration**: Migrate mailboxes from on-premises Exchange Server to Exchange Online
- **Web-Based Interface**: Clean, responsive HTML/CSS interface for management and monitoring
- **Progress Tracking**: Real-time migration progress with detailed logging
- **Connection Management**: Dynamic configuration of source and destination connections
- **Extensible Architecture**: Modular design ready for OneDrive and SharePoint migrations

## Architecture

The application follows Clean Architecture principles:

- **Core**: Domain entities, enums, interfaces, and models
- **Infrastructure**: Data access, external service integrations (AD, Exchange)
- **Services**: Application services and business logic
- **Web**: ASP.NET Core MVC web application

## Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB for development)
- Visual Studio 2022 or VS Code
- PowerShell 5.1 or later

## Getting Started

### 1. Setup Using PowerShell Scripts

Run the PowerShell scripts in order:

```powershell
# 1. Create solution structure
.\01-Create-Solution-Structure.ps1

# 2. Create .NET projects and install packages
.\02-Create-Projects.ps1

# 3. Create core domain files
.\03-Create-Core-Files.ps1

# 4. Create infrastructure layer
.\04-Create-Infrastructure-Files.ps1

# 5. Create services layer
.\05-Create-Services-Files.ps1

# 6. Create web controllers and models
.\06-Create-Web-Files.ps1

# 7. Create Razor views
.\07-Create-Views.ps1

# 8. Create static files (CSS, JS)
.\08-Create-Static-Files.ps1

# 9. Create startup configuration
.\09-Create-Program-And-Startup.ps1

# 10. Build and run
.\10-Build-And-Run.ps1
```

### 2. Manual Setup

If you prefer manual setup:

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd CloudMigrationTool
   ```

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Update connection string**
   Update `appsettings.json` with your SQL Server connection string.

4. **Run the application**
   ```bash
   dotnet run --project src/CloudMigrationTool.Web
   ```

## Configuration

### Database Connection

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=CloudMigrationToolDb;Trusted_Connection=true;"
  }
}
```

### Application Settings

Configure migration settings in `appsettings.json`:

```json
{
  "ApplicationSettings": {
    "MaxConcurrentMigrations": 3,
    "DefaultMigrationTimeout": 480,
    "ExchangeSettings": {
      "DefaultBatchSize": 100,
      "MaxRetryAttempts": 3
    }
  }
}
```

## Usage

### 1. Configure Connections

1. Navigate to **Administration > Connections**
2. Add source connections (on-premises AD, Exchange)
3. Add destination connections (Entra ID, Exchange Online)
4. Test connections to ensure they're working

### 2. Create Migration Jobs

1. Go to **Migrations > New Migration**
2. Select migration type (Active Directory or Exchange)
3. Choose source and destination connections
4. Configure migration settings
5. Start the migration

### 3. Monitor Progress

1. View real-time progress on the **Dashboard**
2. Check detailed logs in **Migration > Job Details**
3. Monitor system health via **Administration**

## Connection String Formats

### Active Directory
```
Domain=contoso.local;Container=DC=contoso,DC=local;Username=admin;Password=password
```

### Exchange Server
```
ServerUrl=https://exchange.contoso.local/PowerShell/;Username=admin;Password=password;Domain=contoso
```

### Exchange Online
```
ConnectionUri=https://outlook.office365.com/powershell-liveid/;Username=admin@contoso.com;Password=password
```

## API Endpoints

The application exposes several API endpoints:

- `GET /api/migrations` - List all migration jobs
- `POST /api/migrations` - Create new migration job
- `GET /api/migrations/{id}/progress` - Get migration progress
- `POST /api/connections/test` - Test connection
- `GET /health` - Health check endpoint

## Docker Support

### Build and run with Docker:

```bash
# Build the image
docker build -t cloudmigrationtool .

# Run with docker-compose
docker-compose up -d
```

### Environment Variables:

- `ASPNETCORE_ENVIRONMENT` - Application environment
- `ConnectionStrings__DefaultConnection` - Database connection string

## Logging

The application uses Serilog for structured logging:

- Console output for development
- File logging to `logs/` directory
- Configurable log levels per namespace
- Health check logging

## Security Considerations

- **Connection Strings**: Encrypt sensitive connection strings
- **Authentication**: Implement proper authentication for production
- **HTTPS**: Enable HTTPS in production environments
- **Secrets Management**: Use Azure Key Vault or similar for secrets

## Troubleshooting

### Common Issues

1. **Database Connection Failed**
   - Verify SQL Server is running
   - Check connection string format
   - Ensure database user has proper permissions

2. **Active Directory Connection Failed**
   - Verify domain connectivity
   - Check credentials and permissions
   - Ensure LDAP ports are accessible

3. **Exchange Connection Failed**
   - Verify PowerShell remoting is enabled
   - Check Exchange server URL and credentials
   - Ensure proper Exchange permissions

### Logs Location

- Development: Console output
- Production: `/app/logs/` directory
- Docker: Mapped to `./logs/` on host

## Future Enhancements

- **OneDrive Migration**: Migrate user files from on-premises file servers
- **SharePoint Migration**: Migrate SharePoint sites and content
- **Teams Migration**: Migrate Teams data and configurations
- **Reporting**: Advanced reporting and analytics
- **Multi-tenant Support**: Support for multiple organizations

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For support and questions:

- Check the documentation in `/docs`
- Review logs in `/logs`
- Open an issue on GitHub
- Contact the development team

## Technology Stack

- **.NET 8.0**: Core framework
- **ASP.NET Core MVC**: Web framework
- **Entity Framework Core**: Data access
- **SQL Server**: Database
- **Bootstrap 5**: UI framework
- **jQuery**: JavaScript library
- **Serilog**: Logging framework
- **AutoMapper**: Object mapping
- **Docker**: Containerization

## Performance Considerations

- Concurrent migration jobs are limited by configuration
- Database connection pooling is enabled
- Async/await patterns used throughout
- Progress tracking minimizes database calls
- Logging is optimized for performance

## Monitoring

- Built-in health checks at `/health`
- Application metrics via logs
- Performance counters for migrations
- Connection status monitoring
- Real-time dashboard updates
"@ | Out-File -FilePath "README.md" -Encoding UTF8

Write-Host "Creating final build script..." -ForegroundColor Cyan

# Build and Run Script
@"
# PowerShell Script to Build and Run the Application
# Step 10: Build, test, and run the Cloud Migration Tool

param(
    [string]$SolutionName = "CloudMigrationTool",
    [switch]$SkipTests,
    [switch]$RunOnly
)

Write-Host "Building and Running Cloud Migration Tool..." -ForegroundColor Green

# Ensure we're in the solution directory
if (!(Test-Path "$SolutionName.sln")) {
    Write-Error "Solution file not found. Please run previous setup scripts first."
    exit 1
}

if (!$RunOnly) {
    Write-Host "Cleaning solution..." -ForegroundColor Cyan
    dotnet clean

    Write-Host "Restoring NuGet packages..." -ForegroundColor Cyan
    dotnet restore

    Write-Host "Building solution..." -ForegroundColor Cyan
    $buildResult = dotnet build --configuration Release --no-restore
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed. Please check the errors above."
        exit 1
    }
    
    if (!$SkipTests) {
        Write-Host "Running tests..." -ForegroundColor Cyan
        dotnet test --no-build --configuration Release --verbosity normal
        
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Some tests failed, but continuing with startup..."
        }
    }
}

Write-Host "Starting the application..." -ForegroundColor Cyan
Write-Host ""
Write-Host "Application will be available at:" -ForegroundColor Yellow
Write-Host "  HTTPS: https://localhost:7167" -ForegroundColor Green
Write-Host "  HTTP:  http://localhost:5167" -ForegroundColor Green
Write-Host ""
Write-Host "Health Check: https://localhost:7167/health" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop the application" -ForegroundColor Yellow
Write-Host ""

# Start the application
Set-Location "src\CloudMigrationTool.Web"
dotnet run --configuration Release --no-build
"@ | Out-File -FilePath "10-Build-And-Run.ps1" -Encoding UTF8

Write-Host "Creating .gitignore..." -ForegroundColor Cyan

# .gitignore
@"
## Ignore Visual Studio temporary files, build results, and
## files generated by popular Visual Studio add-ons.

# User-specific files
*.rsuser
*.suo
*.user
*.userosscache
*.sln.docstates

# User-specific files (MonoDevelop/Xamarin Studio)
*.userprefs

# Mono auto generated files
mono_crash.*

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/
[Ll]ogs/

# Visual Studio 2015/2017 cache/options directory
.vs/

# Visual Studio Code
.vscode/

# Uncomment if you have tasks that create the project's static files in wwwroot
#wwwroot/

# MSTest test Results
[Tt]est[Rr]esult*/
[Bb]uild[Ll]og.*

# NUnit
*.VisualState.xml
TestResult.xml
nunit-*.xml

# Build Results of an ATL Project
[Dd]ebugPS/
[Rr]eleasePS/
dlldata.c

# Benchmark Results
BenchmarkDotNet.Artifacts/

# .NET Core
project.lock.json
project.fragment.lock.json
artifacts/

# StyleCop
StyleCopReport.xml

# Files built by Visual Studio
*_i.c
*_p.c
*_h.h
*.ilk
*.meta
*.obj
*.iobj
*.pch
*.pdb
*.ipdb
*.pgc
*.pgd
*.rsp
*.sbr
*.tlb
*.tli
*.tlh
*.tmp
*.tmp_proj
*_wpftmp.csproj
*.log
*.vspscc
*.vssscc
.builds
*.pidb
*.svclog
*.scc

# Chutzpah Test files
_Chutzpah*

# Visual C++ cache files
ipch/
*.aps
*.ncb
*.opendb
*.opensdf
*.sdf
*.cachefile
*.VC.db
*.VC.VC.opendb

# Visual Studio profiler
*.psess
*.vsp
*.vspx
*.sap

# Visual Studio Trace Files
*.e2e

# TFS 2012 Local Workspace
$tf/

# Guidance Automation Toolkit
*.gpState

# ReSharper is a .NET coding add-in
_ReSharper*/
*.[Rr]e[Ss]harper
*.DotSettings.user

# TeamCity is a build add-in
_TeamCity*

# DotCover is a Code Coverage Tool
*.dotCover

# AxoCover is a Code Coverage Tool
.axoCover/*
!.axoCover/settings.json

# Coverlet is a free, cross platform Code Coverage Tool
coverage*.json
coverage*.xml
coverage*.info

# Visual Studio code coverage results
*.coverage
*.coveragexml

# NCrunch
_NCrunch_*
.*crunch*.local.xml
nCrunchTemp_*

# MightyMoose
*.mm.*
AutoTest.Net/

# Web workbench (sass)
.sass-cache/

# Installshield output folder
[Ee]xpress/

# DocProject is a documentation generator add-in
DocProject/buildhelp/
DocProject/Help/*.HxT
DocProject/Help/*.HxC
DocProject/Help/*.hhc
DocProject/Help/*.hhk
DocProject/Help/*.hhp
DocProject/Help/Html2
DocProject/Help/html

# Click-Once directory
publish/

# Publish Web Output
*.[Pp]ublish.xml
*.azurePubxml
# Note: Comment the next line if you want to checkin your web deploy settings,
# but database connection strings (with potential passwords) will be unencrypted
*.pubxml
*.publishproj

# Microsoft Azure Web App publish settings. Comment the next line if you want to
# checkin your Azure Web App publish settings, but sensitive information contained
# in these files may be used by others to gain access to your Azure account.
*.azurewebsites.net.pubxml

# Microsoft Azure Build Output
csx/
*.build.csdef

# Microsoft Azure Emulator
ecf/
rcf/

# Windows Store app package directories and files
AppPackages/
BundleArtifacts/
Package.StoreAssociation.xml
_pkginfo.txt
*.appx
*.appxbundle
*.appxupload

# Visual Studio cache files
# files ending in .cache can be ignored
*.[Cc]ache
# but keep track of directories ending in .cache
!?*.[Cc]ache/

# Others
ClientBin/
~$*
*~
*.dbmdl
*.dbproj.schemaview
*.jfm
*.pfx
*.publishsettings
orleans.codegen.cs

# Including strong name files can present a security risk
# (https://github.com/github/gitignore/pull/2483#issue-259490424)
#*.snk

# Since there are multiple workflows, uncomment next line to ignore bower_components
# (https://github.com/github/gitignore/pull/1529#issuecomment-104372622)
#bower_components/

# RIA/Silverlight projects
Generated_Code/

# Backup & report files from converting an old project file
# to a newer Visual Studio version. Backup files are not needed,
# because we have git ;-)
_UpgradeReport_Files/
Backup*/
UpgradeLog*.XML
UpgradeLog*.htm
CSharpUpgradeLog*.XML

# SQL Server files
*.mdf
*.ldf
*.ndf

# Business Intelligence projects
*.rdl.data
*.bim.layout
*.bim_*.settings
*.rptproj.rsuser
*- [Bb]ackup.rdl
*- [Bb]ackup ([0-9]).rdl
*- [Bb]ackup ([0-9][0-9]).rdl

# Microsoft Fakes
FakesAssemblies/

# GhostDoc plugin setting file
*.GhostDoc.xml

# Node.js Tools for Visual Studio
.ntvs_analysis.dat
node_modules/

# Visual Studio 6 build log
*.plg

# Visual Studio 6 workspace options file
*.opt

# Visual Studio 6 auto-generated workspace file (contains which files were open etc.)
*.vbw

# Visual Studio LightSwitch build output
**/*.HTMLClient/GeneratedArtifacts
**/*.DesktopClient/GeneratedArtifacts
**/*.DesktopClient/ModelManifest.xml
**/*.Server/GeneratedArtifacts
**/*.Server/ModelManifest.xml
_Pvt_Extensions

# Paket dependency manager
.paket/paket.exe
paket-files/

# FAKE - F# Make
.fake/

# CodeRush personal settings
.cr/personal

# Python Tools for Visual Studio (PTVS)
__pycache__/
*.pyc

# Cake - Uncomment if you are using it
# tools/**
# !tools/packages.config

# Tabs Studio
*.tss

# Telerik's JustMock configuration file
*.jmconfig

# BizTalk build output
*.btp.cs
*.btm.cs
*.odx.cs
*.xsd.cs

# OpenCover UI analysis results
OpenCover/

# Azure Stream Analytics local run output
ASALocalRun/

# MSBuild Binary and Structured Log
*.binlog

# NVidia Nsight GPU debugger configuration file
*.nvuser

# MFractors (Xamarin productivity tool) working folder
.mfractor/

# Local History for Visual Studio
.localhistory/

# BeatPulse healthcheck temp database
healthchecksdb

# Backup folder for Package Reference Convert tool in Visual Studio 2017
MigrationBackup/

# Ionide (cross platform F# VS Code tools) working folder
.ionide/

# Application specific
logs/
*.db
*.db-shm
*.db-wal
appsettings.*.local.json
"@ | Out-File -FilePath ".gitignore" -Encoding UTF8

Write-Host "`nProgram.cs and configuration files created successfully!" -ForegroundColor Green
Write-Host "Solution is now complete!" -ForegroundColor Green
Write-Host ""
Write-Host "To run the application:" -ForegroundColor Yellow
Write-Host "  1. Run: .\10-Build-And-Run.ps1" -ForegroundColor Cyan
Write-Host "  2. Or manually: dotnet run --project src\CloudMigrationTool.Web" -ForegroundColor Cyan
Write-Host ""
Write-Host "Application will be available at:" -ForegroundColor Yellow
Write-Host "  HTTPS: https://localhost:7167" -ForegroundColor Green
Write-Host "  HTTP:  http://localhost:5167" -ForegroundColor Green