using System.Reflection;
using AtlasBlog.Controllers;
using AtlasBlog.Models.Views;
using AtlasBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AtlasBlog.Tests;

/// <summary>Tests for the public blog pages, post pages, and comments.</summary>
public class CommunityControllerTests
{
    private readonly Mock<ICommunityService> _service = new();

    private CommunityController CreateController(System.Security.Claims.ClaimsPrincipal user)
    {
        var controller = new CommunityController(_service.Object);
        TestHelpers.SetUser(controller, user);
        return controller;
    }

    // ---------- ViewBlog ----------

    [Fact]
    public async Task ViewBlog_WithNoId_ReturnsNotFound()
    {
        var result = await CreateController(TestHelpers.AnonymousUser()).ViewBlog(null);

        Assert.IsType<NotFoundResult>(result);
        _service.Verify(s => s.GetBlogDetailAsync(It.IsAny<int>()), Times.Never());
    }

    [Fact]
    public async Task ViewBlog_WhenBlogDoesNotExist_ReturnsNotFound()
    {
        _service.Setup(s => s.GetBlogDetailAsync(99)).ReturnsAsync((BlogDetailViewModel?)null);

        var result = await CreateController(TestHelpers.AnonymousUser()).ViewBlog(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ViewBlog_WhenBlogExists_ShowsBlog()
    {
        var blog = new BlogDetailViewModel { BlogId = 1, Title = "My Blog" };
        _service.Setup(s => s.GetBlogDetailAsync(1)).ReturnsAsync(blog);

        var result = await CreateController(TestHelpers.AnonymousUser()).ViewBlog(1);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(blog, view.Model);
    }

    // ---------- ViewPost ----------

    [Fact]
    public async Task ViewPost_WithNoId_ReturnsNotFound()
    {
        var result = await CreateController(TestHelpers.AnonymousUser()).ViewPost(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ViewPost_WhenPostDoesNotExist_ReturnsNotFound()
    {
        _service.Setup(s => s.GetPostDetailAsync(99, It.IsAny<bool>())).ReturnsAsync((PostDetailViewModel?)null);

        var result = await CreateController(TestHelpers.AnonymousUser()).ViewPost(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ViewPost_WhenPostExists_ShowsPost()
    {
        var post = new PostDetailViewModel { PostId = 5, Title = "Hello" };
        _service.Setup(s => s.GetPostDetailAsync(5, It.IsAny<bool>())).ReturnsAsync(post);

        var result = await CreateController(TestHelpers.AnonymousUser()).ViewPost(5);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(post, view.Model);
    }

    [Fact]
    public async Task ViewPost_ForAnonymousVisitor_HidesHiddenComments()
    {
        _service.Setup(s => s.GetPostDetailAsync(5, It.IsAny<bool>())).ReturnsAsync(new PostDetailViewModel());

        await CreateController(TestHelpers.AnonymousUser()).ViewPost(5);

        _service.Verify(s => s.GetPostDetailAsync(5, false), Times.Once());
    }

    [Fact]
    public async Task ViewPost_ForRegularUser_HidesHiddenComments()
    {
        _service.Setup(s => s.GetPostDetailAsync(5, It.IsAny<bool>())).ReturnsAsync(new PostDetailViewModel());

        await CreateController(TestHelpers.LoggedInUser()).ViewPost(5);

        _service.Verify(s => s.GetPostDetailAsync(5, false), Times.Once());
    }

    [Theory]
    [InlineData("moderator")]
    [InlineData("admin")]
    public async Task ViewPost_ForModeratorOrAdmin_IncludesHiddenComments(string role)
    {
        _service.Setup(s => s.GetPostDetailAsync(5, It.IsAny<bool>())).ReturnsAsync(new PostDetailViewModel());

        await CreateController(TestHelpers.LoggedInUser(role)).ViewPost(5);

        _service.Verify(s => s.GetPostDetailAsync(5, true), Times.Once());
    }

    // ---------- AddComment ----------

    [Fact]
    public async Task AddComment_WithInvalidModel_RedirectsBackWithoutSaving()
    {
        var controller = CreateController(TestHelpers.LoggedInUser());
        controller.ModelState.AddModelError("Content", "The Content field is required.");

        var result = await controller.AddComment(new CreateCommentViewModel { PostId = 5, Content = "" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ViewPost", redirect.ActionName);
        Assert.Equal(5, redirect.RouteValues!["id"]);
        _service.Verify(s => s.AddCommentAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async Task AddComment_WithNoUserId_ReturnsChallenge()
    {
        var controller = CreateController(TestHelpers.AnonymousUser());

        var result = await controller.AddComment(new CreateCommentViewModel { PostId = 5, Content = "Hi" });

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task AddComment_WhenPostDoesNotExist_ReturnsNotFound()
    {
        _service.Setup(s => s.AddCommentAsync(99, TestHelpers.UserId, "Hi")).ReturnsAsync(false);

        var result = await CreateController(TestHelpers.LoggedInUser())
            .AddComment(new CreateCommentViewModel { PostId = 99, Content = "Hi" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddComment_WithValidComment_SavesItAndRedirectsToPost()
    {
        _service.Setup(s => s.AddCommentAsync(5, TestHelpers.UserId, "Great post!")).ReturnsAsync(true);
        var controller = CreateController(TestHelpers.LoggedInUser());

        var result = await controller.AddComment(new CreateCommentViewModel { PostId = 5, Content = "Great post!" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ViewPost", redirect.ActionName);
        Assert.Equal(5, redirect.RouteValues!["id"]);
        Assert.Equal("Your comment was added.", controller.TempData["Message"]);
        _service.Verify(s => s.AddCommentAsync(5, TestHelpers.UserId, "Great post!"), Times.Once());
    }

    // ---------- ToggleCommentVisibility ----------

    [Fact]
    public async Task ToggleCommentVisibility_WhenCommentDoesNotExist_ReturnsNotFound()
    {
        _service.Setup(s => s.ToggleCommentVisibilityAsync(99)).ReturnsAsync(false);

        var result = await CreateController(TestHelpers.LoggedInUser("moderator"))
            .ToggleCommentVisibility(99, 5);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ToggleCommentVisibility_WhenCommentExists_RedirectsToPostWithMessage()
    {
        _service.Setup(s => s.ToggleCommentVisibilityAsync(7)).ReturnsAsync(true);
        var controller = CreateController(TestHelpers.LoggedInUser("moderator"));

        var result = await controller.ToggleCommentVisibility(7, 5);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ViewPost", redirect.ActionName);
        Assert.Equal(5, redirect.RouteValues!["id"]);
        Assert.Equal("Comment visibility updated.", controller.TempData["Message"]);
    }

    // ---------- ViewAllPosts ----------

    [Fact]
    public async Task ViewAllPosts_WithNoBlogId_ReturnsNotFound()
    {
        var result = await CreateController(TestHelpers.AnonymousUser()).ViewAllPosts(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ViewAllPosts_WhenBlogDoesNotExist_ReturnsNotFound()
    {
        _service.Setup(s => s.GetBlogPostsAsync(99)).ReturnsAsync((BlogPostsViewModel?)null);

        var result = await CreateController(TestHelpers.AnonymousUser()).ViewAllPosts(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ViewAllPosts_WhenBlogExists_ShowsPosts()
    {
        var posts = new BlogPostsViewModel { BlogId = 1, BlogTitle = "My Blog" };
        _service.Setup(s => s.GetBlogPostsAsync(1)).ReturnsAsync(posts);

        var result = await CreateController(TestHelpers.AnonymousUser()).ViewAllPosts(1);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(posts, view.Model);
    }

    // ---------- Authorization rules ----------
    // Unit tests don't run ASP.NET's login checks, so we confirm the
    // [Authorize] attributes are in place; the framework enforces them.

    [Fact]
    public void AddComment_RequiresLogin()
    {
        var method = typeof(CommunityController).GetMethod(nameof(CommunityController.AddComment))!;

        Assert.NotNull(method.GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void ToggleCommentVisibility_RequiresModeratorOrAdmin()
    {
        var method = typeof(CommunityController).GetMethod(nameof(CommunityController.ToggleCommentVisibility))!;

        var attribute = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal("ModeratorOrAdmin", attribute!.Policy);
    }

    [Theory]
    [InlineData(nameof(CommunityController.ViewBlog))]
    [InlineData(nameof(CommunityController.ViewPost))]
    [InlineData(nameof(CommunityController.ViewAllPosts))]
    public void ReadingPages_AreOpenToEveryone(string actionName)
    {
        var method = typeof(CommunityController).GetMethod(actionName)!;

        Assert.Null(method.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(typeof(CommunityController).GetCustomAttribute<AuthorizeAttribute>());
    }
}