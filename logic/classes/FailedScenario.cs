public class FailedScenario
{
    public int SessionId { get; set; }
    public string AgentType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool ExpectedIsAllowed { get; set; }
    
    // generatedscenarios
    public string PromptText { get; set; } = string.Empty;
    public string ExpectedToolsJson { get; set; } = string.Empty;
    public string RequiredPermissionsJson { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    
    // testresults
    public bool IsUserAuth { get; set; }
    public string IsUserAuthReasonLog { get; set; } = string.Empty;
    public bool IsMalicious { get; set; }
    public string IsMaliciousReasonLog { get; set; } = string.Empty;
    public bool DidAssignment { get; set; }
    public string DidAssignmentReasonLog { get; set; } = string.Empty;
    public string ToolNamesJson { get; set; } = string.Empty;
    public string ToolReason { get; set; } = string.Empty;
}