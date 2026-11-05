using AtlasBlog.Data;
using AtlasBlog.Models.EF;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AtlasBlog.Services;
using Microsoft.AspNetCore.Authorization;
using AtlasBlog.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// our own services. "Scoped" = one instnace per web request,
// matching the lifetime of ApplicationDbContext.
builder.Services.AddScoped<ICommunityService, CommunityService>();
builder.Services.AddScoped<IAuthorService, AuthorService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Tell Identity where to send people who aren't logged in,
// or who are logged in but lack the right claim.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AuthorRequired", policy => policy.RequireClaim("author", "true"));
    options.AddPolicy("ModeratorRequired", policy => policy.RequireClaim("moderator", "true"));
    options.AddPolicy("AdminRequired", policy => policy.RequireClaim("admin", "true"));
    options.AddPolicy("ModeratorOrAdmin", policy =>
    policy.RequireAssertion(context =>
        context.User.HasClaim("moderator", "true") ||
        context.User.HasClaim("admin", "true")));
});

var app = builder.Build();
// Our custom middleware goes first so it wraps everything after it:
// the logger sees every request and its final status code, and the
// exception handler catches errors thrown anywhere further down.
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();   // Who are you? Reads the login cookie and fills in User
app.UseAuthorization();    // Are you allowed? Checks [Authorize] attributes and policies

app.MapDefaultControllerRoute();

app.Run();