using CloudMigrationTool.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CloudMigrationTool.Web.Controllers
{
    public class MigrationController : Controller
    {
        private readonly ILogger<MigrationController> _logger;

        public MigrationController(ILogger<MigrationController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Return empty list for now
            var jobs = new List<CloudMigrationTool.Core.Entities.MigrationJob>();
            return View(jobs);
        }

        public IActionResult Dashboard()
        {
            var viewModel = new MigrationDashboardViewModel
            {
                ActiveJobs = new List<CloudMigrationTool.Core.Entities.MigrationJob>(),
                RecentJobs = new List<CloudMigrationTool.Core.Entities.MigrationJob>(),
                TotalJobs = 0,
                CompletedJobs = 0,
                FailedJobs = 0,
                InProgressJobs = 0
            };

            return View(viewModel);
        }

        public IActionResult Create()
        {
            var model = new CreateMigrationJobViewModel
            {
                Connections = new List<CloudMigrationTool.Core.Entities.ConnectionSettings>()
            };
            return View(model);
        }

        public IActionResult Details(int id)
        {
            // Return a dummy job for now
            var job = new CloudMigrationTool.Core.Entities.MigrationJob
            {
                Id = id,
                Name = "Sample Migration Job",
                Status = CloudMigrationTool.Core.Enums.MigrationStatus.NotStarted
            };

            var viewModel = new MigrationJobDetailsViewModel
            {
                Job = job,
                Progress = null,
                Logs = new List<CloudMigrationTool.Core.Entities.MigrationJobLog>()
            };

            return View(viewModel);
        }
    }
}