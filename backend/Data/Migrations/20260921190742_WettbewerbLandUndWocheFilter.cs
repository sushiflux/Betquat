using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BetQuote.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class WettbewerbLandUndWocheFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "land",
                table: "wettbewerbe",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "wettbewerbe",
                keyColumn: "id",
                keyValue: 1,
                column: "land",
                value: "Deutschland");

            migrationBuilder.UpdateData(
                table: "wettbewerbe",
                keyColumn: "id",
                keyValue: 2,
                column: "land",
                value: "Deutschland");

            migrationBuilder.UpdateData(
                table: "wettbewerbe",
                keyColumn: "id",
                keyValue: 5,
                column: "land",
                value: "Spanien");

            // Manuell ergaenzt (nicht von EF Core scaffoldet, da Id 3/4 nicht mehr Teil von
            // WettbewerbConfiguration.HasData sind): die historisch belassenen Wettbewerbe 3.
            // Liga/DFB-Pokal (siehe ScopeAenderungLaLigaUndKonfidenz-Migration - bewusst nicht
            // geloescht, um bereits importierte Spiele nicht zu verlieren) sollen nicht mit einem
            // leeren "land"-Wert dastehen, falls sie ueber irgendeinen kuenftigen Endpoint wieder
            // sichtbar werden.
            migrationBuilder.UpdateData(
                table: "wettbewerbe",
                keyColumn: "id",
                keyValue: 3,
                column: "land",
                value: "Deutschland");

            migrationBuilder.UpdateData(
                table: "wettbewerbe",
                keyColumn: "id",
                keyValue: 4,
                column: "land",
                value: "Deutschland");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "land",
                table: "wettbewerbe");
        }
    }
}
