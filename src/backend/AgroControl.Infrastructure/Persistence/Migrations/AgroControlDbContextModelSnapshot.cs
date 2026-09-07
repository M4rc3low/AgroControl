using AgroControl.Domain.Modules.Identity;
using AgroControl.Domain.Modules.Seasons;
using AgroControl.Domain.Modules.Subscriptions;
using AgroControl.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AgroControl.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgroControlDbContext))]
public partial class AgroControlDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0");

        modelBuilder.Entity("AgroControl.Domain.Modules.Organizations.Organization", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("Name").IsRequired().HasMaxLength(160).HasColumnType("character varying(160)");
            b.Property<string>("Slug").IsRequired().HasMaxLength(180).HasColumnType("character varying(180)");
            b.HasKey("Id");
            b.HasIndex("Slug").IsUnique();
            b.ToTable("organizations");
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Identity.User", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("DisplayName").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
            b.Property<string>("Email").IsRequired().HasMaxLength(254).HasColumnType("character varying(254)");
            b.Property<bool>("IsActive").HasColumnType("boolean");
            b.Property<string>("PasswordHash").IsRequired().HasMaxLength(512).HasColumnType("character varying(512)");
            b.HasKey("Id");
            b.HasIndex("Email").IsUnique();
            b.ToTable("users");
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Identity.OrganizationMembership", b =>
        {
            b.Property<Guid>("OrganizationId").HasColumnType("uuid");
            b.Property<Guid>("UserId").HasColumnType("uuid");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<OrganizationRole>("Role").HasConversion(new EnumToStringConverter<OrganizationRole>()).HasMaxLength(32).HasColumnType("character varying(32)");
            b.HasKey("OrganizationId", "UserId");
            b.HasIndex("UserId");
            b.ToTable("organization_memberships");
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Subscriptions.Subscription", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<DateTime?>("EndsAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("OrganizationId").HasColumnType("uuid");
            b.Property<PlanCode>("Plan").HasConversion(new EnumToStringConverter<PlanCode>()).HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<DateTime>("StartedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<SubscriptionStatus>("Status").HasConversion(new EnumToStringConverter<SubscriptionStatus>()).HasMaxLength(32).HasColumnType("character varying(32)");
            b.HasKey("Id");
            b.HasIndex("OrganizationId").IsUnique();
            b.ToTable("subscriptions");
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Subscriptions.OrganizationModuleEntitlement", b =>
        {
            b.Property<Guid>("OrganizationId").HasColumnType("uuid");
            b.Property<ModuleKey>("Module").HasConversion(new EnumToStringConverter<ModuleKey>()).HasMaxLength(64).HasColumnType("character varying(64)");
            b.Property<bool>("IsEnabled").HasColumnType("boolean");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("OrganizationId", "Module");
            b.ToTable("organization_module_entitlements");
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Farms.Farm", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<string>("City").HasMaxLength(120).HasColumnType("character varying(120)");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<bool>("IsActive").HasColumnType("boolean");
            b.Property<string>("Name").IsRequired().HasMaxLength(160).HasColumnType("character varying(160)");
            b.Property<Guid>("OrganizationId").HasColumnType("uuid");
            b.Property<string>("State").HasMaxLength(80).HasColumnType("character varying(80)");
            b.Property<decimal>("TotalAreaHectares").HasPrecision(18, 4).HasColumnType("numeric(18,4)");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("OrganizationId");
            b.HasIndex("OrganizationId", "Name");
            b.ToTable("farms");
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Fields.Field", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<decimal>("AreaHectares").HasPrecision(18, 4).HasColumnType("numeric(18,4)");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("FarmId").HasColumnType("uuid");
            b.Property<bool>("IsActive").HasColumnType("boolean");
            b.Property<string>("Name").IsRequired().HasMaxLength(160).HasColumnType("character varying(160)");
            b.Property<Guid>("OrganizationId").HasColumnType("uuid");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("FarmId");
            b.HasIndex("OrganizationId");
            b.HasIndex("OrganizationId", "Name");
            b.ToTable("fields");
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Crops.Crop", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<bool>("IsActive").HasColumnType("boolean");
            b.Property<string>("Name").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
            b.Property<Guid>("OrganizationId").HasColumnType("uuid");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("Variety").HasMaxLength(120).HasColumnType("character varying(120)");
            b.HasKey("Id");
            b.HasIndex("OrganizationId");
            b.HasIndex("OrganizationId", "Name", "Variety");
            b.ToTable("crops");
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Seasons.Season", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<decimal?>("ActualYieldPerHectare").HasPrecision(18, 4).HasColumnType("numeric(18,4)");
            b.Property<Guid>("CropId").HasColumnType("uuid");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateOnly?>("EndDate").HasColumnType("date");
            b.Property<decimal?>("ExpectedYieldPerHectare").HasPrecision(18, 4).HasColumnType("numeric(18,4)");
            b.Property<Guid>("FieldId").HasColumnType("uuid");
            b.Property<bool>("IsActive").HasColumnType("boolean");
            b.Property<string>("Name").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
            b.Property<Guid>("OrganizationId").HasColumnType("uuid");
            b.Property<DateOnly>("StartDate").HasColumnType("date");
            b.Property<SeasonStatus>("Status").HasConversion(new EnumToStringConverter<SeasonStatus>()).HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("CropId");
            b.HasIndex("FieldId");
            b.HasIndex("OrganizationId");
            b.HasIndex("OrganizationId", "Status");
            b.ToTable("seasons");
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Identity.OrganizationMembership", b =>
        {
            b.HasOne("AgroControl.Domain.Modules.Organizations.Organization", null).WithMany().HasForeignKey("OrganizationId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            b.HasOne("AgroControl.Domain.Modules.Identity.User", null).WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Subscriptions.Subscription", b =>
        {
            b.HasOne("AgroControl.Domain.Modules.Organizations.Organization", null).WithMany().HasForeignKey("OrganizationId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Subscriptions.OrganizationModuleEntitlement", b =>
        {
            b.HasOne("AgroControl.Domain.Modules.Organizations.Organization", null).WithMany().HasForeignKey("OrganizationId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Farms.Farm", b =>
        {
            b.HasOne("AgroControl.Domain.Modules.Organizations.Organization", null).WithMany().HasForeignKey("OrganizationId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Fields.Field", b =>
        {
            b.HasOne("AgroControl.Domain.Modules.Farms.Farm", null).WithMany().HasForeignKey("FarmId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            b.HasOne("AgroControl.Domain.Modules.Organizations.Organization", null).WithMany().HasForeignKey("OrganizationId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Crops.Crop", b =>
        {
            b.HasOne("AgroControl.Domain.Modules.Organizations.Organization", null).WithMany().HasForeignKey("OrganizationId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Seasons.Season", b =>
        {
            b.HasOne("AgroControl.Domain.Modules.Crops.Crop", null).WithMany().HasForeignKey("CropId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            b.HasOne("AgroControl.Domain.Modules.Fields.Field", null).WithMany().HasForeignKey("FieldId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            b.HasOne("AgroControl.Domain.Modules.Organizations.Organization", null).WithMany().HasForeignKey("OrganizationId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });
#pragma warning restore 612, 618
    }
}
