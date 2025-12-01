using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CnabProcessor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false, comment: "Transaction type: 1=Debit, 2=Boleto, 3=Financing, 4=Credit, 5=LoanReceipt, 6=Sales, 7=TedReceipt, 8=DocReceipt, 9=Rent"),
                    Date = table.Column<DateTime>(type: "date", nullable: false, comment: "Date when the transaction occurred"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Transaction amount in decimal format"),
                    Cpf = table.Column<string>(type: "varchar(11)", unicode: false, maxLength: 11, nullable: false, comment: "Beneficiary's CPF (only digits)"),
                    CardNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Card number used in transaction"),
                    Time = table.Column<TimeSpan>(type: "time", nullable: false, comment: "Time when the transaction occurred (UTC-3)"),
                    StoreOwner = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Name of the store owner/representative"),
                    StoreName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Name of the store where transaction occurred"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Timestamp when record was created in database")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Date",
                table: "Transactions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_StoreName",
                table: "Transactions",
                column: "StoreName");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_StoreName_Date",
                table: "Transactions",
                columns: new[] { "StoreName", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transactions");
        }
    }
}
