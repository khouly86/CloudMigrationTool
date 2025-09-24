# PowerShell Script to Fix Package Issues
# This script removes problematic packages and installs compatible alternatives

param(
    [string]$SolutionName = "CloudMigrationTool"
)

Write-Host "Fixing Package Issues..." -ForegroundColor Green

# Ensure we're in the solution directory
if (!(Test-Path "$SolutionName.sln")) {
    Write-Error "Solution file not found. Please run from solution directory."
    exit 1
}

Write-Host "Removing problematic packages..." -ForegroundColor Yellow

# Remove Microsoft.Exchange.WebServices from all projects
$projects = @(
    "src\CloudMigrationTool.Web\CloudMigrationTool.Web.csproj",
    "src\CloudMigrationTool.Infrastructure\CloudMigrationTool.Infrastructure.csproj",
    "src\CloudMigrationTool.Services\CloudMigrationTool.Services.csproj",
    "tests\CloudMigrationTool.Tests.Unit\CloudMigrationTool.Tests.Unit.csproj",
    "tests\CloudMigrationTool.Tests.Integration\CloudMigrationTool.Tests.Integration.csproj"
)

foreach ($project in $projects) {
    if (Test-Path $project) {
        Write-Host "Removing problematic packages from $project..." -ForegroundColor Cyan
        
        # Remove Exchange Web Services
        dotnet remove $project package Microsoft.Exchange.WebServices 2>$null
        
        # Remove problematic Graph packages
        dotnet remove $project package Microsoft.Graph.Auth 2>$null
        
        Write-Host "Cleaned $project" -ForegroundColor Green
    }
}

Write-Host "Installing compatible packages..." -ForegroundColor Yellow

# Install compatible packages for Infrastructure project
Write-Host "Installing Infrastructure packages..." -ForegroundColor Cyan
dotnet add "src\CloudMigrationTool.Infrastructure" package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add "src\CloudMigrationTool.Infrastructure" package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add "src\CloudMigrationTool.Infrastructure" package Microsoft.Graph --version 5.36.0
dotnet add "src\CloudMigrationTool.Infrastructure" package Azure.Identity --version 1.10.4

# Install compatible packages for Services project  
Write-Host "Installing Services packages..." -ForegroundColor Cyan
dotnet add "src\CloudMigrationTool.Services" package Microsoft.Graph --version 5.36.0
dotnet add "src\CloudMigrationTool.Services" package Azure.Identity --version 1.10.4

# Clean and restore
Write-Host "Cleaning and restoring solution..." -ForegroundColor Cyan
dotnet clean
dotnet restore

Write-Host "Package issues fixed!" -ForegroundColor Green
Write-Host "Next: Update the ExchangeService.cs file with the corrected version" -ForegroundColor Yellow