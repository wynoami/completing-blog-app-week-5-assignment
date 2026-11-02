using System.ComponentModel.DataAnnotations;

namespace AtlasBlog.Models.Views;

public class BlogEditViewModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Subtitle { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }
}
