# 1. Frontend Prototype - ALL Transaction Screens

> Each screen includes a label, screenshot, compact access summary, controller reference, HTTP API endpoints, and a short description.

---

## 1.1 Public Landing Page

**Label**: `Client-Index` | **Route**: `/` | **File**: `Views/Client/Index.cshtml`

> [!screenshot]
> `![[screenshots/frontend-01-landing-page.png]]`

**Controller**: `ClientController`
The Landing Page is the main public homepage of CateringFlow. It features a sticky navbar, hero banner with "Book Now" and "Explore Packages" CTAs, animated stats bar, catering services grid (wedding, corporate, galas/municipal, live stations, kitchen management), 4-step booking process, image gallery, package pricing cards, testimonials, a contact / request-a-quote section, CTA banner, and footer. Any visitor can browse packages or send an inquiry.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Client/Index` | Renders the public landing page with active packages and company settings |
| POST | `/Client/Inquiry` | Sends a contact / request-a-quote inquiry (no sign-in needed) → inquiry + CRM lead + notification |

---

## 1.2 Packages Page

**Label**: `Client-Packages` | **Route**: `/Client/Packages` | **File**: `Views/Client/Packages.cshtml`

> [!screenshot]
> `![[screenshots/frontend-02-packages.png]]`

**Controller**: `ClientController`
The Packages Page is used to explore all published catering packages. Visitors see package cards with price per pax, course count, service hours, and feature lists. Clicking "View Details" opens a modal with full package description and a "Book This Package" button. The same contact / request-a-quote form sits below the cards (pre-filled package interest).

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Client/Packages` | Lists active menu packages for client browsing |
| POST | `/Client/Inquiry` | Inquiry sent from this page is tagged with source "Packages Page" |

---

## 1.3 Login / Account Controllers

**Label**: `Account-Login` | **Route**: `/Account/Login` | **File**: `Views/Account/Login.cshtml`

> [!screenshot]
> `![[screenshots/frontend-03-login.png]]`

**Controller**: `AccountController`
The Login Page is a split-screen authentication screen where users sign in with email/password or Google OAuth via Firebase. After sign-in, users are redirected according to their assigned RBAC role. Account Controller handles all authentication, role switching, and access denial flows.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Account/Login` | Renders the login page with Firebase settings |
| POST | `/Account/Login` | Demo login: issues claims cookie + role cookie, redirects by RBAC (disabled if Firebase is configured) |
| POST | `/Account/FirebaseLogin` | Verifies a Firebase ID token, resolves RBAC role from email, issues session cookie |
| GET | `/Account/SwitchRole` | Switches the current user's role cookie, re-signs in, redirects to allowed page |
| GET | `/Account/Logout` | Signs out and deletes the role cookie, redirects to Home |
| GET | `/Account/AccessDenied` | Renders an access-denied page showing the attempted page and current role |

---

## 1.4 Customer Profile Page

**Label**: `Client-Profile` | **Route**: `/Client/Profile` | **File**: `Views/Client/Profile.cshtml`

> [!screenshot]
> `![[screenshots/frontend-04-profile.png]]`

**Controller**: `ClientController`
The Customer Profile Page displays the signed-in customer's avatar initials, name, email, phone, account type, member-since date, and booking count. Customers can review their own account details and click "My Caterings" to view bookings.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Client/Profile` | Shows the signed-in customer's profile with booking count |
| GET | `/Client/CurrentUser` | Returns JSON with the signed-in user's email, name, and phone for booking form pre-fill |

---

## 1.5 My Bookings Page

**Label**: `Client-MyBookings` | **Route**: `/Client/MyBookings` | **File**: `Views/Client/MyBookings.cshtml`

> [!screenshot]
> `![[screenshots/frontend-05-mybookings.png]]`

**Controller**: `ClientController`
The My Bookings Page lists all bookings made by the current customer as cards showing event name, status badge, payment status, event type, date, guest count, package, venue, payment method, and total amount. A "New Booking" button opens the booking wizard.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Client/MyBookings` | Lists all events booked by the signed-in customer with invoices and payment proofs |

---

## 1.6 Booking Details Page

**Label**: `Client-BookingDetails` | **Route**: `/Client/BookingDetails/{id}` | **File**: `Views/Client/BookingDetails.cshtml`

> [!screenshot]
> `![[screenshots/frontend-06-booking-details.png]]`

**Controller**: `ClientController`
The Booking Details Page shows a single booking through three cards: Booking Details (event type, date, venue, guests, package, notes), Booking Summary (total amount, rate, status), and Payment Details (method, reference, amount, status badge, proof thumbnail, and a "Chat about this Payment" button).

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Client/BookingDetails/{id}` | Shows booking details for the event owner only |

