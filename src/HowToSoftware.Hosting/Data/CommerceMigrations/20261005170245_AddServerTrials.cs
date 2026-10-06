using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HowToSoftware.Hosting.Data.CommerceMigrations
{
    /// <inheritdoc />
    public partial class AddServerTrials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "server_trials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    normalized_email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    game_slug = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    locale = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    profile_id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    version = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    state = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    verification_token_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    verification_expires_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    access_token_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    pterodactyl_user_id = table.Column<int>(type: "int", nullable: true),
                    pterodactyl_server_id = table.Column<int>(type: "int", nullable: true),
                    server_identifier = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    server_uuid = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    suspended_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    delete_after = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    converted_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    upgrade_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    reservation_expires_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    lease_token = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    lease_expires_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ready_email_sent_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    expired_email_sent_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_email_sent_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    last_failure_class = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_trials", x => x.id);
                    table.ForeignKey(
                        name: "FK_server_trials_orders_upgrade_order_id",
                        column: x => x.upgrade_order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trial_upgrade_orders",
                columns: table => new
                {
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    trial_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trial_upgrade_orders", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_trial_upgrade_orders_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trial_upgrade_orders_server_trials_trial_id",
                        column: x => x.trial_id,
                        principalTable: "server_trials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_server_trials_access_token_hash",
                table: "server_trials",
                column: "access_token_hash",
                unique: true,
                filter: "[access_token_hash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_server_trials_normalized_email",
                table: "server_trials",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_server_trials_pterodactyl_user_id",
                table: "server_trials",
                column: "pterodactyl_user_id",
                unique: true,
                filter: "[pterodactyl_user_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_server_trials_state_next_attempt_at",
                table: "server_trials",
                columns: new[] { "state", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "IX_server_trials_upgrade_order_id",
                table: "server_trials",
                column: "upgrade_order_id",
                unique: true,
                filter: "[upgrade_order_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_server_trials_verification_token_hash",
                table: "server_trials",
                column: "verification_token_hash",
                unique: true,
                filter: "[verification_token_hash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_trial_upgrade_orders_trial_id",
                table: "trial_upgrade_orders",
                column: "trial_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trial_upgrade_orders");

            migrationBuilder.DropTable(
                name: "server_trials");
        }
    }
}
