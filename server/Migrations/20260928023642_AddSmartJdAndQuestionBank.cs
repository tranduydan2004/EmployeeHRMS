using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EmployeeHRMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartJdAndQuestionBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalNotes",
                table: "JobPostings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "JobPostings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "Certifications",
                table: "JobPostings",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");

            migrationBuilder.AddColumn<List<string>>(
                name: "CoreSkills",
                table: "JobPostings",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "JobPostings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JdContent",
                table: "JobPostings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "JobPostings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SalaryRange_Currency",
                table: "JobPostings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalaryRange_SalaryMax",
                table: "JobPostings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalaryRange_SalaryMin",
                table: "JobPostings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkMode",
                table: "JobPostings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "JobPostings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuestionBankItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobPostingId = table.Column<int>(type: "integer", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScoringRubric = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionBankItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionBankItems_JobPostings_JobPostingId",
                        column: x => x.JobPostingId,
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });


            migrationBuilder.CreateIndex(
                name: "IX_QuestionBankItems_JobPostingId",
                table: "QuestionBankItems",
                column: "JobPostingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionBankItems");

            migrationBuilder.DropColumn(
                name: "AdditionalNotes",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "Certifications",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "CoreSkills",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "JdContent",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "SalaryRange_Currency",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "SalaryRange_SalaryMax",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "SalaryRange_SalaryMin",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "WorkMode",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "JobPostings");

        }
    }
}
