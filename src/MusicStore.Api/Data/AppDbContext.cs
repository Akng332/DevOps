using Microsoft.EntityFrameworkCore;
using MusicStore.Api.Models;

namespace MusicStore.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Musician> Musicians => Set<Musician>();
    public DbSet<Ensemble> Ensembles => Set<Ensemble>();
    public DbSet<Composition> Compositions => Set<Composition>();
    public DbSet<Disc> Discs => Set<Disc>();
    public DbSet<Sale> Sales => Set<Sale>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Disc>()
            .HasOne(d => d.Composition)
            .WithMany()
            .HasForeignKey(d => d.CompositionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Sale>()
            .HasOne(s => s.Disc)
            .WithMany()
            .HasForeignKey(s => s.DiscId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Sale>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Disc>()
            .Property(d => d.Price)
            .HasPrecision(10, 2);

       modelBuilder.Entity<Sale>()
            .Property(s => s.TotalAmount)
            .HasPrecision(10, 2);
        }
}