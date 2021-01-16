namespace WebApp.Models
{
    public class Vehicle : ModelBase
    {
        public string LicenseNumber { get; set; }
        public string Brand { get; set; }
        public string Type { get; set; }
        public string OwnerId { get; set; }
        public bool GenerateDemoError { get; set; }

    }
}