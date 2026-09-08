using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroControl.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgroControlDbContext))]
[Migration("20260908193000_MultiFarmTimezoneBackfillHardening")]
public sealed class MultiFarmTimezoneBackfillHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The first multi-farm migration had to provide a non-null default for legacy rows.
        // Correct the states whose operational timezone is not America/Sao_Paulo before Sprint 18 ships.
        // Amazonas defaults to Manaus; farms in the western-AM timezone can override TimeZoneId explicitly.
        migrationBuilder.Sql("""
            UPDATE farms
            SET "TimeZoneId" = CASE "StateCode"
                WHEN 'MT' THEN 'America/Cuiaba'
                WHEN 'MS' THEN 'America/Campo_Grande'
                WHEN 'AM' THEN 'America/Manaus'
                WHEN 'AC' THEN 'America/Rio_Branco'
                WHEN 'RO' THEN 'America/Porto_Velho'
                WHEN 'RR' THEN 'America/Boa_Vista'
                ELSE "TimeZoneId"
            END
            WHERE "CountryCode" = 'BR'
              AND "TimeZoneId" = 'America/Sao_Paulo'
              AND "StateCode" IN ('MT', 'MS', 'AM', 'AC', 'RO', 'RR');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data correction is intentionally not reversed: doing so could overwrite a timezone
        // that an operator explicitly corrected after the migration ran.
    }
}
