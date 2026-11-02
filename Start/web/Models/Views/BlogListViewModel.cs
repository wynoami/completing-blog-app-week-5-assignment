namespace AtlasBlog.Models.Views;

public class BlogListViewModel
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string AuthorEmail { get; set; } = string.Empty;
    public int BlogId { get; set; }
}
