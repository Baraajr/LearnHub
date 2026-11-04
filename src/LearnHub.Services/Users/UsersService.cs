using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Repositories.Interfaces;
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
                throw new InvalidOperationException(
                    "Cannot deactivate the last active administrator."
                );
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
        if (adminId == userId)
            throw new InvalidOperationException("You cannot change your own role.");

        if (newRole is not (UserRole.ADMIN or UserRole.STUDENT or UserRole.INSTRUCTOR))
        {
            throw new ArgumentException("Invalid target role.");
        }

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var user = await GetUserOrThrow(userId);

            if (user.Role == UserRole.SUPERADMIN)
                throw new InvalidOperationException("Superadmin role cannot be changed.");

            if (user.Role == newRole)
                return true;

            if (user.Role == UserRole.ADMIN && user.IsActive)
            {
                var activeAdmins = await userRepo.CountActiveAsync(UserRole.ADMIN);

                if (activeAdmins <= 1)
                    throw new InvalidOperationException(
                        "Cannot change the role of the last active administrator."
                    );
            }

            // Remove the student profile when leaving STUDENT.
            if (user.Role == UserRole.STUDENT)
            {
                var studentProfile = await studentRepo.GetByUserIdAsync(userId);

                if (studentProfile != null)
                    await studentRepo.DeleteAsync(studentProfile);
            }

            if (user.Role == UserRole.INSTRUCTOR)
            {
                var instructorProfile = await instructorRepo.GetByUserIdAsync(userId);

                if (instructorProfile != null)
                    await instructorRepo.DeleteAsync(instructorProfile);
            }

            // Create a student profile when entering STUDENT.
            if (newRole == UserRole.STUDENT)
            {
                await studentRepo.AddAsync(new StudentProfile { UserId = userId });
            }

            // Create or reset the instructor profile when entering INSTRUCTOR.
            if (newRole == UserRole.INSTRUCTOR)
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
                    instructorProfile.VerificationStatus = InstructorVerificationStatus.PENDING;

                    instructorProfile.ReviewedBy = null;
                    instructorProfile.ReviewedAt = null;
                    instructorProfile.RejectionReason = null;

                    await instructorRepo.UpdateAsync(instructorProfile);
                }
            }

            user.Role = newRole;
            user.TokenVersion++;
            user.UpdatedAt = DateTime.UtcNow;

            userRepo.Update(user);

            await unitOfWork.SaveChangesAsync();

            return true;
        });
    }

    private async Task<User> GetUserOrThrow(Guid userId, UserRole? role = null)
    {
        var user =
            await userRepo.GetByIdAsync(userId, role)
            ?? throw new KeyNotFoundException("User not found.");
        return user;
    }
}
