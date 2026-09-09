using cateringflow.Models;

namespace cateringflow.Data;

public static class SeedData
{
    public static void Initialize(CateringFlowDbContext db)
    {
        if (db.Customers.Any() || db.MenuPackages.Any() || db.Suppliers.Any())
        {
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
            new() { FullName = "Barangay San Roque", Email = "brgy.sanroque@gmail.com", Phone = "0915-555-6789", Address = "Barangay Hall, San Roque, Manila", Type = "Government", Status = "Active", CreatedAt = DateTime.Now.AddMonths(-2), Notes = "Fiesta and community events." },
            new() { FullName = "Dela Torre Family", Email = "delatorrefamily@gmail.com", Phone = "0919-555-3456", Address = "Quezon City", Type = "Individual", Status = "Inactive", CreatedAt = DateTime.Now.AddMonths(-8), Notes = "Was inquiring for a debut but postponed." },
            new() { FullName = "Lopez Manufacturing Inc.", Email = "hr@lopezmfg.com", Phone = "02-8723-8890", Address = "Laguna Technopark, Cabuyao, Laguna", Type = "Corporate", Status = "Active", CreatedAt = DateTime.Now.AddMonths(-1), Notes = "Christmas party booking expected for Q4." },
            new() { FullName = "Reyes & Garcia Law Firm", Email = "admin@reyesgarcia.com", Phone = "02-8891-2200", Address = "Makati Ave, Makati City", Type = "Corporate", Status = "Active", CreatedAt = DateTime.Now.AddMonths(-3), Notes = "Firm anniversary celebration." }
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
                CreatedAt = DateTime.Now.AddMonths(-4)
            }
        };
        db.MenuPackages.AddRange(packages);
        db.SaveChanges();

        // ============ SUPPLIERS ============
        var suppliers = new List<SupplierModel>
        {
            new()
            {
                SupplierName = "Monterey Meats PH",
                ContactPerson = "Ramon Monterde",
                Email = "sales@montereymeats.ph",
                Phone = "0917-111-2222",
                Address = "Las Pinas City",
                Category = "Meats & Poultry",
                Status = "Active",
                CreatedAt = DateTime.Now.AddMonths(-6)
            },
            new()
            {
                SupplierName = "Navotas Fresh Catch",
                ContactPerson = "Berto Naval",
                Email = "orders@navotasfresh.ph",
                Phone = "0916-222-3333",
                Address = "Navotas Fish Port, Malabon",
                Category = "Seafood",
                Status = "Active",
                CreatedAt = DateTime.Now.AddMonths(-5)
            },
            new()
            {
                SupplierName = "Highland Prime Beef",
                ContactPerson = "Sofia Reyes",
                Email = "sofia@highlandprime.com",
                Phone = "0918-333-4444",
                Address = "Baguio City",
                Category = "Meats & Poultry",
                Status = "Active",
                CreatedAt = DateTime.Now.AddMonths(-4)
            },
            new()
            {
                SupplierName = "Baguio Growers Coop",
                ContactPerson = "Kiko Alipio",
                Email = "coop@baguiogrowers.ph",
                Phone = "0919-444-5555",
                Address = "La Trinidad, Benguet",
                Category = "Produce",
                Status = "Active",
                CreatedAt = DateTime.Now.AddMonths(-4)
            },
            new()
            {
                SupplierName = "Nueva Ecija Rice Mills",
                ContactPerson = "Liza Panganiban",
                Email = "sales@necmills.ph",
                Phone = "0920-555-6666",
                Address = "Cabanatuan City, Nueva Ecija",
                Category = "Grains",
                Status = "Active",
                CreatedAt = DateTime.Now.AddMonths(-3)
            }
        };
        db.Suppliers.AddRange(suppliers);
        db.SaveChanges();

