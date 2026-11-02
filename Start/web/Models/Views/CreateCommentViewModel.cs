using System.ComponentModel.DataAnnotations;

namespace AtlasBlog.Models.Views;

public class CreateCommentViewModel
{
    [Required]
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;
    public int PostId { get; set; }
}
