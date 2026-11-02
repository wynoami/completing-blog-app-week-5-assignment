namespace AtlasBlog.Models.Views;

public class BlogDetailViewModel
{
    public int BlogId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string AuthorEmail { get; set; } = string.Empty;
    public List<PostSummaryViewModel> RecentPosts { get; set; } = new();
}