        // ============ INVENTORY ============
        var inventory = new List<InventoryModel>
        {
            new()
            {
                ItemCode = "INV-010",
                ItemName = "Pork Belly (Pork Liempo)",
                Category = "Meats & Poultry",
                CurrentStock = 12,
                MinReorderLevel = 25,
                Unit = "kg",
                UnitCost = 320m,
                SupplierId = suppliers[0].Id,
                StockStatus = "Low Stock",
                LastUpdated = DateTime.Now.AddDays(-3)
            },
            new()
            {
                ItemCode = "INV-014",
                ItemName = "Fresh Black Tiger Shrimp",
                Category = "Seafood",
                CurrentStock = 8,
                MinReorderLevel = 15,
                Unit = "kg",
                UnitCost = 580m,
                SupplierId = suppliers[1].Id,
                StockStatus = "Expiring Soon",
                LastUpdated = DateTime.Now.AddDays(-1)
            },
            new()
            {
                ItemCode = "INV-022",
                ItemName = "Prime Beef Sirloin",
                Category = "Meats & Poultry",
                CurrentStock = 0,
                MinReorderLevel = 30,
                Unit = "kg",
                UnitCost = 890m,
                SupplierId = suppliers[2].Id,
                StockStatus = "Out of Stock",
                LastUpdated = DateTime.Now.AddDays(-2)
            },
            new()
            {
                ItemCode = "INV-031",
                ItemName = "Red Onions",
                Category = "Produce",
                CurrentStock = 4,
                MinReorderLevel = 20,
                Unit = "kg",
                UnitCost = 95m,
                SupplierId = suppliers[3].Id,
                StockStatus = "Low Stock",
                LastUpdated = DateTime.Now.AddDays(-4)
            },
            new()
            {
                ItemCode = "INV-045",
                ItemName = "Jasmine Rice (25kg sack)",
                Category = "Grains",
                CurrentStock = 18,
                MinReorderLevel = 5,
                Unit = "sacks",
                UnitCost = 1350m,
                SupplierId = suppliers[4].Id,
                StockStatus = "Adequate",
                LastUpdated = DateTime.Now.AddDays(-6)
            },
            new()
            {
                ItemCode = "INV-002",
                ItemName = "Chicken Breast Fillet",
                Category = "Meats & Poultry",
                CurrentStock = 45,
                MinReorderLevel = 20,
                Unit = "kg",
                UnitCost = 185m,
                SupplierId = suppliers[0].Id,
                StockStatus = "Adequate",
                LastUpdated = DateTime.Now.AddDays(-2)
            }
        };
        db.InventoryItems.AddRange(inventory);
        db.SaveChanges();

