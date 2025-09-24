# PowerShell Script to Create .NET Projects (CORRECTED)
# Step 2: Create all .NET projects and install compatible packages

param(
    [string]$SolutionName = "CloudMigrationTool"
)

Write-Host "Creating .NET Projects with Compatible Packages..." -ForegroundColor Green

# Ensure we're in the solution directory
if (!(Test-Path "$SolutionName.sln")) {
    Write-Error "Solution file not found. Please run 01-Create-Solution-Structure.ps1 first."
    exit 1
}

# Create Web Project (MVC)
Write-Host "Creating Web Project..." -ForegroundColor Cyan
Set-Location "src\CloudMigrationTool.Web"
dotnet new mvc --no-https false --framework net8.0
Set-Location "..\..\"
dotnet sln add "src\CloudMigrationTool.Web\CloudMigrationTool.Web.csproj"

# Create Core Class Library
Write-Host "Creating Core Library..." -ForegroundColor Cyan
Set-Location "src\CloudMigrationTool.Core"
dotnet new classlib --framework net8.0
Set-Location "..\..\"
dotnet sln add "src\CloudMigrationTool.Core\CloudMigrationTool.Core.csproj"

# Create Infrastructure Class Library
Write-Host "Creating Infrastructure Library..." -ForegroundColor Cyan
Set-Location "src\CloudMigrationTool.Infrastructure"
dotnet new classlib --framework net8.0
Set-Location "..\..\"
dotnet sln add "src\CloudMigrationTool.Infrastructure\CloudMigrationTool.Infrastructure.csproj"

# Create Services Class Library
Write-Host "Creating Services Library..." -ForegroundColor Cyan
Set-Location "src\CloudMigrationTool.Services"
dotnet new classlib --framework net8.0
Set-Location "..\..\"
dotnet sln add "src\CloudMigrationTool.Services\CloudMigrationTool.Services.csproj"

# Create Test Projects
Write-Host "Creating Test Projects..." -ForegroundColor Cyan
Set-Location "tests\CloudMigrationTool.Tests.Unit"
dotnet new xunit --framework net8.0
Set-Location "..\..\"
dotnet sln add "tests\CloudMigrationTool.Tests.Unit\CloudMigrationTool.Tests.Unit.csproj"

Set-Location "tests\CloudMigrationTool.Tests.Integration"
dotnet new xunit --framework net8.0
Set-Location "..\..\"
dotnet sln add "tests\CloudMigrationTool.Tests.Integration\CloudMigrationTool.Tests.Integration.csproj"

# Add project references
Write-Host "Adding project references..." -ForegroundColor Cyan

# Web project references
dotnet add "src\CloudMigrationTool.Web" reference "src\CloudMigrationTool.Core"
dotnet add "src\CloudMigrationTool.Web" reference "src\CloudMigrationTool.Infrastructure"
dotnet add "src\CloudMigrationTool.Web" reference "src\CloudMigrationTool.Services"

# Infrastructure references Core
dotnet add "src\CloudMigrationTool.Infrastructure" reference "src\CloudMigrationTool.Core"

# Services references Core
dotnet add "src\CloudMigrationTool.Services" reference "src\CloudMigrationTool.Core"

# Test project references
dotnet add "tests\CloudMigrationTool.Tests.Unit" reference "src\CloudMigrationTool.Core"
dotnet add "tests\CloudMigrationTool.Tests.Unit" reference "src\CloudMigrationTool.Services"

dotnet add "tests\CloudMigrationTool.Tests.Integration" reference "src\CloudMigrationTool.Web"
dotnet add "tests\CloudMigrationTool.Tests.Integration" reference "src\CloudMigrationTool.Infrastructure"

Write-Host "Installing Compatible NuGet packages..." -ForegroundColor Yellow

# Web Project Packages
Write-Host "Installing Web project packages..." -ForegroundColor Cyan
dotnet add "src\CloudMigrationTool.Web" package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add "src\CloudMigrationTool.Web" package Microsoft.EntityFrameworkCore.Tools --version 8.0.0
dotnet add "src\CloudMigrationTool.Web" package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add "src\CloudMigrationTool.Web" package Serilog.AspNetCore --version 8.0.0
dotnet add "src\CloudMigrationTool.Web" package Serilog.Sinks.File --version 5.0.0
dotnet add "src\CloudMigrationTool.Web" package Serilog.Sinks.Console --version 5.0.0
dotnet add "src\CloudMigrationTool.Web" package Microsoft.AspNetCore.Diagnostics.HealthChecks --version 2.2.0

