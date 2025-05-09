﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedToTreatmentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TreatmentTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TreatmentTypes");
        }
    }
}
