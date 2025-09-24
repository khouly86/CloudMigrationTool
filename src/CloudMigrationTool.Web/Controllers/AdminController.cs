using CloudMigrationTool.Web.Models;
using CloudMigrationTool.Services.Admin;
using CloudMigrationTool.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CloudMigrationTool.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly IConnectionSettingsService _connectionService;

        public AdminController(ILogger<AdminController> logger, IConnectionSettingsService connectionService)
        {
            _logger = logger;
            _connectionService = connectionService;
        }

        public async Task<IActionResult> Index()
        {
            var connections = await _connectionService.GetAllAsync();
            var viewModel = new AdminDashboardViewModel
            {
                Connections = connections.ToList(),
                Settings = new List<CloudMigrationTool.Core.Entities.AppSettings>()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Connections()
        {
            try
            {
                var connections = await _connectionService.GetAllAsync();
                return View(connections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading connections");
                TempData["ErrorMessage"] = "Failed to load connections. Please try again.";
                return View(new List<ConnectionSettings>());
            }
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
                var connectionSettings = new ConnectionSettings
                {
                    Name = model.Name,
                    Type = model.Type,
                    Description = model.Description,
                    ConnectionString = model.ConnectionString,
                    AdditionalSettings = model.AdditionalSettings,
                    IsActive = model.IsActive,
                    Status = Core.Enums.ConnectionStatus.NotConfigured
                };

                await _connectionService.CreateAsync(connectionSettings);
                TempData["SuccessMessage"] = $"Connection '{model.Name}' created successfully!";
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
            try
            {
                var connectionSettings = await _connectionService.GetByIdAsync(id);
                if (connectionSettings == null)
                {
                    TempData["ErrorMessage"] = "Connection not found.";
                    return RedirectToAction(nameof(Connections));
                }

                var model = new ConnectionSettingsViewModel
                {
                    Id = connectionSettings.Id,
                    Name = connectionSettings.Name,
                    Type = connectionSettings.Type,
                    Description = connectionSettings.Description,
                    ConnectionString = connectionSettings.ConnectionString,
                    AdditionalSettings = connectionSettings.AdditionalSettings,
                    IsActive = connectionSettings.IsActive
                };

                return View("CreateConnection", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading connection for editing: {Id}", id);
                TempData["ErrorMessage"] = "Failed to load connection. Please try again.";
                return RedirectToAction(nameof(Connections));
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditConnection(ConnectionSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("CreateConnection", model);
            }

            try
            {
                var connectionSettings = new ConnectionSettings
                {
                    Id = model.Id,
                    Name = model.Name,
                    Type = model.Type,
                    Description = model.Description,
                    ConnectionString = model.ConnectionString,
                    AdditionalSettings = model.AdditionalSettings,
                    IsActive = model.IsActive
                };

                await _connectionService.UpdateAsync(connectionSettings);
                TempData["SuccessMessage"] = $"Connection '{model.Name}' updated successfully!";
                return RedirectToAction(nameof(Connections));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating connection");
                ModelState.AddModelError("", "Failed to update connection. Please try again.");
                return View("CreateConnection", model);
            }
        }

        public IActionResult Settings()
        {
            // Return empty list for now  
            var settings = new List<CloudMigrationTool.Core.Entities.AppSettings>();
            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> TestConnection(int id)
        {
            try
            {
                var success = await _connectionService.TestConnectionAsync(id);
                var connection = await _connectionService.GetByIdAsync(id);

                if (success)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Connection test successful! All services are reachable.",
                        status = connection?.Status.ToString() ?? "Connected"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Connection test failed: Unable to reach the remote service.",
                        status = connection?.Status.ToString() ?? "Error"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection {Id}", id);
                return Json(new
                {
                    success = false,
                    message = $"Connection test error: {ex.Message}",
                    status = "Error"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConnection(int id)
        {
            try
            {
                var deleted = await _connectionService.DeleteAsync(id);
                if (deleted)
                {
                    TempData["SuccessMessage"] = "Connection deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Connection not found.";
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot delete connection {Id} because it is in use", id);
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting connection {Id}", id);
                TempData["ErrorMessage"] = "Failed to delete connection. Please try again.";
            }

            return RedirectToAction(nameof(Connections));
        }
    }
}