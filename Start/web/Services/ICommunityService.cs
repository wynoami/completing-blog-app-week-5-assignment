using AtlasBlog.Models.Views;

namespace AtlasBlog.Services;

/// <summary>
/// Database operations for the public side of the site:
/// the home page, blog pages, post pages, and comments.
/// <summary>
public interface ICommunityService
{
    // Gets every blog with its author's email, sorted by title.
    Task<List<BlogListViewModel>> GetAllBlogsAsync();
}