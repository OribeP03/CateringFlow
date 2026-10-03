# CateringFlow Admin Panels â€” Phase Implementation Plan

> **Workspace**: `cateringflow` (ASP.NET Core MVC, net10.0, SQL Server + EF Core)
> **Scope**: Admin panels (the `/SuperAdmin/*` pages the sidebar links to) â€” stat cards, modal-based CRUD, and 10-per-page pagination.
> **Excluded**: CRM & Leads (no CRUD). Dashboard / Reports (no CRUD). Payment Proofs (already functional; stat cards only).

---

## Global Rules

1. **Phase gating** â€” never start the next phase until the current phase is fully done: build is green, the phase's paired **Unit-Test phase** passes, and the acceptance criteria are met.
2. **Two-phase rhythm** â€” every *Implementation* phase `N` has a paired *Unit-Test* phase `N`. Unit tests must be written/run and green before moving on.
3. **Stat-card rule** â€” KPI stat-card **values** increment left â†’ right (ascending numeric value), so the **largest value card is on the right**. That is a value sort, not a position sort. Ties keep their original relative order. Inside each card, the **value is positioned on the right side** of the card (the label, icon pill, and sub-note sit on the left).
4. **Pagination contract** â€” every list page shows exactly **10 rows per page** (page size = 10). Pagination is server-side (`Skip`/`Take`), default page = 1, filter/search values are preserved across pages.
5. **Modal contract** â€” CRUD lives in **Bootstrap 5 modals** on the list page. Forms submit as a **normal form post** (server-side validation, page reload on submit) to the existing entity controller POST actions, which redirect back to `/SuperAdmin/<Page>` via a `returnUrl` field. TempData success/error messages render in the existing alert banner.
6. **Stat-card data source** â€” once pagination exists, KPI cards are computed from the **entire dataset** (controller-provided totals), not from the current page's 10 rows.
7. **Running the tests** â€” `dotnet test tests/CateringFlow.Tests`. Building: `dotnet build cateringflow.csproj`.

---

## Phase Overview

| Phase | Implementation | Unit-Test |
|---|---|---|
| 1 | Stat-card incremental ordering (all admin panel pages) | A1 |
| 2 | Pagination (limit 10) + Bootstrap modal CRUD infrastructure | A2 |
| 3 | Stat-card value position â€” value on the right, label/icon/sub-note on the left (all pages) | A3 |
| 4 | Customers page â€” live data, modal CRUD, pagination | A4 |
| 5 | Staff page â€” live data, modal CRUD, pagination, dynamic stat cards | A5 |
| 6 | Events page â€” live data + customer-booking visibility (RBAC), modal CRUD, status update, pagination | A6 |
| 7 | Menu & Packages page â€” live data, modal CRUD, pagination | A7 |
| 8 | Inventory page â€” live data, modal CRUD, pagination | A8 |
| 9 | Suppliers page â€” live data, modal CRUD, pagination | A9 |
| 10 | Quotations page â€” live data, create/status/delete, pagination | A10 |
| 11 | Invoices page â€” live data, create/status/delete, pagination | A11 |
| 12 | Payments page â€” live data, record/delete, pagination | A12 |
| 13 | Notifications + Settings pages | A13 |
| 14 | Final integration pass â€” full build + full test suite | A14 |
| 15 | Live Dashboard â€” DB-driven KPI cards, upcoming events, payments, stock alerts, activity, live charts | A15 |
| 16 | Live Reports Hub â€” revenue digest, top packages, event-type breakdown, monthly trend, invoice/payment stats | A16 |
| 17 | Real Event Calendar â€” live month-grid view from the DB (supersedes the removed static calendar) | A17 |
| 18 | Invoice Print view â€” print-friendly invoice layout + print button from Invoice details | A18 |
| 19 | CSV Exports â€” download Customers / Payments / Inventory as CSV respecting list filters | A19 |
| 20 | Activity Log (audit trail) â€” table + automatic logging of key admin actions + visible log on Reports | A20 |
| 21 | Real-Life CRM â€” remove manual "Add Lead", live DB-driven pipeline (inquiry-sourced leads only) | A21 |
| 22 | Inquiry Pipeline (backend) â€” `InquiryModel` + `Client/Inquiry` â†’ CRM lead + notification + activity log + migration | A22 |
| 23 | Client Site â€” improved Services section + Contact / Request-a-Quote inquiry form | A23 |
| 24 | Admin Inquiry Inbox â€” `/SuperAdmin/Inquiries` (list, assign, status, delete) + RBAC + sidebar | A24 |
| 25 | Full CRUD Test Sweep â€” Create/Read/Update/Delete + pagination coverage for every controller | A25 |
| 26 | Final Integration â€” full build + full test suite + docs sync, then commit & push | A26 |

---

## Phase 1 â€” Stat-Card Incremental Ordering

**Goal**: All admin panel pages that show KPI/stat cards render them with **incrementing values, largest on the right**.

### Implementation (Phase 1)

1. New `Models/KpiCardModel.cs` â€” view model capturing a card (`Label`, `Value`, numeric `SortValue`, optional background/border/foreground colors, `FontSize`, `ValueFirst`, `LabelClass`, optional icon pill, optional sub-note). Lives in `cateringflow.Models`.
2. New `Services/StatCardOrdering.cs` â€” public static `OrderIncremental(IEnumerable<KpiCardModel>)` doing a **stable** ascending `OrderBy(SortValue)`.
3. New shared partial `Views/SuperAdmin/Components/_KpiCards.cshtml` â€” renders the ordered cards; enabled by `ViewData["KpiGridClass"]` / `ViewData["KpiGridStyle"]`; supports `ValueFirst` (value on top, label below) and header+icon styles, sub-notes, and `Html.Raw` of the value string.
4. `wwwroot/css/superadmin.css` â€” add `.kpi-cards-grid.three-cols` (and `six-cols` if needed).
5. Convert the stat-card blocks of these files to use `_KpiCards.cshtml` with ascending `SortValue` (largest on the right):
   - `Views/SuperAdmin/Staff.cshtml`, `Invoices.cshtml`, `Quotations.cshtml`, `Payments.cshtml`, `PaymentProofs.cshtml`
   - `Views/SuperAdmin/Components/_DashboardKpiCards.cshtml`, `_CrmKpiCards.cshtml`
   - Real CRUD-backed pages (still reachable, keep consistent): `Views/Staff/Index.cshtml`, `Views/Invoice/Index.cshtml`, `Views/Quotation/Index.cshtml`, `Views/Payment/Index.cshtml`, `Views/CRMLead/Index.cshtml`

