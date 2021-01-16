using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WorkshopManagementAPI.Models;

namespace WorkshopManagementAPI.Services
{
    public interface IWorkshopPlanningService
    {
        Task<bool> RegisterAsync([FromBody] PlanMaintenanceJob planMaintenanceJob);
        Task<bool> UndoPlanMaintenanceJobAsync(Guid jobId);
        bool SendMaintenanceJobScheduleDetailEmail(string emailAddress);
    }
}