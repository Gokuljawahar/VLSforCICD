
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace VehicleLoanSystem.Models
{
    public class LoanPlan
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [DisplayName("Loan Plan in Month")]
        public int Month { get; set; }
        [Required]
        [DisplayName("Loan Interest")]
        public double Interest { get; set; }
        [Required]
        [DisplayName("Monthly Over Due Penalty")]
        public double MonthlyOverDuePenalty { get; set; }
    }
}
