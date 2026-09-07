using AgroControl.Application.Common;
using AgroControl.Domain.Modules.Crops;

namespace AgroControl.Application.Production;

public sealed class CropService(IProductionRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<CropDto>> ListAsync(Guid organizationId, int page, int pageSize, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, totalCount) = await repository.ListCropsAsync(organizationId, (page - 1) * pageSize, pageSize, search, includeInactive, cancellationToken);
        return new PagedResult<CropDto>(items.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<CropDto?> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var crop = await repository.GetCropAsync(organizationId, id, false, cancellationToken);
        return crop is null ? null : ToDto(crop);
    }

    public async Task<OperationResult<CropDto>> CreateAsync(Guid organizationId, CreateCropCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return OperationResult<CropDto>.Validation("Name is required.");

        var crop = Crop.Create(organizationId, command.Name, command.Variety, DateTime.UtcNow);
        repository.AddCrop(crop);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<CropDto>.Success(ToDto(crop));
    }

    public async Task<OperationResult<CropDto>> UpdateAsync(Guid organizationId, Guid id, UpdateCropCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return OperationResult<CropDto>.Validation("Name is required.");

        var crop = await repository.GetCropAsync(organizationId, id, true, cancellationToken);
        if (crop is null)
            return OperationResult<CropDto>.NotFound("Crop not found.");

        crop.Update(command.Name, command.Variety, DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<CropDto>.Success(ToDto(crop));
    }

    public async Task<OperationResult<bool>> DeactivateAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var crop = await repository.GetCropAsync(organizationId, id, true, cancellationToken);
        if (crop is null)
            return OperationResult<bool>.NotFound("Crop not found.");
        crop.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    private static CropDto ToDto(Crop crop) => new(crop.Id, crop.Name, crop.Variety, crop.IsActive, crop.CreatedAtUtc, crop.UpdatedAtUtc);
}
