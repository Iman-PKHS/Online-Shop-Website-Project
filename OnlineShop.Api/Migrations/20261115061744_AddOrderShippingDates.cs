using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineShop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderShippingDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAt",
                table: "Orders",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "Orders");
        }
    }
}
