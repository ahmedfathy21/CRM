# CRM System Design

---

## 1. High-Level Design (HLD)

### Component Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLIENT (HTTP)                            │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                  ASP.NET Core Monolith                          │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              Global Exception Middleware                 │   │
│  └─────────────────────────────────────────────────────────┘   │
│  ┌──────────────────┐  ┌──────────────────────────────────┐    │
│  │  JWT Auth        │  │   Authorization Policies          │    │
│  │  (Shared)        │  │   CrmAccess / CrmManagerOnly      │    │
│  └──────────────────┘  └──────────────────────────────────┘    │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                  CRM Feature Slices                      │   │
│  │                                                          │   │
│  │  Contacts │ Companies │ Deals │ Activities │ Dashboard   │   │
│  │                                                          │   │
│  │  Each slice: Controller → Command/Query → Handler        │   │
│  │              Validator (FluentValidation) → DTOs         │   │
│  └───────────────────────┬─────────────────────────────────┘   │
│                          │                                      │
│  ┌───────────────────────▼─────────────────────────────────┐   │
│  │                   CrmDbContext                           │   │
│  │              (EF Core, Npgsql)                           │   │
│  └───────────────────────┬─────────────────────────────────┘   │
└──────────────────────────┼──────────────────────────────────────┘
                           │
                           ▼
              ┌────────────────────────┐
              │   CRM PostgreSQL DB    │
              │   Port 5434            │
              │   crm_db               │
              └────────────────────────┘
```

### Request Flow

```
HTTP Request
    │
    ├─► GlobalExceptionMiddleware (catches all unhandled exceptions)
    │
    ├─► JWT Authentication (validates Bearer token, extracts claims)
    │
    ├─► Authorization (CrmAccess / CrmManagerOnly policy check)
    │
    ├─► Controller (thin — only dispatches to MediatR)
    │
    ├─► FluentValidation (auto-validation via pipeline behavior)
    │
    ├─► MediatR Handler (business logic)
    │       ├─► CrmDbContext (EF Core queries)
    │       ├─► Data scoping (SalesRep filter applied here)
    │       └─► ApiResponse<T> / PagedResponse<T> returned
    │
    └─► HTTP Response (camelCase JSON via SystemTextJson)
```

### Data Scoping Flow

```
JWT Claim: role = "SalesRep", sub = "user-id-xyz"
                │
                ▼
        Handler reads claims
                │
        ┌───────┴────────┐
        │ SalesRep?      │ SalesManager / Admin?
        ▼                ▼
  Filter by          No filter —
  AssignedToUserId   return all records
  = currentUserId
```

---

## 2. System Design

### Authentication & Identity

- **Mechanism**: JWT Bearer tokens (shared with exam system)
- **Token claims used by CRM**: `sub` (userId), `role`
- **CRM does NOT own identity** — user management stays in the exam system's `AppDbContext`
- **Cross-DB user reference**: CRM stores `userId` as `string` — no EF FK to `AppUser`
- **Token validation**: Same `JwtBearerOptions` config, no changes needed

### Role Matrix

| Feature | SalesRep | SalesManager | Admin |
|---|---|---|---|
| Create contact/company/deal/activity | ✅ (own) | ✅ (any) | ✅ (any) |
| Read own records | ✅ | ✅ | ✅ |
| Read all records | ❌ | ✅ | ✅ |
| Update own records | ✅ | ✅ | ✅ |
| Update others' records | ❌ | ✅ | ✅ |
| Delete any record | ❌ | ✅ | ✅ |
| View dashboard (own metrics) | ✅ | ✅ | ✅ |
| View dashboard (team metrics) | ❌ | ✅ | ✅ |

### Data Scoping Strategy

Scoping is enforced **inside each handler** — not at the controller level. This keeps controllers thin and keeps authorization logic co-located with business logic per slice.

```csharp
// Pattern used in every list/get handler:
var isManager = currentUserRole is "Admin" or "SalesManager";

var query = _db.Contacts.AsQueryable();
if (!isManager)
    query = query.Where(c => c.AssignedToUserId == currentUserId);
