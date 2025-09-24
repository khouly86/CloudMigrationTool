# PowerShell Script to Create Razor Views (CORRECTED)
# Step 7: Create all Razor views and layouts

param(
    [string]$SolutionName = "CloudMigrationTool"
)

Write-Host "Creating Razor Views..." -ForegroundColor Green

# Ensure we're in the solution directory
if (!(Test-Path "$SolutionName.sln")) {
    Write-Error "Solution file not found. Please run previous setup scripts first."
    exit 1
}

Write-Host "Creating Layout Views..." -ForegroundColor Cyan

# Layout View - Using here-string with single quotes to avoid variable expansion
$layoutContent = @'
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - Cloud Migration Tool</title>
    <link href="~/css/bootstrap.min.css" rel="stylesheet" />
    <link href="~/css/site.css" rel="stylesheet" />
</head>
<body>
    <header>
        <nav class="navbar navbar-expand-sm navbar-dark bg-primary">
            <div class="container-fluid">
                <a class="navbar-brand" asp-controller="Home" asp-action="Index">
                    <strong>Cloud Migration Tool</strong>
                </a>
                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
                    <span class="navbar-toggler-icon"></span>
                </button>
                <div class="collapse navbar-collapse" id="navbarNav">
                    <ul class="navbar-nav me-auto">
                        <li class="nav-item">
                            <a class="nav-link" asp-controller="Home" asp-action="Index">Dashboard</a>
                        </li>
                        <li class="nav-item dropdown">
                            <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown">
                                Migrations
                            </a>
                            <ul class="dropdown-menu">
                                <li><a class="dropdown-item" asp-controller="Migration" asp-action="Dashboard">Overview</a></li>
                                <li><a class="dropdown-item" asp-controller="Migration" asp-action="Index">All Jobs</a></li>
                                <li><hr class="dropdown-divider"></li>
                                <li><a class="dropdown-item" asp-controller="Migration" asp-action="Create">New Migration</a></li>
                            </ul>
                        </li>
                        <li class="nav-item dropdown">
                            <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown">
                                Administration
                            </a>
                            <ul class="dropdown-menu">
                                <li><a class="dropdown-item" asp-controller="Admin" asp-action="Index">Overview</a></li>
                                <li><a class="dropdown-item" asp-controller="Admin" asp-action="Connections">Connections</a></li>
                                <li><a class="dropdown-item" asp-controller="Admin" asp-action="Settings">Settings</a></li>
                            </ul>
                        </li>
                    </ul>
                    <div class="navbar-text">
                        <span class="badge bg-success" id="status-indicator">Online</span>
                    </div>
                </div>
            </div>
        </nav>
    </header>

    <main class="container-fluid mt-3">
        @if (TempData["SuccessMessage"] != null)
        {
            <div class="alert alert-success alert-dismissible fade show">
                @TempData["SuccessMessage"]
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        }
        
        @if (TempData["ErrorMessage"] != null)
        {
            <div class="alert alert-danger alert-dismissible fade show">
                @TempData["ErrorMessage"]
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        }
        
        @RenderBody()
    </main>

    <footer class="bg-light mt-5 py-3">
        <div class="container">
            <div class="row">
                <div class="col-md-6">
                    <small class="text-muted">&copy; 2025 Cloud Migration Tool. All rights reserved.</small>
                </div>
                <div class="col-md-6 text-end">
                    <small class="text-muted">Version 1.0.0</small>
                </div>
            </div>
        </div>
    </footer>

    <script src="~/js/bootstrap.bundle.min.js"></script>
    <script src="~/js/jquery-3.6.0.min.js"></script>
    <script src="~/js/site.js"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
'@

$layoutContent | Out-File -FilePath "src\CloudMigrationTool.Web\Views\Shared\_Layout.cshtml" -Force -Encoding UTF8

# ViewStart
$viewStartContent = @'
@{
    Layout = "_Layout";
}
'@

$viewStartContent | Out-File -FilePath "src\CloudMigrationTool.Web\Views\_ViewStart.cshtml" -Force -Encoding UTF8

# ViewImports
$viewImportsContent = @'
@using CloudMigrationTool.Web
@using CloudMigrationTool.Web.Models
@using CloudMigrationTool.Core.Entities
@using CloudMigrationTool.Core.Enums
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
'@

$viewImportsContent | Out-File -FilePath "src\CloudMigrationTool.Web\Views\_ViewImports.cshtml" -Force -Encoding UTF8

