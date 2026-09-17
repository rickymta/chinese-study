using Microsoft.AspNetCore.Authorization;

namespace AntFarm.Auth.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
