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

        if (items.Any(item => item.Quantity <= 0 || item.DiscId <= 0))
            return BadRequest("Количество товара и идентификатор диска должны быть положительными.");

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            return Unauthorized();

        var requestedItems = items.GroupBy(item => item.DiscId)
            .Select(group => new CheckoutItem
            {
                DiscId = group.Key,
                Quantity = checked(group.Sum(item => item.Quantity))
            }).ToList();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var sales = new List<Sale>();
        decimal totalSum = 0;

        foreach (var item in requestedItems)
        {
            var disc = await _context.Discs.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == item.DiscId);
            if (disc == null)
                return BadRequest($"Диск с ID {item.DiscId} не найден.");

            var updated = await _context.Discs
                .Where(d => d.Id == item.DiscId && d.Quantity >= item.Quantity)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    d => d.Quantity, d => d.Quantity - item.Quantity));
            if (updated == 0)
                return BadRequest($"Недостаточно товара «{disc.Title}» на складе. Осталось: {disc.Quantity}.");

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
        await transaction.CommitAsync();

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