Write-Host "Creating Home Views..." -ForegroundColor Cyan

# Home Index View
$homeIndexContent = @'
@model HomeIndexViewModel
@{
    ViewData["Title"] = "Dashboard";
}

<div class="row">
    <div class="col-12">
        <h1 class="mb-4">
            <i class="fas fa-tachometer-alt"></i>
            Migration Dashboard
        </h1>
    </div>
</div>

<div class="row mb-4">
    <div class="col-md-3">
        <div class="card bg-primary text-white">
            <div class="card-body">
                <div class="d-flex align-items-center">
                    <div class="flex-grow-1">
                        <h5 class="card-title">Active Migrations</h5>
                        <h2 class="mb-0">@Model.ActiveMigrationsCount</h2>
                    </div>
                    <div class="ms-3">
                        <i class="fas fa-sync-alt fa-2x"></i>
                    </div>
                </div>
            </div>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card bg-success text-white">
            <div class="card-body">
                <div class="d-flex align-items-center">
                    <div class="flex-grow-1">
                        <h5 class="card-title">Completed</h5>
                        <h2 class="mb-0">@Model.CompletedJobsCount</h2>
                    </div>
                    <div class="ms-3">
                        <i class="fas fa-check-circle fa-2x"></i>
                    </div>
                </div>
            </div>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card bg-danger text-white">
            <div class="card-body">
                <div class="d-flex align-items-center">
                    <div class="flex-grow-1">
                        <h5 class="card-title">Failed</h5>
                        <h2 class="mb-0">@Model.FailedJobsCount</h2>
                    </div>
                    <div class="ms-3">
                        <i class="fas fa-exclamation-triangle fa-2x"></i>
                    </div>
                </div>
            </div>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card bg-info text-white">
            <div class="card-body">
                <div class="d-flex align-items-center">
                    <div class="flex-grow-1">
                        <h5 class="card-title">Connections</h5>
                        <h2 class="mb-0">@Model.ActiveConnectionsCount/@Model.TotalConnectionsCount</h2>
                    </div>
                    <div class="ms-3">
                        <i class="fas fa-plug fa-2x"></i>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<div class="row">
    <div class="col-md-8">
        <div class="card">
            <div class="card-header">
                <h5 class="card-title mb-0">Recent Migration Jobs</h5>
            </div>
            <div class="card-body">
                @if (Model.RecentJobs.Any())
                {
                    <div class="table-responsive">
                        <table class="table table-hover">
                            <thead>
                                <tr>
                                    <th>Job Name</th>
                                    <th>Type</th>
                                    <th>Status</th>
                                    <th>Created</th>
                                    <th>Progress</th>
                                </tr>
                            </thead>
                            <tbody>
                                @foreach (var job in Model.RecentJobs)
                                {
                                    <tr>
                                        <td>
                                            <a asp-controller="Migration" asp-action="Details" asp-route-id="@job.Id">
                                                @job.Name
                                            </a>
                                        </td>
                                        <td>
                                            <span class="badge bg-secondary">@job.Type</span>
                                        </td>
                                        <td>
                                            @await Html.PartialAsync("_StatusBadge", job.Status)
                                        </td>
                                        <td>@job.CreatedAt.ToString("MMM dd, yyyy")</td>
                                        <td>
                                            @if (job.TotalItems > 0)
                                            {
                                                var percentage = (double)job.ProcessedItems / job.TotalItems * 100;
                                                <div class="progress" style="height: 20px;">
                                                    <div class="progress-bar" role="progressbar" 
                                                         style="width: @percentage%"
                                                         aria-valuenow="@percentage" aria-valuemin="0" aria-valuemax="100">
                                                        @percentage.ToString("F1")%
                                                    </div>
                                                </div>
                                            }
                                            else
                                            {
                                                <span class="text-muted">Not started</span>
                                            }
                                        </td>
                                    </tr>
                                }
                            </tbody>
                        </table>
                    </div>
                }
                else
                {
                    <p class="text-muted text-center py-4">No migration jobs found. <a asp-controller="Migration" asp-action="Create">Create your first migration job</a>.</p>
                }
            </div>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card">
            <div class="card-header">
                <h5 class="card-title mb-0">Quick Actions</h5>
            </div>
            <div class="card-body">
                <div class="d-grid gap-2">
                    <a asp-controller="Migration" asp-action="Create" class="btn btn-primary">
                        <i class="fas fa-plus"></i> New Migration
                    </a>
                    <a asp-controller="Admin" asp-action="Connections" class="btn btn-outline-primary">
                        <i class="fas fa-plug"></i> Manage Connections
                    </a>
                    <a asp-controller="Migration" asp-action="Index" class="btn btn-outline-secondary">
                        <i class="fas fa-list"></i> View All Jobs
                    </a>
                    <a asp-controller="Admin" asp-action="Settings" class="btn btn-outline-secondary">
                        <i class="fas fa-cog"></i> Settings
                    </a>
                </div>
            </div>
        </div>
        
        <div class="card mt-3">
            <div class="card-header">
                <h5 class="card-title mb-0">System Status</h5>
            </div>
            <div class="card-body">
                <div class="d-flex justify-content-between align-items-center mb-2">
                    <span>Application Status:</span>
                    <span class="badge bg-success">Online</span>
                </div>
                <div class="d-flex justify-content-between align-items-center mb-2">
                    <span>Active Connections:</span>
                    <span class="badge bg-info">@Model.ActiveConnectionsCount</span>
                </div>
                <div class="d-flex justify-content-between align-items-center">
                    <span>Running Jobs:</span>
                    <span class="badge bg-warning">@Model.ActiveMigrationsCount</span>
                </div>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <script>
        // Auto-refresh every 30 seconds
        setInterval(function() {
            location.reload();
        }, 30000);
    </script>
}
'@

