using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Contracts.Services.Dashboard;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize(Roles = "Administrador,Asesor")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics()
        {
            var metrics = await _dashboardService.GetMetricsAsync();
            return Ok(metrics);
        }
    }
}