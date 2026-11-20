using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MusicStore.Api.Data;

namespace MusicStore.Tests;

public sealed class DatabaseFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public AppDbContext Context { get; }

    public DatabaseFixture()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
