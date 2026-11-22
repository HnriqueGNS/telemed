using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace telemed.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EncryptSensitiveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CpfHash",
                table: "Patients",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ClinicId_CpfHash",
                table: "Patients",
                columns: new[] { "ClinicId", "CpfHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_ClinicId_CpfHash",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "CpfHash",
                table: "Patients");
        }
    }
}
