using AgroControl.Domain.Modules.Seasons;

namespace AgroControl.Application.Production;

public sealed record FarmDto(
    Guid Id,
    string Name,
    decimal TotalAreaHectares,
    string? City,
    string? State,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateFarmCommand(string Name, decimal TotalAreaHectares, string? City, string? State);
public sealed record UpdateFarmCommand(string Name, decimal TotalAreaHectares, string? City, string? State);

public sealed record FieldDto(
    Guid Id,
    Guid FarmId,
    string Name,
    decimal AreaHectares,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateFieldCommand(Guid FarmId, string Name, decimal AreaHectares);
public sealed record UpdateFieldCommand(Guid FarmId, string Name, decimal AreaHectares);

public sealed record CropDto(
    Guid Id,
    string Name,
    string? Variety,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateCropCommand(string Name, string? Variety);
public sealed record UpdateCropCommand(string Name, string? Variety);

public sealed record SeasonDto(
    Guid Id,
    Guid FieldId,
    Guid CropId,
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? ExpectedYieldPerHectare,
    decimal? ActualYieldPerHectare,
    string Status,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateSeasonCommand(
    Guid FieldId,
    Guid CropId,
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? ExpectedYieldPerHectare);

public sealed record UpdateSeasonCommand(
    Guid FieldId,
    Guid CropId,
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? ExpectedYieldPerHectare,
    decimal? ActualYieldPerHectare,
    SeasonStatus Status);
