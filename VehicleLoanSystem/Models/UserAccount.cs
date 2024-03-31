
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;


namespace VehicleLoanSystem.Models
{
    public class UserAccount
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{10,}$", 
        ErrorMessage = "Username must be at least 10 characters long and contain at least one uppercase letter and one number.")]
        public string? User_Name { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[!@#$%^&*])[A-Za-z\d!@#$%^&*]{10,}$",
        ErrorMessage = "Password must be at least 10 characters long and contain at least one uppercase letter and one special character.")]
        public string? User_Password { get; set; }
        
        [DefaultValue(false)]
        public bool IsAdmin { get; set; }

    }
}
