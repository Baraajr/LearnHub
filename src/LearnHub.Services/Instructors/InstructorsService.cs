using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Repositories.Interfaces;
using LearnHub.Services.Instructors.DTOs;
using LearnHub.Services.Instructors.Interfaces;
using LearnHub.Services.Users.Interfaces;

namespace LearnHub.Services.Instructors;

public class InstructorsService(
    IInstructorVerificationHistoryRepository historyRepo,
    IUsersService usersService,
    IUnitOfWork unitOfWork
) : IInstructorsService
{
    public async Task<List<InstructorResponseDto>> GetAllAsync()
    {
        var instructors = await usersService.GetAllAsync(UserRole.INSTRUCTOR);

        return instructors.Select(MapToDto).ToList();
    }

    public async Task<InstructorResponseDto> GetInstructorByIdAsync(Guid userId)
    {
        var instructor = await GetInstructorOrThrow(userId);
        return MapToDto(instructor);
    }

    public async Task<InstructorResponseDto> UpdateRoleAsync(
        Guid adminId,
        Guid userId,
        UserRole newRole
    )
    {
        // Only instructors can be changed through this endpoint.
        await GetInstructorOrThrow(userId);

        await usersService.UpdateRoleAsync(adminId, userId, newRole);

        return MapToDto(await usersService.GetUserByIdAsync(userId));
    }

    public async Task<InstructorResponseDto> SetActiveAsync(Guid userId, bool isActive)
    {
        await GetInstructorOrThrow(userId);

        var User = await usersService.SetActiveStatusAsync(userId, isActive);
        return MapToDto(User);
    }

    public async Task UpdateStatusAsync(Guid adminId, Guid userId, UpdateInstructorStatusDto dto)
    {
        if (!dto.Status.HasValue || !Enum.IsDefined(dto.Status.Value))
            throw new ArgumentException("Invalid verification status.");

        var status = dto.Status.Value;
        var rejectionReason = dto.RejectionReason?.Trim();

        if (
            status == InstructorVerificationStatus.REJECTED
            && string.IsNullOrWhiteSpace(rejectionReason)
        )
        {
            throw new ArgumentException(
                "A rejection reason is required.",
                nameof(dto.RejectionReason)
            );
        }

        var user = await GetInstructorOrThrow(userId);

        InstructorProfile profile =
            user.InstructorProfile
            ?? throw new InvalidOperationException("Instructor profile not found.");

        if (profile.VerificationStatus == status)
            throw new InvalidOperationException("Instructor already has this verification status.");

        var reason = status == InstructorVerificationStatus.REJECTED ? rejectionReason : null;

        var now = DateTime.UtcNow;
        var previousStatus = profile.VerificationStatus;

        profile.VerificationStatus = status;
        profile.ReviewedBy = adminId;
        profile.ReviewedAt = now;
        profile.RejectionReason = reason;
        profile.UpdatedAt = now;

        await historyRepo.AddAsync(
            new InstructorVerificationHistory
            {
                InstructorId = profile.UserId,
                AdminId = adminId,
                PreviousStatus = previousStatus, // See correction below
                NewStatus = status,
                Reason = reason,
            }
        );

        await unitOfWork.SaveChangesAsync();
    }

    public async Task<List<InstructorVerificationHistoryDto>> GetVerificationHistoryAsync(
        Guid instructorId
    )
    {
        await GetInstructorOrThrow(instructorId);

        var history = await historyRepo.GetByInstructorIdAsync(instructorId);

        return history
            .Select(h => new InstructorVerificationHistoryDto
            {
                Id = h.Id,
                AdminId = h.AdminId,
                PreviousStatus = h.PreviousStatus,
                NewStatus = h.NewStatus,
                Reason = h.Reason,
                CreatedAt = h.CreatedAt,
            })
            .ToList();
    }

    private static InstructorResponseDto MapToDto(User user)
    {
        var profile = user.InstructorProfile!;
        Console.WriteLine(profile.Reviewer);

        return new InstructorResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive,
            VerificationStatus = profile.VerificationStatus,
            ReviewedBy = profile.ReviewedBy,
            ReviewerName = profile.Reviewer?.Name,
            ReviewedAt = profile.ReviewedAt,
            RejectionReason = profile.RejectionReason,
            CreatedAt = profile.CreatedAt,
        };
    }

    private async Task<User> GetInstructorOrThrow(Guid userId)
    {
        var instructor = await usersService.GetUserByIdAsync(userId, UserRole.INSTRUCTOR);

        // A stale profile left over from a former instructor must not count.
        if (instructor == null || instructor.Role != UserRole.INSTRUCTOR)
            throw new KeyNotFoundException("Instructor not found.");

        return instructor;
    }
}
