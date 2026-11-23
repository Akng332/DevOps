using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MusicStore.Api.Controllers;
using MusicStore.Api.DTOs;
using MusicStore.Api.Models;
using MusicStore.Api.Services;

namespace MusicStore.Tests;

public class AuthTests
{
    private static AuthController CreateController(DatabaseFixture db, int? userId = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "a-local-test-key-with-at-least-32-characters",
                ["Jwt:Issuer"] = "MusicStoreTests",
                ["Jwt:Audience"] = "MusicStoreTests",
                ["Jwt:ExpiresInHours"] = "1"
            }).Build();
        var claims = userId.HasValue
            ? new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }
            : Array.Empty<Claim>();
        return new AuthController(db.Context, new PasswordHasher(), new JwtTokenService(configuration))
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

    [Fact]
    public async Task Register_CreatesUserAndRejectsDuplicate()
    {
        using var db = new DatabaseFixture();
        var controller = CreateController(db);
        var request = new RegisterDto { Username = "nikita", Password = "long-test-password" };

        var first = await controller.Register(request);
        var response = Assert.IsType<AuthResponseDto>(Assert.IsType<OkObjectResult>(first.Result).Value);
        Assert.Equal("user", response.Role);
        Assert.NotEmpty(response.Token);
        Assert.NotEqual(request.Password, db.Context.Users.Single().PasswordHash);

        var duplicate = await controller.Register(request);
        Assert.IsType<BadRequestObjectResult>(duplicate.Result);
    }

    [Fact]
    public async Task Login_RejectsWrongPasswordAndReturnsTokenForValidOne()
    {
        using var db = new DatabaseFixture();
        var hasher = new PasswordHasher();
        db.Context.Users.Add(new User { Username = "buyer", PasswordHash = hasher.Hash("correct-password") });
        await db.Context.SaveChangesAsync();
        var controller = CreateController(db);

        Assert.IsType<UnauthorizedObjectResult>((await controller.Login(
            new LoginDto { Username = "buyer", Password = "wrong" })).Result);
        Assert.IsType<UnauthorizedObjectResult>((await controller.Login(
            new LoginDto { Username = "missing", Password = "correct-password" })).Result);

        var valid = await controller.Login(new LoginDto { Username = "buyer", Password = "correct-password" });
        Assert.NotEmpty(Assert.IsType<AuthResponseDto>(Assert.IsType<OkObjectResult>(valid.Result).Value).Token);
    }

    [Fact]
    public async Task Me_UsesIdentityAndReportsMissingUser()
    {
        using var db = new DatabaseFixture();
        Assert.IsType<UnauthorizedResult>(await CreateController(db).GetMe());
        Assert.IsType<NotFoundResult>(await CreateController(db, 123).GetMe());

        var user = new User { Username = "buyer", PasswordHash = "hash" };
        db.Context.Users.Add(user);
        await db.Context.SaveChangesAsync();
        Assert.IsType<OkObjectResult>(await CreateController(db, user.Id).GetMe());
    }

    [Fact]
    public void PasswordHasher_RejectsMalformedHash()
    {
        Assert.False(new PasswordHasher().Verify("password", "not-a-bcrypt-hash"));
    }
}
