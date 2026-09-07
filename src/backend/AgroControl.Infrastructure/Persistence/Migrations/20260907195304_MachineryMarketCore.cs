using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroControl.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MachineryMarketCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commodities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DefaultCurrency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DefaultUnit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commodities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commodities_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "machines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    InternalCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CurrentHourMeter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_machines_farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_machines_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "market_quotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommodityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Unit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Source = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    QuotedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_market_quotes_commodities_CommodityId",
                        column: x => x.CommodityId,
                        principalTable: "commodities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_market_quotes_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "price_alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommodityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Direction = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_price_alerts_commodities_CommodityId",
                        column: x => x.CommodityId,
                        principalTable: "commodities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_price_alerts_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "machine_fuelings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Liters = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    HourMeter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_fuelings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_machine_fuelings_machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_machine_fuelings_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "machine_hour_meter_readings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Hours = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_hour_meter_readings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_machine_hour_meter_readings_machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_machine_hour_meter_readings_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "machine_maintenance_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    PerformedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    HourMeter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PartsCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LaborCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NextMaintenanceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NextMaintenanceHourMeter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_maintenance_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_machine_maintenance_records_machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_machine_maintenance_records_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commodities_OrganizationId",
                table: "commodities",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_commodities_OrganizationId_Symbol",
                table: "commodities",
                columns: new[] { "OrganizationId", "Symbol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_machine_fuelings_MachineId",
                table: "machine_fuelings",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_machine_fuelings_OrganizationId",
                table: "machine_fuelings",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_machine_fuelings_OrganizationId_MachineId_OccurredAtUtc",
                table: "machine_fuelings",
                columns: new[] { "OrganizationId", "MachineId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_machine_hour_meter_readings_MachineId",
                table: "machine_hour_meter_readings",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_machine_hour_meter_readings_OrganizationId",
                table: "machine_hour_meter_readings",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_machine_hour_meter_readings_OrganizationId_MachineId_Occurr~",
                table: "machine_hour_meter_readings",
                columns: new[] { "OrganizationId", "MachineId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_machine_maintenance_records_MachineId",
                table: "machine_maintenance_records",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_machine_maintenance_records_NextMaintenanceDate",
                table: "machine_maintenance_records",
                column: "NextMaintenanceDate");

            migrationBuilder.CreateIndex(
                name: "IX_machine_maintenance_records_OrganizationId",
                table: "machine_maintenance_records",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_machine_maintenance_records_OrganizationId_MachineId_Perfor~",
                table: "machine_maintenance_records",
                columns: new[] { "OrganizationId", "MachineId", "PerformedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_machines_FarmId",
                table: "machines",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_machines_OrganizationId",
                table: "machines",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_machines_OrganizationId_InternalCode",
                table: "machines",
                columns: new[] { "OrganizationId", "InternalCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_machines_OrganizationId_Status",
                table: "machines",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_market_quotes_CommodityId",
                table: "market_quotes",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_market_quotes_OrganizationId",
                table: "market_quotes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_market_quotes_OrganizationId_CommodityId_QuotedAtUtc",
                table: "market_quotes",
                columns: new[] { "OrganizationId", "CommodityId", "QuotedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_price_alerts_CommodityId",
                table: "price_alerts",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_price_alerts_OrganizationId",
                table: "price_alerts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_price_alerts_OrganizationId_CommodityId_IsActive",
                table: "price_alerts",
                columns: new[] { "OrganizationId", "CommodityId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "machine_fuelings");

            migrationBuilder.DropTable(
                name: "machine_hour_meter_readings");

            migrationBuilder.DropTable(
                name: "machine_maintenance_records");

            migrationBuilder.DropTable(
                name: "market_quotes");

            migrationBuilder.DropTable(
                name: "price_alerts");

            migrationBuilder.DropTable(
                name: "machines");

            migrationBuilder.DropTable(
                name: "commodities");
        }
    }
}
