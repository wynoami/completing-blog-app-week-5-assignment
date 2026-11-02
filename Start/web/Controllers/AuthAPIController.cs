using AtlasBlog.Data;
using AtlasBlog.Models.EF;
using AtlasBlog.Models.API;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AtlasBlog.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthAPIController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;

    public AuthAPIController(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _configuration = configuration;
        _context = context;
    }

    private bool ValidateApiKey(string apiKey)
    {
        var validApiKey = _configuration["ApiKey"];
        return !string.IsNullOrEmpty(validApiKey) && validApiKey.Equals(apiKey);
    }

    [HttpPost("registeradmin")]
    public async Task<IActionResult> RegisterAdmin(RegisterAPIUserRequest model)
    {
        if (!ValidateApiKey(model.ApiKey))
            return Unauthorized("Invalid API key");

        try
        {
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("admin", "true"));
                return Ok("Admin user created successfully");
            }

            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error creating admin user: " + ex.Message);
        }
    }

    [HttpPost("registerauthor")]
    public async Task<IActionResult> RegisterAuthor(RegisterAPIAuthorRequest model)
    {
        if (!ValidateApiKey(model.ApiKey))
            return Unauthorized("Invalid API key");

        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("author", "true"));

                var blog = new Blog
                {
                    Title = model.BlogTitle,
                    Subtitle = model.BlogSubtitle,
                    Description = model.BlogDescription,
                    CreatedAt = DateTime.UtcNow,
                    LastModifiedAt = DateTime.UtcNow,
                    AuthorId = user.Id
                };

                _context.Blogs.Add(blog);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok("Author user and blog created successfully");
            }

            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error creating author user: " + ex.Message);
        }
    }

    [HttpPost("registermoderator")]
    public async Task<IActionResult> RegisterModerator(RegisterAPIUserRequest model)
    {
        if (!ValidateApiKey(model.ApiKey))
            return Unauthorized("Invalid API key");

        try
        {
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("moderator", "true"));
                return Ok("Moderator user created successfully");
            }

            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error creating moderator user: " + ex.Message);
        }
    }
}
