using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace DentalManagement.Migrations
{
    public partial class AddAppointmentDocuments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentId = table.Column<int>(nullable: false),
                    DocumentName = table.Column<string>(maxLength: 200, nullable: false),
                    S3Key = table.Column<string>(maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(maxLength: 200, nullable: true),
                    FileSize = table.Column<long>(nullable: false),
                    UploadedDate = table.Column<DateTime>(nullable: false),
                    UploadedBy = table.Column<string>(maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentDocuments_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentDocuments_AppointmentId",
                table: "AppointmentDocuments",
                column: "AppointmentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentDocuments");
        }
    }
} 