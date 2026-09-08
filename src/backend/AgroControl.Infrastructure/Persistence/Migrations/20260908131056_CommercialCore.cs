using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroControl.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CommercialCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commercial_customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TradeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TaxId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    State = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commercial_customers_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commercial_contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commercial_contacts_commercial_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "commercial_customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_commercial_contacts_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commercial_opportunities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: true),
                    CropId = table.Column<Guid>(type: "uuid", nullable: true),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExportOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpectedValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ProbabilityPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Stage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExpectedCloseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ClosedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    OwnerName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    NextStep = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_opportunities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commercial_opportunities_commercial_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "commercial_customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_opportunities_crops_CropId",
                        column: x => x.CropId,
                        principalTable: "crops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_opportunities_export_orders_ExportOrderId",
                        column: x => x.ExportOrderId,
                        principalTable: "export_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_opportunities_farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_opportunities_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_commercial_opportunities_seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_opportunity_stage_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ToStage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_opportunity_stage_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commercial_opportunity_stage_events_commercial_opportunitie~",
                        column: x => x.OpportunityId,
                        principalTable: "commercial_opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_commercial_opportunity_stage_events_organizations_Organizat~",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_contacts_CustomerId",
                table: "commercial_contacts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_contacts_OrganizationId",
                table: "commercial_contacts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_contacts_OrganizationId_CustomerId_IsActive",
                table: "commercial_contacts",
                columns: new[] { "OrganizationId", "CustomerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_customers_OrganizationId",
                table: "commercial_customers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_customers_OrganizationId_Status_Name",
                table: "commercial_customers",
                columns: new[] { "OrganizationId", "Status", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_opportunities_CropId",
                table: "commercial_opportunities",
                column: "CropId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_opportunities_CustomerId",
                table: "commercial_opportunities",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_opportunities_ExportOrderId",
                table: "commercial_opportunities",
                column: "ExportOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_opportunities_FarmId",
                table: "commercial_opportunities",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_opportunities_OrganizationId",
                table: "commercial_opportunities",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_opportunities_OrganizationId_Currency_CreatedAtU~",
                table: "commercial_opportunities",
                columns: new[] { "OrganizationId", "Currency", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_opportunities_OrganizationId_Stage_CreatedAtUtc",
                table: "commercial_opportunities",
                columns: new[] { "OrganizationId", "Stage", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_opportunities_SeasonId",
                table: "commercial_opportunities",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_opportunity_stage_events_OpportunityId",
                table: "commercial_opportunity_stage_events",
                column: "OpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_opportunity_stage_events_OrganizationId",
                table: "commercial_opportunity_stage_events",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_opportunity_stage_events_OrganizationId_Opportun~",
                table: "commercial_opportunity_stage_events",
                columns: new[] { "OrganizationId", "OpportunityId", "OccurredOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commercial_contacts");

            migrationBuilder.DropTable(
                name: "commercial_opportunity_stage_events");

            migrationBuilder.DropTable(
                name: "commercial_opportunities");

            migrationBuilder.DropTable(
                name: "commercial_customers");
        }
    }
}
