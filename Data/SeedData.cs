using cateringflow.Models;

namespace cateringflow.Data;

public static class SeedData
{
    public static void Initialize(CateringFlowDbContext db)
    {
        // ============ ACTIVITY LOG (historical rows; runs even on existing databases) ============
        if (!db.ActivityLogs.Any())
        {
            db.ActivityLogs.AddRange(new List<ActivityLogModel>
            {
                new() { Action = "Created", EntityType = "Customer", EntityId = 1, Description = "Customer \"Juan Dela Cruz\" was created.", PerformedBy = "Admin Rivera", CreatedAt = DateTime.Now.AddDays(-6) },
                new() { Action = "Created", EntityType = "Event", EntityId = 1, Description = "Event \"Dela Cruz Wedding\" was created (150 pax, ₱67,500).", PerformedBy = "Carlo Aquino", CreatedAt = DateTime.Now.AddDays(-5) },
                new() { Action = "Created", EntityType = "Invoice", EntityId = 1, Description = "Invoice INV-2026-001 created for ₱67,500.00 (Unpaid).", PerformedBy = "Luz Castillo", CreatedAt = DateTime.Now.AddDays(-4) },
                new() { Action = "Created", EntityType = "Payment", EntityId = 1, Description = "Payment of ₱20,250.00 via GCash recorded against INV-2026-001.", PerformedBy = "Luz Castillo", CreatedAt = DateTime.Now.AddDays(-3) },
                new() { Action = "Created", EntityType = "Inventory", EntityId = 1, Description = "Inventory item \"Pork Belly\" added (25 kg).", PerformedBy = "Mateo Bagatsing", CreatedAt = DateTime.Now.AddDays(-2) },
                new() { Action = "Updated", EntityType = "Supplier", EntityId = 1, Description = "Supplier \"FreshMart Trading\" was updated.", PerformedBy = "Mateo Bagatsing", CreatedAt = DateTime.Now.AddDays(-1) }
            });
            db.SaveChanges();
        }

        if (db.Customers.Any() || db.MenuPackages.Any() || db.Suppliers.Any())
        {
            SeedInquiries(db);
            return;
        }

        // ============ SETTINGS ============
        db.Settings.Add(new SettingsModel
        {
            CompanyName = "CateringFlow PH",
            CompanyEmail = "admin@cateringflow.ph",
            CompanyPhone = "(02) 8123-4567",
            CompanyAddress = "Unit 12, Frontera Verde, Pasig City, Metro Manila, Philippines",
            Tagline = "Serving unforgettable experiences, one event at a time.",
            UpdatedAt = DateTime.Now
        });

        db.SaveChanges();

        // ============ CUSTOMERS ============
        var customers = new List<CustomerModel>
        {
            new() { FullName = "Juan Dela Cruz", Email = "juan.delacruz@example.com", Phone = "0917-555-1234", Address = "Brgy. San Isidro, Antipolo City", Type = "Individual", Status = "Active", CreatedAt = DateTime.Now.AddMonths(-4), Notes = "Prefers classic Filipino buffet menus." },
            new() { FullName = "Maria Santos", Email = "maria.santos@example.com", Phone = "0918-555-2345", Address = "Greenhills, San Juan City", Type = "Individual", Status = "Active", CreatedAt = DateTime.Now.AddMonths(-3), Notes = "Wedding planning for December 2026." },
            new() { FullName = "ACME Corporation", Email = "events@acmecorp.com", Phone = "02-8875-1234", Address = "BGC, Taguig City", Type = "Corporate", Status = "Active", CreatedAt = DateTime.Now.AddMonths(-6), Notes = "Quarterly company events. Requires LED screen." },
            new() { FullName = "Maria Clara Restaurant Group", Email = "carlo@mariaclara.ph", Phone = "02-8123-4567", Address = "Ortigas Center, Pasig", Type = "Corporate", Status = "Active", CreatedAt = DateTime.Now.AddMonths(-5), Notes = "Annual client appreciation dinner." },
            new() { FullName = "Barangay San Roque", Email = "brgy.sanroque@gmail.com", Phone = "0915-555-6789", Address = "Barangay Hall, San Roque, Manila", Type = "Government", Status = "Active", CreatedAt = DateTime.Now.AddMonths(-2), Notes = "Fiesta and community events." }
        };
        db.Customers.AddRange(customers);
        db.SaveChanges();

        // ============ MENU PACKAGES ============
        var packages = new List<MenuPackageModel>
        {
            new()
            {
                PackageName = "Classic Package",
                Description = "4 Courses · Standard Buffet Setup · 4 Hours Service",
                PricePerPax = 450m,
                CourseCount = 4,
                ServiceHours = 4,
                Status = "Active",
                ImageName = "salmon_dish.jpg",
                Features = "4 Gourmet Courses (Appetizer, 2 Mains, Rice)|1 Refreshing Drink & Dessert|Standard Elegant Buffet Setup|Uniformed Banquet Waitstaff (4 Hours)|Standard Chinaware, Flatware & Glassware|Complimentary Table Centerpieces",
                CreatedAt = DateTime.Now.AddMonths(-6)
            },
            new()
            {
                PackageName = "Premium Package",
                Description = "6 Courses · Themed Setup · Sparkling Toast · 5 Hours Service",
                PricePerPax = 750m,
                CourseCount = 6,
                ServiceHours = 5,
                Status = "Active",
                ImageName = "buffet_spread.jpg",
                Highlight = "Most Popular",
                Features = "6 Master Chef Courses (Appetizer, Soup, 3 Mains, Rice)|Live Artisan Dessert & Pastry Station|Complimentary Sparkling Wine Toasting|Premium Themed Tablescapes & Floral Centerpieces|Dedicated Banquet Captain & Waiters (5 Hours)|VIP Head Table Presidential Service|Unlimited Iced Tea & Cold Beverages",
                CreatedAt = DateTime.Now.AddMonths(-5)
            },
            new()
            {
                PackageName = "Grand Package",
                Description = "8 Courses · Carving Station · Open Bar · Butler Service",
                PricePerPax = 1500m,
                CourseCount = 8,
                ServiceHours = 6,
                Status = "Active",
                ImageName = "event_banner.jpg",
                Features = "8 Bespoke Fine-Dining Plated or Buffet Courses|Live Prime Rib Carving Station & Seafood Bar|Unlimited Premium Craft Cocktails & Mocktails|Luxury Crystal Glassware & 24K Gold Accent Cutlery|Dedicated Event Manager & Butler Service|Full Event Styling & Ambient Lighting Sync|Covered Valet Assistance",
                CreatedAt = DateTime.Now.AddMonths(-4)
            },
            new()
            {
                PackageName = "Signature Feast Package",
                Description = "7 Courses · Living Recipe Stations · 5 Hours Service",
                PricePerPax = 1050m,
                CourseCount = 7,
                ServiceHours = 5,
                Status = "Active",
                ImageName = "hero_spread.jpg",
                Highlight = "New",
                Features = "7 Courses with Interactive Living Stations|Roast & Carve-to-Order Meat Station|Fresh Sushi & Maki Bar|Signature Dessert Buffet with Live Crepe Station|Themed Table Styling & Candelight Accents|Dedicated Waitstaff (5 Hours)|Personalized Menu Card Printing",
                CreatedAt = DateTime.Now.AddMonths(-3)
            },
            new()
            {
                PackageName = "Executive Luxury Package",
                Description = "10 Courses · Michelin-Inspired · 8 Hours Full Service",
                PricePerPax = 2200m,
                CourseCount = 10,
                ServiceHours = 8,
                Status = "Active",
                ImageName = "dessert_station.jpg",
                Highlight = "Premium",
                Features = "10 Michelin-Inspired Bespoke Courses|Caviar & Truffle Accents Station|Full Open Bar with Premium Wines & Craft Cocktails|White-Glove Butler Service (8 Hours)|Luxury Linens, Gold Cutlery & Crystal Barware|Private Chef & Sommelier Consult|Dedicated Event Planner Concierge|Premium Canapé Service during Cocktail Hour",
                CreatedAt = DateTime.Now.AddMonths(-2)
            }
        };
        db.MenuPackages.AddRange(packages);
        db.SaveChanges();

        // ============ SUPPLIERS ============
        var suppliers = new List<SupplierModel>
        {
            new() { SupplierName = "Monterey Meats PH", ContactPerson = "Ramon Monterde", Email = "sales@montereymeats.ph", Phone = "0917-111-2222", Address = "Las Pinas City", Category = "Meats & Poultry", Status = "Active", CreatedAt = DateTime.Now.AddMonths(-6) },
            new() { SupplierName = "Navotas Fresh Catch", ContactPerson = "Berto Naval", Email = "orders@navotasfresh.ph", Phone = "0916-222-3333", Address = "Navotas Fish Port, Malabon", Category = "Seafood", Status = "Active", CreatedAt = DateTime.Now.AddMonths(-5) },
            new() { SupplierName = "Highland Prime Beef", ContactPerson = "Sofia Reyes", Email = "sofia@highlandprime.com", Phone = "0918-333-4444", Address = "Baguio City", Category = "Meats & Poultry", Status = "Active", CreatedAt = DateTime.Now.AddMonths(-4) },
            new() { SupplierName = "Baguio Growers Coop", ContactPerson = "Kiko Alipio", Email = "coop@baguiogrowers.ph", Phone = "0919-444-5555", Address = "La Trinidad, Benguet", Category = "Produce", Status = "Active", CreatedAt = DateTime.Now.AddMonths(-4) },
            new() { SupplierName = "Nueva Ecija Rice Mills", ContactPerson = "Liza Panganiban", Email = "sales@necmills.ph", Phone = "0920-555-6666", Address = "Cabanatuan City, Nueva Ecija", Category = "Grains", Status = "Active", CreatedAt = DateTime.Now.AddMonths(-3) }
        };
        db.Suppliers.AddRange(suppliers);
        db.SaveChanges();

        // ============ INVENTORY ============
        var inventory = new List<InventoryModel>
        {
            new() { ItemCode = "INV-010", ItemName = "Pork Belly (Pork Liempo)", Category = "Meats & Poultry", CurrentStock = 12, MinReorderLevel = 25, Unit = "kg", UnitCost = 320m, SupplierId = suppliers[0].Id, StockStatus = "Low Stock", LastUpdated = DateTime.Now.AddDays(-3) },
            new() { ItemCode = "INV-014", ItemName = "Fresh Black Tiger Shrimp", Category = "Seafood", CurrentStock = 8, MinReorderLevel = 15, Unit = "kg", UnitCost = 580m, SupplierId = suppliers[1].Id, StockStatus = "Expiring Soon", LastUpdated = DateTime.Now.AddDays(-1) },
            new() { ItemCode = "INV-022", ItemName = "Prime Beef Sirloin", Category = "Meats & Poultry", CurrentStock = 0, MinReorderLevel = 30, Unit = "kg", UnitCost = 890m, SupplierId = suppliers[2].Id, StockStatus = "Out of Stock", LastUpdated = DateTime.Now.AddDays(-2) },
            new() { ItemCode = "INV-031", ItemName = "Red Onions", Category = "Produce", CurrentStock = 4, MinReorderLevel = 20, Unit = "kg", UnitCost = 95m, SupplierId = suppliers[3].Id, StockStatus = "Low Stock", LastUpdated = DateTime.Now.AddDays(-4) },
            new() { ItemCode = "INV-045", ItemName = "Jasmine Rice (25kg sack)", Category = "Grains", CurrentStock = 18, MinReorderLevel = 5, Unit = "sacks", UnitCost = 1350m, SupplierId = suppliers[4].Id, StockStatus = "Adequate", LastUpdated = DateTime.Now.AddDays(-6) }
        };
        db.InventoryItems.AddRange(inventory);
        db.SaveChanges();

        // ============ EVENTS ============
        var events = new List<EventModel>
        {
            new() { CustomerId = customers[0].Id, EventName = "Dela Cruz Family Reunion", EventType = "Birthday", EventDate = DateTime.Now.AddDays(45), Venue = "Antipolo Country Club", PaxCount = 120, PackageId = packages[0].Id, TotalAmount = 54000m, Status = "Upcoming", Notes = "Buffet line with lechon carving.", CreatedAt = DateTime.Now.AddDays(-5) },
            new() { CustomerId = customers[1].Id, EventName = "Maria & Joshua Wedding Reception", EventType = "Wedding", EventDate = DateTime.Now.AddMonths(3), Venue = "Meralco Gardens, Quezon City", PaxCount = 250, PackageId = packages[2].Id, TotalAmount = 375000m, Status = "In Progress", Notes = "Grand package with open bar. Tasting set for next month.", CreatedAt = DateTime.Now.AddDays(-30) },
            new() { CustomerId = customers[2].Id, EventName = "ACME Corp Quarterly Townhall", EventType = "Corporate", EventDate = DateTime.Now.AddDays(2), Venue = "ACME Headquarters, BGC", PaxCount = 80, PackageId = packages[0].Id, TotalAmount = 36000m, Status = "Upcoming", Notes = "Need extra serving staff.", CreatedAt = DateTime.Now.AddDays(-10) },
            new() { CustomerId = customers[4].Id, EventName = "Barangay Fiesta Celebration", EventType = "Anniversary", EventDate = DateTime.Now.AddDays(20), Venue = "Barangay Covered Court, San Roque", PaxCount = 300, PackageId = packages[0].Id, TotalAmount = 135000m, Status = "Upcoming", Notes = "Community feast.", CreatedAt = DateTime.Now.AddDays(-15) },
            new() { CustomerId = customers[3].Id, EventName = "Maria Clara Client Appreciation Dinner", EventType = "Corporate", EventDate = DateTime.Now.AddDays(60), Venue = "The Manila Hotel", PaxCount = 150, PackageId = packages[1].Id, TotalAmount = 112500m, Status = "Upcoming", Notes = "Annual client appreciation dinner.", CreatedAt = DateTime.Now.AddDays(-20) }
        };
        db.Events.AddRange(events);
        db.SaveChanges();

        // ============ STAFF ============
        var staff = new List<StaffModel>
        {
            new() { FullName = "Andres Bonifacio Jr.", Email = "andres.jr@cateringflow.ph", Phone = "0917-700-1001", Position = "Head Chef", Availability = "Available", EmploymentType = "Full-time", Specialty = "Filipino Cuisine", DateHired = new DateTime(2022, 3, 15), CreatedAt = DateTime.Now.AddMonths(-6) },
            new() { FullName = "Emilio Jacinto", Email = "emilio.j@cateringflow.ph", Phone = "0918-700-1002", Position = "Executive Sous Chef", Availability = "Busy", EmploymentType = "Full-time", Specialty = "Grilled & Roasted Meats", DateHired = new DateTime(2021, 7, 1), CreatedAt = DateTime.Now.AddMonths(-6) },
            new() { FullName = "Melchora Aquino", Email = "melchora.a@cateringflow.ph", Phone = "0919-700-1003", Position = "Pastry Chef", Availability = "Available", EmploymentType = "Full-time", Specialty = "Desserts & Cakes", DateHired = new DateTime(2023, 1, 10), CreatedAt = DateTime.Now.AddMonths(-5) },
            new() { FullName = "Jose Rizal III", Email = "jose.rizal3@cateringflow.ph", Phone = "0920-700-1004", Position = "Event Server", Availability = "Available", EmploymentType = "Part-time", Specialty = "Buffet Service", DateHired = new DateTime(2024, 5, 22), CreatedAt = DateTime.Now.AddMonths(-4) },
            new() { FullName = "Gregoria de Jesus", Email = "gregoria.dj@cateringflow.ph", Phone = "0921-700-1005", Position = "Event Coordinator", Availability = "Available", EmploymentType = "Full-time", Specialty = "Wedding Planning", DateHired = new DateTime(2020, 9, 14), CreatedAt = DateTime.Now.AddMonths(-4) }
        };
        db.Staff.AddRange(staff);
        db.SaveChanges();

        // ============ STAFF ASSIGNMENTS ============
        var assignments = new List<StaffAssignmentModel>
        {
            new() { StaffId = staff[0].Id, EventId = events[0].Id, RoleAtEvent = "Head Chef", AssignedAt = DateTime.Now.AddDays(-5) },
            new() { StaffId = staff[1].Id, EventId = events[0].Id, RoleAtEvent = "Sous Chef", AssignedAt = DateTime.Now.AddDays(-5) },
            new() { StaffId = staff[2].Id, EventId = events[1].Id, RoleAtEvent = "Pastry Chef", AssignedAt = DateTime.Now.AddDays(-20) },
            new() { StaffId = staff[3].Id, EventId = events[2].Id, RoleAtEvent = "Server", AssignedAt = DateTime.Now.AddDays(-8) },
            new() { StaffId = staff[4].Id, EventId = events[1].Id, RoleAtEvent = "Event Coordinator", AssignedAt = DateTime.Now.AddDays(-20) }
        };
        db.StaffAssignments.AddRange(assignments);
        db.SaveChanges();

        // ============ QUOTATIONS ============
        var quotations = new List<QuotationModel>
        {
            new() { QuotationNumber = "QTN-1001", CustomerId = customers[0].Id, EventId = events[0].Id, EventDate = DateTime.Now.AddDays(45), PaxCount = 120, PackageId = packages[0].Id, TotalAmount = 54000m, Status = "Approved", ValidUntil = DateTime.Now.AddDays(60), CreatedAt = DateTime.Now.AddDays(-6), Notes = "Included complimentary lechon dessert station." },
            new() { QuotationNumber = "QTN-1002", CustomerId = customers[1].Id, EventId = events[1].Id, EventDate = DateTime.Now.AddMonths(3), PaxCount = 250, PackageId = packages[2].Id, TotalAmount = 375000m, Status = "Sent", ValidUntil = DateTime.Now.AddMonths(4), CreatedAt = DateTime.Now.AddDays(-25), Notes = "Client reviewing open bar pricing." },
            new() { QuotationNumber = "QTN-1003", CustomerId = customers[2].Id, EventId = events[2].Id, EventDate = DateTime.Now.AddDays(2), PaxCount = 80, PackageId = packages[0].Id, TotalAmount = 36000m, Status = "Approved", ValidUntil = DateTime.Now.AddDays(7), CreatedAt = DateTime.Now.AddDays(-9), Notes = "Corporate rate applied." },
            new() { QuotationNumber = "QTN-1004", CustomerId = customers[4].Id, EventId = events[3].Id, EventDate = DateTime.Now.AddDays(20), PaxCount = 300, PackageId = packages[0].Id, TotalAmount = 135000m, Status = "Sent", ValidUntil = DateTime.Now.AddDays(25), CreatedAt = DateTime.Now.AddDays(-12), Notes = "Awaiting barangay council approval." },
            new() { QuotationNumber = "QTN-1005", CustomerId = customers[3].Id, EventId = null, EventDate = DateTime.Now.AddDays(60), PaxCount = 150, PackageId = packages[1].Id, TotalAmount = 112500m, Status = "Draft", ValidUntil = DateTime.Now.AddDays(75), CreatedAt = DateTime.Now.AddDays(-3), Notes = "Quote for the client appreciation dinner." }
        };
        db.Quotations.AddRange(quotations);
        db.SaveChanges();

        // ============ INVOICES ============
        var invoices = new List<InvoiceModel>
        {
            new() { InvoiceNumber = "INV-2026-001", QuotationId = quotations[2].Id, CustomerId = customers[2].Id, EventId = events[2].Id, TotalAmount = 36000m, AmountPaid = 36000m, IssueDate = DateTime.Now.AddDays(-8), DueDate = DateTime.Now.AddDays(22), Status = "Paid", CreatedAt = DateTime.Now.AddDays(-8), Notes = "Fully paid via GCash." },
            new() { InvoiceNumber = "INV-2026-002", QuotationId = quotations[3].Id, CustomerId = customers[4].Id, EventId = events[3].Id, TotalAmount = 135000m, AmountPaid = 67500m, IssueDate = DateTime.Now.AddDays(-12), DueDate = DateTime.Now.AddDays(18), Status = "Partial", CreatedAt = DateTime.Now.AddDays(-12), Notes = "50% down payment received." },
            new() { InvoiceNumber = "INV-2026-003", QuotationId = quotations[0].Id, CustomerId = customers[0].Id, EventId = events[0].Id, TotalAmount = 54000m, AmountPaid = 0m, IssueDate = DateTime.Now.AddDays(-6), DueDate = DateTime.Now.AddDays(24), Status = "Unpaid", CreatedAt = DateTime.Now.AddDays(-6), Notes = "Balance due before event date." },
            new() { InvoiceNumber = "INV-2026-004", QuotationId = quotations[4].Id, CustomerId = customers[3].Id, EventId = null, TotalAmount = 112500m, AmountPaid = 0m, IssueDate = DateTime.Now.AddDays(-15), DueDate = DateTime.Now.AddDays(-5), Status = "Overdue", CreatedAt = DateTime.Now.AddDays(-15), Notes = "Awaiting down payment confirmation." },
            new() { InvoiceNumber = "INV-2026-005", QuotationId = quotations[1].Id, CustomerId = customers[1].Id, EventId = events[1].Id, TotalAmount = 375000m, AmountPaid = 0m, IssueDate = DateTime.Now.AddDays(-20), DueDate = DateTime.Now.AddDays(40), Status = "Unpaid", CreatedAt = DateTime.Now.AddDays(-20), Notes = "Wedding package - deposit due." }
        };
        db.Invoices.AddRange(invoices);
        db.SaveChanges();

        // ============ PAYMENTS ============
        var payments = new List<PaymentModel>
        {
            new() { InvoiceId = invoices[0].Id, CustomerId = customers[2].Id, Amount = 36000m, PaymentMethod = "GCash", ReferenceNumber = "GC-881204", PaymentDate = DateTime.Now.AddDays(-8), CreatedAt = DateTime.Now.AddDays(-8), Notes = "Full payment for ACME townhall." },
            new() { InvoiceId = invoices[1].Id, CustomerId = customers[4].Id, Amount = 67500m, PaymentMethod = "Cash", ReferenceNumber = "OR-10234", PaymentDate = DateTime.Now.AddDays(-11), CreatedAt = DateTime.Now.AddDays(-11), Notes = "50% downpayment." },
            new() { InvoiceId = invoices[2].Id, CustomerId = customers[0].Id, Amount = 27000m, PaymentMethod = "PayMaya", ReferenceNumber = "PM-452009", PaymentDate = DateTime.Now.AddDays(-2), CreatedAt = DateTime.Now.AddDays(-2), Notes = "50% downpayment." },
            new() { InvoiceId = invoices[3].Id, CustomerId = customers[3].Id, Amount = 56250m, PaymentMethod = "Credit Card", ReferenceNumber = "CC-778120", PaymentDate = DateTime.Now.AddDays(-1), CreatedAt = DateTime.Now.AddDays(-1), Notes = "50% downpayment." },
            new() { InvoiceId = invoices[4].Id, CustomerId = customers[1].Id, Amount = 37500m, PaymentMethod = "GCash", ReferenceNumber = "GC-992410", PaymentDate = DateTime.Now.AddDays(-3), CreatedAt = DateTime.Now.AddDays(-3), Notes = "Wedding deposit (10%)." }
        };
        db.Payments.AddRange(payments);
        db.SaveChanges();

        // ============ CRM LEADS ============
        var crmLeads = new List<CRMLeadModel>
        {
            new() { CustomerId = customers[1].Id, LeadName = "Maria Santos", Company = "N/A", Email = "maria.santos@example.com", Phone = "0918-555-2345", EstimatedValue = 450000m, Stage = "Negotiation", AssignedTo = "Carlo Aquino", LastContact = DateTime.Now.AddDays(-2), CreatedAt = DateTime.Now.AddMonths(-3), Notes = "Wedding package quote is being reviewed." },
            new() { CustomerId = customers[0].Id, LeadName = "Juan Dela Cruz", Company = "N/A", Email = "juan.delacruz@example.com", Phone = "0917-555-1234", EstimatedValue = 150000m, Stage = "Won", AssignedTo = "Carlo Aquino", LastContact = DateTime.Now.AddDays(-6), CreatedAt = DateTime.Now.AddMonths(-4), Notes = "Converted to family event booking." },
            new() { CustomerId = null, LeadName = "Sara Villanueva", Company = "Villanueva Events", Email = "sara@villanuevaevents.com", Phone = "0917-123-0007", EstimatedValue = 320000m, Stage = "Proposal", AssignedTo = "Carlo Aquino", LastContact = DateTime.Now.AddDays(-1), CreatedAt = DateTime.Now.AddDays(-10), Notes = "Wants to partner for 2027 events." },
            new() { CustomerId = customers[4].Id, LeadName = "Brgy. San Roque Fiesta", Company = "Barangay San Roque", Email = "brgy.sanroque@gmail.com", Phone = "0915-555-6789", EstimatedValue = 150000m, Stage = "Contacted", AssignedTo = "Carlo Aquino", LastContact = DateTime.Now.AddDays(-4), CreatedAt = DateTime.Now.AddDays(-15), Notes = "Community feast quote sent." },
            new() { CustomerId = null, LeadName = "Carlos Mercado", Company = "Mercado Group of Companies", Email = "carlos@mercadogroup.com", Phone = "0917-888-4455", EstimatedValue = 250000m, Stage = "New", AssignedTo = "Carlo Aquino", LastContact = DateTime.Now.AddDays(-1), CreatedAt = DateTime.Now.AddDays(-2), Notes = "Corporate Christmas party prospect." }
        };
        db.CrmLeads.AddRange(crmLeads);
        db.SaveChanges();

        // ============ PAYMENT PROOFS ============
        var paymentProofs = new List<PaymentProofModel>
        {
            new()
            {
                EventId = events[4].Id,
                CustomerId = customers[3].Id,
                PaymentMethod = "GCash",
                ReferenceNumber = "GC-102938",
                ProofImagePath = null,
                Amount = 56250m,
                Status = "Pending",
                CreatedAt = DateTime.Now.AddDays(-1),
                Messages = new List<PaymentMessageModel>
                {
                    new()
                    {
                        SenderRole = "Customer",
                        SenderName = "Carlo Mendoza",
                        Message = "Hi! We just sent the 50% down payment via GCash. Reference number GC-102938. Please verify. Thank you!",
                        CreatedAt = DateTime.Now.AddDays(-1)
                    }
                }
            }
        };
        db.PaymentProofs.AddRange(paymentProofs);
        db.SaveChanges();

        // ============ NOTIFICATIONS ============
        var notifications = new List<NotificationModel>
        {
            new() { Title = "New Booking Inquiry", Message = "Sara Villanueva added a new lead worth ₱320,000. Follow up to close the deal.", Type = "Info", IsRead = false, TargetRole = "Super Admin", CreatedAt = DateTime.Now.AddDays(-1) },
            new() { Title = "Quotation Sent", Message = "QTN-1004 was sent to Barangay San Roque. Awaiting approval.", Type = "Info", IsRead = false, TargetRole = "Super Admin", CreatedAt = DateTime.Now.AddDays(-2) },
            new() { Title = "Low Stock Alert", Message = "Pork Belly (INV-010) is below minimum reorder level. 12 kg remaining.", Type = "Warning", IsRead = false, TargetRole = "Inventory Staff", CreatedAt = DateTime.Now.AddDays(-3) },
            new() { Title = "Payment Proof Awaiting Verification", Message = "Maria Clara Restaurant Group submitted a GCash proof of ₱56,250. Verify under Payment Proofs.", Type = "Warning", IsRead = false, TargetRole = "Finance Staff", CreatedAt = DateTime.Now.AddDays(-1) },
            new() { Title = "Payment Received", Message = "ACME Corporation paid ₱36,000 via GCash for the townhall event.", Type = "Success", IsRead = true, TargetRole = "Finance Staff", CreatedAt = DateTime.Now.AddDays(-8) }
        };
        db.Notifications.AddRange(notifications);
        db.SaveChanges();

        SeedInquiries(db);
    }

