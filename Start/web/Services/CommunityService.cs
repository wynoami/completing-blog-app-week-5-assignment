using System.Reflection;
using AtlasBlog.Data;
using AtlasBlog.Models.Views;
using Microsoft.EntityFrameworkCore;

namespace AtlasBlog.Services;

// Entity Framework implementation of <see cref="ICommunityService"/>
public class CommunityService : ICommunityService
{
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
}