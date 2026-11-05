using AtlasBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AtlasBlog.Models.Views;
using System.Security.Claims;

namespace AtlasBlog.Controllers;

/// <summary>
/// Lets an author list, create, and edit posts in their own blog. Authors only.
/// </summary>

[Authorize(Policy = "AuthorRequired")]
public class PostController : Controller
{
    private readonly IAuthorService _authorService;

    public PostController(IAuthorService authorservice)
    {
        _authorService = authorservice;
    }

    // The logged-in user's ID, read from their login cookie.
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // Lists every post in the author's blog, newest first.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;
        if (userId is null)
        {
            return Challenge();
        }

        var posts = await _authorService.GetPostsAsync(userId);
        if (posts is null)
        {
            return NotFound();
        }

        return View(posts);
    }

    // Shows an empty form for a new post.
    // GET: Post/Create
    [HttpGet]
    public IActionResult Create()
    {

        return View(new PostEditViewModel());
    }

    // Saves a new post to the author's blog, then returns to the post list.
    // POST: Post/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PostEditViewModel model)
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

        var created = await _authorService.CreatePostAsync(userId, model);
        if (!created)
        {
            return NotFound();
        }

        TempData["Message"] = "Your post was published.";
        return RedirectToAction(nameof(Index));
    }

    // Shows the edit form for one of the author's own posts.
    // GET: Post/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = CurrentUserId;
        if (userId is null)
        {
            return Challenge();
        }

        // Returns null if the post doesn't exist OR belongs to another author.
        var model = await _authorService.GetPostForEditAsync(userId, id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    // Saves changes to one of the author's own posts, then returns to the post list.
    // POST Post/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PostEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // The form builds its address from PostId, so put it back
            // before showing the form again with error messages
            model.PostId = id;
            return View(model);
        }

        var userId = CurrentUserId;
        if (userId is null)
        {
            return Challenge();
        }

        var updated = await _authorService.UpdatePostAsync(userId, id, model);
        if (!updated)
        {
            return NotFound();
        }

        TempData["Message"] = "Your post was updated.";
        return RedirectToAction(nameof(Index));
    }
}