### Unit-Test Phase 1 (A1)

1. Create `tests/CateringFlow.Tests/` â€” xUnit project (net10.0) referencing `cateringflow.csproj`.
2. `StatCardOrderingTests.cs`:
   - ascending sort puts the largest `SortValue` last (right-most);
   - stable ordering preserves input order for equal `SortValue`s;
   - empty input â†’ empty output; single card â†’ unchanged;
   - rejects null input.

**Acceptance criteria (Phase 1)** â€” `dotnet build cateringflow.csproj` green; `dotnet test tests/CateringFlow.Tests` green; every admin stat-card strip reads ascending value left â†’ right with the biggest value on the right.

---

## Phase 2 â€” Pagination (Limit 10) + Modal CRUD Infrastructure

**Goal**: A reusable, server-side 10-per-page pagination system and a shared Bootstrap-modal CRUD base, so the per-page phases only apply data + forms.

### Implementation (Phase 2)

1. New `Services/PageMeta` (or `Models/PagedList<T>`) helper: computes `Page`, `PageSize=10`, `TotalItems`, `TotalPages`; clamps `page` to valid range; exposes `HasPrevious`/`HasNext`.
2. New partial `Views/SuperAdmin/Components/_Pagination.cshtml` â€” renders prev/next + numbered page links (with windowing, e.g. up to 10 links) and preserves the existing `search`/filter query params.
3. Update every list controller `Index` action (Category: Customer, Staff, Event, Inventory, MenuPackage, Supplier, Quotation, Invoice, Payment, Notification, CRMLead, PaymentProofs) to: read `page`, apply `Skip((page-1)*10).Take(10)`, pass `ViewData["Page"]`, `ViewData["TotalPages"]`, `ViewData["TotalItems"]`, and compute stat-card totals from the full dataset via `ViewData`.
4. `returnUrl` support: each entity POST action (Create/Edit/Delete/UpdateStatus) accepts an optional `returnUrl`; on success redirects there (default stays `RedirectToAction("Index")`). Hidden `returnUrl` field rendered in modals.
5. Shared Bootstrap modal skeleton + `superadmin.js` helpers (`openAdminModal`/`closeAdminModal` enhanced or new `openBsModal`/`closeBsModal`) and a partial `_ModalForm.cshtml` if a shared wrapper is clean.

### Unit-Test Phase 2 (A2)

- `PageMetaTests`: page 1 â†’ first 10; page 2 â†’ rows 11â€“20 with 25 items; out-of-range page clamps; 10 < 10 total â†’ 1 page; page size always 10.
- `IndexPaginationTests` (EF InMemory): for Customer and Payment, `Index` returns â‰¤ 10 rows and correct `TotalItems`; filters still apply and preserve counts across pages.

**Acceptance criteria (Phase 2)** â€” all list pages paginate at 10; page links preserve filters; tests green; `returnUrl` redirects work from any entity POST.

---

## Phase 3 â€” Stat-Card Value Position (Value on the Right)

**Goal**: Inside every KPI/stat card the **value is positioned on the right side** of the card; the label, icon pill, and sub-note sit on the left. This applies to **every** stat strip â€” Dashboard â†’ Payment Proofs â€” without per-page changes, because every strip renders through the shared `_KpiCards.cshtml` partial.

### Implementation (Phase 3)

1. `Models/KpiCardModel.cs` â€” add `KpiValuePosition` enum (`Left`/`Right`) and a `ValuePosition` property **defaulting to `Right`**, plus a `GetValuePositionClass()` helper returning `kpi-card--value-right` / `kpi-card--value-left`.
2. `Views/SuperAdmin/Components/_KpiCards.cshtml` â€” unified card structure: a left column (`.kpi-card-body`: icon pill, label, optional sub-note) and a right column (`.kpi-card-value`: the big value). The `ValueFirst` branch no longer changes the column layout; the value always lands on the right.
3. `wwwroot/css/superadmin.css` â€” `.kpi-card` becomes a flex row (`space-between`); add `.kpi-card-body` (left column) and `.kpi-card-value` (right-aligned, vertically centered). Grid variants (`.three-cols` / `.four-cols` / `.six-cols`) and pill/change styles unchanged.
4. No per-page form/view changes â€” Dashboard, CRM, Staff, Invoices, Quotations, Payments, and Payment Proofs strips all update via the shared partial.

### Unit-Test Phase 3 (A3)

- `KpiCardLayoutTests.cs`:
  - a `KpiCardModel` defaults `ValuePosition` to `Right`;
  - `GetValuePositionClass()` maps `Right â†’ kpi-card--value-right` and `Left â†’ kpi-card--value-left`;
  - `StatCardOrdering.OrderIncremental` still yields ascending `SortValue` (largest on the right) once `ValuePosition` is set; ties stay stable.

**Acceptance criteria (Phase 3)** â€” build green; A3 tests green; every stat-card strip from the Dashboard through Payment Proofs shows the big value on the **right** side of its card.

---

## Phase 4 â€” Customers

### Implementation (Phase 4)

- `SuperAdminController.Customers` passes live `CustomerModel` page + totals.
- `Views/SuperAdmin/Customers.cshtml` becomes model-driven (replaces `_CustomersTable.cshtml` static rows or makes it model-driven).
- Add Bootstrap modals: **Add Customer**, **Edit Customer**, **View Customer**; forms post to `CustomerController` Create/Edit with `returnUrl=/SuperAdmin/Customers`.
- Delete stays an inline confirm form (or modal confirm) posting to `CustomerController.Delete` with `returnUrl`.
- Pagination partial rendered (limit 10). Success/error alert shown via TempData.

### Unit-Test Phase 4 (A4)

- `CustomerControllerTests` (EF InMemory): Create inserts + redirects; Edit persists changes; Delete removes; Index pages at 10; validation failure returns view/model.