$homeIndexContent | Out-File -FilePath "src\CloudMigrationTool.Web\Views\Home\Index.cshtml" -Force -Encoding UTF8

Write-Host "Creating Admin Views..." -ForegroundColor Cyan

# Admin Index View - Using here-string to avoid variable expansion issues
$adminIndexContent = @'
@model AdminDashboardViewModel
@{
    ViewData["Title"] = "Administration";
}

<div class="row">
    <div class="col-12">
        <h1 class="mb-4">
            <i class="fas fa-cogs"></i>
            Administration
        </h1>
    </div>
</div>

<div class="row">
    <div class="col-md-6">
        <div class="card">
            <div class="card-header d-flex justify-content-between align-items-center">
                <h5 class="card-title mb-0">
                    <i class="fas fa-plug"></i>
                    Connection Settings
                </h5>
                <a asp-action="Connections" class="btn btn-sm btn-outline-primary">View All</a>
            </div>
            <div class="card-body">
                @if (Model.Connections.Any())
                {
                    <div class="table-responsive">
                        <table class="table table-sm">
                            <thead>
                                <tr>
                                    <th>Name</th>
                                    <th>Type</th>
                                    <th>Status</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                @foreach (var connection in Model.Connections.Take(5))
                                {
                                    <tr>
                                        <td>@connection.Name</td>
                                        <td>
                                            <span class="badge bg-secondary">@connection.Type</span>
                                        </td>
                                        <td>
                                            @await Html.PartialAsync("_ConnectionStatusBadge", connection.Status)
                                        </td>
                                        <td>
                                            <button class="btn btn-sm btn-outline-primary test-connection" 
                                                    data-id="@connection.Id">Test</button>
                                        </td>
                                    </tr>
                                }
                            </tbody>
                        </table>
                    </div>
                }
                else
                {
                    <p class="text-muted text-center">No connections configured.</p>
                }
            </div>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card">
            <div class="card-header d-flex justify-content-between align-items-center">
                <h5 class="card-title mb-0">
                    <i class="fas fa-sliders-h"></i>
                    Application Settings
                </h5>
                <a asp-action="Settings" class="btn btn-sm btn-outline-primary">View All</a>
            </div>
            <div class="card-body">
                @if (Model.Settings.Any())
                {
                    <div class="table-responsive">
                        <table class="table table-sm">
                            <thead>
                                <tr>
                                    <th>Setting</th>
                                    <th>Value</th>
                                    <th>Category</th>
                                </tr>
                            </thead>
                            <tbody>
                                @foreach (var setting in Model.Settings.Take(5))
                                {
                                    <tr>
                                        <td>@setting.Key</td>
                                        <td>
                                            <code>@(setting.Value.Length > 20 ? setting.Value.Substring(0, 20) + "..." : setting.Value)</code>
                                        </td>
                                        <td>
                                            @if (!string.IsNullOrEmpty(setting.Category))
                                            {
                                                <span class="badge bg-info">@setting.Category</span>
                                            }
                                        </td>
                                    </tr>
                                }
                            </tbody>
                        </table>
                    </div>
                }
                else
                {
                    <p class="text-muted text-center">No settings configured.</p>
                }
            </div>
        </div>
    </div>
