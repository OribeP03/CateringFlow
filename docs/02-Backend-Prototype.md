# 2. Backend Prototype - Source Code Screenshots & Description

> Each section includes a label, source code file reference, screenshot placeholder, and API/Algo usage.

---

## 2.1 Program.cs - Application Entry Point & Configuration

**Label**: `Backend-ProgramCs`  
**File**: `Program.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-01-program-cs.png]]`

**Source Code Overview**:  
The central configuration file that wires up all services and middleware.

**Key Configuration**:
```csharp
// EF Core DbContext registration
builder.Services.AddDbContext<CateringFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// RBAC Service as Singleton
builder.Services.AddSingleton<IRbacService, RbacService>();

// Firebase Admin SDK initialization
FirebaseApp.Create(new AppOptions()
    .SetCredential(GoogleCredential.FromFile(firebaseSettings.ServiceAccountPath)));
```

**Middleware Pipeline Order**:
1. HTTPS Redirection
2. SecurityHeadersMiddleware
3. RateLimitingMiddleware
4. Routing + Authentication + Authorization
5. AuthenticationMiddleware (cookie reconstruction)
6. RoleAccessControlMiddleware (RBAC enforcement)
7. Static Files + Controller Routes

**Startup Tasks**:
- `db.Database.Migrate()` — Auto-applies pending migrations
- `SeedData.Initialize(db)` — Seeds demo data if database is empty
- `FirebaseSeeder.EnsureSeedAdminsAsync()` — Creates Firebase auth accounts

**API/Algo Usage**:
- **Entity Framework Core**: Database migration and ORM
- **Firebase Admin SDK**: Server-side token verification
- **Cookie Authentication**: Session management
- **Dependency Injection**: Service registration (Singleton for RBAC, Scoped for DbContext)

---

## 2.2 CateringFlowDbContext.cs - Database Context & Schema

**Label**: `Backend-DbContext`  
**File**: `Data/CateringFlowDbContext.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-02-db-context.png]]`

**Source Code Overview**:  
Defines the EF Core DbContext with all entity configurations, relationships, and indexes.

**15 DbSets (Tables)**:
```csharp
public DbSet<CustomerModel> Customers { get; set; }
public DbSet<EventModel> Events { get; set; }
public DbSet<MenuPackageModel> MenuPackages { get; set; }
public DbSet<StaffModel> Staff { get; set; }
public DbSet<StaffAssignmentModel> StaffAssignments { get; set; }
public DbSet<SupplierModel> Suppliers { get; set; }
public DbSet<InventoryModel> InventoryItems { get; set; }
public DbSet<QuotationModel> Quotations { get; set; }
public DbSet<InvoiceModel> Invoices { get; set; }
public DbSet<PaymentModel> Payments { get; set; }
public DbSet<PaymentProofModel> PaymentProofs { get; set; }
public DbSet<PaymentMessageModel> PaymentMessages { get; set; }
public DbSet<CRMLeadModel> CrmLeads { get; set; }
public DbSet<NotificationModel> Notifications { get; set; }
public DbSet<SettingsModel> Settings { get; set; }
```

**Key Configurations in OnModelCreating**:
- Unique indexes on `QuotationNumber` and `InvoiceNumber`
- Unique filtered index on `Invoice.QuotationId` (1-to-1 relationship)
- FK relationships with cascade/restrict/set-null delete behaviors
- Decimal precision: `Precision(18, 2)` for all monetary fields

**API/Algo Usage**:
- **Entity Framework Core Fluent API**: Relationship configuration
- **Database-First Migration**: Schema generation from model
- **Index Optimization**: Unique constraints for data integrity

---

## 2.3 SeedData.cs - Database Seeding

**Label**: `Backend-SeedData`  
**File**: `Data/SeedData.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-03-seed-data.png]]`

**Source Code Overview**:  
Idempotent seed method that populates the database with demo data only when empty.

