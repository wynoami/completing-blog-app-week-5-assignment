using System.Net.Mime;
using System.Reflection;
using System.Transactions;
using AtlasBlog.Data;
using AtlasBlog.Models.EF;
using AtlasBlog.Models.Views;
using Microsoft.EntityFrameworkCore;

namespace AtlasBlog.Services;

// Entity Framework implementation of <see cref="ICommunityService"/>
public class CommunityService : ICommunityService
{   
    private const int RecentPostCount = 5;
    private readonly ApplicationDbContext _context;

    public CommunityService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<BlogListViewModel>> GetAllBlogsAsync()
    {
        return await _context.Blogs
            .AsNoTracking()
            .OrderBy(b => b.Title)
            .Select(b => new BlogListViewModel
            {
                BlogId = b.BlogId,
                Title = b.Title,
                Subtitle = b.Subtitle,
                AuthorEmail = b.Author.Email ?? string.Empty
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<BlogDetailViewModel?> GetBlogDetailAsync(int blogId)
    {
        return await _context.Blogs
            .AsNoTracking()
            .Where(b => b.BlogId == blogId)
            .Select(b => new BlogDetailViewModel
            {
                BlogId = b.BlogId,
                Title = b.Title,
                Subtitle = b.Subtitle,
                Description = b.Description,
                AuthorEmail = b.Author.Email ?? string.Empty,
                RecentPosts = b.Posts
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(RecentPostCount)
                    .Select(p => new PostSummaryViewModel
                    {
                        PostId = p.PostId,
                        Title = p.Title,
                        CreatedAt = p.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<BlogPostsViewModel?> GetBlogPostsAsync(int blogId)
    {
        return await _context.Blogs
            .AsNoTracking()
            .Where(b => b.BlogId == blogId)
            .Select(b => new BlogPostsViewModel
            {
                BlogId = b.BlogId,
                BlogTitle = b.Title,
                BlogSubtitle = b.Subtitle,
                Posts = b.Posts
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new PostSummaryViewModel
                    {
                        PostId = p.PostId,
                        Title = p.Title,
                        CreatedAt = p.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<PostDetailViewModel?> GetPostDetailAsync(int postId, bool includeHiddenComments)
    {
        return await _context.Posts
            .AsNoTracking()
            .Where(p => p.PostId == postId)
            .Select(p => new PostDetailViewModel
            {
                PostId = p.PostId,
                Title = p.Title,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                LastModifiedAt = p.LastModifiedAt,
                BlogId = p.BlogId,
                BlogTitle = p.Blog.Title,
                Comments = p.Comments
                    .Where(c => includeHiddenComments || c.IsVisible)
                    .OrderBy(c => c.CreatedAt)
                    .Select(c => new CommentViewModel
                    {
                        CommentId = c.CommentId,
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        UserEmail = c.User.Email ?? string.Empty,
                        IsVisible = c.IsVisible
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<bool> AddCommentAsync(int postId, string userId, string content)
    {
        var postExists = await _context.Posts.AnyAsync(p => p.PostId == postId);
        if (!postExists)
        {
            return false;
        }

        _context.Comments.Add(new Comment
        {
            PostId = postId,
            UserId = userId,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsVisible = true
        });
        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ToggleCommentVisibilityAsync(int commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment is null)
        {
            return false;
        }

        comment.IsVisible = !comment.IsVisible;
        await _context.SaveChangesAsync();
        return true;
    }
}