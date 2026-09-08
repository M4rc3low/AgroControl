using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroControl.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MultiFarmRegionalOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "farms",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "farms",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "farms",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MunicipalityCode",
                table: "farms",
                type: "character varying(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OperationalRegionId",
                table: "farms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "farms",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateCode",
                table: "farms",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "farms",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "farm_access_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScopeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_farm_access_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_farm_access_assignments_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_farm_access_assignments_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "operational_regions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operational_regions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_operational_regions_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_farms_OperationalRegionId",
                table: "farms",
                column: "OperationalRegionId");

            migrationBuilder.CreateIndex(
                name: "IX_farms_OrganizationId_CountryCode_StateCode",
                table: "farms",
                columns: new[] { "OrganizationId", "CountryCode", "StateCode" });

            migrationBuilder.CreateIndex(
                name: "IX_farm_access_assignments_OrganizationId",
                table: "farm_access_assignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_farm_access_assignments_OrganizationId_UserId_ScopeKey",
                table: "farm_access_assignments",
                columns: new[] { "OrganizationId", "UserId", "ScopeKey" },
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_farm_access_assignments_TargetId",
                table: "farm_access_assignments",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_farm_access_assignments_UserId",
                table: "farm_access_assignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_operational_regions_OrganizationId",
                table: "operational_regions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_operational_regions_OrganizationId_Code",
                table: "operational_regions",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_operational_regions_OrganizationId_IsActive",
                table: "operational_regions",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_farms_operational_regions_OperationalRegionId",
                table: "farms",
                column: "OperationalRegionId",
                principalTable: "operational_regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
            migrationBuilder.Sql("""
                UPDATE farms
                SET "CountryCode" = 'BR'
                WHERE "CountryCode" = '';

                UPDATE farms
                SET "StateCode" = UPPER("State")
                WHERE "StateCode" IS NULL AND "State" IS NOT NULL;

                UPDATE farms
                SET "TimeZoneId" = 'America/Sao_Paulo'
                WHERE "TimeZoneId" = '';

                INSERT INTO farm_access_assignments
                    ("Id", "OrganizationId", "UserId", "ScopeType", "TargetId", "ScopeKey", "IsActive", "CreatedByUserId", "CreatedAtUtc", "RevokedByUserId", "RevokedAtUtc")
                SELECT gen_random_uuid(), m."OrganizationId", m."UserId", 'AllFarms', NULL, 'all', TRUE, m."UserId", NOW(), NULL, NULL
                FROM organization_memberships m
                WHERE m."Role" IN ('Owner', 'Admin')
                  AND NOT EXISTS (
                      SELECT 1 FROM farm_access_assignments a
                      WHERE a."OrganizationId" = m."OrganizationId"
                        AND a."UserId" = m."UserId"
                        AND a."ScopeKey" = 'all'
                        AND a."IsActive" = TRUE
                  );
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_farms_operational_regions_OperationalRegionId",
                table: "farms");

            migrationBuilder.DropTable(
                name: "farm_access_assignments");

            migrationBuilder.DropTable(
                name: "operational_regions");

            migrationBuilder.DropIndex(
                name: "IX_farms_OperationalRegionId",
                table: "farms");

            migrationBuilder.DropIndex(
                name: "IX_farms_OrganizationId_CountryCode_StateCode",
                table: "farms");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "farms");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "farms");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "farms");

            migrationBuilder.DropColumn(
                name: "MunicipalityCode",
                table: "farms");

            migrationBuilder.DropColumn(
                name: "OperationalRegionId",
                table: "farms");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "farms");

            migrationBuilder.DropColumn(
                name: "StateCode",
                table: "farms");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "farms");
        }
    }
}
