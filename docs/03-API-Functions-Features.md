# 3. API Functions/Features - Screenshots & Description

> Each API endpoint is documented with its source code location, request/response format, and process explanation.

---

## 3.1 Authentication API

### 3.1.1 Firebase Login

**Label**: `API-FirebaseLogin`  
**Endpoint**: `POST /Account/FirebaseLogin`  
**File**: `Controllers/AccountController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-01-firebase-login.png]]`

**Request**:
```json
{
    "IdToken": "eyJhbGciOiJSUzI1NiIs...",
    "ReturnUrl": "/SuperAdmin/Dashboard"
}
```

**Process**:
1. Client-side Firebase SDK authenticates user (Google OAuth or Email/Password)
2. Firebase returns an ID token to the browser
3. Browser POSTs the ID token to `/Account/FirebaseLogin`
4. Server calls `FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken)` to cryptographically verify the token
5. Extracts email from verified token payload
6. Resolves RBAC role via `FirebaseSettings.ResolveRole(email)`:
   - First checks seeded accounts list
   - Then checks admin emails allowlist
   - Falls back to "Customer" default role
7. Creates `ClaimsPrincipal` with Name, Email, Role, FirebaseUid claims
8. Signs in with cookie authentication scheme
9. Sets `CateringFlow_Role` cookie for middleware access
10. Returns JSON `{ success: true, user: { name, email, role } }`

**Response**: `200 OK` with user info, or `401 Unauthorized`

---

### 3.1.2 Demo Login

**Label**: `API-DemoLogin`  
**Endpoint**: `POST /Account/Login`  
**File**: `Controllers/AccountController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-02-demo-login.png]]`

**Process**:
1. When Firebase is not configured (no service account file), the legacy login form is active
2. Form POSTs email + password (credentials not validated in demo mode)
3. Server creates a ClaimsPrincipal with the provided email as name
4. Default role assigned: "Super Admin" for demo access
5. Issues authentication cookie and role cookie
6. Redirects to return URL or Dashboard

---

### 3.1.3 Role Switch (Demo)

**Label**: `API-SwitchRole`  
**Endpoint**: `GET /Account/SwitchRole?role=...&returnUrl=...`  
**File**: `Controllers/AccountController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-03-switch-role.png]]`

**Process**:
1. Used for testing different RBAC roles
2. Updates the `CateringFlow_Role` cookie with new role value
3. Rebuilds ClaimsPrincipal with new role claim
4. Signs in with updated claims
5. Redirects back to specified URL

---

### 3.1.4 Current User (Client)

**Label**: `API-CurrentUser`  
**Endpoint**: `GET /Client/CurrentUser`  
**File**: `Controllers/ClientController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-04-current-user.png]]`

**Process**:
1. Checks if user is authenticated via `[Authorize]`
2. Finds Customer record by Firebase UID
3. Returns JSON with user details

**Response**:
```json
{
    "fullName": "Juan Dela Cruz",
    "email": "juan@example.com",
    "phone": "+63 917 123 4567"
}
```

---

## 3.2 Booking API

### 3.2.1 Create Booking

**Label**: `API-CreateBooking`  
**Endpoint**: `POST /Client/Book`  
**File**: `Controllers/ClientController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-05-create-booking.png]]`

**Request** (multipart/form-data):
| Field | Type | Description |
|---|---|---|
| FullName | string | Customer name |
| Phone | string | Contact number |
| EventType | string | Wedding/Birthday/Corporate/etc. |
| EventDate | DateTime | Event date |
| PaxCount | int | Number of guests |
| Venue | string | Event venue |
| PackageId | int | Selected package ID |
| Notes | string | Special requests |
| PaymentMethod | string | GCash/PayMaya/Credit Card/Cash |
| ReferenceNumber | string | Payment reference |
| ProofImage | IFormFile | Payment proof image |
| Amount | decimal | Payment amount |

