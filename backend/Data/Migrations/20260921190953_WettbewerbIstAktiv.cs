using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BetQuote.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class WettbewerbIstAktiv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ist_aktiv",
                table: "wettbewerbe",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "wettbewerbe",
                keyColumn: "id",
                keyValue: 1,
                column: "ist_aktiv",
                value: true);

            migrationBuilder.UpdateData(
                table: "wettbewerbe",
                keyColumn: "id",
                keyValue: 2,
                column: "ist_aktiv",
                value: true);

            migrationBuilder.UpdateData(
                table: "wettbewerbe",
                keyColumn: "id",
                keyValue: 5,
                column: "ist_aktiv",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ist_aktiv",
                table: "wettbewerbe");
        }
    }
}