```

### Pagination Strategy

All list endpoints use **offset-based pagination** via the existing `PagedResponse<T>`:

- Query params: `?page=1&pageSize=20`
- Default: page 1, size 20, max size 100
- Response includes: `totalCount`, `totalPages`, `hasNextPage`, `hasPreviousPage`

### Error Handling

Reuses the existing `GlobalExceptionMiddleware` and `AppException` hierarchy:

| Exception | HTTP Status | When Used |
|---|---|---|
| `NotFoundException` | 404 | Contact/Deal/Company not found |
| `ForbiddenException` | 403 | SalesRep accessing other's data |
| `ConflictException` | 409 | Duplicate email on contact |
| `ValidationException` | 422 | FluentValidation failures |
| `AppException` | 400 | Invalid pipeline stage transition |

### Caching

Read-heavy queries opt into caching via the existing `ICacheableQuery` pipeline behavior:

| Query | Cache Key | TTL |
|---|---|---|
| `GetCrmDashboard` | `crm:dashboard:{userId}` | 5 min |
| `ListContacts` | `crm:contacts:{userId}:{page}:{filters}` | 2 min |
| `GetPipeline` | `crm:pipeline:{userId}` | 2 min |

Cache is **invalidated on mutation** — handlers call `ICacheService.RemoveAsync()` after writes.

### Deal Pipeline State Machine

```
         ┌──────────────────────────────┐
         │              LEAD            │
         └────┬──────────────────┬──────┘
              │ qualify          │ lose
              ▼                  ▼
         QUALIFIED             LOST ──► LEAD (re-open)
              │ propose         │
              ▼                 │
          PROPOSAL ─────────────┤ lose
              │ negotiate        │
              ▼                 │
         NEGOTIATION ───────────┤ lose
              │ close-win        │
              ▼                 │
             WON ◄──────────────┘