**Seeded Entities** (5 records each):
| Entity | Sample Records |
|---|---|
| Settings | CateringFlow PH company profile |
| Customers | Juan Dela Cruz, Maria Santos, ACME Corp, etc. |
| MenuPackages | Classic (450/pax), Premium (750/pax), Grand (1500/pax), etc. |
| Suppliers | Monterey Meats, Navotas Fresh Catch, Highland Prime, etc. |
| InventoryItems | Pork Belly, Black Tiger Shrimp, Prime Beef Sirloin, etc. |
| Events | Family Reunion, Wedding, Corp Townhall, Fiesta, Client Dinner |
| Staff | Head Chef, Sous Chef, Pastry Chef, Event Server, Coordinator |
| StaffAssignments | 5 staff-to-event links |
| Quotations | QTN-1001 through QTN-1005 |
| Invoices | INV-2026-001 through INV-2026-005 |
| Payments | GCash, Cash, PayMaya, Credit Card payments |
| CrmLeads | 5 leads across pipeline stages |
| PaymentProofs | 1 proof with chat message |
| Notifications | 5 system notifications |

**API/Algo Usage**:
- **Idempotent Seeding**: Checks `db.Database.EnsureCreated()` / entity count before inserting
- **Bulk Insert**: `AddRange()` for efficient batch inserts

---

## 2.4 ClientController.cs - Customer Portal Backend

**Label**: `Backend-ClientController`  
**File**: `Controllers/ClientController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-04-client-controller.png]]`

**Source Code Overview**:  
Handles all public-facing and customer-authenticated endpoints.

**Key Actions**:
| Action | Method | Description |
|---|---|---|
| `Index()` | GET | Renders landing page with active packages |
| `Packages()` | GET | Packages page with embedded JSON data |
| `CurrentUser()` | GET | Returns logged-in user info as JSON |
| `Profile()` | GET | Customer profile (auto-creates Customer row) |
| `MyBookings()` | GET | Lists customer's events |
| `BookingDetails(id)` | GET | Single booking detail (owner-only check) |
| `Book(model)` | POST | Creates Event + Invoice + PaymentProof + Notification |
| `ProofMessages(proofId)` | GET | Returns chat messages for a proof |
| `SendProofMessage(model)` | POST | Sends customer message on payment proof |

**Key Algorithm — Online Booking (`Book` action)**:
```
1. Validate model state
2. Verify user is authenticated
3. Find or create Customer record from Firebase UID
4. Create Event (linked to Customer and Package)
5. Create Invoice (auto-numbered INV-YYYY-xxx, linked to Event)
6. Create PaymentProof (with uploaded image path)
7. Create Notification (alert admin of new booking)
8. Return redirect to BookingDetails
```

**API/Algo Usage**:
- **File Upload**: `IFormFile` handling for proof images, saved to `wwwroot/uploads/`
- **Auto-Numbering**: Invoice numbers generated as `INV-{year}-{sequence}`
- **Owner Validation**: Ensures customers can only view their own bookings
- **Transaction**: EF Core SaveChanges for atomic multi-table insert

---

## 2.5 SuperAdminController.cs - Admin Dashboard Backend

**Label**: `Backend-SuperAdminController`  
**File**: `Controllers/SuperAdminController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-05-superadmin-controller.png]]`

**Source Code Overview**:  
The largest controller — handles all admin ERP/CRM views and actions.

**Key Actions**:
| Action | Description |
|---|---|
| `Dashboard()` | Aggregates KPIs: revenue, events, customers, pending payments |
| `Customers()` | Customer list with search/filter |
| `CRM()` | CRM Kanban board data |
| `Events()` | Events table with search/filter |
| `MenuPackages()` | Package management |
| `Inventory()` | Inventory with stock status computation |
| `Suppliers()` | Supplier management |
| `Staff()` | Staff directory |
| `Quotations()` | Quotation management |
| `Invoices()` | Invoice management |
| `Payments()` | Payment records with KPIs |
| `PaymentProofs()` | Proof verification queue |
| `ApprovePaymentProof(id)` | Approve proof -> create Payment -> update Invoice |
| `RejectPaymentProof(id)` | Reject proof with admin notes |
| `ProofAdminChat(proofId)` | Get chat messages for admin view |
| `SendProofAdminMessage(model)` | Send admin message in proof chat |
| `Reports()` | Report data aggregation |
| `Notifications()` | Notification management |
| `Settings()` | Company profile CRUD |

**Key Algorithm — Approve Payment Proof**:
```
1. Find PaymentProof by ID
2. Set Status = "Approved"
3. Create new Payment record (Amount, Method, Reference from proof)
4. Find linked Invoice
5. Update Invoice.AmountPaid += proof.Amount
6. If AmountPaid >= TotalAmount → Invoice.Status = "Paid"
7. Else → Invoice.Status = "Partial"
8. Create Notification (proof approved)
9. SaveChanges
```

