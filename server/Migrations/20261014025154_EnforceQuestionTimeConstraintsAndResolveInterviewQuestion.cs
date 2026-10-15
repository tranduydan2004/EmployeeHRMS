using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeHRMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class EnforceQuestionTimeConstraintsAndResolveInterviewQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_QuestionBankItems_ThinkSeconds",
                table: "QuestionBankItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InterviewQuestions_AnswerSeconds",
                table: "InterviewQuestions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InterviewQuestions_ThinkSeconds",
                table: "InterviewQuestions");

            migrationBuilder.AlterColumn<int>(
                name: "ThinkSeconds",
                table: "InterviewQuestions",
                type: "integer",
                nullable: false,
                defaultValue: 20,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AnswerSeconds",
                table: "InterviewQuestions",
                type: "integer",
                nullable: false,
                defaultValue: 90,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuestionBankItems_ThinkSeconds",
                table: "QuestionBankItems",
                sql: "\"ThinkSeconds\" IS NULL OR \"ThinkSeconds\" BETWEEN 5 AND 40");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InterviewQuestions_AnswerSeconds",
                table: "InterviewQuestions",
                sql: "\"AnswerSeconds\" BETWEEN 10 AND 120");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InterviewQuestions_ThinkSeconds",
                table: "InterviewQuestions",
                sql: "\"ThinkSeconds\" BETWEEN 5 AND 40");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_QuestionBankItems_ThinkSeconds",
                table: "QuestionBankItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InterviewQuestions_AnswerSeconds",
                table: "InterviewQuestions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InterviewQuestions_ThinkSeconds",
                table: "InterviewQuestions");

            migrationBuilder.AlterColumn<int>(
                name: "ThinkSeconds",
                table: "InterviewQuestions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 20);

            migrationBuilder.AlterColumn<int>(
                name: "AnswerSeconds",
                table: "InterviewQuestions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 90);

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
    }
}
