using System.Collections.Concurrent;
using AtlasBlog.Models.EF;
using AtlasBlog.Models.Views;

namespace AtlasBlog.Services;

/// <summary>
/// Databse operations for authors managing their own blog and posts.
/// Every method takes the author's user ID, so an author can only
/// ever read or change their own blog.
/// <summary>
public interface IAuthorService
{
    // Gets the author's blog with its posts, or null if they have no blog.
    Task<Blog?> GetBlogWithPostsAsync(string authorId);

    // Gets the author's blog details for the edit form, or null if they have no blog.
    Task<BlogEditViewModel?> GetBlogForEditAsync(string authorId);

    // Saves new blog details. Returns false if the author has no blog.
    Task<bool> UpdateBlogAsync(string authorId, BlogEditViewModel model);

    // Gets all posts in the author's blog (newest first), or null if they have no blog.
    Task<List<Post>?> GetPostsAsync(string authorId);

    // Adds a new post to the author's blog. Returns false if they have no blog,
    Task<bool> CreatePostAsync(string authorId, PostEditViewModel model);

    // Gets a post for the edit form, or null if it doesn't exist
    // or belongs to someone else's blog.
    Task<PostEditViewModel?> GetPostForEditAsync(string authorId, int postId);

    // Saves changes to a post. Returns false if it doesn't exists
    // or belongs to someone else's blog.
    Task<bool> UpdatePostAsync(string authorId, int postId, PostEditViewModel model);
}