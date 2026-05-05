public class ScenarioMission
{
    public ScenarioCategory Category { get; set; }
    public bool ExpectedOutcome { get; set; }
    public List<string> RequiredPermissions { get; set; } = new();
    public List<string> RecommendedTools { get; set; } = new();
    public string Counterparty { get; set; } = string.Empty;
    public List<ResourceSkeleton> Resources { get; set; } = new();
}