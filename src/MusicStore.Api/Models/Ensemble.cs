namespace MusicStore.Api.Models;

public class Ensemble
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Genre { get; set; }
    public int? FoundedYear { get; set; }
}