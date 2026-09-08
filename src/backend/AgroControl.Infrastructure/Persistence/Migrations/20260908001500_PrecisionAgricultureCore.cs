using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AgroControl.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgroControlDbContext))]
[Migration("20260908001500_PrecisionAgricultureCore")]
public sealed class PrecisionAgricultureCore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");
        migrationBuilder.Sql("ALTER TABLE fields ADD COLUMN IF NOT EXISTS \"Boundary\" geography(Polygon,4326) NULL;");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_fields_Boundary_Gist\" ON fields USING GIST (\"Boundary\");");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_fields_Boundary_Gist\";");
        migrationBuilder.Sql("ALTER TABLE fields DROP COLUMN IF EXISTS \"Boundary\";");
    }
}
