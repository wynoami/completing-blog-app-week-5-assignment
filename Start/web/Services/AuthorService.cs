using AtlasBlog.Data;
using AtlasBlog.Models.EF;
using AtlasBlog.Models.Views;
using Microsoft.EntityFrameworkCore;

namespace AtlasBlog.Services;

/// <summary>
/// Entity Framework implementation of <see cref="IAuthorService"/>.
/// </summary>
public class AuthorService : IAuthorService
{
    private readonly ApplicationDbContext _context;

    public AuthorService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Blog?> GetBlogWithPostsAsync(string authorId)
    {
        return await _context.Blogs
            .AsNoTracking()
            .Include(b => b.Posts)
            .FirstOrDefaultAsync(b => b.AuthorId == authorId);
    }

    /// <inheritdoc />
    public async Task<BlogEditViewModel?> GetBlogForEditAsync(string authorId)
    {
        return await _context.Blogs
            .AsNoTracking()
            .Where(b => b.AuthorId == authorId)
            .Select(b => new BlogEditViewModel
            {
                Title = b.Title,
                Subtitle = b.Subtitle,
                Description = b.Description
            })
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<bool> UpdateBlogAsync(string authorId, BlogEditViewModel model)
    {
        var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.AuthorId == authorId);
        if (blog is null)
        {
            return false;
        }

        blog.Title = model.Title.Trim();
        blog.Subtitle = model.Subtitle?.Trim();
        blog.Description = model.Description?.Trim();
        blog.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<List<Post>?> GetPostsAsync(string authorId)
    {
        var blogId = await _context.Blogs
            .Where(b => b.AuthorId == authorId)
            .Select(b => (int?)b.BlogId)
            .FirstOrDefaultAsync();

        if (blogId is null)
        {
            return null;
        }

        return await _context.Posts
            .AsNoTracking()
            .Where(p => p.BlogId == blogId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> CreatePostAsync(string authorId, PostEditViewModel model)
    {
        var blogId = await _context.Blogs
            .Where(b => b.AuthorId == authorId)
            .Select(b => (int?)b.BlogId)
            .FirstOrDefaultAsync();

        if (blogId is null)
        {
            return false;
        }

        // Same timestamp for both, so the post page doesn't show
        // "Last modified" on a post that was never edited
        var now = DateTime.UtcNow;

        _context.Posts.Add(new Post
        {
            BlogId = blogId.Value,
            Title = model.Title.Trim(),
            Content = model.Content,
            CreatedAt = now,
            LastModifiedAt = now
        });

        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<PostEditViewModel?> GetPostForEditAsync(string authorId, int postId)
    {
        return await _context.Posts
            .AsNoTracking()
            .Where(p => p.PostId == postId && p.Blog.AuthorId == authorId)
            .Select(p => new PostEditViewModel
            {
                PostId = p.PostId,
                Title = p.Title,
                Content = p.Content
            })
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<bool> UpdatePostAsync(string authorId, int postId, PostEditViewModel model)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.PostId == postId && p.Blog.AuthorId == authorId);

        if (post is null)
        {
            return false;
        }

        post.Title = model.Title.Trim();
        post.Content = model.Content;
        post.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}