        // ============ EVENTS ============
        var events = new List<EventModel>
        {
            new()
            {
                CustomerId = customers[0].Id,
                EventName = "Dela Cruz Family Reunion",
                EventType = "Birthday",
                EventDate = DateTime.Now.AddDays(45),
                Venue = "Antipolo Country Club",
                PaxCount = 120,
                PackageId = packages[0].Id,
                TotalAmount = 54000m,
                Status = "Upcoming",
                Notes = "Buffet line with lechon carving.",
                CreatedAt = DateTime.Now.AddDays(-5)
            },
            new()
            {
                CustomerId = customers[1].Id,
                EventName = "Maria & Joshua Wedding Reception",
                EventType = "Wedding",
                EventDate = DateTime.Now.AddMonths(3),
                Venue = "Meralco Gardens, Quezon City",
                PaxCount = 250,
                PackageId = packages[2].Id,
                TotalAmount = 375000m,
                Status = "In Progress",
                Notes = "Grand package with open bar. Tasting set for next month.",
                CreatedAt = DateTime.Now.AddDays(-30)
            },
            new()
            {
                CustomerId = customers[3].Id,
                EventName = "ACME Corp Quarterly Townhall",
                EventType = "Corporate",
                EventDate = DateTime.Now.AddDays(2),
                Venue = "ACME Headquarters, BGC",
                PaxCount = 80,
                PackageId = packages[0].Id,
                TotalAmount = 36000m,
                Status = "Upcoming",
                Notes = "Need extra serving staff.",
                CreatedAt = DateTime.Now.AddDays(-10)
            },
            new()
            {
                CustomerId = customers[3].Id,
                EventName = "Casino Night Gala",
                EventType = "Corporate",
                EventDate = DateTime.Now.AddMonths(-1),
                Venue = "Manila Hotel",
                PaxCount = 90,
                PackageId = packages[1].Id,
                TotalAmount = 67500m,
                Status = "Completed",
                Notes = "Cocktail style service.",
                CreatedAt = DateTime.Now.AddMonths(-2)
            },
            new()
            {
                CustomerId = customers[4].Id,
                EventName = "Barangay Fiesta Celebration",
                EventType = "Anniversary",
                EventDate = DateTime.Now.AddDays(20),
                Venue = "Barangay Covered Court, San Roque",
                PaxCount = 300,
                PackageId = packages[0].Id,
                TotalAmount = 135000m,
                Status = "Upcoming",
                Notes = "Community feast.",
                CreatedAt = DateTime.Now.AddDays(-15)
            },
            new()
            {
                CustomerId = customers[1].Id,
                EventName = "50th Wedding Anniversary - Santos Couple",
                EventType = "Anniversary",
                EventDate = DateTime.Now.AddMonths(-3),
                Venue = "Intramuros, Manila",
                PaxCount = 150,
                PackageId = packages[1].Id,
                TotalAmount = 112500m,
                Status = "Completed",
                Notes = "", CreatedAt = DateTime.Now.AddMonths(-4)
            },
            new()
            {
                CustomerId = customers[6].Id,
                EventName = "Lopez Manufacturing Christmas Party",
                EventType = "Corporate",
                EventDate = DateTime.Now.AddMonths(3),
                Venue = "Lopez Convention Hall, Cabuyao Laguna",
                PaxCount = 200,
                PackageId = packages[1].Id,
                TotalAmount = 150000m,
                Status = "Upcoming",
                Notes = "December event - pending final headcount.",
                CreatedAt = DateTime.Now.AddDays(-20)
            },
            new()
            {
                CustomerId = customers[7].Id,
                EventName = "Reyes & Garcia Firm's 10th Anniversary Gala",
                EventType = "Corporate",
                EventDate = DateTime.Now.AddMonths(-2),
                Venue = "The Manila Peninsula",
                PaxCount = 60,
                PackageId = packages[2].Id,
                TotalAmount = 90000m,
                Status = "Completed",
                Notes = "High-end plated dinner service.",
                CreatedAt = DateTime.Now.AddMonths(-3)
            }
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
            new() { FullName = "Gregoria de Jesus", Email = "gregoria.dj@cateringflow.ph", Phone = "0921-700-1005", Position = "Event Coordinator", Availability = "On Leave", EmploymentType = "Full-time", Specialty = "Wedding Planning", DateHired = new DateTime(2020, 9, 14), CreatedAt = DateTime.Now.AddMonths(-4) },
            new() { FullName = "Marcelo H. del Pilar", Email = "marcelo.hdp@cateringflow.ph", Phone = "0922-700-1006", Position = "Bartender", Availability = "Available", EmploymentType = "Part-time", Specialty = "Cocktails & Mixology", DateHired = new DateTime(2024, 2, 11), CreatedAt = DateTime.Now.AddMonths(-3) },
            new() { FullName = "Dr. Jose P. Laurel", Email = "jose.laurel@cateringflow.ph", Phone = "0923-700-1007", Position = "Event Server", Availability = "Available", EmploymentType = "Contractual", Specialty = "Fine Dining Service", DateHired = new DateTime(2025, 6, 1), CreatedAt = DateTime.Now.AddMonths(-2) },
            new() { FullName = "Antonio Luna", Email = "antonio.luna@cateringflow.ph", Phone = "0924-700-1008", Position = "Kitchen Porter", Availability = "Available", EmploymentType = "Full-time", Specialty = "Prep & Cleanup", DateHired = new DateTime(2023, 11, 5), CreatedAt = DateTime.Now.AddMonths(-2) }
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
            new() { StaffId = staff[5].Id, EventId = events[1].Id, RoleAtEvent = "Bartender", AssignedAt = DateTime.Now.AddDays(-20) },
            new() { StaffId = staff[3].Id, EventId = events[1].Id, RoleAtEvent = "Server", AssignedAt = DateTime.Now.AddDays(-20) },
            new() { StaffId = staff[6].Id, EventId = events[4].Id, RoleAtEvent = "Server", AssignedAt = DateTime.Now.AddDays(-14) },
            new() { StaffId = staff[7].Id, EventId = events[2].Id, RoleAtEvent = "Kitchen Porter", AssignedAt = DateTime.Now.AddDays(-8) }
        };
        db.StaffAssignments.AddRange(assignments);
        db.SaveChanges();

