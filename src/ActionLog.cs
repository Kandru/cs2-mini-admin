using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MiniAdmin
{
    /// <summary>One admin action; ids are SteamID64 strings, empty for console, bots and server-wide actions.</summary>
    public readonly record struct LogEntry(DateTime Time, string Admin, string AdminId, string Action, string Target, string TargetId, string Detail);

    /// <summary>
    /// Admin action log: kept in memory for the menu's log tab, appended to a tab separated file
    /// (time, admin, admin id, action, target, target id, detail) and optionally posted to Discord.
    /// </summary>
    public partial class MiniAdmin
    {
        private const int ActionLogSize = 200;
        private const string ActionLogFileName = "MiniAdmin-actions.log";
        private const long ActionLogMaxBytes = 5 * 1024 * 1024;
        private const int ActionLogTailBytes = 256 * 1024;

        // destructive actions: red in the log tab and in Discord
        private static readonly HashSet<string> DangerActions = new(StringComparer.Ordinal)
        {
            "kick", "ban", "kill", "slap", "strip", "rename", "restart", "swap", "scramble", "map"
        };

        private static readonly HttpClient DiscordHttp = new() { Timeout = TimeSpan.FromSeconds(10) };

        private readonly List<LogEntry> _actionLog = [];

        // addons/counterstrikesharp/logs next to the core logs
        private string ActionLogPath => Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "logs", ActionLogFileName));

        private void LoadActionLog()
        {
            _actionLog.Clear();
            if (!Config.ActionLogFile)
            {
                return;
            }
            try
            {
                if (!File.Exists(ActionLogPath))
                {
                    return;
                }
                // only the end of the file: the menu keeps the last ActionLogSize entries anyway
                using FileStream stream = new(ActionLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                long start = Math.Max(0, stream.Length - ActionLogTailBytes);
                _ = stream.Seek(start, SeekOrigin.Begin);
                using StreamReader reader = new(stream);
                List<string> lines = [.. reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries)];
                if (start > 0 && lines.Count > 0)
                {
                    // the first line was cut by the seek
                    lines.RemoveAt(0);
                }
                for (int i = Math.Max(0, lines.Count - ActionLogSize); i < lines.Count; i++)
                {
                    string[] f = lines[i].Split('\t');
                    if (f.Length >= 7 && DateTime.TryParse(f[0], out DateTime time))
                    {
                        _actionLog.Add(new LogEntry(time, f[1], f[2], f[3], f[4], f[5], f[6]));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MiniAdmin] Could not read the action log: {ex.Message}");
            }
        }

        private void LogAction(CCSPlayerController? admin, string action, CCSPlayerController? target = null, string detail = "") =>
            LogAction(admin, action,
                target is { IsValid: true } ? target.PlayerName : string.Empty,
                target is { IsValid: true, IsBot: false } ? target.SteamID.ToString() : string.Empty,
                detail);

        private void LogAction(CCSPlayerController? admin, string action, string target, string targetId, string detail = "")
        {
            LogEntry entry = new(
                DateTime.Now,
                admin is { IsValid: true } ? admin.PlayerName : "Console",
                admin is { IsValid: true, IsBot: false } ? admin.SteamID.ToString() : string.Empty,
                action,
                target,
                targetId,
                detail);
            _actionLog.Add(entry);
            if (_actionLog.Count > ActionLogSize)
            {
                _actionLog.RemoveAt(0);
            }

            if (Config.ActionLogFile)
            {
                try
                {
                    string path = ActionLogPath;
                    _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    // rotate: the current file becomes .1 (the previous .1 is dropped)
                    FileInfo file = new(path);
                    if (file.Exists && file.Length > ActionLogMaxBytes)
                    {
                        File.Move(path, path + ".1", overwrite: true);
                    }
                    File.AppendAllText(path, string.Join('\t',
                        entry.Time.ToString("yyyy-MM-dd HH:mm:ss"), Clean(entry.Admin), entry.AdminId, entry.Action,
                        Clean(entry.Target), entry.TargetId, Clean(entry.Detail)) + "\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MiniAdmin] Could not write the action log: {ex.Message}");
                }
            }

            MarkAllMenusDirty();
            PostToDiscord(entry);
        }

        /// <summary>"{admin}: {action} {target} {detail}" in the language of <paramref name="text"/>.</summary>
        private static string LogText(LogEntry entry, Func<string, string> text)
        {
            string line = text("log.format")
                .Replace("{admin}", entry.Admin)
                .Replace("{action}", text($"log.action.{entry.Action}"))
                .Replace("{target}", entry.Target)
                .Replace("{detail}", entry.Detail.Length > 0 ? "(" + entry.Detail + ")" : string.Empty);
            return string.Join(' ', line.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private void PostToDiscord(LogEntry entry)
        {
            string url = Config.Discord.WebhookUrl;
            if (string.IsNullOrWhiteSpace(url)
                || !Config.Discord.Events.Contains(entry.Action, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            // everything that touches the game is read here, on the game thread
            string Field(string name, string id) => id.Length > 0 ? $"{name} ({id})" : name;
            var payload = new
            {
                username = "MiniAdmin",
                // player names are untrusted: no @everyone / role / user pings, whatever they contain
                allowed_mentions = new { parse = Array.Empty<string>() },
                embeds = new[]
                {
                    new
                    {
                        title = Localizer[$"log.action.{entry.Action}"].Value,
                        description = LogText(entry with { Admin = Field(entry.Admin, entry.AdminId), Target = Field(entry.Target, entry.TargetId) }, key => Localizer[key].Value),
                        color = DangerActions.Contains(entry.Action) ? 0xe05a5a : 0xf0a531,
                        fields = new[]
                        {
                            new { name = "Server", value = FindCvar("hostname")?.StringValue ?? "-", inline = true },
                            new { name = "Map", value = Server.MapName, inline = true }
                        },
                        timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };
            string json = JsonSerializer.Serialize(payload);
            _ = Task.Run(async () =>
            {
                try
                {
                    using StringContent content = new(json, Encoding.UTF8, "application/json");
                    using HttpResponseMessage response = await DiscordHttp.PostAsync(url, content);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[MiniAdmin] Discord webhook answered {(int)response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MiniAdmin] Discord webhook failed: {ex.Message}");
                }
            });
        }

        private static string Clean(string text) => text.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
    }
}
