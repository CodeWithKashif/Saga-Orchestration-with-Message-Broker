namespace WebApp.Common
{
    public enum ServiceResponseMessageType
    {
        RegisterCustomerSucceed = 1,
        RegisterCustomerFailed = 2,
        
        RegisterVehicleSucceed = 3,
        RegisterVehicleFailed = 4,

        PlanMaintenanceJobSucceed = 5,
        PlanMaintenanceJobFailed = 6,
        
        UndoRegisterCustomerSucceed = 7,
        UndoRegisterCustomerFailed = 8,
        UndoRegisterVehicleFailed = 9,
        UndoRegisterVehicleSucceed = 10,
        UndoPlanMaintenanceJobFailed = 11,
        UndoPlanMaintenanceJobSucceed = 12,

    }
}