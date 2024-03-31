
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VehicleLoanSystem.Models
{
    public class LoanType
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [DisplayName("Loan Type")]
        public string? LoanTypeName { get; set; }
        [Required]
        [DisplayName("Loan Descritption")]
        public string? LoanDescription { get; set; }
       
    }
}
