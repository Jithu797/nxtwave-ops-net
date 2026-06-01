# LMS Content Operations Dashboard

> **Internal B2B tool for NxtWave's content operations team.**  
> Replaces manual Google Sheets / Excel tracking with an automated, auditable pipeline for loading course content (Quizzes, Readings, PPTs, Audio, Activities) into the LMS.

---

## Table of Contents

1. [What This Project Does](#1-what-this-project-does)
2. [Tech Stack](#2-tech-stack)
3. [Project Structure — File by File](#3-project-structure--file-by-file)
4. [Database Schema](#4-database-schema)
5. [Enums Reference](#5-enums-reference)
6. [Service Layer](#6-service-layer)
7. [API Reference](#7-api-reference)
8. [Razor Pages (UI)](#8-razor-pages-ui)
9. [Authentication Flow](#9-authentication-flow)
10. [Background Jobs (Hangfire)](#10-background-jobs-hangfire)
11. [Excel Upload Pipeline](#11-excel-upload-pipeline)
12. [Validation Rules](#12-validation-rules)
13. [Google Sheets Integration](#13-google-sheets-integration)
14. [AI Microservice Integration](#14-ai-microservice-integration)
15. [Response Format](#15-response-format)
16. [Configuration Reference](#16-configuration-reference)
17. [Getting Started — Local Setup](#17-getting-started--local-setup)
18. [Getting Started — Docker](#18-getting-started--docker)
19. [Seed Data](#19-seed-data)
20. [Troubleshooting](#20-troubleshooting)

---

## 1. What This Project Does

The content operations team at NxtWave loads dozens of course items into the LMS platform every day. Before this tool existed, the team:

- Tracked every item in a shared Google Sheet manually
- Had no validation before upload (wrong enums, missing fields slipped through)
- Received no automated reports on daily throughput or error rates
- Had to manually copy-paste status updates back into the tracking sheet

**This dashboard automates all of that:**

| Manual Step (Old) | Automated Step (New) |
|---|---|
| Type each item into Google Sheets | Upload a bulk Excel file → instant DB insert |
| Remember valid Type/Track values | Server validates every row before saving |
| Check item status by scrolling Sheet | Dashboard shows status pipeline with charts |
| Weekly manual status report | Nightly report auto-generated at 2am |
| Manually sync Sheet after each update | Hangfire job syncs every 15 minutes |
| No audit trail | Every validation run saved to `ValidationLogs` |

---

## 2. Tech Stack

| Layer | Technology | Why |
|-------|-----------|-----|
| Backend framework | ASP.NET Core 8 | LTS, fast, cross-platform |
| UI | Razor Pages (server-rendered) | Simple for internal tools; no SPA overhead |
| ORM | Entity Framework Core 8 | Code-first migrations, LINQ queries |
| Database | SQL Server (LocalDB dev / Docker prod) | Native EF support; Hangfire requires it |
| Background jobs | Hangfire 1.8 | Cron + queued jobs with SQL storage and dashboard UI |
| Excel upload | EPPlus 7 | Read `.xlsx` row-by-row with header detection |
| Excel export | ClosedXML | Write formatted `.xlsx` reports |
| Auth | JWT Bearer (HS256) | Stateless; works for API + Swagger testing |
| Validation | FluentValidation 11 | Declarative rules, auto-wired to ASP.NET model binding |
| Google Sheets | Google.Apis.Sheets.v4 | Official SDK; mock-first so app works without credentials |
| AI proxy | HttpClient → FastAPI | Forwards raw rows to a Python microservice for structuring |
| API docs | Swashbuckle (Swagger) | Auto-generated from XML comments + controller attributes |
| CSS/JS | Bootstrap 5.3 + Chart.js 4 | CDN, no build step needed |
| Container | Docker + docker-compose | Bundles app + SQL Server together |

---

## 3. Project Structure — File by File

```
LMSDashboard/
│
├── Program.cs                          ← App entry point. Wires all DI, middleware, Hangfire jobs, routing.
├── LMSDashboard.csproj                 ← NuGet package list (all 10 packages declared here)
├── appsettings.json                    ← Base config (DB connection, JWT, Google Sheets, AI URL)
├── appsettings.Development.json        ← Dev overrides (verbose logging, dev DB name)
├── .env.example                        ← Template of all environment variables the app reads
├── .gitignore                          ← Excludes bin/obj/secrets/credentials
├── Dockerfile                          ← Multi-stage build: SDK → publish → runtime image
├── docker-compose.yml                  ← Runs app + SQL Server 2022 together
│
├── Models/                             ← EF Core entity classes (map to DB tables)
│   ├── ContentItem.cs                  ← Main entity. Holds a single course item + all its dates/status.
│   ├── ValidationLog.cs                ← One row per validation rule run per item.
│   ├── SyncLog.cs                      ← One row per Google Sheets sync operation.
│   ├── JobRecord.cs                    ← Audit log for background job runs.
│   └── ReportCache.cs                  ← Cached JSON of the nightly monthly report.
│
├── Data/
│   └── AppDbContext.cs                 ← DbContext. Configures relationships, indexes, enum→string
│                                         conversions, and seeds 20 sample ContentItems.
│
├── Migrations/
│   ├── 20260601000000_InitialCreate.cs ← Creates all 5 tables + indexes + inserts seed rows.
│   └── AppDbContextModelSnapshot.cs   ← EF snapshot (tracks current model state for future migrations).
│
├── DTOs/                               ← Data Transfer Objects (shape the API inputs/outputs)
│   ├── ApiResponse.cs                  ← Generic wrapper: { success, data, message, errors[] }
│   ├── ContentDTOs.cs                  ← ContentItemDto, PaginatedResult, UpdateStatusRequest, etc.
│   └── ReportDTOs.cs                   ← MonthlyReportDto, PeakDayDto, DailyCountDto.
│
├── Validators/
│   └── UpdateStatusRequestValidator.cs ← FluentValidation: status string must be a valid enum value.
│
├── Services/                           ← All business logic lives here (no logic in controllers)
│   ├── ContentService.cs               ← UploadFromExcel, GetPaginated, GetById, UpdateStatus, SoftDelete
│   ├── ValidationService.cs            ← Runs 4 rules against an item; writes ValidationLog rows.
│   ├── SheetsService.cs                ← Mock or real Google Sheets sync; writes SyncLog rows.
│   ├── ReportService.cs                ← Calculates monthly stats, exports Excel, caches nightly report.
│   └── AiStructureService.cs           ← HTTP POST proxy to Python FastAPI microservice.
│
├── Controllers/                        ← API layer. Thin: validate input → call service → return response.
│   ├── AuthController.cs               ← POST /api/auth/token  (dev JWT issuer)
│   ├── ContentController.cs            ← CRUD + upload + validate endpoints
│   ├── ReportsController.cs            ← Monthly stats + Excel export
│   ├── SheetsController.cs             ← Manual sync trigger + sync log viewer
│   └── AiController.cs                 ← Proxy to AI microservice
│
├── Jobs/                               ← Hangfire job classes (called by the scheduler)
│   ├── SheetsSyncJob.cs                ← Runs every 15 min; syncs items changed in last 15 min.
│   └── NightlyReportJob.cs             ← Runs at 2am UTC; calls ReportService.CacheNightlyReport().
│
├── Middleware/
│   └── ExceptionMiddleware.cs          ← Catches all unhandled exceptions; returns ApiResponse JSON.
│
├── Pages/                              ← Razor Pages (server-rendered HTML, not an API)
│   ├── Dashboard.cshtml / .cs          ← 4 metric cards + doughnut + bar chart + recent items table
│   ├── Content.cshtml / .cs            ← Paginated table + filter form + status modal + validate button
│   ├── Upload.cshtml / .cs             ← File upload form; shows created/failed summary after submit
│   ├── Reports.cshtml / .cs            ← Monthly stats cards + daily bar chart + 3 breakdown tables
│   ├── Sync.cshtml / .cs               ← Last sync time + 10-row log table + Sync Now button
│   ├── Content/
│   │   └── View.cshtml / .cs           ← Single item detail: all fields + full validation log table
│   └── Shared/
│       ├── _Layout.cshtml              ← Master layout: navbar + Bootstrap + Chart.js CDN links
│       ├── _ViewImports.cshtml         ← Global @using + @addTagHelper for all pages
│       └── _ViewStart.cshtml           ← Sets _Layout as default layout
│
└── wwwroot/
    ├── css/site.css                    ← Custom styles (card radius, table header, badge sizing)
    └── js/site.js                      ← Auto-dismiss success alerts after 5 seconds
```

---

## 4. Database Schema

### ContentItems

The core table. Every course item loaded or queued for loading lives here.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `uniqueidentifier` PK | GUID, generated on creation |
| `Title` | `nvarchar(500)` NOT NULL | Display name of the content item |
| `Type` | `nvarchar` (enum as string) | Quiz / Reading / Audio / PPT / Activity |
| `Track` | `nvarchar` (enum as string) | Foundation / B1 / Advanced / Applied / Crescent |
| `Difficulty` | `nvarchar` (enum as string) | Easy / Medium / Hard |
| `Status` | `nvarchar(450)` | Pending / InBeta / Validated / InProduction / Failed |
| `BetaUploadedAt` | `datetime2` nullable | Set automatically when status reaches InBeta |
| `ProdUploadedAt` | `datetime2` nullable | Set automatically when status reaches InProduction |
| `ValidatedAt` | `datetime2` nullable | Set automatically when status reaches Validated |
| `CreatedAt` | `datetime2` | UTC timestamp of creation |
| `StatusChangedAt` | `datetime2` nullable | Updated every time status changes; used by Hangfire sync |
| `CreatedBy` | `nvarchar(200)` | Email/username of uploader |
| `Notes` | `nvarchar(2000)` nullable | Free-text notes |
| `IsDeleted` | `bit` | Soft-delete flag; deleted items are never shown in UI |

**Indexes:** Status, Track, Type, CreatedAt, IsDeleted, StatusChangedAt

---

### ValidationLogs

Every time you run validation on an item, 4 rows are inserted (one per rule).

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `uniqueidentifier` PK | |
| `ContentItemId` | `uniqueidentifier` FK | References ContentItems(Id), CASCADE DELETE |
| `RuleName` | `nvarchar(200)` | e.g. `TitleNotEmpty`, `BetaUploadedAtRequiredForBetaStatus` |
| `Result` | `nvarchar` (enum) | Pass / Fail |
| `Message` | `nvarchar(1000)` | Human-readable explanation |
| `CheckedAt` | `datetime2` | When this rule was evaluated |

---

### SyncLogs

Written every time a Google Sheets sync completes (real or mock).

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `uniqueidentifier` PK | |
| `SheetName` | `nvarchar(200)` | Name of the Google Sheet tab synced |
| `RowsUpdated` | `int` | Number of content items pushed to Sheets |
| `SyncedAt` | `datetime2` | UTC timestamp |
| `Status` | `nvarchar(100)` | "Success", "Queued", "Error" |

---

### JobRecords

Audit log for background job executions.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `uniqueidentifier` PK | |
| `JobType` | `nvarchar(200)` | Human-readable job name |
| `PayloadJson` | `nvarchar(max)` nullable | Input payload if applicable |
| `StartedAt` | `datetime2` | When job started |
| `CompletedAt` | `datetime2` nullable | Null if still running or never completed |
| `Result` | `nvarchar(2000)` nullable | Summary message or error |

---

### ReportCaches

Stores the JSON of the most recent nightly monthly report so the Reports page loads instantly.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `uniqueidentifier` PK | |
| `ReportKey` | `nvarchar(100)` UNIQUE | Format: `monthly_2026-05` |
| `DataJson` | `nvarchar(max)` | Full `MonthlyReportDto` serialized as JSON |
| `GeneratedAt` | `datetime2` | When cache was written |
| `ExpiresAt` | `datetime2` | 25 hours after generation |

---

### Entity Relationships

```
ContentItems (1) ──── (many) ValidationLogs
     │
     └── IsDeleted (soft-delete, never physically removed)

SyncLogs      ← written independently per sync operation
JobRecords    ← written independently per background job
ReportCaches  ← one row per month key, upserted nightly
```

---

## 5. Enums Reference

All enums are stored as **strings** in the database (not integers) for readability.

### ContentType
| Value | Description |
|-------|-------------|
| `Quiz` | Multiple-choice or graded quiz |
| `Reading` | Text-based reading material |
| `Audio` | Audio lesson or podcast |
| `PPT` | PowerPoint / slide presentation |
| `Activity` | Interactive exercise or lab |

### Track
| Value | Description |
|-------|-------------|
| `Foundation` | Beginner-level track |
| `B1` | Intermediate level |
| `Advanced` | Advanced level |
| `Applied` | Applied / practical track |
| `Crescent` | Crescent program track |

### Difficulty
| Value |
|-------|
| `Easy` |
| `Medium` |
| `Hard` |

### ContentStatus (pipeline order)
| Value | Meaning |
|-------|---------|
| `Pending` | Created, not yet uploaded to any environment |
| `InBeta` | Uploaded to beta LMS; `BetaUploadedAt` is set |
| `Validated` | QA-reviewed and approved; `ValidatedAt` is set |
| `InProduction` | Live in production LMS; `ProdUploadedAt` is set |
| `Failed` | Upload or validation failed; needs rework |

> When you update status to `InBeta`, `Validated`, or `InProduction`, the corresponding timestamp column is automatically set if it is currently null.

---

## 6. Service Layer

Services contain all business logic. Controllers are intentionally thin — they only parse input and call a service method.

### ContentService

**File:** [Services/ContentService.cs](Services/ContentService.cs)

| Method | What it does |
|--------|-------------|
| `UploadFromExcelAsync(stream, uploadedBy)` | Opens the `.xlsx` stream with EPPlus. Reads row 1 as headers. Validates required columns exist (`Title`, `Type`, `Track`, `Difficulty`). Processes each subsequent row — parses enums, skips rows with errors. Bulk-inserts valid rows. Returns a summary with created count, failed count, and per-row error messages. |
| `GetPaginatedAsync(filters)` | Builds an EF query filtered by Status/Track/Type (if provided). Applies `Skip`/`Take` for pagination. Returns `PaginatedResult<ContentItemDto>` with total count and page info. |
| `GetByIdAsync(id)` | Fetches item + its `ValidationLogs` (via `Include`). Returns full `ContentItemDto` with nested validation history. |
| `UpdateStatusAsync(id, newStatus)` | Parses the status string to enum. Updates `Status` and `StatusChangedAt`. Auto-fills `BetaUploadedAt`, `ValidatedAt`, `ProdUploadedAt` if transitioning to those statuses. Inserts a `SyncLog` row with `Status = "Queued"` to record the pending sync. |
| `SoftDeleteAsync(id)` | Sets `IsDeleted = true`. The item stays in the DB forever but never appears in any query. |

---

### ValidationService

**File:** [Services/ValidationService.cs](Services/ValidationService.cs)

Runs 4 rules against a `ContentItem` and writes one `ValidationLog` row per rule.

| Rule Name | What it checks |
|-----------|---------------|
| `TitleNotEmpty` | `Title` is not null/whitespace |
| `TypeIsValidEnum` | `Type` is a defined value in the `ContentType` enum |
| `TrackIsValidEnum` | `Track` is a defined value in the `Track` enum |
| `BetaUploadedAtRequiredForBetaStatus` | If `Status >= InBeta`, then `BetaUploadedAt` must have a value |

Each rule emits a `Pass` or `Fail` result with a message. All 4 results are saved to `ValidationLogs` in a single `SaveChangesAsync` call.

---

### SheetsService

**File:** [Services/SheetsService.cs](Services/SheetsService.cs)

| Method | What it does |
|--------|-------------|
| `SyncRecentChangesAsync(since)` | Finds all non-deleted items where `StatusChangedAt >= since`. Passes them to the internal sync. |
| `SyncAllAsync()` | Fetches all non-deleted items regardless of when they changed. Used by the manual Sync Now button. |

**Mock mode (default):** When `GoogleSheets:Enabled = false`, the service logs each item to the console and returns the count. A `SyncLog` row is still written with `Status = "Success"` so the Sync page shows history.

**Real mode:** When `GoogleSheets:Enabled = true`, the service calls `SyncToGoogleSheetsAsync()` which uses the Google Sheets v4 API. Requires a service account JSON file and a spreadsheet ID in config.

---

### ReportService

**File:** [Services/ReportService.cs](Services/ReportService.cs)

| Method | What it does |
|--------|-------------|
| `GetMonthlyReportAsync(year, month)` | Queries all non-deleted items created in the given month. Builds breakdowns by Status, Track, and Type. Calculates daily counts, peak day, and error rate (Failed items / total × 100). |
| `ExportMonthlyReportToExcelAsync(year, month)` | Generates the same report, then writes it to a ClosedXML workbook with 3 sheets: Summary, By Status, Daily. Returns the raw `byte[]`. |
| `CacheNightlyReportAsync()` | Calls `GetMonthlyReportAsync` for the current month, serializes to JSON, and upserts a `ReportCache` row with key `monthly_YYYY-MM`. Cache expires after 25 hours. |

---

### AiStructureService

**File:** [Services/AiStructureService.cs](Services/AiStructureService.cs)

A thin HTTP proxy. Takes a `rows` array, wraps it in `{ "rows": [...] }`, and POSTs it to `{AiService:BaseUrl}/ai/structure`. Returns whatever JSON the microservice responds with. The Python FastAPI service is responsible for interpreting and structuring the raw data.

---

## 7. API Reference

All endpoints except `POST /api/auth/token` require a `Bearer` JWT token.

### Auth

#### `POST /api/auth/token`
Issues a JWT token. For development only — replace with a real identity provider in production.

**Request:**
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "expires": "2026-06-02T16:00:00Z"
  },
  "message": "Success",
  "errors": []
}
```

---

### Content

#### `POST /api/content/upload`
Uploads an Excel file and bulk-creates content items.

- **Content-Type:** `multipart/form-data`
- **Field:** `file` — `.xlsx` file

**Response:**
```json
{
  "success": true,
  "data": {
    "created": 18,
    "failed": 2,
    "errors": [
      "Row 3: Invalid Type 'Video'.",
      "Row 7: Title is empty."
    ]
  },
  "message": "Upload complete: 18 created, 2 failed."
}
```

---

#### `GET /api/content`
Paginated, filtered list of content items.

**Query params:**

| Param | Type | Example | Description |
|-------|------|---------|-------------|
| `status` | string | `InBeta` | Filter by ContentStatus enum value |
| `track` | string | `Foundation` | Filter by Track enum value |
| `type` | string | `Quiz` | Filter by ContentType enum value |
| `page` | int | `1` | Page number (default: 1) |
| `pageSize` | int | `20` | Items per page (max: 100, default: 20) |

**Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "00000000-0000-0000-0000-000000000001",
        "title": "Sample Content Item 1",
        "type": "Reading",
        "track": "B1",
        "difficulty": "Easy",
        "status": "Pending",
        "betaUploadedAt": null,
        "prodUploadedAt": null,
        "validatedAt": null,
        "createdAt": "2026-05-01T01:00:00Z",
        "createdBy": "bob@nxtwave.in",
        "notes": "Seeded item 1 for demo",
        "validationLogs": null
      }
    ],
    "totalCount": 20,
    "page": 1,
    "pageSize": 20,
    "totalPages": 1
  },
  "message": "Success"
}
```

---

#### `GET /api/content/{id}`
Returns a single content item with its full validation log history.

**Response `data` includes `validationLogs` array** — same shape as above but populated.

---

#### `PUT /api/content/{id}/status`
Updates the status of an item. Also sets timestamp columns automatically and queues a sync log entry.

**Request:**
```json
{ "status": "InBeta" }
```

**Validation:** `status` must be one of the `ContentStatus` enum values. Returns `400` if invalid, `404` if item not found.

---

#### `POST /api/content/{id}/validate`
Runs all 4 validation rules and saves results to `ValidationLogs`.

**Response:**
```json
{
  "success": true,
  "data": [
    { "ruleName": "TitleNotEmpty", "result": "Pass", "message": "Title is present." },
    { "ruleName": "TypeIsValidEnum", "result": "Pass", "message": "Type 'Quiz' is valid." },
    { "ruleName": "TrackIsValidEnum", "result": "Pass", "message": "Track 'Foundation' is valid." },
    { "ruleName": "BetaUploadedAtRequiredForBetaStatus", "result": "Fail", "message": "BetaUploadedAt is required when status is InBeta or later." }
  ],
  "message": "Some validation rules failed."
}
```

---

#### `DELETE /api/content/{id}`
Soft-deletes the item (`IsDeleted = true`). The row stays in the database.

---

### Reports

#### `GET /api/reports/monthly?year=2026&month=5`
Returns monthly statistics. `year` and `month` default to the current month if omitted.

**Response `data`:**
```json
{
  "totalItemsThisMonth": 20,
  "byStatus": { "Pending": 4, "InBeta": 5, "Validated": 4, "InProduction": 4, "Failed": 3 },
  "byTrack": { "Foundation": 4, "B1": 4, "Advanced": 4, "Applied": 4, "Crescent": 4 },
  "byType": { "Quiz": 4, "Reading": 4, "Audio": 4, "PPT": 4, "Activity": 4 },
  "peakDay": { "date": "2026-05-15T00:00:00Z", "count": 3 },
  "errorRatePercent": 15.0,
  "dailyCounts": [
    { "date": "2026-05-01T00:00:00Z", "count": 1 },
    ...
  ]
}
```

#### `GET /api/reports/monthly/export`
Returns a binary `.xlsx` file as a file download. Use this URL directly in a browser or `<a href>` tag.

---

### Sheets

#### `POST /api/sheets/sync`
Triggers an immediate sync for all items with `StatusChangedAt` in the last 24 hours.

**Response:** `{ "data": 5, "message": "Synced 5 items to Google Sheets." }`

#### `GET /api/sheets/logs?take=10`
Returns the most recent sync log entries.

---

### AI

#### `POST /api/ai/structure`
Forwards raw content rows to the Python AI microservice.

**Request:**
```json
{
  "rows": [
    { "rawTitle": "Intro to Python", "rawType": "video", "rawTrack": "foundation" }
  ]
}
```

**Response:** Whatever the FastAPI microservice returns (passed through unchanged). Returns `502 Bad Gateway` if the microservice is unreachable.

---

## 8. Razor Pages (UI)

The UI is server-rendered using ASP.NET Core Razor Pages. No JavaScript framework — Bootstrap 5.3 and Chart.js are loaded from CDN.

### `/Dashboard`

The home page. On load, the page model queries all non-deleted content items from the DB and computes:

- **4 metric cards**: Total Items, In Beta, Validated, In Production — each with a colored icon
- **Status pipeline doughnut chart** — built with Chart.js using a dictionary of `{StatusName: count}`
- **Track bar chart** — horizontal bar chart showing item count per track
- **Recent items table** — last 20 items ordered by `CreatedAt DESC`, with color-coded status badges

---

### `/Content`

The main data management page.

- **Filters** (top card): Status, Track, Type dropdowns. Submitting the form adds query params to the URL and re-fetches.
- **Table**: 20 items per page. Columns: Title, Type, Track, Difficulty, Status (badge), Created date.
- **Action buttons** per row:
  - Eye icon → navigates to `/Content/View/{id}`
  - Check icon → `POST` form that calls `OnPostValidate()` → runs validation
  - Pencil icon → opens the **Status Update Modal** (Bootstrap modal, populated via JavaScript `data-` attributes)
- **Pagination**: Page links at the bottom, preserving current filters in the URL.

---

### `/Upload`

Two-section layout:

- **Left**: File input + Submit button + Valid values reference card (shows all enum options so the uploader knows what to type in the Excel)
- **Right** (appears after upload): Result summary card with Created/Failed counts and a per-row error list

---

### `/Reports`

- **Stats cards**: Total items, error rate %, peak day count
- **Daily bar chart**: Chart.js bar chart, one bar per day of the month
- **3 breakdown tables**: By Status, By Track, By Type — each a simple 2-column table
- **Export button**: Direct link to `GET /api/reports/monthly/export` — the browser downloads the Excel file

---

### `/Sync`

- **Header**: Shows last sync timestamp from the most recent `SyncLog` row
- **Sync Now button**: `POST` form that calls `SyncAllAsync()` and refreshes the page
- **History table**: Last 10 `SyncLog` rows — SheetName, RowsUpdated, Status badge, timestamp

---

### `/Content/View/{id}`

Routed as `/Content/View/00000000-0000-0000-0000-000000000001`.

- **Left column**: Full item details in a `<dl>` definition list — all fields including timestamp columns
- **Right column**: Validation log table sorted newest-first + **Run Validation** button (POST form)

---

### Status Badge Colors

| Status | Badge Color |
|--------|------------|
| Pending | Grey (`bg-secondary`) |
| InBeta | Yellow (`bg-warning text-dark`) |
| Validated | Cyan (`bg-info text-dark`) |
| InProduction | Green (`bg-success`) |
| Failed | Red (`bg-danger`) |

---

## 9. Authentication Flow

```
Client                          AuthController              JWT Library
  │                                   │                          │
  │  POST /api/auth/token             │                          │
  │  { username, password }  ────────►│                          │
  │                                   │  check vs config         │
  │                                   │  values (admin/admin123) │
  │                                   │─────────────────────────►│
  │                                   │  HS256 sign with Secret  │
  │                                   │◄─────────────────────────│
  │  { token, expires }      ◄────────│                          │
  │                                   │                          │
  │  GET /api/content                 │                          │
  │  Authorization: Bearer <token> ──►│ (JwtBearer middleware    │
  │                                   │  validates automatically) │
  │  200 OK + data           ◄────────│                          │
```

**Token lifetime:** 8 hours.

**Claims included:**
- `ClaimTypes.Name` = username
- `ClaimTypes.Role` = "ContentOps"

**Important:** The `/api/auth/token` endpoint with hardcoded credentials is for **development only**. In production, replace `AuthController` with your actual identity provider (Azure AD, IdentityServer, etc.).

---

## 10. Background Jobs (Hangfire)

Hangfire stores job state in the same SQL Server database under its own set of tables (created automatically). You can view and manage jobs at `/hangfire`.

### Job: `sync-pending-status-changes`

**Schedule:** `*/15 * * * *` — every 15 minutes  
**Class:** `SheetsSyncJob.SyncPendingStatusChanges()`

What it does:
1. Calculates `since = DateTime.UtcNow.AddMinutes(-15)`
2. Calls `SheetsService.SyncRecentChangesAsync(since)`
3. That queries `ContentItems WHERE StatusChangedAt >= since AND IsDeleted = false`
4. Syncs those items to Google Sheets (mock or real)
5. Writes a `SyncLog` row

---

### Job: `nightly-report`

**Schedule:** `0 2 * * *` — every day at 2:00 AM UTC  
**Class:** `NightlyReportJob.GenerateNightlyReport()`

What it does:
1. Calls `ReportService.CacheNightlyReportAsync()`
2. That runs `GetMonthlyReportAsync()` for the current month
3. Serializes the result to JSON
4. Upserts a `ReportCache` row with key `monthly_YYYY-MM`
5. Sets `ExpiresAt = UtcNow + 25 hours`

The cached report is available for use by the Reports page to avoid re-computing on every page load.

---

### Hangfire Dashboard

Navigate to `/hangfire` in the browser (dev: no auth required; prod: restrict access).

The dashboard shows:
- All recurring job definitions and their next run time
- Job execution history (succeeded, failed, enqueued)
- Retry queues and dead-letter jobs

---

## 11. Excel Upload Pipeline

```
User selects .xlsx file
        │
        ▼
ContentController.Upload()
  → validates: file not null, extension is .xlsx
        │
        ▼
ContentService.UploadFromExcelAsync(stream, uploadedBy)
  1. EPPlus opens the stream as ExcelPackage
  2. Takes the first worksheet
  3. Reads row 1 → builds header→column index dictionary
  4. Checks all required headers exist: Title, Type, Track, Difficulty
     → If missing: return immediately with error list
  5. Loops rows 2..N:
     a. Reads Title (trims whitespace)
     b. Parses Type → ContentType enum (case-insensitive)
     c. Parses Track → Track enum
     d. Parses Difficulty → Difficulty enum
     e. Reads Notes column (optional)
     f. If any parse fails → adds error message, increments failed count, CONTINUES
     g. If all valid → creates ContentItem{Status=Pending} → adds to DbContext
  6. SaveChangesAsync() (one batch insert for all valid rows)
  7. Returns UploadSummaryDto { created, failed, errors[] }
```

**No partial rollbacks:** If row 3 fails and rows 4–10 are valid, rows 4–10 are still saved. The error list tells you exactly which rows failed and why.

---

## 12. Validation Rules

Rules are run by `POST /api/content/{id}/validate` or the Validate button in the UI.

| # | Rule Name | Condition for PASS |
|---|-----------|-------------------|
| 1 | `TitleNotEmpty` | `Title` is not null and not whitespace |
| 2 | `TypeIsValidEnum` | `Enum.IsDefined(item.Type)` — always true for DB items, guards against future data corruption |
| 3 | `TrackIsValidEnum` | `Enum.IsDefined(item.Track)` — same reasoning |
| 4 | `BetaUploadedAtRequiredForBetaStatus` | If `Status` is `InBeta`, `Validated`, or `InProduction` then `BetaUploadedAt` must not be null |

Every run inserts **4 new rows** into `ValidationLogs` (even if you run it multiple times). The View page shows all historical runs, newest first.

---

## 13. Google Sheets Integration

### Default (Mock Mode)

Out of the box, `GoogleSheets:Enabled = false`. The `SheetsService` calls `MockSync()`, which:
- Logs item IDs and titles to the console via `ILogger`
- Still writes a `SyncLog` row so the Sync page shows history
- Returns the item count so the API response is accurate

The app is **fully functional** without any Google credentials.

### Enabling Real Sync

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create a project → Enable **Google Sheets API** and **Google Drive API**
3. Create a **Service Account** → Download the JSON key
4. Place the JSON file at the path specified in `GoogleSheets:CredentialsPath` (default: `credentials/google-service-account.json`)
5. Share your target Google Spreadsheet with the service account's email address (give it **Editor** access)
6. Set `GoogleSheets:Enabled = true` and `GoogleSheets:SpreadsheetId = your-sheet-id` in `appsettings.json`

The sheet ID is the long string in the spreadsheet URL:  
`https://docs.google.com/spreadsheets/d/`**`THIS_IS_THE_ID`**`/edit`

---

## 14. AI Microservice Integration

The AI endpoint acts as a pass-through proxy to a Python FastAPI service.

**Expected local service:** `POST http://localhost:8001/ai/structure`

The payload forwarded:
```json
{ "rows": [ ...whatever array you send to /api/ai/structure... ] }
```

The response is returned to the caller unchanged. If the Python service is down, the endpoint returns `502 Bad Gateway` with a descriptive error message.

To start a minimal FastAPI service for testing:

```python
# ai_service.py
from fastapi import FastAPI
app = FastAPI()

@app.post("/ai/structure")
def structure(data: dict):
    return {"structured": data["rows"], "status": "ok"}
```

```bash
pip install fastapi uvicorn
uvicorn ai_service:app --port 8001
```

---

## 15. Response Format

All API endpoints return the same consistent wrapper:

```json
{
  "success": true,
  "data": { ... },
  "message": "Human-readable summary",
  "errors": []
}
```

| Field | Type | Description |
|-------|------|-------------|
| `success` | `bool` | `true` for 2xx responses, `false` for errors |
| `data` | `object` or `null` | The actual payload (DTO or primitive) |
| `message` | `string` | Short human-readable status message |
| `errors` | `string[]` | List of error/validation messages (empty on success) |

**Error example (400):**
```json
{
  "success": false,
  "data": null,
  "message": "Status must be one of: Pending, InBeta, Validated, InProduction, Failed",
  "errors": ["Status must be one of: Pending, InBeta, Validated, InProduction, Failed"]
}
```

---

## 16. Configuration Reference

### `appsettings.json` — Full Reference

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LMSDashboard;Trusted_Connection=True;"
  },
  "Jwt": {
    "Secret": "MINIMUM_32_CHARACTER_SECRET_KEY_HERE",
    "Issuer": "LMSDashboard",
    "Audience": "LMSDashboard"
  },
  "Auth": {
    "DevUsername": "admin",
    "DevPassword": "admin123"
  },
  "GoogleSheets": {
    "Enabled": false,
    "SpreadsheetId": "",
    "SheetName": "LMSContent",
    "CredentialsPath": "credentials/google-service-account.json"
  },
  "AiService": {
    "BaseUrl": "http://localhost:8001"
  },
  "Hangfire": {
    "DashboardPath": "/hangfire"
  }
}
```

### Environment Variable Overrides

ASP.NET Core maps `__` to `:` in environment variables:

| Environment Variable | Config Path |
|---------------------|------------|
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings:DefaultConnection` |
| `Jwt__Secret` | `Jwt:Secret` |
| `Jwt__Issuer` | `Jwt:Issuer` |
| `Jwt__Audience` | `Jwt:Audience` |
| `Auth__DevUsername` | `Auth:DevUsername` |
| `Auth__DevPassword` | `Auth:DevPassword` |
| `GoogleSheets__Enabled` | `GoogleSheets:Enabled` |
| `GoogleSheets__SpreadsheetId` | `GoogleSheets:SpreadsheetId` |
| `AiService__BaseUrl` | `AiService:BaseUrl` |

See [.env.example](.env.example) for a complete list.

---

## 17. Getting Started — Local Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server LocalDB — comes with Visual Studio, or install [SQL Server Express LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)
- Git

### Steps

```bash
# 1. Enter the project directory
cd LMSDashboard

# 2. Restore all NuGet packages
dotnet restore

# 3. Create the database and apply the migration
#    This creates LMSDashboard_Dev database and inserts 20 seed items
dotnet ef database update

# 4. Run the application
dotnet run

# OR for hot-reload during development:
dotnet watch run
```

**Available URLs:**

| URL | Description |
|-----|-------------|
| `http://localhost:5000` | Redirects to `/Dashboard` |
| `http://localhost:5000/Dashboard` | Main dashboard |
| `http://localhost:5000/swagger` | Swagger UI (API documentation + testing) |
| `http://localhost:5000/hangfire` | Hangfire job dashboard |

### Installing EF Tools (if needed)

```bash
dotnet tool install --global dotnet-ef
```

### Creating a new migration (after changing a Model)

```bash
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

---

## 18. Getting Started — Docker

Docker Compose starts both the ASP.NET app and a SQL Server 2022 instance.

```bash
docker-compose up --build
```

| Service | URL |
|---------|-----|
| App | `http://localhost:8080` |
| SQL Server | `localhost:1433` (SA password: `LMS_Dev_Password_123!`) |

To stop:
```bash
docker-compose down
```

To remove volumes (wipes DB):
```bash
docker-compose down -v
```

---

## 19. Seed Data

The initial migration inserts **20 ContentItems** so the dashboard is never empty on first run.

Seed items are distributed evenly across all enum values:

| Property | Distribution |
|----------|-------------|
| Type | Cycles through Quiz, Reading, Audio, PPT, Activity |
| Track | Cycles through Foundation, B1, Advanced, Applied, Crescent |
| Difficulty | Cycles through Easy, Medium, Hard |
| Status | Cycles through Pending, InBeta, Validated, InProduction, Failed |
| CreatedBy | Rotates between `alice@nxtwave.in`, `bob@nxtwave.in`, `carol@nxtwave.in` |
| CreatedAt | One item per day starting May 1, 2026 |

Items with status `InBeta` or higher have their `BetaUploadedAt` set. Items `Validated`+ have `ValidatedAt`. `InProduction` items have `ProdUploadedAt`.

Seed item IDs are deterministic GUIDs: `00000000-0000-0000-0000-000000000001` through `...000000000020`.

---

## 20. Troubleshooting

### `Invalid object name 'ContentItems'`
The database hasn't been migrated. Run:
```bash
dotnet ef database update
```

### Hangfire `Could not obtain a connection from the pool`
The Hangfire SQL storage initialization happens at startup and requires the DB to already exist. Make sure `dotnet ef database update` runs before `dotnet run`, or rely on the automatic `db.Database.Migrate()` call in `Program.cs`.

### `Jwt:Secret is required in configuration`
The app throws on startup if `Jwt:Secret` is missing. Set it in `appsettings.json` or as an environment variable. Minimum 32 characters for HS256.

### `Swashbuckle.AspNetCore` 403 on Swagger
Make sure you're running in `Development` environment. Set `ASPNETCORE_ENVIRONMENT=Development` in your launch profile or environment.

### Excel upload returns `No worksheet found`
EPPlus expects the file to have at least one worksheet. Make sure the `.xlsx` file is not empty or corrupt.

### `502 Bad Gateway` from `/api/ai/structure`
The Python FastAPI microservice at `localhost:8001` is not running. Start it or update `AiService:BaseUrl` in config.

### Google Sheets sync doing nothing visible
By default `GoogleSheets:Enabled = false`. Check console logs for `[MOCK Sheets Sync]` messages. To enable real sync, follow the steps in [Section 13](#13-google-sheets-integration).

---

## Quick Reference Card

```
# Get token
POST /api/auth/token  {"username":"admin","password":"admin123"}

# Upload Excel
POST /api/content/upload  (multipart, field: file)

# List items (page 2, InBeta only)
GET /api/content?status=InBeta&page=2&pageSize=10

# Update status
PUT /api/content/{id}/status  {"status":"Validated"}

# Validate item
POST /api/content/{id}/validate

# Monthly report
GET /api/reports/monthly

# Download Excel report
GET /api/reports/monthly/export

# Manual Sheets sync
POST /api/sheets/sync
```
