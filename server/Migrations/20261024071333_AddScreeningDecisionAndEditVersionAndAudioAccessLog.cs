using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EmployeeHRMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScreeningDecisionAndEditVersionAndAudioAccessLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EditVersion",
                table: "InterviewAnswers",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "AudioAccessLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InterviewId = table.Column<int>(type: "integer", nullable: false),
                    InterviewAnswerId = table.Column<int>(type: "integer", nullable: true),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    UserRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioAccessLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AudioAccessLogs_InterviewAnswers_InterviewAnswerId",
                        column: x => x.InterviewAnswerId,
                        principalTable: "InterviewAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AudioAccessLogs_Interviews_InterviewId",
                        column: x => x.InterviewId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AudioAccessLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScreeningDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationId = table.Column<int>(type: "integer", nullable: false),
                    InterviewId = table.Column<int>(type: "integer", nullable: false),
                    DecidedByUserId = table.Column<int>(type: "integer", nullable: false),
                    DecidedByRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecisionType = table.Column<int>(type: "integer", nullable: false),
                    SystemRecommendation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SystemOverallScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ScoredQuestionCount = table.Column<int>(type: "integer", nullable: false),
                    TotalQuestionCount = table.Column<int>(type: "integer", nullable: false),
                    OverrideReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EvaluationSnapshot = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScreeningDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScreeningDecisions_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScreeningDecisions_Interviews_InterviewId",
                        column: x => x.InterviewId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScreeningDecisions_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AudioAccessLogs_AccessedAt",
                table: "AudioAccessLogs",
                column: "AccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AudioAccessLogs_InterviewAnswerId",
                table: "AudioAccessLogs",
                column: "InterviewAnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_AudioAccessLogs_InterviewId",
                table: "AudioAccessLogs",
                column: "InterviewId");

            migrationBuilder.CreateIndex(
                name: "IX_AudioAccessLogs_UserId",
                table: "AudioAccessLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningDecisions_ApplicationId",
                table: "ScreeningDecisions",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningDecisions_DecidedAt",
                table: "ScreeningDecisions",
                column: "DecidedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningDecisions_DecidedByUserId",
                table: "ScreeningDecisions",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningDecisions_InterviewId",
                table: "ScreeningDecisions",
                column: "InterviewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AudioAccessLogs");

            migrationBuilder.DropTable(
                name: "ScreeningDecisions");

            migrationBuilder.DropColumn(
                name: "EditVersion",
                table: "InterviewAnswers");
        }
    }
}
