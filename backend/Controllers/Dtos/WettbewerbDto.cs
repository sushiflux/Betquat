namespace BetQuote.Api.Controllers.Dtos;

/// <summary>Basis fuer die Land-&gt;Liga-Tab-Navigation im Wochen-Feed (APP_STRUCTURE.md).</summary>
public record WettbewerbDto(int Id, string Name, string Kurzname, string Land, bool IstPokal, int Sortierung);
