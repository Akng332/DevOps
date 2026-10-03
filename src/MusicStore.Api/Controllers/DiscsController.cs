using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicStore.Api.Data;
using MusicStore.Api.DTOs;
using MusicStore.Api.Models;

namespace MusicStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscsController : ControllerBase
{
    private readonly AppDbContext _context;

    public DiscsController(AppDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DiscDto>>> GetAll()
    {
        return await _context.Discs
            .Include(d => d.Musician)
            .Select(d => new DiscDto
            {
                Id = d.Id,
                Title = d.Title,
                ImageUrl = d.ImageUrl,
                Price = d.Price,
                Quantity = d.Quantity,
                AuthorName = d.Musician != null ? d.Musician.Name : "Неизвестен"
            })
            .ToListAsync();
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<DiscDto>> GetById(int id)
    {
        var disc = await _context.Discs
            .Include(d => d.Musician)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (disc == null) return NotFound();

        return new DiscDto
        {
            Id = disc.Id,
            Title = disc.Title,
            ImageUrl = disc.ImageUrl,
            Price = disc.Price,
            Quantity = disc.Quantity,
            AuthorName = disc.Musician != null ? disc.Musician.Name : "Неизвестен"
        };
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DiscDto request)
    {
        var musician = await _context.Musicians.FirstOrDefaultAsync(m => m.Name == request.AuthorName);
        if (musician == null)
        {
            musician = new Musician { Name = request.AuthorName };
            _context.Musicians.Add(musician);
            await _context.SaveChangesAsync();
        }

        var composition = new Composition { Title = request.Title };
        _context.Compositions.Add(composition);
        await _context.SaveChangesAsync();

        var disc = new Disc
        {
            Title = request.Title,
            ImageUrl = request.ImageUrl,
            Price = request.Price,
            Quantity = request.Quantity,
            MusicianId = musician.Id,
            CompositionId = composition.Id
        };

        _context.Discs.Add(disc);
        await _context.SaveChangesAsync();

        return Ok(new DiscDto
        {
            Id = disc.Id,
            Title = disc.Title,
            ImageUrl = disc.ImageUrl,
            Price = disc.Price,
            Quantity = disc.Quantity,
            AuthorName = musician.Name
        });
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] DiscDto request)
    {
        var disc = await _context.Discs
            .Include(d => d.Musician)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (disc == null) return NotFound();

        disc.Title = request.Title;
        disc.ImageUrl = request.ImageUrl;
        disc.Price = request.Price;
        disc.Quantity = request.Quantity;

        if (disc.Musician != null && disc.Musician.Name != request.AuthorName)
        {
            var musician = await _context.Musicians.FirstOrDefaultAsync(m => m.Name == request.AuthorName);
            if (musician == null)
            {
                musician = new Musician { Name = request.AuthorName };
                _context.Musicians.Add(musician);
                await _context.SaveChangesAsync();
            }
            disc.MusicianId = musician.Id;
        }

        await _context.SaveChangesAsync();

        return Ok(new DiscDto
        {
            Id = disc.Id,
            Title = disc.Title,
            ImageUrl = disc.ImageUrl,
            Price = disc.Price,
            Quantity = disc.Quantity,
            AuthorName = request.AuthorName
        });
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var disc = await _context.Discs.FindAsync(id);
        if (disc == null) return NotFound();

        _context.Discs.Remove(disc);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}