using CloudMigrationTool.Core.Interfaces;
using CloudMigrationTool.Core.Models;
using CloudMigrationTool.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Azure.Identity;
using System.Runtime.Versioning;

namespace CloudMigrationTool.Infrastructure.External
{
    public class RealActiveDirectoryService : IActiveDirectoryService
    {
        private readonly ILogger<RealActiveDirectoryService> _logger;

        public RealActiveDirectoryService(ILogger<RealActiveDirectoryService> logger)
        {
            _logger = logger;
        }

        public async Task<ConnectionTestResult> TestConnectionAsync(string connectionString)
        {
            try
            {
                var config = AdConnectionConfig.ParseConnectionString(connectionString);
                _logger.LogInformation("Testing {ConnectionType} AD connection", config.ConnectionType);

                if (config.ConnectionType.Equals("AzureAD", StringComparison.OrdinalIgnoreCase))
                {
                    return await TestAzureAdConnectionAsync(config);
                }
                else
                {
                    return await TestOnPremisesAdConnectionAsync(config);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test Active Directory connection");
                return new ConnectionTestResult
                {
                    IsSuccessful = false,
                    Status = ConnectionStatus.Error,
                    Message = $"Connection test failed: {ex.Message}",
                    TestTime = DateTime.UtcNow
                };
            }
        }

        public async Task<IEnumerable<AdUser>> GetUsersAsync(string? searchFilter = null)
        {
            // For demo, we'll use Azure AD by default
            // In production, this would come from the connection configuration
            var connectionString = GetDefaultConnectionString();
            var config = AdConnectionConfig.ParseConnectionString(connectionString);

            if (config.ConnectionType.Equals("AzureAD", StringComparison.OrdinalIgnoreCase))
            {
                return await GetAzureAdUsersAsync(config, searchFilter);
            }
            else
            {
                return await GetOnPremisesUsersAsync(config, searchFilter);
            }
        }

        public async Task<AdUser?> GetUserAsync(string userPrincipalName)
        {
            var users = await GetUsersAsync();
            return users.FirstOrDefault(u =>
                u.UserPrincipalName?.Equals(userPrincipalName, StringComparison.OrdinalIgnoreCase) == true);
        }

        public async Task<IEnumerable<AdGroup>> GetGroupsAsync()
        {
            var connectionString = GetDefaultConnectionString();
            var config = AdConnectionConfig.ParseConnectionString(connectionString);

            if (config.ConnectionType.Equals("AzureAD", StringComparison.OrdinalIgnoreCase))
            {
                return await GetAzureAdGroupsAsync(config);
            }
            else
            {
                return await GetOnPremisesGroupsAsync(config);
            }
        }

        public async Task<IEnumerable<AdOrganizationalUnit>> GetOrganizationalUnitsAsync()
        {
            var connectionString = GetDefaultConnectionString();
            var config = AdConnectionConfig.ParseConnectionString(connectionString);

            if (config.ConnectionType.Equals("AzureAD", StringComparison.OrdinalIgnoreCase))
            {
                return await GetAzureAdOrganizationalUnitsAsync(config);
            }
            else
            {
                return await GetOnPremisesOrganizationalUnitsAsync(config);
            }
        }

        public async Task<AdGroup?> GetGroupAsync(string groupId)
        {
            var groups = await GetGroupsAsync();
            return groups.FirstOrDefault(g => g.Id.Equals(groupId, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<AdOrganizationalUnit?> GetOrganizationalUnitAsync(string ouId)
        {
            var ous = await GetOrganizationalUnitsAsync();
            return ous.FirstOrDefault(ou => ou.Id.Equals(ouId, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IEnumerable<AdUser>> GetGroupMembersAsync(string groupName)
        {
            var connectionString = GetDefaultConnectionString();
            var config = AdConnectionConfig.ParseConnectionString(connectionString);

            if (config.ConnectionType.Equals("AzureAD", StringComparison.OrdinalIgnoreCase))
            {
                return await GetAzureAdGroupMembersAsync(config, groupName);
            }
            else
            {
                return await GetOnPremisesGroupMembersAsync(config, groupName);
            }
        }

        #region Azure AD Methods

        private async Task<ConnectionTestResult> TestAzureAdConnectionAsync(AdConnectionConfig config)
        {
            try
            {
                if (string.IsNullOrEmpty(config.TenantId) || string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret))
                {
                    return new ConnectionTestResult
                    {
                        IsSuccessful = false,
                        Status = ConnectionStatus.Error,
                        Message = "Missing required Azure AD credentials (TenantId, ClientId, ClientSecret)",
                        TestTime = DateTime.UtcNow
                    };
                }

                var graphClient = CreateGraphClient(config);

                // Test by getting organization info
                var organization = await graphClient.Organization.GetAsync();
                var orgInfo = organization?.Value?.FirstOrDefault();

                return new ConnectionTestResult
                {
                    IsSuccessful = true,
                    Status = ConnectionStatus.Connected,
                    Message = "Successfully connected to Azure AD",
                    TestTime = DateTime.UtcNow,
                    AdditionalInfo = new Dictionary<string, string>
                    {
                        { "TenantId", config.TenantId },
                        { "OrganizationName", orgInfo?.DisplayName ?? "Unknown" },
                        { "ConnectionType", "Azure AD (Microsoft Graph)" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure AD connection test failed");
                return new ConnectionTestResult
                {
                    IsSuccessful = false,
                    Status = ConnectionStatus.Error,
                    Message = $"Azure AD connection failed: {ex.Message}",
                    TestTime = DateTime.UtcNow
                };
            }
        }

        private async Task<IEnumerable<AdUser>> GetAzureAdUsersAsync(AdConnectionConfig config, string? searchFilter = null)
        {
            try
            {
                var graphClient = CreateGraphClient(config);
                var users = new List<AdUser>();

                var usersPage = await graphClient.Users.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Select = new string[]
                    {
                        "id", "userPrincipalName", "displayName", "givenName", "surname",
                        "mail", "department", "jobTitle", "accountEnabled", "createdDateTime"
                    };
                    requestConfiguration.QueryParameters.Top = config.PageSize;

                    if (!string.IsNullOrEmpty(searchFilter))
                    {
                        requestConfiguration.QueryParameters.Filter =
                            $"startswith(displayName,'{searchFilter}') or startswith(userPrincipalName,'{searchFilter}')";
                    }
                });

                if (usersPage?.Value != null)
                {
                    foreach (var user in usersPage.Value)
                    {
                        var adUser = new AdUser
                        {
                            SamAccountName = user.UserPrincipalName?.Split('@')[0],
                            UserPrincipalName = user.UserPrincipalName,
                            DisplayName = user.DisplayName,
                            GivenName = user.GivenName,
                            Surname = user.Surname,
                            EmailAddress = user.Mail ?? user.UserPrincipalName,
                            Department = user.Department,
                            Title = user.JobTitle,
                            Enabled = user.AccountEnabled ?? false,
                            LastLogon = null, // Not available in basic Graph call
                            PasswordLastSet = user.CreatedDateTime?.DateTime,
                            DistinguishedName = $"CN={user.DisplayName},OU=Users,DC=AzureAD",
                            MemberOf = new List<string>() // Groups would require separate call
                        };

                        users.Add(adUser);
                    }
                }

                _logger.LogInformation("Retrieved {Count} users from Azure AD", users.Count);
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Azure AD users");
                throw new InvalidOperationException($"Failed to retrieve Azure AD users: {ex.Message}", ex);
            }
        }

        private async Task<IEnumerable<AdGroup>> GetAzureAdGroupsAsync(AdConnectionConfig config)
        {
            try
            {
                var graphClient = CreateGraphClient(config);
                var groups = new List<AdGroup>();

                var groupsPage = await graphClient.Groups.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Select = new string[] {
                        "id", "displayName", "mailNickname", "description", "groupTypes", "createdDateTime"
                    };
                    requestConfiguration.QueryParameters.Top = config.PageSize;
                });

                if (groupsPage?.Value != null)
                {
                    foreach (var group in groupsPage.Value)
                    {
                        var adGroup = new AdGroup
                        {
                            Id = group.Id ?? string.Empty,
                            Name = group.MailNickname ?? group.DisplayName ?? "Unknown",
                            DisplayName = group.DisplayName ?? string.Empty,
                            Description = group.Description ?? string.Empty,
                            GroupType = group.GroupTypes?.FirstOrDefault() ?? "Security",
                            Scope = "Global",
                            MemberCount = 0, // Would require separate call
                            DistinguishedName = $"CN={group.DisplayName},OU=Groups,DC=AzureAD",
                            Created = group.CreatedDateTime?.DateTime,
                            Modified = group.CreatedDateTime?.DateTime
                        };
                        groups.Add(adGroup);
                    }
                }

                return groups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Azure AD groups");
                throw new InvalidOperationException($"Failed to retrieve Azure AD groups: {ex.Message}", ex);
            }
        }

        private async Task<IEnumerable<AdUser>> GetAzureAdGroupMembersAsync(AdConnectionConfig config, string groupName)
        {
            try
            {
                var graphClient = CreateGraphClient(config);
                var members = new List<AdUser>();

                // First find the group by name
                var groups = await graphClient.Groups.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Filter = $"displayName eq '{groupName}'";
                    requestConfiguration.QueryParameters.Select = new string[] { "id", "displayName" };
                });

                var group = groups?.Value?.FirstOrDefault();
                if (group?.Id == null) return members;

                // Get group members
                var membersPage = await graphClient.Groups[group.Id].Members.GetAsync();

                if (membersPage?.Value != null)
                {
                    foreach (var member in membersPage.Value.OfType<Microsoft.Graph.Models.User>())
                    {
                        var adUser = new AdUser
                        {
                            SamAccountName = member.UserPrincipalName?.Split('@')[0],
                            UserPrincipalName = member.UserPrincipalName,
                            DisplayName = member.DisplayName,
                            GivenName = member.GivenName,
                            Surname = member.Surname,
                            EmailAddress = member.Mail ?? member.UserPrincipalName,
                            Enabled = member.AccountEnabled ?? false
                        };

                        members.Add(adUser);
                    }
                }

                return members;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Azure AD group members for {GroupName}", groupName);
                return new List<AdUser>();
            }
        }

        private GraphServiceClient CreateGraphClient(AdConnectionConfig config)
        {
            var options = new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            };

            var clientSecretCredential = new ClientSecretCredential(
                config.TenantId,
                config.ClientId,
                config.ClientSecret,
                options);

            return new GraphServiceClient(clientSecretCredential);
        }

        #endregion

        #region On-Premises AD Methods

        private async Task<ConnectionTestResult> TestOnPremisesAdConnectionAsync(AdConnectionConfig config)
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                {
                    return new ConnectionTestResult
                    {
                        IsSuccessful = false,
                        Status = ConnectionStatus.Error,
                        Message = "On-premises Active Directory is only supported on Windows. For cross-platform support, use Azure AD.",
                        TestTime = DateTime.UtcNow
                    };
                }

                // Simulate connection test for now
                await Task.Delay(100);

                return new ConnectionTestResult
                {
                    IsSuccessful = true,
                    Status = ConnectionStatus.Connected,
                    Message = "On-premises AD connection simulated (Windows AD integration requires System.DirectoryServices)",
                    TestTime = DateTime.UtcNow,
                    AdditionalInfo = new Dictionary<string, string>
                    {
                        { "Domain", config.Domain ?? "Current Domain" },
                        { "Server", config.Server ?? "Default" },
                        { "ConnectionType", "On-Premises Active Directory (Simulated)" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "On-premises AD connection test failed");
                return new ConnectionTestResult
                {
                    IsSuccessful = false,
                    Status = ConnectionStatus.Error,
                    Message = $"On-premises AD connection failed: {ex.Message}",
                    TestTime = DateTime.UtcNow
                };
            }
        }

        private async Task<IEnumerable<AdUser>> GetOnPremisesUsersAsync(AdConnectionConfig config, string? searchFilter = null)
        {
            return await Task.Run(() =>
            {
                var users = new List<AdUser>();

                try
                {
                    if (!OperatingSystem.IsWindows())
                    {
                        throw new PlatformNotSupportedException("On-premises Active Directory is only supported on Windows platforms.");
                    }

                    _logger.LogInformation("Connecting to on-premises AD: Domain={Domain}, Server={Server}",
                        config.Domain ?? "Current Domain", config.Server ?? "Default");

                    using (var context = CreatePrincipalContext(config))
                    {
                        using (var searcher = new System.DirectoryServices.AccountManagement.UserPrincipal(context))
                        {
                            // Apply search filter if provided
                            if (!string.IsNullOrEmpty(searchFilter))
                            {
                                searcher.DisplayName = $"*{searchFilter}*";
                            }

                            using (var results = new System.DirectoryServices.AccountManagement.PrincipalSearcher(searcher))
                            {
                                var foundUsers = results.FindAll().OfType<System.DirectoryServices.AccountManagement.UserPrincipal>()
                                    .Take(config.PageSize);

                                foreach (var user in foundUsers)
                                {
                                    try
                                    {
                                        var adUser = new AdUser
                                        {
                                            SamAccountName = user.SamAccountName,
                                            UserPrincipalName = user.UserPrincipalName,
                                            DisplayName = user.DisplayName,
                                            GivenName = user.GivenName,
                                            Surname = user.Surname,
                                            EmailAddress = user.EmailAddress,
                                            Enabled = user.Enabled ?? false,
                                            LastLogon = user.LastLogon,
                                            PasswordLastSet = user.LastPasswordSet,
                                            DistinguishedName = user.DistinguishedName,
                                            MemberOf = GetUserGroups(user)
                                        };

                                        // Get additional properties using DirectoryEntry
                                        using (var directoryEntry = user.GetUnderlyingObject() as System.DirectoryServices.DirectoryEntry)
                                        {
                                            if (directoryEntry != null)
                                            {
                                                adUser.Department = GetDirectoryProperty(directoryEntry, "department");
                                                adUser.Title = GetDirectoryProperty(directoryEntry, "title");
                                                adUser.Manager = GetDirectoryProperty(directoryEntry, "manager");
                                            }
                                        }

                                        users.Add(adUser);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to process user {UserName}", user.SamAccountName);
                                    }
                                }
                            }
                        }
                    }

                    _logger.LogInformation("Retrieved {Count} users from on-premises AD", users.Count);
                    return users;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve on-premises AD users");
                    throw new InvalidOperationException($"Failed to connect to on-premises Active Directory: {ex.Message}", ex);
                }
            });
        }

        private async Task<IEnumerable<AdGroup>> GetOnPremisesGroupsAsync(AdConnectionConfig config)
        {
            return await Task.Run(() =>
            {
                var groups = new List<AdGroup>();

                try
                {
                    if (!OperatingSystem.IsWindows())
                    {
                        throw new PlatformNotSupportedException("On-premises Active Directory is only supported on Windows platforms.");
                    }

                    using (var context = CreatePrincipalContext(config))
                    {
                        using (var searcher = new System.DirectoryServices.AccountManagement.GroupPrincipal(context))
                        {
                            using (var results = new System.DirectoryServices.AccountManagement.PrincipalSearcher(searcher))
                            {
                                var foundGroups = results.FindAll().OfType<System.DirectoryServices.AccountManagement.GroupPrincipal>()
                                    .Take(config.PageSize);

                                foreach (var group in foundGroups)
                                {
                                    try
                                    {
                                        var adGroup = new AdGroup
                                        {
                                            Id = group.Guid?.ToString() ?? group.SamAccountName ?? "Unknown",
                                            Name = group.SamAccountName ?? "Unknown",
                                            DisplayName = group.DisplayName ?? group.Name ?? "Unknown",
                                            Description = group.Description ?? string.Empty,
                                            DistinguishedName = group.DistinguishedName
                                        };

                                        groups.Add(adGroup);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to process group {GroupName}", group.SamAccountName);
                                    }
                                }
                            }
                        }
                    }

                    _logger.LogInformation("Retrieved {Count} groups from on-premises AD", groups.Count);
                    return groups;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve on-premises AD groups");
                    throw new InvalidOperationException($"Failed to retrieve groups from on-premises Active Directory: {ex.Message}", ex);
                }
            });
        }

        private async Task<IEnumerable<AdOrganizationalUnit>> GetAzureAdOrganizationalUnitsAsync(AdConnectionConfig config)
        {
            // Azure AD doesn't have traditional OUs, return empty list
            _logger.LogInformation("Azure AD does not have traditional Organizational Units. Returning empty list.");
            await Task.Delay(50);
            return new List<AdOrganizationalUnit>();
        }

        private async Task<IEnumerable<AdOrganizationalUnit>> GetOnPremisesOrganizationalUnitsAsync(AdConnectionConfig config)
        {
            return await Task.Run(() =>
            {
                var organizationalUnits = new List<AdOrganizationalUnit>();

                try
                {
                    if (!OperatingSystem.IsWindows())
                    {
                        throw new PlatformNotSupportedException("On-premises Active Directory is only supported on Windows platforms.");
                    }

                    using (var context = CreatePrincipalContext(config))
                    {
                        // Basic OU implementation - in a full implementation you'd use DirectorySearcher
                        _logger.LogInformation("On-premises OU retrieval is simplified in this implementation");
                        return organizationalUnits;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve on-premises AD organizational units");
                    throw new InvalidOperationException($"Failed to retrieve organizational units from on-premises Active Directory: {ex.Message}", ex);
                }
            });
        }

        private async Task<IEnumerable<AdUser>> GetOnPremisesGroupMembersAsync(AdConnectionConfig config, string groupName)
        {
            return await Task.Run(() =>
            {
                var users = new List<AdUser>();

                try
                {
                    if (!OperatingSystem.IsWindows())
                    {
                        throw new PlatformNotSupportedException("On-premises Active Directory is only supported on Windows platforms.");
                    }

                    using (var context = CreatePrincipalContext(config))
                    {
                        using (var groupSearcher = new System.DirectoryServices.AccountManagement.GroupPrincipal(context))
                        {
                            groupSearcher.Name = groupName;

                            using (var groupResults = new System.DirectoryServices.AccountManagement.PrincipalSearcher(groupSearcher))
                            {
                                var group = groupResults.FindOne() as System.DirectoryServices.AccountManagement.GroupPrincipal;
                                if (group != null)
                                {
                                    var members = group.GetMembers(false);

                                    foreach (var member in members.OfType<System.DirectoryServices.AccountManagement.UserPrincipal>())
                                    {
                                        try
                                        {
                                            var adUser = new AdUser
                                            {
                                                SamAccountName = member.SamAccountName,
                                                UserPrincipalName = member.UserPrincipalName,
                                                DisplayName = member.DisplayName,
                                                GivenName = member.GivenName,
                                                Surname = member.Surname,
                                                EmailAddress = member.EmailAddress,
                                                Enabled = member.Enabled ?? false,
                                                LastLogon = member.LastLogon,
                                                PasswordLastSet = member.LastPasswordSet,
                                                DistinguishedName = member.DistinguishedName,
                                                MemberOf = GetUserGroups(member)
                                            };

                                            users.Add(adUser);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogWarning(ex, "Failed to process group member {UserName}", member.SamAccountName);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return users;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve group members from on-premises AD");
                    throw new InvalidOperationException($"Failed to retrieve group members from on-premises Active Directory: {ex.Message}", ex);
                }
            });
        }

        #endregion

        #region Helper Methods

        private string GetDefaultConnectionString()
        {
            // Check if we have Azure AD environment variables
            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            if (!string.IsNullOrEmpty(tenantId))
            {
                return $"Type=AzureAD;TenantId={tenantId};" +
                       $"ClientId={Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")};" +
                       $"ClientSecret={Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET")}";
            }

            // Default to simulated on-premises AD
            return "Type=OnPremises;Domain=contoso.local;IncludeDisabled=false;PageSize=100";
        }

        private IEnumerable<AdUser> GetSimulatedUsers()
        {
            return new[]
            {
                new AdUser
                {
                    SamAccountName = "john.doe",
                    UserPrincipalName = "john.doe@contoso.com",
                    DisplayName = "John Doe",
                    GivenName = "John",
                    Surname = "Doe",
                    EmailAddress = "john.doe@contoso.com",
                    Department = "IT",
                    Title = "Software Developer",
                    Manager = "jane.smith@contoso.com",
                    Enabled = true,
                    LastLogon = DateTime.Now.AddDays(-1),
                    PasswordLastSet = DateTime.Now.AddDays(-30),
                    DistinguishedName = "CN=John Doe,OU=Users,DC=contoso,DC=com",
                    MemberOf = new List<string> { "IT Staff", "Developers" }
                },
                new AdUser
                {
                    SamAccountName = "jane.smith",
                    UserPrincipalName = "jane.smith@contoso.com",
                    DisplayName = "Jane Smith",
                    GivenName = "Jane",
                    Surname = "Smith",
                    EmailAddress = "jane.smith@contoso.com",
                    Department = "IT",
                    Title = "IT Manager",
                    Manager = "admin@contoso.com",
                    Enabled = true,
                    LastLogon = DateTime.Now.AddHours(-4),
                    PasswordLastSet = DateTime.Now.AddDays(-45),
                    DistinguishedName = "CN=Jane Smith,OU=Users,DC=contoso,DC=com",
                    MemberOf = new List<string> { "IT Staff", "Managers" }
                },
                new AdUser
                {
                    SamAccountName = "mike.johnson",
                    UserPrincipalName = "mike.johnson@contoso.com",
                    DisplayName = "Mike Johnson",
                    GivenName = "Mike",
                    Surname = "Johnson",
                    EmailAddress = "mike.johnson@contoso.com",
                    Department = "Sales",
                    Title = "Sales Representative",
                    Manager = "sarah.wilson@contoso.com",
                    Enabled = true,
                    LastLogon = DateTime.Now.AddDays(-2),
                    PasswordLastSet = DateTime.Now.AddDays(-60),
                    DistinguishedName = "CN=Mike Johnson,OU=Users,DC=contoso,DC=com",
                    MemberOf = new List<string> { "Sales Team" }
                }
            };
        }

        private IEnumerable<AdGroup> GetSimulatedGroups()
        {
            return new[]
            {
                new AdGroup
                {
                    Id = "grp-001",
                    Name = "IT Staff",
                    DisplayName = "IT Staff",
                    Description = "Information Technology Staff",
                    GroupType = "Security",
                    Scope = "Global",
                    MemberCount = 25,
                    DistinguishedName = "CN=IT Staff,OU=Groups,DC=contoso,DC=com",
                    Created = DateTime.Now.AddYears(-2),
                    Modified = DateTime.Now.AddDays(-5)
                },
                new AdGroup
                {
                    Id = "grp-002",
                    Name = "Developers",
                    DisplayName = "Developers",
                    Description = "Software Development Team",
                    GroupType = "Security",
                    Scope = "Global",
                    MemberCount = 15,
                    DistinguishedName = "CN=Developers,OU=Groups,DC=contoso,DC=com",
                    Created = DateTime.Now.AddYears(-2),
                    Modified = DateTime.Now.AddDays(-10)
                },
                new AdGroup
                {
                    Id = "grp-003",
                    Name = "Managers",
                    DisplayName = "Managers",
                    Description = "Management Team",
                    GroupType = "Security",
                    Scope = "Global",
                    MemberCount = 8,
                    DistinguishedName = "CN=Managers,OU=Groups,DC=contoso,DC=com",
                    Created = DateTime.Now.AddYears(-3),
                    Modified = DateTime.Now.AddDays(-15)
                },
                new AdGroup
                {
                    Id = "grp-004",
                    Name = "Sales Team",
                    DisplayName = "Sales Team",
                    Description = "Sales and Marketing",
                    GroupType = "Distribution",
                    Scope = "Universal",
                    MemberCount = 30,
                    DistinguishedName = "CN=Sales Team,OU=Groups,DC=contoso,DC=com",
                    Created = DateTime.Now.AddYears(-1),
                    Modified = DateTime.Now.AddDays(-3)
                }
            };
        }

        private IEnumerable<AdOrganizationalUnit> GetSimulatedOrganizationalUnits()
        {
            return new[]
            {
                new AdOrganizationalUnit
                {
                    Id = "ou-001",
                    Name = "Users",
                    DistinguishedName = "OU=Users,DC=contoso,DC=com",
                    Description = "User accounts",
                    ParentOU = "DC=contoso,DC=com",
                    UserCount = 150,
                    ComputerCount = 0,
                    GroupCount = 5,
                    ChildOUCount = 3,
                    Created = DateTime.Now.AddYears(-5),
                    Modified = DateTime.Now.AddMonths(-1)
                },
                new AdOrganizationalUnit
                {
                    Id = "ou-002",
                    Name = "Computers",
                    DistinguishedName = "OU=Computers,DC=contoso,DC=com",
                    Description = "Computer accounts",
                    ParentOU = "DC=contoso,DC=com",
                    UserCount = 0,
                    ComputerCount = 200,
                    GroupCount = 2,
                    ChildOUCount = 4,
                    Created = DateTime.Now.AddYears(-5),
                    Modified = DateTime.Now.AddDays(-7)
                },
                new AdOrganizationalUnit
                {
                    Id = "ou-003",
                    Name = "IT Department",
                    DistinguishedName = "OU=IT Department,OU=Users,DC=contoso,DC=com",
                    Description = "IT Department users",
                    ParentOU = "OU=Users,DC=contoso,DC=com",
                    UserCount = 25,
                    ComputerCount = 0,
                    GroupCount = 3,
                    ChildOUCount = 0,
                    Created = DateTime.Now.AddYears(-3),
                    Modified = DateTime.Now.AddDays(-14)
                },
                new AdOrganizationalUnit
                {
                    Id = "ou-004",
                    Name = "Sales Department",
                    DistinguishedName = "OU=Sales Department,OU=Users,DC=contoso,DC=com",
                    Description = "Sales Department users",
                    ParentOU = "OU=Users,DC=contoso,DC=com",
                    UserCount = 30,
                    ComputerCount = 0,
                    GroupCount = 2,
                    ChildOUCount = 0,
                    Created = DateTime.Now.AddYears(-3),
                    Modified = DateTime.Now.AddDays(-21)
                }
            };
        }

        private System.DirectoryServices.AccountManagement.PrincipalContext CreatePrincipalContext(AdConnectionConfig config)
        {
            if (!string.IsNullOrEmpty(config.Domain) && !string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password))
            {
                // Use specific domain and credentials
                return new System.DirectoryServices.AccountManagement.PrincipalContext(
                    System.DirectoryServices.AccountManagement.ContextType.Domain,
                    config.Domain,
                    config.Username,
                    config.Password);
            }
            else if (!string.IsNullOrEmpty(config.Domain))
            {
                // Use specific domain with current user credentials
                return new System.DirectoryServices.AccountManagement.PrincipalContext(
                    System.DirectoryServices.AccountManagement.ContextType.Domain,
                    config.Domain);
            }
            else
            {
                // Use current domain with current user credentials
                return new System.DirectoryServices.AccountManagement.PrincipalContext(
                    System.DirectoryServices.AccountManagement.ContextType.Domain);
            }
        }

        private List<string> GetUserGroups(System.DirectoryServices.AccountManagement.UserPrincipal user)
        {
            var groups = new List<string>();
            try
            {
                var userGroups = user.GetGroups();
                foreach (var group in userGroups)
                {
                    groups.Add(group.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get groups for user {UserName}", user.SamAccountName);
            }
            return groups;
        }

        private string? GetDirectoryProperty(System.DirectoryServices.DirectoryEntry directoryEntry, string propertyName)
        {
            try
            {
                if (directoryEntry.Properties.Contains(propertyName))
                {
                    var property = directoryEntry.Properties[propertyName];
                    return property.Value?.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get property {PropertyName}", propertyName);
            }
            return null;
        }

        #endregion
    }
}