using Microsoft.EntityFrameworkCore;
using cateringflow.Models;

namespace cateringflow.Data;

public class CateringFlowDbContext : DbContext
{
    public CateringFlowDbContext(DbContextOptions<CateringFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<CustomerModel> Customers => Set<CustomerModel>();
    public DbSet<EventModel> Events => Set<EventModel>();
    public DbSet<MenuPackageModel> MenuPackages => Set<MenuPackageModel>();
    public DbSet<InventoryModel> InventoryItems => Set<InventoryModel>();
    public DbSet<SupplierModel> Suppliers => Set<SupplierModel>();
    public DbSet<StaffModel> Staff => Set<StaffModel>();
    public DbSet<StaffAssignmentModel> StaffAssignments => Set<StaffAssignmentModel>();
    public DbSet<QuotationModel> Quotations => Set<QuotationModel>();
    public DbSet<InvoiceModel> Invoices => Set<InvoiceModel>();
    public DbSet<PaymentModel> Payments => Set<PaymentModel>();
    public DbSet<CRMLeadModel> CrmLeads => Set<CRMLeadModel>();
    public DbSet<NotificationModel> Notifications => Set<NotificationModel>();
    public DbSet<SettingsModel> Settings => Set<SettingsModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer relationships
        modelBuilder.Entity<CustomerModel>()
            .HasMany(c => c.Events)
            .WithOne(e => e.Customer)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomerModel>()
            .HasMany(c => c.Quotations)
            .WithOne(q => q.Customer)
            .HasForeignKey(q => q.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomerModel>()
            .HasMany(c => c.Invoices)
            .WithOne(i => i.Customer)
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomerModel>()
            .HasMany(c => c.Payments)
            .WithOne(p => p.Customer)
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomerModel>()
            .HasMany(c => c.CrmLeads)
            .WithOne(l => l.Customer)
            .HasForeignKey(l => l.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Event relationships
        modelBuilder.Entity<EventModel>()
            .HasOne(e => e.Package)
            .WithMany(p => p.Events)
            .HasForeignKey(e => e.PackageId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EventModel>()
            .HasMany(e => e.Quotations)
            .WithOne(q => q.Event)
            .HasForeignKey(q => q.EventId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EventModel>()
            .HasMany(e => e.Invoices)
            .WithOne(i => i.Event)
            .HasForeignKey(i => i.EventId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EventModel>()
            .HasMany(e => e.StaffAssignments)
            .WithOne(sa => sa.Event)
            .HasForeignKey(sa => sa.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // MenuPackage relationships
        modelBuilder.Entity<MenuPackageModel>()
            .HasMany(p => p.Quotations)
            .WithOne(q => q.Package)
            .HasForeignKey(q => q.PackageId)
            .OnDelete(DeleteBehavior.SetNull);

        // Supplier -> Inventory
        modelBuilder.Entity<SupplierModel>()
            .HasMany(s => s.InventoryItems)
            .WithOne(i => i.Supplier)
            .HasForeignKey(i => i.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        // Staff
        modelBuilder.Entity<StaffModel>()
            .HasMany(s => s.Assignments)
            .WithOne(sa => sa.Staff)
            .HasForeignKey(sa => sa.StaffId)
            .OnDelete(DeleteBehavior.Cascade);

        // Quotation -> Invoice
        modelBuilder.Entity<QuotationModel>()
            .HasOne(q => q.Invoice)
            .WithOne(i => i.Quotation)
            .HasForeignKey<InvoiceModel>(i => i.QuotationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Invoice -> Payments
        modelBuilder.Entity<InvoiceModel>()
            .HasMany(i => i.Payments)
            .WithOne(p => p.Invoice)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint on invoice number / quotation number
        modelBuilder.Entity<InvoiceModel>()
            .HasIndex(i => i.InvoiceNumber)
            .IsUnique();

        modelBuilder.Entity<QuotationModel>()
            .HasIndex(q => q.QuotationNumber)
            .IsUnique();

        // Money precision for Philippine peso amounts
        modelBuilder.Entity<EventModel>().Property(e => e.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<MenuPackageModel>().Property(p => p.PricePerPax).HasPrecision(18, 2);
        modelBuilder.Entity<InventoryModel>().Property(i => i.UnitCost).HasPrecision(18, 2);
        modelBuilder.Entity<QuotationModel>().Property(q => q.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<InvoiceModel>().Property(i => i.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<InvoiceModel>().Property(i => i.AmountPaid).HasPrecision(18, 2);
        modelBuilder.Entity<PaymentModel>().Property(p => p.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<CRMLeadModel>().Property(l => l.EstimatedValue).HasPrecision(18, 2);
    }
}