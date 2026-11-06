using AtlasBlog.Services;

namespace AtlasBlog.Tests;

/// <summary>
/// Tests the real CommunityService queries against an in-memory
/// SQLite database filled with <see cref="TestData"/>.
/// </summary>
public class CommunityServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public CommunityServiceTests()
    {
        using var context = _database.CreateContext();
        TestData.Seed(context);
    }

    public void Dispose()
    {
        _database.Dispose();
    }

    private CommunityService CreateService()
    {
        return new CommunityService(_database.CreateContext());
    }

    // ---------- GetAllBlogsAsync ----------

    [Fact]
    public async Task GetAllBlogsAsync_ReturnsEveryBlogSortedByTitleWithAuthorEmail()
    {
        var blogs = await CreateService().GetAllBlogsAsync();

        Assert.Collection(blogs,
            first =>
            {
                Assert.Equal("Apple Thoughts", first.Title);
                Assert.Equal("bob@test.com", first.AuthorEmail);
            },
            second =>
            {
                Assert.Equal("Zebra Notes", second.Title);
                Assert.Equal("alice@test.com", second.AuthorEmail);
            });
    }

    [Fact]
    public async Task GetAllBlogsAsync_WhenThereAreNoBlogs_ReturnsEmptyList()
    {
        using var emptyDatabase = new TestDatabase();
        var service = new CommunityService(emptyDatabase.CreateContext());

        var blogs = await service.GetAllBlogsAsync();

        Assert.Empty(blogs);
    }

    // ---------- GetBlogDetailAsync ----------

    [Fact]
    public async Task GetBlogDetailAsync_ReturnsBlogDetails()
    {
        var blog = await CreateService().GetBlogDetailAsync(TestData.AliceBlogId);

        Assert.NotNull(blog);
        Assert.Equal("Zebra Notes", blog!.Title);
        Assert.Equal("Alice writes", blog.Subtitle);
        Assert.Equal("About zebras", blog.Description);
        Assert.Equal("alice@test.com", blog.AuthorEmail);
    }

    [Fact]
    public async Task GetBlogDetailAsync_ReturnsOnlyTheFiveNewestPosts_NewestFirst()
    {
        var blog = await CreateService().GetBlogDetailAsync(TestData.AliceBlogId);

        Assert.Equal(5, blog!.RecentPosts.Count);
        Assert.Equal(new[] { 7, 6, 5, 4, 3 }, blog.RecentPosts.Select(p => p.PostId).ToArray());
    }

    [Fact]
    public async Task GetBlogDetailAsync_WhenBlogDoesNotExist_ReturnsNull()
    {
        var blog = await CreateService().GetBlogDetailAsync(999);

        Assert.Null(blog);
    }

    // ---------- GetBlogPostsAsync ----------

    [Fact]
    public async Task GetBlogPostsAsync_ReturnsAllPosts_NewestFirst()
    {
        var result = await CreateService().GetBlogPostsAsync(TestData.AliceBlogId);

        Assert.NotNull(result);
        Assert.Equal("Zebra Notes", result!.BlogTitle);
        Assert.Equal(TestData.AlicePostCount, result.Posts.Count);
        Assert.Equal(TestData.AliceNewestPostId, result.Posts.First().PostId);
        Assert.Equal(1, result.Posts.Last().PostId);
    }

    [Fact]
    public async Task GetBlogPostsAsync_DoesNotIncludeOtherBlogsPosts()
    {
        var result = await CreateService().GetBlogPostsAsync(TestData.AliceBlogId);

        Assert.DoesNotContain(result!.Posts, p => p.PostId == TestData.BobPostId);
    }

    [Fact]
    public async Task GetBlogPostsAsync_WhenBlogDoesNotExist_ReturnsNull()
    {
        var result = await CreateService().GetBlogPostsAsync(999);

        Assert.Null(result);
    }

    // ---------- GetPostDetailAsync ----------

    [Fact]
    public async Task GetPostDetailAsync_ReturnsPostWithItsBlogTitle()
    {
        var post = await CreateService().GetPostDetailAsync(TestData.AliceNewestPostId, includeHiddenComments: false);

        Assert.NotNull(post);
        Assert.Equal("Alice Post 7", post!.Title);
        Assert.Equal("<p>Content 7</p>", post.Content);
        Assert.Equal("Zebra Notes", post.BlogTitle);
        Assert.Equal(TestData.AliceBlogId, post.BlogId);
    }

    [Fact]
    public async Task GetPostDetailAsync_ForVisitors_LeavesOutHiddenComments()
    {
        var post = await CreateService().GetPostDetailAsync(TestData.AliceNewestPostId, includeHiddenComments: false);

        Assert.Equal(2, post!.Comments.Count);
        Assert.DoesNotContain(post.Comments, c => c.CommentId == TestData.HiddenCommentId);
    }

    [Fact]
    public async Task GetPostDetailAsync_ForModerators_IncludesHiddenComments()
    {
        var post = await CreateService().GetPostDetailAsync(TestData.AliceNewestPostId, includeHiddenComments: true);

        Assert.Equal(3, post!.Comments.Count);
        Assert.Contains(post.Comments, c => c.CommentId == TestData.HiddenCommentId && !c.IsVisible);
    }

    [Fact]
    public async Task GetPostDetailAsync_ListsCommentsOldestFirstWithCommenterEmail()
    {
        var post = await CreateService().GetPostDetailAsync(TestData.AliceNewestPostId, includeHiddenComments: true);

        Assert.Equal(new[] { 1, 2, 3 }, post!.Comments.Select(c => c.CommentId).ToArray());
        Assert.Equal("reader@test.com", post.Comments[0].UserEmail);
    }

    [Fact]
    public async Task GetPostDetailAsync_WhenPostDoesNotExist_ReturnsNull()
    {
        var post = await CreateService().GetPostDetailAsync(999, includeHiddenComments: true);

        Assert.Null(post);
    }

    // ---------- AddCommentAsync ----------

    [Fact]
    public async Task AddCommentAsync_SavesTrimmedVisibleComment()
    {
        var added = await CreateService().AddCommentAsync(TestData.BobPostId, TestData.ReaderId, "   Great read!   ");

        Assert.True(added);
        using var context = _database.CreateContext();
        var comment = Assert.Single(context.Comments.Where(c => c.PostId == TestData.BobPostId).ToList());
        Assert.Equal("Great read!", comment.Content);
        Assert.Equal(TestData.ReaderId, comment.UserId);
        Assert.True(comment.IsVisible);
    }

    [Fact]
    public async Task AddCommentAsync_WhenPostDoesNotExist_ReturnsFalseAndSavesNothing()
    {
        var added = await CreateService().AddCommentAsync(999, TestData.ReaderId, "Hello?");

        Assert.False(added);
        using var context = _database.CreateContext();
        Assert.Equal(3, context.Comments.Count());
    }

    // ---------- ToggleCommentVisibilityAsync ----------

    [Fact]
    public async Task ToggleCommentVisibilityAsync_HidesAVisibleComment()
    {
        var toggled = await CreateService().ToggleCommentVisibilityAsync(TestData.VisibleOldCommentId);

        Assert.True(toggled);
        using var context = _database.CreateContext();
        Assert.False(context.Comments.Single(c => c.CommentId == TestData.VisibleOldCommentId).IsVisible);
    }

    [Fact]
    public async Task ToggleCommentVisibilityAsync_ShowsAHiddenComment()
    {
        var toggled = await CreateService().ToggleCommentVisibilityAsync(TestData.HiddenCommentId);

        Assert.True(toggled);
        using var context = _database.CreateContext();
        Assert.True(context.Comments.Single(c => c.CommentId == TestData.HiddenCommentId).IsVisible);
    }

    [Fact]
    public async Task ToggleCommentVisibilityAsync_WhenCommentDoesNotExist_ReturnsFalse()
    {
        var toggled = await CreateService().ToggleCommentVisibilityAsync(999);

        Assert.False(toggled);
    }
}