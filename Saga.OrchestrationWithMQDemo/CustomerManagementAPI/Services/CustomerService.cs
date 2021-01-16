using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using CustomerManagementAPI.Controllers;
using CustomerManagementAPI.Models;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CustomerManagementAPI.Services
{
    public class CustomerService : ICustomerService
    {

        private readonly ILogger<CustomerController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _env;

        public CustomerService(ILogger<CustomerController> logger, 
            IConfiguration iConfig, 
            IHostingEnvironment env)
        {
            _logger = logger;
            _configuration = iConfig;
            _env = env;
        }

        public async Task<bool> RegisterAsync([FromBody] Customer customer)
        {
            try
            {
                using IDbConnection dbConnection = new SqlConnection(GetConnectionString());
                string sql = @"Insert into Customer(EmailAddress, Name, TelephoneNumber) 
                                    values(@EmailAddress, @Name, @TelephoneNumber) ";

                int rowsAffected = await dbConnection.ExecuteAsync(sql, customer);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                return false;
            }
        }
        
        public async Task<bool> UndoRegisterAsync(string emailAddress)
        {
            try
            {
                //// Undo insert customer
                using IDbConnection dbConnection = new SqlConnection(GetConnectionString());
                string sql = "DELETE FROM Customer WHERE EmailAddress = @emailAddress ";
                int rowsAffected = await dbConnection.ExecuteAsync(sql, new {emailAddress});

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
            string connectionString = _configuration.GetConnectionString("CustomerManagementCN");
            if(connectionString.Contains("%CONTENTROOTPATH%"))
                connectionString = connectionString.Replace("%CONTENTROOTPATH%", _env.ContentRootPath);

            return connectionString;
        }

    }

}
