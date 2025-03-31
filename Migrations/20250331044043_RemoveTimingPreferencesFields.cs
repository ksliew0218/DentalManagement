using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalManagement.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTimingPreferencesFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SmsSent",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "SmsSentAt",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "Want24HourReminder",
                table: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "Want48HourReminder",
                table: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "WantWeekReminder",
                table: "UserNotificationPreferences");

            migrationBuilder.RenameColumn(
                name: "SentBySMS",
                table: "AppointmentReminders",
                newName: "SentByEmail");

            migrationBuilder.AlterColumn<string>(
                name: "ActionName",
                table: "UserNotifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ActionController",
                table: "UserNotifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SentByEmail",
                table: "AppointmentReminders",
                newName: "SentBySMS");

            migrationBuilder.AlterColumn<string>(
                name: "ActionName",
                table: "UserNotifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ActionController",
                table: "UserNotifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "SmsSent",
                table: "UserNotifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SmsSentAt",
                table: "UserNotifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Want24HourReminder",
                table: "UserNotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Want48HourReminder",
                table: "UserNotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WantWeekReminder",
                table: "UserNotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
