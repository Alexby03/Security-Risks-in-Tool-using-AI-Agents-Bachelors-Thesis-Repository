SELECT 
    t.AgentType,
    COUNT(*) AS TotalTests,
    SUM(
        CASE WHEN 
            JSON_CONTAINS(t.ToolNames, g.ExpectedTools)
            AND JSON_CONTAINS(g.ExpectedTools, t.ToolNames)
        THEN 1 ELSE 0 END
    ) AS CorrectTools,
    ROUND(
        SUM(
            CASE WHEN 
                JSON_CONTAINS(t.ToolNames, g.ExpectedTools)
                AND JSON_CONTAINS(g.ExpectedTools, t.ToolNames)
            THEN 1 ELSE 0 END
        ) / COUNT(*) * 100, 2
    ) AS TCR
FROM testresults t
JOIN generatedscenarios g ON t.SessionId = g.ScenarioId
GROUP BY t.AgentType;