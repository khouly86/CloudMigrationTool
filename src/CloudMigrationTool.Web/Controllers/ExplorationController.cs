using CloudMigrationTool.Core.Interfaces;
using CloudMigrationTool.Core.Models;
using CloudMigrationTool.Core.Entities;
using CloudMigrationTool.Core.Enums;
using CloudMigrationTool.Web.Models;
using CloudMigrationTool.Services.Admin;
using CloudMigrationTool.Services.Exploration;
using Microsoft.AspNetCore.Mvc;

namespace CloudMigrationTool.Web.Controllers
{
    public class ExplorationController : Controller
    {
        private readonly ILogger<ExplorationController> _logger;
        private readonly IActiveDirectoryService _adService;
        private readonly IExchangeService _exchangeService;
        private readonly IConnectionSettingsService _connectionService;
        private readonly IExplorationService _explorationService;

        public ExplorationController(
            ILogger<ExplorationController> logger,
            IActiveDirectoryService adService,
            IExchangeService exchangeService,
            IConnectionSettingsService connectionService,
            IExplorationService explorationService)
        {
            _logger = logger;
            _adService = adService;
            _exchangeService = exchangeService;
            _connectionService = connectionService;
            _explorationService = explorationService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> TestConnection()
        {
            var allConnections = await _connectionService.GetAllAsync();
            var viewModel = new ConnectionTestViewModel
            {
                AvailableConnections = allConnections.ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ActiveDirectory(int? connectionId)
        {
            var allConnections = await _connectionService.GetAllAsync();
            var viewModel = new ActiveDirectoryExplorationViewModel
            {
                ConnectionId = connectionId,
                AvailableConnections = allConnections.Where(c =>
                    c.Type == ConnectionType.ActiveDirectory || c.Type == ConnectionType.EntraId).ToList()
            };

            if (connectionId.HasValue)
            {
                try
                {
                    var connection = await _connectionService.GetByIdAsync(connectionId.Value);
                    if (connection != null)
                    {
                        viewModel.Users = await _explorationService.GetUsersForConnectionAsync(connection);
                        viewModel.Groups = await _explorationService.GetGroupsForConnectionAsync(connection);
                        viewModel.OrganizationalUnits = await _explorationService.GetOrganizationalUnitsForConnectionAsync(connection);
                        viewModel.IsConnected = true;
                        _logger.LogInformation("Successfully loaded AD objects for exploration using connection {ConnectionName}", connection.Name);
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Connection not found.";
                        viewModel.IsConnected = false;
                    }
                }
                catch (NotImplementedException ex)
                {
                    _logger.LogError(ex, "AD integration not implemented");
                    ViewBag.ErrorMessage = "On-premises Active Directory integration is not implemented. This feature requires Windows and System.DirectoryServices packages. Please use Azure AD/Entra ID connections instead.";
                    viewModel.IsConnected = false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load AD objects");
                    ViewBag.ErrorMessage = $"Failed to connect to Active Directory: {ex.Message}";
                    viewModel.IsConnected = false;
                }
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Exchange(int? connectionId)
        {
            var allConnections = await _connectionService.GetAllAsync();
            var viewModel = new ExchangeExplorationViewModel
            {
                ConnectionId = connectionId,
                AvailableConnections = allConnections.Where(c =>
                    c.Type == ConnectionType.Exchange).ToList()
            };

            if (connectionId.HasValue)
            {
                try
                {
                    var connection = await _connectionService.GetByIdAsync(connectionId.Value);
                    if (connection != null)
                    {
                        viewModel.Mailboxes = await _explorationService.GetMailboxesForConnectionAsync(connection);
                        viewModel.IsConnected = true;
                        _logger.LogInformation("Successfully loaded Exchange mailboxes for exploration using connection {ConnectionName}", connection.Name);
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Connection not found.";
                        viewModel.IsConnected = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load Exchange mailboxes");
                    ViewBag.ErrorMessage = "Failed to connect to Exchange Server. Please check your connection settings.";
                    viewModel.IsConnected = false;
                }
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ExchangeOnline(int? connectionId)
        {
            var allConnections = await _connectionService.GetAllAsync();
            var viewModel = new ExchangeOnlineExplorationViewModel
            {
                ConnectionId = connectionId,
                AvailableConnections = allConnections.Where(c =>
                    c.Type == ConnectionType.ExchangeOnline).ToList()
            };

            if (connectionId.HasValue)
            {
                try
                {
                    var connection = await _connectionService.GetByIdAsync(connectionId.Value);
                    if (connection != null)
                    {
                        viewModel.Mailboxes = await _explorationService.GetMailboxesForConnectionAsync(connection);
                        viewModel.DistributionGroups = await _explorationService.GetDistributionGroupsForConnectionAsync(connection);
                        viewModel.SharedMailboxes = await _explorationService.GetSharedMailboxesForConnectionAsync(connection);
                        viewModel.IsConnected = true;
                        _logger.LogInformation("Successfully loaded Exchange Online mailboxes for exploration using connection {ConnectionName}", connection.Name);
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Connection not found.";
                        viewModel.IsConnected = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load Exchange Online mailboxes");
                    ViewBag.ErrorMessage = "Failed to connect to Exchange Online. Please check your connection settings.";
                    viewModel.IsConnected = false;
                }
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EntraId(int? connectionId)
        {
            var allConnections = await _connectionService.GetAllAsync();
            var viewModel = new EntraIdExplorationViewModel
            {
                ConnectionId = connectionId,
                AvailableConnections = allConnections.Where(c =>
                    c.Type == ConnectionType.EntraId).ToList()
            };

            if (connectionId.HasValue)
            {
                try
                {
                    var connection = await _connectionService.GetByIdAsync(connectionId.Value);
                    if (connection != null)
                    {
                        viewModel.Users = await _explorationService.GetUsersForConnectionAsync(connection);
                        viewModel.Groups = await _explorationService.GetGroupsForConnectionAsync(connection);
                        // TODO: Implement real applications and roles retrieval from Azure AD
                        viewModel.Applications = new List<CloudMigrationTool.Core.Models.EntraIdApplication>();
                        viewModel.Roles = new List<CloudMigrationTool.Core.Models.EntraIdRole>();
                        viewModel.IsConnected = true;
                        _logger.LogInformation("Successfully loaded Entra ID objects for exploration using connection {ConnectionName}", connection.Name);
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Connection not found.";
                        viewModel.IsConnected = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load Entra ID objects");
                    ViewBag.ErrorMessage = "Failed to connect to Entra ID. Please check your connection settings.";
                    viewModel.IsConnected = false;
                }
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> GetADUserDetails(string userId, int connectionId)
        {
            try
            {
                var connection = await _connectionService.GetByIdAsync(connectionId);
                if (connection == null)
                {
                    return BadRequest(new { error = "Connection not found" });
                }

                var user = await _explorationService.GetUserForConnectionAsync(connection, userId);
                return Json(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get AD user details for {UserId} using connection {ConnectionId}", userId, connectionId);
                return BadRequest(new { error = $"Failed to load user details: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetMailboxDetails(string emailAddress)
        {
            try
            {
                // For now, use the default service - in future we could pass connectionId
                var mailbox = await _exchangeService.GetMailboxAsync(emailAddress);
                return Json(mailbox);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get mailbox details for {Email}", emailAddress);
                return BadRequest(new { error = "Failed to load mailbox details" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestConnection(int connectionId)
        {
            try
            {
                var connection = await _connectionService.GetByIdAsync(connectionId);
                if (connection == null)
                {
                    return BadRequest(new { error = "Connection not found" });
                }

                var testResult = await _explorationService.TestConnectionAsync(connection);

                _logger.LogInformation("Connection test completed for {ConnectionName}: {IsSuccessful}",
                    connection.Name, testResult.IsSuccessful);

                return Json(new
                {
                    isSuccessful = testResult.IsSuccessful,
                    status = testResult.Status.ToString(),
                    message = testResult.Message,
                    testTime = testResult.TestTime,
                    additionalInfo = testResult.AdditionalInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test connection {ConnectionId}", connectionId);
                return BadRequest(new { error = $"Failed to test connection: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult SaveSelection([FromBody] ItemSelectionRequest request)
        {
            try
            {
                HttpContext.Session.SetString($"Selection_{request.Type}_{request.ConnectionId}",
                    System.Text.Json.JsonSerializer.Serialize(request.SelectedItems));

                _logger.LogInformation("Saved {Count} selected items for {Type}",
                    request.SelectedItems.Count, request.Type);

                return Ok(new { success = true, count = request.SelectedItems.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save selection");
                return BadRequest(new { error = "Failed to save selection" });
            }
        }

        [HttpGet]
        public IActionResult GetSavedSelection(string type, int connectionId)
        {
            try
            {
                var key = $"Selection_{type}_{connectionId}";
                var selection = HttpContext.Session.GetString(key);

                if (string.IsNullOrEmpty(selection))
                {
                    return Json(new List<string>());
                }

                var items = System.Text.Json.JsonSerializer.Deserialize<List<string>>(selection);
                return Json(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get saved selection");
                return Json(new List<string>());
            }
        }

        private IEnumerable<CloudMigrationTool.Core.Models.DistributionGroup> GetSimulatedDistributionGroups()
        {
            return new[]
            {
                new CloudMigrationTool.Core.Models.DistributionGroup
                {
                    Id = "dg-001",
                    Name = "All Staff",
                    EmailAddress = "allstaff@contoso.com",
                    DisplayName = "All Staff",
                    MemberCount = 150,
                    ModeratedEnabled = false,
                    ManagedBy = new List<string> { "admin@contoso.com" },
                    Created = DateTime.Now.AddYears(-2)
                },
                new CloudMigrationTool.Core.Models.DistributionGroup
                {
                    Id = "dg-002",
                    Name = "IT Announcements",
                    EmailAddress = "it-announcements@contoso.com",
                    DisplayName = "IT Announcements",
                    MemberCount = 25,
                    ModeratedEnabled = true,
                    ManagedBy = new List<string> { "it-manager@contoso.com", "admin@contoso.com" },
                    Created = DateTime.Now.AddYears(-1)
                }
            };
        }

        private IEnumerable<CloudMigrationTool.Core.Models.SharedMailbox> GetSimulatedSharedMailboxes()
        {
            return new[]
            {
                new CloudMigrationTool.Core.Models.SharedMailbox
                {
                    Id = "sm-001",
                    Name = "Reception",
                    EmailAddress = "reception@contoso.com",
                    DisplayName = "Reception Desk",
                    TotalSize = 1024 * 1024 * 500, // 500MB
                    ItemCount = 1200,
                    Delegates = new List<string> { "receptionist1@contoso.com", "receptionist2@contoso.com" },
                    LastAccessed = DateTime.Now.AddDays(-1)
                },
                new CloudMigrationTool.Core.Models.SharedMailbox
                {
                    Id = "sm-002",
                    Name = "Info",
                    EmailAddress = "info@contoso.com",
                    DisplayName = "General Information",
                    TotalSize = 1024 * 1024 * 250, // 250MB
                    ItemCount = 800,
                    Delegates = new List<string> { "admin@contoso.com", "manager@contoso.com" },
                    LastAccessed = DateTime.Now.AddDays(-3)
                }
            };
        }

        private IEnumerable<CloudMigrationTool.Core.Models.EntraIdApplication> GetSimulatedApplications()
        {
            return new[]
            {
                new CloudMigrationTool.Core.Models.EntraIdApplication
                {
                    Id = "app-001",
                    AppId = "12345678-1234-1234-1234-123456789012",
                    DisplayName = "Company Intranet",
                    Description = "Internal company portal application",
                    SignInAudience = "AzureADMyOrg",
                    ReplyUrls = new List<string> { "https://intranet.contoso.com/signin" },
                    RequiredResourceAccess = new List<string> { "User.Read", "Directory.Read.All" },
                    Created = DateTime.Now.AddYears(-1),
                    IsEnabled = true
                },
                new CloudMigrationTool.Core.Models.EntraIdApplication
                {
                    Id = "app-002",
                    AppId = "87654321-4321-4321-4321-210987654321",
                    DisplayName = "Customer Portal",
                    Description = "External customer facing portal",
                    SignInAudience = "AzureADMultipleOrgs",
                    ReplyUrls = new List<string> { "https://portal.contoso.com/auth", "https://portal.contoso.com/callback" },
                    RequiredResourceAccess = new List<string> { "User.Read", "Mail.Send" },
                    Created = DateTime.Now.AddMonths(-6),
                    IsEnabled = true
                }
            };
        }

        private IEnumerable<CloudMigrationTool.Core.Models.EntraIdRole> GetSimulatedRoles()
        {
            return new[]
            {
                new CloudMigrationTool.Core.Models.EntraIdRole
                {
                    Id = "role-001",
                    DisplayName = "Global Administrator",
                    Description = "Can manage all aspects of Azure AD and Microsoft services",
                    IsBuiltIn = true,
                    IsEnabled = true,
                    MemberCount = 2,
                    Permissions = new List<string> { "*" }
                },
                new CloudMigrationTool.Core.Models.EntraIdRole
                {
                    Id = "role-002",
                    DisplayName = "User Administrator",
                    Description = "Can manage users and groups",
                    IsBuiltIn = true,
                    IsEnabled = true,
                    MemberCount = 3,
                    Permissions = new List<string> { "User.ReadWrite.All", "Group.ReadWrite.All" }
                },
                new CloudMigrationTool.Core.Models.EntraIdRole
                {
                    Id = "role-003",
                    DisplayName = "Helpdesk Administrator",
                    Description = "Can reset passwords for non-administrators",
                    IsBuiltIn = true,
                    IsEnabled = true,
                    MemberCount = 5,
                    Permissions = new List<string> { "User.ReadBasic.All", "UserAuthenticationMethod.ReadWrite.All" }
                }
            };
        }

    }
}