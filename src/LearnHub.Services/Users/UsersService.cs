using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Repositories.InstructorProfiles;
using LearnHub.Repositories.StudentProfiles;
using LearnHub.Repositories.UnitOfWorkRepo;
using LearnHub.Repositories.Users;
using LearnHub.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services.Users;

public class UsersService(
    IUserRepository userRepo,
    IStudentProfileRepository studentRepo,
    IInstructorProfileRepository instructorRepo,
    IUnitOfWork unitOfWork
) : IUsersService
{
    public async Task<List<User>> GetAllAsync(UserRole? role = null)
    {
        var query = userRepo.GetAllAsync();

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        return await query.ToListAsync();
    }

    public async Task<User> GetUserByIdAsync(Guid userId, UserRole? role = null)
    {
        var user = await userRepo.GetByIdAsync(userId);

        if (user is null || (role.HasValue && user.Role != role.Value))
            throw new NotFoundException("User not found.");

        return user;
    }

    public async Task<User> SetActiveStatusAsync(Guid userId, bool isActive)
    {
        var user =
            await userRepo.GetByIdAsync(userId) ?? throw new NotFoundException("User not found.");

        if (user.IsActive == isActive)
            return user;

        if (user.Role == UserRole.ADMIN && user.IsActive && !isActive)
        {
            var activeAdmins = await userRepo
                .GetAllAsync()
                .CountAsync(u => u.Role == UserRole.ADMIN && u.IsActive);

            if (activeAdmins <= 1)
                throw new ConflictException("Cannot deactivate the last active administrator.");
        }

        user.IsActive = isActive;
        user.TokenVersion++;

        await userRepo.UpdateAsync(user);
        await unitOfWork.SaveChangesAsync();

        return user;
    }

    public async Task UpdateRoleAsync(Guid adminId, Guid userId, UserRole newRole)
    {
        if (adminId == userId)
            throw new BadRequestException("You cannot change your own role.");

        if (!Enum.IsDefined(typeof(UserRole), newRole) || newRole == UserRole.SUPERADMIN)
        {
            throw new BadRequestException("Invalid or unauthorized target role.");
        }

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var user =
                await userRepo.GetByIdAsync(userId)
                ?? throw new NotFoundException("User not found.");

            if (user.Role == UserRole.SUPERADMIN)
                throw new BadRequestException("Superadmin role cannot be changed.");

            if (user.Role == newRole)
                return true;

            await EnsureNotLastActiveAdminAsync(user.Role, user.IsActive);

            await CleanupOldProfileAsync(userId, user.Role);
            await InitializeNewProfileAsync(userId, newRole);

            user.Role = newRole;
            user.TokenVersion++;

            await userRepo.UpdateAsync(user);
            await unitOfWork.SaveChangesAsync();

            return true;
        });
    }

    private async Task EnsureNotLastActiveAdminAsync(UserRole currentRole, bool isActive)
    {
        if (currentRole == UserRole.ADMIN && isActive)
        {
            var activeAdmins = await userRepo
                .GetAllAsync()
                .CountAsync(u => u.Role == UserRole.ADMIN && u.IsActive);

            if (activeAdmins <= 1)
                throw new ConflictException(
                    "Cannot change the role of the last active administrator."
                );
        }
    }

    private async Task CleanupOldProfileAsync(Guid userId, UserRole currentRole)
    {
        switch (currentRole)
        {
            case UserRole.STUDENT:
                var studentProfile = await studentRepo.GetByIdAsync(userId);

                if (studentProfile is not null)
                    await studentRepo.DeleteAsync(studentProfile);

                break;

            case UserRole.INSTRUCTOR:
                var instructorProfile = await instructorRepo.GetByIdAsync(userId);

                if (instructorProfile is not null)
                    await instructorRepo.DeleteAsync(instructorProfile);

                break;
        }
    }

    private async Task InitializeNewProfileAsync(Guid userId, UserRole newRole)
    {
        if (newRole == UserRole.STUDENT)
        {
            await studentRepo.AddAsync(new StudentProfile { UserId = userId });
        }
        else if (newRole == UserRole.INSTRUCTOR)
        {
            var instructorProfile = await instructorRepo.GetByIdAsync(userId);

            if (instructorProfile is null)
            {
                await instructorRepo.AddAsync(
                    new InstructorProfile
                    {
                        UserId = userId,
                        VerificationStatus = InstructorVerificationStatus.PENDING,
                    }
                );
            }
            else
            {
                instructorProfile.VerificationStatus = InstructorVerificationStatus.PENDING;

                instructorProfile.ReviewedBy = null;
                instructorProfile.ReviewedAt = null;
                instructorProfile.RejectionReason = null;

                await instructorRepo.UpdateAsync(instructorProfile);
            }
        }
    }

    public async Task<bool> ExistsAsync(Guid userId, UserRole? role = null)
    {
        var query = userRepo.GetAllAsync().Where(u => u.Id == userId);

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        return await query.AnyAsync();
    }
}
