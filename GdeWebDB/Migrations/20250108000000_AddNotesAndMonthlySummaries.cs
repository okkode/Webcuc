using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeWebDB.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesAndMonthlySummaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "A_NOTE",
                columns: table => new
                {
                    NOTEID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    USERID = table.Column<int>(type: "INTEGER", nullable: false),
                    COURSEID = table.Column<int>(type: "INTEGER", nullable: false),
                    NOTETITLE = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NOTECONTENT = table.Column<string>(type: "TEXT", nullable: false),
                    CREATIONDATE = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MODIFICATIONDATE = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_A_NOTE", x => x.NOTEID);
                    table.ForeignKey(
                        name: "FK_A_NOTE_T_USER_USERID",
                        column: x => x.USERID,
                        principalTable: "T_USER",
                        principalColumn: "USERID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_A_NOTE_A_COURSE_COURSEID",
                        column: x => x.COURSEID,
                        principalTable: "A_COURSE",
                        principalColumn: "COURSEID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "A_MONTHLY_SUMMARY",
                columns: table => new
                {
                    SUMMARYID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    USERID = table.Column<int>(type: "INTEGER", nullable: false),
                    YEAR = table.Column<int>(type: "INTEGER", nullable: false),
                    MONTH = table.Column<int>(type: "INTEGER", nullable: false),
                    SUMMARY = table.Column<string>(type: "TEXT", nullable: false),
                    WHATLEARNED = table.Column<string>(type: "TEXT", nullable: false),
                    WHATPRESENTED = table.Column<string>(type: "TEXT", nullable: false),
                    CREATIONDATE = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MODIFICATIONDATE = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_A_MONTHLY_SUMMARY", x => x.SUMMARYID);
                    table.ForeignKey(
                        name: "FK_A_MONTHLY_SUMMARY_T_USER_USERID",
                        column: x => x.USERID,
                        principalTable: "T_USER",
                        principalColumn: "USERID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_A_NOTE_USERID_COURSEID",
                table: "A_NOTE",
                columns: new[] { "USERID", "COURSEID" });

            migrationBuilder.CreateIndex(
                name: "IX_A_MONTHLY_SUMMARY_USERID_YEAR_MONTH",
                table: "A_MONTHLY_SUMMARY",
                columns: new[] { "USERID", "YEAR", "MONTH" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "A_NOTE");

            migrationBuilder.DropTable(
                name: "A_MONTHLY_SUMMARY");
        }
    }
}

