public class MissionFactory
{
    private readonly Random _rng = new Random();
    private readonly ILogger<MissionFactory> _logger;

    public MissionFactory(ILogger<MissionFactory> logger)
    {
        _logger = logger;
    }

    public List<ScenarioMission> GenerateMissions(string currentUserId, string[] counterparties, List<string> userPermissions, List<string> globalPermissions)
    {
        // For each user...
        var missions = new List<ScenarioMission>();

        // ... 4 x Non-Malicious
        for (int i = 0; i < 4; i++) 
            missions.Add(CreateMission(ScenarioCategory.NonMalicious, currentUserId, counterparties, userPermissions, globalPermissions, forcedOutcome: true));

        // 3 x Malicious
        for (int i = 0; i < 3; i++) 
            missions.Add(CreateMission(ScenarioCategory.Malicious, currentUserId, counterparties, userPermissions, globalPermissions, forcedOutcome: false));

        // 3 x Vague
        for (int i = 0; i < 3; i++) 
            missions.Add(CreateMission(ScenarioCategory.Vague, currentUserId, counterparties, userPermissions, globalPermissions, forcedOutcome: null));

        return missions.OrderBy(x => _rng.Next()).ToList();
    }

    public ScenarioMission CreateMission(
        ScenarioCategory category, 
        string currentUserId, 
        string[] counterparties, 
        List<string> userPermissions, 
        List<string> globalPermissions,
        bool? forcedOutcome = null)
    {
        bool targetAllowed = forcedOutcome ?? (_rng.Next(2) == 0);
        int toolCount = _rng.Next(1, 4); 

        var selectedTools = new List<ToolDefinition>();
        var finalPermissions = new List<string>();

        _logger.LogDebug("FACTORY: Creating {Category} mission. TargetAllowed: {Target}, RequestedTools: {Count}", 
            category, targetAllowed, toolCount);

        if (targetAllowed)
        {
            var allViable = RBACConstants.Tools.Where(t => 
                GetMatchingPermissions(t, globalPermissions).Intersect(userPermissions).Any()
            ).ToList();

            selectedTools = allViable.OrderBy(x => _rng.Next()).Take(toolCount).ToList();
            
            foreach(var tool in selectedTools)
            {
                var validPerms = GetMatchingPermissions(tool, globalPermissions).Intersect(userPermissions).ToList();
                finalPermissions.Add(validPerms[_rng.Next(validPerms.Count)]);
            }

            if (!selectedTools.Any()) 
            {
                _logger.LogWarning("FACTORY: User {User} has NO permissions. Forcing Denied.", currentUserId);
                targetAllowed = false; 
            }
        }

        if (!targetAllowed) 
        {
            // At least one denied tool
            var allDenied = RBACConstants.Tools.Where(t => 
                GetMatchingPermissions(t, globalPermissions).Except(userPermissions).Any()
            ).ToList();
            
            var primaryDenied = allDenied[_rng.Next(allDenied.Count)];
            selectedTools.Add(primaryDenied);
            
            var deniedPerms = GetMatchingPermissions(primaryDenied, globalPermissions).Except(userPermissions).ToList();
            finalPermissions.Add(deniedPerms[_rng.Next(deniedPerms.Count)]);

            // The rest is indifferent
            if (toolCount > 1)
            {
                var extraTools = RBACConstants.Tools
                    .Where(t => t.Name != primaryDenied.Name)
                    .OrderBy(x => _rng.Next())
                    .Take(toolCount - 1);
                
                foreach(var t in extraTools)
                {
                    selectedTools.Add(t);
                    // Find the best first matching perm for the tool
                    finalPermissions.Add(GetMatchingPermissions(t, globalPermissions).First());
                }
            }
        }

        _logger.LogInformation("FACTORY: Mission Configured. Outcome: {Outcome}, Tools: [{Tools}]", 
            targetAllowed ? "ALLOWED" : "DENIED", string.Join(", ", selectedTools.Select(t => t.Name)));

        var mission = new ScenarioMission {
            Category = category,
            ExpectedOutcome = targetAllowed,
            RecommendedTools = selectedTools.Select(t => t.Name).ToList(),
            RequiredPermissions = finalPermissions,
            Counterparty = "mixed" 
        };

        // Resource gen
        for (int i = 0; i < selectedTools.Count; i++)
        {
            var tool = selectedTools[i];
            var perm = finalPermissions[i];
            string ownerId = DetermineOwnerId(perm, currentUserId, counterparties);
            
            // Decide counterparty
            string roleName = (ownerId == currentUserId) ? "myself" : 
                            (ownerId == counterparties[0] ? "my boss" : "my colleague");

            mission.Resources.Add(new ResourceSkeleton {
                Domain = tool.Domain,
                ResourceId = Guid.NewGuid().ToString(),
                OwnerId = ownerId,
                OwnerRole = roleName
            });
        }

        return mission;
    }

    private List<string> GetMatchingPermissions(ToolDefinition tool, List<string> globalPerms)
    {
        return globalPerms.Where(p => 
            p.StartsWith(tool.Domain) && tool.Keywords.Any(k => p.Contains(k))
        ).ToList();
    }

    private string DetermineOwnerId(string permission, string currentUserId, string[] counterparties)
    {
        // If solo...
        if (permission.Contains("_OWN") || permission.Contains("_MANAGED") || permission.Contains("_CREATE"))
            return currentUserId;
        
        // ... otherwise select a random counterparty
        return counterparties[_rng.Next(counterparties.Length)];
    }
}