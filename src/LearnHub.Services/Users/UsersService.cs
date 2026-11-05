using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Repositories.Interfaces;
using LearnHub.Services.Exceptions;
using LearnHub.Services.Users.Interfaces;

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
        return await userRepo.GetAllSync(role);
    }

    public async Task<User> GetUserByIdAsync(Guid userId, UserRole? role = null)
    {
        var user = await GetUserOrThrow(userId, role);
        return user;
    }

    public async Task<User> SetActiveStatusAsync(Guid userId, bool isActive)
    {
        var user = await GetUserOrThrow(userId);

        if (user.IsActive == isActive)
            return user;

        if (user.Role == UserRole.ADMIN && user.IsActive && !isActive)
        {
            var activeAdmins = await userRepo.CountActiveAsync(UserRole.ADMIN);

            if (activeAdmins <= 1)
                throw new ConflictException("Cannot deactivate the last active administrator.");
        }

        user.IsActive = isActive;
        user.TokenVersion++;
        user.UpdatedAt = DateTime.UtcNow;

        userRepo.Update(user);
        await unitOfWork.SaveChangesAsync();
        return user;
    }

    public async Task UpdateRoleAsync(Guid adminId, Guid userId, UserRole newRole)
    {
        // 1. Guard Clauses & Input Validation
        if (adminId == userId)
            throw new BadRequestException("You cannot change your own role.");

        if (!Enum.IsDefined(typeof(UserRole), newRole) || newRole == UserRole.SUPERADMIN)
            throw new BadRequestException("Invalid or unauthorized target role.");

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var user = await GetUserOrThrow(userId);

            if (user.Role == UserRole.SUPERADMIN)
                throw new BadRequestException("Superadmin role cannot be changed.");

            if (user.Role == newRole)
                return true;

            // 2. Business Rule: Protect the last active admin
            await EnsureNotLastActiveAdminAsync(user.Role, user.IsActive);

            // 3. Handle Profile Transitions (Cleanup old profile)
            await CleanupOldProfileAsync(userId, user.Role);

            // 4. Handle Profile Transitions (Initialize new profile)
            await InitializeNewProfileAsync(userId, newRole);

            // 5. Update User State
            user.Role = newRole;
            user.TokenVersion++;
            user.UpdatedAt = DateTime.UtcNow;

            userRepo.Update(user);
            await unitOfWork.SaveChangesAsync();

            return true;
        });
    }

    private async Task EnsureNotLastActiveAdminAsync(UserRole currentRole, bool isActive)
    {
        if (currentRole == UserRole.ADMIN && isActive)
        {
            var activeAdmins = await userRepo.CountActiveAsync(UserRole.ADMIN);
            if (activeAdmins <= 1)
            {
                throw new ConflictException(
                    "Cannot change the role of the last active administrator."
                );
            }
        }
    }

    private async Task CleanupOldProfileAsync(Guid userId, UserRole currentRole)
    {
        switch (currentRole)
        {
            case UserRole.STUDENT:
                var studentProfile = await studentRepo.GetByUserIdAsync(userId);
                if (studentProfile != null)
                    await studentRepo.DeleteAsync(studentProfile);
                break;

            case UserRole.INSTRUCTOR:
                var instructorProfile = await instructorRepo.GetByUserIdAsync(userId);
                if (instructorProfile != null)
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
            var instructorProfile = await instructorRepo.GetByUserIdAsync(userId);

            if (instructorProfile == null)
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
                // Reset verification state if they are re-entering instructor role
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
        return await userRepo.ExistsAsync(userId, role);
    }

    private async Task<User> GetUserOrThrow(Guid userId, UserRole? role = null)
    {
        return await userRepo.GetByIdAsync(userId, role)
            ?? throw new NotFoundException("User not found.");
    }
}
