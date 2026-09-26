namespace MusicStore.Api.Models;

public class Disc
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public int CompositionId { get; set; }
    public Composition? Composition { get; set; }

    public decimal Price { get; set; }
    public int Stock { get; set; }
}