# PowerShell Script to Create Web Application Files
# Step 6: Create web controllers, views, and static files

param(
    [string]$SolutionName = "CloudMigrationTool"
)

Write-Host "Creating Web Application Files..." -ForegroundColor Green

# Ensure we're in the solution directory
if (!(Test-Path "$SolutionName.sln")) {
    Write-Error "Solution file not found. Please run previous setup scripts first."
    exit 1
}

Write-Host "Creating Controllers..." -ForegroundColor Cyan

# Admin Controller
@"
using CloudMigrationTool.Core.Interfaces;
using CloudMigrationTool.Core.Entities;
using CloudMigrationTool.Core.Enums;
using CloudMigrationTool.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CloudMigrationTool.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfigurationService _configurationService;
        private readonly IActiveDirectoryService _activeDirectoryService;
        private readonly IExchangeService _exchangeService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IUnitOfWork unitOfWork,
            IConfigurationService configurationService,
            IActiveDirectoryService activeDirectoryService,
            IExchangeService exchangeService,
            ILogger<AdminController> logger)
        {
            _unitOfWork = unitOfWork;
            _configurationService = configurationService;
            _activeDirectoryService = activeDirectoryService;
            _exchangeService = exchangeService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var connections = await _unitOfWork.ConnectionSettings.GetAllAsync();
            var settings = await _unitOfWork.AppSettings.GetAllAsync();
            
            var viewModel = new AdminDashboardViewModel
            {
                Connections = connections.ToList(),
                Settings = settings.ToList()
            };
            
            return View(viewModel);
        }

        public async Task<IActionResult> Connections()
        {
            var connections = await _unitOfWork.ConnectionSettings.GetAllAsync();
            return View(connections);
        }

        public IActionResult CreateConnection()
        {
            var model = new ConnectionSettingsViewModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateConnection(ConnectionSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var connection = new ConnectionSettings
                {
                    Name = model.Name,
                    Type = model.Type,
                    Description = model.Description,
                    ConnectionString = model.ConnectionString,
                    AdditionalSettings = model.AdditionalSettings,
                    IsActive = model.IsActive
                };

                await _unitOfWork.ConnectionSettings.AddAsync(connection);
                await _unitOfWork.SaveChangesAsync();

                TempData["SuccessMessage"] = "Connection created successfully!";
                return RedirectToAction(nameof(Connections));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating connection");
                ModelState.AddModelError("", "Failed to create connection. Please try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> EditConnection(int id)
        {
            var connection = await _unitOfWork.ConnectionSettings.GetByIdAsync(id);
            if (connection == null)
            {
                return NotFound();
            }

            var model = new ConnectionSettingsViewModel
            {
                Id = connection.Id,
                Name = connection.Name,
                Type = connection.Type,
                Description = connection.Description,
                ConnectionString = connection.ConnectionString,
                AdditionalSettings = connection.AdditionalSettings,
                IsActive = connection.IsActive
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditConnection(ConnectionSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var connection = await _unitOfWork.ConnectionSettings.GetByIdAsync(model.Id);
                if (connection == null)
                {
                    return NotFound();
                }

                connection.Name = model.Name;
                connection.Type = model.Type;
                connection.Description = model.Description;
                connection.ConnectionString = model.ConnectionString;
                connection.AdditionalSettings = model.AdditionalSettings;
                connection.IsActive = model.IsActive;

                await _unitOfWork.ConnectionSettings.UpdateAsync(connection);
                await _unitOfWork.SaveChangesAsync();

                TempData["SuccessMessage"] = "Connection updated successfully!";
                return RedirectToAction(nameof(Connections));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating connection");
                ModelState.AddModelError("", "Failed to update connection. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestConnection(int id)
        {
            try
            {
                var connection = await _unitOfWork.ConnectionSettings.GetByIdAsync(id);
                if (connection == null)
                {
                    return Json(new { success = false, message = "Connection not found" });
                }

                var testResult = connection.Type switch
                {
                    MigrationType.ActiveDirectory => await _activeDirectoryService.TestConnectionAsync(connection.ConnectionString),
                    MigrationType.Exchange => await _exchangeService.TestConnectionAsync(connection.ConnectionString),
                    _ => new Core.Models.ConnectionTestResult
                    {
                        IsSuccessful = false,
                        Message = "Unsupported connection type"
                    }
                };

                // Update connection with test result
                connection.Status = testResult.Status;
                connection.LastTestedAt = testResult.TestTime;
                connection.LastTestResult = testResult.Message;
                await _unitOfWork.ConnectionSettings.UpdateAsync(connection);
                await _unitOfWork.SaveChangesAsync();

                return Json(new { 
                    success = testResult.IsSuccessful, 
                    message = testResult.Message,
                    status = testResult.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection {Id}", id);
                return Json(new { success = false, message = "Test failed: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConnection(int id)
        {
            try
            {
                await _unitOfWork.ConnectionSettings.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();
                TempData["SuccessMessage"] = "Connection deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting connection {Id}", id);
                TempData["ErrorMessage"] = "Failed to delete connection. Please try again.";
            }

            return RedirectToAction(nameof(Connections));
        }

        public async Task<IActionResult> Settings()
        {
            var settings = await _unitOfWork.AppSettings.GetAllAsync();
            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSetting(int id, string value)
        {
            try
            {
                var setting = await _unitOfWork.AppSettings.GetByIdAsync(id);
                if (setting == null || setting.IsReadOnly)
                {
                    return Json(new { success = false, message = "Setting not found or read-only" });
                }

                setting.Value = value;
                await _unitOfWork.AppSettings.UpdateAsync(setting);
                await _unitOfWork.SaveChangesAsync();

                return Json(new { success = true, message = "Setting updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating setting {Id}", id);
                return Json(new { success = false, message = "Failed to update setting" });
            }
        }
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Web\Controllers\AdminController.cs" -Encoding UTF8

# Migration Controller
@"
using CloudMigrationTool.Core.Interfaces;
using CloudMigrationTool.Core.Entities;
using CloudMigrationTool.Core.Enums;
using CloudMigrationTool.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CloudMigrationTool.Web.Controllers
{
    public class MigrationController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMigrationService _migrationService;
        private readonly ILogger<MigrationController> _logger;

        public MigrationController(
            IUnitOfWork unitOfWork,
            IMigrationService migrationService,
            ILogger<MigrationController> logger)
        {
            _unitOfWork = unitOfWork;
            _migrationService = migrationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var jobs = await _unitOfWork.MigrationJobs.GetAllAsync();
            return View(jobs);
        }

        public async Task<IActionResult> Dashboard()
        {
            var activeJobs = await _migrationService.GetActiveMigrationJobsAsync();
            var allJobs = await _unitOfWork.MigrationJobs.GetAllAsync();
            
            var viewModel = new MigrationDashboardViewModel
            {
                ActiveJobs = activeJobs.ToList(),
                RecentJobs = allJobs.OrderByDescending(j => j.CreatedAt).Take(10).ToList(),
                TotalJobs = allJobs.Count(),
                CompletedJobs = allJobs.Count(j => j.Status == MigrationStatus.Completed),
                FailedJobs = allJobs.Count(j => j.Status == MigrationStatus.Failed),
                InProgressJobs = allJobs.Count(j => j.Status == MigrationStatus.InProgress)
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Create()
        {
            var connections = await _unitOfWork.ConnectionSettings.FindAsync(c => c.IsActive);
            var model = new CreateMigrationJobViewModel
            {
                Connections = connections.ToList()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateMigrationJobViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var connections = await _unitOfWork.ConnectionSettings.FindAsync(c => c.IsActive);
                model.Connections = connections.ToList();
                return View(model);
            }

            try
            {
                var job = new MigrationJob
                {
                    Name = model.Name,
                    Description = model.Description,
                    Type = model.Type,
                    SourceConnectionId = model.SourceConnectionId,
                    DestinationConnectionId = model.DestinationConnectionId,
                    Configuration = model.Configuration
                };

                var jobId = await _migrationService.CreateMigrationJobAsync(job);
                TempData["SuccessMessage"] = "Migration job created successfully!";
                return RedirectToAction(nameof(Details), new { id = jobId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating migration job");
                ModelState.AddModelError("", "Failed to create migration job. Please try again.");
                
                var connections = await _unitOfWork.ConnectionSettings.FindAsync(c => c.IsActive);
                model.Connections = connections.ToList();
                return View(model);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var job = await _unitOfWork.MigrationJobs.GetByIdAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            var progress = await _migrationService.GetMigrationProgressAsync(id);
            var logs = await _unitOfWork.MigrationJobLogs.FindAsync(l => l.MigrationJobId == id);
            
            var viewModel = new MigrationJobDetailsViewModel
            {
                Job = job,
                Progress = progress,
                Logs = logs.OrderByDescending(l => l.Timestamp).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Start(int id)
        {
            try
            {
                var success = await _migrationService.StartMigrationJobAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Migration job started successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to start migration job.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting migration job {Id}", id);
                TempData["ErrorMessage"] = "Failed to start migration job.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Pause(int id)
        {
            try
            {
                var success = await _migrationService.PauseMigrationJobAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Migration job paused successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to pause migration job.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing migration job {Id}", id);
                TempData["ErrorMessage"] = "Failed to pause migration job.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var success = await _migrationService.CancelMigrationJobAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Migration job cancelled successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to cancel migration job.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling migration job {Id}", id);
                TempData["ErrorMessage"] = "Failed to cancel migration job.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> GetProgress(int id)
        {
            try
            {
                var progress = await _migrationService.GetMigrationProgressAsync(id);
                if (progress == null)
                {
                    return NotFound();
                }

                return Json(new
                {
                    totalItems = progress.TotalItems,
                    processedItems = progress.ProcessedItems,
                    successfulItems = progress.SuccessfulItems,
                    failedItems = progress.FailedItems,
                    percentageComplete = progress.PercentageComplete,
                    currentOperation = progress.CurrentOperation,
                    lastError = progress.LastError
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting progress for job {Id}", id);
                return StatusCode(500, "Failed to get progress");
            }
        }
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Web\Controllers\MigrationController.cs" -Encoding UTF8

# Update Home Controller
@"
using CloudMigrationTool.Core.Interfaces;
using CloudMigrationTool.Core.Enums;
using CloudMigrationTool.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CloudMigrationTool.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMigrationService _migrationService;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, IMigrationService migrationService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _migrationService = migrationService;
        }

        public async Task<IActionResult> Index()
        {
            var activeJobs = await _migrationService.GetActiveMigrationJobsAsync();
            var allJobs = await _unitOfWork.MigrationJobs.GetAllAsync();
            var connections = await _unitOfWork.ConnectionSettings.GetAllAsync();
            
            var viewModel = new HomeIndexViewModel
            {
                ActiveMigrationsCount = activeJobs.Count(),
                TotalJobsCount = allJobs.Count(),
                CompletedJobsCount = allJobs.Count(j => j.Status == MigrationStatus.Completed),
                FailedJobsCount = allJobs.Count(j => j.Status == MigrationStatus.Failed),
                ActiveConnectionsCount = connections.Count(c => c.Status == ConnectionStatus.Connected),
                TotalConnectionsCount = connections.Count(),
                RecentJobs = allJobs.OrderByDescending(j => j.CreatedAt).Take(5).ToList()
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Web\Controllers\HomeController.cs" -Force -Encoding UTF8

Write-Host "Creating View Models..." -ForegroundColor Cyan

# Base View Models
@"
using CloudMigrationTool.Core.Entities;

namespace CloudMigrationTool.Web.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }

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
"@ | Out-File -FilePath "src\CloudMigrationTool.Web\Models\BaseViewModels.cs" -Encoding UTF8

# Admin View Models
@"
using CloudMigrationTool.Core.Entities;
using CloudMigrationTool.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace CloudMigrationTool.Web.Models
{
    public class AdminDashboardViewModel
    {
        public List<ConnectionSettings> Connections { get; set; } = new List<ConnectionSettings>();
        public List<AppSettings> Settings { get; set; } = new List<AppSettings>();
    }

    public class ConnectionSettingsViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Connection Name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Connection Type")]
        public MigrationType Type { get; set; }

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Connection String")]
        public string ConnectionString { get; set; } = string.Empty;

        [Display(Name = "Additional Settings")]
        public string? AdditionalSettings { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Web\Models\AdminViewModels.cs" -Encoding UTF8

# Migration View Models
@"
using CloudMigrationTool.Core.Entities;
using CloudMigrationTool.Core.Models;
using CloudMigrationTool.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace CloudMigrationTool.Web.Models
{
    public class MigrationDashboardViewModel
    {
        public List<MigrationJob> ActiveJobs { get; set; } = new List<MigrationJob>();
        public List<MigrationJob> RecentJobs { get; set; } = new List<MigrationJob>();
        public int TotalJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int FailedJobs { get; set; }
        public int InProgressJobs { get; set; }
    }

    public class CreateMigrationJobViewModel
    {
        [Required]
        [Display(Name = "Job Name")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Migration Type")]
        public MigrationType Type { get; set; }

        [Required]
        [Display(Name = "Source Connection")]
        public int SourceConnectionId { get; set; }

        [Required]
        [Display(Name = "Destination Connection")]
        public int DestinationConnectionId { get; set; }

        [Display(Name = "Configuration (JSON)")]
        public string? Configuration { get; set; }

        public List<ConnectionSettings> Connections { get; set; } = new List<ConnectionSettings>();
    }

    public class MigrationJobDetailsViewModel
    {
        public MigrationJob Job { get; set; } = null!;
        public MigrationProgress? Progress { get; set; }
        public List<MigrationJobLog> Logs { get; set; } = new List<MigrationJobLog>();
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Web\Models\MigrationViewModels.cs" -Encoding UTF8

Write-Host "`nWeb application files created successfully!" -ForegroundColor Green
Write-Host "Next: Run 07-Create-Views.ps1 to create the Razor views" -ForegroundColor Yellow