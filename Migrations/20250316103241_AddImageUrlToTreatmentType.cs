using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToTreatmentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "TreatmentTypes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "TreatmentTypes");
        }
    }
}
