# PowerShell Script to Build and Run the Application (CORRECTED)
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

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Package restore failed. Please check the errors above."
        exit 1
    }

    Write-Host "Building solution..." -ForegroundColor Cyan
    dotnet build --configuration Release --no-restore
    
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

try {
    dotnet run --configuration Release --no-build
}
catch {
    Write-Error "Failed to start the application: $($_.Exception.Message)"
    exit 1
}
finally {
    # Return to the original directory
    Set-Location "..\..\"
}