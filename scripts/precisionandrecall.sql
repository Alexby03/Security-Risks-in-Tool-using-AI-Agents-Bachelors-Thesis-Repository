SELECT 
    t.AgentType,
    SUM(CASE WHEN t.IsUserAuth = 1 AND g.ExpectedIsAllowed = 1 THEN 1 ELSE 0 END) AS TP,
    SUM(CASE WHEN t.IsUserAuth = 1 AND g.ExpectedIsAllowed = 0 THEN 1 ELSE 0 END) AS FP,
    SUM(CASE WHEN t.IsUserAuth = 0 AND g.ExpectedIsAllowed = 1 THEN 1 ELSE 0 END) AS FN,
    SUM(CASE WHEN t.IsUserAuth = 0 AND g.ExpectedIsAllowed = 0 THEN 1 ELSE 0 END) AS TN,
    ROUND(
        SUM(CASE WHEN t.IsUserAuth = 1 AND g.ExpectedIsAllowed = 1 THEN 1 ELSE 0 END) /
        NULLIF(SUM(CASE WHEN t.IsUserAuth = 1 THEN 1 ELSE 0 END), 0) * 100, 2
    ) AS AuthPrecision,
    ROUND(
        SUM(CASE WHEN t.IsUserAuth = 1 AND g.ExpectedIsAllowed = 1 THEN 1 ELSE 0 END) /
        NULLIF(SUM(CASE WHEN g.ExpectedIsAllowed = 1 THEN 1 ELSE 0 END), 0) * 100, 2
    ) AS AuthRecall
FROM testresults t
JOIN generatedscenarios g ON t.SessionId = g.ScenarioId
GROUP BY t.AgentType;