---

## 1.7 Booking Wizard Modal (4-Step)

**Label**: `Client-BookingModal` | **Trigger**: "Book Now" / "New Booking" | **File**: `wwwroot/js/client.js`

> [!screenshot]
> `![[screenshots/frontend-07-booking-wizard.png]]`

**Controller**: `ClientController`
The Booking Wizard Modal is a 4-step form: Step 1 (Your Details), Step 2 (Payment Method with QR panels and live total calculation), Step 3 (Proof of Payment drag-and-drop upload), Step 4 (Review & Confirm). On submit it creates the Event, Invoice, PaymentProof, and Notification records.

| HTTP | Route | Description |
|------|-------|-------------|
| POST | `/Client/Book` | Creates a new event + invoice + optional payment proof from the client booking form |

---

## 1.8 Payment Chat Modal

**Label**: `Client-PaymentChat` | **Trigger**: "Chat about this Payment" | **File**: `wwwroot/js/client.js`

> [!screenshot]
> `![[screenshots/frontend-08-payment-chat.png]]`

**Controller**: `ClientController` + `SuperAdminController`
The Payment Chat Modal is a real-time conversation between the customer and admin about a payment proof. It shows proof metadata, chat bubbles with sender distinction and timestamps, image attachments, and a message input. Customers send via ClientController; admins reply via SuperAdminController.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Client/ProofMessages` | Returns JSON chat messages for a payment proof (owner-only) |
| POST | `/Client/SendProofMessage` | Sends a customer chat message on a payment proof thread |
| GET | `/SuperAdmin/ProofAdminChat/{id}` | Returns JSON proof details and chat messages for the admin verification modal |
| POST | `/SuperAdmin/SendProofAdminMessage` | Sends an admin chat message on a payment proof thread |

---

## 9. Super Admin Dashboard Page

**Label**: `SuperAdmin-Dashboard` | **Route**: `/SuperAdmin/Dashboard` | **File**: `Views/SuperAdmin/Dashboard.cshtml`

> [!screenshot]
> `![[screenshots/frontend-09-admin-dashboard.png]]`

**Access**: Super Admin — Manage | All other roles — View | Customer — No Access

**Controller**: `SuperAdminController`
The Dashboard Page is the business overview hub. It shows KPI cards (Total Revenue, Upcoming Events, Active Customers, Pending Payments), a revenue line chart, an events-by-type bar chart, and four list panels (upcoming events, recent payments, low stock alerts, recent activity).

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/SuperAdmin/Dashboard` | Renders the admin dashboard view with KPI cards and charts |

---

## 10. Super Admin Customers Page

**Label**: `SuperAdmin-Customers` | **Route**: `/SuperAdmin/Customers` | **File**: `Views/SuperAdmin/Customers.cshtml`

> [!screenshot]
> `![[screenshots/frontend-10-customers.png]]`

**Access**: Super Admin — Manage | Sales / CRM Staff — Manage | Customer — Own Profile

**Controller**: `CustomerController`
The Customers Page manages the customer database — listing, searching, creating, editing, and deleting customer records. Filters by name, email, type, and status.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Customer/Index` | Lists all customers with optional search/filter by name, type, and status |
| GET | `/Customer/Details/{id}` | Shows a single customer with related events, quotations, invoices, payments, and CRM leads |
| GET | `/Customer/Create` | Renders the create-customer form |
| POST | `/Customer/Create` | Validates and saves a new customer, defaults Status=Active |
| GET | `/Customer/Edit/{id}` | Loads the edit form for an existing customer |
| POST | `/Customer/Edit/{id}` | Updates all fields of an existing customer |
| POST | `/Customer/Delete/{id}` | Deletes a customer by ID |

---

## 11. Super Admin CRM Pipeline Page

**Label**: `SuperAdmin-CRM` | **Route**: `/SuperAdmin/CRM` | **File**: `Views/SuperAdmin/CRM.cshtml`

> [!screenshot]
> `![[screenshots/frontend-11-crm.png]]`

**Access**: Super Admin — Manage | Sales / CRM Staff — Manage | Customer — Interactions

**Controller**: `CRMLeadController`
The CRM Pipeline Page manages potential customers through a live Kanban board plus a filterable lead table. Stages: New, Contacted, Qualified, Proposal, Negotiation, Won, Lost. KPIs include Total Leads, Conversion Rate, Pipeline Value, and Won Deals. Marking a lead as "Won" auto-creates a Customer record. **There is no manual "Add Lead" control** — leads only enter the pipeline from website inquiries (`/Client/Inquiry`), which is how a real CRM behaves.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/CRMLead/Index` | Lists CRM leads with search/stage/assignee filters, 10 per page |
| GET | `/CRMLead/Details/{id}` | Shows a single lead with its linked customer |
| GET | `/CRMLead/Edit/{id}` | Loads the edit form for a lead |
| POST | `/CRMLead/Edit/{id}` | Updates all lead fields including stage, assignedTo, lastContact |
| POST | `/CRMLead/UpdateStage` | Updates a lead's stage; if "Won" with no customer, auto-creates a Customer record |
| POST | `/CRMLead/Delete/{id}` | Deletes a CRM lead by ID |

