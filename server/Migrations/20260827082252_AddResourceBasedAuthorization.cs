using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeHRMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceBasedAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Candidates_CandidateId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CandidateId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CandidateId",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "InterviewerId",
                table: "Interviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Candidates",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHwxk+HrWGlpmWoMCADzIFqMa7hkZeDzeT27OoQGzxbdeoIgpsJTN5t2EO0tDiBbRw==");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_InterviewerId",
                table: "Interviews",
                column: "InterviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_UserId",
                table: "Candidates",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidates_Users_UserId",
                table: "Candidates",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Interviews_Users_InterviewerId",
                table: "Interviews",
                column: "InterviewerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Candidates_Users_UserId",
                table: "Candidates");

            migrationBuilder.DropForeignKey(
                name: "FK_Interviews_Users_InterviewerId",
                table: "Interviews");

            migrationBuilder.DropIndex(
                name: "IX_Interviews_InterviewerId",
                table: "Interviews");

            migrationBuilder.DropIndex(
                name: "IX_Candidates_UserId",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "InterviewerId",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Candidates");

            migrationBuilder.AddColumn<int>(
                name: "CandidateId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CandidateId", "PasswordHash" },
                values: new object[] { null, "AQAAAAIAAYagAAAAECOWoxl+3gX2DVXO5wn2pKd2tEMNJdbUGytVU/LrLmostK2cBSR1dZy0fNkQetM2gg==" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CandidateId",
                table: "Users",
                column: "CandidateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Candidates_CandidateId",
                table: "Users",
                column: "CandidateId",
                principalTable: "Candidates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
