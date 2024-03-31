using NUnit.Framework;
using System.Threading.Tasks;
using VehicleLoanSystem.Controllers;
using VehicleLoanSystem.Models;
using VehicleLoanSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace VehicleLoanSystem.test
{
    public class LoginControllerTests
    {
        private VehicleLoanSystemContext _context;

        [SetUp]
        public void Setup()
        {
            // In-memory database for testing
            var options = new DbContextOptionsBuilder<VehicleLoanSystemContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
            _context = new VehicleLoanSystemContext(options);

            // Add some test data if needed
        }
        [Test]
        public async Task ActivateAdminAccount_AlreadyActivated()
        {
            // Arrange
            var controller = new LoginController(_context);

            // Add an admin account manually
            var adminUser = new UserAccount
            {
                User_Name = "VLSadministrator05",
                User_Password = "StrongPassword@123",
                IsAdmin = true
            };
            _context.Accounts.Add(adminUser);
            await _context.SaveChangesAsync();

            // Act
            bool result = await controller.ActivateAdminAccount();

            // Assert
            // If the method is expected to create a new admin account even if one already exists
            Assert.IsTrue(result);

            // If the method is expected to return false when an admin account already exists
            // Assert.IsFalse(result);
        }

        

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
        }
    }
}