---

## 11b. Super Admin Inquiry Inbox

**Label**: `SuperAdmin-Inquiries` | **Route**: `/SuperAdmin/Inquiries` | **File**: `Views/SuperAdmin/Inquiries.cshtml`

**Access**: Super Admin — Manage | Sales / CRM Staff — Manage

**Controller**: `InquiryController`
Website inquiries land here automatically (home page + packages page contact forms). Staff work them by hand: assign an owner, advance the status, or delete spam. The linked CRM lead follows the same stage, and deleting an inquiry never deletes the lead.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/SuperAdmin/Inquiries` | Paged inbox (10 per page) with search, status and assignee filters + status KPIs |
| POST | `/Inquiry/UpdateStatus` | Moves the inquiry to New/Contacted/Quoted/Won/Lost and syncs the CRM lead stage |
| POST | `/Inquiry/Assign` | Claims or releases the inquiry (also syncs the lead's assignee) |
| POST | `/Inquiry/Delete/{id}` | Deletes the inquiry record; the CRM lead stays in the pipeline |

---

## 12. Super Admin Events Page

**Label**: `SuperAdmin-Events` | **Route**: `/SuperAdmin/Events` | **File**: `Views/SuperAdmin/Events.cshtml`

> [!screenshot]
> `![[screenshots/frontend-12-events.png]]`

**Access**: Super Admin — Manage | Sales / CRM Staff — Submit / Manage | Event Coordinator — Submit / Manage / View | Customer — Submit / View Own

**Controller**: `EventController`
The Events Page manages customer events and reservations with List and Calendar view modes. Search, type/status filters, and a New Event modal. Table shows event name, customer, type, date, venue, pax, package, status, and amount.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Event/Index` | Lists events with search, type, and status filters |
| GET | `/Event/Details/{id}` | Shows a single event with customer, package, staff assignments, and invoices |
| GET | `/Event/Create` | Renders the create-event form with active customers and packages |
| POST | `/Event/Create` | Saves a new event, calculates TotalAmount from package×pax count, sends notification |
| GET | `/Event/Edit/{id}` | Loads the edit form for an event |
| POST | `/Event/Edit/{id}` | Updates all event fields, recalculates TotalAmount based on package |
| POST | `/Event/Delete/{id}` | Deletes an event by ID |
| POST | `/Event/UpdateStatus` | Updates an event's status field only |

---

## 13. Super Admin Menu Packages Page

**Label**: `SuperAdmin-MenuPackages` | **Route**: `/SuperAdmin/MenuPackages` | **File**: `Views/SuperAdmin/MenuPackages.cshtml`

> [!screenshot]
> `![[screenshots/frontend-13-packages.png]]`

**Access**: Super Admin — Manage | Kitchen Manager — Manage | Customer — View (public)

