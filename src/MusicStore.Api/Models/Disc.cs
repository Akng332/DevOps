namespace MusicStore.Api.Models;

public class Disc
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    public int MusicianId { get; set; }
    public Musician? Musician { get; set; }

    public int CompositionId { get; set; }
    public Composition? Composition { get; set; }
}