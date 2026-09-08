using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroControl.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgroControlDbContext))]
[Migration("20260908194500_MultiFarmIndirectFarmBackfill")]
public sealed class MultiFarmIndirectFarmBackfill : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Sprint 18 makes FarmId the canonical operational-scope key whenever a
        // record is already linked indirectly through Field or Season. Older rows
        // may predate that invariant, so normalize them before horizontal scope is
        // relied on for every module.
        migrationBuilder.Sql("""
            UPDATE stock_movements AS target
            SET "FarmId" = field."FarmId"
            FROM fields AS field
            WHERE target."FarmId" IS NULL
              AND target."FieldId" = field."Id";

            UPDATE stock_movements AS target
            SET "FarmId" = field."FarmId"
            FROM seasons AS season
            JOIN fields AS field ON field."Id" = season."FieldId"
            WHERE target."FarmId" IS NULL
              AND target."SeasonId" = season."Id";

            UPDATE financial_transactions AS target
            SET "FarmId" = field."FarmId"
            FROM fields AS field
            WHERE target."FarmId" IS NULL
              AND target."FieldId" = field."Id";

            UPDATE financial_transactions AS target
            SET "FarmId" = field."FarmId"
            FROM seasons AS season
            JOIN fields AS field ON field."Id" = season."FieldId"
            WHERE target."FarmId" IS NULL
              AND target."SeasonId" = season."Id";

            UPDATE emission_activities AS target
            SET "FarmId" = field."FarmId"
            FROM fields AS field
            WHERE target."FarmId" IS NULL
              AND target."FieldId" = field."Id";

            UPDATE emission_activities AS target
            SET "FarmId" = field."FarmId"
            FROM seasons AS season
            JOIN fields AS field ON field."Id" = season."FieldId"
            WHERE target."FarmId" IS NULL
              AND target."SeasonId" = season."Id";

            UPDATE export_orders AS target
            SET "FarmId" = field."FarmId"
            FROM fields AS field
            WHERE target."FarmId" IS NULL
              AND target."FieldId" = field."Id";

            UPDATE export_orders AS target
            SET "FarmId" = field."FarmId"
            FROM seasons AS season
            JOIN fields AS field ON field."Id" = season."FieldId"
            WHERE target."FarmId" IS NULL
              AND target."SeasonId" = season."Id";

            UPDATE commercial_opportunities AS target
            SET "FarmId" = field."FarmId"
            FROM fields AS field
            WHERE target."FarmId" IS NULL
              AND target."FieldId" = field."Id";

            UPDATE commercial_opportunities AS target
            SET "FarmId" = field."FarmId"
            FROM seasons AS season
            JOIN fields AS field ON field."Id" = season."FieldId"
            WHERE target."FarmId" IS NULL
              AND target."SeasonId" = season."Id";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally irreversible. Clearing FarmId would destroy information
        // and could reopen a horizontal-authorization ambiguity.
    }
}
