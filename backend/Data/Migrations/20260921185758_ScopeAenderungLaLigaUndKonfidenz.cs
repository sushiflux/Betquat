using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BetQuote.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ScopeAenderungLaLigaUndKonfidenz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // WICHTIG (manuell angepasst gegenueber dem scaffoldeten Stand): EF Core hat hier
            // urspruenglich DeleteData fuer Wettbewerb-Id 3 (3. Liga) und 4 (DFB-Pokal) generiert,
            // weil sie aus WettbewerbConfiguration.HasData entfernt wurden (Scope-Aenderung: nur
            // noch 1./2. Bundesliga + La Liga, siehe PREDICTION_FACTORS.md "Ziel-Wettbewerbe").
            // Beide Wettbewerbe haben zu diesem Zeitpunkt aber bereits importierte Spiele/Saisons
            // in der Datenbank (7052 bzw. 390 Spiele im Dev-Stand) - ein DELETE waere durch den
            // Restrict-Fremdschluessel auf spiele.wettbewerb_id ohnehin fehlgeschlagen, haette bei
            // einer zukuenftigen leeren DB aber unbemerkt historische Daten geloescht. Bewusst
            // NICHT geloescht (siehe Abschlussbericht "destruktive Aenderung, Rueckfrage an
            // Nutzer") - beide Zeilen bleiben unveraendert in der Tabelle stehen, sind einfach kein
            // Ziel mehr fuer neue Importlaeufe (siehe OpenLigaDbImportService/WettbewerbConfiguration).
            migrationBuilder.DropIndex(
                name: "ix_wettbewerbe_open_liga_db_kuerzel",
                table: "wettbewerbe");

            migrationBuilder.AlterColumn<string>(
                name: "open_liga_db_kuerzel",
                table: "wettbewerbe",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "football_data_org_code",
                table: "wettbewerbe",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ist_hohe_konfidenz",
                table: "vorhersagen",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "konfidenz_score",
                table: "vorhersagen",
                type: "numeric(6,5)",
                precision: 6,
                scale: 5,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "wettbewerbe",
                keyColumn: "id",
                keyValue: 1,
                column: "football_data_org_code",
                value: null);

            migrationBuilder.UpdateData(
                table: "wettbewerbe",
                keyColumn: "id",
                keyValue: 2,
                column: "football_data_org_code",
                value: null);

            migrationBuilder.InsertData(
                table: "wettbewerbe",
                columns: new[] { "id", "football_data_org_code", "ist_pokal", "kurzname", "name", "open_liga_db_kuerzel", "sortierung" },
                values: new object[] { 5, "PD", false, "LIGA", "La Liga", null, 3 });

            migrationBuilder.CreateIndex(
                name: "ix_wettbewerbe_football_data_org_code",
                table: "wettbewerbe",
                column: "football_data_org_code",
                unique: true,
                filter: "\"football_data_org_code\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_wettbewerbe_open_liga_db_kuerzel",
                table: "wettbewerbe",
                column: "open_liga_db_kuerzel",
                unique: true,
                filter: "\"open_liga_db_kuerzel\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_wettbewerbe_football_data_org_code",
                table: "wettbewerbe");

            migrationBuilder.DropIndex(
                name: "ix_wettbewerbe_open_liga_db_kuerzel",
                table: "wettbewerbe");

            migrationBuilder.DeleteData(
                table: "wettbewerbe",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "football_data_org_code",
                table: "wettbewerbe");

            migrationBuilder.DropColumn(
                name: "ist_hohe_konfidenz",
                table: "vorhersagen");

            migrationBuilder.DropColumn(
                name: "konfidenz_score",
                table: "vorhersagen");

            // Kein AlterColumn zurueck auf NOT NULL und kein re-InsertData fuer Id 3/4: beide
            // Zeilen wurden im Up() bewusst nie geloescht (siehe dortiger Kommentar), ein Downgrade
            // muesste sie also auch nicht wiederherstellen. Ein AlterColumn auf NOT NULL wuerde
            // hier zudem an der (nie geloeschten) 3./4.-Liga-Zeile ohnehin nichts aendern, aber
            // faelschlich suggerieren, dass "open_liga_db_kuerzel" wieder Pflichtfeld ist, obwohl
            // dieses Downgrade selbst keine 5. Zeile mit NULL mehr entfernt hat, falls die
            // La-Liga-Zeile (Id 5) inzwischen erneut angelegt wurde - deshalb entfaellt der
            // urspruenglich scaffoldete AlterColumn-Schritt hier bewusst.
            migrationBuilder.CreateIndex(
                name: "ix_wettbewerbe_open_liga_db_kuerzel",
                table: "wettbewerbe",
                column: "open_liga_db_kuerzel",
                unique: true,
                filter: "\"open_liga_db_kuerzel\" IS NOT NULL");
        }
    }
}
