// ========================================
// File: CnabProcessor.Infrastructure/Data/CnabDbContext.cs
// Purpose: Entity Framework Core database context
// ========================================

using Microsoft.EntityFrameworkCore;
using CnabProcessor.Domain.Entities;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;

namespace CnabProcessor.Infrastructure.Data;

/// <summary>
/// Database context for CNAB Processor application.
/// Manages database connections and entity configurations.
/// </summary>
public class CnabDbContext : DbContext
{
    public CnabDbContext(DbContextOptions<CnabDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Transaction entities collection.
    /// </summary>
    public DbSet<Transaction> Transactions { get; set; } = null!;

    /// <summary>
    /// Configures entity models and database schema.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Transaction entity
        modelBuilder.Entity<Transaction>(entity =>
        {
            // Table configuration
            entity.ToTable("Transactions");

            // Primary key
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            // Type - Store as integer, convert to enum
            entity.Property(e => e.Type)
                .IsRequired()
                .HasConversion<int>()
                .HasComment("Transaction type: 1=Debit, 2=Boleto, 3=Financing, 4=Credit, 5=LoanReceipt, 6=Sales, 7=TedReceipt, 8=DocReceipt, 9=Rent");

            // Date
            entity.Property(e => e.Date)
                .IsRequired()
                .HasColumnType("date")
                .HasComment("Date when the transaction occurred");

            // Amount - Decimal with 2 decimal places
            entity.Property(e => e.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasComment("Transaction amount in decimal format");

            // CPF - Brazilian tax ID
            entity.Property(e => e.Cpf)
                .IsRequired()
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasComment("Beneficiary's CPF (only digits)");

            // Card Number
            entity.Property(e => e.CardNumber)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("Card number used in transaction");

            // Time
            entity.Property(e => e.Time)
                .IsRequired()
                .HasComment("Time when the transaction occurred (UTC-3)");

            // Store Owner
            entity.Property(e => e.StoreOwner)
                .IsRequired()
                .HasMaxLength(50)
                .HasComment("Name of the store owner/representative");

            // Store Name
            entity.Property(e => e.StoreName)
                .IsRequired()
                .HasMaxLength(50)
                .HasComment("Name of the store where transaction occurred");

            // CreatedAt
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()")
                .HasComment("Timestamp when record was created in database");

            // Indexes for better query performance
            entity.HasIndex(e => e.StoreName)
                .HasDatabaseName("IX_Transactions_StoreName");

            entity.HasIndex(e => e.Date)
                .HasDatabaseName("IX_Transactions_Date");

            entity.HasIndex(e => new { e.StoreName, e.Date })
                .HasDatabaseName("IX_Transactions_StoreName_Date");

            // Ignore computed properties (not stored in database)
            entity.Ignore(e => e.SignedAmount);
            entity.Ignore(e => e.Nature);
            entity.Ignore(e => e.TypeDescription);
            entity.Ignore(e => e.IsIncome);
            entity.Ignore(e => e.IsExpense);
        });
    }

    /// <summary>
    /// Override SaveChanges to add automatic audit fields.
    /// </summary>
    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to add automatic audit fields.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Automatically set CreatedAt for new entities.
    /// </summary>
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<Transaction>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            entry.Entity.CreatedAt = DateTime.UtcNow;
        }
    }
}