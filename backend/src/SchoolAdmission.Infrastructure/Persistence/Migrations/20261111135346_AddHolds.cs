using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAdmission.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHolds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "holds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<string>(type: "varchar(17)", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_holds", x => x.id);
                    table.CheckConstraint("ck_holds_status", "status IN ('HELD','CONFIRMED','EXPIRED','CANCELLED')");
                    table.CheckConstraint("ck_holds_student_id_digits", "student_id ~ '^[0-9]{17}$'");
                    table.ForeignKey(
                        name: "fk_holds_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_holds_class_status",
                table: "holds",
                columns: new[] { "class_id", "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_holds_student_created",
                table: "holds",
                columns: new[] { "student_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_holds_one_active_per_student",
                table: "holds",
                column: "student_id",
                unique: true,
                filter: "status IN ('HELD','CONFIRMED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "holds");
        }
    }
}
