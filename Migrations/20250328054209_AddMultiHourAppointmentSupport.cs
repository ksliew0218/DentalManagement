using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiHourAppointmentSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppointmentId",
                table: "TimeSlots",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Appointments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "Appointments",
                type: "integer",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_AppointmentId",
                table: "TimeSlots",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeSlots_Appointments_AppointmentId",
                table: "TimeSlots",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeSlots_Appointments_AppointmentId",
                table: "TimeSlots");

            migrationBuilder.DropIndex(
                name: "IX_TimeSlots_AppointmentId",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Appointments");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Appointments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
