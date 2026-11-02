namespace AtlasBlog.Models.Views;

public class BlogPostsViewModel
{
    public int BlogId { get; set; }
    public string BlogTitle { get; set; } = string.Empty;
    public string? BlogSubtitle { get; set; }
    public List<PostSummaryViewModel> Posts { get; set; } = new();
}
