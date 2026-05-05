using Microsoft.EntityFrameworkCore;
using DragChain.API.Models;

namespace DragChain.API.Data;

public class DragChainDbContext : DbContext
{
    public DragChainDbContext(DbContextOptions<DragChainDbContext> options) : base(options) { }

    public DbSet<PipeType> PipeTypes => Set<PipeType>();
    public DbSet<WzlCatalog> WzlCatalog => Set<WzlCatalog>();
    public DbSet<MeCatalog> MeCatalog => Set<MeCatalog>();
    public DbSet<TrunkingCatalog> TrunkingCatalog => Set<TrunkingCatalog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PipeType>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Type);
        });

        modelBuilder.Entity<WzlCatalog>(entity =>
        {
            entity.HasIndex(e => e.Model);
            entity.HasIndex(e => e.Function);
        });

        modelBuilder.Entity<MeCatalog>(entity =>
        {
            entity.HasIndex(e => e.BaseModel);
        });

        modelBuilder.Entity<TrunkingCatalog>(entity =>
        {
            entity.HasIndex(e => e.Model);
        });
    }
}
