using AtlasBlog.Models.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtlasBlog.Controllers;

[Authorize(Policy = "AuthorRequired")]
public class BlogController : Controller
{
    [HttpGet]
    public async Task<IActionResult> MyBlog()
    {
        // TODO: Retrieve the currently logged-in user
        // TODO: If no user is logged in, return a challenge response
        // TODO: Fetch the user's blog along with its related posts from the database
        // TODO: If no blog is found, return a NotFound response
        // TODO: Return the blog data to the view

        return View(); // Placeholder response
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        // TODO: Retrieve the currently logged-in user
        // TODO: If no user is logged in, return a challenge response
        // TODO: Fetch the user's blog from the database
        // TODO: If no blog is found, return a NotFound response
        // TODO: Create and populate a view model with the blog details
        // TODO: Return the view model to the view

        return View(); // Placeholder response
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BlogEditViewModel model)
    {
        // TODO: Validate the model state
        // TODO: Retrieve the currently logged-in user
        // TODO: If no user is logged in, return a challenge response
        // TODO: Fetch the user's blog from the database
        // TODO: If no blog is found, return a NotFound response
        // TODO: Update the blog properties with the new values from the view model
        // TODO: Update the LastModifiedAt timestamp
        // TODO: Save the changes to the database
        // TODO: Store a success message in TempData
        // TODO: Redirect to the MyBlog page

        return View(model); // Placeholder response
    }
}
