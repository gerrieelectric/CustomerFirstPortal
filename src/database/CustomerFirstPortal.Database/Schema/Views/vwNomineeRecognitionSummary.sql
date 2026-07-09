CREATE VIEW [dbo].[vwNomineeRecognitionSummary]
AS
SELECT
    s.[NomineeName],
    s.[NomineeEmail],
    s.[NomineeDepartment],
    COUNT_BIG(*) AS [TotalRecommendations],
    SUM(CASE WHEN s.[Status] IN (N'Approved', N'GerrieSynced') THEN 1 ELSE 0 END) AS [ApprovedRecommendations],
    MAX(s.[SubmittedUtc]) AS [MostRecentSubmissionUtc],
    AVG(CAST(s.[ServiceRating] AS DECIMAL(5, 2))) AS [AverageServiceRating]
FROM [dbo].[Submissions] AS s
GROUP BY
    s.[NomineeName],
    s.[NomineeEmail],
    s.[NomineeDepartment];