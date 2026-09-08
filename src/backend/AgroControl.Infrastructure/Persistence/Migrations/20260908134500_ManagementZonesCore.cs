using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AgroControl.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgroControlDbContext))]
[Migration("20260908134500_ManagementZonesCore")]
public sealed class ManagementZonesCore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS management_zones (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "FieldId" uuid NOT NULL REFERENCES fields("Id") ON DELETE RESTRICT,
                "Type" varchar(32) NOT NULL,
                "Name" varchar(160) NOT NULL,
                "Description" varchar(1000) NULL,
                "Classification" varchar(120) NULL,
                "Value" numeric(18,6) NULL,
                "Unit" varchar(32) NULL,
                "Geometry" geography(Polygon,4326) NOT NULL,
                "IsActive" boolean NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_management_zones_OrganizationId" ON management_zones ("OrganizationId");
            CREATE INDEX IF NOT EXISTS "IX_management_zones_FieldId" ON management_zones ("FieldId");
            CREATE INDEX IF NOT EXISTS "IX_management_zones_Organization_Type" ON management_zones ("OrganizationId", "Type");
            CREATE INDEX IF NOT EXISTS "IX_management_zones_Organization_Classification" ON management_zones ("OrganizationId", "Classification");
            CREATE INDEX IF NOT EXISTS "IX_management_zones_Geometry_Gist" ON management_zones USING GIST ("Geometry");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS management_zones;");
    }
}
