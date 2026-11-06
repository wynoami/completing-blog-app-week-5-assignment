using AtlasBlog.Models.Views;
using AtlasBlog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AtlasBlog.Controllers;

/// <summary>
/// Shows the home page: a list of every blog on the site.
/// </summary>
public class HomeController : Controller
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly ICommunityService _communityService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ICommunityService communityService,
        IMemoryCache cache,
        ILogger<HomeController> logger)
    {
        _communityService = communityService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Lists all blogs. The list is cached for 30 minutes so the
    /// database isn't queried on every visit.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        // Try the cache first; only go to the database if it's empty or expired
        if (!_cache.TryGetValue(CacheKeys.BlogList, out List<BlogListViewModel>? blogs) || blogs is null)
        {
            blogs = await _communityService.GetAllBlogsAsync();
            _cache.Set(CacheKeys.BlogList, blogs, CacheDuration);
            _logger.LogInformation("Blog list loaded from the database ({Count} blogs) and cached for 30 minutes.", blogs.Count);
        }

        return View(blogs);
    }
}