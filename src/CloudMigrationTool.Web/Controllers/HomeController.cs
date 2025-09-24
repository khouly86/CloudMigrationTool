using CloudMigrationTool.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CloudMigrationTool.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Create sample data for demonstration
            var viewModel = new HomeIndexViewModel
            {
                ActiveMigrationsCount = 2,
                TotalJobsCount = 15,
                CompletedJobsCount = 10,
                FailedJobsCount = 2,
                ActiveConnectionsCount = 3,
                TotalConnectionsCount = 5,
                RecentJobs = new List<CloudMigrationTool.Core.Entities.MigrationJob>()
                // Sample jobs would go here, but we'll leave empty for now
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