**API/Algo Usage**:
- **LINQ Aggregation**: KPI calculations (Sum, Count, GroupBy)
- **Cascade Business Logic**: Proof approval cascades to Payment and Invoice
- **Auto-Status Computation**: Invoice status derived from payment totals
- **JSON Serialization**: Chat messages returned as JSON for AJAX

---

## 2.6 AccountController.cs - Authentication Backend

**Label**: `Backend-AccountController`  
**File**: `Controllers/AccountController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-06-account-controller.png]]`

**Source Code Overview**:  
Handles authentication, session management, and role resolution.

**Key Actions**:
| Action | Description |
|---|---|
| `Login(GET)` | Renders login page |
| `Login(POST)` | Demo login (credential-less for testing) |
| `FirebaseLogin(model)` | Verifies Firebase ID token, issues cookie |
| `SwitchRole(role)` | Changes role cookie (demo/testing) |
| `Logout()` | Signs out, clears cookies |
| `AccessDenied()` | RBAC denial page |

**Key Algorithm — Firebase Login Flow**:
```
1. Receive Firebase ID token from client-side SDK
2. Verify token: FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken)
3. Extract email from verified token
4. Resolve RBAC role:
   a. Check SeedAccounts list for email match
   b. Check AdminEmails allowlist
   c. Default to "Customer" role
5. Resolve display name from Firebase or seed data
6. Create ClaimsPrincipal with: Name, Email, Role, FirebaseUid
7. Issue authentication cookie (7-day expiry, sliding)
8. Set CateringFlow_Role cookie for middleware access
9. Return success with user info
```

**API/Algo Usage**:
- **Firebase Admin SDK**: `VerifyIdTokenAsync()` for server-side token verification
- **Claims-Based Authentication**: Building ClaimsIdentity with role claims
- **Cookie Authentication**: Issuing encrypted session cookies
- **Role Resolution Algorithm**: Priority-based role lookup (Seed -> AdminEmails -> Default)

---

## 2.7 RbacService.cs - Role-Based Access Control

**Label**: `Backend-RbacService`  
**File**: `Services/RbacService.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-07-rbac-service.png]]`

**Source Code Overview**:  
Static permission matrix that maps roles to allowed pages and permission levels.

**Permission Matrix**:
```csharp
private static readonly Dictionary<string, Dictionary<string, string>> _permissions = new()
{
    ["Super Admin"] = new() {
        ["Dashboard"] = "Manage",
        ["Customers"] = "Manage",
        ["Events"] = "Manage",
        ["MenuPackages"] = "Manage",
        ["Inventory"] = "Manage",
        ["Suppliers"] = "Manage",
        ["Staff"] = "Manage",
        ["Quotations"] = "Manage",
        ["Invoices"] = "Manage",
        ["Payments"] = "Manage",
        ["PaymentProofs"] = "Manage",
        ["CRM"] = "Manage",
        ["Reports"] = "View/Generate",
        ["Notifications"] = "Manage",
        ["Settings"] = "Manage"
    },
    ["Sales / CRM Staff"] = new() {
        ["Dashboard"] = "View",
        ["Customers"] = "Manage",
        ["CRM"] = "Manage",
        ["Events"] = "Submit/Manage",
        ["Quotations"] = "Quotations"
    },
    // ... 6 more roles
};
```

**Interface Methods**:
```csharp
public interface IRbacService {
    bool HasAccess(string role, string page, out string permissionLevel);
    bool CanAccessPage(string role, string page);
    Dictionary<string, string> GetAllowedPages(string role);
}
```

**API/Algo Usage**:
- **Dictionary Lookup**: O(1) permission check
- **Strategy Pattern**: Interface-based service for testability
- **Singleton Registration**: Single instance shared across all requests
- **Out Parameter**: Returns permission level string alongside boolean check

---

## 2.8 EventController.cs - Event CRUD Backend

**Label**: `Backend-EventController`  
**File**: `Controllers/EventController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-08-event-controller.png]]`

**Source Code Overview**:  
Full CRUD operations for event management with filtering.

