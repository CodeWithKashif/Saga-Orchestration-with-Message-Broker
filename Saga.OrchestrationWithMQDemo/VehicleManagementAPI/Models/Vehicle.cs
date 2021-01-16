namespace VehicleManagementAPI.Models
{
    public class Vehicle
    {
        public string LicenseNumber { get; set; }
        public string Brand { get; set; }
        public string Type { get; set; }
        public string OwnerId { get; set; }

        //Added for Demonstration purpose - this will throw exception in api
        public bool GenerateDemoError { get; set; }

    }

}