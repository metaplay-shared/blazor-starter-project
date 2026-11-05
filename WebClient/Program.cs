using Game.Logic;
using WebClient.Services;
using WebClientBase.Components;
using WebClientBase.Configuration;
using WebClientBase.Startup;

// Dark, cinematic navy base palette with light text and a command-blue accent. This drives the CSS variables used
// across the game screen and WebClientBase's components. Tweak these (and app.css) to set your game's look.
// wwwroot/index.html repeats BgPrimary and TextSecondary for the page before the client starts, and
// wwwroot/manifest.webmanifest repeats BgPrimary for the installed app's window: change them with it.
ThemeColors theme = new ThemeColors
{
    BgPrimary     = "#02060a",
    BgSecondary   = "#070d15",
    BgCard        = "#0c141f",
    BgHover       = "#16202e",
    BorderColor   = "rgba(255, 255, 255, 0.08)",
    TextPrimary   = "#f4f4f5",
    TextSecondary = "#a1a1aa",
    TextMuted     = "#71717a",
    AccentBlue    = "#1f7fff",
};

await WebClientHostBuilder<App, PlayerModel, MetaplayClientService>
    .Create(args, appTitle: new GlobalOptions().Options.ProjectName, logoEmoji: "🎮", theme: theme)
    .RunAsync();
