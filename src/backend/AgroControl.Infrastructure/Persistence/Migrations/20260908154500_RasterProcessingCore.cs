using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AgroControl.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgroControlDbContext))]
[Migration("20260908154500_RasterProcessingCore")]
public sealed class RasterProcessingCore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS raster_products (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "SceneId" uuid NOT NULL REFERENCES remote_sensing_scenes("Id") ON DELETE RESTRICT,
                "ProductType" varchar(32) NOT NULL,
                "CustomProductName" varchar(120) NULL,
                "AssetReference" varchar(2048) NOT NULL,
                "Band" integer NOT NULL,
                "Crs" varchar(120) NULL,
                "ResolutionX" numeric(18,8) NULL,
                "ResolutionY" numeric(18,8) NULL,
                "Nodata" numeric(24,8) NULL,
                "Width" integer NULL,
                "Height" integer NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "CK_raster_products_Band" CHECK ("Band" > 0),
                CONSTRAINT "CK_raster_products_Custom" CHECK (("ProductType" = 'Custom' AND "CustomProductName" IS NOT NULL) OR "ProductType" <> 'Custom'),
                CONSTRAINT "CK_raster_products_Size" CHECK (("Width" IS NULL OR "Width" > 0) AND ("Height" IS NULL OR "Height" > 0))
            );
            CREATE INDEX IF NOT EXISTS "IX_raster_products_Organization_Scene" ON raster_products ("OrganizationId", "SceneId", "CreatedAtUtc" DESC);

            CREATE TABLE IF NOT EXISTS raster_processing_runs (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "ProductId" uuid NOT NULL REFERENCES raster_products("Id") ON DELETE RESTRICT,
                "SceneId" uuid NOT NULL REFERENCES remote_sensing_scenes("Id") ON DELETE RESTRICT,
                "ProcessingKey" varchar(160) NOT NULL,
                "Status" varchar(32) NOT NULL,
                "IncludeManagementZones" boolean NOT NULL,
                "RequestedAtUtc" timestamp with time zone NOT NULL,
                "StartedAtUtc" timestamp with time zone NULL,
                "CompletedAtUtc" timestamp with time zone NULL,
                "FailureMessage" varchar(500) NULL,
                CONSTRAINT "CK_raster_processing_runs_Status" CHECK ("Status" IN ('Pending','Processing','Succeeded','Failed'))
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "UX_raster_processing_runs_Organization_Key" ON raster_processing_runs ("OrganizationId", "ProcessingKey");
            CREATE INDEX IF NOT EXISTS "IX_raster_processing_runs_Organization_Scene" ON raster_processing_runs ("OrganizationId", "SceneId", "RequestedAtUtc" DESC);
            CREATE INDEX IF NOT EXISTS "IX_raster_processing_runs_Organization_Status" ON raster_processing_runs ("OrganizationId", "Status");

            CREATE TABLE IF NOT EXISTS raster_zonal_results (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL REFERENCES organizations("Id") ON DELETE CASCADE,
                "RunId" uuid NOT NULL REFERENCES raster_processing_runs("Id") ON DELETE RESTRICT,
                "ProductId" uuid NOT NULL REFERENCES raster_products("Id") ON DELETE RESTRICT,
                "ObservationId" uuid NOT NULL REFERENCES vegetation_index_observations("Id") ON DELETE RESTRICT,
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
                "SampleCount" bigint NOT NULL,
                "Source" varchar(120) NOT NULL,
                "ObservedAtUtc" timestamp with time zone NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "CK_raster_zonal_results_MinMax" CHECK ("Minimum" <= "Maximum"),
                CONSTRAINT "CK_raster_zonal_results_StdDev" CHECK ("StandardDeviation" >= 0),
                CONSTRAINT "CK_raster_zonal_results_Coverage" CHECK ("ValidCoveragePercent" >= 0 AND "ValidCoveragePercent" <= 100),
                CONSTRAINT "CK_raster_zonal_results_SampleCount" CHECK ("SampleCount" >= 0)
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "UX_raster_zonal_results_Observation" ON raster_zonal_results ("ObservationId");
            CREATE UNIQUE INDEX IF NOT EXISTS "UX_raster_zonal_results_Run_Context" ON raster_zonal_results
                ("RunId", COALESCE("ManagementZoneId", '00000000-0000-0000-0000-000000000000'::uuid));
            CREATE INDEX IF NOT EXISTS "IX_raster_zonal_results_Organization_Field_Date" ON raster_zonal_results ("OrganizationId", "FieldId", "ObservedAtUtc" DESC);
            CREATE INDEX IF NOT EXISTS "IX_raster_zonal_results_Organization_Season_Date" ON raster_zonal_results ("OrganizationId", "SeasonId", "ObservedAtUtc" DESC);
            CREATE INDEX IF NOT EXISTS "IX_raster_zonal_results_Organization_Zone_Date" ON raster_zonal_results ("OrganizationId", "ManagementZoneId", "ObservedAtUtc" DESC);
            CREATE INDEX IF NOT EXISTS "IX_raster_zonal_results_Organization_Index_Date" ON raster_zonal_results ("OrganizationId", "IndexType", "ObservedAtUtc" DESC);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS raster_zonal_results;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS raster_processing_runs;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS raster_products;");
    }
}
