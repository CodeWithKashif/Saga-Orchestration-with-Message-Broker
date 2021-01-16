using System;

namespace WebApp.Saga
{
    public class RegisterAndPlanJobSagaModel
    {
        //Here EmailAddress is an Aggregate Root
        public string EmailAddress { get; set; }
        public string LicenseNumber { get; set; }
        public string JobId { get; set; }//TODO:convert it to Guid


        //Response from MicroServices
        public bool? RegisterCustomerSucceed { get; set; }
        public bool? RegisterVehicleSucceed { get; set; }
        public bool? PlanMaintenanceJobSucceed { get; set; }

        //Saga progress stats
        public DateTime? SagaStartTimeStamp { get; set; }
        public bool IsSagaStarted => SagaStartTimeStamp != null;
        
        public bool IsSagaSuccessful => 
            (RegisterCustomerSucceed != null && (bool) RegisterCustomerSucceed) 
            && (RegisterVehicleSucceed != null && (bool) RegisterVehicleSucceed) 
            && (PlanMaintenanceJobSucceed != null && (bool) PlanMaintenanceJobSucceed);

        //If any one is failed, Saga would be marked as failed
        public bool IsSagaFailed =>
            (RegisterCustomerSucceed != null && (bool) !RegisterCustomerSucceed)
            || (RegisterVehicleSucceed != null && (bool) !RegisterVehicleSucceed)
            || (PlanMaintenanceJobSucceed != null && (bool) !PlanMaintenanceJobSucceed);

        public DateTime? SagaCompleteTimeStamp { get; set; }
        public bool IsSagaCompleted => (IsSagaSuccessful || IsSagaFailed);

    }
}