**Controller**: `MenuPackageController`
The Menu Packages Page creates and manages catering packages with name, description, price per pax, course count, service hours, highlight badge, and status. Customers see published packages on the public portal.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/MenuPackage/Index` | Lists all menu packages ordered by price per pax |
| GET | `/MenuPackage/Details/{id}` | Shows a single package with its events and their customers |
| GET | `/MenuPackage/Create` | Renders the create-package form |
| POST | `/MenuPackage/Create` | Validates and saves a new menu package |
| GET | `/MenuPackage/Edit/{id}` | Loads the edit form for an existing package |
| POST | `/MenuPackage/Edit/{id}` | Updates all package fields |
| POST | `/MenuPackage/Delete/{id}` | Deletes a menu package by ID |

---

## 14. Super Admin Inventory Page

**Label**: `SuperAdmin-Inventory` | **Route**: `/SuperAdmin/Inventory` | **File**: `Views/SuperAdmin/Inventory.cshtml`

> [!screenshot]
> `![[screenshots/frontend-14-inventory.png]]`

**Access**: Super Admin — Manage | Inventory Staff — Manage | Kitchen Manager — Manage

**Controller**: `InventoryController`
The Inventory Page tracks ingredient stock with auto-generated item codes (INV-NNN), category, current stock, reorder level, unit, unit cost, and linked supplier. Stock status badges are auto-computed (Adequate, Low, Critical, Out of Stock) based on current stock vs. reorder level.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Inventory/Index` | Lists inventory items with search/filter by name, category, and stock status |
| GET | `/Inventory/Create` | Renders create form with auto-generated item code and active suppliers |
| POST | `/Inventory/Create` | Saves a new inventory item, auto-computes stock status |
| GET | `/Inventory/Edit/{id}` | Loads the edit form for an inventory item |
| POST | `/Inventory/Edit/{id}` | Updates all fields and recomputes stock status automatically |
| POST | `/Inventory/Delete/{id}` | Deletes an inventory item by ID |

---

## 15. Super Admin Suppliers Page

**Label**: `SuperAdmin-Suppliers` | **Route**: `/SuperAdmin/Suppliers` | **File**: `Views/SuperAdmin/Suppliers.cshtml`

> [!screenshot]
> `![[screenshots/frontend-15-suppliers.png]]`

**Access**: Super Admin — Manage | Inventory Staff — Manage

**Controller**: `SupplierController`
The Suppliers Page manages the supplier database with supplier name, contact person, email, phone, category, and status. Filter by category (Meats & Poultry, Seafood, Produce, Grains, etc.).

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Supplier/Index` | Lists suppliers with search/filter by name, contact person, and category |
| GET | `/Supplier/Create` | Renders the create-supplier form |
| POST | `/Supplier/Create` | Validates and saves a new supplier |
| GET | `/Supplier/Edit/{id}` | Loads the edit form for an existing supplier |
| POST | `/Supplier/Edit/{id}` | Updates all supplier fields |
| POST | `/Supplier/Delete/{id}` | Deletes a supplier by ID |

---

## 16. Super Admin Staff Page

**Label**: `SuperAdmin-Staff` | **Route**: `/SuperAdmin/Staff` | **File**: `Views/SuperAdmin/Staff.cshtml`

> [!screenshot]
> `![[screenshots/frontend-16-staff.png]]`

**Access**: Super Admin — Manage | Event Coordinator — Assign / View | Staff / Crew — View Schedule

**Controller**: `StaffController`
The Staff Page manages the staff directory with name, email, phone, position, availability, employment type, specialty, and hire date. Availability badges (Available, Busy, On Leave) help with scheduling.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Staff/Index` | Lists staff with search/filter by name, position, email, availability, and employment type |
| GET | `/Staff/Details/{id}` | Shows a single staff member with their event assignments |
| GET | `/Staff/Create` | Renders the create-staff form |
| POST | `/Staff/Create` | Validates and saves a new staff member |
| GET | `/Staff/Edit/{id}` | Loads the edit form for a staff member |
| POST | `/Staff/Edit/{id}` | Updates all staff fields |
| POST | `/Staff/Delete/{id}` | Deletes a staff member by ID |

---

## 17. Super Admin Staff Assignments Page

**Label**: `SuperAdmin-StaffAssignments` | **Route**: `/StaffAssignment/Index` | **File**: `Views/StaffAssignment/Index.cshtml`

> [!screenshot]
> `![[screenshots/frontend-17-staff-assignments.png]]`

**Access**: Super Admin — Manage | Event Coordinator — Assign / View | Staff / Crew — View Schedule

