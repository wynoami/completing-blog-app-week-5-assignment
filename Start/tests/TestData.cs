using AtlasBlog.Data;
using AtlasBlog.Models.EF;

namespace AtlasBlog.Tests;

/// <summary>
/// A small, known set of users, blogs, posts, and comments that the
/// service tests can rely on. Every value is fixed so tests can check exact results.
/// </summary>
public static class TestData
{
    public const string AliceId = "alice-id";
    public const string BobId = "bob-id";
    public const string ReaderId = "reader-id";   // a regular user with no blog

    public const int AliceBlogId = 1;
    public const int BobBlogId = 2;

    public const int AlicePostCount = 7;
    public const int AliceNewestPostId = 7;        // posts 1-7 are created one day apart
    public const int BobPostId = 8;

    public const int VisibleOldCommentId = 1;
    public const int HiddenCommentId = 2;
    public const int VisibleNewCommentId = 3;

    public static readonly DateTime Start = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Fills an empty test database with the known data.</summary>
    public static void Seed(ApplicationDbContext context)
    {
        context.Users.AddRange(
            new ApplicationUser { Id = AliceId, UserName = "alice@test.com", Email = "alice@test.com" },
            new ApplicationUser { Id = BobId, UserName = "bob@test.com", Email = "bob@test.com" },
            new ApplicationUser { Id = ReaderId, UserName = "reader@test.com", Email = "reader@test.com" });

        // Titles chosen so alphabetical order (Apple, Zebra) is the opposite of ID order
        context.Blogs.AddRange(
            new Blog { BlogId = AliceBlogId, Title = "Zebra Notes", Subtitle = "Alice writes", Description = "About zebras", AuthorId = AliceId, CreatedAt = Start, LastModifiedAt = Start },
            new Blog { BlogId = BobBlogId, Title = "Apple Thoughts", Subtitle = "Bob writes", AuthorId = BobId, CreatedAt = Start, LastModifiedAt = Start });

        for (int i = 1; i <= AlicePostCount; i++)
        {
            context.Posts.Add(new Post
            {
                PostId = i,
                BlogId = AliceBlogId,
                Title = $"Alice Post {i}",
                Content = $"<p>Content {i}</p>",
                CreatedAt = Start.AddDays(i),
                LastModifiedAt = Start.AddDays(i)
            });
        }

        context.Posts.Add(new Post { PostId = BobPostId, BlogId = BobBlogId, Title = "Bob Post", Content = "<p>Bob</p>", CreatedAt = Start, LastModifiedAt = Start });

        // Three comments on Alice's newest post: oldest visible, middle hidden, newest visible
        context.Comments.AddRange(
            new Comment { CommentId = VisibleOldCommentId, PostId = AliceNewestPostId, UserId = ReaderId, Content = "First!", CreatedAt = Start.AddDays(10), IsVisible = true },
            new Comment { CommentId = HiddenCommentId, PostId = AliceNewestPostId, UserId = BobId, Content = "Rude comment", CreatedAt = Start.AddDays(11), IsVisible = false },
            new Comment { CommentId = VisibleNewCommentId, PostId = AliceNewestPostId, UserId = BobId, Content = "Nice post", CreatedAt = Start.AddDays(12), IsVisible = true });

        context.SaveChanges();
    }
}