</div>

<div class="row mt-4">
    <div class="col-12">
        <div class="card">
            <div class="card-header">
                <h5 class="card-title mb-0">
                    <i class="fas fa-tasks"></i>
                    Quick Actions
                </h5>
            </div>
            <div class="card-body">
                <div class="row">
                    <div class="col-md-3">
                        <div class="d-grid">
                            <a asp-controller="Admin" asp-action="CreateConnection" class="btn btn-primary">
                                <i class="fas fa-plus"></i>
                                Add Connection
                            </a>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="d-grid">
                            <a asp-controller="Admin" asp-action="Connections" class="btn btn-outline-primary">
                                <i class="fas fa-plug"></i>
                                Manage Connections
                            </a>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="d-grid">
                            <a asp-controller="Admin" asp-action="Settings" class="btn btn-outline-secondary">
                                <i class="fas fa-cog"></i>
                                Application Settings
                            </a>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="d-grid">
                            <a asp-controller="Migration" asp-action="Index" class="btn btn-outline-info">
                                <i class="fas fa-list"></i>
                                View Migrations
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>
'@

$adminIndexContent | Out-File -FilePath "src\CloudMigrationTool.Web\Views\Admin\Index.cshtml" -Force -Encoding UTF8

# Create Admin Connections view with JavaScript properly escaped
Write-Host "Creating Admin Connections View..." -ForegroundColor Cyan

# Use separate variables for JavaScript sections to avoid PowerShell variable expansion
$adminConnectionsStart = @'
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
                                    <th>Last Tested</th>
                                    <th>Active</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                @foreach (var connection in Model)
                                {
                                    <tr>
                                        <td>
                                            <strong>@connection.Name</strong>
                                        </td>
                                        <td>
                                            <span class="badge bg-secondary">@connection.Type</span>
                                        </td>
                                        <td>
                                            @(connection.Description ?? "No description")
                                        </td>
                                        <td>
                                            @await Html.PartialAsync("_ConnectionStatusBadge", connection.Status)
                                        </td>
                                        <td>
                                            @if (connection.LastTestedAt.HasValue)
                                            {
                                                <span class="text-muted">@connection.LastTestedAt.Value.ToString("MMM dd, yyyy HH:mm")</span>
                                            }
                                            else
                                            {
                                                <span class="text-muted">Never tested</span>
                                            }
                                        </td>
                                        <td>
                                            @if (connection.IsActive)
                                            {
                                                <span class="badge bg-success">Active</span>
                                            }
                                            else
                                            {
                                                <span class="badge bg-secondary">Inactive</span>
                                            }
                                        </td>
                                        <td>
                                            <div class="btn-group" role="group">
                                                <button class="btn btn-sm btn-outline-primary test-connection" 
                                                        data-id="@connection.Id" title="Test Connection">
                                                    <i class="fas fa-plug"></i>
                                                </button>
                                                <a asp-action="EditConnection" asp-route-id="@connection.Id" 
                                                   class="btn btn-sm btn-outline-secondary" title="Edit">
                                                    <i class="fas fa-edit"></i>
                                                </a>
                                                <button class="btn btn-sm btn-outline-danger delete-connection" 
                                                        data-id="@connection.Id" data-name="@connection.Name" title="Delete">
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
                        <p class="text-muted">Create your first connection to get started with migrations.</p>
                        <a asp-action="CreateConnection" class="btn btn-primary">
                            <i class="fas fa-plus"></i> Create Connection
                        </a>
                    </div>
                }
            </div>
        </div>
    </div>
</div>

<!-- Delete Confirmation Modal -->
<div class="modal fade" id="deleteModal" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Confirm Delete</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                Are you sure you want to delete the connection "<span id="connectionName"></span>"?
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                <form method="post" style="display: inline;">
                    <input type="hidden" name="id" id="deleteId" />
                    <button type="submit" class="btn btn-danger" id="confirmDelete">Delete</button>
                </form>
            </div>
        </div>
    </div>
</div>
'@

