using AgroControl.Domain.Platform;

namespace AgroControl.Application.Platform;

public sealed record ModuleDefinition(
    ModuleKey Key,
    string Name,
    ModuleStatus Status,
    string Description);