**Key Actions**:
| Action | Method | Description |
|---|---|---|
| `Index(search, type, status)` | GET | Filtered event list |
| `Details(id)` | GET | Event detail with staff assignments, invoices |
| `Create(model)` | POST | Creates event + notification |
| `Edit(id, model)` | POST | Updates event |
| `Delete(id)` | POST | Deletes event |
| `UpdateStatus(id, status)` | POST | Changes event status |

**Key Algorithm — Event Filtering**:
```csharp
var events = _context.Events.Include(e => e.Customer).Include(e => e.Package).AsQueryable();
if (!string.IsNullOrEmpty(search))
    events = events.Where(e => e.EventName.Contains(search) || e.Customer.FullName.Contains(search));
if (!string.IsNullOrEmpty(type))
    events = events.Where(e => e.EventType == type);
if (!string.IsNullOrEmpty(status))
    events = events.Where(e => e.Status == status);
```

**API/Algo Usage**:
- **LINQ Dynamic Query Building**: Conditional Where clauses for filtering
- **Eager Loading**: `Include()` for related entities (Customer, Package)
- **MVC Model Binding**: Automatic form-to-model mapping
- **Validation**: DataAnnotations on model with ModelState check

---

## 2.9 QuotationController.cs - Quotation & Auto-Invoice

**Label**: `Backend-QuotationController`  
**File**: `Controllers/QuotationController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-09-quotation-controller.png]]`

**Source Code Overview**:  
Quotation CRUD with auto-numbering and automatic invoice generation on approval.

**Key Algorithm — Auto-Numbering**:
```csharp
var lastQtn = _context.Quotations.OrderByDescending(q => q.Id).FirstOrDefault();
int nextNum = lastQtn != null 
    ? int.Parse(lastQtn.QuotationNumber.Replace("QTN-", "")) + 1 
    : 1001;
model.QuotationNumber = $"QTN-{nextNum}";
```

**Key Algorithm — Auto-Invoice on Approval**:
```csharp
[HttpPost]
public async Task<IActionResult> UpdateStatus(int id, string status)
{
    var quotation = await _context.Quotations.FindAsync(id);
    quotation.Status = status;
    
    if (status == "Approved")
    {
        var invoice = new InvoiceModel {
            InvoiceNumber = GenerateInvoiceNumber(),
            QuotationId = quotation.Id,
            CustomerId = quotation.CustomerId,
            EventId = quotation.EventId,
            TotalAmount = quotation.TotalAmount,
            IssueDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            Status = "Unpaid"
        };
        _context.Invoices.Add(invoice);
    }
    await _context.SaveChangesAsync();
    return RedirectToAction(nameof(Index));
}
```

**API/Algo Usage**:
- **Auto-Sequential Numbering**: QTN-xxxx based on last record
- **Cross-Entity Transaction**: Creating Invoice from Quotation data
- **State Machine**: Quotation status workflow (Draft -> Sent -> Approved/Rejected)
- **Cascade Creation**: Approved quotation triggers invoice generation

---

## 2.10 InvoiceController.cs & PaymentController.cs - Financial Backend

**Label**: `Backend-FinancialControllers`  
**Files**: `Controllers/InvoiceController.cs`, `Controllers/PaymentController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-10-financial-controllers.png]]`

**Source Code Overview**:  
Invoice and Payment management with automatic balance computation.

**Key Algorithm — Payment Recording**:
```csharp
// In PaymentController.Create(POST)
var payment = new PaymentModel { InvoiceId = model.InvoiceId, Amount = model.Amount, ... };
_context.Payments.Add(payment);

// Update invoice
var invoice = await _context.Invoices.FindAsync(model.InvoiceId);
invoice.AmountPaid += model.Amount;

if (invoice.AmountPaid >= invoice.TotalAmount)
    invoice.Status = "Paid";
else if (invoice.AmountPaid > 0)
    invoice.Status = "Partial";

await _context.SaveChangesAsync();
```

**Key Algorithm — Payment Deletion with Reversal**:
```csharp
// In PaymentController.Delete(POST)
var invoice = await _context.Invoices.FindAsync(payment.InvoiceId);
invoice.AmountPaid -= payment.Amount;

if (invoice.AmountPaid <= 0)
    invoice.Status = "Unpaid";
else
    invoice.Status = "Partial";

_context.Payments.Remove(payment);
await _context.SaveChangesAsync();
```

