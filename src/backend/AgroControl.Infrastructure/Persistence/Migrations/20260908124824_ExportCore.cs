using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroControl.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExportCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "export_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    BuyerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BuyerReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DestinationCountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: true),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: true),
                    CropId = table.Column<Guid>(type: "uuid", nullable: true),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductDescription = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ExchangeRateToBrl = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Incoterm = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OriginLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DestinationLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContractedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    EstimatedShipmentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ActualShipmentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EstimatedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ActualDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ShipmentReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    BookingReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ContainerReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_export_orders_crops_CropId",
                        column: x => x.CropId,
                        principalTable: "crops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_export_orders_farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_export_orders_fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_export_orders_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_export_orders_seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "export_costs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ExchangeRateToBrl = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    AmountBrl = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IncurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export_costs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_export_costs_export_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "export_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_export_costs_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "export_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    CustomLabel = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    IssuedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_export_documents_export_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "export_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_export_documents_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "export_order_status_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export_order_status_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_export_order_status_events_export_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "export_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_export_order_status_events_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_export_costs_OrderId",
                table: "export_costs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_export_costs_OrganizationId",
                table: "export_costs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_export_costs_OrganizationId_OrderId_IncurredOn",
                table: "export_costs",
                columns: new[] { "OrganizationId", "OrderId", "IncurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_export_documents_OrderId",
                table: "export_documents",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_export_documents_OrganizationId",
                table: "export_documents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_export_documents_OrganizationId_OrderId_Status",
                table: "export_documents",
                columns: new[] { "OrganizationId", "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_export_order_status_events_OrderId",
                table: "export_order_status_events",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_export_order_status_events_OrganizationId",
                table: "export_order_status_events",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_export_order_status_events_OrganizationId_OrderId_OccurredOn",
                table: "export_order_status_events",
                columns: new[] { "OrganizationId", "OrderId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_export_orders_CropId",
                table: "export_orders",
                column: "CropId");

            migrationBuilder.CreateIndex(
                name: "IX_export_orders_FarmId",
                table: "export_orders",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_export_orders_FieldId",
                table: "export_orders",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_export_orders_OrganizationId",
                table: "export_orders",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_export_orders_OrganizationId_DestinationCountryCode_Created~",
                table: "export_orders",
                columns: new[] { "OrganizationId", "DestinationCountryCode", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_export_orders_OrganizationId_OrderNumber",
                table: "export_orders",
                columns: new[] { "OrganizationId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_export_orders_OrganizationId_Status_CreatedAtUtc",
                table: "export_orders",
                columns: new[] { "OrganizationId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_export_orders_SeasonId",
                table: "export_orders",
                column: "SeasonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "export_costs");

            migrationBuilder.DropTable(
                name: "export_documents");

            migrationBuilder.DropTable(
                name: "export_order_status_events");

            migrationBuilder.DropTable(
                name: "export_orders");
        }
    }
}
