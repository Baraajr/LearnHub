using LearnHub.Data.Entities;
using LearnHub.Services.Admins.DTOs;

namespace LearnHub.Services.Admins;

public interface IAdminService
{
    Task<IReadOnlyList<AdminDto>> GetAllAsync();

    Task<AdminDto> GetByIdAsync(Guid id);

    Task<AdminDto> CreateAsync(CreateAdminDto dto);

    Task DeleteAsync(Guid id);
    Task ActivateAsync(Guid id);
}
