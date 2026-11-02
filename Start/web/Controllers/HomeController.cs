using AtlasBlog.Models.Views;
using Microsoft.AspNetCore.Mvc;

namespace AtlasBlog.Controllers;

public class HomeController : Controller
{

    public async Task<IActionResult> Index()
    {
        // TODO: Try retrieving the blog list from the cache
        // TODO: If cache is empty, fetch blogs from the database (including author information)
        // TODO: Map the blog data to a view model
        // TODO: Store the result in the cache with a 30-minute expiration
        // TODO: Return the blog list view

        // Placeholder data used temporarily for application response; will be replaced with actual data.
        return View(new List<BlogListViewModel>
        {
            new BlogListViewModel
            {
                 AuthorEmail= "author @example.com",
                BlogId=1,
                Title= "World of Code",
                Subtitle = "My bits and bytes"
            }

        });
    }
}



