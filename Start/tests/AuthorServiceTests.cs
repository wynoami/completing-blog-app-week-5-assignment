using AtlasBlog.Models.Views;
using AtlasBlog.Services;

namespace AtlasBlog.Tests;

/// <summary>
/// Tests the real AuthorService against an in-memory SQLite database,
/// with special attention to authors only ever reaching their own blog.
/// </summary>
public class AuthorServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public AuthorServiceTests()
    {
        using var context = _database.CreateContext();
        TestData.Seed(context);
    }

    public void Dispose()
    {
        _database.Dispose();
    }

    private AuthorService CreateService()
    {
        return new AuthorService(_database.CreateContext());
    }

    // ---------- GetBlogWithPostsAsync ----------

    [Fact]
    public async Task GetBlogWithPostsAsync_ReturnsAuthorsOwnBlogWithItsPosts()
    {
        var blog = await CreateService().GetBlogWithPostsAsync(TestData.AliceId);

        Assert.NotNull(blog);
        Assert.Equal(TestData.AliceBlogId, blog!.BlogId);
        Assert.Equal(TestData.AlicePostCount, blog.Posts.Count);
    }

    [Fact]
    public async Task GetBlogWithPostsAsync_WhenUserHasNoBlog_ReturnsNull()
    {
        var blog = await CreateService().GetBlogWithPostsAsync(TestData.ReaderId);

        Assert.Null(blog);
    }

    // ---------- GetBlogForEditAsync ----------

    [Fact]
    public async Task GetBlogForEditAsync_ReturnsCurrentDetails()
    {
        var model = await CreateService().GetBlogForEditAsync(TestData.AliceId);

        Assert.NotNull(model);
        Assert.Equal("Zebra Notes", model!.Title);
        Assert.Equal("Alice writes", model.Subtitle);
        Assert.Equal("About zebras", model.Description);
    }

    [Fact]
    public async Task GetBlogForEditAsync_WhenUserHasNoBlog_ReturnsNull()
    {
        var model = await CreateService().GetBlogForEditAsync(TestData.ReaderId);

        Assert.Null(model);
    }

    // ---------- UpdateBlogAsync ----------

    [Fact]
    public async Task UpdateBlogAsync_SavesTrimmedDetailsAndUpdatesTimestamp()
    {
        var model = new BlogEditViewModel { Title = "  New Title  ", Subtitle = " New Sub ", Description = " New description " };

        var updated = await CreateService().UpdateBlogAsync(TestData.AliceId, model);

        Assert.True(updated);
        using var context = _database.CreateContext();
        var blog = context.Blogs.Single(b => b.BlogId == TestData.AliceBlogId);
        Assert.Equal("New Title", blog.Title);
        Assert.Equal("New Sub", blog.Subtitle);
        Assert.Equal("New description", blog.Description);
        Assert.True(blog.LastModifiedAt > TestData.Start);
    }

    [Fact]
    public async Task UpdateBlogAsync_NeverChangesSomeoneElsesBlog()
    {
        await CreateService().UpdateBlogAsync(TestData.AliceId, new BlogEditViewModel { Title = "Changed" });

        using var context = _database.CreateContext();
        Assert.Equal("Apple Thoughts", context.Blogs.Single(b => b.BlogId == TestData.BobBlogId).Title);
    }

    [Fact]
    public async Task UpdateBlogAsync_WhenUserHasNoBlog_ReturnsFalse()
    {
        var updated = await CreateService().UpdateBlogAsync(TestData.ReaderId, new BlogEditViewModel { Title = "Sneaky" });

        Assert.False(updated);
    }

    // ---------- GetPostsAsync ----------

    [Fact]
    public async Task GetPostsAsync_ReturnsOnlyTheAuthorsOwnPosts_NewestFirst()
    {
        var posts = await CreateService().GetPostsAsync(TestData.AliceId);

        Assert.NotNull(posts);
        Assert.Equal(TestData.AlicePostCount, posts!.Count);
        Assert.All(posts, p => Assert.Equal(TestData.AliceBlogId, p.BlogId));
        Assert.Equal(TestData.AliceNewestPostId, posts.First().PostId);
    }

    [Fact]
    public async Task GetPostsAsync_WhenUserHasNoBlog_ReturnsNull()
    {
        var posts = await CreateService().GetPostsAsync(TestData.ReaderId);

        Assert.Null(posts);
    }

    // ---------- CreatePostAsync ----------

    [Fact]
    public async Task CreatePostAsync_AddsPostToTheAuthorsOwnBlog()
    {
        var model = new PostEditViewModel { Title = "  Brand New  ", Content = "<p>Fresh</p>" };

        var created = await CreateService().CreatePostAsync(TestData.BobId, model);

        Assert.True(created);
        using var context = _database.CreateContext();
        var post = context.Posts.Single(p => p.Title == "Brand New");
        Assert.Equal(TestData.BobBlogId, post.BlogId);
        Assert.Equal("<p>Fresh</p>", post.Content);
    }

    [Fact]
    public async Task CreatePostAsync_GivesNewPostMatchingCreatedAndModifiedTimes()
    {
        await CreateService().CreatePostAsync(TestData.BobId, new PostEditViewModel { Title = "Timed", Content = "x" });

        using var context = _database.CreateContext();
        var post = context.Posts.Single(p => p.Title == "Timed");
        Assert.Equal(post.CreatedAt, post.LastModifiedAt);
    }

    [Fact]
    public async Task CreatePostAsync_WhenUserHasNoBlog_ReturnsFalseAndSavesNothing()
    {
        var created = await CreateService().CreatePostAsync(TestData.ReaderId, new PostEditViewModel { Title = "Nope", Content = "x" });

        Assert.False(created);
        using var context = _database.CreateContext();
        Assert.Equal(TestData.AlicePostCount + 1, context.Posts.Count());
    }

    // ---------- GetPostForEditAsync ----------

    [Fact]
    public async Task GetPostForEditAsync_ForOwnPost_ReturnsIt()
    {
        var model = await CreateService().GetPostForEditAsync(TestData.AliceId, TestData.AliceNewestPostId);

        Assert.NotNull(model);
        Assert.Equal(TestData.AliceNewestPostId, model!.PostId);
        Assert.Equal("Alice Post 7", model.Title);
    }

    [Fact]
    public async Task GetPostForEditAsync_ForSomeoneElsesPost_ReturnsNull()
    {
        // Bob tries to open Alice's post
        var model = await CreateService().GetPostForEditAsync(TestData.BobId, TestData.AliceNewestPostId);

        Assert.Null(model);
    }

    [Fact]
    public async Task GetPostForEditAsync_WhenPostDoesNotExist_ReturnsNull()
    {
        var model = await CreateService().GetPostForEditAsync(TestData.AliceId, 999);

        Assert.Null(model);
    }

    // ---------- UpdatePostAsync ----------

    [Fact]
    public async Task UpdatePostAsync_ForOwnPost_SavesChangesAndUpdatesTimestamp()
    {
        var model = new PostEditViewModel { Title = " Edited ", Content = "<p>Edited</p>" };

        var updated = await CreateService().UpdatePostAsync(TestData.AliceId, TestData.AliceNewestPostId, model);

        Assert.True(updated);
        using var context = _database.CreateContext();
        var post = context.Posts.Single(p => p.PostId == TestData.AliceNewestPostId);
        Assert.Equal("Edited", post.Title);
        Assert.Equal("<p>Edited</p>", post.Content);
        Assert.True(post.LastModifiedAt > post.CreatedAt);
    }

    [Fact]
    public async Task UpdatePostAsync_ForSomeoneElsesPost_ReturnsFalseAndChangesNothing()
    {
        // Bob tries to edit Alice's post
        var updated = await CreateService().UpdatePostAsync(
            TestData.BobId, TestData.AliceNewestPostId, new PostEditViewModel { Title = "Hacked", Content = "x" });

        Assert.False(updated);
        using var context = _database.CreateContext();
        Assert.Equal("Alice Post 7", context.Posts.Single(p => p.PostId == TestData.AliceNewestPostId).Title);
    }

    [Fact]
    public async Task UpdatePostAsync_WhenPostDoesNotExist_ReturnsFalse()
    {
        var updated = await CreateService().UpdatePostAsync(
            TestData.AliceId, 999, new PostEditViewModel { Title = "Ghost", Content = "x" });

        Assert.False(updated);
    }
}