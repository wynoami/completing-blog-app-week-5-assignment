using System.Reflection;
using System.Security.Claims;
using AtlasBlog.Controllers;
using AtlasBlog.Data;
using AtlasBlog.Models.EF;
using AtlasBlog.Models.Views;
using AtlasBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace AtlasBlog.Tests;

/// <summary>Tests for logging in, registering, logging out, and access denied.</summary>
public class AccountControllerTests : IDisposable
{
    private const string Email = "new@test.com";
    private const string Password = "Passw0rd!";

    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManager;
    private readonly Mock<IUrlHelper> _urlHelper = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly TestDatabase _database = new(enforceForeignKeys: false);
    private readonly ApplicationDbContext _context;

    public AccountControllerTests()
    {
        // UserManager and SignInManager are classes, not interfaces, so Moq needs
        // the same constructor arguments the real classes take. Only the first
        // one (or first three for SignInManager) must be non-null; the rest can be null.
        _userManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _signInManager = new Mock<SignInManager<ApplicationUser>>(
            _userManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            null!, null!, null!, null!);

        _context = _database.CreateContext();
    }

    public void Dispose()
    {
        _context.Dispose();
        _database.Dispose();
    }

    private AccountController CreateController()
    {
        var controller = new AccountController(_userManager.Object, _signInManager.Object, _context, _cache);
        TestHelpers.SetUser(controller, TestHelpers.AnonymousUser());
        controller.Url = _urlHelper.Object;
        return controller;
    }

    private static LoginViewModel ValidLogin()
    {
        return new LoginViewModel { Email = "reader@test.com", Password = Password };
    }

    private static RegisterViewModel ValidRegistration(string accountType = "User")
    {
        return new RegisterViewModel { Email = Email, Password = Password, ConfirmPassword = Password, AccountType = accountType };
    }

    private void SetupSignIn(SignInResult result)
    {
        _signInManager
            .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(result);
    }

    private void SetupCreateUser(IdentityResult result)
    {
        _userManager
            .Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(result);
    }

