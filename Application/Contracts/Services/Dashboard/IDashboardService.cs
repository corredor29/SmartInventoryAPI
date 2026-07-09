using System.Threading.Tasks;
using Application.DTOs.Dashboard;

namespace Application.Contracts.Services.Dashboard
{
    public interface IDashboardService
    {
        Task<DashboardMetricsDto> GetMetricsAsync();
    }
}