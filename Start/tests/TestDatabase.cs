using AtlasBlog.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AtlasBlog.Tests;

/// <summary>
/// A brand-new, empty SQLite database that lives only in memory.
/// Each test creates its own, so tests never share data, and it
/// disappears when the test finishes.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    /// <param name="enforceForeignKeys">
    /// Pass false when a test saves rows that point at users who were never
    /// actually created (for example, when UserManager is a mock).
    /// </param>
    public TestDatabase(bool enforceForeignKeys = true)
    {
        // The in-memory database exists only while this connection stays open
        _connection = new SqliteConnection($"DataSource=:memory:;Foreign Keys={enforceForeignKeys}");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates a context connected to this test's database. Use one context
    /// for setting up data and a fresh one for checking results, just like
    /// separate web requests would.
    /// </summary>
    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}