---

## Phase 5 â€” Staff

### Implementation (Phase 5)

- `SuperAdminController.Staff` passes live staff + KPI totals (Available / Busy / On Leave / Full-time) from the full dataset.
- `Views/SuperAdmin/Staff.cshtml` model-driven table + Bootstrap modals for Add/Edit/View posting to `StaffController` with `returnUrl`.
- Stat cards become the dynamic `_KpiCards` strip in incremental order (largest on the right). Pagination at 10.

### Unit-Test Phase 5 (A5)

- `StaffControllerTests` (EF InMemory): Create/Edit/Delete, pagination, KPI count calculation correctness, assignment count display.

---

## Phase 6 â€” Events

**Goal**: The `/SuperAdmin/Events` page is **live** â€” and, critically, **every booking a customer submits through the client site (`ClientController.Book`) is immediately visible to Super Admin and to the roles who manage the Events page** (Sales CRM `Submit / Manage`, Event Coordinator `Submit / Manage / View`). Bookings are `EventModel` rows in the DB, so the live list renders them at once; RBAC (`RbacService.GetAllowedPages`) already controls menu/page visibility per role.

### Implementation (Phase 6)

- `SuperAdminController.Events` passes live paged `EventModel` rows (+ `Customers` and `Packages` for modal dropdowns) with search / type / status filters.
- `Views/SuperAdmin/Events.cshtml` becomes model-driven: total count, server-side filter form, DB rows (customer, type, date, pax, amount, status pill), actions (View / Edit / Delete â€” Bootstrap modal or inline confirm), `_Pagination` at 10, TempData alerts.
- STC Modals post to `EventController` Create / Edit / Delete / UpdateStatus with `returnUrl=/SuperAdmin/Events`. Any booking created via the client site already persists as an `EventModel` (Status `Upcoming`) and shows automatically.
- No stale/static event rows or static calendar markup remain on the page.

### Unit-Test Phase 6 (A6)

- `EventControllerTests`: Create recomputes `TotalAmount` from package (pax Ã— price), Edit recomputes, UpdateStatus, Delete, pagination.
- `CustomerBookingVisibilityTests`:
  - `ClientController.Book` (authenticated customer POST) inserts an `EventModel` (Status `Upcoming`) **plus** an invoice + "New Client Booking" notification;
  - the same booking then appears in `SuperAdminController.Events` (total count includes it);
  - RBAC: `GetAllowedPages` contains "Events" for Super Admin, Sales CRM, and Event Coordinator (the users who manage that page).

**Acceptance criteria (Phase 6)** â€” build green; A6 green; a booking submitted from the client site shows up on `/SuperAdmin/Events` for Super Admin and Events-managing roles; no static prototype rows remain.

---

## Phase 7 â€” Menu & Packages

### Implementation (Phase 7)

- `SuperAdminController.MenuPackages` live; Add/Edit/View modals posting to `MenuPackageController`; pagination 10; existing package pricing cards kept/updated.

### Unit-Test Phase 7 (A7)

- `MenuPackageControllerTests`: Create/Edit/Delete, pagination.

---

## Phase 8 â€” Inventory

### Implementation (Phase 8)

- `SuperAdminController.Inventory` live; Add/Edit/View modals â†’ `InventoryController` (auto item-code, stock-status recompute); pagination 10; warning banner for low stock from full dataset.

### Unit-Test Phase 8 (A8)

- `InventoryControllerTests`: Create auto stock-status (>= reorder â†’ Adequate, < reorder â†’ Low Stock, 0 â†’ Out of Stock), Edit recompute, Delete, pagination, low-stock filter, SuperAdmin Inventory page live at 10.

---

## Phase 9 â€” Suppliers

### Implementation (Phase 9)

- `SuperAdminController.Suppliers` live; Add/Edit/View modals â†’ `SupplierController`; pagination 10.

### Unit-Test Phase 9 (A9)

- `SupplierControllerTests`: Create/Edit/Delete, pagination, category filter, SuperAdmin Suppliers page live at 10.

---

## Phase 10 â€” Quotations

### Implementation (Phase 10)

- `SuperAdminController.Quotations` live; Bootstrap modals: Create Quotation (auto QTN number, pax Ã— package price), View, status transitions (Sent / Approved / Rejected â†’ `UpdateStatus`, auto-invoice on Approve), Delete; pagination 10; stat cards incremental.

### Unit-Test Phase 10 (A10)

- `QuotationControllerTests`: Create number/total, UpdateStatus â†’ Approved auto-creates Invoice, Delete, pagination.

---

## Phase 11 â€” Invoices

### Implementation (Phase 11)

- `SuperAdminController.Invoices` live; modal Create (auto INV number, from approved quotation), View, UpdateStatus (mark Paid â†’ AmountPaid = Total), Delete; pagination 10; stat cards incremental (Total / Paid / Partial / Unpaid / Overdue).

### Unit-Test Phase 11 (A11)

- `InvoiceControllerTests`: Create numbering, UpdateStatus Paid settles amount, Delete, pagination.

---

## Phase 12 â€” Payments

### Implementation (Phase 12)

- `SuperAdminController.Payments` live; modal Record Payment (updates invoice AmountPaid/status) â†’ `PaymentController.Create`; Delete (reverses invoice); pagination 10; stat cards incremental (Total Collected / This Month / Most Used Method).
- **Settlement chain fix (approve â†’ Payment + Invoice)**: `ApprovePaymentProof` now **always** records the official `Payment` and **guarantees an invoice exists** for the event before settling:
  - Reuses the booking invoice when one exists (online bookings already get one via `ClientController.Book` â€” `CustomerId` + `EventId` match);
  - otherwise **auto-creates** the event's invoice (unique `INV-YYYY-NNN`, event total, due = event date) and links it to the customer + event;
  - applies the approved amount to `AmountPaid`, recomputes invoice status (Partial/Paid), caps overpayments at the remaining balance, and records the `PaymentModel` row on every approval â€” this removes the "proof approved but nothing shows on Payments / Invoices" disconnect.
- `PaymentController.Create` (modal record) + Delete continue to sync invoice balance/status.

### Unit-Test Phase 12 (A12)

