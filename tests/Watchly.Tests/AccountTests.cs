using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Watchly.Web.Controllers;
using Watchly.Web.Data;
using Watchly.Web.Models.DataModels;
using Watchly.Web.Models.ViewModels;
using Watchly.Web.Services;
using Microsoft.AspNetCore.Hosting;

namespace Watchly.Tests;

public class AccountTests
{
    private static Mock<UserManager<ApplicationUser>> BuildUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<SignInManager<ApplicationUser>> BuildSignInManagerMock(Mock<UserManager<ApplicationUser>> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        return new Mock<SignInManager<ApplicationUser>>(userManager.Object, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);
    }

    private static ApplicationDbContext BuildDbContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static AccountController BuildController(
        Mock<UserManager<ApplicationUser>> userManager,
        Mock<SignInManager<ApplicationUser>> signInManager,
        ApplicationDbContext? db = null)
    {
        db ??= BuildDbContext(Guid.NewGuid().ToString());
        var movieService = Mock.Of<IMovieService>();
        var env = Mock.Of<IWebHostEnvironment>();
        return new AccountController(userManager.Object, signInManager.Object, db, movieService, env);
    }

    [Fact]
    public void Register_Get_Anonymous_ReturnsView()
    {
        var userManager = BuildUserManagerMock();
        var signInManager = BuildSignInManagerMock(userManager);
        var controller = BuildController(userManager, signInManager);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = controller.Register();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Register_Get_Authenticated_Redirects()
    {
        var userManager = BuildUserManagerMock();
        var signInManager = BuildSignInManagerMock(userManager);
        signInManager.Setup(x => x.IsSignedIn(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(true);
        var controller = BuildController(userManager, signInManager);

        var result = controller.Register();

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public void Login_Get_Anonymous_ReturnsView()
    {
        var userManager = BuildUserManagerMock();
        var signInManager = BuildSignInManagerMock(userManager);
        var controller = BuildController(userManager, signInManager);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = controller.Login();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Login_Get_Authenticated_Redirects()
    {
        var userManager = BuildUserManagerMock();
        var signInManager = BuildSignInManagerMock(userManager);
        signInManager.Setup(x => x.IsSignedIn(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(true);
        var controller = BuildController(userManager, signInManager);

        var result = controller.Login();

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task Logout_RedirectsToHome()
    {
        var userManager = BuildUserManagerMock();
        var signInManager = BuildSignInManagerMock(userManager);
        var controller = BuildController(userManager, signInManager);

        var result = await controller.Logout();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task Register_Post_InvalidModel_ReturnsView()
    {
        var userManager = BuildUserManagerMock();
        var signInManager = BuildSignInManagerMock(userManager);
        var controller = BuildController(userManager, signInManager);
        controller.ModelState.AddModelError("x", "error");

        var result = await controller.Register(new RegisterViewModel());

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Login_Post_InvalidModel_ReturnsView()
    {
        var userManager = BuildUserManagerMock();
        var signInManager = BuildSignInManagerMock(userManager);
        var controller = BuildController(userManager, signInManager);
        controller.ModelState.AddModelError("x", "error");

        var result = await controller.Login(new LoginViewModel());

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Register_Post_Success_Redirects()
    {
        var userManager = BuildUserManagerMock();
        userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        var signInManager = BuildSignInManagerMock(userManager);

        var controller = BuildController(userManager, signInManager);
        controller.Url = Mock.Of<Microsoft.AspNetCore.Mvc.IUrlHelper>();

        var result = await controller.Register(new RegisterViewModel
        {
            Email = "a@a.com",
            Username = "user",
            Password = "Password1",
            ConfirmPassword = "Password1"
        });

        Assert.IsType<RedirectToActionResult>(result);
    }
}
