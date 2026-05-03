SELECT 
    t.AgentType,
    SUM(tc.total_called) AS TotalCalls,
    SUM(wc.wrong_calls) AS WrongCalls,
    ROUND(SUM(wc.wrong_calls) / SUM(tc.total_called) * 100, 2) AS TCER
FROM testresults t
JOIN generatedscenarios g ON t.SessionId = g.ScenarioId
CROSS JOIN LATERAL (
    SELECT COUNT(*) AS total_called
    FROM JSON_TABLE(t.ToolNames, '$[*]' COLUMNS (tool VARCHAR(100) PATH '$')) AS at2
) tc
CROSS JOIN LATERAL (
    SELECT COUNT(*) AS wrong_calls
    FROM JSON_TABLE(t.ToolNames, '$[*]' COLUMNS (tool VARCHAR(100) PATH '$')) AS at3
    WHERE at3.tool NOT IN (
        SELECT et.tool
        FROM JSON_TABLE(g.ExpectedTools, '$[*]' COLUMNS (tool VARCHAR(100) PATH '$')) AS et
    )
) wc
WHERE JSON_LENGTH(t.ToolNames) > 0
GROUP BY t.AgentType;