# Core Project Packages  
Write-Host "Installing Core project packages..." -ForegroundColor Cyan
dotnet add "src\CloudMigrationTool.Core" package System.ComponentModel.DataAnnotations --version 5.0.0
dotnet add "src\CloudMigrationTool.Core" package Microsoft.Extensions.Configuration.Abstractions --version 8.0.0
dotnet add "src\CloudMigrationTool.Core" package Microsoft.Extensions.Logging.Abstractions --version 8.0.0

# Infrastructure Project Packages (Using compatible alternatives)
Write-Host "Installing Infrastructure project packages..." -ForegroundColor Cyan
dotnet add "src\CloudMigrationTool.Infrastructure" package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add "src\CloudMigrationTool.Infrastructure" package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add "src\CloudMigrationTool.Infrastructure" package System.DirectoryServices --version 8.0.0
dotnet add "src\CloudMigrationTool.Infrastructure" package System.DirectoryServices.AccountManagement --version 8.0.0

# Using Microsoft Graph instead of Exchange Web Services (more modern and supported)
dotnet add "src\CloudMigrationTool.Infrastructure" package Microsoft.Graph --version 5.36.0
dotnet add "src\CloudMigrationTool.Infrastructure" package Microsoft.Graph.Authentication --version 2.0.0

# Services Project Packages
Write-Host "Installing Services project packages..." -ForegroundColor Cyan
dotnet add "src\CloudMigrationTool.Services" package Microsoft.Extensions.DependencyInjection.Abstractions --version 8.0.0
dotnet add "src\CloudMigrationTool.Services" package Microsoft.Extensions.Logging.Abstractions --version 8.0.0
dotnet add "src\CloudMigrationTool.Services" package Microsoft.Extensions.Configuration.Abstractions --version 8.0.0
dotnet add "src\CloudMigrationTool.Services" package System.DirectoryServices --version 8.0.0
dotnet add "src\CloudMigrationTool.Services" package Microsoft.Graph --version 5.36.0
dotnet add "src\CloudMigrationTool.Services" package AutoMapper --version 12.0.1

# Test Project Packages
Write-Host "Installing Test project packages..." -ForegroundColor Cyan
dotnet add "tests\CloudMigrationTool.Tests.Unit" package Microsoft.NET.Test.Sdk --version 17.8.0
dotnet add "tests\CloudMigrationTool.Tests.Unit" package xunit --version 2.6.1
dotnet add "tests\CloudMigrationTool.Tests.Unit" package xunit.runner.visualstudio --version 2.5.3
dotnet add "tests\CloudMigrationTool.Tests.Unit" package Moq --version 4.20.69

dotnet add "tests\CloudMigrationTool.Tests.Integration" package Microsoft.AspNetCore.Mvc.Testing --version 8.0.0
dotnet add "tests\CloudMigrationTool.Tests.Integration" package Microsoft.EntityFrameworkCore.InMemory --version 8.0.0

Write-Host "Restoring all packages..." -ForegroundColor Cyan
dotnet restore

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nAll projects created and compatible packages installed successfully!" -ForegroundColor Green
    Write-Host "Next: Run 03-Create-Core-Files.ps1 to create the core domain files" -ForegroundColor Yellow
} else {
    Write-Warning "Some packages may have compatibility issues. Check the output above."
    Write-Host "You can continue with the next step, but you may need to update package versions manually." -ForegroundColor Yellow
}

Write-Host "`nPackage Compatibility Notes:" -ForegroundColor Cyan
Write-Host "- Using Microsoft Graph instead of Exchange Web Services for better compatibility" -ForegroundColor Yellow
Write-Host "- All packages are compatible with .NET 8.0" -ForegroundColor Yellow
Write-Host "- If you need Exchange Web Services, consider using Exchange Online PowerShell or Graph API" -ForegroundColor Yellow