using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels
{
    public class VehicleRegisterVM
    {
        //[Required]
        [Display(Name = "License number")]
        public string LicenseNumber { get; set; }

        //[Required]
        [Display(Name = "Brand")]
        public string Brand { get; set; }

        //[Required]
        [Display(Name = "Type")]
        public string Type { get; set; }

        [Display(Name = "Owner")]
        public string OwnerId { get; set; }

        public bool GenerateDemoError { get; set; }
    }
}