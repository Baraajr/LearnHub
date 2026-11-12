using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Repositories.InstructorProfiles;
using LearnHub.Repositories.InstructorVerificationHistories;
using LearnHub.Repositories.UnitOfWorkRepo;
using LearnHub.Repositories.Users;
using LearnHub.Services.Exceptions;
using LearnHub.Services.Instructors.DTOs;
using LearnHub.Services.Users;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services.Instructors;

public class InstructorsService(
    IUserRepository userRepo,
    IInstructorProfileRepository instructorRepo,
    IInstructorVerificationHistoryRepository historyRepo,
    IUsersService usersService,
    IUnitOfWork unitOfWork
) : IInstructorsService, IScopedService
{
    public async Task<List<InstructorResponseDto>> GetAllAsync()
    {
        return await GetInstructorQuery().ToListAsync();
    }

    public async Task<InstructorResponseDto> GetInstructorByIdAsync(Guid userId)
    {
        var instructor = await GetInstructorQuery().FirstOrDefaultAsync(i => i.Id == userId);

        return instructor ?? throw new NotFoundException("Instructor not found.");
    }

    public async Task<User> UpdateRoleAsync(Guid adminId, Guid userId, UserRole newRole)
    {
        var instructor = await userRepo.GetByIdAsync(userId);

        if (instructor is null || instructor.Role != UserRole.INSTRUCTOR)
            throw new NotFoundException("Instructor not found.");

        await usersService.UpdateRoleAsync(adminId, userId, newRole);

        return await userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");
    }

    public async Task<InstructorResponseDto> SetActiveAsync(Guid userId, bool isActive)
    {
        var instructor = await userRepo.GetByIdAsync(userId);

        if (instructor is null || instructor.Role != UserRole.INSTRUCTOR)
            throw new NotFoundException("Instructor not found.");

        await usersService.SetActiveStatusAsync(userId, isActive);

        return await GetInstructorByIdAsync(userId);
    }

    public async Task UpdateStatusAsync(Guid adminId, Guid userId, UpdateInstructorStatusDto dto)
    {
        if (!dto.Status.HasValue || !Enum.IsDefined(dto.Status.Value))
            throw new BadRequestException("Invalid verification status.");

        var status = dto.Status.Value;
        var rejectionReason = dto.RejectionReason?.Trim();

        if (
            status == InstructorVerificationStatus.REJECTED
            && string.IsNullOrWhiteSpace(rejectionReason)
        )
        {
            throw new BadRequestException("A rejection reason is required.");
        }

        var user = await userRepo.GetByIdAsync(userId);

        if (user is null || user.Role != UserRole.INSTRUCTOR)
            throw new NotFoundException("Instructor not found.");

        var profile =
            await instructorRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Instructor profile not found.");

        if (profile.VerificationStatus == status)
            throw new ConflictException("Instructor already has this verification status.");

        var reason = status == InstructorVerificationStatus.REJECTED ? rejectionReason : null;

        var now = DateTime.UtcNow;
        var previousStatus = profile.VerificationStatus;

        profile.VerificationStatus = status;
        profile.ReviewedBy = adminId;
        profile.ReviewedAt = now;
        profile.RejectionReason = reason;

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

    public async Task EnsureCanLoginAsync(Guid userId)
    {
        var user = await userRepo.GetByIdAsync(userId);

        if (user is null || user.Role != UserRole.INSTRUCTOR)
            throw new NotFoundException("Instructor not found.");

        if (!user.IsActive)
            throw new ForbiddenException("Your instructor account is inactive.");

        var profile =
            await instructorRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Instructor profile not found.");

        switch (profile.VerificationStatus)
        {
            case InstructorVerificationStatus.PENDING:
                throw new ForbiddenException("Your instructor account is awaiting verification.");

            case InstructorVerificationStatus.REJECTED:
                throw new ForbiddenException("Your instructor application was rejected.");

            case InstructorVerificationStatus.VERIFIED:
                return;

            default:
                throw new ForbiddenException("Your instructor account cannot access the platform.");
        }
    }

    private IQueryable<InstructorResponseDto> GetInstructorQuery()
    {
        var users = userRepo.GetAllAsync();
        var profiles = instructorRepo.GetAllAsync();

        var query =
            from profile in profiles
            join user in users on profile.UserId equals user.Id
            join reviewer in users
                on profile.ReviewedBy equals (Guid?)reviewer.Id
                into reviewerGroup
            from reviewer in reviewerGroup.DefaultIfEmpty()
            select new InstructorResponseDto
            {
                // Account
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                IsActive = user.IsActive,

                // Verification
                VerificationStatus = profile.VerificationStatus,
                ReviewedBy = profile.ReviewedBy,
                ReviewerName = reviewer == null ? null : reviewer.UserName,
                ReviewedAt = profile.ReviewedAt,
                RejectionReason = profile.RejectionReason,

                // Profile audit
                CreatedAt = profile.CreatedAt,
                CreatedBy = profile.CreatedBy,
                UpdatedAt = profile.UpdatedAt,
                UpdatedBy = profile.UpdatedBy,

                // Account audit
                AccountCreatedAt = user.CreatedAt,
                AccountCreatedBy = user.CreatedBy,
                AccountUpdatedAt = user.UpdatedAt,
                AccountUpdatedBy = user.UpdatedBy,
            };

        return query;
    }

    public async Task<List<InstructorVerificationHistoryDto>> GetVerificationHistoryAsync(
        Guid instructorId
    )
    {
        var user = await userRepo.GetByIdAsync(instructorId);

        if (user is null || user.Role != UserRole.INSTRUCTOR)
            throw new NotFoundException("Instructor not found.");

        var history = await historyRepo
            .GetAllAsync()
            .Where(h => h.InstructorId == instructorId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

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
}