```

Transition validation lives in a `DealStageTransitionService` (static helper called by `MoveDealStageHandler`). Returns `AppException("Invalid stage transition", ErrorCode.BadRequest)` on violations.

---

## 3. Database Design

### Connection

| Property | Value |
|---|---|
| Host | localhost |
| Port | 5434 |
| Database | crm_db |
| DbContext | `CrmDbContext` |
| Migration assembly | `ExaminationSystem` |
| Migration command | `dotnet ef migrations add InitCrm --context CrmDbContext` |

---

### Table: `contacts`

| Column | Type | Constraints |
|---|---|---|
| `id` | `uuid` | PK, default `gen_random_uuid()` |
| `first_name` | `varchar(100)` | NOT NULL |
| `last_name` | `varchar(100)` | NOT NULL |
| `email` | `varchar(200)` | NULLABLE, UNIQUE |
| `phone` | `varchar(30)` | NULLABLE |
| `job_title` | `varchar(150)` | NULLABLE |
| `status` | `varchar(20)` | NOT NULL, default `'Lead'` |
| `source` | `varchar(20)` | NOT NULL, default `'Other'` |
| `company_id` | `uuid` | FK → `companies.id` SET NULL |
| `assigned_to_user_id` | `varchar(450)` | NULLABLE (AppUser.Id) |
| `created_at` | `timestamptz` | NOT NULL, default `NOW()` |
| `updated_at` | `timestamptz` | NOT NULL, default `NOW()` |

**Indexes:**
- `IX_contacts_email` (unique, nullable)
- `IX_contacts_status`
- `IX_contacts_assigned_to_user_id`
- `IX_contacts_company_id`

---

### Table: `companies`

| Column | Type | Constraints |
|---|---|---|
| `id` | `uuid` | PK |
| `name` | `varchar(200)` | NOT NULL |
| `industry` | `varchar(100)` | NULLABLE |
| `website` | `varchar(300)` | NULLABLE |
| `phone` | `varchar(30)` | NULLABLE |
| `address` | `text` | NULLABLE |
| `employee_count` | `int` | NOT NULL, default `0` |
| `created_at` | `timestamptz` | NOT NULL, default `NOW()` |
| `updated_at` | `timestamptz` | NOT NULL, default `NOW()` |

**Indexes:**
- `IX_companies_name`

---

### Table: `deals`

| Column | Type | Constraints |
|---|---|---|
| `id` | `uuid` | PK |
| `title` | `varchar(200)` | NOT NULL |
| `value` | `numeric(18,2)` | NOT NULL, default `0` |
| `currency` | `varchar(10)` | NOT NULL, default `'USD'` |
| `stage` | `varchar(20)` | NOT NULL, default `'Lead'` |
| `probability` | `int` | NOT NULL, default `10`, CHECK (0–100) |
| `expected_close_date` | `date` | NULLABLE |
| `closed_at` | `timestamptz` | NULLABLE |
| `contact_id` | `uuid` | FK → `contacts.id` SET NULL |
| `company_id` | `uuid` | FK → `companies.id` SET NULL |
| `owner_user_id` | `varchar(450)` | NULLABLE (AppUser.Id) |
| `created_at` | `timestamptz` | NOT NULL, default `NOW()` |
| `updated_at` | `timestamptz` | NOT NULL, default `NOW()` |

**Indexes:**
- `IX_deals_stage`
- `IX_deals_owner_user_id`
- `IX_deals_contact_id`
- `IX_deals_company_id`
- `IX_deals_expected_close_date`

---

### Table: `activities`

| Column | Type | Constraints |
|---|---|---|
| `id` | `uuid` | PK |
| `type` | `varchar(20)` | NOT NULL |
| `subject` | `varchar(300)` | NOT NULL |
| `description` | `text` | NULLABLE |
| `scheduled_at` | `timestamptz` | NULLABLE |
| `completed_at` | `timestamptz` | NULLABLE |
| `is_completed` | `boolean` | NOT NULL, default `false` |
| `contact_id` | `uuid` | FK → `contacts.id` CASCADE |
| `deal_id` | `uuid` | FK → `deals.id` CASCADE |
| `created_by_user_id` | `varchar(450)` | NOT NULL |
| `created_at` | `timestamptz` | NOT NULL, default `NOW()` |
| `updated_at` | `timestamptz` | NOT NULL, default `NOW()` |

**Indexes:**
- `IX_activities_contact_id`
- `IX_activities_deal_id`
- `IX_activities_created_by_user_id`
- `IX_activities_is_completed`
- `IX_activities_scheduled_at`

---

### Table: `notes`

| Column | Type | Constraints |
|---|---|---|
| `id` | `uuid` | PK |
| `content` | `text` | NOT NULL |
| `contact_id` | `uuid` | FK → `contacts.id` CASCADE, NULLABLE |
| `deal_id` | `uuid` | FK → `deals.id` CASCADE, NULLABLE |
| `created_by_user_id` | `varchar(450)` | NOT NULL |
| `created_at` | `timestamptz` | NOT NULL, default `NOW()` |
| `updated_at` | `timestamptz` | NOT NULL, default `NOW()` |

**Constraint:** CHECK (`contact_id IS NOT NULL OR deal_id IS NOT NULL`)

---

### Table: `tags`

| Column | Type | Constraints |
|---|---|---|
| `id` | `uuid` | PK |
| `name` | `varchar(50)` | NOT NULL, UNIQUE |
| `color` | `varchar(7)` | NULLABLE (hex, e.g. `#3B82F6`) |
| `created_at` | `timestamptz` | NOT NULL, default `NOW()` |
| `updated_at` | `timestamptz` | NOT NULL, default `NOW()` |

---

### Table: `contact_tags` (join)

| Column | Type | Constraints |
|---|---|---|
| `contact_id` | `uuid` | PK (composite), FK → `contacts.id` CASCADE |
| `tag_id` | `uuid` | PK (composite), FK → `tags.id` CASCADE |

---

### Relationship Summary

```
companies ──< contacts >── contact_tags ──> tags
     │             │
     │             ├──< activities
     │             └──< notes
     │
     └──< deals >── activities
               └── notes
```

---

