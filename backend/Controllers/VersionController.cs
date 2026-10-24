using Microsoft.AspNetCore.Mvc;

namespace BetQuote.Api.Controllers;

/// <summary>
/// Liefert die aktuell laufende Version der Anwendung, gelesen live aus version.txt
/// (siehe scripts/git-hooks/post-commit). Die Datei wird bei jedem Request neu gelesen,
/// damit ein Deploy/Rebuild ohne Neustart sichtbar wird.
/// Im Dev-Profil wird ein "-dev"-Suffix angehängt (siehe CLAUDE.md "Dev/live environment
/// separation"), damit auf einen Blick erkennbar ist, welche Umgebung antwortet.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public VersionController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var version = ReadVersion();

        if (_environment.IsDevelopment())
        {
            version += "-dev";
        }

        return Ok(new { version });
    }

    private static string ReadVersion()
    {
        // version.txt liegt im Repo-Root, wird vom post-commit-Hook gepflegt und
        // beim Build/Container-Start mit ins Backend-Verzeichnis kopiert (siehe Dockerfile).
        var candidatePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "version.txt"),
            Path.Combine(Directory.GetCurrentDirectory(), "version.txt"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "version.txt"),
        };

        foreach (var path in candidatePaths)
        {
            if (System.IO.File.Exists(path))
            {
                var content = System.IO.File.ReadAllText(path).Trim();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    return content;
                }
            }
        }

        return "v0.0.0";
    }
}
