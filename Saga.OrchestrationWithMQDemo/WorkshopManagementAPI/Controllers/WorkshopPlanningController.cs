using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkshopManagementAPI.Models;

namespace WorkshopManagementAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorkshopPlanningController : ControllerBase
    {

        private readonly ILogger<WorkshopPlanningController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _env;

        public WorkshopPlanningController(ILogger<WorkshopPlanningController> logger,
            IConfiguration iConfig, IHostingEnvironment env)
        {
            _logger = logger;
            _configuration = iConfig;
            _env = env;
        }

        [HttpPost]
        [Route(ApiEndPoint.PlanMaintenanceJobAsync)]
        public async Task<IActionResult> PlanMaintenanceJobAsync([FromBody] PlanMaintenanceJob planMaintenanceJob)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest();

                if(planMaintenanceJob.GenerateDemoError)
                    throw new InvalidOperationException("Generated Demo Error based on the input");

                using (IDbConnection dbConnection = new SqlConnection(GetConnectionString()))
                {
                    string sql = @" INSERT INTO [dbo].[MaintenanceJob] 
                            ([JobId], [WorkshopPlanningDate], [EmailAddress], [VehicleLicenseNumber], [StartTime], [EndTime], [Notes]) 
                        values(@JobId, @PlanningDate, @OwnerId, @LicenseNumber, @StartTime, @EndTime, @Notes)  ";

                    int rowsAffected = await dbConnection.ExecuteAsync(sql, planMaintenanceJob);
                };                
                
                return Ok(new { jobId = planMaintenanceJob.JobId });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet]
        [Route(ApiEndPoint.UndoPlanMaintenanceJobAsync)]
        public async Task<IActionResult> UndoPlanMaintenanceJobAsync(Guid jobId)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest();

                //// Undo insert MaintenanceJob
                using (IDbConnection dbConnection = new SqlConnection(GetConnectionString()))
                {
                    string sql = "DELETE FROM MaintenanceJob WHERE JobId = @jobId ";
                    int rowsAffected = await dbConnection.ExecuteAsync(sql, new {jobId});
                }

                return Ok(new { jobId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet]
        [Route(ApiEndPoint.SendMaintenanceJobScheduleDetailEmail)]
        public IActionResult SendMaintenanceJobScheduleDetailEmail(string emailAddress)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest();

                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Send Maintenance Job Schedule Detail Email failed");
                _logger.LogError(ex.StackTrace);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private string GetConnectionString()
        {
            string connectionString = _configuration.GetConnectionString("WorkshopManagementCN");
            if(connectionString.Contains("%CONTENTROOTPATH%"))
                connectionString = connectionString.Replace("%CONTENTROOTPATH%", _env.ContentRootPath);

            return connectionString;
        }


    }
}
