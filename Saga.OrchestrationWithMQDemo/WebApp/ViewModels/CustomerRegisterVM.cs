using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels
{
    public class CustomerRegisterVM
    {
        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "PhoneNumber")]
        public string TelephoneNumber { get; set; }

        [Required]
        [Display(Name = "Email address")]
        [EmailAddress()]
        public string EmailAddress { get; set; }
    }
}