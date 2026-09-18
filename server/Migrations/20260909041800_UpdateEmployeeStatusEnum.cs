using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeHRMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmployeeStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEOAvqJxgzkTNBpAukh561BKj6eVDfMDMMascsZf6RnNZwHEAuq/BY4TzBQVnr7mtXA==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEE5bMxpZ1TfLjO9n/fhb9e4ztuSaOATmgll4dAkVxe+WUeDRcUckovMeYsdEgKWkLw==");
        }
    }
}
