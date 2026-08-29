using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlatformOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReservationId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Ingredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    CostPerUnit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AllergenCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ingredients_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderBillingDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    CustomerType = table.Column<int>(type: "int", nullable: false),
                    InvoiceStatus = table.Column<int>(type: "int", nullable: false),
                    ReceiptEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PersonName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TaxId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    BillingAddressLine1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    BillingAddressLine2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    BillingCity = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    BillingPostalCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    BillingCountry = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    InvoiceIssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvoiceSentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderBillingDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderBillingDetails_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    AuthorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AuthorRole = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsCustomerVisible = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderComments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderTypeOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTypeOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethodOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethodOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    RestaurantTableId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GuestEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    GuestPhone = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PartySize = table.Column<int>(type: "int", nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_RestaurantTables_RestaurantTableId",
                        column: x => x.RestaurantTableId,
                        principalTable: "RestaurantTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IngredientTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IngredientId = table.Column<int>(type: "int", nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngredientTranslations_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuItemId = table.Column<int>(type: "int", nullable: false),
                    IngredientId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemIngredients_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuItemIngredients_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderTypeOptionTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderTypeOptionId = table.Column<int>(type: "int", nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTypeOptionTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderTypeOptionTranslations_OrderTypeOptions_OrderTypeOptionId",
                        column: x => x.OrderTypeOptionId,
                        principalTable: "OrderTypeOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethodOptionTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentMethodOptionId = table.Column<int>(type: "int", nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethodOptionTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentMethodOptionTranslations_PaymentMethodOptions_PaymentMethodOptionId",
                        column: x => x.PaymentMethodOptionId,
                        principalTable: "PaymentMethodOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ReservationId",
                table: "Orders",
                column: "ReservationId");

            migrationBuilder.Sql("""
                INSERT INTO OrderTypeOptions (Value, Code, IsActive, SortOrder)
                SELECT v.Value, v.Code, 1, v.SortOrder
                FROM (VALUES
                    (0, 'table', 1),
                    (1, 'takeaway', 2),
                    (2, 'delivery', 3)
                ) v(Value, Code, SortOrder)
                WHERE NOT EXISTS (SELECT 1 FROM OrderTypeOptions o WHERE o.Value = v.Value);

                INSERT INTO OrderTypeOptionTranslations (OrderTypeOptionId, Culture, Name)
                SELECT o.Id, t.Culture, t.Name
                FROM OrderTypeOptions o
                JOIN (VALUES
                    ('table', 'pl-PL', N'Przy stoliku'),
                    ('table', 'en-US', N'Table'),
                    ('takeaway', 'pl-PL', N'Odbiór osobisty'),
                    ('takeaway', 'en-US', N'Takeaway'),
                    ('delivery', 'pl-PL', N'Dostawa'),
                    ('delivery', 'en-US', N'Delivery')
                ) t(Code, Culture, Name) ON t.Code = o.Code
                WHERE NOT EXISTS (
                    SELECT 1 FROM OrderTypeOptionTranslations existing
                    WHERE existing.OrderTypeOptionId = o.Id AND existing.Culture = t.Culture
                );

                INSERT INTO PaymentMethodOptions (Value, Code, IsActive, SortOrder)
                SELECT v.Value, v.Code, 1, v.SortOrder
                FROM (VALUES
                    (0, 'in_app', 1),
                    (1, 'at_counter', 2)
                ) v(Value, Code, SortOrder)
                WHERE NOT EXISTS (SELECT 1 FROM PaymentMethodOptions o WHERE o.Value = v.Value);

                INSERT INTO PaymentMethodOptionTranslations (PaymentMethodOptionId, Culture, Name)
                SELECT o.Id, t.Culture, t.Name
                FROM PaymentMethodOptions o
                JOIN (VALUES
                    ('in_app', 'pl-PL', N'Płatność w aplikacji'),
                    ('in_app', 'en-US', N'Pay in app'),
                    ('at_counter', 'pl-PL', N'Płatność przy kasie'),
                    ('at_counter', 'en-US', N'Pay at counter')
                ) t(Code, Culture, Name) ON t.Code = o.Code
                WHERE NOT EXISTS (
                    SELECT 1 FROM PaymentMethodOptionTranslations existing
                    WHERE existing.PaymentMethodOptionId = o.Id AND existing.Culture = t.Culture
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_RestaurantId_IsActive",
                table: "Ingredients",
                columns: new[] { "RestaurantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_IngredientTranslations_IngredientId_Culture",
                table: "IngredientTranslations",
                columns: new[] { "IngredientId", "Culture" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemIngredients_IngredientId",
                table: "MenuItemIngredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemIngredients_MenuItemId_IngredientId",
                table: "MenuItemIngredients",
                columns: new[] { "MenuItemId", "IngredientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderBillingDetails_OrderId",
                table: "OrderBillingDetails",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderComments_OrderId_CreatedAt",
                table: "OrderComments",
                columns: new[] { "OrderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderTypeOptions_Code",
                table: "OrderTypeOptions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderTypeOptions_Value",
                table: "OrderTypeOptions",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderTypeOptionTranslations_OrderTypeOptionId_Culture",
                table: "OrderTypeOptionTranslations",
                columns: new[] { "OrderTypeOptionId", "Culture" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodOptions_Code",
                table: "PaymentMethodOptions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodOptions_Value",
                table: "PaymentMethodOptions",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodOptionTranslations_PaymentMethodOptionId_Culture",
                table: "PaymentMethodOptionTranslations",
                columns: new[] { "PaymentMethodOptionId", "Culture" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RestaurantId_StartAt_Status",
                table: "Reservations",
                columns: new[] { "RestaurantId", "StartAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RestaurantTableId",
                table: "Reservations",
                column: "RestaurantTableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Reservations_ReservationId",
                table: "Orders",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Reservations_ReservationId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "IngredientTranslations");

            migrationBuilder.DropTable(
                name: "MenuItemIngredients");

            migrationBuilder.DropTable(
                name: "OrderBillingDetails");

            migrationBuilder.DropTable(
                name: "OrderComments");

            migrationBuilder.DropTable(
                name: "OrderTypeOptionTranslations");

            migrationBuilder.DropTable(
                name: "PaymentMethodOptionTranslations");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "Ingredients");

            migrationBuilder.DropTable(
                name: "OrderTypeOptions");

            migrationBuilder.DropTable(
                name: "PaymentMethodOptions");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ReservationId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReservationId",
                table: "Orders");
        }
    }
}
