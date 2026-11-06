using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace AtlasBlog.Tests;

/// <summary>
/// Shared setup used by the controller tests: fake logged-in users
/// and the pieces a controller expects from a real web request.
/// </summary>
public static class TestHelpers
{
    public const string UserId = "user-123";
    public const string UserEmail = "tester@test.com";

    /// <summary>A visitor who is not logged in.</summary>
    public static ClaimsPrincipal AnonymousUser()
    {
        return new ClaimsPrincipal(new ClaimsIdentity());
    }

    /// <summary>
    /// A logged-in user. Pass claim names like "author", "moderator",
    /// or "admin" to give them that role (each is set to "true").
    /// </summary>
    public static ClaimsPrincipal LoggedInUser(params string[] roleClaims)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, UserId),
            new Claim(ClaimTypes.Name, UserEmail)
        };

        foreach (var role in roleClaims)
        {
            claims.Add(new Claim(role, "true"));
        }

        // Passing an authentication type ("TestAuth") marks the identity as logged in
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    /// <summary>
    /// Gives a controller a fake web request with the given user,
    /// plus working TempData so success messages can be checked.
    /// </summary>
    public static void SetUser(Controller controller, ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext { User = user };

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
    }
}