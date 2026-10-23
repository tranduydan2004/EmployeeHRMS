using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EmployeeHRMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAiScoringAndQuestionEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Recommendation",
                table: "Interviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "InterviewQuestions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetectedLanguage",
                table: "InterviewAnswers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditedTranscript",
                table: "InterviewAnswers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedStage",
                table: "InterviewAnswers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEditedAt",
                table: "InterviewAnswers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastEditedByUserId",
                table: "InterviewAnswers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpeechMetrics",
                table: "InterviewAnswers",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SttDurationSeconds",
                table: "InterviewAnswers",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuestionEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InterviewAnswerId = table.Column<int>(type: "integer", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    TranscriptSnapshot = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: true),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Analysis = table.Column<string>(type: "text", nullable: true),
                    EvidenceQuotes = table.Column<List<string>>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    Strengths = table.Column<List<string>>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    Concerns = table.Column<List<string>>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EvaluatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ConfirmedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionEvaluations_InterviewAnswers_InterviewAnswerId",
                        column: x => x.InterviewAnswerId,
                        principalTable: "InterviewAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionEvaluations_InterviewAnswerId",
                table: "QuestionEvaluations",
                column: "InterviewAnswerId",
                unique: true,
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionEvaluations");

            migrationBuilder.DropColumn(
                name: "Recommendation",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "DetectedLanguage",
                table: "InterviewAnswers");

            migrationBuilder.DropColumn(
                name: "EditedTranscript",
                table: "InterviewAnswers");

            migrationBuilder.DropColumn(
                name: "FailedStage",
                table: "InterviewAnswers");

            migrationBuilder.DropColumn(
                name: "LastEditedAt",
                table: "InterviewAnswers");

            migrationBuilder.DropColumn(
                name: "LastEditedByUserId",
                table: "InterviewAnswers");

            migrationBuilder.DropColumn(
                name: "SpeechMetrics",
                table: "InterviewAnswers");

            migrationBuilder.DropColumn(
                name: "SttDurationSeconds",
                table: "InterviewAnswers");
        }
    }
}
