using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HowToSoftware.Hosting.Data.CommerceMigrations
{
    /// <inheritdoc />
    public partial class InitialCommerce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    hts_user_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    stripe_customer_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    slug = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    primary_game = table.Column<bool>(type: "bit", nullable: false),
                    artwork_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_games", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hosting_nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    pterodactyl_node_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    region = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    location = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    enabled = table.Column<bool>(type: "bit", nullable: false),
                    maintenance = table.Column<bool>(type: "bit", nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    supported_games = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hosting_nodes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stripe_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    stripe_event_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    event_type = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    processed = table.Column<bool>(type: "bit", nullable: false),
                    processing_error = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stripe_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "game_deployment_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    game_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    pterodactyl_nest_id = table.Column<int>(type: "int", nullable: false),
                    pterodactyl_egg_id = table.Column<int>(type: "int", nullable: false),
                    docker_image = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    default_startup = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    deployment_config_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_deployment_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_game_deployment_profiles_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hosting_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    game_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    slug = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ram_mb = table.Column<int>(type: "int", nullable: false),
                    cpu_percent = table.Column<int>(type: "int", nullable: false),
                    disk_mb = table.Column<int>(type: "int", nullable: false),
                    allocation_limit = table.Column<int>(type: "int", nullable: false),
                    database_limit = table.Column<int>(type: "int", nullable: false),
                    backup_limit = table.Column<int>(type: "int", nullable: false),
                    monthly_price_cents = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hosting_plans", x => x.id);
                    table.CheckConstraint("ck_hosting_plans_limits", "allocation_limit >= 0 AND database_limit >= 0 AND backup_limit >= 0");
                    table.CheckConstraint("ck_hosting_plans_price", "monthly_price_cents >= 0");
                    table.CheckConstraint("ck_hosting_plans_resources", "ram_mb > 0 AND cpu_percent > 0 AND disk_mb > 0");
                    table.ForeignKey(
                        name: "FK_hosting_plans_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_profile_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    hts_user_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    customer_email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    game_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    game_slug = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    plan_slug = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    plan_name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    billing_period = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    monthly_price_cents = table.Column<long>(type: "bigint", nullable: false),
                    base_amount_cents = table.Column<long>(type: "bigint", nullable: false),
                    discount_percent = table.Column<int>(type: "int", nullable: false),
                    discount_amount_cents = table.Column<long>(type: "bigint", nullable: false),
                    final_amount_cents = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    provisioning_stage = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    stripe_customer_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    stripe_checkout_session_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    stripe_payment_intent_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    stripe_subscription_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    stripe_invoice_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    subscription_status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    server_identifier = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    failure_reason = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.CheckConstraint("ck_orders_amounts", "monthly_price_cents >= 0 AND base_amount_cents >= 0 AND discount_amount_cents >= 0 AND final_amount_cents >= 0");
                    table.CheckConstraint("ck_orders_discount", "discount_percent BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_orders_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_hosting_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "hosting_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plan_billing_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    billing_period = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    discount_percent = table.Column<int>(type: "int", nullable: false),
                    stripe_product_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    stripe_price_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    amount_cents = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_billing_prices", x => x.id);
                    table.CheckConstraint("ck_plan_billing_prices_amount", "amount_cents >= 0");
                    table.CheckConstraint("ck_plan_billing_prices_discount", "discount_percent BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_plan_billing_prices_period", "billing_period IN ('monthly', 'quarterly', 'annual')");
                    table.ForeignKey(
                        name: "FK_plan_billing_prices_hosting_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "hosting_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hosting_services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_profile_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    game_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    stripe_subscription_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    pterodactyl_server_id = table.Column<int>(type: "int", nullable: true),
                    pterodactyl_server_uuid = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    pterodactyl_node_id = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    activated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    suspended_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hosting_services", x => x.id);
                    table.ForeignKey(
                        name: "FK_hosting_services_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hosting_services_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hosting_services_hosting_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "hosting_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hosting_services_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "billing_invoice_refs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_profile_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    hosting_service_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    stripe_invoice_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    amount_due_cents = table.Column<long>(type: "bigint", nullable: false),
                    amount_paid_cents = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    invoice_date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    hosted_invoice_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    invoice_pdf_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    synced_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_billing_invoice_refs", x => x.id);
                    table.CheckConstraint("ck_billing_invoice_refs_amounts", "amount_due_cents >= 0 AND amount_paid_cents >= 0");
                    table.ForeignKey(
                        name: "FK_billing_invoice_refs_customer_profiles_customer_profile_id",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_billing_invoice_refs_hosting_services_hosting_service_id",
                        column: x => x.hosting_service_id,
                        principalTable: "hosting_services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "provisioning_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    hosting_service_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    selected_node_id = table.Column<int>(type: "int", nullable: true),
                    pterodactyl_server_id = table.Column<int>(type: "int", nullable: true),
                    pterodactyl_server_uuid = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    last_error = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provisioning_jobs", x => x.id);
                    table.CheckConstraint("ck_provisioning_jobs_attempts", "attempt_count >= 0");
                    table.ForeignKey(
                        name: "FK_provisioning_jobs_hosting_services_hosting_service_id",
                        column: x => x.hosting_service_id,
                        principalTable: "hosting_services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_provisioning_jobs_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "deployment_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provisioning_job_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    hosting_service_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    message = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    metadata_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deployment_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_deployment_events_hosting_services_hosting_service_id",
                        column: x => x.hosting_service_id,
                        principalTable: "hosting_services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_deployment_events_provisioning_jobs_provisioning_job_id",
                        column: x => x.provisioning_job_id,
                        principalTable: "provisioning_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_billing_invoice_refs_customer_profile_id_invoice_date",
                table: "billing_invoice_refs",
                columns: new[] { "customer_profile_id", "invoice_date" });

            migrationBuilder.CreateIndex(
                name: "IX_billing_invoice_refs_hosting_service_id",
                table: "billing_invoice_refs",
                column: "hosting_service_id");

            migrationBuilder.CreateIndex(
                name: "IX_billing_invoice_refs_stripe_invoice_id",
                table: "billing_invoice_refs",
                column: "stripe_invoice_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_profiles_hts_user_id",
                table: "customer_profiles",
                column: "hts_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_profiles_stripe_customer_id",
                table: "customer_profiles",
                column: "stripe_customer_id",
                unique: true,
                filter: "[stripe_customer_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_deployment_events_hosting_service_id",
                table: "deployment_events",
                column: "hosting_service_id");

            migrationBuilder.CreateIndex(
                name: "IX_deployment_events_provisioning_job_id_created_at",
                table: "deployment_events",
                columns: new[] { "provisioning_job_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_game_deployment_profiles_game_id",
                table: "game_deployment_profiles",
                column: "game_id",
                unique: true,
                filter: "[active] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_games_active_primary_game",
                table: "games",
                columns: new[] { "active", "primary_game" });

            migrationBuilder.CreateIndex(
                name: "IX_games_slug",
                table: "games",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hosting_nodes_enabled_maintenance_priority",
                table: "hosting_nodes",
                columns: new[] { "enabled", "maintenance", "priority" });

            migrationBuilder.CreateIndex(
                name: "IX_hosting_nodes_pterodactyl_node_id",
                table: "hosting_nodes",
                column: "pterodactyl_node_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hosting_plans_game_id_active_sort_order",
                table: "hosting_plans",
                columns: new[] { "game_id", "active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_hosting_plans_slug",
                table: "hosting_plans",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hosting_services_customer_profile_id_status",
                table: "hosting_services",
                columns: new[] { "customer_profile_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_hosting_services_game_id",
                table: "hosting_services",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "IX_hosting_services_order_id",
                table: "hosting_services",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hosting_services_plan_id",
                table: "hosting_services",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_hosting_services_pterodactyl_server_id",
                table: "hosting_services",
                column: "pterodactyl_server_id",
                unique: true,
                filter: "[pterodactyl_server_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_hosting_services_pterodactyl_server_uuid",
                table: "hosting_services",
                column: "pterodactyl_server_uuid",
                unique: true,
                filter: "[pterodactyl_server_uuid] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_hosting_services_stripe_subscription_id",
                table: "hosting_services",
                column: "stripe_subscription_id",
                unique: true,
                filter: "[stripe_subscription_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_orders_customer_profile_id",
                table: "orders",
                column: "customer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_game_id",
                table: "orders",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_plan_id",
                table: "orders",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_status",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_orders_stripe_checkout_session_id",
                table: "orders",
                column: "stripe_checkout_session_id",
                unique: true,
                filter: "[stripe_checkout_session_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_orders_stripe_subscription_id",
                table: "orders",
                column: "stripe_subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_billing_prices_plan_id_billing_period",
                table: "plan_billing_prices",
                columns: new[] { "plan_id", "billing_period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plan_billing_prices_stripe_price_id",
                table: "plan_billing_prices",
                column: "stripe_price_id",
                unique: true,
                filter: "[stripe_price_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_provisioning_jobs_hosting_service_id",
                table: "provisioning_jobs",
                column: "hosting_service_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provisioning_jobs_order_id",
                table: "provisioning_jobs",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provisioning_jobs_status_created_at",
                table: "provisioning_jobs",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_stripe_events_processed_received_at",
                table: "stripe_events",
                columns: new[] { "processed", "received_at" });

            migrationBuilder.CreateIndex(
                name: "IX_stripe_events_stripe_event_id",
                table: "stripe_events",
                column: "stripe_event_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "billing_invoice_refs");

            migrationBuilder.DropTable(
                name: "deployment_events");

            migrationBuilder.DropTable(
                name: "game_deployment_profiles");

            migrationBuilder.DropTable(
                name: "hosting_nodes");

            migrationBuilder.DropTable(
                name: "plan_billing_prices");

            migrationBuilder.DropTable(
                name: "stripe_events");

            migrationBuilder.DropTable(
                name: "provisioning_jobs");

            migrationBuilder.DropTable(
                name: "hosting_services");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "customer_profiles");

            migrationBuilder.DropTable(
                name: "hosting_plans");

            migrationBuilder.DropTable(
                name: "games");
        }
    }
}
