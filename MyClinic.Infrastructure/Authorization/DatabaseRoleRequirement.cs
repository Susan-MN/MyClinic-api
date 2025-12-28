using Microsoft.AspNetCore.Authorization;

namespace MyClinic.Infrastructure.Authorization
{
    public class DatabaseRoleRequirement : IAuthorizationRequirement
    {
        public string RequiredRole { get; }

        public DatabaseRoleRequirement(string requiredRole)
        {
            RequiredRole = requiredRole;
        }
    }
}

