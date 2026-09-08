using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroControl.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IrrigationCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "irrigation_zones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    AreaHectares = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MinimumMoisturePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TargetMoisturePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    MaximumMoisturePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TelemetryDeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_irrigation_zones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_irrigation_zones_fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_irrigation_zones_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "irrigation_applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaHectaresSnapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DepthMillimeters = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    EstimatedVolumeCubicMeters = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_irrigation_applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_irrigation_applications_fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_irrigation_applications_irrigation_zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "irrigation_zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_irrigation_applications_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_irrigation_applications_FieldId",
                table: "irrigation_applications",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_irrigation_applications_OrganizationId",
                table: "irrigation_applications",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_irrigation_applications_OrganizationId_ZoneId_StartedAtUtc",
                table: "irrigation_applications",
                columns: new[] { "OrganizationId", "ZoneId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_irrigation_applications_ZoneId",
                table: "irrigation_applications",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_irrigation_zones_FieldId",
                table: "irrigation_zones",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_irrigation_zones_OrganizationId",
                table: "irrigation_zones",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_irrigation_zones_OrganizationId_FieldId_IsActive",
                table: "irrigation_zones",
                columns: new[] { "OrganizationId", "FieldId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "irrigation_applications");

            migrationBuilder.DropTable(
                name: "irrigation_zones");
        }
    }
}