**Process**:
1. Validate ModelState (all required fields)
2. Find or create `CustomerModel` from authenticated user's Firebase UID
3. Save uploaded proof image to `wwwroot/uploads/` with unique filename
4. **Create Event**: New `EventModel` with customer, package, details
5. **Create Invoice**: Auto-numbered `INV-{year}-{seq}`, linked to event, 30-day due date
6. **Create PaymentProof**: With image path, status = "Pending", linked to event + customer
7. **Create Notification**: Alert admin of new booking submission
8. `SaveChanges()` — All 4 records created atomically
9. Redirect to `/Client/BookingDetails/{eventId}`

**Database Operations**:
```
INSERT INTO Events → new EventModel
INSERT INTO Invoices → new InvoiceModel (INV-2026-006)
INSERT INTO PaymentProofs → new PaymentProofModel (Pending)
INSERT INTO Notifications → new NotificationModel
```

---

### 3.2.2 Payment Proof Chat (Client)

**Label**: `API-ProofMessages`  
**Endpoint**: `GET /Client/ProofMessages?proofId=...`  
**File**: `Controllers/ClientController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-06-proof-messages.png]]`

**Process**:
1. Verify user is authenticated
2. Find PaymentProof by ID
3. Verify the proof belongs to the current customer (owner check)
4. Load all related PaymentMessages ordered by CreatedAt
5. Return JSON array of messages

**Response**:
```json
[
    {
        "senderRole": "Customer",
        "senderName": "Juan Dela Cruz",
        "message": "Here is my GCash receipt",
        "createdAt": "2026-09-14T10:30:00"
    },
    {
        "senderRole": "Admin",
        "senderName": "Admin",
        "message": "Received, verifying now",
        "createdAt": "2026-09-14T10:35:00"
    }
]
```

---

### 3.2.3 Send Proof Message (Client)

**Label**: `API-SendProofMessage`  
**Endpoint**: `POST /Client/SendProofMessage`  
**File**: `Controllers/ClientController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-07-send-proof-message.png]]`

**Process**:
1. Verify authentication and ownership
2. Create new `PaymentMessageModel` with:
   - SenderRole = "Customer"
   - SenderName from authenticated user
   - Message text
   - Optional ImagePath for uploaded images
3. Link to PaymentProof via PaymentProofId
4. SaveChanges

---

## 3.3 Payment Proof Verification API

### 3.3.1 Admin Chat Messages

**Label**: `API-ProofAdminChat`  
**Endpoint**: `GET /SuperAdmin/ProofAdminChat?id=...`  
**File**: `Controllers/SuperAdminController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-08-admin-proof-chat.png]]`

**Process**:
1. Find PaymentProof by ID
2. Load related Customer, Event, and Messages
3. Return JSON with proof metadata + all messages

**Response**:
```json
{
    "proof": {
        "customerName": "Juan Dela Cruz",
        "paymentMethod": "GCash",
        "referenceNumber": "GC-12345678",
        "amount": 22500,
        "status": "Pending",
        "proofImagePath": "/uploads/proof_12345.jpg"
    },
    "messages": [...]
}
```

---

### 3.3.2 Send Admin Message

**Label**: `API-SendAdminMessage`  
**Endpoint**: `POST /SuperAdmin/SendProofAdminMessage`  
**File**: `Controllers/SuperAdminController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-09-send-admin-message.png]]`

**Process**:
1. Verify admin is authenticated
2. Create `PaymentMessageModel` with SenderRole = "Admin"
3. Link to PaymentProof
4. SaveChanges

---

### 3.3.3 Approve Payment Proof

**Label**: `API-ApproveProof`  
**Endpoint**: `POST /SuperAdmin/ApprovePaymentProof/{id}`  
**File**: `Controllers/SuperAdminController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-10-approve-proof.png]]`