    /// <summary>
    /// Seeds website inquiries so the admin inbox is populated. Idempotent — runs on
    /// an existing database too (after the guard above).
    /// </summary>
    private static void SeedInquiries(CateringFlowDbContext db)
    {
        if (db.Inquiries.Any())
        {
            // Database already seeded: still backfill the Phase 27 demo quotation so an
            // older database shows the inquiry -> quotation link too.
            SeedInquiryQuotation(db, db.Inquiries.ToList());
            return;
        }

        var packages = db.MenuPackages.OrderBy(p => p.PricePerPax).ToList();
        var leads = db.CrmLeads.OrderBy(l => l.Id).ToList();

        var inquiries = new List<InquiryModel>
        {
            new()
            {
                FullName = "Kevin Tan",
                Email = "kevin.tan@brightsidehr.ph",
                Phone = "0917-555-1212",
                EventType = "Corporate",
                Company = "Brightside HR Solutions",
                EventDate = DateTime.Today.AddDays(45),
                PaxCount = 120,
                Venue = "Makati Grand Ballroom",
                Message = "We need a full-service buffet for our annual summit. Please send a menu proposal and availability.",
                Source = InquirySources.Website,
                Status = InquiryStatuses.New,
                CreatedAt = DateTime.Now.AddHours(-6)
            },
            new()
            {
                FullName = "Marisol dela Paz",
                Email = "marisol.delapaz@gmail.com",
                Phone = "0918-444-7788",
                EventType = "Wedding",
                EventDate = DateTime.Today.AddDays(90),
                PaxCount = 180,
                Venue = "Tagaytay Ridgeline Resort",
                Message = "Looking for a wedding package with a grand buffet and live stations. Can we schedule a tasting?",
                Source = InquirySources.Website,
                Status = InquiryStatuses.Contacted,
                AssignedTo = "Carlo Aquino",
                CreatedAt = DateTime.Now.AddDays(-2)
            },
            new()
            {
                FullName = "Ramon Ortiz",
                Email = "ramon.ortiz@barangaypineda.gov.ph",
                Phone = "0922-333-1122",
                EventType = "Government",
                Company = "Barangay Pineda",
                EventDate = DateTime.Today.AddDays(30),
                PaxCount = 350,
                Venue = "Pineda Covered Court",
                Message = "Fiesta 2026 catering requirements. Please include vegetarian options and streaming stations.",
                Source = InquirySources.PackagesPage,
                Status = InquiryStatuses.Quoted,
                AssignedTo = "Carlo Aquino",
                CreatedAt = DateTime.Now.AddDays(-8)
            }
        };

        // Attach each seeded inquiry to its own pipeline lead so the CRM board and the
        // inquiry inbox tell the same story.
        for (var i = 0; i < inquiries.Count && i < leads.Count; i++)
        {
            var inquiry = inquiries[i];
            var lead = leads[i];
            inquiry.CrmLeadId = lead.Id;
            lead.Notes = $"Auto-created from {inquiry.Source} inquiry on {inquiry.CreatedAt:MMM dd, yyyy}. {inquiry.Message}";
            if (packages.Count > i)
            {
                inquiry.PackageId = packages[i].Id;
            }
            db.Entry(lead).Property(l => l.Notes).IsModified = true;
        }

        db.Inquiries.AddRange(inquiries);
        db.SaveChanges();

        SeedInquiryQuotation(db, inquiries);
    }

