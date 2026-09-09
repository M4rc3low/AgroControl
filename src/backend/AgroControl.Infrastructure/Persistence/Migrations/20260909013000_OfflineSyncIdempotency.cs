using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AgroControl.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgroControlDbContext))]
[Migration("20260909013000_OfflineSyncIdempotency")]
public sealed class OfflineSyncIdempotency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS offline_sync_operations (
                organization_id uuid NOT NULL,
                user_id uuid NOT NULL,
                operation_id uuid NOT NULL,
                farm_id uuid NOT NULL,
                entity_kind varchar(32) NOT NULL,
                entity_id uuid NOT NULL,
                operation_type varchar(16) NOT NULL,
                request_hash varchar(64) NOT NULL,
                status varchar(16) NOT NULL,
                result_json jsonb NULL,
                created_at_utc timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                completed_at_utc timestamp with time zone NULL,
                expires_at_utc timestamp with time zone NOT NULL,
                CONSTRAINT pk_offline_sync_operations
                    PRIMARY KEY (organization_id, user_id, operation_id),
                CONSTRAINT ck_offline_sync_operations_status
                    CHECK (status IN ('processing', 'completed')),
                CONSTRAINT ck_offline_sync_operations_entity_kind
                    CHECK (entity_kind IN ('field', 'season')),
                CONSTRAINT ck_offline_sync_operations_type
                    CHECK (operation_type IN ('create', 'update', 'delete'))
            );

            CREATE INDEX IF NOT EXISTS ix_offline_sync_operations_expires
                ON offline_sync_operations (expires_at_utc);
            CREATE INDEX IF NOT EXISTS ix_offline_sync_operations_scope
                ON offline_sync_operations (organization_id, farm_id, created_at_utc);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS offline_sync_operations;");
    }
}
