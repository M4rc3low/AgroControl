from pathlib import Path


def patch_precision() -> None:
    path = Path('src/backend/AgroControl.Infrastructure/Persistence/PrecisionAgricultureRepository.cs')
    s = path.read_text()
    s = s.replace('using AgroControl.Application.PrecisionAgriculture;\n', 'using AgroControl.Application.PrecisionAgriculture;\nusing AgroControl.Application.RegionalOperations;\n', 1)
    s = s.replace(
        'public sealed class PrecisionAgricultureRepository(AgroControlDbContext dbContext) : IPrecisionAgricultureRepository',
        'public sealed class PrecisionAgricultureRepository(AgroControlDbContext dbContext, IOperationalScopeContext? operationalScope = null) : IPrecisionAgricultureRepository', 1)
    s = s.replace('const string sql = """', 'var sql = $"""')
    s = s.replace('WHERE f."OrganizationId" = @organizationId', 'WHERE f."OrganizationId" = @organizationId{FieldScopeSql("f")}')
    s = s.replace('WHERE z."OrganizationId" = @organizationId', 'WHERE z."OrganizationId" = @organizationId{ZoneScopeSql("z")}')
    marker = '    private static void AddParameter(DbCommand command, string name, object? value)\n'
    if marker not in s:
        raise SystemExit('Precision AddParameter marker not found')
    helpers = '''    private string FieldScopeSql(string alias)\n    {\n        if (operationalScope is not { IsInitialized: true, IsRestricted: true }) return string.Empty;\n        if (operationalScope.FarmIds.Count == 0) return " AND FALSE";\n        return $" AND {alias}.\\\"FarmId\\\" IN ({FarmIdSqlList()})";\n    }\n\n    private string ZoneScopeSql(string alias)\n    {\n        if (operationalScope is not { IsInitialized: true, IsRestricted: true }) return string.Empty;\n        if (operationalScope.FarmIds.Count == 0) return " AND FALSE";\n        return $" AND EXISTS (SELECT 1 FROM fields AS scope_field WHERE scope_field.\\\"OrganizationId\\\" = @organizationId AND scope_field.\\\"Id\\\" = {alias}.\\\"FieldId\\\" AND scope_field.\\\"FarmId\\\" IN ({FarmIdSqlList()}))";\n    }\n\n    private string FarmIdSqlList() => string.Join(", ", operationalScope!.FarmIds.Select(id => $"'{id:D}'::uuid"));\n\n'''
    s = s.replace(marker, helpers + marker, 1)
    path.write_text(s)


def patch_remote() -> None:
    path = Path('src/backend/AgroControl.Infrastructure/Persistence/RemoteSensingRepository.cs')
    s = path.read_text()
    s = s.replace('using AgroControl.Application.PrecisionAgriculture;\n', 'using AgroControl.Application.PrecisionAgriculture;\nusing AgroControl.Application.RegionalOperations;\n', 1)
    s = s.replace(
        'public sealed class RemoteSensingRepository(AgroControlDbContext dbContext) : IRemoteSensingRepository',
        'public sealed class RemoteSensingRepository(AgroControlDbContext dbContext, IOperationalScopeContext? operationalScope = null) : IRemoteSensingRepository', 1)
    s = s.replace('const string filter = """', 'var filter = $"""')
    s = s.replace('const string sql = """', 'var sql = $"""')
    s = s.replace('WHERE s."OrganizationId" = @organizationId', 'WHERE s."OrganizationId" = @organizationId{SceneScopeSql("s")}')
    s = s.replace('WHERE o."OrganizationId" = @organizationId', 'WHERE o."OrganizationId" = @organizationId{ObservationScopeSql("o")}')
    s = s.replace('WHERE f."OrganizationId" = @organizationId', 'WHERE f."OrganizationId" = @organizationId{FieldScopeSql("f")}')
    marker = '    private static void AddParameter(DbCommand command, string name, object? value)\n'
    if marker not in s:
        raise SystemExit('Remote AddParameter marker not found')
    helpers = '''    private string FieldScopeSql(string alias)\n    {\n        if (operationalScope is not { IsInitialized: true, IsRestricted: true }) return string.Empty;\n        if (operationalScope.FarmIds.Count == 0) return " AND FALSE";\n        return $" AND {alias}.\\\"FarmId\\\" IN ({FarmIdSqlList()})";\n    }\n\n    private string SceneScopeSql(string alias) => FieldReferenceScopeSql($"{alias}.\\\"FieldId\\\"");\n    private string ObservationScopeSql(string alias) => FieldReferenceScopeSql($"{alias}.\\\"FieldId\\\"");\n\n    private string FieldReferenceScopeSql(string fieldIdExpression)\n    {\n        if (operationalScope is not { IsInitialized: true, IsRestricted: true }) return string.Empty;\n        if (operationalScope.FarmIds.Count == 0) return " AND FALSE";\n        return $" AND EXISTS (SELECT 1 FROM fields AS scope_field WHERE scope_field.\\\"OrganizationId\\\" = @organizationId AND scope_field.\\\"Id\\\" = {fieldIdExpression} AND scope_field.\\\"FarmId\\\" IN ({FarmIdSqlList()}))";\n    }\n\n    private string FarmIdSqlList() => string.Join(", ", operationalScope!.FarmIds.Select(id => $"'{id:D}'::uuid"));\n\n'''
    s = s.replace(marker, helpers + marker, 1)
    path.write_text(s)


