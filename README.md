# Cloud Migration Tool

A comprehensive .NET application for migrating on-premises environments to Microsoft cloud services including Active Directory to Entra ID and Exchange Server to Exchange Online.

## Features

- **Active Directory Migration**: Migrate users, groups, and organizational units from on-premises AD to Entra ID (Azure AD)
- **Exchange Migration**: Migrate mailboxes from on-premises Exchange Server to Exchange Online
- **Web-Based Interface**: Clean, responsive HTML/CSS interface for management and monitoring
- **Progress Tracking**: Real-time migration progress with detailed logging
- **Connection Management**: Dynamic configuration of source and destination connections
- **Extensible Architecture**: Modular design ready for OneDrive and SharePoint migrations

## Architecture

The application follows Clean Architecture principles:

- **Core**: Domain entities, enums, interfaces, and models
- **Infrastructure**: Data access, external service integrations (AD, Exchange)
- **Services**: Application services and business logic
- **Web**: ASP.NET Core MVC web application

## Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB for development)
- Visual Studio 2022 or VS Code
- PowerShell 5.1 or later

## Getting Started

### 1. Setup Using PowerShell Scripts

Run the PowerShell scripts in order:

`powershell
# 1. Create solution structure
.\01-Create-Solution-Structure.ps1

# 2. Create .NET projects and install packages
.\02-Create-Projects.ps1

# 3. Create core domain files
.\03-Create-Core-Files.ps1

# 4. Create infrastructure layer
.\04-Create-Infrastructure-Files.ps1

# 5. Create services layer
.\05-Create-Services-Files.ps1

# 6. Create web controllers and models
.\06-Create-Web-Files.ps1

# 7. Create Razor views
.\07-Create-Views.ps1

# 8. Create static files (CSS, JS)
.\08-Create-Static-Files.ps1

# 9. Create startup configuration
.\09-Create-Program-And-Startup.ps1

# 10. Build and run
.\10-Build-And-Run.ps1
`

### 2. Manual Setup

If you prefer manual setup:

1. **Clone the repository**
   `ash
   git clone <repository-url>
   cd CloudMigrationTool
   `

2. **Restore packages**
   `ash
   dotnet restore
   `

3. **Update connection string**
   Update ppsettings.json with your SQL Server connection string.

4. **Run the application**
   `ash
   dotnet run --project src/CloudMigrationTool.Web
   `

## Configuration

### Database Connection

Update the connection string in ppsettings.json:

`json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=CloudMigrationToolDb;Trusted_Connection=true;"
  }
}
`

### Application Settings

Configure migration settings in ppsettings.json:

`json
{
  "ApplicationSettings": {
    "MaxConcurrentMigrations": 3,
    "DefaultMigrationTimeout": 480,
    "ExchangeSettings": {
      "DefaultBatchSize": 100,
      "MaxRetryAttempts": 3
    }
  }
}
`

## Usage

### 1. Configure Connections

1. Navigate to **Administration > Connections**
2. Add source connections (on-premises AD, Exchange)
3. Add destination connections (Entra ID, Exchange Online)
4. Test connections to ensure they're working

### 2. Create Migration Jobs

1. Go to **Migrations > New Migration**
2. Select migration type (Active Directory or Exchange)
3. Choose source and destination connections
4. Configure migration settings
5. Start the migration

### 3. Monitor Progress

1. View real-time progress on the **Dashboard**
2. Check detailed logs in **Migration > Job Details**
3. Monitor system health via **Administration**

## Connection String Formats

### Active Directory
`
Domain=contoso.local;Container=DC=contoso,DC=local;Username=admin;Password=password
`

### Exchange Server
`
ServerUrl=https://exchange.contoso.local/PowerShell/;Username=admin;Password=password;Domain=contoso
`

### Exchange Online
`
ConnectionUri=https://outlook.office365.com/powershell-liveid/;Username=admin@contoso.com;Password=password
`

## API Endpoints

The application exposes several API endpoints:

- GET /api/migrations - List all migration jobs
- POST /api/migrations - Create new migration job
- GET /api/migrations/{id}/progress - Get migration progress
- POST /api/connections/test - Test connection
- GET /health - Health check endpoint

## Docker Support

### Build and run with Docker:

`ash
# Build the image
docker build -t cloudmigrationtool .

# Run with docker-compose
docker-compose up -d
`

### Environment Variables:

- ASPNETCORE_ENVIRONMENT - Application environment
- ConnectionStrings__DefaultConnection - Database connection string

## Logging

The application uses Serilog for structured logging:

- Console output for development
- File logging to logs/ directory
- Configurable log levels per namespace
- Health check logging

## Security Considerations

- **Connection Strings**: Encrypt sensitive connection strings
- **Authentication**: Implement proper authentication for production
- **HTTPS**: Enable HTTPS in production environments
- **Secrets Management**: Use Azure Key Vault or similar for secrets

## Troubleshooting

### Common Issues

1. **Database Connection Failed**
   - Verify SQL Server is running
   - Check connection string format
   - Ensure database user has proper permissions

2. **Active Directory Connection Failed**
   - Verify domain connectivity
   - Check credentials and permissions
   - Ensure LDAP ports are accessible

3. **Exchange Connection Failed**
   - Verify PowerShell remoting is enabled
   - Check Exchange server URL and credentials
   - Ensure proper Exchange permissions

### Logs Location

- Development: Console output
- Production: /app/logs/ directory
- Docker: Mapped to ./logs/ on host

## Future Enhancements

- **OneDrive Migration**: Migrate user files from on-premises file servers
- **SharePoint Migration**: Migrate SharePoint sites and content
- **Teams Migration**: Migrate Teams data and configurations
- **Reporting**: Advanced reporting and analytics
- **Multi-tenant Support**: Support for multiple organizations

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For support and questions:

- Check the documentation in /docs
- Review logs in /logs
- Open an issue on GitHub
- Contact the development team

## Technology Stack

- **.NET 8.0**: Core framework
- **ASP.NET Core MVC**: Web framework
- **Entity Framework Core**: Data access
- **SQL Server**: Database
- **Bootstrap 5**: UI framework
- **jQuery**: JavaScript library
- **Serilog**: Logging framework
- **AutoMapper**: Object mapping
- **Docker**: Containerization

## Performance Considerations

- Concurrent migration jobs are limited by configuration
- Database connection pooling is enabled
- Async/await patterns used throughout
- Progress tracking minimizes database calls
- Logging is optimized for performance

## Monitoring

- Built-in health checks at /health
- Application metrics via logs
- Performance counters for migrations
- Connection status monitoring
- Real-time dashboard updates