        // ============ QUOTATIONS ============
        var quotations = new List<QuotationModel>
        {
            new()
            {
                QuotationNumber = "QTN-1001",
                CustomerId = customers[0].Id,
                EventId = events[0].Id,
                EventDate = DateTime.Now.AddDays(45),
                PaxCount = 120,
                PackageId = packages[0].Id,
                TotalAmount = 54000m,
                Status = "Approved",
                ValidUntil = DateTime.Now.AddDays(60),
                CreatedAt = DateTime.Now.AddDays(-6),
                Notes = "Included complimentary lechon dessert station."
            },
            new()
            {
                QuotationNumber = "QTN-1002",
                CustomerId = customers[1].Id,
                EventId = events[1].Id,
                EventDate = DateTime.Now.AddMonths(3),
                PaxCount = 250,
                PackageId = packages[2].Id,
                TotalAmount = 375000m,
                Status = "Sent",
                ValidUntil = DateTime.Now.AddMonths(4),
                CreatedAt = DateTime.Now.AddDays(-25),
                Notes = "Client reviewing open bar pricing."
            },
            new()
            {
                QuotationNumber = "QTN-1003",
                CustomerId = customers[3].Id,
                EventId = events[2].Id,
                EventDate = DateTime.Now.AddDays(2),
                PaxCount = 80,
                PackageId = packages[0].Id,
                TotalAmount = 36000m,
                Status = "Approved",
                ValidUntil = DateTime.Now.AddDays(7),
                CreatedAt = DateTime.Now.AddDays(-9),
                Notes = "Corporate rate applied."
            },
            new()
            {
                QuotationNumber = "QTN-1004",
                CustomerId = customers[4].Id,
                EventId = events[4].Id,
                EventDate = DateTime.Now.AddDays(20),
                PaxCount = 300,
                PackageId = packages[0].Id,
                TotalAmount = 135000m,
                Status = "Sent",
                ValidUntil = DateTime.Now.AddDays(25),
                CreatedAt = DateTime.Now.AddDays(-12),
                Notes = "Awaiting barangay council approval."
            },
            new()
            {
                QuotationNumber = "QTN-1005",
                CustomerId = customers[5].Id,
                EventId = null,
                EventDate = DateTime.Now.AddMonths(4),
                PaxCount = 100,
                PackageId = packages[1].Id,
                TotalAmount = 75000m,
                Status = "Draft",
                ValidUntil = DateTime.Now.AddMonths(5),
                CreatedAt = DateTime.Now.AddDays(-3),
                Notes = "Quote for possible debut event."
            },
            new()
            {
                QuotationNumber = "QTN-1006",
                CustomerId = customers[6].Id,
                EventId = events[6].Id,
                EventDate = DateTime.Now.AddMonths(3),
                PaxCount = 200,
                PackageId = packages[1].Id,
                TotalAmount = 150000m,
                Status = "Rejected",
                ValidUntil = DateTime.Now.AddMonths(4),
                CreatedAt = DateTime.Now.AddDays(-18),
                Notes = "Client went with a cheaper vendor."
            }
        };
        db.Quotations.AddRange(quotations);
        db.SaveChanges();

        // ============ INVOICES ============
        var invoices = new List<InvoiceModel>
        {
            new()
            {
                InvoiceNumber = "INV-2026-001",
                QuotationId = quotations[2].Id,
                CustomerId = customers[3].Id,
                EventId = events[2].Id,
                TotalAmount = 36000m,
                AmountPaid = 36000m,
                IssueDate = DateTime.Now.AddDays(-8),
                DueDate = DateTime.Now.AddDays(22),
                Status = "Paid",
                CreatedAt = DateTime.Now.AddDays(-8),
                Notes = "Fully paid via bank transfer."
            },
            new()
            {
                InvoiceNumber = "INV-2026-002",
                QuotationId = quotations[3].Id,
                CustomerId = customers[4].Id,
                EventId = events[4].Id,
                TotalAmount = 135000m,
                AmountPaid = 67500m,
                IssueDate = DateTime.Now.AddDays(-12),
                DueDate = DateTime.Now.AddDays(18),
                Status = "Partial",
                CreatedAt = DateTime.Now.AddDays(-12),
                Notes = "50% down payment received."
            },
            new()
            {
                InvoiceNumber = "INV-2026-003",
                QuotationId = quotations[0].Id,
                CustomerId = customers[0].Id,
                EventId = events[0].Id,
                TotalAmount = 54000m,
                AmountPaid = 0m,
                IssueDate = DateTime.Now.AddDays(-6),
                DueDate = DateTime.Now.AddDays(24),
                Status = "Unpaid",
                CreatedAt = DateTime.Now.AddDays(-6),
                Notes = "Balance due before event date."
            },
            new()
            {
                InvoiceNumber = "INV-2026-004",
                QuotationId = quotations[4].Id,
                CustomerId = customers[5].Id,
                EventId = null,
                TotalAmount = 75000m,
                AmountPaid = 0m,
                IssueDate = DateTime.Now.AddDays(-15),
                DueDate = DateTime.Now.AddDays(-5),
                Status = "Overdue",
                CreatedAt = DateTime.Now.AddDays(-15),
                Notes = "Client unresponsive on follow-ups."
            },
            new()
            {
                InvoiceNumber = "INV-2026-005",
                CustomerId = customers[1].Id,
                EventId = events[5].Id,
                TotalAmount = 112500m,
                AmountPaid = 112500m,
                IssueDate = DateTime.Now.AddMonths(-3),
                DueDate = DateTime.Now.AddMonths(-3).AddDays(30),
                Status = "Paid",
                CreatedAt = DateTime.Now.AddMonths(-3),
                Notes = "Completed anniversary event."
            },
            new()
            {
                InvoiceNumber = "INV-2026-006",
                CustomerId = customers[7].Id,
                EventId = events[7].Id,
                TotalAmount = 90000m,
                AmountPaid = 90000m,
                IssueDate = DateTime.Now.AddMonths(-2),
                DueDate = DateTime.Now.AddMonths(-2).AddDays(30),
                Status = "Paid",
                CreatedAt = DateTime.Now.AddMonths(-2),
                Notes = "Completed gala event."
            }
        };
        db.Invoices.AddRange(invoices);
        db.SaveChanges();

