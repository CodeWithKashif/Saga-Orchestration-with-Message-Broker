using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Models;

namespace WebApp.ViewModels
{
    public class WorkShopManagementNewVM
    {
        public CustomerRegisterVM Customer { get; set; }
        public VehicleRegisterVM Vehicle { get; set; }
        public PlanMaintenanceJobVM MaintenanceJob { get; set; }
        
        private string _microServiceName;
        public string MicroServiceName
        {
            get { return _microServiceName; }
            set
            {
                _microServiceName = value;

                if (value == "VehicleManagementMicroservice")
                    this.Vehicle.GenerateDemoError = true;
                else if (value == "WorkshopManagementMicroservice")
                    this.MaintenanceJob.GenerateDemoError = true;
                else
                {
                    this.Vehicle.GenerateDemoError = false;
                    this.MaintenanceJob.GenerateDemoError = false;
                }
            }
        }

    }

}