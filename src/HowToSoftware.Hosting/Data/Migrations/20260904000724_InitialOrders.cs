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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CustomerEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    GameId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PlanId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PlanName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    BillingPeriod = table.Column<int>(type: "INTEGER", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    BaseAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DiscountPercentage = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FinalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ProvisioningStage = table.Column<int>(type: "INTEGER", nullable: false),
                    StripeCheckoutSessionId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    StripeCustomerId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    SubscriptionStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    ServerIdentifier = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedStripeEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
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
                unique: true);

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
