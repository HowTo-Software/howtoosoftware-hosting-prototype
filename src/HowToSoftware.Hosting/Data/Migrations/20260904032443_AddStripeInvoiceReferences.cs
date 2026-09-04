using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HowToSoftware.Hosting.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeInvoiceReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeInvoiceId",
                table: "Orders",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                table: "Orders",
                type: "TEXT",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeInvoiceId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                table: "Orders");
        }
    }
}
