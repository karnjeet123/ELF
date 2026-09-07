using Microsoft.EntityFrameworkCore;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;

namespace Elf.Brewery.Infrastructure.Data;

public class BreweryDbContext : DbContext
{
    public BreweryDbContext(DbContextOptions<BreweryDbContext> options) : base(options)
    {
    }

    public DbSet<DomainBrewery> Breweries => Set<DomainBrewery>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<DomainBrewery>(entity =>
        {
            entity.ToTable("Breweries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(300);
            entity.Property(e => e.City).HasMaxLength(200);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.City);
        });

    }
}