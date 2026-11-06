using AtlasBlog.Models.Views;
using AtlasBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AtlasBlog.Controllers;

/// <summary>
/// The public reading side of the site: blog pages, post pages,
/// and comments. Anyone can read; commenting and moderating need a login.
/// </summary>
public class CommunityController : Controller
{
    private readonly ICommunityService _communityService;

    public CommunityController(ICommunityService communityService)
    {
        _communityService = communityService;
    }

    /// <summary>Shows a blog's details and its 5 most recent posts.</summary>
    // GET: Community/ViewBlog/5
    [HttpGet]
    public async Task<IActionResult> ViewBlog(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var blog = await _communityService.GetBlogDetailAsync(id.Value);
        if (blog is null)
        {
            return NotFound();
        }

        return View(blog);
    }

    /// <summary>
    /// Shows a full post with its comments. Moderators and admins also
    /// see hidden comments so they can un-hide them.
    /// </summary>
    // GET: Community/ViewPost/5
    [HttpGet]
    public async Task<IActionResult> ViewPost(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var canModerate = User.HasClaim("moderator", "true") || User.HasClaim("admin", "true");

        var post = await _communityService.GetPostDetailAsync(id.Value, includeHiddenComments: canModerate);
        if (post is null)
        {
            return NotFound();
        }

        return View(post);
    }

    /// <summary>Adds a comment from the logged-in user, then returns to the post.</summary>
    // POST: Community/AddComment
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> AddComment(CreateCommentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Message"] = "Comments must be between 1 and 1000 characters.";
            return RedirectToAction(nameof(ViewPost), new { id = model.PostId });
        }

        // The logged-in user's ID is stored in their login cookie as a claim
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var added = await _communityService.AddCommentAsync(model.PostId, userId, model.Content);
        if (!added)
        {
            return NotFound();
        }

        TempData["Message"] = "Your comment was added.";
        return RedirectToAction(nameof(ViewPost), new { id = model.PostId });
    }

    /// <summary>Hides a visible comment or shows a hidden one. Moderators and admins only.</summary>
    // POST: Community/ToggleCommentVisibility
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ModeratorOrAdmin")]
    public async Task<IActionResult> ToggleCommentVisibility(int commentId, int postId)
    {
        var toggled = await _communityService.ToggleCommentVisibilityAsync(commentId);
        if (!toggled)
        {
            return NotFound();
        }

        TempData["Message"] = "Comment visibility updated.";
        return RedirectToAction(nameof(ViewPost), new { id = postId });
    }

    /// <summary>Shows every post in a blog, newest first.</summary>
    // GET: Community/ViewAllPosts?blogId=5
    [HttpGet]
    public async Task<IActionResult> ViewAllPosts(int? blogId)
    {
        if (blogId is null)
        {
            return NotFound();
        }

        var blogPosts = await _communityService.GetBlogPostsAsync(blogId.Value);
        if (blogPosts is null)
        {
            return NotFound();
        }

        return View(blogPosts);
    }
}