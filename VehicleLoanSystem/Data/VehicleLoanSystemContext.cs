using Microsoft.EntityFrameworkCore;

using VehicleLoanSystem.Models;

namespace VehicleLoanSystem.Data
{
    public class VehicleLoanSystemContext : DbContext
    {
        public VehicleLoanSystemContext(DbContextOptions<VehicleLoanSystemContext> options) : base(options)
        {
        }

        public DbSet<UserAccount> Accounts { get; set; }
        public DbSet<LoanPlan> LoanPlans { get; set; }
        public DbSet<LoanType> LoanTypes { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<Payment> Payments { get; set; }

        
    }
}
