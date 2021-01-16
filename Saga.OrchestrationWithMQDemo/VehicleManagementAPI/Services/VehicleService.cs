using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using VehicleManagementAPI.Models;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VehicleManagementAPI.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly ILogger<VehicleService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _env;

        public VehicleService(ILogger<VehicleService> logger, 
            IConfiguration iConfig, 
            IHostingEnvironment env)
        {
            _logger = logger;
            _configuration = iConfig;
            _env = env;
        }

        public async Task<bool> RegisterAsync([FromBody] Vehicle vehicle)
        {
            try
            {
                //if (!ModelState.IsValid) return BadRequest();
                if(vehicle.GenerateDemoError)
                    throw new InvalidOperationException("Generated Demo Error based on the input");

                using IDbConnection dbConnection = new SqlConnection(GetConnectionString());
                string sql = @"Insert into Vehicle(LicenseNumber, Brand, Type, OwnerId) 
                                    values(@LicenseNumber, @Brand, @Type, @OwnerId) ";

                int rowsAffected = await dbConnection.ExecuteAsync(sql, vehicle);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                return false;
            }
        }
        
        public async Task<bool> UndoRegisterAsync(string licenseNumber)
        {
            try
            { 
                using IDbConnection dbConnection = new SqlConnection(GetConnectionString());
                string sql = "DELETE FROM Vehicle WHERE LicenseNumber = @licenseNumber ";
                int rowsAffected = await dbConnection.ExecuteAsync(sql, new {licenseNumber});

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                return false;
            }
        }

        private string GetConnectionString()
        {
            string connectionString = _configuration.GetConnectionString("VehicleManagementCN");
            if(connectionString.Contains("%CONTENTROOTPATH%"))
                connectionString = connectionString.Replace("%CONTENTROOTPATH%", _env.ContentRootPath);

            return connectionString;
        }

    }

}
