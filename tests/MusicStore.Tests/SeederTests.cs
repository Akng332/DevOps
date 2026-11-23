using MusicStore.Api.Data;
using MusicStore.Api.Services;

namespace MusicStore.Tests;

public class SeederTests
{
    [Fact]
    public async Task Seeder_CreatesCatalogAndAdminOnlyOnce()
    {
        using var db = new DatabaseFixture();
        var hasher = new PasswordHasher();
        await DbSeeder.SeedAsync(db.Context, hasher, "admin", "a-password-with-16-characters");

        Assert.Equal(6, db.Context.Discs.Count());
        Assert.Equal(6, db.Context.Musicians.Count());
        Assert.Single(db.Context.Users);
        Assert.True(hasher.Verify("a-password-with-16-characters", db.Context.Users.Single().PasswordHash));

        await DbSeeder.SeedAsync(db.Context, hasher, "admin", "a-password-with-16-characters");
        Assert.Equal(6, db.Context.Discs.Count());
        Assert.Single(db.Context.Users);
    }

    [Fact]
    public async Task Seeder_RejectsShortBootstrapPassword()
    {
        using var db = new DatabaseFixture();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DbSeeder.SeedAsync(db.Context, new PasswordHasher(), "admin", "short"));
        Assert.Empty(db.Context.Users);
    }
}
