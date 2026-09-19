using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HowToSoftware.Hosting.Data.CommerceMigrations
{
    /// <inheritdoc />
    public partial class AddPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promotion_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    discount_type = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    discount_percent = table.Column<int>(type: "int", nullable: true),
                    fixed_amount_cents = table.Column<long>(type: "bigint", nullable: true),
                    currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    stackable = table.Column<bool>(type: "bit", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    max_redemptions = table.Column<int>(type: "int", nullable: true),
                    minimum_amount_cents = table.Column<long>(type: "bigint", nullable: true),
                    game_slug = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    plan_slug = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_codes", x => x.id);
                    table.CheckConstraint("ck_promotion_codes_discount_type", "discount_type IN ('percent', 'fixed')");
                    table.CheckConstraint("ck_promotion_codes_discount_value", "(discount_type = 'percent' AND discount_percent BETWEEN 1 AND 100 AND fixed_amount_cents IS NULL) OR (discount_type = 'fixed' AND fixed_amount_cents > 0 AND discount_percent IS NULL)");
                    table.CheckConstraint("ck_promotion_codes_max_redemptions", "max_redemptions IS NULL OR max_redemptions > 0");
                    table.CheckConstraint("ck_promotion_codes_minimum_amount", "minimum_amount_cents IS NULL OR minimum_amount_cents >= 0");
                });

            migrationBuilder.CreateTable(
                name: "promotion_redemptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    promotion_code_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    discount_amount_cents = table.Column<long>(type: "bigint", nullable: false),
                    redeemed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_redemptions", x => x.id);
                    table.CheckConstraint("ck_promotion_redemptions_discount", "discount_amount_cents >= 0");
                    table.ForeignKey(
                        name: "FK_promotion_redemptions_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_redemptions_promotion_codes_promotion_code_id",
                        column: x => x.promotion_code_id,
                        principalTable: "promotion_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_codes_active_expires_at",
                table: "promotion_codes",
                columns: new[] { "active", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_codes_code",
                table: "promotion_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promotion_redemptions_order_id",
                table: "promotion_redemptions",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promotion_redemptions_promotion_code_id_order_id",
                table: "promotion_redemptions",
                columns: new[] { "promotion_code_id", "order_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promotion_redemptions");

            migrationBuilder.DropTable(
                name: "promotion_codes");
        }
    }
}
