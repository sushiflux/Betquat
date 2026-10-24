using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BetQuote.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedWettbewerbe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "wettbewerbe",
                columns: new[] { "id", "ist_pokal", "kurzname", "name", "open_liga_db_kuerzel", "sortierung" },
                values: new object[,]
                {
                    { 1, false, "BL1", "1. Bundesliga", "bl1", 1 },
                    { 2, false, "BL2", "2. Bundesliga", "bl2", 2 },
                    { 3, false, "L3", "3. Liga", "bl3", 3 },
                    { 4, true, "DFB", "DFB-Pokal", "dfb", 4 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "wettbewerbe",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "wettbewerbe",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "wettbewerbe",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "wettbewerbe",
                keyColumn: "id",
                keyValue: 4);
        }
    }
}
