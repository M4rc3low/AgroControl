using AgroControl.Domain.Modules.Identity;
using AgroControl.Domain.Modules.Subscriptions;
using AgroControl.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
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
            b.Property<OrganizationRole>("Role")
                .HasConversion(new EnumToStringConverter<OrganizationRole>())
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");
            b.HasKey("OrganizationId", "UserId");
            b.HasIndex("UserId");
            b.ToTable("organization_memberships");
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Subscriptions.Subscription", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<DateTime?>("EndsAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("OrganizationId").HasColumnType("uuid");
            b.Property<PlanCode>("Plan")
                .HasConversion(new EnumToStringConverter<PlanCode>())
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");
            b.Property<DateTime>("StartedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<SubscriptionStatus>("Status")
                .HasConversion(new EnumToStringConverter<SubscriptionStatus>())
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");
            b.HasKey("Id");
            b.HasIndex("OrganizationId").IsUnique();
            b.ToTable("subscriptions");
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Subscriptions.OrganizationModuleEntitlement", b =>
        {
            b.Property<Guid>("OrganizationId").HasColumnType("uuid");
            b.Property<ModuleKey>("Module")
                .HasConversion(new EnumToStringConverter<ModuleKey>())
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");
            b.Property<bool>("IsEnabled").HasColumnType("boolean");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("OrganizationId", "Module");
            b.ToTable("organization_module_entitlements");
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Identity.OrganizationMembership", b =>
        {
            b.HasOne("AgroControl.Domain.Modules.Organizations.Organization", null)
                .WithMany()
                .HasForeignKey("OrganizationId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("AgroControl.Domain.Modules.Identity.User", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Subscriptions.Subscription", b =>
        {
            b.HasOne("AgroControl.Domain.Modules.Organizations.Organization", null)
                .WithMany()
                .HasForeignKey("OrganizationId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("AgroControl.Domain.Modules.Subscriptions.OrganizationModuleEntitlement", b =>
        {
            b.HasOne("AgroControl.Domain.Modules.Organizations.Organization", null)
                .WithMany()
                .HasForeignKey("OrganizationId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
#pragma warning restore 612, 618
    }
}
