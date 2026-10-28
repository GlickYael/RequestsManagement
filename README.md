# מערכת ניהול פניות - רשות (Requests Management)

מערכת לניהול פניות המתקבלות מארגונים ומעסיקים: שליפה עם Pagination/Filtering/Sorting בצד שרת, עדכון סטטוס עם Optimistic Concurrency, היסטוריית שינויים (Audit), עדכון קבוצתי (Bulk), ו-Caching לנתוני סיכום - על בסיס 100,000 רשומות.

תכנון העבודה והחלוקה למשימות מתועדים בנפרד ב-[TASKS.md](TASKS.md).

## תוכן עניינים

1. [טכנולוגיות וגרסאות](#טכנולוגיות-וגרסאות)
2. [מבנה הפתרון](#מבנה-הפתרון)
3. [הוראות הפעלה](#הוראות-הפעלה)
4. [בסיס הנתונים והאינדקסים](#בסיס-הנתונים-והאינדקסים)
5. [Concurrency ו-Bulk Update](#concurrency-ו-bulk-update)
6. [Cache ו-Invalidation](#cache-ו-invalidation)
7. [מדידת ביצועים](#מדידת-ביצועים)
8. [בדיקות אוטומטיות](#בדיקות-אוטומטיות)
9. [החלטות טכנולוגיות משמעותיות](#החלטות-טכנולוגיות-משמעותיות)
10. [מגבלה ידועה ושיפור אפשרי](#מגבלה-ידועה-ושיפור-אפשרי)
11. [שימוש בכלי AI](#שימוש-בכלי-ai)

---

## טכנולוגיות וגרסאות

| שכבה | טכנולוגיה | גרסה |
|---|---|---|
| Backend | ASP.NET Core (.NET) | .NET 10 |
| ORM | Entity Framework Core | 10.0.14 |
| בסיס נתונים | SQL Server (LocalDB) | - |
| בדיקות Backend | xUnit + Microsoft.AspNetCore.Mvc.Testing + EF Core InMemory / SQLite (בדיקות Concurrency) | xUnit 2.9.3 |
| Frontend | Angular (**Zoneless**, ללא zone.js) | 21.2 |
| State management | Angular Signals | מובנה ב-Angular 21 |
| HTTP/Reactive | RxJS | 7.8 |
| בדיקות Frontend | Vitest (builder מובנה של Angular CLI) | 4.0.8 |

## מבנה הפתרון

```
RequestsManagement/
├── RM-Backend/                      # ASP.NET Core Web API
│   ├── Controllers/                 # RequestsController, RequestStatusAuditsController
│   ├── Services/                    # RequestsService (לוגיקה עסקית), RequestStatusAuditService, SeedService
│   ├── DTOs/                        # חוזה ה-API (לא חושף Entities)
│   ├── Models/                      # RequestEntity, RequestStatusAuditEntity (ישויות בלבד, ללא enums)
│   ├── Enums/                       # RequestStatus, RequestPriority, UpdateStatusResult (כל ה-enums במקום אחד)
│   ├── Data/                        # AppDbContext
│   ├── Infrastructure/              # GlobalExceptionHandler
│   ├── Migrations/                  # היסטוריית סכמה (EF Core)
│   └── RM-Backend.Tests/            # פרויקט בדיקות xUnit
├── RM-client/                       # Angular SPA
│   └── src/app/
│       ├── pages/requests-dashboard/         # Smart/Container - State+Orchestration בלבד
│       ├── components/
│       │   ├── request-filters-bar/          # חיפוש+סינון+מיון, פולטת filtersChange מאוחד
│       │   ├── aggregations-panel/           # Presentational טהורה (StatusCounts/PriorityCounts)
│       │   ├── requests-table/               # טבלה + בחירת שורות + Bulk (צמוד לטבלה)
│       │   ├── pagination/                   # גנרית לגמרי - לא מכירה "פניות" בכלל
│       │   └── audit-history-modal/          # Smart-leaf עצמאית - שולפת את ההיסטוריה בעצמה
│       ├── services/requests.ts              # עטיפת HTTP דקה (Service)
│       └── models/request.model.ts           # טיפוסים התואמים ל-DTOs של השרת
├── README.md                        # מסמך זה
└── TASKS.md                         # תכנון עבודה וחלוקה למשימות (דרישה #9)
```

## הוראות הפעלה

### הרצת הבק-אנד

```bash
cd RM-Backend
dotnet run                   # בפעם הראשונה: יוצר את ה-DB ומריץ migrations אוטומטית
                             # בהרצות הבאות: מוודא ה-DB עדכני ומתחיל את השרת
```

**בפעם הראשונה בלבד:** לא צריך שום צעד נוסף - `dotnet run` מטפל בהכל (`context.Database.Migrate()` בתוך Program.cs יוצר את הDB וה-schema אם הם לא קיימים).

השרת עולה על `http://localhost:5154` (ראו `Properties/launchSettings.json`).

**יצירת 100,000 רשומות בדיקה (אופציונלי):**

אם את רוצה להתחיל עם נתונים:
```bash
dotnet run -- seedTestData forceReseed  # יוצר את הDB עם 100,000 רשומות דטרמיניסטיות מאפס
```

הייצור דטרמיניסטי (לא רנדומלי) - כל הרצה עם `forceReseed` מניבה בדיוק את אותו דאטהסט (שימושי לבדיקות חוזרות וקריאות).

### הרצת הפרונט (Angular)

```bash
cd RM-client
npm install                  # מתקין את התלויות בפעם הראשונה בלבד
npm start                    # ng serve - dev server בפורט 4200
```

פתיחת הדפדפן ב-`http://localhost:4200`.

### הרצת בדיקות

```bash
# בק-אנד
cd RM-Backend
dotnet test RM-Backend.Tests/RM-Backend.Tests.csproj

# פרונט
cd RM-client
npm test
```

## בסיס הנתונים והאינדקסים.

**אינדקסים קיימים על טבלת `Requests`:**

| אינדקס | שדות | מטרה |
|---|---|---|
| `IX_Requests_Status` | Status | אגרגציית `StatusCounts` (`GROUP BY Status`) - **לא** סינון, ראו ההסבר המדוד למטה |
| `IX_Requests_Priority` | Priority | אגרגציית `PriorityCounts` (`GROUP BY Priority`) - **לא** סינון, ראו ההסבר המדוד למטה |
| `IX_Requests_CreatedAt` | CreatedAt | מיון/טווח תאריכים (ברירת המחדל), וגם הסינון הרגיל לפי Status כשאין את האינדקס המורכב - ראו למטה |
| `IX_Requests_Status_CreatedAt` | Status, CreatedAt (מורכב) | תבנית השימוש הנפוצה ביותר: סינון לפי Status + מיון לפי CreatedAt - ראו [מדידת ביצועים](#מדידת-ביצועים) להוכחה מדודה לשיפור |
| `IX_Requests_Priority_CreatedAt` | Priority, CreatedAt (מורכב) | אותה תבנית עבור סינון לפי Priority + מיון ברירת המחדל - כדי שגם סינון לפי Priority יהיה `Index Seek` ולא Scan, באותו היגיון של האינדקס המדוד מעליו |

**מה לא מואץ באינדקס (בכוונה):** סינון לפי `OrganizationName`/`AssignedTo`/חיפוש טקסט חופשי מבוצע עם `Contains` (wildcard מוביל), שאינו יכול להשתמש ב-B-tree index - נשאר Scan. שילוב Status + Priority יחד משתמש באחד האינדקסים המורכבים ובודק את השדה השני כתנאי שיורי. לחיפוש טקסט מהיר באמת נדרש Full-Text Search - מחוץ לסקופ.


### Optimistic Concurrency

שתי שכבות הגנה משלימות:

1. **ברמת אפליקציה:** השוואה ידנית מפורשת (`request.Version != clientVersion`) לפני ביצוע השינוי - מחזירה `409 Conflict` ברור עם הודעה קריאה.
2. **ברמת EF/DB:** `Version` מוגדר כ-`.IsConcurrencyToken()` (ראו `AppDbContext.OnModelCreating`) - ה-`UPDATE` שנשלח בפועל כולל את הגרסה המקורית ב-`WHERE`, כך שאם שורה כבר שונתה *בין* הבדיקה הידנית ל-`SaveChangesAsync` (חלון race תיאורטי), יוחזרו 0 שורות מושפעות ו-EF יזרוק `DbUpdateConcurrencyException`, שנתפס ומתורגם גם הוא ל-409.

**מניעת Lost Update:** השילוב משמש הגנה כפולה - הבדיקה הידנית תופסת את רוב המקרים מוקדם וברור, וה-concurrency token של EF סוגר את חלון ה-race הנותר בין קריאה לכתיבה.

**מעברי סטטוס מותרים** (state machine ב-`RequestsService.AllowedTransitions`):

```
New → InProgress
InProgress → Waiting | Completed
Waiting → InProgress | Completed
Completed → (סופי, אין מעברים ממנו)
```

מעבר שלא ברשימה (כולל חזרה ל-New) נדחה כ-`400 Bad Request` ("Invalid status value or unsupported status transition").

### Bulk Update - בחירה ב-Partial Success

ה-Endpoint (`PATCH /api/requests/batch-status`, עד 100 פריטים) מעבד כל פריט בנפרד ומחזיר **200 OK** עם רשימת הכשלונות בלבד (רשימה ריקה = הכל הצליח).

**נימוק הבחירה ב-Partial Success על פני All-or-Nothing:**
1. **חוויית משתמש:** בתרחיש אמיתי של טריאז' פניות, עדכון קבוצתי בדרך כלל נבחר ע"י המשתמש על בסיס תצוגה שכבר "מיושנת" במעט (כמה שניות בין טעינת המסך ללחיצה על הכפתור). דחיית כל ה-batch בגלל פריט בודד שהשתנה בינתיים (Conflict) או לא קיים יותר (נמחק) הייתה פוגעת משמעותית בחוויית העבודה - המשתמש היה צריך לנסות שוב ושוב עד שאף פריט לא "נתפס" כבעייתי.
2. **אין תלות לוגית בין הפניות:** כל פנייה ב-batch היא ישות עצמאית - אין קשר עסקי/לוגי בין עדכון פנייה אחת להצלחת עדכון פנייה אחרת, ולכן אין סיבה לחסום את האחת בגלל כשל באחרת.

Partial Success מאפשר להתקדם עם כל מה שתקף, ומחזיר ללקוח בדיוק אילו פריטים נכשלו ומדוע, כך שהוא יכול לטפל בהם נקודתית. אין Transaction עוטף את כל ה-batch - כל פריט נשמר (`SaveChangesAsync`) בנפרד, ועקביות על פריט בודד מובטחת ע"י ה-Concurrency Token.


## Cache ו-Invalidation

**מה ממומש בקאש:** `StatusCounts`/`PriorityCounts` (האגרגציה המוצגת בכרטיסי הסיכום) בלבד - **לא** רשימת הפריטים המדופדפת, שחייבת תמיד להיות עדכנית לחלוטין.

**נימוק הבחירה:** האגרגציה דורשת `GROUP BY` על כל הסט המסונן (יקרה יחסית בהשוואה לשליפת עמוד בודד), ואינה חייבת דיוק תת-שנייתי - סטייה של עד 30 שניות בספירה מוצגת היא קביל לחלוטין עבור נתון סיכום, בעוד שרשימת הפריטים חייבת להיות מדויקת (אחרת המשתמש פועל על נתונים שגויים - למשל מעדכן גרסה לא נכונה).

**מימוש:** `IMemoryCache` (מובנה, ללא חבילה נוספת), מפתח = צירוף פרמטרי הסינון הרלוונטיים לאגרגציה (Organization/Status/Priority/AssignedTo/FromDate/ToDate/SearchTerm - **לא** Sort/Page, שלא משפיעים על האגרגציה). Expiration אבסולוטי של 30 שניות.

**Invalidation:** דפוס "cache generation token" - `CancellationTokenSource` סטטי; כל ערך במטמון נרשם גם עם `AddExpirationToken` שמצביע על ה-token הנוכחי. בכל כתיבה שבה הסטטוס השתנה בפועל (status update בודד, או bulk שבו לפחות פריט אחד השתנה - פעם אחת בסוף ה-bulk) ה-token מבוטל (`Cancel()`) ומוחלף בחדש - מבטל את **כל** ערכי המטמון הקיימים בבת אחת, ללא צורך במיפוי invalidation לפי כל צירוף פילטרים אפשרי.

**אומת בפועל** (ראו היסטוריית הסשן): קריאה ראשונה → MISS (מחושב ונשמר), קריאה זהה שנייה → HIT (מוגש מהמטמון), עדכון סטטוס → קריאה חוזרת מציגה מיד את הספירה המעודכנת (לא מיושנת).

**הערת Scale-out:** `IMemoryCache` הוא **per-instance** (in-process). בסביבה עם כמה מופעי שרת מאחורי Load Balancer, כתיבה במופע A מבטלת רק את המטמון שלו - מופע B ימשיך להגיש ערכים ישנים עד שפג התוקף שלהם (עד 30 שניות, לא מעבר). לעקביות מלאה בין מופעים נדרש `IDistributedCache` (למשל Redis) במקום `IMemoryCache` - שינוי מקומי יחסית (אותו Interface), שלא בוצע כאן כי אין בסקופ הנוכחי יותר ממופע שרת יחיד.

## מדידת ביצועים

נמדד בפועל (לא הערכה) על בסיס 100,000 הרשומות, באמצעות `sqlcmd` מול LocalDB עם `SET STATISTICS IO, TIME ON`. המתודולוגיה זהה לכל אינדקס: מריצים את השאילתה שהוא נועד לשרת פעמיים - פעם רגילה (SQL Server בוחר בעצמו אם להשתמש באינדקס), ופעם עם `WITH (INDEX(0))` שמכריח קריאה מהטבלה הבסיסית בלבד ומדמה "כאילו האינדקס לא קיים". ה-Script המלא, הניתן להרצה חוזרת לאימות עצמי, נמצא ב-[`RM-Backend/Scripts/performance_per_index.sql`](RM-Backend/Scripts/performance_per_index.sql).

### טבלה מרכזית: שאילתה לכל אינדקס, עם/בלי

| אינדקס | שאילתה מייצגת | Logical Reads עם | Logical Reads בלי | שיפור |
|---|---|---|---|---|
| `IX_Requests_CreatedAt` | מיון ברירת מחדל (ללא סינון), עמוד ראשון | 156 | 1,723 | פי 11 |
| `IX_Requests_Status` | `GROUP BY Status` (StatusCounts) | 353 | 1,723 | פי 4.9 |
| `IX_Requests_Priority` | `GROUP BY Priority` (PriorityCounts) | 352 | 1,723 | פי 4.9 |
| `IX_Requests_Status_CreatedAt` (מורכב) | `WHERE Status=0 ORDER BY CreatedAt DESC` + דפדוף - **זו בדיוק השאילתה של** `GET /api/requests` | **38** | 1,723 | **פי 45** |

ה"בלי" זהה (1,723) בכל השורות - כך זה אמור להיות: `WITH (INDEX(0))` מכריח תמיד סריקה מלאה של אותה טבלה בת 100,000 שורות, לא משנה איזו שאילתה מופעלת מעליה. זו גם בדיקת-שפיות שהמדידות אמינות (לא רעש אקראי).

### ה-Bottleneck שזוהה, ולמה האינדקס המורכב הוא הפתרון המשמעותי ביותר

לפני שהאינדקס המורכב נוסף, SQL Server נאלץ לבחור בין שני אינדקסים חד-ממדיים בשביל `WHERE Status=0 ORDER BY CreatedAt DESC` - הוא בחר לסרוק לפי `IX_Requests_CreatedAt` (כדי לקבל את סדר המיון בחינם) ולבדוק את ה-Status כתנאי שיורי על כל שורה (`Key Lookup` אחר `Key Lookup`). האינדקס המורכב פותר את זה: SQL Server יכול כעת *לקפוץ ישירות* ל-Status=0 כשהוא כבר ממוין לפי CreatedAt בתוך אותו אינדקס - `Index Seek` נקי במקום `Scan`+`Lookup` לכל שורה. זו הסיבה שהשיפור שלו (פי 45) גדול משמעותית מזה של כל אינדקס חד-ממדי בודד בטבלה למעלה - הוא היחיד שמשרת את *שני* חלקי השאילתה (הסינון והמיון) יחד.

## בדיקות אוטומטיות

**Backend (xUnit, 10 בדיקות אינטגרציה - HTTP אמיתי דרך `WebApplicationFactory`):**
- `RequestsApiIntegrationTests` (InMemory): סינון, ולידציית `SortBy`, עדכון תקין, גרסה ישנה → 409 (הבדיקה הידנית), מעבר סטטוס אסור → 400, Bulk עם כשל חלקי.
- `ConcurrencyIntegrationTests` (SQLite): race מדומה אחרי הבדיקה הידנית → 409 והכתיבה המתחרה נשמרת; 10 בקשות מקבילות → אחת מצליחה; כשל concurrency בפריט אחד ב-Bulk לא מפיל את הבאים; עדכון לאותו סטטוס לא כותב כלום.

**Frontend (Vitest, 19 בדיקות):** קומפוננטות (טבלה כולל טיפול ב-409, מודאל היסטוריה, פילטרים, דפדוף, דשבורד) ו-`NotificationService`.

## החלטות טכנולוגיות משמעותיות

### 1. Partial Success ב-Bulk Update (לא All-or-Nothing)

**החלופה שנשקלה:** All-or-Nothing - דחיית כל ה-batch אם פריט בודד נכשל (Conflict/לא קיים/מעבר אסור).

**הבחירה וההנמקה:** Partial Success - כל פריט מעודכן בנפרד, ומוחזרת רשימת הכשלונות בלבד (ריקה = הצלחה מלאה). שני נימוקים: (1) **חוויית משתמש** - אין סיבה לחסום עדכון של כל הפריטים התקינים בגלל פריט בעייתי בודד; המשתמש לא צריך לנסות שוב ושוב עד שאף פריט לא "נתפס". (2) **הפניות אינן תלויות זו בזו לוגית** - אין קשר עסקי בין עדכון פנייה אחת להצלחת עדכון פנייה אחרת, ולכן אין סיבה טכנית או עסקית שלא לאשר את השאר. פירוט מלא בסעיף [Concurrency ו-Bulk Update](#concurrency-ו-bulk-update).

### 2. Seeding לפי דרישה בלבד (לא אוטומטי בכל עליית שרת)

**ההתלבטות:** הזרקת 100,000 רשומות בכל `dotnet run` הייתה מאטה משמעותית את מחזור הפיתוח השוטף (כל הרצה מקומית/בדיקה הייתה ממתינה לסיום ה-seeding).

**הבחירה וההנמקה:** `SeedService` מופעל אך ורק כשמועבר פרמטר ייעודי בשורת הפקודה (`dotnet run -- seedTestData`), ולא כברירת מחדל. כך מתאפשרת גמישות מלאה - פיתוח שוטף עם מסד נתונים ריק/קטן ומהיר, מול בדיקות עומס/ביצועים עם מלוא 100K הרשומות לפי דרישה מפורשת, ללא פגיעה בקצב הפיתוח.

## מגבלה ידועה ושיפור אפשרי

**מגבלה ידועה:** `ChangedBy` בטבלת ה-Audit קבוע לערך `"CurrentAdmin"` - אין Authentication/Authorization בסקופ הנוכחי של המערכת, ולכן אין משתמש מזוהה אמיתי לשייך לשינוי.

**שיפור אפשרי:** הוספת שכבת Authentication וניהול משתמשים, והוספה של ארגונים.

## שימוש בכלי AI

העבודה בוצעה בשיתוף עם Claude Code (Anthropic) כ-AI assistant, בשלושה שלבים עבור כל פיצ'ר:

1. **מסירת האפיון:** עבור כל משימה התחלתי עם איפיון של הקוד וכתיבת פרומפט מתאים.
2. **סקירת התוכנית:** עברתי על התוכנית שהצאט נתן, ונתתי הערות וכיווני עבודה; Claude Code מימש בהתאם.
3. **סקירת התוצר ותיקונים:** עברתי על מה שמומש ונתתי הנחיות לתיקון חלקים ספציפיים (ניסוח UI, ארגון קוד, ארכיטקטורת הקומפוננטות בפרונט) לפני הסגירה הסופית.

כל קוד אומת בפועל לאורך התהליך - `dotnet build`/`ng build` נקיים, הרצת הבדיקות האוטומטיות (`dotnet test`/`ng test`), והרצת האפליקציה בפועל.
