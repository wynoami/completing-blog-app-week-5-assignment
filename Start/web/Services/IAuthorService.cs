using AtlasBlog.Models.EF;
using AtlasBlog.Models.Views;

namespace AtlasBlog.Services;

/// <summary>
/// Database operations for authors managing their own blog and posts.
/// Every method takes the author's user ID, so an author can only
/// ever read or change their own blog.
/// </summary>
public interface IAuthorService
{
    /// <summary>Gets the author's blog with its posts, or null if they have no blog.</summary>
    Task<Blog?> GetBlogWithPostsAsync(string authorId);

    /// <summary>Gets the author's blog details for the edit form, or null if they have no blog.</summary>
    Task<BlogEditViewModel?> GetBlogForEditAsync(string authorId);

    /// <summary>Saves new blog details. Returns false if the author has no blog.</summary>
    Task<bool> UpdateBlogAsync(string authorId, BlogEditViewModel model);

    /// <summary>Gets all posts in the author's blog (newest first), or null if they have no blog.</summary>
    Task<List<Post>?> GetPostsAsync(string authorId);

    /// <summary>Adds a new post to the author's blog. Returns false if they have no blog.</summary>
    Task<bool> CreatePostAsync(string authorId, PostEditViewModel model);

    /// <summary>
    /// Gets a post for the edit form, or null if it doesn't exist
    /// or belongs to someone else's blog.
    /// </summary>
    Task<PostEditViewModel?> GetPostForEditAsync(string authorId, int postId);

    /// <summary>
    /// Saves changes to a post. Returns false if it doesn't exist
    /// or belongs to someone else's blog.
    /// </summary>
    Task<bool> UpdatePostAsync(string authorId, int postId, PostEditViewModel model);
}