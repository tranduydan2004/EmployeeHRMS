using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeHRMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioInterviewInviteFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnswerSeconds",
                table: "QuestionBankItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ThinkSeconds",
                table: "QuestionBankItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentedAt",
                table: "Interviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InviteExpiresAt",
                table: "Interviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnswerSeconds",
                table: "InterviewQuestions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScoringRubric",
                table: "InterviewQuestions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ThinkSeconds",
                table: "InterviewQuestions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "Flags",
                table: "InterviewAnswers",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuestionBankItems_AnswerSeconds",
                table: "QuestionBankItems",
                sql: "\"AnswerSeconds\" IS NULL OR \"AnswerSeconds\" BETWEEN 10 AND 120");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuestionBankItems_ThinkSeconds",
                table: "QuestionBankItems",
                sql: "\"ThinkSeconds\" IS NULL OR \"ThinkSeconds\" BETWEEN 5 AND 300");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InterviewQuestions_AnswerSeconds",
                table: "InterviewQuestions",
                sql: "\"AnswerSeconds\" IS NULL OR \"AnswerSeconds\" BETWEEN 10 AND 120");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InterviewQuestions_ThinkSeconds",
                table: "InterviewQuestions",
                sql: "\"ThinkSeconds\" IS NULL OR \"ThinkSeconds\" BETWEEN 5 AND 300");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_QuestionBankItems_AnswerSeconds",
                table: "QuestionBankItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuestionBankItems_ThinkSeconds",
                table: "QuestionBankItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InterviewQuestions_AnswerSeconds",
                table: "InterviewQuestions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InterviewQuestions_ThinkSeconds",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "AnswerSeconds",
                table: "QuestionBankItems");

            migrationBuilder.DropColumn(
                name: "ThinkSeconds",
                table: "QuestionBankItems");

            migrationBuilder.DropColumn(
                name: "ConsentedAt",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "InviteExpiresAt",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "AnswerSeconds",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "ScoringRubric",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "ThinkSeconds",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "Flags",
                table: "InterviewAnswers");
        }
    }
}
