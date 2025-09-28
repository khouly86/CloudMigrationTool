using CloudMigrationTool.Core.Models;
using CloudMigrationTool.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace CloudMigrationTool.Web.Models
{
    public class BaseExplorationViewModel
    {
        public int? ConnectionId { get; set; }
        public bool IsConnected { get; set; }
        public List<ConnectionSettings> AvailableConnections { get; set; } = new List<ConnectionSettings>();
        public string SearchQuery { get; set; } = string.Empty;
        public List<string> SelectedItems { get; set; } = new List<string>();
    }

    public class ActiveDirectoryExplorationViewModel : BaseExplorationViewModel
    {
        public IEnumerable<AdUser> Users { get; set; } = new List<AdUser>();
        public IEnumerable<CloudMigrationTool.Core.Models.AdGroup> Groups { get; set; } = new List<CloudMigrationTool.Core.Models.AdGroup>();
        public IEnumerable<CloudMigrationTool.Core.Models.AdOrganizationalUnit> OrganizationalUnits { get; set; } = new List<CloudMigrationTool.Core.Models.AdOrganizationalUnit>();
        public ADFilterOptions FilterOptions { get; set; } = new ADFilterOptions();
        public int TotalUsers => Users.Count();
        public int TotalGroups => Groups.Count();
        public int TotalOUs => OrganizationalUnits.Count();
    }

    public class ExchangeExplorationViewModel : BaseExplorationViewModel
    {
        public IEnumerable<ExchangeMailbox> Mailboxes { get; set; } = new List<ExchangeMailbox>();
        public ExchangeFilterOptions FilterOptions { get; set; } = new ExchangeFilterOptions();
        public int TotalMailboxes => Mailboxes.Count();
        public long TotalSize => Mailboxes.Sum(m => m.TotalItemSize);
        public int TotalItems => Mailboxes.Sum(m => m.ItemCount);
    }

    public class ExchangeOnlineExplorationViewModel : BaseExplorationViewModel
    {
        public IEnumerable<ExchangeMailbox> Mailboxes { get; set; } = new List<ExchangeMailbox>();
        public IEnumerable<CloudMigrationTool.Core.Models.DistributionGroup> DistributionGroups { get; set; } = new List<CloudMigrationTool.Core.Models.DistributionGroup>();
        public IEnumerable<CloudMigrationTool.Core.Models.SharedMailbox> SharedMailboxes { get; set; } = new List<CloudMigrationTool.Core.Models.SharedMailbox>();
        public ExchangeOnlineFilterOptions FilterOptions { get; set; } = new ExchangeOnlineFilterOptions();
        public int TotalMailboxes => Mailboxes.Count();
        public int TotalDistributionGroups => DistributionGroups.Count();
        public int TotalSharedMailboxes => SharedMailboxes.Count();
    }

    public class EntraIdExplorationViewModel : BaseExplorationViewModel
    {
        public IEnumerable<AdUser> Users { get; set; } = new List<AdUser>();
        public IEnumerable<CloudMigrationTool.Core.Models.AdGroup> Groups { get; set; } = new List<CloudMigrationTool.Core.Models.AdGroup>();
        public IEnumerable<CloudMigrationTool.Core.Models.EntraIdApplication> Applications { get; set; } = new List<CloudMigrationTool.Core.Models.EntraIdApplication>();
        public IEnumerable<CloudMigrationTool.Core.Models.EntraIdRole> Roles { get; set; } = new List<CloudMigrationTool.Core.Models.EntraIdRole>();
        public EntraIdFilterOptions FilterOptions { get; set; } = new EntraIdFilterOptions();
        public int TotalUsers => Users.Count();
        public int TotalGroups => Groups.Count();
        public int TotalApplications => Applications.Count();
        public int TotalRoles => Roles.Count();
    }

    public class ADFilterOptions
    {
        public bool ShowEnabledOnly { get; set; } = true;
        public bool ShowUsersWithMailbox { get; set; }
        public string OUFilter { get; set; } = string.Empty;
        public DateTime? LastLogonAfter { get; set; }
        public List<string> GroupMemberships { get; set; } = new List<string>();
    }

    public class ExchangeFilterOptions
    {
        public string MailboxType { get; set; } = "All";
        public long? MinimumSize { get; set; }
        public long? MaximumSize { get; set; }
        public string DatabaseFilter { get; set; } = string.Empty;
        public bool ShowArchiveEnabled { get; set; }
        public DateTime? LastLogonAfter { get; set; }
    }

    public class ExchangeOnlineFilterOptions
    {
        public string MailboxType { get; set; } = "All";
        public bool ShowLicensedOnly { get; set; } = true;
        public bool ShowActiveOnly { get; set; } = true;
        public string DomainFilter { get; set; } = string.Empty;
        public List<string> LicenseTypes { get; set; } = new List<string>();
    }

    public class EntraIdFilterOptions
    {
        public bool ShowSyncedOnly { get; set; }
        public bool ShowCloudOnly { get; set; } = true;
        public string DomainFilter { get; set; } = string.Empty;
        public List<string> RoleFilter { get; set; } = new List<string>();
        public bool ShowGuestUsers { get; set; }
    }


    public class ItemSelectionRequest
    {
        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        public int ConnectionId { get; set; }

        public List<string> SelectedItems { get; set; } = new List<string>();
    }

    public class ItemDetailsRequest
    {
        [Required]
        public string ItemId { get; set; } = string.Empty;

        [Required]
        public string ItemType { get; set; } = string.Empty;
    }

    public class ConnectionTestViewModel
    {
        public List<ConnectionSettings> AvailableConnections { get; set; } = new List<ConnectionSettings>();
        public Dictionary<int, ConnectionTestResult> TestResults { get; set; } = new Dictionary<int, ConnectionTestResult>();
    }
}