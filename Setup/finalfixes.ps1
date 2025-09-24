# Final Fixes Script - CORRECTED VERSION
# Fixes the remaining build errors

param(
    [string]$SolutionName = "CloudMigrationTool"
)

Write-Host "Applying final fixes..." -ForegroundColor Green

# Ensure we're in the solution directory
if (!(Test-Path "$SolutionName.sln")) {
    Write-Error "Solution file not found. Please run from solution directory."
    exit 1
}

Write-Host "Fix 1: Updating Azure.Identity to latest secure version..." -ForegroundColor Cyan

# Update Azure.Identity to latest version to fix security vulnerabilities
dotnet add "src\CloudMigrationTool.Infrastructure" package Azure.Identity --version 1.12.0
dotnet add "src\CloudMigrationTool.Services" package Azure.Identity --version 1.12.0

Write-Host "Fix 2: Creating correct ErrorViewModel..." -ForegroundColor Cyan

# Create the correct ErrorViewModel content
$errorViewModelContent = @'
using System.Diagnostics;

namespace CloudMigrationTool.Web.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
'@

# Write the ErrorViewModel file
$errorViewModelPath = "src\CloudMigrationTool.Web\Models\ErrorViewModel.cs"
$errorViewModelContent | Out-File -FilePath $errorViewModelPath -Force -Encoding UTF8
Write-Host "Created correct ErrorViewModel.cs" -ForegroundColor Green

Write-Host "Fix 3: Updating BaseViewModels..." -ForegroundColor Cyan

# Update BaseViewModels.cs to only contain HomeIndexViewModel
$baseViewModelsContent = @'
using CloudMigrationTool.Core.Entities;

namespace CloudMigrationTool.Web.Models
{
    public class HomeIndexViewModel
    {
        public int ActiveMigrationsCount { get; set; }
        public int TotalJobsCount { get; set; }
        public int CompletedJobsCount { get; set; }
        public int FailedJobsCount { get; set; }
        public int ActiveConnectionsCount { get; set; }
        public int TotalConnectionsCount { get; set; }
        public List<MigrationJob> RecentJobs { get; set; } = new List<MigrationJob>();
    }
}
'@

$baseViewModelsPath = "src\CloudMigrationTool.Web\Models\BaseViewModels.cs"
$baseViewModelsContent | Out-File -FilePath $baseViewModelsPath -Force -Encoding UTF8
Write-Host "Updated BaseViewModels.cs" -ForegroundColor Green

Write-Host "Fix 4: Creating correct Admin Connections view..." -ForegroundColor Cyan

# Create the corrected Connections.cshtml
$connectionsViewContent = @'
@model IEnumerable<ConnectionSettings>
@{
    ViewData["Title"] = "Manage Connections";
}

<div class="row">
    <div class="col-12">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <h1>
                <i class="fas fa-plug"></i>
                Connection Settings
            </h1>
            <a asp-action="CreateConnection" class="btn btn-primary">
                <i class="fas fa-plus"></i> Add Connection
            </a>
        </div>
    </div>
</div>

<div class="row">
    <div class="col-12">
        <div class="card">
            <div class="card-body">
                @if (Model.Any())
                {
                    <div class="table-responsive">
                        <table class="table table-hover">
                            <thead>
                                <tr>
                                    <th>Name</th>
                                    <th>Type</th>
                                    <th>Description</th>
                                    <th>Status</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                @foreach (var connection in Model)
                                {
                                    <tr>
                                        <td><strong>@connection.Name</strong></td>
                                        <td><span class="badge bg-secondary">@connection.Type</span></td>
                                        <td>@(connection.Description ?? "No description")</td>
                                        <td>@await Html.PartialAsync("_ConnectionStatusBadge", connection.Status)</td>
                                        <td>
                                            <div class="btn-group" role="group">
                                                <button class="btn btn-sm btn-outline-primary test-connection" 
                                                        data-id="@connection.Id" title="Test Connection">
                                                    <i class="fas fa-plug"></i>
                                                </button>
                                                <button class="btn btn-sm btn-outline-secondary" title="Edit">
                                                    <i class="fas fa-edit"></i>
                                                </button>
                                                <button class="btn btn-sm btn-outline-danger" title="Delete">
                                                    <i class="fas fa-trash"></i>
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                }
                            </tbody>
                        </table>
                    </div>
                }
                else
                {
                    <div class="text-center py-5">
                        <i class="fas fa-plug fa-3x text-muted mb-3"></i>
                        <h4 class="text-muted">No connections configured</h4>
                        <p class="text-muted">Create your first connection to get started.</p>
                        <a asp-action="CreateConnection" class="btn btn-primary">
                            <i class="fas fa-plus"></i> Create Connection
                        </a>
                    </div>
                }
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <script>
        $(document).ready(function() {
            $('.test-connection').on('click', function() {
                var btn = $(this);
                var connectionId = btn.data('id');
                var originalIcon = btn.html();
                
                btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i>');
                
                // Simulate test
                setTimeout(function() {
                    btn.removeClass('btn-outline-primary')
                       .addClass('btn-outline-success')
                       .html('<i class="fas fa-check"></i>');
                    
                    setTimeout(function() {
                        btn.prop('disabled', false)
                           .removeClass('btn-outline-success')
                           .addClass('btn-outline-primary')
                           .html(originalIcon);
                    }, 2000);
                }, 1000);
            });
        });
    </script>
}
'@

$connectionsViewPath = "src\CloudMigrationTool.Web\Views\Admin\Connections.cshtml"
$connectionsViewContent | Out-File -FilePath $connectionsViewPath -Force -Encoding UTF8
Write-Host "Created correct Connections.cshtml" -ForegroundColor Green

Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "" 
    Write-Host "🎉 BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host ""
    Write-Host "To run the application:" -ForegroundColor Yellow
    Write-Host "dotnet run --project src\CloudMigrationTool.Web" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Application will be available at:" -ForegroundColor Yellow
    Write-Host "• https://localhost:7167" -ForegroundColor Green  
    Write-Host "• http://localhost:5167" -ForegroundColor Green
} else {
    Write-Host "Build still has issues. Please check the errors above." -ForegroundColor Red
}