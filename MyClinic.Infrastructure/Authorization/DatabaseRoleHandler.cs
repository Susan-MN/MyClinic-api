using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyClinic.Infrastructure.Data;
using System.Security.Claims;

namespace MyClinic.Infrastructure.Authorization
{
    public class DatabaseRoleHandler : AuthorizationHandler<DatabaseRoleRequirement>
    {
        private readonly AppDbContext _dbContext;

        public DatabaseRoleHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            DatabaseRoleRequirement requirement)
        {
            // Get KeycloakId from claims
            var keycloakId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? context.User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakId))
            {
                context.Fail();
                return;
            }

            // Check role in database based on requirement
            switch (requirement.RequiredRole.ToLower())
            {
                case "doctor":
                    var doctor = await _dbContext.Doctors
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.KeycloakId == keycloakId);
                    if (doctor != null)
                    {
                        context.Succeed(requirement);
                    }
                    break;

                case "patient":
                    var patient = await _dbContext.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);
                    if (patient != null)
                    {
                        context.Succeed(requirement);
                    }
                    break;

                case "admin":
                    // First check Keycloak for admin role
                    var realmAccessClaim = context.User.FindFirst("realm_access");
                    if (realmAccessClaim != null && realmAccessClaim.Value.Contains("\"admin\""))
                    {
                        context.Succeed(requirement);
                        return;
                    }
                    
                    // Also check standard role claims
                    if (context.User.HasClaim(c =>
                        (c.Type == ClaimTypes.Role || c.Type == "role") &&
                        c.Value.Equals("admin", StringComparison.OrdinalIgnoreCase)))
                    {
                        context.Succeed(requirement);
                        return;
                    }
                    
                    // Fallback: Check database for admin
                    var admin = await _dbContext.Admins
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.KeycloakId == keycloakId);
                    if (admin != null)
                    {
                        context.Succeed(requirement);
                    }
                    break;
            }
        }
    }
}