- `PaymentControllerTests`: Create adds payment + invoice amount adjustment + status recompute; Delete reverses amount; pagination; summary stats.
- `PaymentApprovalSettlementTests` (A12): 
  - Approve with existing booking invoice â†’ `Payment` recorded against that invoice + `AmountPaid`/status updated;
  - Approve with **no invoice** â†’ invoice auto-created (linked to customer + event) then `Payment` recorded;
  - Overpayment is capped at the remaining balance;
  - Reject only flips proof status (no payment, no invoice).

---

## Phase 13 â€” Notifications + Settings

### Implementation (Phase 13)

- Notifications: live list with pagination 10, Mark as Read / Mark All Read / Delete (no create â€” auto-generated). Stat-free page.
- Settings: single-row company-profile edit (Bootstrap modal or inline form) â†’ `SettingsController` upsert.

### Unit-Test Phase 13 (A13)

- `NotificationControllerTests`: MarkAsRead, MarkAllRead, Delete, pagination.
- `SettingsControllerTests`: upsert persists, defaults when no row exists.

---

## Phase 14 â€” Final Integration

### Implementation (Phase 14)

- Full pass over every admin page: stat cards incremental with value on the right, CRUD modals work end-to-end, pagination 10 on all lists, `returnUrl` redirects correct, no leftover static prototype tables.
- Remove/retire now-unused static prototype markup where superseded.

### Unit-Test Phase 14 (A14)

- Run the **entire** `tests/CateringFlow.Tests` suite; `dotnet build` from clean; manual smoke checklist over all sidebar pages.

**Acceptance criteria (Phase 14)** â€” all unit tests green, build green, every admin panel page satisfies the stat-card rule, modal CRUD, and 10-per-page pagination.

---

## Phase 15 â€” Live Dashboard

**Goal**: `/SuperAdmin/Dashboard` stops showing hardcoded numbers (`78 bookings`, `â‚±4.52M`, fake widget rows) and renders **real DB data**.

### Implementation (Phase 15)

- New `Models/DashboardViewModel.cs` â€” live `KpiCards` (from DB), upcoming events (top 5), recent payments (top 5), stock alerts (top 5 below reorder), recent activity (top 5 bookings), plus monthly-revenue and events-by-type chart data.
- `SuperAdminController.Dashboard` computes everything from the DB: Pending Quotations (`Status != "Approved"`), Total Bookings + new this month, Active Customers + new this month, Outstanding (sum of invoice balances), Total Revenue (sum of `Payments.Amount`), etc. KPI cards are ran through `StatCardOrdering` (ascending value, largest right â€” the global stat-card rule) with value on the right.
- `Views/SuperAdmin/Dashboard.cshtml` renders the view model; `_DashboardKpiCards` / `_DashboardLists` / `_DashboardCharts` become parameterized partials fed live data (labels/data serialized for Chart.js).
- Sidebar "Dashboard" nav unchanged (still RBAC-gated).

### Unit-Test Phase 15 (A15)

- `DashboardTests`: KPI card values match seeded DB totals (bookings, active customers, outstanding, revenue), pending-quotation count excludes approved, upcoming-events widget is the next 5 by date, stock-alert widget only lists below-reorder items, recent-payments widget shows newest first, chart series contain the seeded months/types, stat-card ordering rule holds (cards ascending, largest right).

**Status â€” BUILT & verified.** `DashboardLive` action + `DashboardViewModel`; partials (KPI/lists/charts) are DB-driven; Chart.js reads `data-*` attributes. `DashboardTests` (7 tests) green.

---

## Phase 16 â€” Live Reports Hub

**Goal**: `/SuperAdmin/Reports` is a real reporting hub (scope churn), fed from the DB instead of alert() links and static charts.

### Implementation (Phase 16)

- New `Models/ReportsViewModel.cs` â€” totals (revenue this month vs last, total collected, bookings count, avg booking value), 6-month revenue trend, top 5 packages by revenue, events-by-type counts + revenue, invoice status buckets (Paid/Partial/Unpaid/Overdue), payment-method breakdown.
- `SuperAdminController.Reports` computes the aggregates; `Views/SuperAdmin/Reports.cshtml` renders live summary cards + charts (labels/data from the model).
- Where a "report" is list-based (Stock Report â†’ low-stock items, Payment Report â†’ payments), the report card links to the **live filtered page** (e.g. `/SuperAdmin/Inventory`) instead of a fake alert.

### Unit-Test Phase 16 (A16)

- `ReportsTests`: revenue this month/last month match payments, top packages ordered by revenue, events-by-type buckets correct, invoice status buckets correct, payment-method breakdown correct, empty DB renders zeros without throwing.

**Status â€” BUILT & verified.** `Reports` action + `ReportsViewModel`; Reports.cshtml renders live month-over-month strip, KPI cards, and charts; `_ReportsHub` is model-driven (top packages, invoice status, payment methods, event status). `ReportsTests` (5 tests) green.

---

## Phase 17 â€” Real Event Calendar

**Goal**: a real month-grid calendar of events from the DB (the static calendar that was removed earlier is not reintroduced â€” this is live).

### Implementation (Phase 17)

- New `Models/CalendarViewModel.cs` â€” selected year/month, first day offset, list of `CalendarDay` cells (date, events on that day).
- `SuperAdminController.Calendar(int? year, int? month)` queries `Events` for the month range (incl. days from adjacent months in the grid), groups by `EventDate.Date`.
- `Views/SuperAdmin/Calendar.cshtml` â€” server-rendered month table (no JS lib): weekday header, leading blanks, one cell per day with an event pill per booking (name + pax + amount), today highlighted, Prev/Next month links.
- `_Pagination`/filters not needed (month-scoped); "Calendar" nav item added to the sidebar right under Dashboard (RBAC-gated like Events).

### Unit-Test Phase 17 (A17)

- `CalendarTests`: month grid has correct number of weekday cells, events land on the right day cell, events from adjacent-month overflow days appear in their overflow cell, prev/next month parameters shift the viewed month, empty month renders blanks.