        // ============ PAYMENTS ============
        var payments = new List<PaymentModel>
        {
            new()
            {
                InvoiceId = invoices[0].Id,
                CustomerId = customers[3].Id,
                Amount = 36000m,
                PaymentMethod = "Bank Transfer",
                ReferenceNumber = "TRF-88271",
                PaymentDate = DateTime.Now.AddDays(-8),
                CreatedAt = DateTime.Now.AddDays(-8),
                Notes = "Full payment for ACME townhall."
            },
            new()
            {
                InvoiceId = invoices[1].Id,
                CustomerId = customers[4].Id,
                Amount = 67500m,
                PaymentMethod = "Cash",
                ReferenceNumber = "OR-10234",
                PaymentDate = DateTime.Now.AddDays(-11),
                CreatedAt = DateTime.Now.AddDays(-11),
                Notes = "50% downpayment."
            },
            new()
            {
                InvoiceId = invoices[4].Id,
                CustomerId = customers[1].Id,
                Amount = 112500m,
                PaymentMethod = "Bank Transfer",
                ReferenceNumber = "TRF-77452",
                PaymentDate = DateTime.Now.AddMonths(-3),
                CreatedAt = DateTime.Now.AddMonths(-3),
                Notes = "Full payment anniversary package."
            },
            new()
            {
                InvoiceId = invoices[5].Id,
                CustomerId = customers[7].Id,
                Amount = 90000m,
                PaymentMethod = "GCash",
                ReferenceNumber = "GC-556201",
                PaymentDate = DateTime.Now.AddMonths(-2),
                CreatedAt = DateTime.Now.AddMonths(-2),
                Notes = "Full payment gala event."
            }
        };
        db.Payments.AddRange(payments);
        db.SaveChanges();

