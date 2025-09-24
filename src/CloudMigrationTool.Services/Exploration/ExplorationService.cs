using CloudMigrationTool.Core.Entities;
using CloudMigrationTool.Core.Models;
using CloudMigrationTool.Core.Enums;
using CloudMigrationTool.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Azure.Identity;

namespace CloudMigrationTool.Services.Exploration
{
    public class ExplorationService : IExplorationService
    {
        private readonly ILogger<ExplorationService> _logger;

        public ExplorationService(ILogger<ExplorationService> logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<AdUser>> GetUsersForConnectionAsync(ConnectionSettings connection, string? searchFilter = null)
        {
            try
            {
                if (!connection.IsActive)
                {
                    _logger.LogWarning("Connection {ConnectionName} is not active", connection.Name);
                    return new List<AdUser>();
                }

                _logger.LogInformation("Getting users for connection: {ConnectionName} of type {ConnectionType}",
                    connection.Name, connection.Type);

                var config = AdConnectionConfig.ParseConnectionString(connection.ConnectionString);

                if (connection.Type == ConnectionType.EntraId ||
                    config.ConnectionType.Equals("AzureAD", StringComparison.OrdinalIgnoreCase))
                {
                    return await GetAzureAdUsersAsync(config, searchFilter);
                }
                else if (connection.Type == ConnectionType.ActiveDirectory)
                {
                    return await GetOnPremisesUsersAsync(config, searchFilter);
                }

                return new List<AdUser>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get users for connection {ConnectionName}", connection.Name);
                throw; // Re-throw the exception so it can be handled by the controller
            }
        }

        public async Task<AdUser?> GetUserForConnectionAsync(ConnectionSettings connection, string userPrincipalName)
        {
            var users = await GetUsersForConnectionAsync(connection);
            return users.FirstOrDefault(u =>
                u.UserPrincipalName?.Equals(userPrincipalName, StringComparison.OrdinalIgnoreCase) == true);
        }

        public async Task<IEnumerable<AdGroup>> GetGroupsForConnectionAsync(ConnectionSettings connection)
        {
            try
            {
                if (!connection.IsActive)
                {
                    return new List<AdGroup>();
                }

                _logger.LogInformation("Getting groups for connection: {ConnectionName}", connection.Name);

                var config = AdConnectionConfig.ParseConnectionString(connection.ConnectionString);

                if (connection.Type == ConnectionType.EntraId ||
                    config.ConnectionType.Equals("AzureAD", StringComparison.OrdinalIgnoreCase))
                {
                    return await GetAzureAdGroupsAsync(config);
                }
                else if (connection.Type == ConnectionType.ActiveDirectory)
                {
                    return await GetOnPremisesGroupsAsync(config);
                }

                return new List<AdGroup>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get groups for connection {ConnectionName}", connection.Name);
                throw; // Re-throw the exception so it can be handled by the controller
            }
        }

        public async Task<IEnumerable<AdOrganizationalUnit>> GetOrganizationalUnitsForConnectionAsync(ConnectionSettings connection)
        {
            try
            {
                if (!connection.IsActive)
                {
                    return new List<AdOrganizationalUnit>();
                }

                _logger.LogInformation("Getting OUs for connection: {ConnectionName}", connection.Name);

                var config = AdConnectionConfig.ParseConnectionString(connection.ConnectionString);

                if (connection.Type == ConnectionType.EntraId ||
                    config.ConnectionType.Equals("AzureAD", StringComparison.OrdinalIgnoreCase))
                {
                    // Azure AD doesn't have traditional OUs, return administrative units or empty
                    return await GetAzureAdOrganizationalUnitsAsync(config);
                }
                else if (connection.Type == ConnectionType.ActiveDirectory)
                {
                    return await GetOnPremisesOrganizationalUnitsAsync(config);
                }

                return new List<AdOrganizationalUnit>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get OUs for connection {ConnectionName}", connection.Name);
                throw; // Re-throw the exception so it can be handled by the controller
            }
        }

        public async Task<IEnumerable<AdUser>> GetGroupMembersForConnectionAsync(ConnectionSettings connection, string groupName)
        {
            try
            {
                if (!connection.IsActive)
                {
                    return new List<AdUser>();
                }

                var config = AdConnectionConfig.ParseConnectionString(connection.ConnectionString);

                if (connection.Type == ConnectionType.EntraId ||
                    config.ConnectionType.Equals("AzureAD", StringComparison.OrdinalIgnoreCase))
                {
                    return await GetAzureAdGroupMembersAsync(config, groupName);
                }
                else if (connection.Type == ConnectionType.ActiveDirectory)
                {
                    return await GetOnPremisesGroupMembersAsync(config, groupName);
                }

                return new List<AdUser>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get group members for connection {ConnectionName}", connection.Name);
                throw; // Re-throw the exception so it can be handled by the controller
            }
        }

        public async Task<IEnumerable<ExchangeMailbox>> GetMailboxesForConnectionAsync(ConnectionSettings connection)
        {
            try
            {
                if (!connection.IsActive)
                {
                    _logger.LogWarning("Connection {ConnectionName} is not active", connection.Name);
                    return new List<ExchangeMailbox>();
                }

                _logger.LogInformation("Getting mailboxes for connection: {ConnectionName} of type {ConnectionType}",
                    connection.Name, connection.Type);

                if (connection.Type == Core.Enums.ConnectionType.ExchangeOnline)
                {
                    return await GetExchangeOnlineMailboxesAsync(connection);
                }
                else if (connection.Type == Core.Enums.ConnectionType.Exchange)
                {
                    return await GetOnPremisesExchangeMailboxesAsync(connection);
                }
                else
                {
                    _logger.LogWarning("Connection type {ConnectionType} is not supported for mailbox retrieval", connection.Type);
                    return new List<ExchangeMailbox>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get mailboxes for connection {ConnectionName}", connection.Name);
                throw new InvalidOperationException($"Failed to retrieve mailboxes for connection {connection.Name}: {ex.Message}", ex);
            }
        }

        public async Task<ExchangeMailbox?> GetMailboxForConnectionAsync(ConnectionSettings connection, string emailAddress)
        {
            var mailboxes = await GetMailboxesForConnectionAsync(connection);
            return mailboxes.FirstOrDefault(m =>
                m.PrimarySmtpAddress?.Equals(emailAddress, StringComparison.OrdinalIgnoreCase) == true);
        }

        #region Azure AD Methods

        private async Task<IEnumerable<AdUser>> GetAzureAdUsersAsync(AdConnectionConfig config, string? searchFilter = null)
        {
            try
            {
                if (string.IsNullOrEmpty(config.TenantId) || string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret))
                {
                    _logger.LogError("Missing Azure AD credentials: TenantId={TenantId}, ClientId={ClientId}, ClientSecret={HasSecret}",
                        config.TenantId ?? "MISSING", config.ClientId ?? "MISSING", !string.IsNullOrEmpty(config.ClientSecret) ? "PROVIDED" : "MISSING");
                    throw new InvalidOperationException("Missing required Azure AD credentials. Please verify TenantId, ClientId, and ClientSecret are configured.");
                }

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
                            LastLogon = null,
                            PasswordLastSet = user.CreatedDateTime?.DateTime,
                            DistinguishedName = $"CN={user.DisplayName},OU=Users,DC=AzureAD",
                            MemberOf = new List<string>()
                        };

                        users.Add(adUser);
                    }
                }