**Status â€” BUILT & verified.** `Calendar` action + `CalendarViewModel`; `Views/SuperAdmin/Calendar.cshtml` server-rendered 6Ã—7 grid with nav; "Calendar" added to RBAC + sidebar. `CalendarTests` (5 tests) green.

---

## Phase 18 â€” Invoice Print View

**Goal**: clean print-friendly invoice output.

### Implementation (Phase 18)

- `InvoiceController.Print(int? id)` GET â€” loads invoice with Customer / Quotation / Event / Payments; 404 if missing.
- New `Views/Invoice/Print.cshtml` â€” sheet-style layout plus `@media print` CSS (header, bill-to, event/package line items, totals, amount paid, balance, payments table, footer), a toolbar with Back + Print (`window.print()`) that is hidden when printing.
- "Print" button added to `Views/Invoice/Details.cshtml`, linking to `Invoice/Print/{id}`.

### Unit-Test Phase 18 (A18)

- `InvoicePrintTests`: Print returns the view for an existing invoice, 404 for missing/`null` id, view lists the invoice payments, details page includes a Print link to `/Invoice/Print/{id}`.

**Status â€” BUILT & verified.** `InvoiceController.Print` + standalone `Views/Invoice/Print.cshtml` with `@media print` toolbar-hiding; Print button added to Details header. `InvoicePrintTests` (5 tests) green.

---

## Phase 19 â€” CSV Exports

**Goal**: one-click CSV download for the three main lists, honoring the current filters.

### Implementation (Phase 19)

- `SuperAdminController.ExportCustomers(string? search, string? type, string? status)`, `ExportPayments` and `ExportInventory(string? category, string? stockStatus, â€¦)` â€” same filters as the pages, return `File(â€¦, "text/csv", "customers.csv")` (UTF-8 with BOM so Excel opens â‚± correctly), headers + rows built with CSV escaping.
- Export buttons on Customers / Payments / Inventory page headers (they are plain GET links â€” safe because filters come via query string; no mutation).

### Unit-Test Phase 19 (A19)

- `CsvExportTests`: exported CSV contains the header row, one row per matching record, honors the filter (e.g. only produce category), quoting escapes commas/quotes in values, filename + content-type correct (customers.csv / text/csv).

**Status â€” BUILT & verified.** `ExportCustomers` / `ExportPayments` / `ExportInventory` (filters shared with pages, UTF-8 BOM CSV with escaping) + Export CSV buttons on Customers, Inventory, and Payments headers. `CsvExportTests` (5 tests) green.

---

## Phase 20 â€” Activity Log (Audit Trail)

**Goal**: lightweight internal audit log of key admin actions, visible in-app.

### Implementation (Phase 20)

- New `Models/ActivityLogModel.cs` (Id, Action, EntityType, EntityId, Detail, UserName, Timestamp) + `DbSet<ActivityLogModel> ActivityLogs` + EF migration.
- `Services/ActivityLogger.cs` â€” `LogAsync(ctx, action, entityType, entityId, detail, userName)` used by Customer / Event / Inventory / Supplier / Payment / Invoice / Proof-approval controllers (create/edit/delete/approve/reject).
- New `Views/SuperAdmin/ActivityLog.cshtml` + `SuperAdminController.ActivityLog` (10 per page, filter by user/action) â€” internal page reachable from the Reports hub ("Activity Log" card).
- SeedData seeds a few historical log rows so the page isn't empty on first run.

### Unit-Test Phase 20 (A20)

- `ActivityLoggerTests`: logger inserts a row with all fields; API: customer create + event delete + inventory edit + approval each produce a log entry; `SuperAdminController.ActivityLog` pages at 10 in newest-first order; migration adds the table (schema smoke test via EF).

**Status â€” BUILT & verified.** `ActivityLogModel` + `DbSet` + EF migration `AddActivityLog` (created via `dotnet ef`); `Services/ActivityLogger.cs` hooked into Customer/Event/Inventory/Supplier/Payment/Invoice creates and proof approval; `Views/SuperAdmin/ActivityLog.cshtml` (10/page, filters) + sidebar/RBAC + Reports-hub link; SeedData seeds historical rows (independent guard). `ActivityLoggerTests` (5 tests) green. Note: `dotnet ef` must run with `-p:OutputPath=<temp>` while the app is running (bin lock).
---

## Phase 21 — Real-Life CRM (No Manual Lead Creation, Live Pipeline)

**Goal**: mirror how a CRM actually works. Leads are **never typed in by an admin** — they arrive from the public
website's inquiry/booking forms (Phase 22/23 feed them). Admins can only **qualify, assign, advance, win or lose**
them. So the "+ Add Lead" button and the fake alert-only modal are removed, and `/SuperAdmin/CRM` renders the real
pipeline from `CrmLeads`.

### Implementation (Phase 21)

1. `Views/SuperAdmin/CRM.cshtml` — **delete the "+ Add Lead" button**; header becomes "inquiry-sourced" wording
   (`X open inquiries · ?Y pipeline value · leads are created automatically from website inquiries`) and a link to the
   Inquiries inbox.
2. Delete `Views/SuperAdmin/Components/_AddLeadModal.cshtml` (static form that only called `alert(...)`).
3. `Controllers/CRMLeadController.cs` — **remove `Create` (GET + POST)** entirely; a lead can no longer be created by
   hand. Keep `Index`, `Details`, `Edit`, `UpdateStage`, `Delete`. `Index` gains the stage/assignee filter set.
4. Delete `Views/CRMLead/Create.cshtml` and the "+ Add Lead" link in `Views/CRMLead/Index.cshtml`.
5. `Controllers/SuperAdminController.cs` — `CRM()` becomes async and DB-driven: live KPI cards, the ordered stage list,
   and a paged lead list (10/page) with the newest inquiries first.
6. `Views/SuperAdmin/Components/_CrmKpiCards.cshtml` — live cards (Open Leads, Pipeline Value, Won This Quarter,
   Conversion Rate, New Inquiries) run through `StatCardOrdering`.
7. `Views/SuperAdmin/Components/_CrmKanbanBoard.cshtml` — real board grouped by stage (New ? Contacted ? Qualified ?
   Proposal ? Negotiation ? Won ? Lost), each column counting/value computed from the DB, card = lead name, event
   type/company, amount, assignee, "days waiting" age.
