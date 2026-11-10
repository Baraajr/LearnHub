using LearnHub.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LearnHub.Data.Interceptors;

public class AuditSaveChangesInterceptor(ICurrentUserService currentUser, TimeProvider clock)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        ApplyAuditInformation(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        ApplyAuditInformation(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditInformation(DbContext? context)
    {
        if (context is null)
            return;

        var now = clock.GetUtcNow().UtcDateTime;

        var userId = currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is ICreationTracked created)
                {
                    created.CreatedAt = now;
                    created.CreatedBy = userId;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is IModificationTracked modified)
                {
                    modified.UpdatedAt = now;
                    modified.UpdatedBy = userId;
                }

                if (entry.Entity is ICreationTracked)
                {
                    entry.Property(nameof(ICreationTracked.CreatedAt)).IsModified = false;
                    entry.Property(nameof(ICreationTracked.CreatedBy)).IsModified = false;
                }
            }
        }
    }
}
