using AtlasBlog.Controllers;
using AtlasBlog.Models.Views;
using AtlasBlog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AtlasBlog.Tests;

/// <summary>Tests for the home page and its 30-minute cache.</summary>
public class HomeControllerTests
{
    private readonly Mock<ICommunityService> _service = new();

    // A real in-memory cache is simpler and more reliable than a mocked one
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private HomeController CreateController()
    {
        return new HomeController(_service.Object, _cache, NullLogger<HomeController>.Instance);
    }

    private static List<BlogListViewModel> SampleBlogs()
    {
        return new List<BlogListViewModel>
        {
            new BlogListViewModel { BlogId = 1, Title = "First Blog", AuthorEmail = "a@test.com" },
            new BlogListViewModel { BlogId = 2, Title = "Second Blog", AuthorEmail = "b@test.com" }
        };
    }

    [Fact]
    public async Task Index_WhenCacheIsEmpty_LoadsBlogsFromServiceAndShowsThem()
    {
        var blogs = SampleBlogs();
        _service.Setup(s => s.GetAllBlogsAsync()).ReturnsAsync(blogs);

        var result = await CreateController().Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<BlogListViewModel>>(view.Model);
        Assert.Equal(2, model.Count);
        _service.Verify(s => s.GetAllBlogsAsync(), Times.Once());
    }

    [Fact]
    public async Task Index_WhenCacheIsEmpty_StoresBlogsInCache()
    {
        var blogs = SampleBlogs();
        _service.Setup(s => s.GetAllBlogsAsync()).ReturnsAsync(blogs);

        await CreateController().Index();

        Assert.True(_cache.TryGetValue(CacheKeys.BlogList, out List<BlogListViewModel>? cached));
        Assert.Same(blogs, cached);
    }

    [Fact]
    public async Task Index_WhenCacheHasBlogs_DoesNotCallService()
    {
        var blogs = SampleBlogs();
        _cache.Set(CacheKeys.BlogList, blogs);

        var result = await CreateController().Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(blogs, view.Model);
        _service.Verify(s => s.GetAllBlogsAsync(), Times.Never());
    }

    [Fact]
    public async Task Index_CalledTwice_OnlyQueriesServiceOnce()
    {
        _service.Setup(s => s.GetAllBlogsAsync()).ReturnsAsync(SampleBlogs());
        var controller = CreateController();

        await controller.Index();
        await controller.Index();

        _service.Verify(s => s.GetAllBlogsAsync(), Times.Once());
    }

    [Fact]
    public async Task Index_WhenThereAreNoBlogs_ShowsEmptyList()
    {
        _service.Setup(s => s.GetAllBlogsAsync()).ReturnsAsync(new List<BlogListViewModel>());

        var result = await CreateController().Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<BlogListViewModel>>(view.Model);
        Assert.Empty(model);
    }

    [Fact]
    public async Task Index_WhenServiceThrows_LetsExceptionReachErrorMiddleware()
    {
        _service.Setup(s => s.GetAllBlogsAsync()).ThrowsAsync(new InvalidOperationException("Database down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateController().Index());

        // Nothing should be cached when loading failed
        Assert.False(_cache.TryGetValue(CacheKeys.BlogList, out _));
    }
}