**Process** (Multi-step cascade):
```
Step 1: Find PaymentProof by ID
Step 2: Set proof.Status = "Approved"
Step 3: Create new PaymentModel:
         - InvoiceId = null (or linked invoice)
         - CustomerId = proof.CustomerId
         - Amount = proof.Amount
         - PaymentMethod = proof.PaymentMethod
         - ReferenceNumber = proof.ReferenceNumber
         - PaymentDate = DateTime.Now
Step 4: Find linked Invoice (via Event)
Step 5: invoice.AmountPaid += proof.Amount
Step 6: If invoice.AmountPaid >= invoice.TotalAmount:
            invoice.Status = "Paid"
        Else:
            invoice.Status = "Partial"
Step 7: Create NotificationModel (proof approved)
Step 8: SaveChanges (all records updated atomically)
```

**Database Operations**:
```
UPDATE PaymentProofs SET Status = 'Approved' WHERE Id = @id
INSERT INTO Payments → new PaymentModel
UPDATE Invoices SET AmountPaid = AmountPaid + @amount, Status = 'Partial'/'Paid'
INSERT INTO Notifications → approval notification
```

---

### 3.3.4 Reject Payment Proof

**Label**: `API-RejectProof`  
**Endpoint**: `POST /SuperAdmin/RejectPaymentProof/{id}`  
**File**: `Controllers/SuperAdminController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-11-reject-proof.png]]`

**Process**:
1. Find PaymentProof by ID
2. Set proof.Status = "Rejected"
3. Set proof.AdminNotes = rejection reason
4. Create Notification (proof rejected)
5. SaveChanges

---

## 3.4 CRUD Operations API

### 3.4.1 Customer CRUD

**Label**: `API-CustomerCRUD`  
**File**: `Controllers/CustomerController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-12-customer-crud.png]]`

| Endpoint | Method | Process |
|---|---|---|
| `/Customer/Index` | GET | Query with search, type, status filters |
| `/Customer/Create` | POST | Validate → Insert → Redirect |
| `/Customer/Edit/{id}` | POST | Find → Update fields → SaveChanges |
| `/Customer/Delete/{id}` | POST | Find → Remove → SaveChanges |
| `/Customer/Details/{id}` | GET | Eager-load all nav properties (Events, Quotations, Invoices, Payments, Leads) |

---

### 3.4.2 Event CRUD

**Label**: `API-EventCRUD`  
**File**: `Controllers/EventController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-13-event-crud.png]]`

| Endpoint | Method | Process |
|---|---|---|
| `/Event/Index` | GET | Query with search, type, status, date filters |
| `/Event/Create` | POST | Validate → Create Event + Notification → Redirect |
| `/Event/Edit/{id}` | POST | Find → Update fields → SaveChanges |
| `/Event/Delete/{id}` | POST | Find → Remove → SaveChanges |
| `/Event/UpdateStatus/{id}` | POST | Find → Update Status → SaveChanges + Notification |

---

### 3.4.3 Quotation CRUD with Auto-Invoice

**Label**: `API-QuotationCRUD`  
**File**: `Controllers/QuotationController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-14-quotation-crud.png]]`

| Endpoint | Method | Process |
|---|---|---|
| `/Quotation/Create` | POST | Auto-number QTN-xxxx → Calculate Total (Pax × Price) → Insert |
| `/Quotation/UpdateStatus/{id}` | POST | If "Approved" → Auto-generate Invoice → SaveChanges |

---

### 3.4.4 Payment CRUD

**Label**: `API-PaymentCRUD`  
**File**: `Controllers/PaymentController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-15-payment-crud.png]]`

| Endpoint | Method | Process |
|---|---|---|
| `/Payment/Create` | POST | Create Payment → Update Invoice.AmountPaid → Recompute Status |
| `/Payment/Delete/{id}` | POST | Reverse Invoice.AmountPaid → Remove Payment → Recompute Status |

---

### 3.4.5 Inventory CRUD

**Label**: `API-InventoryCRUD`  
**File**: `Controllers/InventoryController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-16-inventory-crud.png]]`