8. New `Models/CrmBoardViewModel.cs` — `CrmStageColumn` (stage, label, css class, leads, total value) +
   `CrmBoardViewModel` (columns, cards, paged leads, totals).

### Test Phase A21 (`CrmRealLifeTests.cs`) — mirror of Phase 21

- `CRMLeadController` exposes **no public `Create`** action (reflection over `GetMethods`).
- No CRM view renders an "Add Lead" control (`Views/SuperAdmin/CRM.cshtml`, `Views/CRMLead/Index.cshtml`,
  `_CrmKanbanBoard.cshtml`, `_CrmKpiCards.cshtml` contain no `Add Lead` / `asp-action="Create"`).
- `SuperAdminController.CRM()` renders one column per known stage and puts every seeded lead in exactly one column.
- Column counts and per-column totals equal the DB values; pipeline value KPI equals the sum of open leads.
- `CRMLeadController.Index` pages at 10 and honors the stage filter.
- `UpdateStage` advances a lead + stamps `LastContact`; `Won` on an unlinked lead auto-creates the customer.
- `Delete` removes the lead; `Edit` persists changes.

**Gate**: build green + A21 green before Phase 22.

**Status - BUILT & verified.** `CrmBoardViewModel` + DB-driven `SuperAdminController.CRM` (live board, KPI cards,
filterable paged table at 10/page); `CRMLeadController.Create` (GET + POST) and both create views deleted; no "Add
Lead" control left in any CRM view. `CrmRealLifeTests` (31 tests) green.

---

## Phase 22 — Inquiry Pipeline (Backend Delivery of Client Inquiries)

**Goal**: a public inquiry submitted from the client site is **persisted as an Inquiry**, **converted into a CRM lead**
(stage `New`), **notified** to the staff who handle inquiries, and **logged** in the audit trail.

### Implementation (Phase 22)

1. New `Models/InquiryModel.cs` — `Inquiry`: `Id`, `FullName`, `Email`, `Phone`, `EventType`, `EventDate?`, `PaxCount`,
   `Venue`, `PackageId?`, `Message`, `Source` (`Website`), `Status` (`New`), `AssignedTo?`, `CrmLeadId?`,
   `CreatedAt`, `UpdatedAt?`. Validation attributes + `[NotMapped] Reference` (`INQ-YYYY-NNN`).
2. `Data/CateringFlowDbContext.cs` — `DbSet<InquiryModel> Inquiries`, FK to `MenuPackage` (SetNull) and to
   `CrmLead`/`Customer` (Restrict/SetNull as appropriate) + EF migration `20261003065551_AddActivityLogAndInquiries` (combined with the pending ActivityLog migration so a single `dotnet ef database update` brings both up).
3. `Controllers/ClientController.cs` — `[HttpPost] Inquiry(InquiryRequest)` (anonymous-allowed, antiforgery):
   validate ? persist `InquiryModel` ? create/refresh `CRMLeadModel` (`Stage = "New"`, `Source`-noted) linked to the
   inquiry ? `NotificationModel` for `Sales / CRM Staff` ? `ActivityLogger` entry ? `Ok({ success, inquiryId, reference })`;
   `BadRequest({ error })` on invalid input.
4. `Data/SeedData.cs` — seed a few inquiries (mix of `New` / `Contacted` / `Quoted`) so the inbox is not empty.

### Test Phase A22 (`ClientInquiryTests.cs`) — mirror of Phase 22

- A valid inquiry creates **one Inquiry row** with all fields persisted, `Status = "New"`, `Source = "Website"`.
- The same submission creates a **CRM lead at stage `New`** whose `CrmLeadId` links back to the inquiry.
- A **notification** is created for `Sales / CRM Staff` and an **activity log** row is written.
- Validation: missing name / invalid email / empty message ? `BadRequest` and **no** rows written.
- Blank email/phone are stored as `null`; `PaxCount <= 0` is rejected; a package id that does not exist still stores
  the inquiry (no FK crash) with `PackageId = null`.
- `INQ-` reference is unique per inquiry.

**Gate**: build green + A22 green before Phase 23.

**Status - BUILT & verified.** `InquiryModel` + `InquiryStatuses` / `InquirySources` / `InquiryAssignees` /
`CRMLeadModelStages`; `ClientController.Inquiry` persists the inquiry, creates the CRM lead (stage `New`, source noted
in the lead notes), notifies `Sales / CRM Staff` and writes an activity row; `SeedData.SeedInquiries`; migration
`20261003065551_AddActivityLogAndInquiries` applied to the local DB. `ClientInquiryTests` (24 tests) green.

---

## Phase 23 — Client Site: Services + Contact / Inquiry

**Goal**: the public site gains a real **Services** section (what we actually cater) and a **Contact / Request a Quote**
section whose form delivers straight into the CRM (Phase 22 endpoint).

### Implementation (Phase 23)

1. `Views/Client/Components/_BuiltForPros.cshtml` ? replaced by a proper **Services** section
   (`Views/Client/Components/_Services.cshtml`): wedding, corporate, birthday/anniversary, gala/municipal,
   live station, and full-service kitchen management — each card with what is included and a "Request this service"
   link that pre-fills the inquiry form.
2. New `Views/Client/Components/_ContactInquiry.cshtml` — contact details (address, phone, email, business hours,
   response promise) + the inquiry form (name, email, phone, event type, event date, pax, venue, package, message)
   posting to `/Client/Inquiry`, with inline validation and a success panel that shows the returned reference.
3. `Views/Client/Index.cshtml` — render `_Services` (replacing `_BuiltForPros`) and `_ContactInquiry` before the CTA;
   keep section anchors (`#services`, `#contact`) that the navbar/footer already link to.
4. `wwwroot/js/client.js` — `initInquiryForm()`: submit via `fetch` with the antiforgery token, disable the submit
   button while sending, show success/error messaging, reset on success.
5. `wwwroot/css/client.css` — services grid + contact section styles (dark espresso / gold, matching the existing
   tokens), responsive rules, and inquiry form field styling.
6. Navbar gets a "Contact" link (`#contact`) and the CTA banner gets a "Request a Quote" secondary action.

