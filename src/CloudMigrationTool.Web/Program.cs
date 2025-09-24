using CloudMigrationTool.Infrastructure.Data;
using CloudMigrationTool.Core.Interfaces;
using CloudMigrationTool.Infrastructure.External;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Configure Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register application services
builder.Services.AddScoped<IActiveDirectoryService, RealActiveDirectoryService>();
builder.Services.AddScoped<IExchangeService, ExchangeService>();
builder.Services.AddScoped<CloudMigrationTool.Services.Admin.IConnectionSettingsService, CloudMigrationTool.Services.Admin.ConnectionSettingsService>();
builder.Services.AddScoped<CloudMigrationTool.Services.Exploration.IExplorationService, CloudMigrationTool.Services.Exploration.ExplorationService>();

// Add session support for selection persistence
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// Configure routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        context.Database.EnsureCreated();
        Console.WriteLine("Database initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database error: {ex.Message}");
    }
}

Console.WriteLine("Cloud Migration Tool started successfully");

app.Run();