    private static string AllErrors(Controller controller)
    {
        return string.Join(" | ", controller.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
    }

    // ---------- Login (GET) ----------

    [Fact]
    public void LoginGet_ShowsLoginForm()
    {
        var result = CreateController().Login();

        Assert.IsType<ViewResult>(result);
    }

    // ---------- Login (POST) ----------

    [Fact]
    public async Task LoginPost_WithInvalidModel_ShowsFormWithoutTryingToSignIn()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError("Email", "The Email field is required.");
        var model = new LoginViewModel();

        var result = await controller.Login(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        _signInManager.Verify(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());
    }

    [Fact]
    public async Task LoginPost_WithCorrectPassword_RedirectsToHome()
    {
        SetupSignIn(SignInResult.Success);

        var result = await CreateController().Login(ValidLogin());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task LoginPost_TurnsOnLockoutForFailedAttempts()
    {
        SetupSignIn(SignInResult.Success);

        await CreateController().Login(ValidLogin());

        // The last argument is lockoutOnFailure, which must be true
        _signInManager.Verify(s => s.PasswordSignInAsync("reader@test.com", Password, false, true), Times.Once());
    }

    [Fact]
    public async Task LoginPost_WithLocalReturnUrl_SendsUserBackToThatPage()
    {
        SetupSignIn(SignInResult.Success);
        _urlHelper.Setup(u => u.IsLocalUrl("/Post/Create")).Returns(true);

        var result = await CreateController().Login(ValidLogin(), "/Post/Create");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/Post/Create", redirect.Url);
    }

    [Fact]
    public async Task LoginPost_WithOutsideReturnUrl_IgnoresItAndGoesHome()
    {
        SetupSignIn(SignInResult.Success);
        _urlHelper.Setup(u => u.IsLocalUrl("https://evil.example.com")).Returns(false);

        var result = await CreateController().Login(ValidLogin(), "https://evil.example.com");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task LoginPost_WithWrongPassword_ShowsGenericError()
    {
        SetupSignIn(SignInResult.Failed);
        var controller = CreateController();

        var result = await controller.Login(ValidLogin());

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains("Invalid login attempt.", AllErrors(controller));
    }

    [Fact]
    public async Task LoginPost_WhenAccountIsLockedOut_ShowsLockoutMessage()
    {
        SetupSignIn(SignInResult.LockedOut);
        var controller = CreateController();

        var result = await controller.Login(ValidLogin());

        Assert.IsType<ViewResult>(result);
        Assert.Contains("locked", AllErrors(controller));
    }

    // ---------- Register (GET) ----------

    [Fact]
    public void RegisterGet_ShowsFormWithUserPreselected()
    {
        var result = CreateController().Register();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<RegisterViewModel>(view.Model);
        Assert.Equal("User", model.AccountType);
    }

    // ---------- Register (POST): failures ----------

    [Fact]
    public async Task RegisterPost_WithInvalidModel_ShowsFormWithoutCreatingUser()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError("Email", "The Email field is required.");

        var result = await controller.Register(ValidRegistration());

        Assert.IsType<ViewResult>(result);
        _userManager.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async Task RegisterPost_WithMadeUpAccountType_IsRejected()
    {
        var controller = CreateController();

        var result = await controller.Register(ValidRegistration("SuperAdmin"));

        Assert.IsType<ViewResult>(result);
        Assert.Contains("valid account type", AllErrors(controller));
        _userManager.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async Task RegisterPost_WhenIdentityRejectsPassword_ShowsReasonsAndDoesNotSignIn()
    {
        SetupCreateUser(IdentityResult.Failed(new IdentityError { Description = "Passwords must have at least one digit ('0'-'9')." }));
        var controller = CreateController();

        var result = await controller.Register(ValidRegistration());

        Assert.IsType<ViewResult>(result);
        Assert.Contains("at least one digit", AllErrors(controller));
        _signInManager.Verify(s => s.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never());
    }

    // ---------- Register (POST): success ----------

    [Fact]
    public async Task RegisterPost_AsUser_CreatesAccountSignsInAndRedirectsHome()
    {
        SetupCreateUser(IdentityResult.Success);
        var controller = CreateController();

        var result = await controller.Register(ValidRegistration("User"));

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal("Welcome! Your account has been created.", controller.TempData["Message"]);
        _userManager.Verify(u => u.CreateAsync(It.Is<ApplicationUser>(x => x.Email == Email && x.UserName == Email), Password), Times.Once());
        _signInManager.Verify(s => s.SignInAsync(It.IsAny<ApplicationUser>(), false, It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public async Task RegisterPost_AsUser_AddsNoRoleClaims()
    {
        SetupCreateUser(IdentityResult.Success);

        await CreateController().Register(ValidRegistration("User"));

        _userManager.Verify(u => u.AddClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>()), Times.Never());
    }

    [Theory]
    [InlineData("Author", "author")]
    [InlineData("Moderator", "moderator")]
    [InlineData("Admin", "admin")]
    public async Task RegisterPost_WithRoleAccountType_AddsMatchingClaim(string accountType, string expectedClaim)
    {
        SetupCreateUser(IdentityResult.Success);

        await CreateController().Register(ValidRegistration(accountType));

        _userManager.Verify(u => u.AddClaimAsync(
            It.IsAny<ApplicationUser>(),
            It.Is<Claim>(c => c.Type == expectedClaim && c.Value == "true")), Times.Once());
    }

    [Fact]
    public async Task RegisterPost_AsAuthor_CreatesStarterBlog()
    {
        SetupCreateUser(IdentityResult.Success);

        await CreateController().Register(ValidRegistration("Author"));

        using var checkContext = _database.CreateContext();
        var blog = Assert.Single(checkContext.Blogs.ToList());
        Assert.Equal($"{Email}'s Blog", blog.Title);
    }

    [Fact]
    public async Task RegisterPost_AsAuthor_ClearsHomePageCache()
    {
        SetupCreateUser(IdentityResult.Success);
        _cache.Set(CacheKeys.BlogList, new List<BlogListViewModel>());

        await CreateController().Register(ValidRegistration("Author"));

        Assert.False(_cache.TryGetValue(CacheKeys.BlogList, out _));
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Moderator")]
    [InlineData("Admin")]
    public async Task RegisterPost_AsNonAuthor_CreatesNoBlogAndKeepsCache(string accountType)
    {
        SetupCreateUser(IdentityResult.Success);
        _cache.Set(CacheKeys.BlogList, new List<BlogListViewModel>());

        await CreateController().Register(ValidRegistration(accountType));

        using var checkContext = _database.CreateContext();
        Assert.Empty(checkContext.Blogs.ToList());
        Assert.True(_cache.TryGetValue(CacheKeys.BlogList, out _));
    }

    [Fact]
    public async Task RegisterPost_WhenCreatingUserThrows_LetsExceptionReachErrorMiddleware()
    {
        _userManager
            .Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Database down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateController().Register(ValidRegistration()));
    }

    // ---------- Logout and AccessDenied ----------

    [Fact]
    public async Task Logout_SignsOutAndRedirectsHomeWithMessage()
    {
        var controller = CreateController();

        var result = await controller.Logout();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal("You have been logged out.", controller.TempData["Message"]);
        _signInManager.Verify(s => s.SignOutAsync(), Times.Once());
    }

    [Fact]
    public void AccessDenied_ShowsAccessDeniedPage()
    {
        var result = CreateController().AccessDenied();

        Assert.IsType<ViewResult>(result);
    }

    // ---------- Security ----------

    [Theory]
    [InlineData(nameof(AccountController.Login))]
    [InlineData(nameof(AccountController.Register))]
    [InlineData(nameof(AccountController.Logout))]
    public void PostActions_AreProtectedAgainstForgedRequests(string actionName)
    {
        var postAction = typeof(AccountController).GetMethods()
            .Single(m => m.Name == actionName && m.GetCustomAttribute<HttpPostAttribute>() != null);

        Assert.NotNull(postAction.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public void AccountController_IsOpenToVisitorsWhoAreNotLoggedIn()
    {
        Assert.Null(typeof(AccountController).GetCustomAttribute<AuthorizeAttribute>());
    }
}