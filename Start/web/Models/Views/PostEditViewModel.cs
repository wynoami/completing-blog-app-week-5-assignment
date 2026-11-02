using System.ComponentModel.DataAnnotations;

namespace AtlasBlog.Models.Views;

public class PostEditViewModel
{
    public int? PostId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;
}