| Endpoint | Method | Process |
|---|---|---|
| `/Inventory/Create` | POST | Auto-generate ItemCode → Compute StockStatus → Insert |
| `/Inventory/Edit/{id}` | POST | Find → Update fields → Recompute StockStatus → SaveChanges |

---

### 3.4.6 Staff Assignment

**Label**: `API-StaffAssignment`  
**File**: `Controllers/StaffAssignmentController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-17-staff-assignment.png]]`

| Endpoint | Method | Process |
|---|---|---|
| `/StaffAssignment/Create` | POST | Create Assignment → Set Staff.Availability = "Busy" |
| `/StaffAssignment/Delete/{id}` | POST | Remove Assignment → Set Staff.Availability = "Available" |

---

### 3.4.7 CRM Pipeline

**Label**: `API-CRMPipeline`  
**File**: `Controllers/CRMLeadController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-18-crm-pipeline.png]]`

| Endpoint | Method | Process |
|---|---|---|
| `/Client/Inquiry` | POST | Website inquiry → Save `InquiryModel` → Auto-create CRM Lead ("New") + Notification + Activity Log. **No manual lead-create endpoint.** |
| `/CRMLead/UpdateStage/{id}` | POST | Update Stage → If "Won" + no Customer → Auto-create Customer |
| `/Inquiry/UpdateStatus/{id}` | POST | Move inquiry to Contacted/Quoted/Won/Lost → Syncs the linked CRM lead stage + Notification + Activity Log |
| `/Inquiry/Assign/{id}` | POST | Assign/unassign the inquiry owner (also syncs the lead's `AssignedTo`) |
| `/Inquiry/Delete/{id}` | POST | Delete the inquiry record → CRM lead is kept in the pipeline |

---

## 3.5 Notification API

**Label**: `API-Notifications`  
**File**: `Controllers/NotificationController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-19-notifications.png]]`

| Endpoint | Method | Process |
|---|---|---|
| `/Notification/Index` | GET | List all notifications |
| `/Notification/MarkAsRead/{id}` | POST | Set IsRead = true |
| `/Notification/MarkAllRead` | POST | Set all IsRead = true |
| `/Notification/Delete/{id}` | POST | Remove notification |

**Auto-Generated Notifications**:
- New booking submitted → Admin notification
- Payment proof approved/rejected → Customer notification
- Event status changed → Relevant party notification
- Lead marked as Won → CRM notification

---

## 3.6 Settings API

**Label**: `API-Settings`  
**File**: `Controllers/SettingsController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/api-20-settings.png]]`

| Endpoint | Method | Process |
|---|---|---|
| `/Settings/Index` | GET | Load single Settings row (create if not exists) |
| `/Settings/Index` | POST | Update or create Settings row |

**Process**:
1. Check if Settings row exists
2. If not, create with default values (Company Name = "CateringFlow PH")
3. On POST: Update all fields, set UpdatedAt = DateTime.Now
4. SaveChanges

---

## 3.7 Dashboard KPI API

**Label**: `API-DashboardKPIs`  
**File**: `Controllers/SuperAdminController.cs` → `Dashboard()` action

> [!screenshot] Place screenshot here
> `![[screenshots/api-21-dashboard-kpis.png]]`

**KPI Computation**:
```csharp
// Total Revenue
var totalRevenue = _context.Payments.Sum(p => p.Amount);

// Upcoming Events
var upcomingEvents = _context.Events.Count(e => e.Status == "Upcoming");

// Active Customers
var activeCustomers = _context.Customers.Count(c => c.Status == "Active");

// Pending Payments (invoices with status Unpaid or Partial)
var pendingPayments = _context.Invoices.Count(i => i.Status == "Unpaid" || i.Status == "Partial");
```

**API/Algo Usage**:
- **LINQ Aggregate Functions**: Sum, Count for real-time KPIs
- **Entity Framework Core**: Direct database queries
- **Computed Properties**: Invoice.Balance for real-time balance display