def patch_raster() -> None:
    path = Path('src/backend/AgroControl.Infrastructure/Persistence/RasterProcessingRepository.cs')
    s = path.read_text()
    s = s.replace('using AgroControl.Application.PrecisionAgriculture;\n', 'using AgroControl.Application.PrecisionAgriculture;\nusing AgroControl.Application.RegionalOperations;\n', 1)
    s = s.replace(
        'public sealed class RasterProcessingRepository(AgroControlDbContext dbContext) : IRasterProcessingRepository',
        'public sealed class RasterProcessingRepository(AgroControlDbContext dbContext, IOperationalScopeContext? operationalScope = null) : IRasterProcessingRepository', 1)
    s = s.replace(
        'RunSelect + " WHERE r.\\\"OrganizationId\\\" = @organizationId AND r.\\\"ProcessingKey\\\" = @processingKey;"',
        'RunSelect + $" WHERE r.\\\"OrganizationId\\\" = @organizationId AND r.\\\"ProcessingKey\\\" = @processingKey{RunScopeSql("r")};"')
    s = s.replace(
        'RunSelect + " WHERE r.\\\"OrganizationId\\\" = @organizationId AND r.\\\"SceneId\\\" = @sceneId ORDER BY r.\\\"RequestedAtUtc\\\" DESC;"',
        'RunSelect + $" WHERE r.\\\"OrganizationId\\\" = @organizationId AND r.\\\"SceneId\\\" = @sceneId{RunScopeSql("r")} ORDER BY r.\\\"RequestedAtUtc\\\" DESC;"')
    s = s.replace(
        'ResultSelect + " WHERE z.\\\"OrganizationId\\\" = @organizationId AND z.\\\"RunId\\\" = @runId ORDER BY z.\\\"ManagementZoneId\\\" NULLS FIRST, z.\\\"Id\\\";"',
        'ResultSelect + $" WHERE z.\\\"OrganizationId\\\" = @organizationId AND z.\\\"RunId\\\" = @runId{ResultScopeSql("z")} ORDER BY z.\\\"ManagementZoneId\\\" NULLS FIRST, z.\\\"Id\\\";"')
    s = s.replace(
        'QueryRunSingleAsync(RunSelect + " WHERE r.\\\"OrganizationId\\\" = @organizationId AND r.\\\"Id\\\" = @runId;",',
        'QueryRunSingleAsync(RunSelect + $" WHERE r.\\\"OrganizationId\\\" = @organizationId AND r.\\\"Id\\\" = @runId{RunScopeSql("r")};",')
    s = s.replace('const string filter = """', 'var filter = $"""')
    s = s.replace('WHERE z."OrganizationId" = @organizationId\n', 'WHERE z."OrganizationId" = @organizationId{ResultScopeSql("z")}\n', 1)
    s = s.replace(
        'WHERE s."OrganizationId" = @organizationId AND s."Id" = @sceneId AND s."IsActive";',
        'WHERE s."OrganizationId" = @organizationId AND s."Id" = @sceneId AND s."IsActive"{SceneScopeSql("s")};', 1)
    s = s.replace('command.CommandText = """\n                        INSERT INTO raster_products', 'command.CommandText = $"""\n                        INSERT INTO raster_products', 1)
    s = s.replace('UPDATE raster_processing_runs SET "Status" = \'Processing\'', 'UPDATE raster_processing_runs AS r SET "Status" = \'Processing\'', 1)
    s = s.replace('WHERE "OrganizationId" = @organizationId AND "Id" = @runId AND "Status" = \'Pending\';', 'WHERE r."OrganizationId" = @organizationId AND r."Id" = @runId AND r."Status" = \'Pending\'{RunScopeSql("r")};', 1)
    s = s.replace('UPDATE raster_processing_runs\n            SET "Status" = \'Failed\'', 'UPDATE raster_processing_runs AS r\n            SET "Status" = \'Failed\'', 1)
    s = s.replace('WHERE "OrganizationId" = @organizationId AND "Id" = @runId AND "Status" IN (\'Pending\', \'Processing\');', 'WHERE r."OrganizationId" = @organizationId AND r."Id" = @runId AND r."Status" IN (\'Pending\', \'Processing\'){RunScopeSql("r")};', 1)
    s = s.replace('SELECT "ProductId" FROM raster_processing_runs\n                        WHERE "OrganizationId" = @organizationId AND "Id" = @runId AND "Status" = \'Processing\'', 'SELECT r."ProductId" FROM raster_processing_runs AS r\n                        WHERE r."OrganizationId" = @organizationId AND r."Id" = @runId AND r."Status" = \'Processing\'{RunScopeSql("r")}', 1)
    s = s.replace('await ExecuteNonQueryAsync("""\n            UPDATE raster_processing_runs AS r SET', 'await ExecuteNonQueryAsync($"""\n            UPDATE raster_processing_runs AS r SET', 1)
    s = s.replace('await ExecuteNonQueryAsync("""\n            UPDATE raster_processing_runs AS r\n', 'await ExecuteNonQueryAsync($"""\n            UPDATE raster_processing_runs AS r\n', 1)
    s = s.replace('command.CommandText = """\n                        SELECT r."ProductId"', 'command.CommandText = $"""\n                        SELECT r."ProductId"', 1)
    marker = '    private static void AddParameter(DbCommand command, string name, object? value)\n'
    if marker not in s:
        raise SystemExit('Raster AddParameter marker not found')
    helpers = '''    private string SceneScopeSql(string alias) => FieldReferenceScopeSql($"{alias}.\\\"FieldId\\\"");\n    private string ResultScopeSql(string alias) => FieldReferenceScopeSql($"{alias}.\\\"FieldId\\\"");\n\n    private string RunScopeSql(string alias)\n    {\n        if (operationalScope is not { IsInitialized: true, IsRestricted: true }) return string.Empty;\n        if (operationalScope.FarmIds.Count == 0) return " AND FALSE";\n        return $" AND EXISTS (SELECT 1 FROM remote_sensing_scenes AS scope_scene JOIN fields AS scope_field ON scope_field.\\\"Id\\\" = scope_scene.\\\"FieldId\\\" AND scope_field.\\\"OrganizationId\\\" = scope_scene.\\\"OrganizationId\\\" WHERE scope_scene.\\\"OrganizationId\\\" = @organizationId AND scope_scene.\\\"Id\\\" = {alias}.\\\"SceneId\\\" AND scope_field.\\\"FarmId\\\" IN ({FarmIdSqlList()}))";\n    }\n\n    private string FieldReferenceScopeSql(string fieldIdExpression)\n    {\n        if (operationalScope is not { IsInitialized: true, IsRestricted: true }) return string.Empty;\n        if (operationalScope.FarmIds.Count == 0) return " AND FALSE";\n        return $" AND EXISTS (SELECT 1 FROM fields AS scope_field WHERE scope_field.\\\"OrganizationId\\\" = @organizationId AND scope_field.\\\"Id\\\" = {fieldIdExpression} AND scope_field.\\\"FarmId\\\" IN ({FarmIdSqlList()}))";\n    }\n\n    private string FarmIdSqlList() => string.Join(", ", operationalScope!.FarmIds.Select(id => $"'{id:D}'::uuid"));\n\n'''
    s = s.replace(marker, helpers + marker, 1)
    path.write_text(s)


patch_precision()
patch_remote()
patch_raster()
