using Microsoft.EntityFrameworkCore;
using DragChain.API.Sensor.Models;

namespace DragChain.API.Sensor.Data;

public class SensorDbContext : DbContext
{
    public SensorDbContext(DbContextOptions<SensorDbContext> options) : base(options) { }

    public DbSet<SensorType> SensorTypes => Set<SensorType>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProcessNote> ProcessNotes => Set<ProcessNote>();
    public DbSet<Scenario> Scenarios => Set<Scenario>();
    public DbSet<ScenarioFunction> ScenarioFunctions => Set<ScenarioFunction>();
    public DbSet<FunctionCondition> FunctionConditions => Set<FunctionCondition>();
    public DbSet<SelectionRule> SelectionRules => Set<SelectionRule>();
    public DbSet<RuleProduct> RuleProducts => Set<RuleProduct>();
    public DbSet<ProcessScenario> ProcessScenarios => Set<ProcessScenario>();
    public DbSet<AffectedMechanism> AffectedMechanisms => Set<AffectedMechanism>();
    public DbSet<UnaffectedMechanism> UnaffectedMechanisms => Set<UnaffectedMechanism>();
    public DbSet<SelectionEntry> SelectionEntries => Set<SelectionEntry>();
    public DbSet<BusinessNode> BusinessNodes => Set<BusinessNode>();
    public DbSet<EntryTreeNode> EntryTreeNodes => Set<EntryTreeNode>();
    public DbSet<RuleEntryBinding> RuleEntryBindings => Set<RuleEntryBinding>();
    public DbSet<SelectionResult> SelectionResults => Set<SelectionResult>();
    public DbSet<SelectionResultProduct> SelectionResultProducts => Set<SelectionResultProduct>();
    public DbSet<RbacUser> RbacUsers => Set<RbacUser>();
    public DbSet<RbacRolePermission> RbacRolePermissions => Set<RbacRolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Scenario -> Functions (one-to-many)
        modelBuilder.Entity<Scenario>()
            .HasMany(s => s.Functions)
            .WithOne(f => f.Scenario)
            .HasForeignKey(f => f.ScenarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Function -> Conditions (one-to-many)
        modelBuilder.Entity<ScenarioFunction>()
            .HasMany(f => f.Conditions)
            .WithOne(c => c.Function)
            .HasForeignKey(c => c.FunctionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Rule -> RuleProducts (one-to-many)
        modelBuilder.Entity<SelectionRule>()
            .HasMany(r => r.RuleProducts)
            .WithOne(rp => rp.Rule)
            .HasForeignKey(rp => rp.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProcessScenario -> AffectedMechanisms (one-to-many)
        modelBuilder.Entity<ProcessScenario>()
            .HasMany(ps => ps.AffectedMechanisms)
            .WithOne(am => am.ProcessScenario)
            .HasForeignKey(am => am.ProcessScenarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProcessScenario -> UnaffectedMechanisms (one-to-many)
        modelBuilder.Entity<ProcessScenario>()
            .HasMany(ps => ps.UnaffectedMechanisms)
            .WithOne(um => um.ProcessScenario)
            .HasForeignKey(um => um.ProcessScenarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraints
        modelBuilder.Entity<SelectionRule>()
            .HasIndex(r => new { r.ScenarioId, r.FunctionId, r.ConditionId })
            .IsUnique();

        modelBuilder.Entity<RuleProduct>()
            .HasIndex(rp => new { rp.RuleId, rp.ProductId })
            .IsUnique();

        modelBuilder.Entity<SelectionEntry>()
            .HasIndex(e => e.Code)
            .IsUnique();

        modelBuilder.Entity<BusinessNode>()
            .HasIndex(n => n.Code)
            .IsUnique();

        modelBuilder.Entity<EntryTreeNode>()
            .HasOne(n => n.Parent)
            .WithMany(n => n.Children)
            .HasForeignKey(n => n.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EntryTreeNode>()
            .HasIndex(n => new { n.EntryId, n.BusinessNodeId, n.ParentId })
            .IsUnique();

        modelBuilder.Entity<RuleEntryBinding>()
            .HasIndex(b => new { b.RuleId, b.EntryTreeNodeId })
            .IsUnique();

        modelBuilder.Entity<SelectionResult>()
            .HasMany(result => result.Products)
            .WithOne(product => product.SelectionResult)
            .HasForeignKey(product => product.SelectionResultId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SelectionResultProduct>()
            .HasIndex(product => new { product.SelectionResultId, product.ProductId })
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Code)
            .IsUnique();

        modelBuilder.Entity<Scenario>()
            .HasIndex(s => s.Code)
            .IsUnique();

        modelBuilder.Entity<ScenarioFunction>()
            .HasIndex(f => f.Code)
            .IsUnique();

        modelBuilder.Entity<FunctionCondition>()
            .HasIndex(c => c.Code)
            .IsUnique();

        modelBuilder.Entity<RbacUser>()
            .HasIndex(u => u.EmployeeNo)
            .IsUnique();

        modelBuilder.Entity<RbacRolePermission>()
            .HasIndex(p => new { p.Role, p.PermissionCode })
            .IsUnique();

        // Seed data
        SeedData.Configure(modelBuilder);
    }
}
