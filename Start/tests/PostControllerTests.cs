using System.Reflection;
using AtlasBlog.Controllers;
using AtlasBlog.Models.EF;
using AtlasBlog.Models.Views;
using AtlasBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AtlasBlog.Tests;

/// <summary>Tests for an author listing, creating, and editing their own posts.</summary>
public class PostControllerTests
{
    private readonly Mock<IAuthorService> _service = new();

    private PostController CreateController(System.Security.Claims.ClaimsPrincipal user)
    {
        var controller = new PostController(_service.Object);
        TestHelpers.SetUser(controller, user);
        return controller;
    }

    private static PostEditViewModel ValidPost()
    {
        return new PostEditViewModel { Title = "My Post", Content = "<p>Hello</p>" };
    }

    // ---------- Index ----------

    [Fact]
    public async Task Index_WhenNotLoggedIn_ReturnsChallenge()
    {
        var result = await CreateController(TestHelpers.AnonymousUser()).Index();

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task Index_WhenAuthorHasNoBlog_ReturnsNotFound()
    {
        _service.Setup(s => s.GetPostsAsync(TestHelpers.UserId)).ReturnsAsync((List<Post>?)null);

        var result = await CreateController(TestHelpers.LoggedInUser("author")).Index();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Index_WhenAuthorHasPosts_ShowsThem()
    {
        var posts = new List<Post> { new Post { PostId = 1, Title = "First" } };
        _service.Setup(s => s.GetPostsAsync(TestHelpers.UserId)).ReturnsAsync(posts);

        var result = await CreateController(TestHelpers.LoggedInUser("author")).Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(posts, view.Model);
    }

    // ---------- Create (GET) ----------

    [Fact]
    public void CreateGet_ShowsEmptyForm()
    {
        var result = CreateController(TestHelpers.LoggedInUser("author")).Create();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PostEditViewModel>(view.Model);
        Assert.Equal(string.Empty, model.Title);
        Assert.Null(model.PostId);
    }

    // ---------- Create (POST) ----------

    [Fact]
    public async Task CreatePost_WithInvalidModel_ShowsFormAgainWithoutSaving()
    {
        var controller = CreateController(TestHelpers.LoggedInUser("author"));
        controller.ModelState.AddModelError("Title", "The Title field is required.");
        var model = new PostEditViewModel();

        var result = await controller.Create(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        _service.Verify(s => s.CreatePostAsync(It.IsAny<string>(), It.IsAny<PostEditViewModel>()), Times.Never());
    }

    [Fact]
    public async Task CreatePost_WhenNotLoggedIn_ReturnsChallenge()
    {
        var result = await CreateController(TestHelpers.AnonymousUser()).Create(ValidPost());

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task CreatePost_WhenAuthorHasNoBlog_ReturnsNotFound()
    {
        _service.Setup(s => s.CreatePostAsync(TestHelpers.UserId, It.IsAny<PostEditViewModel>())).ReturnsAsync(false);

        var result = await CreateController(TestHelpers.LoggedInUser("author")).Create(ValidPost());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreatePost_WithValidModel_SavesPostOnce()
    {
        var model = ValidPost();
        _service.Setup(s => s.CreatePostAsync(TestHelpers.UserId, model)).ReturnsAsync(true);

        await CreateController(TestHelpers.LoggedInUser("author")).Create(model);

        // This is the test that catches a Create action that forgets to save
        _service.Verify(s => s.CreatePostAsync(TestHelpers.UserId, model), Times.Once());
    }

    [Fact]
    public async Task CreatePost_WithValidModel_RedirectsToPostListWithMessage()
    {
        _service.Setup(s => s.CreatePostAsync(TestHelpers.UserId, It.IsAny<PostEditViewModel>())).ReturnsAsync(true);
        var controller = CreateController(TestHelpers.LoggedInUser("author"));

        var result = await controller.Create(ValidPost());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Your post was published.", controller.TempData["Message"]);
    }

    // ---------- Edit (GET) ----------

    [Fact]
    public async Task EditGet_WhenNotLoggedIn_ReturnsChallenge()
    {
        var result = await CreateController(TestHelpers.AnonymousUser()).Edit(5);

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task EditGet_WhenPostIsMissingOrBelongsToSomeoneElse_ReturnsNotFound()
    {
        _service.Setup(s => s.GetPostForEditAsync(TestHelpers.UserId, 5)).ReturnsAsync((PostEditViewModel?)null);

        var result = await CreateController(TestHelpers.LoggedInUser("author")).Edit(5);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditGet_WhenAuthorOwnsPost_ShowsFormWithPost()
    {
        var model = new PostEditViewModel { PostId = 5, Title = "My Post", Content = "Hi" };
        _service.Setup(s => s.GetPostForEditAsync(TestHelpers.UserId, 5)).ReturnsAsync(model);

        var result = await CreateController(TestHelpers.LoggedInUser("author")).Edit(5);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
    }

    // ---------- Edit (POST) ----------

    [Fact]
    public async Task EditPost_WithInvalidModel_ShowsFormAgainWithPostIdRestored()
    {
        var controller = CreateController(TestHelpers.LoggedInUser("author"));
        controller.ModelState.AddModelError("Title", "The Title field is required.");
        var model = new PostEditViewModel { Title = "", Content = "Hi" };

        var result = await controller.Edit(5, model);

        var view = Assert.IsType<ViewResult>(result);
        var returned = Assert.IsType<PostEditViewModel>(view.Model);
        Assert.Equal(5, returned.PostId);
        _service.Verify(s => s.UpdatePostAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<PostEditViewModel>()), Times.Never());
    }

    [Fact]
    public async Task EditPost_WhenNotLoggedIn_ReturnsChallenge()
    {
        var result = await CreateController(TestHelpers.AnonymousUser()).Edit(5, ValidPost());

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task EditPost_WhenPostIsMissingOrBelongsToSomeoneElse_ReturnsNotFound()
    {
        _service.Setup(s => s.UpdatePostAsync(TestHelpers.UserId, 5, It.IsAny<PostEditViewModel>())).ReturnsAsync(false);

        var result = await CreateController(TestHelpers.LoggedInUser("author")).Edit(5, ValidPost());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditPost_WithValidModel_SavesAndRedirectsToPostList()
    {
        var model = ValidPost();
        _service.Setup(s => s.UpdatePostAsync(TestHelpers.UserId, 5, model)).ReturnsAsync(true);
        var controller = CreateController(TestHelpers.LoggedInUser("author"));

        var result = await controller.Edit(5, model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Your post was updated.", controller.TempData["Message"]);
        _service.Verify(s => s.UpdatePostAsync(TestHelpers.UserId, 5, model), Times.Once());
    }

    [Fact]
    public async Task EditPost_WhenServiceThrows_LetsExceptionReachErrorMiddleware()
    {
        _service.Setup(s => s.UpdatePostAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<PostEditViewModel>()))
            .ThrowsAsync(new InvalidOperationException("Database down"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateController(TestHelpers.LoggedInUser("author")).Edit(5, ValidPost()));
    }

    // ---------- Authorization and security ----------

    [Fact]
    public void PostController_RequiresAuthorPolicy()
    {
        var attribute = typeof(PostController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("AuthorRequired", attribute!.Policy);
    }

    [Theory]
    [InlineData(nameof(PostController.Create))]
    [InlineData(nameof(PostController.Edit))]
    public void PostActions_AreProtectedAgainstForgedRequests(string actionName)
    {
        var postAction = typeof(PostController).GetMethods()
            .Single(m => m.Name == actionName && m.GetCustomAttribute<HttpPostAttribute>() != null);

        Assert.NotNull(postAction.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }
}