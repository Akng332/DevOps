using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicStore.Api.Data;
using MusicStore.Api.Models;
using System.Security.Claims;

namespace MusicStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly AppDbContext _context;

    public SalesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] List<CheckoutItem> items)
    {
        if (items == null || items.Count == 0)
            return BadRequest("Корзина пуста.");

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            return Unauthorized();

        var sales = new List<Sale>();
        decimal totalSum = 0;

        foreach (var item in items)
        {
            var disc = await _context.Discs.FindAsync(item.DiscId);
            if (disc == null) continue;

            if (disc.Quantity < item.Quantity)
                return BadRequest($"Недостаточно товара «{disc.Title}» на складе. Осталось: {disc.Quantity}.");

            disc.Quantity -= item.Quantity;

            var sale = new Sale
            {
                DiscId = disc.Id,
                UserId = userId,
                Quantity = item.Quantity,
                TotalAmount = disc.Price * item.Quantity,
                SaleDate = DateTime.UtcNow
            };
            sales.Add(sale);
            totalSum += sale.TotalAmount;
        }

        _context.Sales.AddRange(sales);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Покупка успешно оформлена", total = totalSum, count = sales.Count });
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMySales()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            return Unauthorized();

        var sales = await _context.Sales
            .Where(s => s.UserId == userId)
            .Include(s => s.Disc)
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new
            {
                s.Id,
                DiscTitle = s.Disc != null ? s.Disc.Title : "—",
                s.Quantity,
                s.TotalAmount,
                s.SaleDate
            })
            .ToListAsync();

        return Ok(sales);
    }

    [Authorize(Roles = "admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllSales()
    {
        var sales = await _context.Sales
            .Include(s => s.Disc)
            .Include(s => s.User)
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new
            {
                s.Id,
                DiscTitle = s.Disc != null ? s.Disc.Title : "—",
                Username = s.User != null ? s.User.Username : "—",
                s.Quantity,
                s.TotalAmount,
                s.SaleDate
            })
            .ToListAsync();

        return Ok(sales);
    }
}

public class CheckoutItem
{
    public int DiscId { get; set; }
    public int Quantity { get; set; } = 1;
}