# JavaScript section for the connections page - using backticks to escape dollar signs
$adminConnectionsJS = @'
@section Scripts {
    <script>
        `$(document).ready(function() {
            // Test connection functionality
            `$('.test-connection').on('click', function() {
                var btn = `$(this);
                var connectionId = btn.data('id');
                var originalIcon = btn.html();
                
                btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i>');
                
                `$.post('@Url.Action("TestConnection", "Admin")', { id: connectionId })
                    .done(function(data) {
                        if (data.success) {
                            btn.removeClass('btn-outline-primary btn-outline-danger')
                               .addClass('btn-outline-success')
                               .html('<i class="fas fa-check"></i>');
                        } else {
                            btn.removeClass('btn-outline-primary btn-outline-success')
                               .addClass('btn-outline-danger')
                               .html('<i class="fas fa-times"></i>');
                        }
                    })
                    .fail(function() {
                        btn.removeClass('btn-outline-primary btn-outline-success')
                           .addClass('btn-outline-danger')
                           .html('<i class="fas fa-exclamation"></i>');
                    })
                    .always(function() {
                        setTimeout(function() {
                            btn.prop('disabled', false)
                               .removeClass('btn-outline-success btn-outline-danger')
                               .addClass('btn-outline-primary')
                               .html(originalIcon);
                        }, 2000);
                    });
            });
            
            // Delete connection functionality
            `$('.delete-connection').on('click', function() {
                var connectionId = `$(this).data('id');
                var connectionName = `$(this).data('name');
                
                `$('#connectionName').text(connectionName);
                `$('#deleteId').val(connectionId);
                `$('#confirmDelete').attr('formaction', '@Url.Action("DeleteConnection", "Admin")');
                
                `$('#deleteModal').modal('show');
            });
        });
    </script>
}
'@

# Combine the content parts
$adminConnectionsContent = $adminConnectionsStart + $adminConnectionsJS

$adminConnectionsContent | Out-File -FilePath "src\CloudMigrationTool.Web\Views\Admin\Connections.cshtml" -Force -Encoding UTF8

Write-Host "Creating Partial Views..." -ForegroundColor Cyan

# Status Badge Partial View
$statusBadgeContent = @'
@model CloudMigrationTool.Core.Enums.MigrationStatus

@switch (Model)
{
    case MigrationStatus.NotStarted:
        <span class="badge bg-secondary">Not Started</span>
        break;
    case MigrationStatus.InProgress:
        <span class="badge bg-primary">
            <i class="fas fa-spinner fa-spin"></i> In Progress
        </span>
        break;
    case MigrationStatus.Completed:
        <span class="badge bg-success">
            <i class="fas fa-check"></i> Completed
        </span>
        break;
    case MigrationStatus.Failed:
        <span class="badge bg-danger">
            <i class="fas fa-times"></i> Failed
        </span>
        break;
    case MigrationStatus.Paused:
        <span class="badge bg-warning">
            <i class="fas fa-pause"></i> Paused
        </span>
        break;
    case MigrationStatus.Cancelled:
        <span class="badge bg-dark">
            <i class="fas fa-stop"></i> Cancelled
        </span>
        break;
    default:
        <span class="badge bg-secondary">Unknown</span>
        break;
}
'@

$statusBadgeContent | Out-File -FilePath "src\CloudMigrationTool.Web\Views\Shared\_StatusBadge.cshtml" -Force -Encoding UTF8

# Connection Status Badge Partial View
$connectionStatusBadgeContent = @'
@model CloudMigrationTool.Core.Enums.ConnectionStatus

@switch (Model)
{
    case ConnectionStatus.NotConfigured:
        <span class="badge bg-secondary">Not Configured</span>
        break;
    case ConnectionStatus.Connected:
        <span class="badge bg-success">
            <i class="fas fa-plug"></i> Connected
        </span>
        break;
    case ConnectionStatus.Disconnected:
        <span class="badge bg-warning">
            <i class="fas fa-unlink"></i> Disconnected
        </span>
        break;
    case ConnectionStatus.Error:
        <span class="badge bg-danger">
            <i class="fas fa-exclamation-triangle"></i> Error
        </span>
        break;
    default:
        <span class="badge bg-secondary">Unknown</span>
        break;
}
'@

$connectionStatusBadgeContent | Out-File -FilePath "src\CloudMigrationTool.Web\Views\Shared\_ConnectionStatusBadge.cshtml" -Force -Encoding UTF8

Write-Host "`nRazor views created successfully!" -ForegroundColor Green
Write-Host "Next: Run 08-Create-Static-Files.ps1 to create CSS, JS, and static resources" -ForegroundColor Yellow