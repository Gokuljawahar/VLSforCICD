

using Microsoft.EntityFrameworkCore;


namespace VehicleLoanSystem.Models
{
    [Keyless]
    public class AllModel
    {
        public List<Loan>? Loans { get; set; }
        public List<LoanPlan>? LoanPlans { get; set; }
        public List<LoanType>? LoanTypes { get; set; }
        public List<Payment>? Payments { get; set; }
        public List<UserAccount>? Accounts { get; set; }
    }
}