        // ============ CRM LEADS ============
        var crmLeads = new List<CRMLeadModel>
        {
            new() { CustomerId = customers[1].Id, LeadName = "Maria Santos", Company = "N/A", Email = "maria.santos@example.com", Phone = "0918-555-2345", EstimatedValue = 450000m, Stage = "Negotiation", AssignedTo = "Carlo Aquino", LastContact = DateTime.Now.AddDays(-2), CreatedAt = DateTime.Now.AddMonths(-3), Notes = "Wedding package quote is being reviewed." },
            new() { CustomerId = customers[0].Id, LeadName = "Juan Dela Cruz", Company = "N/A", Email = "juan.delacruz@example.com", Phone = "0917-555-1234", EstimatedValue = 150000m, Stage = "Won", AssignedTo = "Carlo Aquino", LastContact = DateTime.Now.AddDays(-6), CreatedAt = DateTime.Now.AddMonths(-4), Notes = "Converted to family event booking." },
            new() { CustomerId = null, LeadName = "Sara Villanueva", Company = "Villanueva Events", Email = "sara@villanuevaevents.com", Phone = "0917-123-0007", EstimatedValue = 320000m, Stage = "Proposal", AssignedTo = "Carlo Aquino", LastContact = DateTime.Now.AddDays(-1), CreatedAt = DateTime.Now.AddDays(-10), Notes = "Wants to partner for 2027 events." },
            new() { CustomerId = customers[7].Id, LeadName = "Atty. Reyes", Company = "Reyes & Garcia Law Firm", Email = "admin@reyesgarcia.com", Phone = "02-8891-2200", EstimatedValue = 250000m, Stage = "Won", AssignedTo = "Carlo Aquino", LastContact = DateTime.Now.AddMonths(-2), CreatedAt = DateTime.Now.AddMonths(-3), Notes = "Gala event converted." },
            new() { CustomerId = null, LeadName = "Mang Lito Catering", Company = "Mang Lito Lechon", Email = "manglito@gmail.com", Phone = "0919-777-8844", EstimatedValue = 100000m, Stage = "New", AssignedTo = "Carlo Aquino", LastContact = DateTime.Now.AddDays(-4), CreatedAt = DateTime.Now.AddDays(-5), Notes = "Partner for bulk lechon supply at events." },
            new() { CustomerId = customers[6].Id, LeadName = "HR Head - Lopez Mfg", Company = "Lopez Manufacturing Inc.", Email = "hr@lopezmfg.com", Phone = "02-8723-8890", EstimatedValue = 200000m, Stage = "Contacted", AssignedTo = "Carlo Aquino", LastContact = DateTime.Now.AddDays(-3), CreatedAt = DateTime.Now.AddDays(-15), Notes = "Christmas party inquiry." },
            new() { CustomerId = null, LeadName = "Kate Fernandez", Company = "N/A", Email = "kate.f@gmail.com", Phone = "0917-222-8899", EstimatedValue = 50000m, Stage = "New", AssignedTo = "Carlo Aquino", LastContact = DateTime.Now.AddDays(-1), CreatedAt = DateTime.Now.AddDays(-2), Notes = "Small birthday party for October." },
            new() { CustomerId = null, LeadName = "Coach Ryan Tan", Company = "BQ Sports Center", Email = "ryan@bqsports.ph", Phone = "0918-333-0011", EstimatedValue = 180000m, Stage = "Locked/Lost", AssignedTo = "Carlo Aquino", LastContact = DateTime.Now.AddMonths(-1), CreatedAt = DateTime.Now.AddMonths(-2), Notes = "Went with self-catered sports event." }
        };
        db.CrmLeads.AddRange(crmLeads);
        db.SaveChanges();

        // ============ NOTIFICATIONS ============
        var notifications = new List<NotificationModel>
        {
            new() { Title = "New Booking Inquiry", Message = "Sara Villanueva added a new lead worth ₱320,000. Follow up to close the deal.", Type = "Info", IsRead = false, TargetRole = "Super Admin", CreatedAt = DateTime.Now.AddDays(-1) },
            new() { Title = "Quotation Sent", Message = "QTN-1004 was sent to Barangay San Roque. Awaiting approval.", Type = "Info", IsRead = false, TargetRole = "Super Admin", CreatedAt = DateTime.Now.AddDays(-2) },
            new() { Title = "Low Stock Alert", Message = "Pork Belly (INV-010) is below minimum reorder level. 12 kg remaining.", Type = "Warning", IsRead = false, TargetRole = "Inventory Staff", CreatedAt = DateTime.Now.AddDays(-3) },
            new() { Title = "Payment Received", Message = "ACME Corporation paid ₱36,000 via bank transfer for the townhall event.", Type = "Success", IsRead = true, TargetRole = "Finance Staff", CreatedAt = DateTime.Now.AddDays(-8) },
            new() { Title = "Invoice Overdue", Message = "Invoice INV-2026-004 for ₱75,000 is overdue. Immediate follow-up required.", Type = "Warning", IsRead = true, TargetRole = "Finance Staff", CreatedAt = DateTime.Now.AddDays(-5) },
            new() { Title = "Event Completed", Message = "ACME Casino Night Gala was marked as completed by the team.", Type = "Info", IsRead = true, TargetRole = "Super Admin", CreatedAt = DateTime.Now.AddMonths(-1) },
            new() { Title = "New Lead Assigned", Message = "Lead 'Mang Lito Catering' was assigned to Carlo Aquino for partnership discussion.", Type = "Info", IsRead = true, TargetRole = "Sales / CRM Staff", CreatedAt = DateTime.Now.AddDays(-5) },
            new() { Title = "Stock Reorder Suggested", Message = "Red Onions (INV-031) is low. Reorder 20 kg from Baguio Growers Coop.", Type = "Info", IsRead = true, TargetRole = "Inventory Staff", CreatedAt = DateTime.Now.AddDays(-4) }
        };
        db.Notifications.AddRange(notifications);
        db.SaveChanges();
    }
}