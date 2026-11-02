namespace AtlasBlog.Models.Views;

public class PostDetailViewModel
{
    public int PostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public string BlogTitle { get; set; } = string.Empty;
    public int BlogId { get; set; }
    public List<CommentViewModel> Comments { get; set; } = new();
}

public class CommentViewModel
{
    public int CommentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public bool IsVisible { get; set; }
}