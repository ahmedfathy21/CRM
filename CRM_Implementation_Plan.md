# CRM Implementation Plan

This plan outlines the next steps to complete the CRM module using the Vertical Slice Architecture. The foundational data models, DTOs, database context, and authentication have been successfully scaffolded. The next phase will focus on building out the API slices and introducing advanced, enterprise-grade features inspired by leading CRMs like Salesforce and HubSpot.

## Proposed Core Features Execution

We will implement the following slices, adhering to CQRS with MediatR, FluentValidation, and role-based data scoping:

### 1. Contacts & Companies Management
- **Commands**: `CreateContact`, `UpdateContact`, `DeleteContact`, `CreateCompany`, `UpdateCompany`, `DeleteCompany`.
- **Queries**: `GetContactById`, `GetContactsList` (paginated & scoped), `GetCompanyById`, `GetCompaniesList`.
- **Logic**: Apply `AssignedToUserId` scoping for Sales Representatives.

### 2. Deals & Pipeline Management
- **Commands**: `CreateDeal`, `UpdateDeal`, `ChangeDealStage` (validating state transitions via `DealStageTransitionService`), `DeleteDeal`.
- **Queries**: `GetDealById`, `GetDealsList`, `GetPipelineView` (Kanban style).

### 3. Activities & Tasks
- **Commands**: `CreateActivity`, `UpdateActivity`, `CompleteActivity`, `DeleteActivity`.
- **Queries**: `GetActivitiesList` (filtered by Contact/Deal and User).

### 4. Dashboard & Analytics
- **Queries**: `GetCrmDashboard` (metrics like Win Rate, Revenue Forecast, Contacts by Status).
- **Caching**: Integrate `ICacheableQuery` for the dashboard to reduce database load.

---

## 🚀 Creative Feature Additions (Salesforce / HubSpot Inspired)

To make the CRM feel premium and competitive, here are the advanced features we will include in our execution plan:

### A. Lead Scoring & Grading System (Hybrid Matrix)
- **Concept (Finalized)**: We will implement a powerful hybrid approach that combines both Profile Fit and Time-Decay Engagement:
  - **Profile Grade (A-F)**: Based on demographic fit (e.g., industry, company size, job title).
  - **Engagement Score (1-100)**: Points awarded for activities, but they **decay over time** (e.g., active 6 months ago vs active yesterday).
  - **Result**: A lead is graded as an "A-85" (Great Fit, Highly Active recently) or "C-20" (Poor Fit, Activity has decayed). 
- **Value**: This provides the most accurate and dynamic representation of a lead's true temperature, ensuring Sales Reps always focus on the best, most active prospects.

### B. Audit Log & Rich Timeline
- **Concept**: Instead of only seeing manual "Activities", users will see a timeline on a Deal or Contact page. It automatically records system events (e.g., "Deal moved from Lead to Proposal by Ahmed", "Contact created").
- **Implementation**: **(Finalized)** We will use explicit **MediatR `INotification` (Domain Events)**. This provides rich business context and decoupled architecture. Handlers will publish events like `DealWonEvent`, and an audit handler will asynchronously write these to the timeline.

### C. Automated Workflows (Domain Events)
- **Concept**: Automate mundane tasks. For example, when a Deal stage changes to "Won", the system automatically creates a "Customer Onboarding" Activity and assigns it to the owner.
- **Implementation**: **(Finalized)** We will start with **MediatR Event Handlers** for hardcoded, reliable automations. 
  - *Dynamic Alternative*: If we want this to be user-configurable in the future, we can integrate Microsoft's `RulesEngine` library. This allows storing rules (like `if Deal.Stage == 'Won' then trigger Onboarding`) as JSON in the database, allowing Admins to change workflows without recompiling code.

### D. Document / Attachment Management
- **Concept**: Allow users to attach proposals, contracts, or NDAs to Deals and Contacts.
- **Implementation**: **(Finalized)** We will build an `Attachments` table to store metadata, and we will save the actual files **locally on the disk** for now. This provides a fast and simple implementation that can easily be migrated to AWS S3 or Azure Blob Storage later.

---

## Verification Plan
1. **Automated Testing**: Write unit tests for the MediatR Handlers and Data Scoping logic using an in-memory or SQLite database.
2. **Manual API Testing**: Run the application and test the endpoints via the Scalar/Swagger UI, ensuring that a `SalesRep` token cannot access unassigned contacts, while an `Admin` token can.
3. **Database Inspection**: Verify PostgreSQL tables and relationships are correctly populated.
