namespace MusicStore.Api.Models;

public class Composition
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }

    // "musician" или "ensemble"
    public string PerformerType { get; set; } = "musician";
    public int PerformerId { get; set; }
}