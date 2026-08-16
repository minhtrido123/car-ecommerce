using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Categories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProductType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Specs = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Products_Users_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.Sql("""
                INSERT INTO "Products" ("Id", "ProductType", "SellerId", "CategoryId", "Price", "Quantity", "Status", "Description", "Specs", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy")
                SELECT "Id", 'Car', "SellerId", "CategoryId", "Price", 1, "Status", "Description", NULL, "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
                FROM "Cars";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Cars_Categories_CategoryId",
                table: "Cars");

            migrationBuilder.DropForeignKey(
                name: "FK_Cars_Users_SellerId",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_Cars_CategoryId",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_Cars_SellerId",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_cars_status",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Cars");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "Reviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Reviews\" SET \"ProductId\" = \"CarId\";");

            migrationBuilder.DropColumn(
                name: "CarId",
                table: "Reviews");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "Favorites",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Favorites\" SET \"ProductId\" = \"CarId\";");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Favorites",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "CarId",
                table: "Favorites");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Favorites",
                table: "Favorites",
                columns: new[] { "UserId", "ProductId" });

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "CartItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"CartItems\" SET \"ProductId\" = \"CarId\";");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CartItems",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "CarId",
                table: "CartItems");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CartItems",
                table: "CartItems",
                columns: new[] { "UserId", "ProductId" });

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "OrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"OrderItems\" SET \"ProductId\" = \"CarId\";");

            migrationBuilder.DropColumn(
                name: "CarId",
                table: "OrderItems");

            migrationBuilder.CreateTable(
                name: "Parts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parts_Products_Id",
                        column: x => x.Id,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO "ProductImages" ("Id", "ProductId", "Url", "IsPrimary", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy")
                SELECT "Id", "CarId", "Url", "IsPrimary", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
                FROM "CarImages";
                """);

            migrationBuilder.DropTable(
                name: "CarImages");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Type",
                table: "Categories",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Year",
                table: "Cars",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_Parts_Sku",
                table: "Parts",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductType",
                table: "Products",
                column: "ProductType");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SellerId",
                table: "Products",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_products_status",
                table: "Products",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Cars_Products_Id",
                table: "Cars",
                column: "Id",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cars_Products_Id",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_products_status",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_SellerId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductType",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_Parts_Sku",
                table: "Parts");

            migrationBuilder.DropIndex(
                name: "IX_Cars_Year",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Type",
                table: "Categories");

            migrationBuilder.CreateTable(
                name: "CarImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CarId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarImages_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "Parts");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Categories");

            migrationBuilder.AddColumn<Guid>(
                name: "CarId",
                table: "Reviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Reviews\" SET \"CarId\" = \"ProductId\";");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Reviews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Favorites",
                table: "Favorites");

            migrationBuilder.AddColumn<Guid>(
                name: "CarId",
                table: "Favorites",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Favorites\" SET \"CarId\" = \"ProductId\";");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Favorites");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Favorites",
                table: "Favorites",
                columns: new[] { "UserId", "CarId" });

            migrationBuilder.DropPrimaryKey(
                name: "PK_CartItems",
                table: "CartItems");

            migrationBuilder.AddColumn<Guid>(
                name: "CarId",
                table: "CartItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"CartItems\" SET \"CarId\" = \"ProductId\";");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CartItems");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CartItems",
                table: "CartItems",
                columns: new[] { "UserId", "CarId" });

            migrationBuilder.AddColumn<Guid>(
                name: "CarId",
                table: "OrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"OrderItems\" SET \"CarId\" = \"ProductId\";");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "OrderItems");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Cars",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Cars",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Cars",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Cars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Cars",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "SellerId",
                table: "Cars",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Cars",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Cars",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Cars",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cars_CategoryId",
                table: "Cars",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_SellerId",
                table: "Cars",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_cars_status",
                table: "Cars",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CarImages_CarId",
                table: "CarImages",
                column: "CarId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cars_Categories_CategoryId",
                table: "Cars",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cars_Users_SellerId",
                table: "Cars",
                column: "SellerId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
