namespace MusicStore.Api.DTOs;

public class DiscDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string AuthorName { get; set; } = string.Empty;
}