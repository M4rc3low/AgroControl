using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AgroControl.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgroControlDbContext))]
[Migration("20260907010000_ProductionCore")]
public sealed class ProductionCore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "crops",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Variety = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_crops", x => x.Id);
                table.ForeignKey(
                    name: "FK_crops_organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "farms",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                TotalAreaHectares = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                State = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_farms", x => x.Id);
                table.ForeignKey(
                    name: "FK_farms_organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "fields",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                AreaHectares = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_fields", x => x.Id);
                table.ForeignKey(
                    name: "FK_fields_farms_FarmId",
                    column: x => x.FarmId,
                    principalTable: "farms",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_fields_organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "seasons",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                CropId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                ExpectedYieldPerHectare = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                ActualYieldPerHectare = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_seasons", x => x.Id);
                table.ForeignKey(
                    name: "FK_seasons_crops_CropId",
                    column: x => x.CropId,
                    principalTable: "crops",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_seasons_fields_FieldId",
                    column: x => x.FieldId,
                    principalTable: "fields",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_seasons_organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_crops_OrganizationId", "crops", "OrganizationId");
        migrationBuilder.CreateIndex("IX_crops_OrganizationId_Name_Variety", "crops", new[] { "OrganizationId", "Name", "Variety" });
        migrationBuilder.CreateIndex("IX_farms_OrganizationId", "farms", "OrganizationId");
        migrationBuilder.CreateIndex("IX_farms_OrganizationId_Name", "farms", new[] { "OrganizationId", "Name" });
        migrationBuilder.CreateIndex("IX_fields_FarmId", "fields", "FarmId");
        migrationBuilder.CreateIndex("IX_fields_OrganizationId", "fields", "OrganizationId");
        migrationBuilder.CreateIndex("IX_fields_OrganizationId_Name", "fields", new[] { "OrganizationId", "Name" });
        migrationBuilder.CreateIndex("IX_seasons_CropId", "seasons", "CropId");
        migrationBuilder.CreateIndex("IX_seasons_FieldId", "seasons", "FieldId");
        migrationBuilder.CreateIndex("IX_seasons_OrganizationId", "seasons", "OrganizationId");
        migrationBuilder.CreateIndex("IX_seasons_OrganizationId_Status", "seasons", new[] { "OrganizationId", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("seasons");
        migrationBuilder.DropTable("crops");
        migrationBuilder.DropTable("fields");
        migrationBuilder.DropTable("farms");
    }
}