**Computed Property on InvoiceModel**:
```csharp
[NotMapped]
public decimal Balance => TotalAmount - AmountPaid;
```

**API/Algo Usage**:
- **Transactional Updates**: Payment + Invoice updated atomically
- **Balance Computation**: Real-time balance via computed property
- **Status Auto-Determination**: Status derived from payment totals
- **Reversal Logic**: Deleting payment reverses invoice balance

---

## 2.11 InventoryController.cs - Stock Management

**Label**: `Backend-InventoryController`  
**File**: `Controllers/InventoryController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-11-inventory-controller.png]]`

**Source Code Overview**:  
Inventory CRUD with auto-generated item codes and stock status computation.

**Key Algorithm — Auto-Generate Item Code**:
```csharp
var prefix = model.Category.Substring(0, 3).ToUpper();
var lastItem = _context.InventoryItems
    .Where(i => i.ItemCode.StartsWith(prefix))
    .OrderByDescending(i => i.ItemCode).FirstOrDefault();
int nextSeq = lastItem != null 
    ? int.Parse(lastItem.ItemCode.Split('-').Last()) + 1 
    : 1;
model.ItemCode = $"{prefix}-{nextSeq:D4}";
```

**Key Algorithm — Stock Status Computation**:
```csharp
if (model.CurrentStock <= 0)
    model.StockStatus = "Out of Stock";
else if (model.CurrentStock < model.MinReorderLevel * 0.5)
    model.StockStatus = "Critical";
else if (model.CurrentStock < model.MinReorderLevel)
    model.StockStatus = "Low";
else
    model.StockStatus = "Adequate";
```

**API/Algo Usage**:
- **Auto-Generated IDs**: Category-prefix + sequential number (e.g., MEA-0001)
- **Threshold Algorithm**: Multi-level stock status based on reorder ratios
- **Dynamic Filtering**: LINQ Where clauses for category and status filters

---

## 2.12 StaffAssignmentController.cs - Staff Scheduling

**Label**: `Backend-StaffAssignment`  
**File**: `Controllers/StaffAssignmentController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-12-staff-assignment.png]]`

**Source Code Overview**:  
Links staff to events with automatic availability management.

**Key Algorithm — Auto-Availability Toggle**:
```csharp
// On Create: Set staff to Busy
[HttpPost]
public async Task<IActionResult> Create(StaffAssignmentModel model)
{
    var staff = await _context.Staff.FindAsync(model.StaffId);
    staff.Availability = "Busy";
    model.AssignedAt = DateTime.Now;
    _context.StaffAssignments.Add(model);
    await _context.SaveChangesAsync();
    return RedirectToAction(nameof(Index));
}

// On Delete: Restore to Available
[HttpPost]
public async Task<IActionResult> Delete(int id)
{
    var assignment = await _context.StaffAssignments.FindAsync(id);
    var staff = await _context.Staff.FindAsync(assignment.StaffId);
    staff.Availability = "Available";
    _context.StaffAssignments.Remove(assignment);
    await _context.SaveChangesAsync();
    return RedirectToAction(nameof(Index));
}
```

**API/Algo Usage**:
- **State Synchronization**: Staff availability auto-syncs with assignments
- **Cascade Delete**: Deleting assignment restores staff state
- **Business Rule Enforcement**: Prevents double-booking (availability check)

---

## 2.13 CRMLeadController.cs - CRM Pipeline Backend

**Label**: `Backend-CRMLeadController`  
**File**: `Controllers/CRMLeadController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-13-crm-lead.png]]`

**Source Code Overview**:  
CRM lead management with auto-customer creation on deal closure.

**Key Algorithm — Auto-Create Customer on "Won"**:
```csharp
[HttpPost]
public async Task<IActionResult> UpdateStage(int id, string stage)
{
    var lead = await _context.CrmLeads.FindAsync(id);
    lead.Stage = stage;
    
    if (stage == "Won" && lead.CustomerId == null)
    {
        var customer = new CustomerModel {
            FullName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            Type = lead.Company != null ? "Corporate" : "Individual",
            Status = "Active"
        };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        lead.CustomerId = customer.Id;
    }
    
    await _context.SaveChangesAsync();
    return RedirectToAction(nameof(Index));
}
```

