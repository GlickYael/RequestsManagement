-- =========================================================================
-- שאילתת בדיקה לכל אינדקס - WITH (עם, הבחירה הטבעית של SQL Server)
-- מול WITH (INDEX(0)) (בלי - מאלץ Base Table בלבד, מדמה "כאילו האינדקס לא קיים")
-- הרץ כל זוג (עם/בלי) בנפרד, והשווה את Logical reads מתוך STATISTICS IO.
--
-- הרצה: sqlcmd -S "(localdb)\mssqllocaldb" -d RequestsManagementDb -i performance_per_index.sql
-- (או דרך SSMS / SQL Server Object Explorer ב-Visual Studio - הדביקו את הקובץ והריצו)
-- =========================================================================

-- -------------------------------------------------------------------------
-- 1) IX_Requests_CreatedAt - תומך במיון ברירת המחדל (ללא סינון) + טווח תאריכים
-- -------------------------------------------------------------------------
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

PRINT '--- IX_Requests_CreatedAt: WITH (מיון ברירת מחדל, עמוד ראשון) ---';
SELECT Id, Title, CreatedAt
FROM Requests
ORDER BY CreatedAt DESC
OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
OPTION (RECOMPILE);
GO

SET STATISTICS IO ON;
SET STATISTICS TIME ON;
PRINT '--- IX_Requests_CreatedAt: WITHOUT (אותה שאילתה, בלי אינדקסים נוספים) ---';
SELECT Id, Title, CreatedAt
FROM Requests WITH (INDEX(0))
ORDER BY CreatedAt DESC
OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
OPTION (RECOMPILE);
GO

-- -------------------------------------------------------------------------
-- 2) IX_Requests_Status - תומך באגרגציית StatusCounts (GROUP BY) בלבד
--    (לא בסינון - שם האופטימייזר בפועל מעדיף CreatedAt/האינדקס המורכב, ראו (4))
-- -------------------------------------------------------------------------
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
PRINT '--- IX_Requests_Status: WITH (GROUP BY Status) ---';
SELECT Status, COUNT(*) FROM Requests GROUP BY Status OPTION (RECOMPILE);
GO

SET STATISTICS IO ON;
SET STATISTICS TIME ON;
PRINT '--- IX_Requests_Status: WITHOUT (GROUP BY Status, בלי אינדקסים) ---';
SELECT Status, COUNT(*) FROM Requests WITH (INDEX(0)) GROUP BY Status OPTION (RECOMPILE);
GO

-- -------------------------------------------------------------------------
-- 3) IX_Requests_Priority - תומך באגרגציית PriorityCounts (GROUP BY) בלבד
-- -------------------------------------------------------------------------
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
PRINT '--- IX_Requests_Priority: WITH (GROUP BY Priority) ---';
SELECT Priority, COUNT(*) FROM Requests GROUP BY Priority OPTION (RECOMPILE);
GO

SET STATISTICS IO ON;
SET STATISTICS TIME ON;
PRINT '--- IX_Requests_Priority: WITHOUT (GROUP BY Priority, בלי אינדקסים) ---';
SELECT Priority, COUNT(*) FROM Requests WITH (INDEX(0)) GROUP BY Priority OPTION (RECOMPILE);
GO

-- -------------------------------------------------------------------------
-- 3b) הוכחה: למה לא כדאי להכריח את IX_Requests_Priority לסינון+מיון+דפדוף
--     (מראה שהאופטימייזר צודק שהוא לא בוחר בו שם בעצמו - תוצאה גרועה בהרבה)
-- -------------------------------------------------------------------------
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
PRINT '--- Priority filter+sort+paginate: הבחירה החופשית של SQL Server (ללא כפייה) ---';
SELECT Id, Title, Priority
FROM Requests
WHERE Priority = 1
ORDER BY CreatedAt DESC
OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
OPTION (RECOMPILE);
GO

SET STATISTICS IO ON;
SET STATISTICS TIME ON;
PRINT '--- אותה שאילתה, IX_Requests_Priority מאולץ (לא קורה בפועל - להדגמה בלבד) ---';
SELECT Id, Title, Priority
FROM Requests WITH (INDEX(IX_Requests_Priority))
WHERE Priority = 1
ORDER BY CreatedAt DESC
OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
OPTION (RECOMPILE);
GO

-- -------------------------------------------------------------------------
-- 4) IX_Requests_Status_CreatedAt (מורכב) - תבנית השימוש המרכזית: סינון לפי
--    Status + מיון לפי CreatedAt + דפדוף (בדיוק מה ש-GET /api/requests עושה)
-- -------------------------------------------------------------------------
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
PRINT '--- IX_Requests_Status_CreatedAt: WITH (סינון+מיון+דפדוף) ---';
SELECT Id, Title, Status, CreatedAt
FROM Requests
WHERE Status = 0
ORDER BY CreatedAt DESC
OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
OPTION (RECOMPILE);
GO

SET STATISTICS IO ON;
SET STATISTICS TIME ON;
PRINT '--- IX_Requests_Status_CreatedAt: WITHOUT (אותה שאילתה, בלי אינדקסים) ---';
SELECT Id, Title, Status, CreatedAt
FROM Requests WITH (INDEX(0))
WHERE Status = 0
ORDER BY CreatedAt DESC
OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
OPTION (RECOMPILE);
GO
