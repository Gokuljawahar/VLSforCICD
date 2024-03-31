using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Microsoft.AspNetCore.TestHost;
using System.Threading.Tasks;
using VehicleLoanSystem.Controllers;
using VehicleLoanSystem.Data;
using VehicleLoanSystem.Models;
using Microsoft.AspNetCore.Http.Features;
namespace VehicleLoanSystem.test
{
    public class SignUpControllerTests
    {
        private SignUpController _controller;

        [SetUp]
        public void Setup()
        {
            // Mocking DbContextOptions
            var options = new DbContextOptionsBuilder<VehicleLoanSystemContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            // Creating an instance of VehicleLoanSystemContext
            var context = new VehicleLoanSystemContext(options);

            // Mocking HttpContext
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Request.Path = new PathString("/");

            // Initializing SignUpController with the mocked context and HttpContext
            _controller = new SignUpController(context)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContext
                }
            };
        }


        [Test]
        public async Task Create_ValidUser_RedirectsToCustomerDashboard()
        {
            // Arrange
            var newUser = new UserAccount
            {
                User_Name = "TestUser123",
                User_Password = "TestPassword@123"
            };

            // Act
            var result = await _controller.Create(newUser) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Customer", result.ControllerName);
        }




    }
}



