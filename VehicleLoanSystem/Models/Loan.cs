
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;


namespace VehicleLoanSystem.Models
{
    public class Loan
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [DisplayName("First Name")]
        public string? FirstName { get; set; }
        [Required]
        [DisplayName("Last Name")]
        public string? LastName { get; set; }


        [Required]
        [DisplayName("Gender")]
        public string? Gender { get; set; }

        [DisplayName("Email")]
        [Required(ErrorMessage = "The Email Address is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string? Email { get; set; }

        
        [Required(ErrorMessage = "Phone number is required")]
        [DisplayName("Phone Number")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid phone number. Please enter a 10-digit number starting with 6 to 9.")]
        public decimal Phone { get; set; }
        [Required]
        [DisplayName("Loan Plan")]
        public int LoanPlanId { get; set; }
        [Required]
        [Range(100000, double.MaxValue, ErrorMessage = "The Minimul Salary to avail Loan is 100000")]
        [DisplayName("Salary")]
        public double Salary { get; set; }
        [Required]
        [DisplayName("Loan Type")]
        public int LoanTypeId { get; set; }
        [Required]
        [DisplayName("Loan Purpose")]
        public string? LoanPurpose { get; set; }
        [Required]
        [Range(50000, double.MaxValue, ErrorMessage = "The Minimul loan amount is 50000")]
        [DisplayName("Loan Amount")]
        public double LoanAmount { get; set; }


        [DefaultValue("PENDING")]
        [DisplayName("Loan Grant")]
        public string? LoanGrant { get; set; }

        [DisplayName("Loan Date")]
        public DateTime LoanDate { get; set; }
        [DefaultValue(0)]
        [DisplayName("Total Payable Amount")]
        public double TotalPayableAmount { get; set; }
        [DefaultValue(0)]
        [DisplayName("Monthly Payable Amount")]
        public double MonthlyPayableAmount { get; set; }
        [DefaultValue(0)]
        [DisplayName("Monthly Penalty")]
        public double MonthlyPenalty { get; set; }
        [DisplayName("Rejection Reason")]
        [DefaultValue("None")]
        public string? RejectionReason { get; set; }
       
        [Required]
        [DisplayName("Credit score")]

        public string? credit_score { get; set; }
       
        [Display(Name = "Identity Report")]
        public byte[]? IdentityImage { get; set; }


        [Display(Name = "Income Report")]
        public byte[]? IncomeImage { get; set; }

        [Display(Name = "CIBIL Report")]
        public byte[]? CibilImage { get; set; }

        [DisplayName("User Id")]
        public int UserId { get; set; }


    }
}


