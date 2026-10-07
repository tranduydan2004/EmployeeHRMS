using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EmployeeHRMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewAnswerAndInviteToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InviteToken",
                table: "Interviews",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.CreateTable(
                name: "InterviewAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InterviewId = table.Column<int>(type: "integer", nullable: false),
                    InterviewQuestionId = table.Column<int>(type: "integer", nullable: false),
                    AudioStorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AudioDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    AudioMimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AudioFileSize = table.Column<long>(type: "bigint", nullable: false),
                    TabSwitchCount = table.Column<int>(type: "integer", nullable: false),
                    OutOfFocusDurationSeconds = table.Column<double>(type: "double precision", nullable: false),
                    Transcript = table.Column<string>(type: "text", nullable: true),
                    TranscriptSegments = table.Column<string>(type: "jsonb", nullable: true),
                    AiScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    AiFeedback = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProcessingStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewAnswers_InterviewQuestions_InterviewQuestionId",
                        column: x => x.InterviewQuestionId,
                        principalTable: "InterviewQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewAnswers_Interviews_InterviewId",
                        column: x => x.InterviewId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_InviteToken",
                table: "Interviews",
                column: "InviteToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAnswers_InterviewId",
                table: "InterviewAnswers",
                column: "InterviewId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAnswers_InterviewQuestionId",
                table: "InterviewAnswers",
                column: "InterviewQuestionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewAnswers");

            migrationBuilder.DropIndex(
                name: "IX_Interviews_InviteToken",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "InviteToken",
                table: "Interviews");
        }
    }
}
