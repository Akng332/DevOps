namespace MusicStore.Api.Models;

public class Musician
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Country { get; set; }
    public DateOnly? BirthDate { get; set; }
}