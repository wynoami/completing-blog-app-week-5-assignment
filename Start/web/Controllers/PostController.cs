using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AtlasBlog.Models.Views;

namespace AtlasBlog.Controllers;

[Authorize(Policy = "AuthorRequired")]
public class PostController : Controller
{
    public async Task<IActionResult> Index()
    {
        // TODO: Get the currently logged-in user
        // TODO: If user is not found, return Challenge()
        // TODO: Fetch posts associated with the user's blog, ordered by creation date
        // TODO: Return the posts to the view

        return View(); // Placeholder response
    }

    [HttpGet]
    public IActionResult Create()
    {
        // TODO: Return an empty PostEditViewModel to the view

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PostEditViewModel model)
    {
        // TODO: Validate the model state
        // TODO: Get the currently logged-in user
        // TODO: If user is not found, return Challenge()
        // TODO: Fetch the user's blog
        // TODO: If blog is not found, return NotFound
        // TODO: Create a new Post entity and save it
        // TODO: Show a success message and redirect to Index

        return View(model); // Placeholder response
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        // TODO: Get the currently logged-in user
        // TODO: If user is not found, return Challenge()
        // TODO: Fetch the post and ensure it belongs to the user's blog
        // TODO: If post is not found, return NotFound
        // TODO: Map the post details to a PostEditViewModel
        // TODO: Return the view with the populated model

        return View(); // Placeholder response
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PostEditViewModel model)
    {
        // TODO: Validate the model state
        // TODO: Get the currently logged-in user
        // TODO: If user is not found, return Challenge()
        // TODO: Fetch the post and ensure it belongs to the user's blog
        // TODO: If post is not found, return NotFound
        // TODO: Update the post details and save changes
        // TODO: Show a success message and redirect to Index

        return View(model); // Placeholder response
    }
}