**Controller**: `StaffAssignmentController`
The Staff Assignments Page links staff members to events with a role at event (Server, Chef, Coordinator). Assigning a staff member automatically sets their availability to "Busy"; unassigning reverts to "Available".

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/StaffAssignment/Index` | Lists staff assignments, optionally filtered by event |
| GET | `/StaffAssignment/Create` | Renders the create-assignment form with available staff and non-completed events |
| POST | `/StaffAssignment/Create` | Saves assignment and automatically sets staff availability to "Busy" |
| POST | `/StaffAssignment/Delete/{id}` | Removes an assignment and reverts the staff member's availability to "Available" |

---

## 18. Super Admin Quotations Page

**Label**: `SuperAdmin-Quotations` | **Route**: `/SuperAdmin/Quotations` | **File**: `Views/SuperAdmin/Quotations.cshtml`

> [!screenshot]
> `![[screenshots/frontend-18-quotations.png]]`

**Access**: Super Admin — Manage | Sales / CRM Staff — Quotations | Finance Staff — Manage | Customer — No Access

**Controller**: `QuotationController`
The Quotations Page creates and manages customer proposals with auto-numbered quotation codes (QTN-xxxx). Totals are auto-calculated from package price × pax count. Approving a quotation automatically generates the invoice.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Quotation/Index` | Lists quotations with search/filter by number, customer name, and status |
| GET | `/Quotation/Details/{id}` | Shows a single quotation with customer, event, package, and linked invoice |
| GET | `/Quotation/Create` | Renders create form with active customers, events, packages, and auto-generated QTN number |
| POST | `/Quotation/Create` | Saves a quotation, calculates TotalAmount from package price × pax count |
| POST | `/Quotation/UpdateStatus` | Updates status; if "Approved" and no invoice exists, auto-generates an Invoice record |
| POST | `/Quotation/Delete/{id}` | Deletes a quotation by ID |

---

## 19. Super Admin Invoices Page

**Label**: `SuperAdmin-Invoices` | **Route**: `/SuperAdmin/Invoices` | **File**: `Views/SuperAdmin/Invoices.cshtml`

> [!screenshot]
> `![[screenshots/frontend-19-invoices.png]]`

**Access**: Super Admin — Manage | Finance Staff — Manage | Customer — No Access

**Controller**: `InvoiceController`
The Invoices Page manages billing records with invoice number, customer, event, total amount, amount paid, balance, issue and due dates, and status. Status badges (Unpaid, Partial, Paid, Overdue) update automatically as payments are recorded, and the balance is always auto-computed.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Invoice/Index` | Lists invoices with search/filter by invoice number, customer, and status |
| GET | `/Invoice/Details/{id}` | Shows a single invoice with customer, quotation, event, and payments |
| GET | `/Invoice/Create` | Renders create form with active customers, approved quotations, events, and auto-generated INV number |
| POST | `/Invoice/Create` | Saves an invoice, auto-fills customer/event/amount from linked quotation, computes status |
| POST | `/Invoice/UpdateStatus` | Updates invoice status; if "Paid" sets AmountPaid = TotalAmount |
| POST | `/Invoice/Delete/{id}` | Deletes an invoice by ID |

---

## 20. Super Admin Payments Page

**Label**: `SuperAdmin-Payments` | **Route**: `/SuperAdmin/Payments` | **File**: `Views/SuperAdmin/Payments.cshtml`

> [!screenshot]
> `![[screenshots/frontend-20-payments.png]]`

**Access**: Super Admin — Manage | Finance Staff — Manage | Customer — Manage Own Payments

**Controller**: `PaymentController`
The Payments Page records and manages payment transactions against invoices. KPI cards show total collected, payments this month, and the most used method. Recording a payment auto-updates the invoice balance and status; deleting a payment reverses the balance.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Payment/Index` | Lists all payments with summary stats (total collected, this month, most used method) |
| GET | `/Payment/Create` | Renders create-payment form with unpaid/partial invoices, pre-selects invoice if provided |
| POST | `/Payment/Create` | Records a payment, updates linked invoice AmountPaid and status (Paid/Partial) |
| POST | `/Payment/Delete/{id}` | Deletes a payment and reverses the amount on the linked invoice |

---

## 21. Super Admin Payment Proofs Page

**Label**: `SuperAdmin-PaymentProofs` | **Route**: `/SuperAdmin/PaymentProofs` | **File**: `Views/SuperAdmin/PaymentProofs.cshtml`

> [!screenshot]
> `![[screenshots/frontend-21-payment-proofs.png]]`

**Access**: Super Admin — Manage | Finance Staff — Manage | Customer — Interactions

