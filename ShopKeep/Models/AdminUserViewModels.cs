using System;
using System.Collections.Generic;

namespace ShopKeep.Models;

public class AdminUserListItemVM
{
    public required string Id { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public DateTimeOffset? LockoutEnd { get; set; }

    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
}

public class AdminEditUserRolesVM
{
    public required string Id { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }

    public List<string> AllRoles { get; set; } = new();
    public List<string> SelectedRoles { get; set; } = new();
}
