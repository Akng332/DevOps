using Microsoft.EntityFrameworkCore;
using MusicStore.Api.Models;
using MusicStore.Api.Services;

namespace MusicStore.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, PasswordHasher hasher)
    {
        if (!await context.Users.AnyAsync(u => u.Username == "ad1"))
        {
            context.Users.Add(new User
            {
                Username = "ad1",
                PasswordHash = hasher.Hash("ad1"),
                Role = "admin",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        if (!await context.Discs.AnyAsync())
        {
            var musicians = new[]
            {
                new Musician { Name = "The Beatles" },
                new Musician { Name = "Pink Floyd" },
                new Musician { Name = "Queen" },
                new Musician { Name = "Nirvana" },
                new Musician { Name = "Michael Jackson" },
                new Musician { Name = "Кино" }
            };
            context.Musicians.AddRange(musicians);
            await context.SaveChangesAsync();

            var compositions = new[]
            {
                new Composition { Title = "Abbey Road" },
                new Composition { Title = "The Dark Side of the Moon" },
                new Composition { Title = "A Night at the Opera" },
                new Composition { Title = "Nevermind" },
                new Composition { Title = "Thriller" },
                new Composition { Title = "Группа крови" }
            };
            context.Compositions.AddRange(compositions);
            await context.SaveChangesAsync();

            var discs = new[]
            {
                new Disc
{
    Title = "Abbey Road",
    ImageUrl = "/images/abbey_road.webp",
    Price = 1500,
    Quantity = 10,
    MusicianId = musicians[0].Id,
    CompositionId = compositions[0].Id
},
new Disc
{
    Title = "The Dark Side of the Moon",
    ImageUrl = "/images/dark_side.jpeg",
    Price = 1800,
    Quantity = 10,
    MusicianId = musicians[1].Id,
    CompositionId = compositions[1].Id
},
new Disc
{
    Title = "A Night at the Opera",
    ImageUrl = "/images/opera.jpg",
    Price = 1700,
    Quantity = 10,
    MusicianId = musicians[2].Id,
    CompositionId = compositions[2].Id
},
new Disc
{
    Title = "Nevermind",
    ImageUrl = "/images/nevermind.webp",
    Price = 1600,
    Quantity = 10,
    MusicianId = musicians[3].Id,
    CompositionId = compositions[3].Id
},
new Disc
{
    Title = "Thriller",
    ImageUrl = "/images/thriller.jpg",
    Price = 1900,
    Quantity = 10,
    MusicianId = musicians[4].Id,
    CompositionId = compositions[4].Id
},
new Disc
{
    Title = "Группа крови",
    ImageUrl = "/images/gruppa_krovi.webp",
    Price = 1200,
    Quantity = 10,
    MusicianId = musicians[5].Id,
    CompositionId = compositions[5].Id
}
            };
            context.Discs.AddRange(discs);
            await context.SaveChangesAsync();
        }
    }
}