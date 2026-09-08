using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AgroControl.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgroControlDbContext))]
[Migration("20260908150000_RemoteSensingCore")]
public sealed class RemoteSensingCore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS remote_sensing_scenes (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "FieldId" uuid NOT NULL REFERENCES fields("Id") ON DELETE RESTRICT,
                "SeasonId" uuid NULL REFERENCES seasons("Id") ON DELETE RESTRICT,
                "Provider" varchar(120) NOT NULL,
                "ExternalId" varchar(200) NOT NULL,
                "Platform" varchar(32) NOT NULL,
                "AcquiredAtUtc" timestamp with time zone NOT NULL,
                "CloudCoveragePercent" numeric(6,3) NULL,
                "SpatialResolutionMeters" numeric(18,6) NULL,
                "AssetReference" varchar(2048) NULL,
                "Notes" varchar(2000) NULL,
                "Footprint" geography(Polygon,4326) NULL,
                "IsActive" boolean NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "CK_remote_sensing_scenes_CloudCoverage" CHECK ("CloudCoveragePercent" IS NULL OR ("CloudCoveragePercent" >= 0 AND "CloudCoveragePercent" <= 100)),
                CONSTRAINT "CK_remote_sensing_scenes_Resolution" CHECK ("SpatialResolutionMeters" IS NULL OR "SpatialResolutionMeters" > 0)
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "UX_remote_sensing_scenes_Organization_Provider_ExternalId"
                ON remote_sensing_scenes ("OrganizationId", "Provider", "ExternalId");
            CREATE INDEX IF NOT EXISTS "IX_remote_sensing_scenes_Organization_Field_Date"
                ON remote_sensing_scenes ("OrganizationId", "FieldId", "AcquiredAtUtc" DESC);
            CREATE INDEX IF NOT EXISTS "IX_remote_sensing_scenes_Organization_Season_Date"
                ON remote_sensing_scenes ("OrganizationId", "SeasonId", "AcquiredAtUtc" DESC);
            CREATE INDEX IF NOT EXISTS "IX_remote_sensing_scenes_Organization_Platform"
                ON remote_sensing_scenes ("OrganizationId", "Platform");
            CREATE INDEX IF NOT EXISTS "IX_remote_sensing_scenes_Footprint_Gist"
                ON remote_sensing_scenes USING GIST ("Footprint");

            CREATE TABLE IF NOT EXISTS vegetation_index_observations (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "SceneId" uuid NOT NULL REFERENCES remote_sensing_scenes("Id") ON DELETE RESTRICT,
                "FieldId" uuid NOT NULL REFERENCES fields("Id") ON DELETE RESTRICT,
                "SeasonId" uuid NULL REFERENCES seasons("Id") ON DELETE RESTRICT,
                "ManagementZoneId" uuid NULL REFERENCES management_zones("Id") ON DELETE RESTRICT,
                "IndexType" varchar(32) NOT NULL,
                "CustomIndexName" varchar(120) NULL,
                "Minimum" numeric(18,8) NOT NULL,
                "Maximum" numeric(18,8) NOT NULL,
                "Mean" numeric(18,8) NOT NULL,
                "Median" numeric(18,8) NOT NULL,
                "StandardDeviation" numeric(18,8) NOT NULL,
                "ValidCoveragePercent" numeric(6,3) NOT NULL,
                "SampleCount" bigint NULL,
                "Source" varchar(120) NOT NULL,
                "ObservedAtUtc" timestamp with time zone NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "CK_vegetation_index_observations_MinMax" CHECK ("Minimum" <= "Maximum"),
                CONSTRAINT "CK_vegetation_index_observations_Mean" CHECK ("Mean" >= "Minimum" AND "Mean" <= "Maximum"),
                CONSTRAINT "CK_vegetation_index_observations_Median" CHECK ("Median" >= "Minimum" AND "Median" <= "Maximum"),
                CONSTRAINT "CK_vegetation_index_observations_StdDev" CHECK ("StandardDeviation" >= 0),
                CONSTRAINT "CK_vegetation_index_observations_Coverage" CHECK ("ValidCoveragePercent" >= 0 AND "ValidCoveragePercent" <= 100),
                CONSTRAINT "CK_vegetation_index_observations_SampleCount" CHECK ("SampleCount" IS NULL OR "SampleCount" >= 0)
            );
            CREATE INDEX IF NOT EXISTS "IX_vegetation_index_observations_Organization_Field_Date"
                ON vegetation_index_observations ("OrganizationId", "FieldId", "ObservedAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_vegetation_index_observations_Organization_Season_Date"
                ON vegetation_index_observations ("OrganizationId", "SeasonId", "ObservedAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_vegetation_index_observations_Organization_Zone_Date"
                ON vegetation_index_observations ("OrganizationId", "ManagementZoneId", "ObservedAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_vegetation_index_observations_Organization_Index_Date"
                ON vegetation_index_observations ("OrganizationId", "IndexType", "ObservedAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_vegetation_index_observations_SceneId"
                ON vegetation_index_observations ("SceneId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS vegetation_index_observations;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS remote_sensing_scenes;");
    }
}
