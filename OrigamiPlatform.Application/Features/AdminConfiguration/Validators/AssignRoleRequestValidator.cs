using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Features.AdminConfiguration.Validators;

public static class AssignRoleRequestValidator
{
    private static readonly HashSet<string> AssignableRoles =
    [
        nameof(UserRoleType.ContributorReviewer),
        nameof(UserRoleType.Manager)
    ];

    public static void Validate(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new DomainException("Role is required.");

        if (!AssignableRoles.Contains(role))
            throw new DomainException($"Invalid role. Assignable roles are: {string.Join(", ", AssignableRoles)}.");
    }
}
