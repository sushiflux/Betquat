using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BetQuote.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "datenquellen",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    schluessel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    anzeigename = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_datenquellen", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "schiedsrichter",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    open_liga_db_schiedsrichter_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_schiedsrichter", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    kurzname = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    open_liga_db_team_id = table.Column<int>(type: "integer", nullable: true),
                    standort_stadt = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    standort_breitengrad = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    standort_laengengrad = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_teams", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wettbewerbe",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    kurzname = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    open_liga_db_kuerzel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ist_pokal = table.Column<bool>(type: "boolean", nullable: false),
                    sortierung = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wettbewerbe", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "spieler",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    team_id = table.Column<int>(type: "integer", nullable: true),
                    position = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_spieler", x => x.id);
                    table.ForeignKey(
                        name: "fk_spieler_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "team_aliase",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    team_id = table.Column<int>(type: "integer", nullable: false),
                    datenquelle_id = table.Column<int>(type: "integer", nullable: false),
                    alias_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    externe_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_team_aliase", x => x.id);
                    table.ForeignKey(
                        name: "fk_team_aliase_datenquellen_datenquelle_id",
                        column: x => x.datenquelle_id,
                        principalTable: "datenquellen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_team_aliase_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saisons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    wettbewerb_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    startjahr = table.Column<int>(type: "integer", nullable: false),
                    open_liga_db_saison_kuerzel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ist_aktuell = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saisons", x => x.id);
                    table.ForeignKey(
                        name: "fk_saisons_wettbewerbe_wettbewerb_id",
                        column: x => x.wettbewerb_id,
                        principalTable: "wettbewerbe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "schiedsrichter_statistiken",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    schiedsrichter_id = table.Column<int>(type: "integer", nullable: false),
                    wettbewerb_id = table.Column<int>(type: "integer", nullable: false),
                    saison_id = table.Column<int>(type: "integer", nullable: false),
                    anzahl_spiele = table.Column<int>(type: "integer", nullable: false),
                    gelbe_karten_schnitt = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    gelb_rote_karten_schnitt = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    rote_karten_schnitt = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    elfmeter_schnitt = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    berechnet_am = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_schiedsrichter_statistiken", x => x.id);
                    table.ForeignKey(
                        name: "fk_schiedsrichter_statistiken_saisons_saison_id",
                        column: x => x.saison_id,
                        principalTable: "saisons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_schiedsrichter_statistiken_schiedsrichter_schiedsrichter_id",
                        column: x => x.schiedsrichter_id,
                        principalTable: "schiedsrichter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_schiedsrichter_statistiken_wettbewerbe_wettbewerb_id",
                        column: x => x.wettbewerb_id,
                        principalTable: "wettbewerbe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "spiele",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    wettbewerb_id = table.Column<int>(type: "integer", nullable: false),
                    saison_id = table.Column<int>(type: "integer", nullable: false),
                    spieltag_nummer = table.Column<int>(type: "integer", nullable: false),
                    spieltag_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    kw_jahr = table.Column<int>(type: "integer", nullable: false),
                    kw_nummer = table.Column<int>(type: "integer", nullable: false),
                    heim_team_id = table.Column<int>(type: "integer", nullable: false),
                    auswaerts_team_id = table.Column<int>(type: "integer", nullable: false),
                    anstoss = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    live_minute = table.Column<int>(type: "integer", nullable: true),
                    tore_heim = table.Column<int>(type: "integer", nullable: true),
                    tore_auswaerts = table.Column<int>(type: "integer", nullable: true),
                    halbzeit_tore_heim = table.Column<int>(type: "integer", nullable: true),
                    halbzeit_tore_auswaerts = table.Column<int>(type: "integer", nullable: true),
                    schiedsrichter_id = table.Column<int>(type: "integer", nullable: true),
                    open_liga_db_match_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_spiele", x => x.id);
                    table.CheckConstraint("ck_spiel_heim_ungleich_auswaerts", "\"heim_team_id\" <> \"auswaerts_team_id\"");
                    table.CheckConstraint("ck_spiel_tore_konsistent", "((\"tore_heim\" IS NULL AND \"tore_auswaerts\" IS NULL) OR (\"tore_heim\" IS NOT NULL AND \"tore_auswaerts\" IS NOT NULL)) AND (\"tore_heim\" IS NULL OR \"tore_heim\" >= 0) AND (\"tore_auswaerts\" IS NULL OR \"tore_auswaerts\" >= 0)");
                    table.ForeignKey(
                        name: "fk_spiele_saisons_saison_id",
                        column: x => x.saison_id,
                        principalTable: "saisons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_spiele_schiedsrichter_schiedsrichter_id",
                        column: x => x.schiedsrichter_id,
                        principalTable: "schiedsrichter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_spiele_teams_auswaerts_team_id",
                        column: x => x.auswaerts_team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_spiele_teams_heim_team_id",
                        column: x => x.heim_team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_spiele_wettbewerbe_wettbewerb_id",
                        column: x => x.wettbewerb_id,
                        principalTable: "wettbewerbe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trefferquote_eintraege",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    wettbewerb_id = table.Column<int>(type: "integer", nullable: false),
                    ebene = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    saison_id = table.Column<int>(type: "integer", nullable: true),
                    spieltag_nummer = table.Column<int>(type: "integer", nullable: true),
                    anzahl_spiele = table.Column<int>(type: "integer", nullable: false),
                    anzahl_exakte_treffer = table.Column<int>(type: "integer", nullable: false),
                    exakte_trefferquote = table.Column<decimal>(type: "numeric(6,5)", precision: 6, scale: 5, nullable: false),
                    anzahl_tendenz_treffer = table.Column<int>(type: "integer", nullable: false),
                    tendenz_trefferquote = table.Column<decimal>(type: "numeric(6,5)", precision: 6, scale: 5, nullable: false),
                    berechnet_am = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trefferquote_eintraege", x => x.id);
                    table.ForeignKey(
                        name: "fk_trefferquote_eintraege_saisons_saison_id",
                        column: x => x.saison_id,
                        principalTable: "saisons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_trefferquote_eintraege_wettbewerbe_wettbewerb_id",
                        column: x => x.wettbewerb_id,
                        principalTable: "wettbewerbe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "elo_rating_historie",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    team_id = table.Column<int>(type: "integer", nullable: false),
                    spiel_id = table.Column<int>(type: "integer", nullable: false),
                    elo_vor_spiel = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    elo_nach_spiel = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    spiel_zeitpunkt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_elo_rating_historie", x => x.id);
                    table.ForeignKey(
                        name: "fk_elo_rating_historie_spiele_spiel_id",
                        column: x => x.spiel_id,
                        principalTable: "spiele",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_elo_rating_historie_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "radio_links",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    spiel_id = table.Column<int>(type: "integer", nullable: false),
                    einzelspiel_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    konferenz_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ermittelt_am = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_radio_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_radio_links_spiele_spiel_id",
                        column: x => x.spiel_id,
                        principalTable: "spiele",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "spieler_verletzungen",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    spieler_id = table.Column<int>(type: "integer", nullable: false),
                    art = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    grund = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    von = table.Column<DateOnly>(type: "date", nullable: false),
                    bis = table.Column<DateOnly>(type: "date", nullable: true),
                    ausloesendes_spiel_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_spieler_verletzungen", x => x.id);
                    table.CheckConstraint("ck_verletzung_zeitraum", "\"bis\" IS NULL OR \"bis\" >= \"von\"");
                    table.ForeignKey(
                        name: "fk_spieler_verletzungen_spiele_ausloesendes_spiel_id",
                        column: x => x.ausloesendes_spiel_id,
                        principalTable: "spiele",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_spieler_verletzungen_spieler_spieler_id",
                        column: x => x.spieler_id,
                        principalTable: "spieler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tor_events",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    spiel_id = table.Column<int>(type: "integer", nullable: false),
                    team_id = table.Column<int>(type: "integer", nullable: false),
                    spieler_id = table.Column<int>(type: "integer", nullable: true),
                    spieler_name_freitext = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    minute = table.Column<int>(type: "integer", nullable: false),
                    ist_elfmeter = table.Column<bool>(type: "boolean", nullable: false),
                    ist_eigentor = table.Column<bool>(type: "boolean", nullable: false),
                    tore_heim_nach_event = table.Column<int>(type: "integer", nullable: false),
                    tore_auswaerts_nach_event = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tor_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_tor_events_spiele_spiel_id",
                        column: x => x.spiel_id,
                        principalTable: "spiele",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tor_events_spieler_spieler_id",
                        column: x => x.spieler_id,
                        principalTable: "spieler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tor_events_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vorhersagen",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    spiel_id = table.Column<int>(type: "integer", nullable: false),
                    berechnet_am = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modell_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipp_tore_heim = table.Column<int>(type: "integer", nullable: false),
                    tipp_tore_auswaerts = table.Column<int>(type: "integer", nullable: false),
                    tipp_wahrscheinlichkeit = table.Column<decimal>(type: "numeric(6,5)", precision: 6, scale: 5, nullable: false),
                    wahrscheinlichkeit_heimsieg = table.Column<decimal>(type: "numeric(6,5)", precision: 6, scale: 5, nullable: false),
                    wahrscheinlichkeit_remis = table.Column<decimal>(type: "numeric(6,5)", precision: 6, scale: 5, nullable: false),
                    wahrscheinlichkeit_auswaertssieg = table.Column<decimal>(type: "numeric(6,5)", precision: 6, scale: 5, nullable: false),
                    wahrscheinlichkeit_ueber25 = table.Column<decimal>(type: "numeric(6,5)", precision: 6, scale: 5, nullable: true),
                    wahrscheinlichkeit_beide_treffen = table.Column<decimal>(type: "numeric(6,5)", precision: 6, scale: 5, nullable: true),
                    ergebnisverteilung = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    faktoren_snapshot = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    ist_aktuell = table.Column<bool>(type: "boolean", nullable: false),
                    traf_exakt = table.Column<bool>(type: "boolean", nullable: true),
                    traf_tendenz = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vorhersagen", x => x.id);
                    table.ForeignKey(
                        name: "fk_vorhersagen_spiele_spiel_id",
                        column: x => x.spiel_id,
                        principalTable: "spiele",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "xg_statistiken",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    spiel_id = table.Column<int>(type: "integer", nullable: false),
                    team_id = table.Column<int>(type: "integer", nullable: false),
                    ist_heimteam = table.Column<bool>(type: "boolean", nullable: false),
                    datenquelle_id = table.Column<int>(type: "integer", nullable: false),
                    xg = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    xga = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    xgot = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    importiert_am = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_xg_statistiken", x => x.id);
                    table.ForeignKey(
                        name: "fk_xg_statistiken_datenquellen_datenquelle_id",
                        column: x => x.datenquelle_id,
                        principalTable: "datenquellen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_xg_statistiken_spiele_spiel_id",
                        column: x => x.spiel_id,
                        principalTable: "spiele",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_xg_statistiken_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "zeitsegment_statistiken",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    spiel_id = table.Column<int>(type: "integer", nullable: false),
                    team_id = table.Column<int>(type: "integer", nullable: false),
                    segment_start = table.Column<int>(type: "integer", nullable: false),
                    ballbesitz_prozent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    schuesse = table.Column<int>(type: "integer", nullable: true),
                    schuesse_aufs_tor = table.Column<int>(type: "integer", nullable: true),
                    gefaehrliche_angriffe = table.Column<int>(type: "integer", nullable: true),
                    datenquelle_id = table.Column<int>(type: "integer", nullable: false),
                    importiert_am = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_zeitsegment_statistiken", x => x.id);
                    table.CheckConstraint("ck_zeitsegment_start_gueltig", "\"segment_start\" IN (0, 15, 30, 45, 60, 75)");
                    table.ForeignKey(
                        name: "fk_zeitsegment_statistiken_datenquellen_datenquelle_id",
                        column: x => x.datenquelle_id,
                        principalTable: "datenquellen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_zeitsegment_statistiken_spiele_spiel_id",
                        column: x => x.spiel_id,
                        principalTable: "spiele",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_zeitsegment_statistiken_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "datenquellen",
                columns: new[] { "id", "anzeigename", "schluessel" },
                values: new object[,]
                {
                    { 1, "OpenLigaDB", "openligadb" },
                    { 2, "Kaggle AllBundesligaGamesEver", "kaggle" },
                    { 3, "Understat", "understat" },
                    { 4, "Sofascore", "sofascore" },
                    { 5, "FBref", "fbref" },
                    { 6, "ARD Sportschau", "sportschau" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_datenquellen_schluessel",
                table: "datenquellen",
                column: "schluessel",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_elo_rating_historie_spiel_id",
                table: "elo_rating_historie",
                column: "spiel_id");

            migrationBuilder.CreateIndex(
                name: "ix_elo_rating_historie_team_id_spiel_id",
                table: "elo_rating_historie",
                columns: new[] { "team_id", "spiel_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_elo_rating_historie_team_id_spiel_zeitpunkt",
                table: "elo_rating_historie",
                columns: new[] { "team_id", "spiel_zeitpunkt" });

            migrationBuilder.CreateIndex(
                name: "ix_radio_links_spiel_id",
                table: "radio_links",
                column: "spiel_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saisons_wettbewerb_id_ist_aktuell",
                table: "saisons",
                columns: new[] { "wettbewerb_id", "ist_aktuell" });

            migrationBuilder.CreateIndex(
                name: "ix_saisons_wettbewerb_id_name",
                table: "saisons",
                columns: new[] { "wettbewerb_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_schiedsrichter_name",
                table: "schiedsrichter",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_schiedsrichter_open_liga_db_schiedsrichter_id",
                table: "schiedsrichter",
                column: "open_liga_db_schiedsrichter_id",
                unique: true,
                filter: "\"open_liga_db_schiedsrichter_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_schiedsrichter_statistiken_saison_id",
                table: "schiedsrichter_statistiken",
                column: "saison_id");

            migrationBuilder.CreateIndex(
                name: "ix_schiedsrichter_statistiken_schiedsrichter_id_wettbewerb_id_",
                table: "schiedsrichter_statistiken",
                columns: new[] { "schiedsrichter_id", "wettbewerb_id", "saison_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_schiedsrichter_statistiken_wettbewerb_id",
                table: "schiedsrichter_statistiken",
                column: "wettbewerb_id");

            migrationBuilder.CreateIndex(
                name: "ix_spiele_auswaerts_team_id",
                table: "spiele",
                column: "auswaerts_team_id");

            migrationBuilder.CreateIndex(
                name: "ix_spiele_heim_team_id",
                table: "spiele",
                column: "heim_team_id");

            migrationBuilder.CreateIndex(
                name: "ix_spiele_kw_jahr_kw_nummer_anstoss",
                table: "spiele",
                columns: new[] { "kw_jahr", "kw_nummer", "anstoss" });

            migrationBuilder.CreateIndex(
                name: "ix_spiele_open_liga_db_match_id",
                table: "spiele",
                column: "open_liga_db_match_id",
                unique: true,
                filter: "\"open_liga_db_match_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_spiele_saison_id",
                table: "spiele",
                column: "saison_id");

            migrationBuilder.CreateIndex(
                name: "ix_spiele_schiedsrichter_id",
                table: "spiele",
                column: "schiedsrichter_id");

            migrationBuilder.CreateIndex(
                name: "ix_spiele_status",
                table: "spiele",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_spiele_wettbewerb_id_saison_id_spieltag_nummer",
                table: "spiele",
                columns: new[] { "wettbewerb_id", "saison_id", "spieltag_nummer" });

            migrationBuilder.CreateIndex(
                name: "ix_spieler_team_id_name",
                table: "spieler",
                columns: new[] { "team_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_spieler_verletzungen_ausloesendes_spiel_id",
                table: "spieler_verletzungen",
                column: "ausloesendes_spiel_id");

            migrationBuilder.CreateIndex(
                name: "ix_spieler_verletzungen_spieler_id_von_bis",
                table: "spieler_verletzungen",
                columns: new[] { "spieler_id", "von", "bis" });

            migrationBuilder.CreateIndex(
                name: "ix_team_aliase_datenquelle_id_alias_name",
                table: "team_aliase",
                columns: new[] { "datenquelle_id", "alias_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_team_aliase_team_id",
                table: "team_aliase",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_teams_name",
                table: "teams",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_teams_open_liga_db_team_id",
                table: "teams",
                column: "open_liga_db_team_id",
                unique: true,
                filter: "\"open_liga_db_team_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tor_events_spiel_id_minute",
                table: "tor_events",
                columns: new[] { "spiel_id", "minute" });

            migrationBuilder.CreateIndex(
                name: "ix_tor_events_spieler_id",
                table: "tor_events",
                column: "spieler_id");

            migrationBuilder.CreateIndex(
                name: "ix_tor_events_team_id_minute",
                table: "tor_events",
                columns: new[] { "team_id", "minute" });

            migrationBuilder.CreateIndex(
                name: "ix_trefferquote_eintraege_saison_id",
                table: "trefferquote_eintraege",
                column: "saison_id");

            migrationBuilder.CreateIndex(
                name: "ix_trefferquote_eintraege_wettbewerb_id_ebene",
                table: "trefferquote_eintraege",
                columns: new[] { "wettbewerb_id", "ebene" });

            migrationBuilder.CreateIndex(
                name: "ix_trefferquote_eintraege_wettbewerb_id_ebene_saison_id_spielt",
                table: "trefferquote_eintraege",
                columns: new[] { "wettbewerb_id", "ebene", "saison_id", "spieltag_nummer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vorhersagen_ist_aktuell_traf_exakt_traf_tendenz",
                table: "vorhersagen",
                columns: new[] { "ist_aktuell", "traf_exakt", "traf_tendenz" });

            migrationBuilder.CreateIndex(
                name: "ix_vorhersagen_spiel_id_aktuell",
                table: "vorhersagen",
                column: "spiel_id",
                unique: true,
                filter: "\"ist_aktuell\" = true");

            migrationBuilder.CreateIndex(
                name: "ix_vorhersagen_spiel_id_berechnet_am",
                table: "vorhersagen",
                columns: new[] { "spiel_id", "berechnet_am" });

            migrationBuilder.CreateIndex(
                name: "ix_wettbewerbe_name",
                table: "wettbewerbe",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wettbewerbe_open_liga_db_kuerzel",
                table: "wettbewerbe",
                column: "open_liga_db_kuerzel",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_xg_statistiken_datenquelle_id",
                table: "xg_statistiken",
                column: "datenquelle_id");

            migrationBuilder.CreateIndex(
                name: "ix_xg_statistiken_spiel_id_team_id_datenquelle_id",
                table: "xg_statistiken",
                columns: new[] { "spiel_id", "team_id", "datenquelle_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_xg_statistiken_team_id",
                table: "xg_statistiken",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_zeitsegment_statistiken_datenquelle_id",
                table: "zeitsegment_statistiken",
                column: "datenquelle_id");

            migrationBuilder.CreateIndex(
                name: "ix_zeitsegment_statistiken_spiel_id_team_id_segment_start_date",
                table: "zeitsegment_statistiken",
                columns: new[] { "spiel_id", "team_id", "segment_start", "datenquelle_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_zeitsegment_statistiken_team_id",
                table: "zeitsegment_statistiken",
                column: "team_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "elo_rating_historie");

            migrationBuilder.DropTable(
                name: "radio_links");

            migrationBuilder.DropTable(
                name: "schiedsrichter_statistiken");

            migrationBuilder.DropTable(
                name: "spieler_verletzungen");

            migrationBuilder.DropTable(
                name: "team_aliase");

            migrationBuilder.DropTable(
                name: "tor_events");

            migrationBuilder.DropTable(
                name: "trefferquote_eintraege");

            migrationBuilder.DropTable(
                name: "vorhersagen");

            migrationBuilder.DropTable(
                name: "xg_statistiken");

            migrationBuilder.DropTable(
                name: "zeitsegment_statistiken");

            migrationBuilder.DropTable(
                name: "spieler");

            migrationBuilder.DropTable(
                name: "datenquellen");

            migrationBuilder.DropTable(
                name: "spiele");

            migrationBuilder.DropTable(
                name: "saisons");

            migrationBuilder.DropTable(
                name: "schiedsrichter");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropTable(
                name: "wettbewerbe");
        }
    }
}
