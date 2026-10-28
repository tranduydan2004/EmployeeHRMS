using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeHRMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddServedAtAndActiveSessionNonce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActiveSessionNonce",
                table: "Interviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServedAt",
                table: "InterviewQuestions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveSessionNonce",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "ServedAt",
                table: "InterviewQuestions");
        }
    }
}
