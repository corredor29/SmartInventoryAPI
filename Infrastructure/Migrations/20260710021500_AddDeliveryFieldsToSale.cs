using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryFieldsToSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "delivery_address",
                table: "sale",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "delivery_lat",
                table: "sale",
                type: "numeric(10,7)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "delivery_lng",
                table: "sale",
                type: "numeric(10,7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_phone",
                table: "sale",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_document",
                table: "sale",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "delivery_address", table: "sale");
            migrationBuilder.DropColumn(name: "delivery_lat", table: "sale");
            migrationBuilder.DropColumn(name: "delivery_lng", table: "sale");
            migrationBuilder.DropColumn(name: "contact_phone", table: "sale");
            migrationBuilder.DropColumn(name: "contact_document", table: "sale");
        }
    }
}