                _logger.LogInformation("Retrieved {Count} users from Azure AD", users.Count);
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Azure AD users with TenantId={TenantId}, ClientId={ClientId}, ClientSecret={HasSecret}, Error: {Error}",
                    config.TenantId, config.ClientId, !string.IsNullOrEmpty(config.ClientSecret) ? "***" : "MISSING", ex.Message);
                throw new InvalidOperationException($"Failed to retrieve Azure AD users: {ex.Message}", ex);
            }
        }

        private async Task<IEnumerable<AdGroup>> GetAzureAdGroupsAsync(AdConnectionConfig config)
        {
            try
            {
                if (string.IsNullOrEmpty(config.TenantId) || string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret))
                {
                    _logger.LogError("Missing Azure AD credentials for groups retrieval: TenantId={TenantId}, ClientId={ClientId}, ClientSecret={HasSecret}",
                        config.TenantId ?? "MISSING", config.ClientId ?? "MISSING", !string.IsNullOrEmpty(config.ClientSecret) ? "PROVIDED" : "MISSING");
                    throw new InvalidOperationException("Missing required Azure AD credentials for groups retrieval. Please verify TenantId, ClientId, and ClientSecret are configured.");
                }

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
                            MemberCount = 0,
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
                _logger.LogError(ex, "Failed to retrieve Azure AD groups with TenantId={TenantId}, ClientId={ClientId}, ClientSecret={HasSecret}, Error: {Error}",
                    config.TenantId, config.ClientId, !string.IsNullOrEmpty(config.ClientSecret) ? "***" : "MISSING", ex.Message);
                throw new InvalidOperationException($"Failed to retrieve Azure AD groups: {ex.Message}", ex);
            }
        }

        private async Task<IEnumerable<AdUser>> GetAzureAdGroupMembersAsync(AdConnectionConfig config, string groupName)
        {
            try
            {
                if (string.IsNullOrEmpty(config.TenantId) || string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret))
                {
                    return new List<AdUser>();
                }

                var graphClient = CreateGraphClient(config);
                var members = new List<AdUser>();

                var groups = await graphClient.Groups.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Filter = $"displayName eq '{groupName}'";
                    requestConfiguration.QueryParameters.Select = new string[] { "id", "displayName" };
                });

                var group = groups?.Value?.FirstOrDefault();
                if (group?.Id == null) return members;

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

                    _logger.LogInformation("Retrieving groups from on-premises AD: Domain={Domain}", config.Domain ?? "Current Domain");

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
                                            DistinguishedName = group.DistinguishedName,
                                            GroupType = GetGroupType(group),
                                            Scope = GetGroupScope(group),
                                            MemberCount = GetGroupMemberCount(group)
                                        };

                                        // Get additional properties using DirectoryEntry
                                        using (var directoryEntry = group.GetUnderlyingObject() as System.DirectoryServices.DirectoryEntry)
                                        {
                                            if (directoryEntry != null)
                                            {
                                                var whenCreated = GetDirectoryProperty(directoryEntry, "whenCreated");
                                                var whenChanged = GetDirectoryProperty(directoryEntry, "whenChanged");

                                                if (DateTime.TryParse(whenCreated, out var created))
                                                    adGroup.Created = created;

                                                if (DateTime.TryParse(whenChanged, out var modified))
                                                    adGroup.Modified = modified;
                                            }
                                        }

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

                    _logger.LogInformation("Retrieving organizational units from on-premises AD: Domain={Domain}", config.Domain ?? "Current Domain");

                    using (var context = CreatePrincipalContext(config))
                    {
                        // Get the domain DN for LDAP search
                        var domainDN = GetDomainDistinguishedName(context);

                        using (var directoryEntry = new System.DirectoryServices.DirectoryEntry($"LDAP://{domainDN}"))
                        {
                            using (var searcher = new System.DirectoryServices.DirectorySearcher(directoryEntry))
                            {
                                searcher.Filter = "(objectClass=organizationalUnit)";
                                searcher.PropertiesToLoad.AddRange(new[] {
                                    "name", "distinguishedName", "description", "whenCreated", "whenChanged",
                                    "ou", "objectGUID"
                                });
                                searcher.PageSize = config.PageSize;

                                using (var results = searcher.FindAll())
                                {
                                    foreach (System.DirectoryServices.SearchResult result in results)
                                    {
                                        try
                                        {
                                            var ou = new AdOrganizationalUnit
                                            {
                                                Id = GetSearchResultProperty(result, "objectGUID") ?? Guid.NewGuid().ToString(),
                                                Name = GetSearchResultProperty(result, "name") ?? "Unknown",
                                                DistinguishedName = GetSearchResultProperty(result, "distinguishedName") ?? "Unknown",
                                                Description = GetSearchResultProperty(result, "description") ?? string.Empty,
                                                ParentOU = GetParentOU(GetSearchResultProperty(result, "distinguishedName")),
                                                UserCount = GetOUUserCount(result.Properties["distinguishedName"][0]?.ToString()),
                                                ComputerCount = GetOUComputerCount(result.Properties["distinguishedName"][0]?.ToString()),
                                                GroupCount = GetOUGroupCount(result.Properties["distinguishedName"][0]?.ToString()),
                                                ChildOUCount = GetChildOUCount(result.Properties["distinguishedName"][0]?.ToString())
                                            };

                                            var whenCreated = GetSearchResultProperty(result, "whenCreated");
                                            var whenChanged = GetSearchResultProperty(result, "whenChanged");

                                            if (DateTime.TryParse(whenCreated, out var created))
                                                ou.Created = created;

                                            if (DateTime.TryParse(whenChanged, out var modified))
                                                ou.Modified = modified;

                                            organizationalUnits.Add(ou);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogWarning(ex, "Failed to process OU {OU}", GetSearchResultProperty(result, "name"));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    _logger.LogInformation("Retrieved {Count} organizational units from on-premises AD", organizationalUnits.Count);
                    return organizationalUnits;
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

                    _logger.LogInformation("Retrieving members for group {GroupName} from on-premises AD", groupName);

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
                                    var members = group.GetMembers(false); // false = don't recurse into nested groups

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

                                            // Get additional properties using DirectoryEntry
                                            using (var directoryEntry = member.GetUnderlyingObject() as System.DirectoryServices.DirectoryEntry)
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
                                            _logger.LogWarning(ex, "Failed to process group member {UserName}", member.SamAccountName);
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Group {GroupName} not found in Active Directory", groupName);
                                }
                            }
                        }
                    }

                    _logger.LogInformation("Retrieved {Count} members for group {GroupName} from on-premises AD", users.Count, groupName);
                    return users;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve group members for {GroupName} from on-premises AD", groupName);
                    throw new InvalidOperationException($"Failed to retrieve group members from on-premises Active Directory: {ex.Message}", ex);
                }
            });
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

        private string GetGroupType(System.DirectoryServices.AccountManagement.GroupPrincipal group)
        {
            try
            {
                using (var directoryEntry = group.GetUnderlyingObject() as System.DirectoryServices.DirectoryEntry)
                {
                    if (directoryEntry != null && directoryEntry.Properties.Contains("groupType"))
                    {
                        var groupType = directoryEntry.Properties["groupType"].Value;
                        if (groupType != null)
                        {
                            var groupTypeValue = Convert.ToInt32(groupType);
                            // Check if it's a security group (ADS_GROUP_TYPE_SECURITY_ENABLED = 0x80000000)
                            return (groupTypeValue & 0x80000000) != 0 ? "Security" : "Distribution";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get group type for {GroupName}", group.SamAccountName);
            }
            return "Security"; // Default to Security
        }

        private string GetGroupScope(System.DirectoryServices.AccountManagement.GroupPrincipal group)
        {
            try
            {
                using (var directoryEntry = group.GetUnderlyingObject() as System.DirectoryServices.DirectoryEntry)
                {
                    if (directoryEntry != null && directoryEntry.Properties.Contains("groupType"))
                    {
                        var groupType = directoryEntry.Properties["groupType"].Value;
                        if (groupType != null)
                        {
                            var groupTypeValue = Convert.ToInt32(groupType);
                            // Extract scope from group type
                            if ((groupTypeValue & 0x00000004) != 0) return "Domain Local";
                            if ((groupTypeValue & 0x00000002) != 0) return "Global";
                            if ((groupTypeValue & 0x00000008) != 0) return "Universal";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get group scope for {GroupName}", group.SamAccountName);
            }
            return "Global"; // Default to Global
        }

        private int GetGroupMemberCount(System.DirectoryServices.AccountManagement.GroupPrincipal group)
        {
            try
            {
                return group.GetMembers().Count();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get member count for group {GroupName}", group.SamAccountName);
                return 0;
            }
        }

        private string? GetSearchResultProperty(System.DirectoryServices.SearchResult result, string propertyName)
        {
            try
            {
                if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
                {
                    var value = result.Properties[propertyName][0];
                    if (propertyName == "objectGUID" && value is byte[] guidBytes)
                    {
                        return new Guid(guidBytes).ToString();
                    }
                    return value?.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get search result property {PropertyName}", propertyName);
            }
            return null;
        }

        private string GetDomainDistinguishedName(System.DirectoryServices.AccountManagement.PrincipalContext context)
        {
            try
            {
                using (var domainContext = context)
                {
                    var domainName = domainContext.Name ?? Environment.UserDomainName;
                    // Convert domain name to DN format (e.g., "contoso.com" -> "DC=contoso,DC=com")
                    return string.Join(",", domainName.Split('.').Select(part => $"DC={part}"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get domain DN, using default");
                return "DC=domain,DC=local"; // Fallback
            }
        }

        private string? GetParentOU(string? distinguishedName)
        {
            if (string.IsNullOrEmpty(distinguishedName))
                return null;

            var parts = distinguishedName.Split(',');
            if (parts.Length > 1)
            {
                return string.Join(",", parts.Skip(1));
            }
            return null;
        }

        private int GetOUUserCount(string? ouDN)
        {
            if (string.IsNullOrEmpty(ouDN)) return 0;

            try
            {
                using (var entry = new System.DirectoryServices.DirectoryEntry($"LDAP://{ouDN}"))
                using (var searcher = new System.DirectoryServices.DirectorySearcher(entry))
                {
                    searcher.Filter = "(&(objectClass=user)(objectCategory=person))";
                    searcher.SearchScope = System.DirectoryServices.SearchScope.OneLevel;
                    return searcher.FindAll().Count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get user count for OU {OU}", ouDN);
                return 0;
            }
        }

        private int GetOUComputerCount(string? ouDN)
        {
            if (string.IsNullOrEmpty(ouDN)) return 0;

            try
            {
                using (var entry = new System.DirectoryServices.DirectoryEntry($"LDAP://{ouDN}"))
                using (var searcher = new System.DirectoryServices.DirectorySearcher(entry))
                {
                    searcher.Filter = "(objectClass=computer)";
                    searcher.SearchScope = System.DirectoryServices.SearchScope.OneLevel;
                    return searcher.FindAll().Count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get computer count for OU {OU}", ouDN);
                return 0;
            }
        }

        private int GetOUGroupCount(string? ouDN)
        {
            if (string.IsNullOrEmpty(ouDN)) return 0;

            try
            {
                using (var entry = new System.DirectoryServices.DirectoryEntry($"LDAP://{ouDN}"))
                using (var searcher = new System.DirectoryServices.DirectorySearcher(entry))
                {
                    searcher.Filter = "(objectClass=group)";
                    searcher.SearchScope = System.DirectoryServices.SearchScope.OneLevel;
                    return searcher.FindAll().Count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get group count for OU {OU}", ouDN);
                return 0;
            }
        }

        private int GetChildOUCount(string? ouDN)
        {
            if (string.IsNullOrEmpty(ouDN)) return 0;

            try
            {
                using (var entry = new System.DirectoryServices.DirectoryEntry($"LDAP://{ouDN}"))
                using (var searcher = new System.DirectoryServices.DirectorySearcher(entry))
                {
                    searcher.Filter = "(objectClass=organizationalUnit)";
                    searcher.SearchScope = System.DirectoryServices.SearchScope.OneLevel;
                    return searcher.FindAll().Count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get child OU count for OU {OU}", ouDN);
                return 0;
            }
        }

        #endregion

        #region Exchange Methods

        private async Task<IEnumerable<ExchangeMailbox>> GetExchangeOnlineMailboxesAsync(ConnectionSettings connection)
        {
            try
            {
                _logger.LogInformation("Parsing connection string for Exchange Online connection: {ConnectionName}", connection.Name);
                var connectionParams = ParseExchangeConnectionString(connection.ConnectionString);

                _logger.LogInformation("Connection parameters found: {ParameterKeys}", string.Join(", ", connectionParams.Keys));

                if (connectionParams.ContainsKey("TenantId") && connectionParams.ContainsKey("ClientId") && connectionParams.ContainsKey("ClientSecret"))
                {
                    _logger.LogInformation("Valid credentials found. Attempting Microsoft Graph connection...");
                    return await GetExchangeOnlineMailboxesWithGraphAsync(connectionParams);
                }
                else
                {
                    _logger.LogError("Exchange Online connection missing required credentials. Found parameters: [{Parameters}]. Need: TenantId, ClientId, ClientSecret.",
                        string.Join(", ", connectionParams.Keys));
                    throw new InvalidOperationException("Exchange Online connection missing required credentials. Please verify TenantId, ClientId, and ClientSecret are configured.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Exchange Online mailboxes");
                throw new InvalidOperationException("Failed to retrieve Exchange Online mailboxes", ex);
            }
        }

        private async Task<IEnumerable<ExchangeMailbox>> GetExchangeOnlineMailboxesWithGraphAsync(Dictionary<string, string> connectionParams)
        {
            try
            {
                var graphClient = CreateGraphClientForExchange(connectionParams);
                var mailboxes = new List<ExchangeMailbox>();

                // Get all users from Microsoft Graph (no filter for broader results)
                var users = await graphClient.Users.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Select = new string[]
                    {
                        "id", "userPrincipalName", "displayName", "mail", "mailNickname",
                        "accountEnabled", "createdDateTime"
                    };
                    requestConfiguration.QueryParameters.Top = 50;
                });

                if (users?.Value != null)
                {
                    foreach (var user in users.Value)
                    {
                        // Check if user has an Exchange license
                        if (user.AssignedLicenses?.Count > 0)
                        {
                            var mailbox = new ExchangeMailbox
                            {
                                PrimarySmtpAddress = user.Mail ?? user.UserPrincipalName,
                                DisplayName = user.DisplayName,
                                Alias = user.MailNickname ?? user.UserPrincipalName?.Split('@')[0],
                                SamAccountName = user.UserPrincipalName?.Split('@')[0],
                                ServerName = "Exchange Online",
                                DatabaseName = "Exchange Online Database",
                                TotalItemSize = 0, // Would need separate call to get mailbox size
                                ItemCount = 0, // Would need separate call to get item count
                                LastLogonTime = user.CreatedDateTime?.DateTime,
                                IsArchiveEnabled = false, // Would need separate call to determine this
                                MailboxType = "UserMailbox",
                                EmailAddresses = new List<string> { user.Mail ?? user.UserPrincipalName ?? "" }
                            };

                            mailboxes.Add(mailbox);
                        }
                    }
                }

                _logger.LogInformation("Retrieved {Count} mailboxes from Exchange Online", mailboxes.Count);
                return mailboxes;
            }
            catch (Exception ex)
            {
                var tenantId = connectionParams.ContainsKey("TenantId") ? connectionParams["TenantId"] : "MISSING";
                var clientId = connectionParams.ContainsKey("ClientId") ? connectionParams["ClientId"] : "MISSING";
                var hasSecret = connectionParams.ContainsKey("ClientSecret") && !string.IsNullOrEmpty(connectionParams["ClientSecret"]);

                _logger.LogError(ex, "Failed to retrieve Exchange Online mailboxes using Microsoft Graph with TenantId={TenantId}, ClientId={ClientId}, ClientSecret={HasSecret}, Error: {Error}",
                    tenantId, clientId, hasSecret ? "***" : "MISSING", ex.Message);
                throw new InvalidOperationException($"Failed to retrieve Exchange Online mailboxes: {ex.Message}", ex);
            }
        }

        private async Task<IEnumerable<ExchangeMailbox>> GetOnPremisesExchangeMailboxesAsync(ConnectionSettings connection)
        {
            try
            {
                _logger.LogError("On-premises Exchange integration is not implemented. This feature requires PowerShell and Exchange Management Shell.");
                await Task.Delay(100);
                throw new NotImplementedException("On-premises Exchange integration is not implemented. This feature requires PowerShell and Exchange Management Shell.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve on-premises Exchange mailboxes");
                throw new InvalidOperationException($"Failed to retrieve on-premises Exchange mailboxes: {ex.Message}", ex);
            }
        }

        private GraphServiceClient CreateGraphClientForExchange(Dictionary<string, string> connectionParams)
        {
            var options = new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            };

            var clientSecretCredential = new ClientSecretCredential(
                connectionParams["TenantId"],
                connectionParams["ClientId"],
                connectionParams["ClientSecret"],
                options);

            return new GraphServiceClient(clientSecretCredential);
        }

        private Dictionary<string, string> ParseExchangeConnectionString(string connectionString)
        {
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(connectionString))
                return parameters;

            var pairs = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length == 2)
                {
                    parameters[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }

            return parameters;
        }

        public async Task<IEnumerable<DistributionGroup>> GetDistributionGroupsForConnectionAsync(ConnectionSettings connection)
        {
            try
            {
                if (connection.Type != ConnectionType.ExchangeOnline)
                {
                    return new List<DistributionGroup>();
                }

                var connectionParams = ParseExchangeConnectionString(connection.ConnectionString);
                if (!connectionParams.ContainsKey("TenantId") || !connectionParams.ContainsKey("ClientId") || !connectionParams.ContainsKey("ClientSecret"))
                {
                    _logger.LogWarning("Missing credentials for distribution groups, returning empty list");
                    return new List<DistributionGroup>();
                }

                var graphClient = CreateGraphClientForExchange(connectionParams);
                var groups = new List<DistributionGroup>();

                var groupsPage = await graphClient.Groups.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Select = new string[]
                    {
                        "id", "displayName", "mail", "description", "groupTypes", "createdDateTime"
                    };
                    requestConfiguration.QueryParameters.Filter = "mailEnabled eq true";
                    requestConfiguration.QueryParameters.Top = 50;
                });

                if (groupsPage?.Value != null)
                {
                    foreach (var group in groupsPage.Value)
                    {
                        var distGroup = new DistributionGroup
                        {
                            Id = group.Id,
                            Name = group.DisplayName,
                            EmailAddress = group.Mail,
                            DisplayName = group.DisplayName,
                            MemberCount = 0, // Would need separate call to get member count
                            ModeratedEnabled = false,
                            ManagedBy = new List<string>(),
                            Created = group.CreatedDateTime?.DateTime ?? DateTime.Now
                        };
                        groups.Add(distGroup);
                    }
                }

                _logger.LogInformation("Retrieved {Count} distribution groups from Exchange Online", groups.Count);
                return groups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve distribution groups, returning empty list");
                return new List<DistributionGroup>();
            }
        }

        public async Task<IEnumerable<SharedMailbox>> GetSharedMailboxesForConnectionAsync(ConnectionSettings connection)
        {
            try
            {
                if (connection.Type != ConnectionType.ExchangeOnline)
                {
                    return new List<SharedMailbox>();
                }

                var connectionParams = ParseExchangeConnectionString(connection.ConnectionString);
                if (!connectionParams.ContainsKey("TenantId") || !connectionParams.ContainsKey("ClientId") || !connectionParams.ContainsKey("ClientSecret"))
                {
                    _logger.LogWarning("Missing credentials for shared mailboxes, returning empty list");
                    return new List<SharedMailbox>();
                }

                var graphClient = CreateGraphClientForExchange(connectionParams);
                var sharedMailboxes = new List<SharedMailbox>();

                // Note: In a real implementation, you might need to use different Graph API endpoints
                // to specifically identify shared mailboxes vs regular user mailboxes
                var users = await graphClient.Users.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Select = new string[]
                    {
                        "id", "userPrincipalName", "displayName", "mail"
                    };
                    requestConfiguration.QueryParameters.Filter = "accountEnabled eq false";
                    requestConfiguration.QueryParameters.Top = 20;
                });

                if (users?.Value != null)
                {
                    foreach (var user in users.Value)
                    {
                        var sharedMailbox = new SharedMailbox
                        {
                            Id = user.Id,
                            Name = user.UserPrincipalName?.Split('@')[0] ?? "Unknown",
                            EmailAddress = user.Mail ?? user.UserPrincipalName,
                            DisplayName = user.DisplayName,
                            TotalSize = 0, // Would need separate call to get mailbox statistics
                            ItemCount = 0,
                            Delegates = new List<string>(),
                            LastAccessed = DateTime.Now.AddDays(-7)
                        };
                        sharedMailboxes.Add(sharedMailbox);
                    }
                }

                _logger.LogInformation("Retrieved {Count} shared mailboxes from Exchange Online", sharedMailboxes.Count);
                return sharedMailboxes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve shared mailboxes, returning empty list");
                return new List<SharedMailbox>();
            }
        }

        #endregion

        #region Simulated Data Methods

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
                }
            };
        }

        private IEnumerable<ExchangeMailbox> GetSimulatedMailboxes()
        {
            return new[]
            {
                new ExchangeMailbox
                {
                    SamAccountName = "john.doe",
                    DisplayName = "John Doe",
                    PrimarySmtpAddress = "john.doe@contoso.com",
                    TotalItemSize = 1024 * 1024 * 250, // 250MB
                    ItemCount = 1500,
                    LastLogonTime = DateTime.Now.AddDays(-1),
                    MailboxType = "UserMailbox"
                },
                new ExchangeMailbox
                {
                    SamAccountName = "jane.smith",
                    DisplayName = "Jane Smith",
                    PrimarySmtpAddress = "jane.smith@contoso.com",
                    TotalItemSize = 1024 * 1024 * 180, // 180MB
                    ItemCount = 890,
                    LastLogonTime = DateTime.Now.AddHours(-4),
                    MailboxType = "UserMailbox"
                }
            };
        }

        #endregion
    }
}