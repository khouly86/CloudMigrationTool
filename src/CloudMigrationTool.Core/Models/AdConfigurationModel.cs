namespace CloudMigrationTool.Core.Models
{
    public class AdConnectionConfig
    {
        public string ConnectionType { get; set; } = "OnPremises"; // OnPremises, AzureAD
        
        // On-Premises AD Configuration
        public string? Domain { get; set; }
        public string? Server { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Container { get; set; } // OU=Users,DC=contoso,DC=com
        public int Port { get; set; } = 389;
        public bool UseSSL { get; set; } = false;
        
        // Azure AD Configuration
        public string? TenantId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        
        // Common Settings
        public int TimeoutSeconds { get; set; } = 30;
        public int PageSize { get; set; } = 999;
        public bool IncludeDisabledUsers { get; set; } = false;
        
        public static AdConnectionConfig ParseConnectionString(string connectionString)
        {
            var config = new AdConnectionConfig();
            
            if (string.IsNullOrWhiteSpace(connectionString))
                return config;

            foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length != 2) continue;

                var key = keyValue[0].Trim();
                var value = keyValue[1].Trim();

                switch (key.ToLowerInvariant())
                {
                    case "type":
                    case "connectiontype":
                        config.ConnectionType = value;
                        break;
                    case "domain":
                        config.Domain = value;
                        break;
                    case "server":
                        config.Server = value;
                        break;
                    case "username":
                    case "user":
                        config.Username = value;
                        break;
                    case "password":
                    case "pwd":
                        config.Password = value;
                        break;
                    case "container":
                    case "ou":
                        config.Container = value;
                        break;
                    case "port":
                        if (int.TryParse(value, out var port))
                            config.Port = port;
                        break;
                    case "ssl":
                    case "usessl":
                        config.UseSSL = bool.Parse(value);
                        break;
                    case "tenantid":
                        config.TenantId = value;
                        break;
                    case "clientid":
                        config.ClientId = value;
                        break;
                    case "clientsecret":
                        config.ClientSecret = value;
                        break;
                    case "timeout":
                        if (int.TryParse(value, out var timeout))
                            config.TimeoutSeconds = timeout;
                        break;
                    case "pagesize":
                        if (int.TryParse(value, out var pageSize))
                            config.PageSize = pageSize;
                        break;
                    case "includedisabled":
                        config.IncludeDisabledUsers = bool.Parse(value);
                        break;
                }
            }
            
            return config;
        }
        
        public string ToConnectionString()
        {
            var parts = new List<string>();
            
            parts.Add($"Type={ConnectionType}");
            
            if (ConnectionType.Equals("OnPremises", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(Domain)) parts.Add($"Domain={Domain}");
                if (!string.IsNullOrEmpty(Server)) parts.Add($"Server={Server}");
                if (!string.IsNullOrEmpty(Username)) parts.Add($"Username={Username}");
                if (!string.IsNullOrEmpty(Password)) parts.Add($"Password={Password}");
                if (!string.IsNullOrEmpty(Container)) parts.Add($"Container={Container}");
                if (Port != 389) parts.Add($"Port={Port}");
                if (UseSSL) parts.Add($"UseSSL={UseSSL}");
            }
            else if (ConnectionType.Equals("AzureAD", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(TenantId)) parts.Add($"TenantId={TenantId}");
                if (!string.IsNullOrEmpty(ClientId)) parts.Add($"ClientId={ClientId}");
                if (!string.IsNullOrEmpty(ClientSecret)) parts.Add($"ClientSecret={ClientSecret}");
            }
            
            if (TimeoutSeconds != 30) parts.Add($"Timeout={TimeoutSeconds}");
            if (PageSize != 999) parts.Add($"PageSize={PageSize}");
            if (IncludeDisabledUsers) parts.Add($"IncludeDisabled={IncludeDisabledUsers}");
            
            return string.Join(";", parts);
        }
    }
}