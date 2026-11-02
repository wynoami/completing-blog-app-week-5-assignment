namespace AtlasBlog.Models.API;

public class RegisterAPIUserRequest
{
    public string ApiKey { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterAPIAuthorRequest : RegisterAPIUserRequest
{
    public string BlogTitle { get; set; } = string.Empty;
    public string? BlogSubtitle { get; set; }
    public string? BlogDescription { get; set; }
}