### Test Phase A23 (`ClientServicesContactTests.cs`) — mirror of Phase 23

- `_Services.cshtml` and `_ContactInquiry.cshtml` exist and are rendered by `Views/Client/Index.cshtml`.
- The contact form posts to `/Client/Inquiry` and contains the required inputs (`fullName`, `email`, `phone`,
  `eventType`, `paxCount`, `message`).
- Every service card links to the inquiry form (`#contact` / `openInquiryForm`).
- The services section no longer contains the old ERP-only feature copy.
- `client.js` wires the inquiry form to `/Client/Inquiry` and `client.css` defines the new section classes.
- The landing page still renders `_BuiltForPros` consumers correctly (no dangling partial references anywhere).

**Gate**: build green + A23 green before Phase 24.

**Status - BUILT & verified.** `_Services.cshtml` (six real catering service lines, each with inclusions + "Request
this service") replaces `_BuiltForPros.cshtml`; `_ContactInquiry.cshtml` renders company contact details + the inquiry
form (both pages tag their source: `Website` / `Packages Page`); navbar "Services"/"Contact" links, CTA "Request a
Quote", footer service links; `client.js` `initInquiryForm()` posts to `/Client/Inquiry` with antiforgery and shows the
returned reference; `client.css` services + inquiry styles with responsive rules. `ClientServicesInquiryViewTests`
(44 tests) green.

---

## Phase 24 — Admin Inquiry Inbox (`/SuperAdmin/Inquiries`)

**Goal**: the admin panel account that owns inquiries (Sales CRM / Super Admin) works a real inbox: read the
inquiry, assign it, advance its status, and delete junk inquiries. **No "add inquiry" button** — inquiries arrive
from the site.

### Implementation (Phase 24)

1. `Controllers/SuperAdminController.Inquiries(string? search, string? status, int? page)` — paged (10/page) live
   inquiry list + KPI cards (New / Contacted / Quoted / Won / Lost / Total) from the full dataset.
2. New `Controllers/InquiryController.cs` — POST `UpdateStatus(id, status, returnUrl)`, `Assign(id, assignee, returnUrl)`,
   `Delete(id, returnUrl)`. Status change to `Quoted`/`Won` also advances the linked CRM lead stage; every action
   writes an `ActivityLogger` entry. **No `Create` action.**
3. `Views/SuperAdmin/Inquiries.cshtml` — searchable/filterable inbox table, status pills, inline assign form, status
   transitions, delete confirm, `_Pagination`, TempData alerts, and an explanatory banner ("new inquiries arrive
   automatically from the website inquiry form").
4. `Services/RbacService.cs` — `"Inquiries": "Manage"` for Super Admin and Sales CRM.
5. `Views/SuperAdmin/Components/_AdminSidebar.cshtml` — "Inquiries" nav item right under "CRM & Leads" (RBAC-gated);
   CRM page links to the inbox.

### Test Phase A24 (`InquiryAdminTests.cs`) — mirror of Phase 24

- `InquiryController` has `UpdateStatus` / `Assign` / `Delete` but **no `Create`** (reflection).
- `SuperAdminController.Inquiries` pages at 10, honors `search` and `status` filters, and reports totals from the
  full dataset (not the page).
- `UpdateStatus` flips the inquiry status **and** the linked CRM lead stage, and logs an activity row.
- `Assign` sets `AssignedTo` and `UpdatedAt`; `Delete` removes the inquiry (lead survives).
- Unknown id ? `NotFound`; RBAC exposes `Inquiries` for Super Admin + Sales CRM and not for Staff Crew.

**Gate**: build green + A24 green before Phase 25.

**Status - BUILT & verified.** `SuperAdminController.Inquiries` (paged 10/page, search + status + assignee filters,
status KPIs incl. unassigned) + `Views/SuperAdmin/Inquiries.cshtml` (status pills, inline assign + status forms,
delete confirm, pagination, message row); `InquiryController` with `UpdateStatus` / `Assign` / `Delete` and **no
`Create`** (status syncs the CRM lead, every action writes an activity row, delete keeps the lead); RBAC
`Inquiries` + `UpdateInquiryStatus` / `AssignInquiry` / `DeleteInquiry` for Super Admin and Sales CRM only; sidebar
"Inquiry Inbox" item; inbox CSS. `InquiryInboxTests` (36 tests) green.

---

## Phase 25 — Full CRUD Test Sweep (All Controllers)

**Goal**: prove every CRUD surface in the app actually works — the user-facing requirement "test all CRUD".

### Implementation (Phase 25)

1. New test files (xUnit, EF InMemory, `TestControllerSupport`):
   - `StaffCrudTests.cs` - staff Create/Edit/Delete/filters/pagination/Details 404 + staff-assignment Create (flips
     staff to `Busy`), Delete (restores `Available`), event filter, dropdown contents.
   - `MenuPackageCrudTests.cs` - Create/Edit/Delete/pagination, validation redisplay, delete keeps event history.
   - `BillingCrudTests.cs` - quotation Create pricing + `UpdateStatus` (Approved auto-creates exactly one invoice),
     invoice Create status computation / quotation inheritance / `UpdateStatus` Paid settles / Print / Delete,
     payment Create settles + logs, Delete reopens the balance, summary stats.
   - `NotificationSettingsCrudTests.cs` - MarkAsRead / MarkAllRead / Delete / pagination + settings upsert
     (insert then update the single row) feeding the public contact section.
   - `AccountAuthTests.cs` - login GET returnUrl, demo login blocked once Firebase is configured, logout clears the
     role cookie, SwitchRole re-signs-in and honours RBAC, AccessDenied messaging.
   - `CustomerControllerTests` / `EventControllerTests` / `InventoryControllerTests` / `SupplierControllerTests` already
     cover their branches; `ClientInquiryTests` / `InquiryInboxTests` cover the inquiry pipeline end to end.
2. Shared helpers: `TestControllerSupport.RepoFile(...)` (repo-root file reads for view assertions) and
   `CreateContext()` already present.

### Test Phase A25

- Run the **whole** suite: every CRUD verb of every controller has at least one green test; `dotnet build` clean;
  test count grows from the Phase 21 baseline with zero regressions.

**Gate**: full suite green before Phase 26.

**Status - BUILT & verified.** Suite grew 104 -> 159 -> 203 -> 239 -> 316 tests across Phases 22-25 with zero
regressions; every CRUD verb of every controller now has at least one green test.

---

## Phase 26 - Final Integration, Docs, Commit & Push

### Implementation (Phase 26)

- Full `dotnet build cateringflow.csproj` + `dotnet test tests/CateringFlow.Tests`.
- `Phase.md` status lines for Phases 21-26; `docs/01-Frontend-Prototype.md` + `docs/03-API-Functions-Features.md`
  updated with the services/contact surfaces, the inquiry endpoint and the CRM "no manual lead creation" rule.
- Verify no stale references remain: no `Add Lead` control, no `_AddLeadModal`/`CRMLead/Create` references, no view
  pointing at a removed controller action.
- Commit the work and push to `origin` on the current branch.

### Test Phase A26

- Full suite green one final time, post-docs.

**Status - BUILT & verified.** `dotnet build` clean (0 warnings / 0 errors); full suite **316 passed, 0 failed,
0 skipped**; no stale `Add Lead` / `_AddLeadModal` / `CRMLead/Create` / `_BuiltForPros` references remain; committed as
`52d4a62` and pushed to `origin/main`.

---

## Phase 27 - CRM-to-Quotation Conversion (Close the Pipeline Loop)

**Goal**: today an inquiry becomes a CRM lead and the lead can be won, but nobody can turn a qualified lead into an
actual **quotation** — so the commercial loop is still broken. Phase 27 closes it: staff quote a live inquiry from the
inbox or the pipeline, the quotation is **prefilled from the inquiry** (event date, pax, package, venue, client), it is
**linked to the inquiry and the lead**, the inquiry flips to `Quoted`, the lead advances to `Proposal`, and the CRM page
reports how many leads turned into quotes.

### Implementation (Phase 27)

1. `Models/QuotationModel.cs` - `InquiryId?` + `[ForeignKey] Inquiry`, `[NotMapped] InquiryReference`. One quotation per
   inquiry (unique index), so a quote can never be duplicated by accident.
2. `Models/InquiryModel.cs` - inverse `Quotation?` navigation so the inbox can show "QTN-xxxx" without extra queries,
   plus `[NotMapped] IsQuotable` (a `Lost` inquiry cannot be quoted).
3. `Data/CateringFlowDbContext.cs` - `Quotation -> Inquiry` one-to-one (`Restrict` on delete, unique index on
   `InquiryId`) + EF migration `AddQuotationInquiryLink`.
4. `Controllers/QuotationController.cs`:
   - private `BuildFromInquiry(InquiryModel)` - maps inquiry → draft quotation (next `QTN-` number, client, event date
     or +30 days, pax >= 1, package, `Total = PricePerPax * Pax`, `Status = Draft`, `ValidUntil = +14 days`, notes
     carrying the inquiry reference / venue / source) and resolves the **customer** (inquiry → lead → email match →
     auto-create, mirroring the "Won lead creates a customer" rule).
   - `GET CreateFromInquiry(id, returnUrl)` - prefilled review screen (reuses `Views/Quotation/Create.cshtml` with an
     "converted from inquiry" banner); writes nothing.
   - `POST ConvertFromInquiry(id, returnUrl)` - one-click draft: creates the quotation, links it, sets the inquiry to
     `Quoted`, advances the linked lead to `Proposal` (never downgrades `Won`/`Lost`), writes an `ActivityLogger` row and
     a `Sales / CRM Staff` notification, redirects to the new quotation. Converting an inquiry that already has a
     quotation does **not** duplicate it - it redirects to the existing one.
   - `Create(quotation, returnUrl, inquiryId)` - the save path honours the inquiry link: same linking, stage sync,
     activity row and notification.
5. `Controllers/SuperAdminController.cs` - `CRM()` adds `QuotedLeads` / `QuotedValue` and an `InquiryIdByLead` map
   (lead → its inquiry) to `CrmBoardViewModel`; `Inquiries()` includes `Quotation` and a `QuotableCount`.
6. `Models/CrmBoardViewModel.cs` - `QuotedLeads`, `QuotedValue`, `InquiryIdByLead` (query conversion is
   `QuotedLeads / TotalLeads`).
7. Views: `Views/SuperAdmin/CRM.cshtml` shows "N of M leads converted to quotations · ₱X quoted" and a per-row
   **Quote** button (only for leads that have an inquiry); `Views/SuperAdmin/Inquiries.cshtml` gains a Quotation column
   (linked number or "Create quote" / "Review quote") and the actions cell keeps status + delete;
   `Views/Quotation/Create.cshtml` shows the source-inquiry banner + hidden `inquiryId`.
8. `Services/RbacService.cs` - `ConvertInquiryToQuotation` for Super Admin and Sales CRM (staff crew / finance cannot
   convert an inquiry into a quote).
9. `Data/SeedData.cs` - the seeded `Quoted` inquiry gets a linked draft quotation (idempotent) so the inbox and
   pipeline demo the conversion out of the box.

### Test Phase A27 (`CrmQuotationConversionTests.cs`) - mirror of Phase 27

- `BuildFromInquiry` maps every field (number, client, date, pax, package, total = price x pax, Draft, valid-until,
  notes with the inquiry reference); a `Lost` inquiry is not quotable.
- `GET CreateFromInquiry` prefills and writes **no** rows; unknown id -> `NotFound`; the view carries the inquiry banner.
- `POST ConvertFromInquiry` creates one Draft quotation, links quotation -> inquiry -> lead, sets the inquiry to
  `Quoted`, advances the lead to `Proposal`, writes an activity row + notification, and redirects to the quotation.
- The customer is created once and reused (email match does not create a second customer).
- Converting twice does not create a duplicate quotation (redirects to the existing one).
- `Create` with `inquiryId` links and syncs the same way; a lead already `Won` is never downgraded to `Proposal`.
- RBAC exposes `ConvertInquiryToQuotation` for Super Admin + Sales CRM only.
- `/SuperAdmin/CRM` reports the conversion stats and the inquiry id per lead; both views render the conversion control.

**Gate**: build green + A27 green + full suite green before Phase 28.
