namespace AtlasBlog.Models.Views;

public class PostSummaryViewModel
{
    public int PostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
