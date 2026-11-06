using System.Reflection;
using AtlasBlog.Controllers;
using AtlasBlog.Models.EF;
using AtlasBlog.Models.Views;
using AtlasBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace AtlasBlog.Tests;

/// <summary>Tests for an author viewing and editing their own blog.</summary>
public class BlogControllerTests
{
    private readonly Mock<IAuthorService> _service = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private BlogController CreateController(System.Security.Claims.ClaimsPrincipal user)
    {
        var controller = new BlogController(_service.Object, _cache);
        TestHelpers.SetUser(controller, user);
        return controller;
    }

    private static BlogEditViewModel ValidEdit()
    {
        return new BlogEditViewModel { Title = "New Title", Subtitle = "New Subtitle", Description = "New description" };
    }

    // ---------- MyBlog ----------

    [Fact]
    public async Task MyBlog_WhenNotLoggedIn_ReturnsChallenge()
    {
        var result = await CreateController(TestHelpers.AnonymousUser()).MyBlog();

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task MyBlog_WhenAuthorHasNoBlog_ReturnsNotFound()
    {
        _service.Setup(s => s.GetBlogWithPostsAsync(TestHelpers.UserId)).ReturnsAsync((Blog?)null);

        var result = await CreateController(TestHelpers.LoggedInUser("author")).MyBlog();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task MyBlog_WhenAuthorHasBlog_ShowsIt()
    {
        var blog = new Blog { BlogId = 1, Title = "My Blog", AuthorId = TestHelpers.UserId };
        _service.Setup(s => s.GetBlogWithPostsAsync(TestHelpers.UserId)).ReturnsAsync(blog);

        var result = await CreateController(TestHelpers.LoggedInUser("author")).MyBlog();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(blog, view.Model);
    }

    // ---------- Edit (GET) ----------

    [Fact]
    public async Task EditGet_WhenNotLoggedIn_ReturnsChallenge()
    {
        var result = await CreateController(TestHelpers.AnonymousUser()).Edit();

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task EditGet_WhenAuthorHasNoBlog_ReturnsNotFound()
    {
        _service.Setup(s => s.GetBlogForEditAsync(TestHelpers.UserId)).ReturnsAsync((BlogEditViewModel?)null);

        var result = await CreateController(TestHelpers.LoggedInUser("author")).Edit();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditGet_WhenAuthorHasBlog_ShowsFormWithCurrentDetails()
    {
        var model = new BlogEditViewModel { Title = "Current Title" };
        _service.Setup(s => s.GetBlogForEditAsync(TestHelpers.UserId)).ReturnsAsync(model);

        var result = await CreateController(TestHelpers.LoggedInUser("author")).Edit();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
    }

    // ---------- Edit (POST) ----------

    [Fact]
    public async Task EditPost_WithInvalidModel_ShowsFormAgainWithoutSaving()
    {
        var controller = CreateController(TestHelpers.LoggedInUser("author"));
        controller.ModelState.AddModelError("Title", "The Title field is required.");
        var model = new BlogEditViewModel { Title = "" };

        var result = await controller.Edit(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        _service.Verify(s => s.UpdateBlogAsync(It.IsAny<string>(), It.IsAny<BlogEditViewModel>()), Times.Never());
    }

    [Fact]
    public async Task EditPost_WhenNotLoggedIn_ReturnsChallenge()
    {
        var result = await CreateController(TestHelpers.AnonymousUser()).Edit(ValidEdit());

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task EditPost_WhenAuthorHasNoBlog_ReturnsNotFound()
    {
        _service.Setup(s => s.UpdateBlogAsync(TestHelpers.UserId, It.IsAny<BlogEditViewModel>())).ReturnsAsync(false);

        var result = await CreateController(TestHelpers.LoggedInUser("author")).Edit(ValidEdit());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditPost_WithValidModel_SavesAndRedirectsToMyBlog()
    {
        var model = ValidEdit();
        _service.Setup(s => s.UpdateBlogAsync(TestHelpers.UserId, model)).ReturnsAsync(true);
        var controller = CreateController(TestHelpers.LoggedInUser("author"));

        var result = await controller.Edit(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("MyBlog", redirect.ActionName);
        Assert.Equal("Your blog was updated.", controller.TempData["Message"]);
        _service.Verify(s => s.UpdateBlogAsync(TestHelpers.UserId, model), Times.Once());
    }

    [Fact]
    public async Task EditPost_WithValidModel_ClearsHomePageCache()
    {
        _cache.Set(CacheKeys.BlogList, new List<BlogListViewModel>());
        _service.Setup(s => s.UpdateBlogAsync(TestHelpers.UserId, It.IsAny<BlogEditViewModel>())).ReturnsAsync(true);

        await CreateController(TestHelpers.LoggedInUser("author")).Edit(ValidEdit());

        Assert.False(_cache.TryGetValue(CacheKeys.BlogList, out _));
    }

    [Fact]
    public async Task EditPost_WhenSaveFails_KeepsHomePageCache()
    {
        _cache.Set(CacheKeys.BlogList, new List<BlogListViewModel>());
        _service.Setup(s => s.UpdateBlogAsync(TestHelpers.UserId, It.IsAny<BlogEditViewModel>())).ReturnsAsync(false);

        await CreateController(TestHelpers.LoggedInUser("author")).Edit(ValidEdit());

        Assert.True(_cache.TryGetValue(CacheKeys.BlogList, out _));
    }

    [Fact]
    public async Task EditPost_WhenServiceThrows_LetsExceptionReachErrorMiddleware()
    {
        _service.Setup(s => s.UpdateBlogAsync(It.IsAny<string>(), It.IsAny<BlogEditViewModel>()))
            .ThrowsAsync(new InvalidOperationException("Database down"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateController(TestHelpers.LoggedInUser("author")).Edit(ValidEdit()));
    }

    // ---------- Authorization and security ----------

    [Fact]
    public void BlogController_RequiresAuthorPolicy()
    {
        var attribute = typeof(BlogController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("AuthorRequired", attribute!.Policy);
    }

    [Fact]
    public void EditPost_IsProtectedAgainstForgedRequests()
    {
        var postEdit = typeof(BlogController).GetMethods()
            .Single(m => m.Name == nameof(BlogController.Edit) && m.GetCustomAttribute<HttpPostAttribute>() != null);

        Assert.NotNull(postEdit.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }
}