    /// <summary>
    /// Phase 27 - links the seeded "Quoted" inquiry to a draft quotation so the inbox and
    /// the pipeline demonstrate the conversion out of the box. Idempotent.
    /// </summary>
    private static void SeedInquiryQuotation(CateringFlowDbContext db, IReadOnlyList<InquiryModel> inquiries)
    {
        var quoted = inquiries.FirstOrDefault(i => i.Status == InquiryStatuses.Quoted && i.Id > 0);
        if (quoted == null || db.Quotations.Any(q => q.InquiryId == quoted.Id)) return;

        var package = quoted.PackageId.HasValue
            ? db.MenuPackages.FirstOrDefault(p => p.Id == quoted.PackageId.Value)
            : null;
        var lead = quoted.CrmLeadId.HasValue
            ? db.CrmLeads.FirstOrDefault(l => l.Id == quoted.CrmLeadId.Value)
            : null;

        // The quotation needs a customer; reuse the lead's if it already has one.
        var customer = (quoted.CustomerId.HasValue ? db.Customers.FirstOrDefault(c => c.Id == quoted.CustomerId.Value) : null)
                       ?? (lead?.CustomerId != null ? db.Customers.FirstOrDefault(c => c.Id == lead.CustomerId.Value) : null)
                       ?? db.Customers.FirstOrDefault(c => c.Email == quoted.Email);

        if (customer == null)
        {
            customer = new CustomerModel
            {
                FullName = quoted.FullName,
                Email = quoted.Email,
                Phone = quoted.Phone,
                Type = "Government",
                Status = "Active",
                CreatedAt = quoted.CreatedAt,
                Notes = $"Auto-created from {quoted.Source} inquiry {quoted.Reference}."
            };
            db.Customers.Add(customer);
            db.SaveChanges();
            quoted.CustomerId = customer.Id;
        }

        var pax = Math.Max(1, quoted.PaxCount);
        var last = db.Quotations.OrderByDescending(q => q.Id).FirstOrDefault();
        var nextNumber = last != null && int.TryParse(last.QuotationNumber.Replace("QTN-", ""), out var n)
            ? $"QTN-{n + 1}"
            : "QTN-1001";

        db.Quotations.Add(new QuotationModel
        {
            QuotationNumber = nextNumber,
            CustomerId = customer.Id,
            EventDate = (quoted.EventDate ?? DateTime.Today.AddDays(30)).Date,
            PaxCount = pax,
            PackageId = package?.Id,
            TotalAmount = Math.Round((package?.PricePerPax ?? 0m) * pax, 2),
            Status = "Draft",
            ValidUntil = DateTime.Today.AddDays(14),
            CreatedAt = quoted.CreatedAt.AddHours(2),
            Notes = $"Converted from inquiry {quoted.Reference} ({quoted.Source}) · {quoted.EventType} · {quoted.Venue}",
            InquiryId = quoted.Id
        });
        db.SaveChanges();
    }
}