## 4. API Design

**Base path:** `/api/crm`
**Auth:** All endpoints require `Authorization: Bearer {jwt}` header
**Content-Type:** `application/json`
**Response envelope:** `ApiResponse<T>` / `PagedResponse<T>`

---

### 4.1 Contacts

#### `POST /api/crm/contacts`
**Auth:** `CrmAccess`

**Request:**
```json
{
  "firstName": "Ahmed",
  "lastName": "Fathy",
  "email": "ahmed@company.com",
  "phone": "+201001234567",
  "jobTitle": "CTO",
  "status": "Lead",
  "source": "Website",
  "companyId": "uuid | null",
  "tagIds": ["uuid"]
}
```

**Validation:**
- `firstName`: required, max 100
- `lastName`: required, max 100
- `email`: valid format, unique in DB
- `status`: must be valid `ContactStatus` enum value
- `source`: must be valid `ContactSource` enum value

**Response `201`:**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "firstName": "Ahmed",
    "lastName": "Fathy",
    "email": "ahmed@company.com",
    "phone": "+201001234567",
    "jobTitle": "CTO",
    "status": "Lead",
    "source": "Website",
    "companyId": null,
    "companyName": null,
    "assignedToUserId": "current-user-id",
    "tags": [],
    "createdAt": "2026-05-30T18:00:00Z"
  }
}
```

**Errors:** `409` duplicate email | `422` validation

---

#### `GET /api/crm/contacts/{id}`
**Auth:** `CrmAccess`

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "firstName": "Ahmed",
    "lastName": "Fathy",
    "email": "ahmed@company.com",
    "phone": "+201001234567",
    "jobTitle": "CTO",
    "status": "Lead",
    "source": "Website",
    "company": { "id": "uuid", "name": "Acme Corp" },
    "assignedToUserId": "user-id",
    "tags": [{ "id": "uuid", "name": "Hot Lead", "color": "#EF4444" }],
    "deals": [{ "id": "uuid", "title": "Acme Deal", "stage": "Proposal", "value": 15000 }],
    "recentActivities": [{ "id": "uuid", "type": "Call", "subject": "Intro call", "isCompleted": true }],
    "createdAt": "2026-05-30T18:00:00Z",
    "updatedAt": "2026-05-30T18:00:00Z"
  }
}
```

**Errors:** `404` not found | `403` SalesRep accessing unassigned contact

---

#### `GET /api/crm/contacts`
**Auth:** `CrmAccess`

**Query params:**

| Param | Type | Description |
|---|---|---|
| `page` | int | Default: 1 |
| `pageSize` | int | Default: 20, max: 100 |
| `search` | string | Searches firstName, lastName, email, phone |
| `status` | string | Filter by `ContactStatus` |
| `source` | string | Filter by `ContactSource` |
| `companyId` | uuid | Filter by company |
| `assignedToUserId` | string | Manager/Admin only |

**Response `200`:**
```json
{
  "success": true,
  "data": [...],
  "totalCount": 146,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

---

#### `PUT /api/crm/contacts/{id}`
**Auth:** `CrmAccess`

**Request:** Same shape as `POST`, all fields optional (patch semantics).

**Response `200`:** Updated `ContactResponse`

**Errors:** `404` | `403` | `409` duplicate email | `422`

---

#### `DELETE /api/crm/contacts/{id}`
**Auth:** `CrmManagerOnly`

**Response `200`:**
```json
{ "success": true, "message": "Contact deleted." }
```

**Errors:** `404` | `403`

---

### 4.2 Companies

#### `POST /api/crm/companies`
**Auth:** `CrmAccess`

**Request:**
```json
{
  "name": "Acme Corp",
  "industry": "Technology",
  "website": "https://acme.com",
  "phone": "+1-800-000-0000",
  "address": "123 Main St, NY",
  "employeeCount": 250
}
```

**Validation:** `name` required, max 200 | `website` valid URL if provided | `employeeCount` >= 0

**Response `201`:** `CompanyResponse`

---

#### `GET /api/crm/companies/{id}`
**Auth:** `CrmAccess`

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "name": "Acme Corp",
    "industry": "Technology",
    "website": "https://acme.com",
    "phone": "+1-800-000-0000",
    "address": "123 Main St, NY",
    "employeeCount": 250,
    "contactsCount": 12,
    "openDealsCount": 3,
    "openDealsValue": 95000,
    "createdAt": "2026-05-30T18:00:00Z"
  }
}
```

