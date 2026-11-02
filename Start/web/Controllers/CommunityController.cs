using AtlasBlog.Models.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtlasBlog.Controllers;

public class CommunityController : Controller
{
    // GET: Community/ViewBlog/5
    public async Task<IActionResult> ViewBlog(int? id)
    {
        // TODO: Check if the provided blog ID is null, return NotFound if true
        // TODO: Fetch the blog from the database, including its author and latest 5 posts
        // TODO: If no blog is found, return NotFound
        // TODO: Map the blog data to a view model
        // TODO: Return the blog view model to the view

        return View(); // Placeholder response
    }

    // GET: Community/ViewPost/5
    public async Task<IActionResult> ViewPost(int? id)
    {
        // TODO: Check if the provided post ID is null, return NotFound if true
        // TODO: Fetch the post, including its blog and related comments (only visible ones unless user is moderator/admin)
        // TODO: If no post is found, return NotFound
        // TODO: Map the post data to a view model
        // TODO: Return the post view model to the view

        return View(); // Placeholder response
    }

    // POST: Community/AddComment
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> AddComment(CreateCommentViewModel model)
    {
        // TODO: Validate the model state
        // TODO: Create a new comment entity using the provided model data
        // TODO: Set the current timestamp and associate the comment with the logged-in user
        // TODO: Save the comment to the database
        // TODO: Redirect back to the ViewPost page

        return RedirectToAction(nameof(ViewPost), new { id = model.PostId }); // Placeholder response
    }

    // POST: Community/ToggleCommentVisibility
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ModeratorOrAdmin")]
    public async Task<IActionResult> ToggleCommentVisibility(int commentId, int postId)
    {
        // TODO: Fetch the comment by ID from the database
        // TODO: If the comment is not found, return NotFound
        // TODO: Toggle the visibility of the comment
        // TODO: Save the changes to the database
        // TODO: Redirect back to the ViewPost page

        return RedirectToAction(nameof(ViewPost), new { id = postId }); // Placeholder response
    }

    // GET: Community/ViewAllPosts/5
    public async Task<IActionResult> ViewAllPosts(int? blogId)
    {
        // TODO: Check if the provided blog ID is null, return NotFound if true
        // TODO: Fetch the blog and its posts, ordered by CreatedAt descending
        // TODO: If no blog is found, return NotFound
        // TODO: Map the blog and its posts to a view model
        // TODO: Return the blog posts view model to the view

        return View(); // Placeholder response
    }
}
