using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroControl.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinanceCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cost_centers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cost_centers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cost_centers_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "financial_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_financial_categories_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "financial_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Counterparty = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CompetenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SettledOn = table.Column<DateOnly>(type: "date", nullable: true),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: true),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: true),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_financial_transactions_cost_centers_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "cost_centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_financial_transactions_farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_financial_transactions_fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_financial_transactions_financial_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "financial_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_financial_transactions_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_financial_transactions_seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cost_centers_OrganizationId",
                table: "cost_centers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_cost_centers_OrganizationId_Name",
                table: "cost_centers",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_financial_categories_OrganizationId",
                table: "financial_categories",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_categories_OrganizationId_Name",
                table: "financial_categories",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_CategoryId",
                table: "financial_transactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_CostCenterId",
                table: "financial_transactions",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_FarmId",
                table: "financial_transactions",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_FieldId",
                table: "financial_transactions",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_OrganizationId",
                table: "financial_transactions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_OrganizationId_DueDate",
                table: "financial_transactions",
                columns: new[] { "OrganizationId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_OrganizationId_Type_Status_Competenc~",
                table: "financial_transactions",
                columns: new[] { "OrganizationId", "Type", "Status", "CompetenceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_SeasonId",
                table: "financial_transactions",
                column: "SeasonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financial_transactions");

            migrationBuilder.DropTable(
                name: "cost_centers");

            migrationBuilder.DropTable(
                name: "financial_categories");
        }
    }
}
