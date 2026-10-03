namespace MusicStore.Api.Models;

public class Sale
{
    public int Id { get; set; }
    public int DiscId { get; set; }
    public Disc? Disc { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
}