**API/Algo Usage**:
- **Pipeline State Machine**: Lead stages (New -> Contacted -> Proposal -> Negotiation -> Won)
- **Auto-Entity Creation**: Won deal creates Customer record
- **Conditional Logic**: Corporate vs Individual type based on Company field

---

## 2.14 Client Portal JavaScript (client.js)

**Label**: `Backend-ClientJS`  
**File**: `wwwroot/js/client.js`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-14-client-js.png]]`

**Source Code Overview**:  
524-line vanilla JavaScript file powering the client portal interactions.

**Key Functions**:
| Function | Purpose |
|---|---|
| `openBookingModal()` | Initiates 4-step booking wizard, fetches user data |
| `wizardNext()` / `wizardGoTo(step)` | Step navigation with validation |
| `openPackageDetails(index)` | Shows package detail modal from JSON data |
| `openPaymentChat(proofId)` | Loads payment proof chat messages |
| `SendProofMessage(proofId)` | Posts chat message via AJAX |

**Key Algorithm — Booking Wizard Validation**:
```javascript
function wizardNext() {
    if (currentStep === 1) {
        // Validate: name, phone, date, pax required
        if (!name || !phone || !date || !pax) { showError(); return; }
    } else if (currentStep === 2) {
        // Validate: payment method selected
        if (!paymentMethod) { showError(); return; }
        // Calculate total
        totalAmount = paxCount * packagePrice;
    } else if (currentStep === 3) {
        // Validate: proof image uploaded
        if (!proofFile) { showError(); return; }
    }
    currentStep++;
    wizardGoTo(currentStep);
}
```

**Key Algorithm — AJAX Booking Submission**:
```javascript
const formData = new FormData();
formData.append('FullName', name);
formData.append('Phone', phone);
formData.append('EventDate', date);
formData.append('PaxCount', pax);
formData.append('PackageId', packageId);
formData.append('PaymentMethod', paymentMethod);
formData.append('ProofImage', proofFile);

fetch('/Client/Book', { method: 'POST', body: formData })
    .then(r => { if (r.redirected) window.location.href = r.url; });
```

**API/Algo Usage**:
- **Fetch API**: AJAX requests for data and form submission
- **FormData**: Multipart file upload for proof images
- **DOM Manipulation**: Dynamic UI updates without page reload
- **JSON Data Embedding**: `window.__CATERINGFLOW_PACKAGES__` for client-side access

---

## 2.15 Admin Portal JavaScript (superadmin.js)

**Label**: `Backend-SuperAdminJS`  
**File**: `wwwroot/js/superadmin.js`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-15-superadmin-js.png]]`

**Source Code Overview**:  
234-line JavaScript file for admin dashboard interactivity.

**Key Functions**:
| Function | Purpose |
|---|---|
| Chart.js initialization | Revenue line chart + Events bar chart |
| `toggleView(view)` | List/Calendar view switcher for events |
| Table search/filter | Live filtering of table rows |
| `openAdminModal(id)` / `closeAdminModal(id)` | Modal open/close helpers |
| `handleDeleteRow(id)` | Confirmation dialog for deletions |

**API/Algo Usage**:
- **Chart.js**: Data visualization (line charts, bar charts)
- **Event Delegation**: Efficient table row filtering
- **CSS Class Toggle**: View switching via `.open` class

---

## 2.16 Admin Payment Proof Chat (adminproof.js)

**Label**: `Backend-AdminProofJS`  
**File**: `wwwroot/js/adminproof.js`

> [!screenshot] Place screenshot here
> `![[screenshots/backend-16-admin-proof-js.png]]`

**Source Code Overview**:  
116-line JavaScript file for admin-side payment proof chat.

**Key Functions**:
| Function | Purpose |
|---|---|
| `openAdminProofChat(proofId)` | Opens chat modal, loads proof info + messages |
| `closeAdminProofChat()` | Closes chat modal |
| `loadAdminChat(proofId)` | Fetches messages from `/SuperAdmin/ProofAdminChat` |
| `sendAdminMessage(proofId)` | Posts message to `/SuperAdmin/SendProofAdminMessage` |

**API/Algo Usage**:
- **Fetch API**: AJAX for loading/sending chat messages
- **DOM Rendering**: Dynamic chat bubble creation
- **CSRF Token**: Anti-forgery token included in POST requests
