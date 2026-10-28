# תוכנית עבודה וחלוקה למשימות
---

## שלב 1: תשתית וניהול נתונים 📦

### תיאור
הגדרת מודלים (Request, Audit), Enums, יצירת AppDbContext והקמת אינדקסים (כולל אינדקס מורכב Status + CreatedAt).

### משימות
- ✅ יצירת Entity Models עם DataAnnotations ([Required], [MaxLength], Enum validation)
- ✅ הגדרת AppDbContext עם Fluent API ל-Relationships
- ✅ יצירת אינדקסים בת-מימדיים (B-tree) והאינדקס המורכב (Composite)
- ✅ הוספת CHECK constraints בברמת-DB לאכיפת ערכי Enum

### תוצר
שכבת נתונים יציבה עם Migrations EF Core נקיים

### מאמץ
**⭐ S** (קטן)

---

## שלב 2: פיתוח ה-API ותשתיות השרת 🔌

### תיאור
מימוש Endpoints לפי סדר האפיון:
1. **GET** – שליפה עם Pagination, סינון וסדר
2. **PATCH** – עדכון סטטוס יחיד עם Optimistic Concurrency
3. **GET Audit** – היסטוריית שינויים לבקשה
4. **PATCH Bulk** – עדכון הרבה פריטים עם Partial Success

### משימות
- ✅ מימוש RequestsService עם IQueryable deferral (ללא N+1)
- ✅ אימוש UpdateStatusAsync עם Concurrency Token (Version)
- ✅ בניית RequestStatusAuditService לתיעוד שינויים
- ✅ BulkUpdateStatusAsync עם Pattern של Partial Success
- ✅ Controllers עם DTO validation (DataAnnotations + EnumDataType)

### תוצר
שרת ASP.NET Core יציב המטפל ב-Concurrency ומונע Lost Updates

### מאמץ
**⭐⭐ M** (בינוני)
---

## שלב 3: יכולות חוצות-מערכת 🚀

### תיאור
אופטימיזציה ו-Caching חכם:
- הוספת IMemoryCache לאגרגציות (COUNT per Status/Priority)
- Invalidation מבוסס Change Token
- מדידת ביצועים מדודה (Performance Benchmarking עם 100,000 רשומות)

### משימות
- ✅ יצירת Aggregation Service עם Memory Cache
- ✅ כתיבת SQL Performance tests בעזרת STATISTICS IO
- ✅ תיעוד ההשוואה בפועל (WITH vs WITHOUT אינדקסים)
- ✅ וידוא שכל query משתמשת באינדקס הנכון

### תוצר
ביצועים אופטימליים: ירידה של **45x** ב-Logical Reads למהפכת הסינון+מיון+דפדוף

### מאמץ
**⭐⭐ M** (בינוני)

---

## שלב 4: צד לקוח – Angular 🎨

### תיאור
הקמת ארכיטקטורה רכיבים:
- **Container Components** – ניהול state ו-HTTP
- **Presentational Components** – UI ותכנות responsive
- RxJS לניהול זרמים (debounceTime, switchMap, takeUntil)
- Angular Signals לביטול Race Conditions ב-Zoneless אפליקציה

### משימות
- ✅ RequestsDashboardComponent (Container, תחת `pages/`) עם ניהול פילטרים ודפדוף; AppComponent הוא מעטפת בלבד
- ✅ RequestsTableComponent ("Half-smart") - טבלה, בחירת שורות, עדכון סטטוס בשורה ו-Bulk
- ✅ FilterBar עם Search, Sort buttons ו-Advanced filters
- ✅ AggregationsPanel לתצוגת סטטיסטיקות
- ✅ PaginationComponent גנרי
- ✅ AuditHistoryModalComponent לתיעוד
- ✅ ToastComponent + NotificationService להודעות (במקום `alert()`)

### תוצר
ממשק משתמש רספונסיבי, מהיר ללא Race Conditions

### מאמץ
**⭐⭐ M** (בינוני)

---

## שלב 5: בדיקות ותיעוד ✅

### תיאור
מערכת בדיקות שלמה + תיעוד מפורט:

**Backend:**
- 10 Integration Tests (סינון, ולידציה, Concurrency מול SQLite כולל race מדומה ובקשות מקבילות, Bulk)

**Frontend:**
- 19 Unit Tests (קומפוננטות, טיפול ב-409, NotificationService)

**Documentation:**
- README.md עם setup, מבנה ופרטי ביצועים
- TASKS.md לעדכוני Progress
- SQL Performance scripts לעמידת Performance

### משימות
- ✅ כתיבת Integration Tests עם xUnit
- ✅ כתיבת Unit Tests עם Vitest
- ✅ תיעוד מלא של Setup ו-Running
- ✅ Performance Documentation עם טבלאות בפועל

### תוצר
מערכת מאומתת, מוכנה להפעלה בלחיצת כפתור

### מאמץ
**⭐⭐ M** (בינוני)

---

## תלות מרכזית ⛓️

```
שלב 1 (תשתית)
    ↓
שלב 2 (API) ← תלוי בשלב 1
    ↓
שלב 3 (אופטימיזציה) ← תלוי בשלב 2
    ↓
שלב 4 (Angular) ← תלוי בשלב 2
    ↓
שלב 5 (בדיקות + תיעוד) ← תלוי בשלב 2, 3, 4
```

**ביאור:** השלמת שכבת הנתונים (שלב 1) איפשרה את פיתוח ה-API (שלב 2), אשר היווה בסיס ל:
- אופטימיזציה (שלב 3)
- פיתוח הממשק (שלב 4)
- בדיקות וולידציה (שלב 5)

---

## סטטוס סיום

| שלב | משימה | סטטוס |
|-----|--------|--------|
| 1 | תשתית וניהול נתונים | ✅ הושלם |
| 2 | API ותשתיות השרת | ✅ הושלם |
| 3 | יכולות חוצות-מערכת | ✅ הושלם |
| 4 | Angular Frontend | ✅ הושלם |
| 5 | בדיקות ותיעוד | ✅ הושלם |

---
