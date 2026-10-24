using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BetQuote.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SpielFootballDataOrgMatchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "football_data_org_match_id",
                table: "spiele",
                type: "bigint",
                nullable: true);

            migrationBuilder.InsertData(
                table: "datenquellen",
                columns: new[] { "id", "anzeigename", "schluessel" },
                values: new object[] { 7, "football-data.org", "footballdataorg" });

            migrationBuilder.CreateIndex(
                name: "ix_spiele_football_data_org_match_id",
                table: "spiele",
                column: "football_data_org_match_id",
                unique: true,
                filter: "\"football_data_org_match_id\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_spiele_football_data_org_match_id",
                table: "spiele");

            migrationBuilder.DeleteData(
                table: "datenquellen",
                keyColumn: "id",
                keyValue: 7);

            migrationBuilder.DropColumn(
                name: "football_data_org_match_id",
                table: "spiele");
        }
    }
}
