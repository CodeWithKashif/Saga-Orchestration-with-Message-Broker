using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkshopManagementAPI.Models;

namespace WorkshopManagementAPI.Services
{
    public class WorkshopPlanningService : IWorkshopPlanningService
    {
        private readonly ILogger<WorkshopPlanningService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _env;

        public WorkshopPlanningService(
            ILogger<WorkshopPlanningService> logger, 
            IConfiguration iConfig, 
            IHostingEnvironment env)
        {
            _logger = logger;
            _configuration = iConfig;
            _env = env;
        }

        public async Task<bool> RegisterAsync([FromBody] PlanMaintenanceJob planMaintenanceJob)
        {
            try
            {
                using IDbConnection dbConnection = new SqlConnection(GetConnectionString());
                string sql = @" INSERT INTO [dbo].[MaintenanceJob] 
                            ([JobId], [WorkshopPlanningDate], [EmailAddress], [VehicleLicenseNumber], [StartTime], [EndTime], [Notes]) 
                        values(@JobId, @PlanningDate, @OwnerId, @LicenseNumber, @StartTime, @EndTime, @Notes)  ";

                int rowsAffected = await dbConnection.ExecuteAsync(sql, planMaintenanceJob);
                
                if(planMaintenanceJob.GenerateDemoError)
                    throw new InvalidOperationException("Generated Demo Error based on the input");

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                return false;
            }
        }
        
        public async Task<bool> UndoPlanMaintenanceJobAsync(Guid jobId)
        {
            try
            {
                using IDbConnection dbConnection = new SqlConnection(GetConnectionString());
                string sql = "DELETE FROM MaintenanceJob WHERE JobId = @jobId ";
                int rowsAffected = await dbConnection.ExecuteAsync(sql, new {jobId});
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                return false;
            }
        }

        public bool SendMaintenanceJobScheduleDetailEmail(string emailAddress)
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Maintenance Job ScheduleDetail Email failed");
                _logger.LogError(ex.StackTrace);
                return false;
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
