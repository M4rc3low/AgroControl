using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroControl.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SustainabilityCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "emission_factors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    KgCo2ePerUnit = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    MethodologyReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emission_factors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_emission_factors_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "emission_activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmissionFactorId = table.Column<Guid>(type: "uuid", nullable: false),
                    FactorNameSnapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CategorySnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UnitSnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FactorKgCo2ePerUnitSnapshot = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    EmissionsKgCo2e = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    ActivityDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Origin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DataQuality = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: true),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: true),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceModule = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SourceReferenceId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emission_activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_emission_activities_emission_factors_EmissionFactorId",
                        column: x => x.EmissionFactorId,
                        principalTable: "emission_factors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_emission_activities_farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_emission_activities_fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_emission_activities_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_emission_activities_seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_emission_activities_EmissionFactorId",
                table: "emission_activities",
                column: "EmissionFactorId");

            migrationBuilder.CreateIndex(
                name: "IX_emission_activities_FarmId",
                table: "emission_activities",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_emission_activities_FieldId",
                table: "emission_activities",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_emission_activities_OrganizationId",
                table: "emission_activities",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_emission_activities_OrganizationId_ActivityDate",
                table: "emission_activities",
                columns: new[] { "OrganizationId", "ActivityDate" });

            migrationBuilder.CreateIndex(
                name: "IX_emission_activities_OrganizationId_CategorySnapshot_Activit~",
                table: "emission_activities",
                columns: new[] { "OrganizationId", "CategorySnapshot", "ActivityDate" });

            migrationBuilder.CreateIndex(
                name: "IX_emission_activities_OrganizationId_SourceModule_SourceRefer~",
                table: "emission_activities",
                columns: new[] { "OrganizationId", "SourceModule", "SourceReferenceId" },
                unique: true,
                filter: "\"SourceModule\" IS NOT NULL AND \"SourceReferenceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_emission_activities_SeasonId",
                table: "emission_activities",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_emission_factors_OrganizationId",
                table: "emission_factors",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_emission_factors_OrganizationId_Category_IsActive",
                table: "emission_factors",
                columns: new[] { "OrganizationId", "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_emission_factors_OrganizationId_Name",
                table: "emission_factors",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "emission_activities");

            migrationBuilder.DropTable(
                name: "emission_factors");
        }
    }
}
