public enum OwnershipType { Own, Others, Shared, Participating, Global }

public class ParsedPermission
{
    public required string RawName { get; set; }
    public required string Domain { get; set; }
    public string Action { get; set; } = string.Empty;
    public OwnershipType Ownership { get; set; }
}

public class PermissionEvaluator
{
    private readonly List<ParsedPermission> _userPermissions;

    public PermissionEvaluator(IEnumerable<string> rawPermissions)
    {
        _userPermissions = rawPermissions.Select(Parse).ToList();
    }

    private ParsedPermission Parse(string perm)
    {
        var parts = perm.Split('_');
        var domain = parts[0];
        
        OwnershipType ownership = OwnershipType.Global;
        if (perm.Contains("_OWN") || perm.Contains("_MANAGED_")) ownership = OwnershipType.Own;
        else if (perm.Contains("_OTHERS") || perm.Contains("_OTHER")) ownership = OwnershipType.Others;
        else if (perm.Contains("_SHARED")) ownership = OwnershipType.Shared;
        else if (perm.Contains("_PARTICIPATING") || perm.Contains("_INVITED")) ownership = OwnershipType.Participating;

        return new ParsedPermission 
        { 
            RawName = perm, 
            Domain = domain, 
            Ownership = ownership 
        };
    }

    public bool CanUserPerform(string domain, string actionPart, OwnershipType targetOwnership)
    {
        return _userPermissions.Any(p => 
            p.Domain == domain && 
            p.Ownership == targetOwnership && 
            p.RawName.Contains(actionPart));
    }
}