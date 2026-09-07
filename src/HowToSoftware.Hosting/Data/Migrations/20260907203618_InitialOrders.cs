using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HowToSoftware.Hosting.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, collation: "Latin1_General_100_BIN2"),
                    CustomerEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    GameId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PlanId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    BillingPeriod = table.Column<int>(type: "int", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountPercentage = table.Column<int>(type: "int", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FinalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProvisioningStage = table.Column<int>(type: "int", nullable: false),
                    StripeCheckoutSessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, collation: "Latin1_General_100_BIN2"),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, collation: "Latin1_General_100_BIN2"),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, collation: "Latin1_General_100_BIN2"),
                    StripeSubscriptionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, collation: "Latin1_General_100_BIN2"),
                    StripeInvoiceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, collation: "Latin1_General_100_BIN2"),
                    SubscriptionStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ServerIdentifier = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedStripeEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Type = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedStripeEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StripeCheckoutSessionId",
                table: "Orders",
                column: "StripeCheckoutSessionId",
                unique: true,
                filter: "[StripeCheckoutSessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StripeSubscriptionId",
                table: "Orders",
                column: "StripeSubscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "ProcessedStripeEvents");
        }
    }
}
