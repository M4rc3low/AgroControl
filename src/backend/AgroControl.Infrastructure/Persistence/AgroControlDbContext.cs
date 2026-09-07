using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Finance;
using AgroControl.Domain.Modules.Identity;
using AgroControl.Domain.Modules.Inventory;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Domain.Modules.Seasons;
using AgroControl.Domain.Modules.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class AgroControlDbContext(DbContextOptions<AgroControlDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<OrganizationModuleEntitlement> OrganizationModuleEntitlements => Set<OrganizationModuleEntitlement>();
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<Field> Fields => Set<Field>();
    public DbSet<Crop> Crops => Set<Crop>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<FinancialCategory> FinancialCategories => Set<FinancialCategory>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("organizations"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired(); entity.Property(x => x.Slug).HasMaxLength(180).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
        });
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(254).IsRequired(); entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired(); entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });
        modelBuilder.Entity<OrganizationMembership>(entity =>
        {
            entity.ToTable("organization_memberships"); entity.HasKey(x => new { x.OrganizationId, x.UserId });
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); entity.HasIndex(x => x.UserId);
        });
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("subscriptions"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Plan).HasConversion<string>().HasMaxLength(32).IsRequired(); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade); entity.HasIndex(x => x.OrganizationId).IsUnique();
        });
        modelBuilder.Entity<OrganizationModuleEntitlement>(entity =>
        {
            entity.ToTable("organization_module_entitlements"); entity.HasKey(x => new { x.OrganizationId, x.Module });
            entity.Property(x => x.Module).HasConversion<string>().HasMaxLength(64).IsRequired();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Farm>(entity =>
        {
            entity.ToTable("farms"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(160).IsRequired(); entity.Property(x => x.TotalAreaHectares).HasPrecision(18, 4).IsRequired(); entity.Property(x => x.City).HasMaxLength(120); entity.Property(x => x.State).HasMaxLength(80);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade); entity.HasIndex(x => x.OrganizationId); entity.HasIndex(x => new { x.OrganizationId, x.Name });
        });
        modelBuilder.Entity<Field>(entity =>
        {
            entity.ToTable("fields"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(160).IsRequired(); entity.Property(x => x.AreaHectares).HasPrecision(18, 4).IsRequired();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade); entity.HasOne<Farm>().WithMany().HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Restrict); entity.HasIndex(x => x.OrganizationId); entity.HasIndex(x => x.FarmId); entity.HasIndex(x => new { x.OrganizationId, x.Name });
        });
        modelBuilder.Entity<Crop>(entity =>
        {
            entity.ToTable("crops"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired(); entity.Property(x => x.Variety).HasMaxLength(120);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade); entity.HasIndex(x => x.OrganizationId); entity.HasIndex(x => new { x.OrganizationId, x.Name, x.Variety });
        });
        modelBuilder.Entity<Season>(entity =>
        {
            entity.ToTable("seasons"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired(); entity.Property(x => x.ExpectedYieldPerHectare).HasPrecision(18, 4); entity.Property(x => x.ActualYieldPerHectare).HasPrecision(18, 4); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade); entity.HasOne<Field>().WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Restrict); entity.HasOne<Crop>().WithMany().HasForeignKey(x => x.CropId).OnDelete(DeleteBehavior.Restrict); entity.HasIndex(x => x.OrganizationId); entity.HasIndex(x => x.FieldId); entity.HasIndex(x => x.CropId); entity.HasIndex(x => new { x.OrganizationId, x.Status });
        });
        modelBuilder.Entity<InventoryCategory>(entity =>
        {
            entity.ToTable("inventory_categories"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade); entity.HasIndex(x => x.OrganizationId); entity.HasIndex(x => new { x.OrganizationId, x.Name });
        });
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.ToTable("inventory_items"); entity.HasKey(x => x.Id); entity.Property(x => x.Sku).HasMaxLength(80).IsRequired(); entity.Property(x => x.Name).HasMaxLength(160).IsRequired(); entity.Property(x => x.Unit).HasConversion<string>().HasMaxLength(32).IsRequired(); entity.Property(x => x.MinimumStock).HasPrecision(18, 4).IsRequired();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade); entity.HasOne<InventoryCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict); entity.HasIndex(x => x.OrganizationId); entity.HasIndex(x => new { x.OrganizationId, x.Sku }).IsUnique(); entity.HasIndex(x => x.CategoryId);
        });
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("warehouses"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(160).IsRequired(); entity.Property(x => x.Location).HasMaxLength(240);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade); entity.HasOne<Farm>().WithMany().HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Restrict); entity.HasIndex(x => x.OrganizationId); entity.HasIndex(x => x.FarmId); entity.HasIndex(x => new { x.OrganizationId, x.Name });
        });
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("stock_movements"); entity.HasKey(x => x.Id); entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(32).IsRequired(); entity.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired(); entity.Property(x => x.BatchNumber).HasMaxLength(120); entity.Property(x => x.Notes).HasMaxLength(1000); entity.Ignore(x => x.SignedQuantity);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade); entity.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict); entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict); entity.HasOne<Farm>().WithMany().HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Restrict); entity.HasOne<Field>().WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Restrict); entity.HasOne<Season>().WithMany().HasForeignKey(x => x.SeasonId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.OrganizationId); entity.HasIndex(x => x.ItemId); entity.HasIndex(x => x.WarehouseId); entity.HasIndex(x => new { x.OrganizationId, x.ItemId, x.WarehouseId, x.OccurredAtUtc }); entity.HasIndex(x => x.BatchNumber);
        });
        modelBuilder.Entity<FinancialCategory>(entity =>
        {
            entity.ToTable("financial_categories"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade); entity.HasIndex(x => x.OrganizationId); entity.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();
        });
        modelBuilder.Entity<CostCenter>(entity =>
        {
            entity.ToTable("cost_centers"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade); entity.HasIndex(x => x.OrganizationId); entity.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();
        });
        modelBuilder.Entity<FinancialTransaction>(entity =>
        {
            entity.ToTable("financial_transactions"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Counterparty).HasMaxLength(200);
            entity.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<FinancialCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CostCenter>().WithMany().HasForeignKey(x => x.CostCenterId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Farm>().WithMany().HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Field>().WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Season>().WithMany().HasForeignKey(x => x.SeasonId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.OrganizationId);
            entity.HasIndex(x => x.CategoryId);
            entity.HasIndex(x => x.CostCenterId);
            entity.HasIndex(x => x.FarmId);
            entity.HasIndex(x => x.FieldId);
            entity.HasIndex(x => x.SeasonId);
            entity.HasIndex(x => new { x.OrganizationId, x.Type, x.Status, x.CompetenceDate });
            entity.HasIndex(x => new { x.OrganizationId, x.DueDate });
        });
    }
}
