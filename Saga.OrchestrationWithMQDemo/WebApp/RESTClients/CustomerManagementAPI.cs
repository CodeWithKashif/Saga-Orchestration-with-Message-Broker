using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebApp.RESTClients
{
    public class CustomerManagementAPI : RESTClientsBase, ICustomerManagementAPI
    {
        private readonly ILogger<CustomerManagementAPI> _logger;

        public CustomerManagementAPI(IConfiguration config, ILogger<CustomerManagementAPI> logger)
            : base(config, "CustomerManagementAPI")
        {
            _logger = logger;
        }

        public async Task<HttpResponseMessage> SendWelcomeEmail(string emailAddress)
        {
            HttpResponseMessage httpResponse = await Get("customer/SendWelcomeEmail/", emailAddress);
            _logger.LogInformation($"Welcome Email sent to {emailAddress}");
            return httpResponse;
        }
    }
}
