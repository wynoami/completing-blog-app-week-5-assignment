using AtlasBlog.Models.Views;

namespace AtlasBlog.Services;

/// <summary>
/// Database operations for the public side of the site:
/// the home page, blog pages, post pages, and comments.
/// </summary>
public interface ICommunityService
{
    /// <summary>Gets every blog with its author's email, sorted by title.</summary>
    Task<List<BlogListViewModel>> GetAllBlogsAsync();

    /// <summary>Gets one blog with its 5 newest posts, or null if the blog doesn't exist.</summary>
    Task<BlogDetailViewModel?> GetBlogDetailAsync(int blogId);

    /// <summary>Gets one blog with all of its posts (newest first), or null if the blog doesn't exist.</summary>
    Task<BlogPostsViewModel?> GetBlogPostsAsync(int blogId);

    /// <summary>
    /// Gets one post with its comments (oldest first), or null if the post doesn't exist.
    /// Hidden comments are included only when <paramref name="includeHiddenComments"/> is true.
    /// </summary>
    Task<PostDetailViewModel?> GetPostDetailAsync(int postId, bool includeHiddenComments);

    /// <summary>Adds a comment to a post. Returns false if the post doesn't exist.</summary>
    Task<bool> AddCommentAsync(int postId, string userId, string content);

    /// <summary>Flips a comment between visible and hidden. Returns false if the comment doesn't exist.</summary>
    Task<bool> ToggleCommentVisibilityAsync(int commentId);
}