---

#### `GET /api/crm/companies`
**Auth:** `CrmAccess`

**Query params:** `page`, `pageSize`, `search` (name), `industry`

**Response `200`:** `PagedResponse<CompanyResponse>`

---

#### `PUT /api/crm/companies/{id}` | `DELETE /api/crm/companies/{id}`

Same pattern as Contacts. DELETE requires `CrmManagerOnly`.

---

### 4.3 Deals

#### `POST /api/crm/deals`
**Auth:** `CrmAccess`

**Request:**
```json
{
  "title": "Acme Enterprise License",
  "value": 50000,
  "currency": "USD",
  "expectedCloseDate": "2026-08-31",
  "contactId": "uuid | null",
  "companyId": "uuid | null"
}
```

**Validation:** `title` required | `value` >= 0 | `currency` 3-char ISO code | `expectedCloseDate` must be future date

**Response `201`:** `DealResponse` (stage defaults to `Lead`, probability to `10`)

---

#### `GET /api/crm/deals/{id}`
**Auth:** `CrmAccess`

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "title": "Acme Enterprise License",
    "value": 50000,
    "currency": "USD",
    "stage": "Proposal",
    "probability": 50,
    "expectedCloseDate": "2026-08-31",
    "closedAt": null,
    "contact": { "id": "uuid", "firstName": "Ahmed", "lastName": "Fathy" },
    "company": { "id": "uuid", "name": "Acme Corp" },
    "ownerUserId": "user-id",
    "activities": [],
    "notes": [],
    "createdAt": "2026-05-30T18:00:00Z"
  }
}
```

---

#### `GET /api/crm/deals`
**Auth:** `CrmAccess`

**Query params:**

| Param | Type | Description |
|---|---|---|
| `page` | int | Default: 1 |
| `pageSize` | int | Default: 20 |
| `stage` | string | Filter by `DealStage` |
| `ownerUserId` | string | Manager/Admin only |
| `minValue` | decimal | Minimum deal value |
| `maxValue` | decimal | Maximum deal value |
| `closingBefore` | date | Expected close date before |
| `closingAfter` | date | Expected close date after |

---

#### `GET /api/crm/deals/pipeline`
**Auth:** `CrmAccess`

Returns deals grouped by stage for a Kanban view.

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "columns": [
      {
        "stage": "Lead",
        "stageProbability": 10,
        "count": 8,
        "totalValue": 24000,
        "deals": [
          { "id": "uuid", "title": "Deal A", "value": 3000, "company": "Acme", "expectedCloseDate": "2026-09-01" }
        ]
      },
      { "stage": "Qualified", "stageProbability": 25, "count": 5, "totalValue": 67000, "deals": [] },
      { "stage": "Proposal", "stageProbability": 50, "count": 3, "totalValue": 45000, "deals": [] },
      { "stage": "Negotiation", "stageProbability": 75, "count": 2, "totalValue": 30000, "deals": [] },
      { "stage": "Won", "stageProbability": 100, "count": 10, "totalValue": 450000, "deals": [] },
      { "stage": "Lost", "stageProbability": 0, "count": 4, "totalValue": 12000, "deals": [] }
    ]
  }
}
```

---

#### `PATCH /api/crm/deals/{id}/stage`
**Auth:** `CrmAccess`

**Request:**
```json
{ "newStage": "Proposal" }
```

**Transition rules enforced:**

| From | Allowed `newStage` |
|---|---|
| Lead | Qualified, Lost |
| Qualified | Proposal, Lost |
| Proposal | Negotiation, Lost |
| Negotiation | Won, Lost |
| Won | *(none)* |
| Lost | Lead |

