using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foodprint.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMealFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MealFavorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    NameNormalized = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    PortionSize = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    PortionGrams = table.Column<int>(type: "INTEGER", nullable: true),
                    MealGroupId = table.Column<int>(type: "INTEGER", nullable: true),
                    TagsCsv = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealFavorites", x => x.Id);
                    table.CheckConstraint("CK_MealFavorite_Portion", "(\"PortionSize\" IS NULL OR \"PortionGrams\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_MealFavorites_MealGroups_MealGroupId",
                        column: x => x.MealGroupId,
                        principalTable: "MealGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MealFavorites_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MealFavorites_MealGroupId",
                table: "MealFavorites",
                column: "MealGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MealFavorites_UserId_NameNormalized_MealGroupId",
                table: "MealFavorites",
                columns: new[] { "UserId", "NameNormalized", "MealGroupId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MealFavorites");
        }
    }
}
