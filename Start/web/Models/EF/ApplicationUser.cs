using Microsoft.AspNetCore.Identity;

namespace AtlasBlog.Models.EF;

public class ApplicationUser : IdentityUser
{
    public virtual Blog? Blog { get; set; }
}