**Response `200`:** Updated `DealResponse` with new stage + auto-updated probability

**Errors:** `400` invalid transition | `404` | `403`

---

#### `PUT /api/crm/deals/{id}` | `DELETE /api/crm/deals/{id}`

Same pattern. DELETE requires `CrmManagerOnly`.

---

### 4.4 Activities

#### `POST /api/crm/activities`
**Auth:** `CrmAccess`

**Request:**
```json
{
  "type": "Call",
  "subject": "Discovery call with Ahmed",
  "description": "Discuss enterprise requirements",
  "scheduledAt": "2026-06-05T14:00:00Z",
  "contactId": "uuid | null",
  "dealId": "uuid | null"
}
```

**Validation:** `type` required enum | `subject` required max 300 | at least one of `contactId` or `dealId` must be provided | `scheduledAt` if provided must be valid datetime

**Response `201`:** `ActivityResponse`

---

#### `GET /api/crm/activities`
**Auth:** `CrmAccess`

**Query params:**

| Param | Type | Description |
|---|---|---|
| `page` | int | Default: 1 |
| `pageSize` | int | Default: 20 |
| `type` | string | Filter by `ActivityType` |
| `contactId` | uuid | Filter by contact |
| `dealId` | uuid | Filter by deal |
| `isCompleted` | bool | Filter by completion |
| `from` | datetime | Scheduled from date |
| `to` | datetime | Scheduled to date |

---

#### `PATCH /api/crm/activities/{id}/complete`
**Auth:** `CrmAccess`

No request body.

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "isCompleted": true,
    "completedAt": "2026-05-30T18:00:00Z"
  }
}
```

**Errors:** `409` already completed | `404`

---

#### `PUT /api/crm/activities/{id}` | `DELETE /api/crm/activities/{id}`

Standard pattern. Both require `CrmAccess` (SalesRep can delete own activities).

---

### 4.5 Dashboard

#### `GET /api/crm/dashboard`
**Auth:** `CrmAccess`

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "totalContacts": 146,
    "totalCompanies": 28,
    "totalDeals": 34,
    "contactsByStatus": {
      "lead": 45,
      "qualified": 12,
      "customer": 89,
      "churned": 0,
      "inactive": 0
    },
    "dealsPipeline": [
      { "stage": "Lead",        "count": 8,  "totalValue": 24000  },
      { "stage": "Qualified",   "count": 5,  "totalValue": 67000  },
      { "stage": "Proposal",    "count": 3,  "totalValue": 45000  },
      { "stage": "Negotiation", "count": 2,  "totalValue": 30000  },
      { "stage": "Won",         "count": 10, "totalValue": 450000 },
      { "stage": "Lost",        "count": 4,  "totalValue": 12000  }
    ],
    "revenueWon": 450000,
    "revenueForecast": 185000,
    "winRate": 0.71,
    "recentActivities": [
      { "id": "uuid", "type": "Call", "subject": "Follow-up", "contactName": "Ahmed Fathy", "completedAt": "..." }
    ],
    "upcomingActivities": [
      { "id": "uuid", "type": "Meeting", "subject": "Demo", "contactName": "Sara Ali", "scheduledAt": "..." }
    ]
  }
}
```

**Notes:**
- `revenueForecast` = `SUM(deal.value * deal.probability / 100)` for open deals
- `winRate` = `won / (won + lost)` (only closed deals counted)
- SalesRep: all metrics scoped to their assigned data
- SalesManager / Admin: team-wide aggregates
- Response is cached for 5 minutes per user

---

## 5. Summary

| Slice | Endpoints | Auth Policies Used |
|---|---|---|
| Contacts | 5 | CrmAccess (CRUD) + CrmManagerOnly (delete) |
| Companies | 5 | CrmAccess (CRUD) + CrmManagerOnly (delete) |
| Deals | 7 | CrmAccess (CRUD) + CrmManagerOnly (delete) |
| Activities | 5 | CrmAccess (all) |
| Dashboard | 1 | CrmAccess |
| **Total** | **23** | |