**Controller**: `SuperAdminController`
The Payment Proofs Page verifies customer-submitted payment proofs. It shows pending, approved, and rejected proofs with customer, event, method, reference number, and amount. Approving a proof auto-creates the Payment and updates the Invoice. Rejecting sets status to "Rejected".

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/SuperAdmin/PaymentProofs` | Lists all payment proofs with customer, event, and chat messages |
| POST | `/SuperAdmin/ApprovePaymentProof` | Approves a proof, settles amount on the linked invoice, creates a Payment record, sends notification |
| POST | `/SuperAdmin/RejectPaymentProof` | Sets a payment proof status to "Rejected" |

---

## 22. Super Admin Reports Page

**Label**: `SuperAdmin-Reports` | **Route**: `/SuperAdmin/Reports` | **File**: `Views/SuperAdmin/Reports.cshtml`

> [!screenshot]
> `![[screenshots/frontend-22-reports.png]]`

**Access**: Super Admin — View / Generate | Inventory Staff — View | Kitchen Manager — View | Finance Staff — View / Generate

**Controller**: `SuperAdminController`
The Reports Page is the business intelligence hub. It shows a revenue overview chart, an events-by-type chart, printable report sections, and summary statistics.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/SuperAdmin/Reports` | Renders the admin reports view with revenue charts and printable sections |

---

## 23. Super Admin Notifications Page

**Label**: `SuperAdmin-Notifications` | **Route**: `/SuperAdmin/Notifications` | **File**: `Views/SuperAdmin/Notifications.cshtml`

> [!screenshot]
> `![[screenshots/frontend-23-notifications.png]]`

**Access**: Super Admin — Manage | All other roles — View in bell

**Controller**: `NotificationController`
The Notifications Page lists system alerts with title, message, type (Info/Warning/Success), read status, and date. Notifications are auto-generated for new bookings, payment proofs, and status changes.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Notification/Index` | Lists all notifications ordered by newest first |
| POST | `/Notification/MarkAsRead` | Marks a single notification as read |
| POST | `/Notification/MarkAllRead` | Marks all unread notifications as read |
| POST | `/Notification/Delete` | Deletes a single notification |

---

## 24. Super Admin Settings Page

**Label**: `SuperAdmin-Settings` | **Route**: `/SuperAdmin/Settings` | **File**: `Views/SuperAdmin/Settings.cshtml`

> [!screenshot]
> `![[screenshots/frontend-24-settings.png]]`

**Access**: Super Admin — Manage

**Controller**: `SettingsController`
The Settings Page manages the company profile: company name, email, phone, address, and tagline. These values are stored as a single settings row and appear in reports and public-facing pages. Only the Super Admin can edit.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Settings/Index` | Displays company settings (name, email, phone, address, tagline); creates defaults if none exist |
| POST | `/Settings/Index` | Creates or updates company settings record |

---

## 25. Access Denied Page

**Label**: `Account-AccessDenied` | **Route**: `/Account/AccessDenied` | **File**: `Views/Account/AccessDenied.cshtml`

> [!screenshot]
> `![[screenshots/frontend-25-access-denied.png]]`

**Controller**: `AccountController`
The Access Denied Page is shown by the RoleAccessControlMiddleware when a user tries to open a page their role does not permit. It displays a shield icon, the current role, the attempted page, a short RBAC explanation, and links to return to the dashboard or re-login.

| HTTP | Route | Description |
|------|-------|-------------|
| GET | `/Account/AccessDenied` | Renders an access-denied page showing the attempted page and current role |

---

## RBAC Summary Matrix

| Page | Super Admin | Sales / CRM | Event Coord | Inventory | Kitchen Mgr | Finance | Staff / Crew | Customer |
|---|---|---|---|---|---|---|---|---|
| **Dashboard** | Manage | View | View | View | View | View | View | - |
| **Customers** | Manage | Manage | - | - | - | - | - | Own Profile |
| **CRM** | Manage | Manage | - | - | - | - | - | Interactions |
| **Events** | Manage | Submit / Manage | Submit / Manage / View | - | - | - | - | Submit / View Own |
| **MenuPackages** | Manage | - | - | - | Manage | - | - | View (public) |
| **Inventory** | Manage | - | - | Manage | Manage | - | - | - |
| **Suppliers** | Manage | - | - | Manage | - | - | - | - |
| **Staff** | Manage | - | Assign / View | - | - | - | View Schedule | - |
| **StaffAssignments** | Manage | - | Assign / View | - | - | - | View Schedule | - |
| **Quotations** | Manage | Quotations | - | - | - | Manage | - | - |
| **Invoices** | Manage | - | - | - | - | Manage | - | - |
| **Payments** | Manage | - | - | - | - | Manage | - | Manage Own |
| **PaymentProofs** | Manage | - | - | - | - | Manage | - | Interactions |
| **Reports** | View / Generate | - | - | View | View | View / Generate | - | - |
| **Notifications** | Manage | - | - | - | - | - | - | - |
| **Settings** | Manage | - | - | - | - | - | - | - |