using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Exporting;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Finance;
using AgroControl.Domain.Modules.Identity;
using AgroControl.Domain.Modules.Inventory;
using AgroControl.Domain.Modules.Irrigation;
using AgroControl.Domain.Modules.Machinery;
using AgroControl.Domain.Modules.Market;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Domain.Modules.Seasons;
using AgroControl.Domain.Modules.Subscriptions;
using AgroControl.Domain.Modules.Sustainability;
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
    public DbSet<IrrigationZone> IrrigationZones => Set<IrrigationZone>();
    public DbSet<IrrigationApplication> IrrigationApplications => Set<IrrigationApplication>();
    public DbSet<EmissionFactor> EmissionFactors => Set<EmissionFactor>();
    public DbSet<EmissionActivity> EmissionActivities => Set<EmissionActivity>();
    public DbSet<ExportOrder> ExportOrders => Set<ExportOrder>();
    public DbSet<ExportDocument> ExportDocuments => Set<ExportDocument>();
    public DbSet<ExportCost> ExportCosts => Set<ExportCost>();
    public DbSet<ExportOrderStatusEvent> ExportOrderStatusEvents => Set<ExportOrderStatusEvent>();
    public DbSet<FinancialCategory> FinancialCategories => Set<FinancialCategory>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<HourMeterReading> HourMeterReadings => Set<HourMeterReading>();
    public DbSet<Fueling> Fuelings => Set<Fueling>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<Commodity> Commodities => Set<Commodity>();
    public DbSet<MarketQuote> MarketQuotes => Set<MarketQuote>();
    public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();

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
        modelBuilder.Entity<Machine>(entity =>
        {
            entity.ToTable("machines"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.InternalCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Manufacturer).HasMaxLength(120);
            entity.Property(x => x.Model).HasMaxLength(120);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.CurrentHourMeter).HasPrecision(18, 2).IsRequired();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Farm>().WithMany().HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.OrganizationId);
            entity.HasIndex(x => x.FarmId);
            entity.HasIndex(x => new { x.OrganizationId, x.InternalCode }).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.Status });
        });
        modelBuilder.Entity<HourMeterReading>(entity =>
        {
            entity.ToTable("machine_hour_meter_readings"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Hours).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Machine>().WithMany().HasForeignKey(x => x.MachineId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.OrganizationId);
            entity.HasIndex(x => new { x.OrganizationId, x.MachineId, x.OccurredAtUtc });
        });
        modelBuilder.Entity<Fueling>(entity =>
        {
            entity.ToTable("machine_fuelings"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Liters).HasPrecision(18, 3).IsRequired();
            entity.Property(x => x.TotalCost).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.HourMeter).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Ignore(x => x.UnitCost);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Machine>().WithMany().HasForeignKey(x => x.MachineId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.OrganizationId);
            entity.HasIndex(x => new { x.OrganizationId, x.MachineId, x.OccurredAtUtc });
        });
        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.ToTable("machine_maintenance_records"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(240).IsRequired();
            entity.Property(x => x.HourMeter).HasPrecision(18, 2);
            entity.Property(x => x.PartsCost).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.LaborCost).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.OtherCost).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.NextMaintenanceHourMeter).HasPrecision(18, 2);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Ignore(x => x.TotalCost);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Machine>().WithMany().HasForeignKey(x => x.MachineId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.OrganizationId);
            entity.HasIndex(x => new { x.OrganizationId, x.MachineId, x.PerformedOn });
            entity.HasIndex(x => x.NextMaintenanceDate);
        });
        modelBuilder.Entity<Commodity>(entity =>
        {
            entity.ToTable("commodities"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Symbol).HasMaxLength(40).IsRequired();
            entity.Property(x => x.DefaultCurrency).HasMaxLength(16).IsRequired();
            entity.Property(x => x.DefaultUnit).HasMaxLength(40).IsRequired();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.OrganizationId);
            entity.HasIndex(x => new { x.OrganizationId, x.Symbol }).IsUnique();
        });
        modelBuilder.Entity<MarketQuote>(entity =>
        {
            entity.ToTable("market_quotes"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Price).HasPrecision(18, 4).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Unit).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(120).IsRequired();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Commodity>().WithMany().HasForeignKey(x => x.CommodityId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.OrganizationId);
            entity.HasIndex(x => new { x.OrganizationId, x.CommodityId, x.QuotedAtUtc });
        });
        modelBuilder.Entity<PriceAlert>(entity =>
        {
            entity.ToTable("price_alerts"); entity.HasKey(x => x.Id);
            entity.Property(x => x.TargetPrice).HasPrecision(18, 4).IsRequired();
            entity.Property(x => x.Direction).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Commodity>().WithMany().HasForeignKey(x => x.CommodityId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.OrganizationId);
            entity.HasIndex(x => new { x.OrganizationId, x.CommodityId, x.IsActive });
        });
        modelBuilder.Entity<IrrigationZone>(entity =>
        {
            entity.ToTable("irrigation_zones"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.AreaHectares).HasPrecision(18, 4).IsRequired();
            entity.Property(x => x.Method).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.MinimumMoisturePercent).HasPrecision(5, 2).IsRequired();
            entity.Property(x => x.TargetMoisturePercent).HasPrecision(5, 2).IsRequired();
            entity.Property(x => x.MaximumMoisturePercent).HasPrecision(5, 2).IsRequired();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Field>().WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.OrganizationId); entity.HasIndex(x => x.FieldId);
            entity.HasIndex(x => new { x.OrganizationId, x.FieldId, x.IsActive });
        });
        modelBuilder.Entity<IrrigationApplication>(entity =>
        {
            entity.ToTable("irrigation_applications"); entity.HasKey(x => x.Id);
            entity.Property(x => x.AreaHectaresSnapshot).HasPrecision(18, 4).IsRequired();
            entity.Property(x => x.DepthMillimeters).HasPrecision(18, 3).IsRequired();
            entity.Property(x => x.EstimatedVolumeCubicMeters).HasPrecision(18, 3).IsRequired();
            entity.Property(x => x.Source).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<IrrigationZone>().WithMany().HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Field>().WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.OrganizationId); entity.HasIndex(x => x.FieldId); entity.HasIndex(x => x.ZoneId);
            entity.HasIndex(x => new { x.OrganizationId, x.ZoneId, x.StartedAtUtc });
        });
        modelBuilder.Entity<EmissionFactor>(entity =>
        {
            entity.ToTable("emission_factors"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Unit).HasMaxLength(32).IsRequired();
            entity.Property(x => x.KgCo2ePerUnit).HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.MethodologyReference).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.OrganizationId);
            entity.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.Category, x.IsActive });
        });
        modelBuilder.Entity<EmissionActivity>(entity =>
        {
            entity.ToTable("emission_activities"); entity.HasKey(x => x.Id);
            entity.Property(x => x.FactorNameSnapshot).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CategorySnapshot).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.UnitSnapshot).HasMaxLength(32).IsRequired();
            entity.Property(x => x.FactorKgCo2ePerUnitSnapshot).HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.EmissionsKgCo2e).HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.Origin).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.DataQuality).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(240).IsRequired();
            entity.Property(x => x.SourceModule).HasMaxLength(64);
            entity.Property(x => x.SourceReferenceId).HasMaxLength(160);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Ignore(x => x.EmissionsTCo2e);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<EmissionFactor>().WithMany().HasForeignKey(x => x.EmissionFactorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Farm>().WithMany().HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Field>().WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Season>().WithMany().HasForeignKey(x => x.SeasonId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.OrganizationId);
            entity.HasIndex(x => x.EmissionFactorId);
            entity.HasIndex(x => x.FarmId); entity.HasIndex(x => x.FieldId); entity.HasIndex(x => x.SeasonId);
            entity.HasIndex(x => new { x.OrganizationId, x.ActivityDate });
            entity.HasIndex(x => new { x.OrganizationId, x.CategorySnapshot, x.ActivityDate });
            entity.HasIndex(x => new { x.OrganizationId, x.SourceModule, x.SourceReferenceId }).IsUnique().HasFilter("\"SourceModule\" IS NOT NULL AND \"SourceReferenceId\" IS NOT NULL");
        });
        modelBuilder.Entity<ExportOrder>(entity =>
        {
            entity.ToTable("export_orders"); entity.HasKey(x => x.Id);
            entity.Property(x => x.OrderNumber).HasMaxLength(80).IsRequired();
            entity.Property(x => x.BuyerName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.BuyerReference).HasMaxLength(120);
            entity.Property(x => x.DestinationCountryCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.ProductDescription).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.Unit).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(18, 4).IsRequired();
            entity.Property(x => x.ExchangeRateToBrl).HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.Incoterm).HasConversion<string>().HasMaxLength(8).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.OriginLocation).HasMaxLength(200);
            entity.Property(x => x.DestinationLocation).HasMaxLength(200);
            entity.Property(x => x.ShipmentReference).HasMaxLength(160);
            entity.Property(x => x.BookingReference).HasMaxLength(160);
            entity.Property(x => x.ContainerReference).HasMaxLength(160);
            entity.Property(x => x.Notes).HasMaxLength(1200);
            entity.Ignore(x => x.CommercialValue); entity.Ignore(x => x.EstimatedValueBrl); entity.Ignore(x => x.IsTerminal);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Farm>().WithMany().HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Field>().WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Crop>().WithMany().HasForeignKey(x => x.CropId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Season>().WithMany().HasForeignKey(x => x.SeasonId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.OrganizationId);
            entity.HasIndex(x => new { x.OrganizationId, x.OrderNumber }).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.Status, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.OrganizationId, x.DestinationCountryCode, x.CreatedAtUtc });
            entity.HasIndex(x => x.FarmId); entity.HasIndex(x => x.FieldId); entity.HasIndex(x => x.CropId); entity.HasIndex(x => x.SeasonId);
        });
        modelBuilder.Entity<ExportDocument>(entity =>
        {
            entity.ToTable("export_documents"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(48).IsRequired();
            entity.Property(x => x.CustomLabel).HasMaxLength(160);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.ReferenceNumber).HasMaxLength(160);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ExportOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.OrganizationId); entity.HasIndex(x => x.OrderId);
            entity.HasIndex(x => new { x.OrganizationId, x.OrderId, x.Status });
        });
        modelBuilder.Entity<ExportCost>(entity =>
        {
            entity.ToTable("export_costs"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.ExchangeRateToBrl).HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.AmountBrl).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ExportOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.OrganizationId); entity.HasIndex(x => x.OrderId);
            entity.HasIndex(x => new { x.OrganizationId, x.OrderId, x.IncurredOn });
        });
        modelBuilder.Entity<ExportOrderStatusEvent>(entity =>
        {
            entity.ToTable("export_order_status_events"); entity.HasKey(x => x.Id);
            entity.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ExportOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.OrganizationId); entity.HasIndex(x => x.OrderId);
            entity.HasIndex(x => new { x.OrganizationId, x.OrderId, x.OccurredOn });
        });
    }
}
