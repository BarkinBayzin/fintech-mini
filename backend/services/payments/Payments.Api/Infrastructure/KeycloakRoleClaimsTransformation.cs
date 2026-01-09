using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Payments.Api.Infrastructure;

public sealed class KeycloakRoleClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        var realmAccessClaim = principal.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccessClaim))
        {
            return Task.FromResult(principal);
        }

        try
        {
            using var doc = JsonDocument.Parse(realmAccessClaim);
            if (!doc.RootElement.TryGetProperty("roles", out var rolesElement))
            {
                return Task.FromResult(principal);
            }

            foreach (var role in rolesElement.EnumerateArray())
            {
                var roleName = role.GetString();
                if (string.IsNullOrWhiteSpace(roleName))
                {
                    continue;
                }

                if (!identity.HasClaim(ClaimTypes.Role, roleName))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                }
            }
        }
        catch (JsonException)
        {
            return Task.FromResult(principal);
        }

        return Task.FromResult(principal);
    }
}
