using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using Polly;
using Polly.Retry;
using WebApp.Models;
using WebApp.RESTClients;
using WebApp.Saga;
using WebApp.ViewModels;

namespace WebApp.Controllers
{
    public class WorkshopManagementController : Controller
    {
        private readonly ILogger<WorkshopManagementController> _logger;
        private readonly ICustomerManagementAPI _customerAPI;
        private readonly IWorkshopManagementAPI _workshopAPI;
        private readonly ISagaOrchestratorBackgroundService _sagaOrchestrator;

        
        public WorkshopManagementController(
            ILogger<WorkshopManagementController> logger, 
            ICustomerManagementAPI customerAPI, 
            IWorkshopManagementAPI workshopAPI, 
            ISagaOrchestratorBackgroundService sagaOrchestrator)
        {
            _logger = logger;
            _customerAPI = customerAPI;
            _workshopAPI = workshopAPI;
            _sagaOrchestrator = sagaOrchestrator;
        }

        public IActionResult Index()
        {
            return View("~/Views/Home/Index.cshtml");
        }

        [HttpGet]
        public IActionResult New()
        {
            var model = new WorkShopManagementNewVM
            {
                Customer = new CustomerRegisterVM()
            };
            return View(model);
        }

        /// <summary>
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RegisterAndPlanMaintenanceJob([FromForm] WorkShopManagementNewVM inputModel)
        {
            if (ModelState.IsValid)
            {
                await _sagaOrchestrator.StartProcessing(inputModel);
                
                RegisterAndPlanJobSagaModel sagaResult = 
                    await _sagaOrchestrator.GetDetailOnSagaComplete(inputModel.Customer.EmailAddress);
                
                if(sagaResult.IsSagaSuccessful)
                {
                    await Notify(inputModel.Customer.EmailAddress);
                    TempData["Message"] = "Your Transaction has been Completed....!";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Message"] = "Something went wrong, Your Transaction is Not Completed.";
                    return View("New", inputModel);
                }
            }
            else
            {
                return View("New", inputModel);
            }
        }


        private async Task Notify(string emailAddress)
        {
            /*If these emails fail we wont consider it as a transaction failure
            and that's the business decision you have to take. 
            Otherwise you have to send another email to disregard(compensate) the previous email*/
            await _customerAPI.SendWelcomeEmail(emailAddress);
            await _workshopAPI.SendMaintenanceJobScheduleDetailEmail(emailAddress);
        }
    }
}
