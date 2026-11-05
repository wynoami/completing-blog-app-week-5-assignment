using AtlasBlog.Models.Views;
using AtlasBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace AtlasBlog.Controllers;

/// <summary>
/// Lets an author view and edit their own blog. Authors only.
/// </summary>

[Authorize(Policy = "AuthorRequired")]
public class BlogController : Controller
{
    private readonly IAuthorService _authorService;
    private readonly IMemoryCache _cache;

    public BlogController(IAuthorService authorService, IMemoryCache cache)
    {
        _authorService = authorService;
        _cache = cache;
    }

    // The logged-in user's ID, read from their login cookie.
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // Shows the author's blog and its most recent posts.
    // GET: Blog/MyBlog
    [HttpGet]
    public async Task<IActionResult> MyBlog()
    {
        var userId = CurrentUserId;
        if (userId is null)
        {
            return Challenge();
        }

        var blog = await _authorService.GetBlogWithPostsAsync(userId);
        if (blog is null)
        {
            return NotFound();
        }

        return View(blog);
    }

    // Shows the form for editing the blog's title, subtitle, and description.
    // GET: Blog/Edit
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = CurrentUserId;
        if (userId is null)
        {
            return Challenge();
        }

        var model = await _authorService.GetBlogForEditAsync(userId);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    // Saves the blog's new details, then returns to My Blog.
    // POST: Blog/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BlogEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = CurrentUserId;
        if (userId is null)
        {
            return Challenge();
        }

        var updated = await _authorService.UpdateBlogAsync(userId, model);
        if (!updated)
        {
            return NotFound();
        }

        // The home page shows blog titles, so its cached list is now out of date
        _cache.Remove(CacheKeys.BlogList);

        TempData["Message"] = "Your blog was updated.";

        return RedirectToAction(nameof(MyBlog));
    }
}
