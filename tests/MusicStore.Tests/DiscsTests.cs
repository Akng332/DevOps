using Microsoft.AspNetCore.Mvc;
using MusicStore.Api.Controllers;
using MusicStore.Api.DTOs;
using MusicStore.Api.Models;

namespace MusicStore.Tests;

public class DiscsTests
{
    [Fact]
    public async Task LowStock_UsesPersistedThreshold()
    {
        using var db = new DatabaseFixture();
        var musician = new Musician { Name = "Автор" };
        var composition = new Composition { Title = "Альбом" };
        db.Context.Musicians.Add(musician);
        db.Context.Compositions.Add(composition);
        await db.Context.SaveChangesAsync();
        db.Context.Discs.AddRange(
            new Disc { Title = "Пора заказать", Price = 100, Quantity = 2, ReorderThreshold = 2, MusicianId = musician.Id, CompositionId = composition.Id },
            new Disc { Title = "В наличии", Price = 100, Quantity = 5, ReorderThreshold = 2, MusicianId = musician.Id, CompositionId = composition.Id });
        await db.Context.SaveChangesAsync();

        var result = await new DiscsController(db.Context).GetLowStock();

        var discs = Assert.IsAssignableFrom<IEnumerable<DiscDto>>(result.Value);
        Assert.Equal("Пора заказать", Assert.Single(discs).Title);
    }

    [Theory]
    [InlineData("", "Автор", 100, 1, 3)]
    [InlineData("Диск", "", 100, 1, 3)]
    [InlineData("Диск", "Автор", 0, 1, 3)]
    [InlineData("Диск", "Автор", 100, -1, 3)]
    [InlineData("Диск", "Автор", 100, 1, -1)]
    public async Task Create_RejectsInvalidData(string title, string author, decimal price, int quantity, int threshold)
    {
        using var db = new DatabaseFixture();
        var result = await new DiscsController(db.Context).Create(new DiscDto
        {
            Title = title,
            AuthorName = author,
            Price = price,
            Quantity = quantity,
            ReorderThreshold = threshold
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Context.Discs);
    }

    [Fact]
    public async Task Create_AndUpdate_PersistThreshold()
    {
        using var db = new DatabaseFixture();
        var controller = new DiscsController(db.Context);
        var created = await controller.Create(new DiscDto
        {
            Title = "Диск",
            AuthorName = "Автор",
            Price = 100,
            Quantity = 4,
            ReorderThreshold = 5
        });
        var first = Assert.IsType<OkObjectResult>(created);
        var disc = Assert.IsType<DiscDto>(first.Value);
        Assert.Equal(5, disc.ReorderThreshold);

        var updated = await controller.Update(disc.Id, new DiscDto
        {
            Title = "Диск",
            AuthorName = "Автор",
            Price = 100,
            Quantity = 4,
            ReorderThreshold = 1
        });
        Assert.IsType<OkObjectResult>(updated);
        Assert.Equal(1, db.Context.Discs.Single().ReorderThreshold);
    }

    [Fact]
    public async Task MissingDisc_ReturnsNotFound()
    {
        using var db = new DatabaseFixture();
        var controller = new DiscsController(db.Context);

        Assert.IsType<NotFoundResult>((await controller.GetById(999)).Result);
        Assert.IsType<NotFoundResult>(await controller.Delete(999));
    }
}
