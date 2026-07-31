using KhataFlow.Core.Domain.IdentityEntities;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.Mappers;

public static class ApplicationUserToUserResponseMappingProfile
{
    public static UserResponse ToUserResponse(this ApplicationUser user)
    {
        return new UserResponse(
            Id: user.Id,
            FullName: user.FullName,
            FullNameUr: user.FullNameUr,
            DisplayName: user.DisplayName,
            DisplayNameUr: user.DisplayNameUr,
            Email: user.Email,
            PhoneNumber: user.PhoneNumber,
            ProfilePictureUrl: user.ProfilePictureUrl,
            BusinessId: user.BusinessId,
            Gender: user.Gender.ToString(),
            DateOfBirth: user.DateOfBirth,
            Role: user.Role.ToString(),
            Status: user.Status.ToString(),
            Plan: user.Plan.ToString(),
            PlanExpiryDate: user.PlanExpiryDate,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt
        );
    }
}