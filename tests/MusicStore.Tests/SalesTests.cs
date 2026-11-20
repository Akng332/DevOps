using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicStore.Api.Controllers;
using MusicStore.Api.Models;

namespace MusicStore.Tests;

public class SalesTests
{
    private static SalesController CreateController(DatabaseFixture db, int? userId = 1)
    {
        var claims = userId.HasValue
            ? new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }
            : Array.Empty<Claim>();
        return new SalesController(db.Context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims))
                }
            }
        };
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Checkout_RejectsNonpositiveQuantity(int quantity)
    {
        using var db = new DatabaseFixture();
        var result = await CreateController(db).Checkout([new CheckoutItem { DiscId = 1, Quantity = quantity }]);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Checkout_RequiresIdentityAndKnownDisc()
    {
        using var db = new DatabaseFixture();
        var item = new CheckoutItem { DiscId = 123, Quantity = 1 };

        Assert.IsType<UnauthorizedResult>(await CreateController(db, null).Checkout([item]));
        Assert.IsType<BadRequestObjectResult>(await CreateController(db).Checkout([item]));
    }

    [Fact]
    public async Task Checkout_RejectsOverstockWithoutChangingData()
    {
        using var db = new DatabaseFixture();
        var disc = await SeedDisc(db, 2);

        var result = await CreateController(db).Checkout([new CheckoutItem { DiscId = disc.Id, Quantity = 3 }]);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(2, db.Context.Discs.AsNoTracking().Single().Quantity);
        Assert.Empty(db.Context.Sales);
    }

    [Fact]
    public async Task Checkout_GroupsDuplicatesAndSavesSale()
    {
        using var db = new DatabaseFixture();
        var disc = await SeedDisc(db, 5);
        var controller = CreateController(db);

        var result = await controller.Checkout([
            new CheckoutItem { DiscId = disc.Id, Quantity = 1 },
            new CheckoutItem { DiscId = disc.Id, Quantity = 2 }
        ]);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(2, db.Context.Discs.AsNoTracking().Single().Quantity);
        Assert.Equal(3, db.Context.Sales.Single().Quantity);
        Assert.Equal(300, db.Context.Sales.Single().TotalAmount);
        Assert.IsType<OkObjectResult>(await controller.GetMySales());
    }

    private static async Task<Disc> SeedDisc(DatabaseFixture db, int quantity)
    {
        var user = new User { Username = "buyer", PasswordHash = "hash" };
        var musician = new Musician { Name = "Автор" };
        var composition = new Composition { Title = "Альбом" };
        db.Context.Users.Add(user);
        db.Context.Musicians.Add(musician);
        db.Context.Compositions.Add(composition);
        await db.Context.SaveChangesAsync();
        var disc = new Disc
        {
            Title = "Диск",
            Price = 100,
            Quantity = quantity,
            MusicianId = musician.Id,
            CompositionId = composition.Id
        };
        db.Context.Discs.Add(disc);
        await db.Context.SaveChangesAsync();
        return disc;
    }
}
