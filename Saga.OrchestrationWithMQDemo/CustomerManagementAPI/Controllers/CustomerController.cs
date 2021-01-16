using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.Http;

namespace CustomerManagementAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly ILogger<CustomerController> _logger;
        public CustomerController(ILogger<CustomerController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route(ApiEndPoint.SendWelcomeEmail)]
        public IActionResult SendWelcomeEmail(string emailAddress)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest();

                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Welcome Email failed");
                _logger.LogError(ex.StackTrace);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

    }
}
