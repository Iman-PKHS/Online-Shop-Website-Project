using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineShop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueCartAndWishlistLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WishlistItems_WishlistId",
                table: "WishlistItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_WishlistId_ItemId",
                table: "WishlistItems",
                columns: new[] { "WishlistId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ItemId",
                table: "CartItems",
                columns: new[] { "CartId", "ItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WishlistItems_WishlistId_ItemId",
                table: "WishlistItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_ItemId",
                table: "CartItems");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_WishlistId",
                table: "WishlistItems",
                column: "WishlistId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");
        }
    }
}
