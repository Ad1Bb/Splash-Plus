// Tests for the parts of Splash Plus that do not need Windows: settings, migration of old settings,
// detection rules, image sizes and the gamepad button logic.
// Build the add-on first (dotnet build SplashAddonPlus.csproj -c Release), then: dotnet run --project Tests -c Release
// The exit code is the number of failed checks.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SplashAddonPlus;

int failures = 0;
void Check(bool ok, string name) { Console.WriteLine((ok ? "PASS " : "FAIL ") + name); if (!ok) failures++; }

// 1) Settings of the original Splash Addon 1.0.2 load; new fields get defaults.
string oldJson = @"{""ExcludedGameIds"":[""aaa""],""SplashScreenDuration"":12,""UsePlatformSpecificTimers"":true,
 ""GameSpecificDurations"":{""bbb"":30},""UseGameStartedTimer"":true,""LogoSize"":250,
 ""Platform1"":""PC (Windows)"",""Duration1"":7,""Platform2"":""Nintendo Switch"",""Duration2"":20,""Platform3"":""PC (Windows)"",""Duration3"":99}";
var s = JsonConvert.DeserializeObject<SplashAddonSettings>(oldJson);
s.Upgrade(); // the plugin does this right after loading
Check(s.SplashScreenDuration == 12 && s.LogoSize == 250, "old values kept");
Check(!JsonConvert.SerializeObject(s).Contains("UseGameStartedTimer"), "removed \"count from game start\" option is dropped");
Check(s.AutoDetectFullscreen && s.MinimumDisplaySeconds == 2 && s.DetectionDelaySeconds == 5 && s.MaximumWaitSeconds == 60, "new fields default");
Check(s.IsExcluded("aaa") && s.IsExcluded("AAA") && !s.IsExcluded("bbb"), "IsExcluded");

// 2) Duration priority + duplicate platform no longer throws.
Check(s.GetDurationForGame("bbb", "PC (Windows)") == 30, "per-game wins");
Check(s.GetDurationForGame("x", "PC (Windows)") == 7, "platform exact (first duplicate wins)");
Check(s.GetDurationForGame("x", "switch") == 20, "platform partial match");
Check(s.GetDurationForGame("x", "Sega Saturn") == 12, "fallback default");

// 3) Menu helpers.
s.SetExcluded(new[] { "ccc", "aaa" }, true);
Check(s.ExcludedGameIds.Count == 2 && s.IsExcluded("ccc"), "exclude adds without duplicates");
s.SetExcluded(new[] { "aaa" }, false);
Check(!s.IsExcluded("aaa") && s.IsExcluded("ccc"), "include removes");
var changed = new List<string>();
s.PropertyChanged += (o, e) => changed.Add(e.PropertyName);
s.SetGameSpecificDuration(new[] { "ddd" }, 5);
Check(s.HasGameSpecificDuration("ddd") && changed.Contains("GameSpecificDurationsText"), "set duration + text refresh");
s.SetGameSpecificDuration(new[] { "ddd" }, null);
Check(!s.HasGameSpecificDuration("ddd"), "remove duration");

// 4) Cancel restores, End keeps and requests save.
int saves = 0;
var t = new SplashAddonSettings();
typeof(SplashAddonSettings).GetProperty("SaveRequested", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
    .SetValue(t, (Action)(() => saves++));
t.BeginEdit();
t.AutoDetectFullscreen = false; t.MaximumWaitSeconds = 5; t.ExcludedGameIdsText = "x1\r\ny2"; t.GameSpecificDurationsText = "g:3";
t.CancelEdit();
Check(t.AutoDetectFullscreen && t.MaximumWaitSeconds == 60 && t.ExcludedGameIds.Count == 0 && t.GameSpecificDurations.Count == 0, "cancel restores");
t.BeginEdit(); t.MinimumDisplaySeconds = 4; t.EndEdit();
Check(t.MinimumDisplaySeconds == 4 && saves == 1, "end edit keeps + saves");

// 5) Serialization: text helpers not written, new fields written.
string json = JsonConvert.SerializeObject(t);
Check(!json.Contains("Text") && json.Contains("AutoDetectFullscreen") && !json.Contains("SaveRequested"), "json shape");

// 6) Validation.
t.MaximumWaitSeconds = 1; t.MinimumDisplaySeconds = 3;
Check(!t.VerifySettings(out var errs) && errs.Count == 1, "max < min rejected: " + string.Join(" | ", errs));
t.MaximumWaitSeconds = 60;
Check(t.VerifySettings(out _), "valid settings accepted");

// 7) Text format of per-game times, as in the original.
t.GameSpecificDurationsText = "a:5\nbad\nb: 0\nc:x\n d : 9 ";
Check(t.GameSpecificDurations.Count == 2 && t.GameSpecificDurations["d"] == 9, "duration text parsing");


// 8) Ignored processes.
var ip = new SplashAddonSettings();
Check(ip.IgnoredProcessNames.Count == 0, "ignored list empty before Upgrade");
ip.IgnoredProcessNamesText = " EADesktop.exe \r\nUbisoftConnect; eadesktop ,  \n\n";
Check(ip.IgnoredProcessNames.Count == 2 && ip.IgnoredProcessNames[0] == "EADesktop" && ip.IgnoredProcessNames[1] == "UbisoftConnect",
      "ignored text parsing: " + string.Join("|", ip.IgnoredProcessNames));
string ipJson = JsonConvert.SerializeObject(ip);
var reloaded = JsonConvert.DeserializeObject<SplashAddonSettings>(ipJson);
reloaded = JsonConvert.DeserializeObject<SplashAddonSettings>(JsonConvert.SerializeObject(reloaded));
Check(reloaded.IgnoredProcessNames.Count == 2, "reload does not duplicate list");
var fromOld = JsonConvert.DeserializeObject<SplashAddonSettings>(oldJson);
fromOld.Upgrade();
Check(fromOld.IgnoredProcessNames.Count == 1 && fromOld.IgnoredProcessNames[0] == "gamingservicesui", "original config gets the default ignore entry");
ip.BeginEdit(); ip.IgnoredProcessNamesText = "x"; ip.CancelEdit();
Check(ip.IgnoredProcessNames.Count == 2, "cancel restores ignore list");

// 9) Diagnostic logging: off by default, survives save/load, imported configs stay off.
Check(!new SplashAddonSettings().EnableDiagnosticLogging, "diagnostic logging off by default");
Check(!JsonConvert.DeserializeObject<SplashAddonSettings>(oldJson).EnableDiagnosticLogging, "old config keeps logging off");
var dl = new SplashAddonSettings { EnableDiagnosticLogging = true };
Check(JsonConvert.DeserializeObject<SplashAddonSettings>(JsonConvert.SerializeObject(dl)).EnableDiagnosticLogging, "logging flag round-trips");
dl.BeginEdit(); dl.EnableDiagnosticLogging = false; dl.CancelEdit();
Check(dl.EnableDiagnosticLogging, "cancel restores logging flag");

// 10) Gamepad B button.
{
    var pads = new ushort?[4];           // null = slot empty
    var calls = new int[4];
    var now = new DateTime(2026, 1, 1, 12, 0, 0);
    const ushort B = 0x2000, A = 0x1000;
    pads[0] = B;                          // B already held when the splash appears
    var w = new GamepadWatcher(i => { calls[i]++; return pads[i]; }, () => now);

    now = now.AddMilliseconds(100);
    Check(w.PollForBPress(true) == -1, "held B at start does not close");
    pads[0] = 0; now = now.AddMilliseconds(100); w.PollForBPress(true);
    pads[0] = B; now = now.AddMilliseconds(100);
    Check(w.PollForBPress(true) == 0, "fresh B press closes (controller 1)");
    now = now.AddMilliseconds(100);
    Check(w.PollForBPress(true) == -1, "holding B does not fire again");

    pads[0] = 0; now = now.AddMilliseconds(100); w.PollForBPress(false);
    pads[0] = B; now = now.AddMilliseconds(100);
    Check(w.PollForBPress(false) == -1, "press during grace period ignored");
    now = now.AddMilliseconds(100);
    Check(w.PollForBPress(true) == -1, "B held through grace period still ignored");

    pads[0] = A; now = now.AddMilliseconds(100);
    Check(w.PollForBPress(true) == -1, "other buttons ignored");

    // Slot 2 empty: probed only once per second.
    int before = calls[1];
    for (int i = 0; i < 9; i++) { now = now.AddMilliseconds(100); w.PollForBPress(true); }
    Check(calls[1] - before <= 1, "empty slot probed at most once per second (" + (calls[1] - before) + " calls / 0.9 s)");

    // Controller connected while holding B: not a press.
    pads[1] = B; now = now.AddSeconds(1.1);
    Check(w.PollForBPress(true) == -1, "pad connected with B held does not close");
    pads[1] = 0; now = now.AddMilliseconds(100); w.PollForBPress(true);
    pads[1] = B; now = now.AddMilliseconds(100);
    Check(w.PollForBPress(true) == 1, "second controller press reported as controller 2");

    // Unplugged controller resets cleanly.
    pads[1] = null; now = now.AddMilliseconds(100);
    Check(w.PollForBPress(true) == -1, "unplugged controller ignored");
}

// 11) English texts (the default before Playnite's language is set).
Check(Loc.MenuPreview == "Show splash screen preview", "menu text in English");
var bad = new SplashAddonSettings { MaximumWaitSeconds = 0 };
bad.VerifySettings(out var enErrors);
Check(enErrors.Count == 1 && enErrors[0] == "The maximum waiting time must be greater than 0.", "validation error in English: " + enErrors[0]);
Check(Loc.AskDuration("Forza", 1).Contains("\"Forza\"") && Loc.AskDuration("x", 3).Contains("selected games"), "duration prompt singular/plural");

// 12) Game folder.
Check(GameFolder.Normalize(@"D:\SteamLibrary\steamapps\common\BALLxPIT\") == @"D:\SteamLibrary\steamapps\common\BALLxPIT", "steam game folder kept, trailing slash removed");
Check(GameFolder.Normalize(@"C:\Program Files\WindowsApps\Microsoft.ForteBaseGame_3.483.970.0_x64__8wekyb3d8bbwe") != null, "Game Pass folder kept");
Check(GameFolder.Normalize("D:/Witcher3") == @"D:\Witcher3", "forward slashes, one level deep");
Check(GameFolder.Normalize(@"\\nas\games\Doom") == @"\\nas\games\Doom", "network folder kept");
foreach (string badFolder in new[] { @"C:\", "C:", @"D:\Games", @"D:\SteamLibrary\steamapps\common", @"E:\Steam\steamapps", @"C:\Program Files",
                                @"C:\Program Files (x86)", @"C:\Program Files\WindowsApps", @"C:\XboxGames", @"\\nas\games", "relative\\folder", "", null,
                                "C:Games\\Doom", @"\Games\Doom", @"..\Doom" })
{
    Check(GameFolder.Normalize(badFolder) == null, "too general or invalid folder rejected: " + (badFolder ?? "null"));
}
Check(GameFolder.Normalize(@"C:\Users\Me", new[] { @"C:\Users\Me\" }) == null, "system folder from the list rejected");
string bj = GameFolder.Normalize(@"D:\SteamLibrary\steamapps\common\BALLxPIT");
Check(GameFolder.Contains(bj, @"d:\steamlibrary\steamapps\common\ballxpit\Balls.exe"), "exe in folder (case-insensitive)");
Check(GameFolder.Contains(bj, @"D:\SteamLibrary\steamapps\common\BALLxPIT\bin\x64\Game.exe"), "exe in a subfolder");
Check(!GameFolder.Contains(bj, @"D:\SteamLibrary\steamapps\common\BALLxPIT2\Balls.exe"), "sibling folder with the same prefix is not inside");
Check(!GameFolder.Contains(bj, @"C:\Windows\explorer.exe") && !GameFolder.Contains(null, @"C:\a.exe") && !GameFolder.Contains(bj, ""), "outside / empty");

// 13) Window coverage.
Check(Math.Abs(GameWindowDetector.CoverageOf(0, 0, 1920, 1080, 0, 0, 3840, 2160) - 0.25) < 1e-9, "1080p window on a 4K screen = 25%");
Check(Math.Abs(GameWindowDetector.CoverageOf(-8, -8, 3848, 2168, 0, 0, 3840, 2160) - 1.0) < 1e-9, "window larger than screen = 100%");
Check(GameWindowDetector.CoverageOf(0, 0, 622, 302, 0, 0, 3840, 2160) < GameWindowDetector.WindowedMinCoverage, "Forza's small launch window stays below the threshold");
Check(Math.Abs(GameWindowDetector.CoverageOf(3000, 0, 4840, 1000, 0, 0, 3840, 2160) - (840.0 * 1000 / (3840.0 * 2160))) < 1e-9, "only the part on this screen counts");
Check(GameWindowDetector.CoverageOf(0, 0, 100, 100, 0, 0, 0, 0) == 0, "empty screen rectangle");

// 14) Image decode size.
Check(ImageLoader.ComputeDecodeWidth(3840, 2160, 1920, 1080, true) == 1920, "4K background for a 1080p screen decoded at 1920");
Check(ImageLoader.ComputeDecodeWidth(5120, 1440, 1920, 1080, true) == 3840, "ultrawide image keeps enough height to fill the screen");
Check(ImageLoader.ComputeDecodeWidth(1280, 720, 1920, 1080, true) == 0, "small image is never upscaled at decode time");
Check(ImageLoader.ComputeDecodeWidth(2000, 500, 307, 864, false) == 307, "logo fits the target width");
Check(ImageLoader.ComputeDecodeWidth(1200, 1800, 360, 0, false) == 360, "width-only target (blurred cover)");
Check(ImageLoader.ComputeDecodeWidth(600, 900, 0, 1296, false) == 0, "height-only target larger than image");
Check(ImageLoader.ComputeDecodeWidth(0, 0, 1920, 1080, true) == 0 && ImageLoader.ComputeDecodeWidth(100, 100, 0, 0, true) == 0, "unknown sizes");

// 15) Platform list and appearance settings.
var up = JsonConvert.DeserializeObject<SplashAddonSettings>(oldJson);
up.Upgrade();
Check(up.PlatformDurations.Count == 3 && up.PlatformDurations[0].Platform == "PC (Windows)" && up.PlatformDurations[1].Seconds == 20, "10 fixed slots moved into the list");
up.Upgrade();
Check(up.PlatformDurations.Count == 3, "upgrade can run twice");
string upJson = JsonConvert.SerializeObject(up);
Check(!upJson.Contains("\"Platform1\"") && !upJson.Contains("\"Duration1\"") && upJson.Contains("\"PlatformDurations\""), "old slots are no longer written");
var again = JsonConvert.DeserializeObject<SplashAddonSettings>(upJson);
again.Upgrade();
Check(again.PlatformDurations.Count == 3 && again.GetDurationForGame("x", "PC (Windows)") == 7, "list survives save/load");
up.BeginEdit(); up.PlatformDurations.Add(new PlatformDuration { Platform = "Sega Saturn", Seconds = 3 }); up.PlatformDurations.RemoveAt(0); up.CancelEdit();
Check(up.PlatformDurations.Count == 3 && up.PlatformDurations[0].Platform == "PC (Windows)", "cancel restores the platform list");
up.PlatformDurations.Clear(); up.BeginEdit(); up.CancelEdit(); up.Upgrade();
Check(up.PlatformDurations.Count == 0, "removed rows are not brought back from the old slots");

Check(new SplashAddonSettings().LogoWidthPercent == 16, "default logo width 16%");
Check(JsonConvert.DeserializeObject<SplashAddonSettings>(oldJson).LogoWidthPercent == 13, "old 250 px logo becomes 13%");
var lw = new SplashAddonSettings { LogoWidthPercent = 22 };
Check(JsonConvert.DeserializeObject<SplashAddonSettings>(JsonConvert.SerializeObject(lw)).LogoWidthPercent == 22, "logo width round-trips");

var ap = new SplashAddonSettings { LogoPosition = LogoPlacement.TopRight, BackgroundDimPercent = 30, BackgroundZoom = true };
string apJson = JsonConvert.SerializeObject(ap);
var ap2 = JsonConvert.DeserializeObject<SplashAddonSettings>(apJson);
Check(apJson.Contains("\"TopRight\"") && ap2.LogoPosition == LogoPlacement.TopRight && ap2.BackgroundDimPercent == 30 && ap2.BackgroundZoom,
      "appearance saved, logo position as a readable name");
var defs = JsonConvert.DeserializeObject<SplashAddonSettings>(oldJson);
Check(defs.DetectWindowedGames && defs.LogoPosition == LogoPlacement.BottomLeft
      && defs.BackgroundDimPercent == 0 && !defs.BackgroundVignette && !defs.BackgroundZoom, "old configs get the new defaults (look unchanged)");

Check(new SplashAddonSettings { LogoWidthPercent = 0 }.VerifySettings(out _), "logo size 0% (no logo) accepted");
Check(!new SplashAddonSettings { LogoWidthPercent = 91 }.VerifySettings(out _), "logo size above 90% rejected");
Check(JsonConvert.DeserializeObject<SplashAddonSettings>("{\"LogoSize\":0}").LogoWidthPercent == 0, "original 0 px logo (off) stays off");
Check(JsonConvert.DeserializeObject<SplashAddonSettings>("{\"LogoSize\":5000}").LogoWidthPercent == 90, "huge original logo capped at 90%");

// 16) Default ignore entry: added once, removable.
var dflt = (SplashAddonSettings)new SplashAddonSettings().GetDefaults();
Check(dflt.IgnoredProcessNames.Count == 1 && dflt.IgnoredProcessNames[0] == "gamingservicesui" && dflt.DefaultsVersion == 1, "fresh settings start with gamingservicesui");
var mine = JsonConvert.DeserializeObject<SplashAddonSettings>("{\"IgnoredProcessNames\":[\"EADesktop\"]}");
mine.Upgrade();
Check(mine.IgnoredProcessNames.Count == 2 && mine.IgnoredProcessNames[0] == "gamingservicesui" && mine.IgnoredProcessNames[1] == "EADesktop", "added in front of the user's own entries");
mine.Upgrade();
Check(mine.IgnoredProcessNames.Count == 2, "added only once");
mine.IgnoredProcessNamesText = "EADesktop";
var reread = JsonConvert.DeserializeObject<SplashAddonSettings>(JsonConvert.SerializeObject(mine));
reread.Upgrade();
Check(reread.IgnoredProcessNames.Count == 1 && reread.IgnoredProcessNames[0] == "EADesktop", "removing it sticks after save, load and upgrade");
var dup = JsonConvert.DeserializeObject<SplashAddonSettings>("{\"IgnoredProcessNames\":[\"GamingServicesUI\"]}");
dup.Upgrade();
Check(dup.IgnoredProcessNames.Count == 1, "no duplicate when the user already had it (any letter case)");
var badDim = new SplashAddonSettings { BackgroundDimPercent = 95 };
Check(!badDim.VerifySettings(out _), "dimming above 90% rejected");

// 17) Readable game lists in the settings.
var names = new Dictionary<string, string> { ["b"] = "witcher 3", ["a"] = "Alan Wake", ["c"] = "Celeste" };
var sorted = GameEntries.Sort(new[] { "b", "zzz-gone", "a", "A", "c", " ", null }, id => names.TryGetValue(id, out var n) ? n : null);
Check(string.Join(",", sorted.Select(g => g.Name ?? "?" + g.Id)) == "Alan Wake,Celeste,witcher 3,?zzz-gone",
      "sorted by name, case-insensitive, unknown last, duplicates and blanks dropped: " + string.Join(",", sorted.Select(g => g.Name ?? "?" + g.Id)));
var failing = GameEntries.Sort(new[] { "x" }, id => throw new InvalidOperationException());
Check(failing.Count == 1 && failing[0].Name == null, "a failing name lookup shows the game as unknown");
Check(GameEntries.Sort(null, null).Count == 0, "no games");
Check(Loc.UnknownGame("abc") == "Unknown game (abc)", "unknown game label");
// Editing a time in place keeps the value through Save, and Cancel brings the old one back.
var ed = new SplashAddonSettings();
ed.SetGameSpecificDuration(new[] { "g1" }, 15);
ed.BeginEdit(); ed.GameSpecificDurations["g1"] = 40; ed.CancelEdit();
Check(ed.GameSpecificDurations["g1"] == 15, "cancel restores an edited game time");
ed.BeginEdit(); ed.GameSpecificDurations["g1"] = 0;
Check(!ed.VerifySettings(out _), "a 0 s game time is reported on Save");

// 18) Splash styles: setting, game details and the texts they show.
Check(new SplashAddonSettings().Style == SplashStyle.Classic, "Classic style by default");
Check(JsonConvert.DeserializeObject<SplashAddonSettings>(oldJson).Style == SplashStyle.Classic, "old configs keep the Classic look");
var st = new SplashAddonSettings { Style = SplashStyle.Showcase };
string stJson = JsonConvert.SerializeObject(st);
Check(stJson.Contains("\"Style\":\"Showcase\"") && JsonConvert.DeserializeObject<SplashAddonSettings>(stJson).Style == SplashStyle.Showcase, "style saved as a readable name");
st.BeginEdit(); st.Style = SplashStyle.Cinema; st.CancelEdit();
Check(st.Style == SplashStyle.Showcase, "cancel restores the style");

Check(SplashText.Playtime(0) == null && SplashText.Playtime(59) == "1m" && SplashText.Playtime(600) == "10m"
      && SplashText.Playtime((42 * 60 + 10) * 60) == "42h 10m" && SplashText.Playtime(3600) == "1h 0m"
      && SplashText.Playtime(150UL * 3600 + 120) == "150h", "play time format");
Check(SplashText.Join(" · ", "2020", null, " ", "RPG") == "2020 · RPG" && SplashText.Join(" · ", null, "") == null, "details skip empty parts");
Check(Math.Abs(SplashText.LetterboxShare(1920, 1080) - 0.128) < 0.001 && SplashText.LetterboxShare(1920, 1200) == 0.13
      && SplashText.LetterboxShare(2560, 1080) == 0.04 && SplashText.LetterboxShare(5120, 1440) == 0.04, "letterbox bars for 16:9, 16:10, 21:9, 32:9");

var removedStyle = JsonConvert.DeserializeObject<SplashAddonSettings>("{\"Style\":\"Slash\",\"MaximumWaitSeconds\":30}");
Check(removedStyle.Style == SplashStyle.Classic && removedStyle.MaximumWaitSeconds == 30, "a removed or unknown style falls back to Classic, other settings stay");
Check(JsonConvert.DeserializeObject<SplashAddonSettings>("{\"Style\":7}").Style == SplashStyle.Classic
      && JsonConvert.DeserializeObject<SplashAddonSettings>("{\"Style\":null}").Style == SplashStyle.Classic, "odd style values fall back to Classic");

// 19) Settings page: one "when to close" choice, style options, one per-game table.
var cm = new SplashAddonSettings();
Check(cm.CloseMode == CloseMode.WhenGameAppears, "close mode: detection by default");
var cmEvents = new List<string>();
cm.PropertyChanged += (o, e) => cmEvents.Add(e.PropertyName);
cm.CloseMode = CloseMode.FixedTime;
Check(!cm.AutoDetectFullscreen && cm.CloseMode == CloseMode.FixedTime && cmEvents.Contains("CloseMode"), "close mode: fixed time");
cm.CloseMode = CloseMode.WhenGameAppears;
Check(cm.AutoDetectFullscreen && cm.CloseMode == CloseMode.WhenGameAppears, "close mode: back to detection");
Check(!JsonConvert.SerializeObject(cm).Contains("CloseMode"), "close mode is not saved separately");
Check(JsonConvert.DeserializeObject<SplashAddonSettings>("{\"AutoDetectFullscreen\":false,\"UseGameStartedTimer\":true}").CloseMode == CloseMode.FixedTime,
      "old \"count from game start\" setting reads as fixed time");
Check(Enum.GetValues(typeof(CloseMode)).Length == 2, "two close modes");

Check(StyleOptions.Customizable(SplashStyle.Classic) && !StyleOptions.Customizable(SplashStyle.Cinema) && !StyleOptions.Customizable(SplashStyle.PopFrost), "look options only in Custom (saved as Classic)");
Check(JsonConvert.DeserializeObject<SplashAddonSettings>("{\"Style\":\"Vhs\"}").Style == SplashStyle.Classic
      && JsonConvert.DeserializeObject<SplashAddonSettings>("{\"Style\":\"EightBit\"}").Style == SplashStyle.Classic, "removed VHS and 8-Bit styles fall back to Custom");

var pg = new SplashAddonSettings { SplashScreenDuration = 12 };
pg.SetExcluded(new[] { "a" }, true);
pg.SetGameSpecificDuration(new[] { "b" }, 30);
Check(pg.GamesWithOwnSettings.OrderBy(x => x).SequenceEqual(new[] { "a", "b" }), "one table lists turned-off and fixed-time games");
pg.SetGameSplashMode("a", GameSplashMode.FixedTime);
Check(!pg.IsExcluded("a") && pg.GameSpecificDurations["a"] == 12, "Off → fixed time uses the default time");
pg.SetGameSplashMode("b", GameSplashMode.Off);
Check(pg.IsExcluded("b") && !pg.HasGameSpecificDuration("b"), "fixed time → Off");
pg.SetGameSplashMode("b", GameSplashMode.FixedTime);
Check(pg.GameSpecificDurations["b"] == 12 && !pg.IsExcluded("b"), "back to fixed time");
pg.SetGameSplashMode("b", GameSplashMode.Default);
Check(pg.GetGameSplashMode("b") == GameSplashMode.Default && !pg.GamesWithOwnSettings.Contains("b"), "Default drops the game's own closing");
pg.ForgetGame("a"); pg.ForgetGame("b");
Check(!pg.GamesWithOwnSettings.Any(), "remove takes the game out of both lists");
foreach (SplashStyle style in Enum.GetValues(typeof(SplashStyle)))
{
    var saved = JsonConvert.DeserializeObject<SplashAddonSettings>(JsonConvert.SerializeObject(new SplashAddonSettings { Style = style }));
    Check(saved.Style == style, "style round-trips: " + style);
}

// Per-game styles and Random
var gs = new SplashAddonSettings { Style = SplashStyle.Focus };
gs.SetGameStyle(new[] { "g1", "g2" }, SplashStyle.Slant);
gs.SetGameStyle(new[] { "g3" }, SplashStyle.Random);
Check(gs.StyleForGame("g1") == SplashStyle.Slant && gs.StyleForGame("g3") == SplashStyle.Random && gs.StyleForGame("other") == SplashStyle.Focus,
      "a game's own style wins over the style from the settings");
Check(gs.GamesWithOwnSettings.OrderBy(x => x).SequenceEqual(new[] { "g1", "g2", "g3" })
      && gs.GetGameSplashMode("g1") == GameSplashMode.Default, "games with only a style are listed, closing as set for all");
string gsJson = JsonConvert.SerializeObject(gs);
var gsSaved = JsonConvert.DeserializeObject<SplashAddonSettings>(gsJson);
Check(gsJson.Contains("\"g1\":\"Slant\"") && gsSaved.StyleForGame("g2") == SplashStyle.Slant && gsSaved.StyleForGame("g3") == SplashStyle.Random,
      "per-game styles saved by name");
Check(JsonConvert.DeserializeObject<SplashAddonSettings>("{\"GameStyles\":{\"x\":\"Vhs\"}}").StyleForGame("x") == SplashStyle.Classic,
      "an unknown per-game style reads as Custom");
Check(JsonConvert.DeserializeObject<SplashAddonSettings>(oldJson).GameStyles.Count == 0, "old configs have no per-game styles");
Check(!gs.ShowCustomOptions && gsSaved.GameStyles.Count == 3, "Custom options hidden while no one uses Custom");
gs.SetGameStyle(new[] { "g2" }, SplashStyle.Classic);
Check(gs.ShowCustomOptions, "Custom options shown when a game uses Custom");
gs.BeginEdit(); gs.SetGameStyle(new[] { "g1" }, null); gs.CancelEdit();
Check(gs.StyleForGame("g1") == SplashStyle.Slant, "cancel restores per-game styles");
gs.SetGameStyle(new[] { "g1" }, null); gs.ForgetGame("g2"); gs.ForgetGame("g3");
Check(!gs.GamesWithOwnSettings.Any(), "Default and Remove clear a game's style");

Check(StyleList.Designed.Length == Enum.GetValues(typeof(SplashStyle)).Length - 2
      && !StyleList.Designed.Contains(SplashStyle.Classic) && !StyleList.Designed.Contains(SplashStyle.Random), "ready-made styles exclude Custom and Random");
Check(StyleList.RandomPool.Length == StyleList.Designed.Length - 2
      && !StyleList.RandomPool.Contains(SplashStyle.TextMode) && !StyleList.RandomPool.Contains(SplashStyle.TextModeDark),
      "Random leaves out both Retro styles");
Check(StyleList.Designed.Select(StyleList.Name).SequenceEqual(StyleList.Designed.Select(StyleList.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase)),
      "ready-made styles listed A–Z");
Check(StyleList.Special.SequenceEqual(new[] { SplashStyle.Classic, SplashStyle.Random }), "Custom and Random on top");
Check(StyleList.Resolve(SplashStyle.Plinth, new Random(1), SplashStyle.Plinth) == SplashStyle.Plinth, "a chosen style stays as it is");
var rnd = new Random(7);
SplashStyle? last = null;
var seen = new HashSet<SplashStyle>();
bool repeated = false;
for (int i = 0; i < 2000; i++)
{
    SplashStyle picked = StyleList.Resolve(SplashStyle.Random, rnd, last);
    repeated |= picked == last;
    seen.Add(picked);
    last = picked;
}
Check(!repeated && seen.SetEquals(StyleList.RandomPool), "Random reaches every style of its pool, never twice in a row");

// Splash size in Desktop mode
Check(new SplashAddonSettings().DesktopSplashSize == DesktopSplashSize.FullScreen
      && JsonConvert.DeserializeObject<SplashAddonSettings>(oldJson).DesktopSplashSize == DesktopSplashSize.FullScreen, "full screen by default and for old configs");
string sizeJson = JsonConvert.SerializeObject(new SplashAddonSettings { DesktopSplashSize = DesktopSplashSize.Small });
Check(sizeJson.Contains("\"DesktopSplashSize\":\"Small\"")
      && JsonConvert.DeserializeObject<SplashAddonSettings>(sizeJson).DesktopSplashSize == DesktopSplashSize.Small, "splash size saved by name");
Check(JsonConvert.DeserializeObject<SplashAddonSettings>("{\"DesktopSplashSize\":\"Huge\"}").DesktopSplashSize == DesktopSplashSize.FullScreen
      && JsonConvert.DeserializeObject<SplashAddonSettings>("{\"DesktopSplashSize\":3}").DesktopSplashSize == DesktopSplashSize.FullScreen, "unknown size reads as full screen");
Check(SplashWindowPlacement.Fraction(DesktopSplashSize.FullScreen) == 1 && SplashWindowPlacement.Fraction(DesktopSplashSize.Large) == 0.75
      && SplashWindowPlacement.Fraction(DesktopSplashSize.Small) == 0.5, "size shares");
int[] place = SplashWindowPlacement.Compute(1920, 1080, 0, 0, 1920, 1040, 0.75);
Check(place.SequenceEqual(new[] { 240, 115, 1440, 810 }), "75% window centered in the work area: " + string.Join(",", place));
place = SplashWindowPlacement.Compute(2560, 1440, -2560, 0, 2560, 1400, 0.5);
Check(place.SequenceEqual(new[] { -1920, 340, 1280, 720 }), "50% window on a second monitor left of the primary: " + string.Join(",", place));
place = SplashWindowPlacement.Compute(1920, 1080, 0, 0, 1920, 700, 0.75);
Check(place[3] <= 700 && place[2] == (int)Math.Round(1920 * 700 / 1080.0) && place[1] == 0, "never taller than the work area, proportions kept: " + string.Join(",", place));
Check(JsonConvert.DeserializeObject<SplashAddonSettings>(JsonConvert.SerializeObject(new SplashAddonSettings { DesktopSplashSize = DesktopSplashSize.PlayniteWindow })).DesktopSplashSize
      == DesktopSplashSize.PlayniteWindow && SplashWindowPlacement.Fraction(DesktopSplashSize.PlayniteWindow) == 1, "Playnite-window size saved; its fallback is the whole screen");
Check(Math.Abs(SplashText.CoverAspect(null) - 2.0 / 3) < 1e-9 && SplashText.CoverAspect(0.4) == 0.55 && SplashText.CoverAspect(1.78) == 1.0
      && SplashText.CoverAspect(0.75) == 0.75 && Math.Abs(SplashText.CoverAspect(double.NaN) - 2.0 / 3) < 1e-9, "cover proportions limited to 0.55-1.0, 2:3 when unknown");

var gi2 = SplashGameInfo.FromGame(new Playnite.SDK.Models.Game("The Elder Scrolls V: Skyrim Special Edition"), "PC (Windows)");
Check(gi2.Name.StartsWith("The Elder") && gi2.Platform == "PC (Windows)" && gi2.Developer == null && gi2.ReleaseYear == null, "game details from a game outside the library");
Check(SplashGameInfo.FromGame(null, null).Name == "" && gi2.Library == null, "no game, no library");

// Loot Drop and Lock-On details: score, rating marks, whole hours, the rarity color.
var scored = new Playnite.SDK.Models.Game("Scored") { CriticScore = null, CommunityScore = 0, UserScore = 84 };
Check(SplashGameInfo.FromGame(scored, null).Score == 84, "score: first one above 0 (critic, community, user)");
Check(SplashGameInfo.FromGame(new Playnite.SDK.Models.Game("x") { CriticScore = 71, UserScore = 90 }, null).Score == 71, "critic score first");
Check(SplashGameInfo.FromGame(new Playnite.SDK.Models.Game("x"), null).Score == null, "no score");
Check(SplashText.RatingMarks(84) == 4 && SplashText.RatingMarks(90) == 5 && SplashText.RatingMarks(50) == 3
      && SplashText.RatingMarks(5) == 0 && SplashText.RatingMarks(100) == 5 && SplashText.RatingMarks(null) == null, "rating marks out of 5");
Check(SplashText.Hours(0) == null && SplashText.Hours(59) == "<1h" && SplashText.Hours(3599) == "<1h"
      && SplashText.Hours(42 * 3600 + 3000) == "42h", "whole hours played");

byte[] Pixels(int count, byte r, byte g, byte b, byte a = 255)
{
    var px = new byte[count * 4];
    for (int i = 0; i < count; i++) { px[4 * i] = b; px[4 * i + 1] = g; px[4 * i + 2] = r; px[4 * i + 3] = a; }
    return px;
}
Check(RarityTint.FromPixels(Pixels(1024, 128, 128, 128), 1024) == RarityTint.Neutral, "grey image: neutral tint");
Check(RarityTint.FromPixels(Pixels(1024, 20, 0, 0), 1024) == RarityTint.Neutral, "too dark: neutral tint");
Check(RarityTint.FromPixels(Pixels(1024, 255, 0, 0, 0), 1024) == RarityTint.Neutral, "transparent: neutral tint");
int red = RarityTint.FromPixels(Pixels(1024, 200, 30, 30), 1024);
Check(red == 0xEB7A7A,$"red cover: light red tint (got {red:X6})");
int blue = RarityTint.FromPixels(Pixels(1024, 40, 90, 220), 1024);
Check((blue & 0xFF) > ((blue >> 16) & 0xFF) && Math.Abs(((blue >> 16) & 0xFF) + (blue & 0xFF) - 2 * 179) <= 2, $"blue cover: light blue tint, lightness 0.70 (got {blue:X6})");
// A little color on a grey image stays neutral (at most 3% of the weight); more of it wins.
var mixed = Pixels(1024, 128, 128, 128);
for (int i = 0; i < 20; i++) { mixed[4 * i] = 0; mixed[4 * i + 1] = 200; mixed[4 * i + 2] = 0; }
Check(RarityTint.FromPixels(mixed, 1024) == RarityTint.Neutral, "a few colored pixels: neutral");
for (int i = 0; i < 200; i++) { mixed[4 * i] = 0; mixed[4 * i + 1] = 200; mixed[4 * i + 2] = 0; }
int green = RarityTint.FromPixels(mixed, 1024);
Check(((green >> 8) & 0xFF) > ((green >> 16) & 0xFF) && ((green >> 8) & 0xFF) > (green & 0xFF), $"enough green: green tint (got {green:X6})");

// Translations: every language has exactly the English keys, no empty texts, the same {0} placeholders.
var englishTable = Loc.Table("en");
Check(Loc.Languages.SequenceEqual(new[] { "en", "de", "fr", "es", "it", "pt", "pl", "ru", "zh" }), "language order");
foreach (string lang in Loc.Languages)
{
    var table = Loc.Table(lang);
    Check(table != null, "table exists: " + lang);
    if (table == null) continue;
    var missing = englishTable.Keys.Where(k => !table.ContainsKey(k)).ToList();
    var extra = table.Keys.Where(k => !englishTable.ContainsKey(k)).ToList();
    Check(missing.Count == 0 && extra.Count == 0, $"{lang}: same keys (missing: {string.Join(",", missing)}; extra: {string.Join(",", extra)})");
    var empty = table.Where(p => string.IsNullOrWhiteSpace(p.Value)).Select(p => p.Key).ToList();
    Check(empty.Count == 0, $"{lang}: no empty texts {string.Join(",", empty)}");
    var placeholders = englishTable.Keys.Where(table.ContainsKey)
        .Where(k => englishTable[k].Contains("{0}") != table[k].Contains("{0}") || table[k].Contains("{1}")).ToList();
    Check(placeholders.Count == 0, $"{lang}: placeholders match {string.Join(",", placeholders)}");
}

// Every translated property has an English text (a missing key would throw).
foreach (var prop in typeof(Loc).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
             .Where(p => p.PropertyType == typeof(string) && p.Name != "Language"))
{
    string value = null;
    try { value = (string)prop.GetValue(null); } catch { }
    if (string.IsNullOrEmpty(value)) Check(false, "English text for " + prop.Name);
}

// The language follows Playnite's language code; unsupported ones fall back to English.
foreach (var (code, expected) in new[] { ("pl_PL", "pl"), ("de_DE", "de"), ("pt_BR", "pt"), ("pt_PT", "pt"), ("zh_CN", "zh"),
                                         ("zh_TW", "en"), ("en_US", "en"), ("cs_CZ", "en"), ("", "en"), (null, "en") })
{
    Loc.SetLanguage(code);
    Check(Loc.Language == expected, $"language {code ?? "null"} -> {expected} (got {Loc.Language})");
}
Loc.SetLanguage("pl_PL");
Check(Loc.MenuPreview == "Pokaż podgląd ekranu powitalnego" && Loc.UnknownGame("abc") == "Nieznana gra (abc)"
      && Loc.AskDuration("Forza", 1).Contains("„Forza”") && Loc.Starting == "Uruchamianie...", "Polish texts in use");
var plErrors = new List<string>();
new SplashAddonSettings { MaximumWaitSeconds = 0 }.VerifySettings(out plErrors);
Check(plErrors.Count == 1 && plErrors[0].StartsWith("Maksymalny czas"), "validation error in Polish");
Loc.SetLanguage("de_DE");
Check(Loc.SectionAppearance == "Darstellung" && Loc.StyleCinema == "Cinema", "German texts; style names stay");
Loc.SetLanguage("en_US");
Check(Loc.MenuPreview == "Show splash screen preview", "back to English");

// Update check: tags and yaml versions compare as versions; only a newer release is announced.
Check(UpdateCheck.Parse("2.1") == new Version(2, 1, 0) && UpdateCheck.Parse("v2.10") == new Version(2, 10, 0)
      && UpdateCheck.Parse("2.1.3") == new Version(2, 1, 3) && UpdateCheck.Parse("beta") == null && UpdateCheck.Parse(null) == null
      && UpdateCheck.Parse("99999999999") == null && UpdateCheck.Parse("2.1-rc1") == null, "update: versions parse, odd ones are ignored");
Check(UpdateCheck.FromManifest("Id: X\r\nVersion: 2.1\r\nModule: a.dll") == new Version(2, 1, 0)
      && UpdateCheck.FromManifest("Version: '2.0'") == new Version(2, 0, 0) && UpdateCheck.FromManifest("Name: X") == null
      && UpdateCheck.FromManifest("Version:\r\n") == null, "update: version from extension.yaml");
Check(UpdateCheck.NewerTag("2.2", new Version(2, 1, 0)) == "2.2" && UpdateCheck.NewerTag("v2.10", new Version(2, 9, 0)) == "2.10"
      && UpdateCheck.NewerTag("2.1", new Version(2, 1, 0)) == null && UpdateCheck.NewerTag("2.0", new Version(2, 1, 0)) == null
      && UpdateCheck.NewerTag("nightly", new Version(2, 1, 0)) == null && UpdateCheck.NewerTag(null, new Version(2, 1, 0)) == null,
      "update: only a newer release is announced");
Check(UpdateCheck.SafePage("https://github.com/Ad1Bb/Splash-Plus/releases/tag/2.2") == "https://github.com/Ad1Bb/Splash-Plus/releases/tag/2.2"
      && UpdateCheck.SafePage("https://evil.example/") == UpdateCheck.ReleasesPage && UpdateCheck.SafePage(null) == UpdateCheck.ReleasesPage,
      "update: only this add-on's release pages are opened");
var nowUtc = new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);
Check(UpdateCheck.IsDue(null, nowUtc) && UpdateCheck.IsDue("garbage", nowUtc) && UpdateCheck.IsDue(nowUtc.AddHours(-21).ToString("o"), nowUtc)
      && !UpdateCheck.IsDue(nowUtc.AddHours(-2).ToString("o"), nowUtc) && UpdateCheck.IsDue(nowUtc.AddDays(3).ToString("o"), nowUtc),
      "update: at most one check a day; a damaged or future time means due");
Check(Loc.UpdateAvailable("2.2").Contains("2.2") && Loc.UpdateAvailableFullscreen("2.2").Contains("2.2"), "update: notification texts");

// The whole check against a local stand-in for GitHub: every failure is only logged, never thrown.
string Serve(string status, string body)
{
    var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
    listener.Start();
    int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    System.Threading.Tasks.Task.Run(() =>
    {
        using var client = listener.AcceptTcpClient();
        using var stream = client.GetStream();
        var request = new byte[8192];
        stream.Read(request, 0, request.Length);
        byte[] content = System.Text.Encoding.UTF8.GetBytes(body);
        byte[] head = System.Text.Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Type: application/json\r\nContent-Length: {content.Length}\r\nConnection: close\r\n\r\n");
        stream.Write(head, 0, head.Length);
        stream.Write(content, 0, content.Length);
        listener.Stop();
    });
    return $"http://127.0.0.1:{port}/";
}
(string Tag, string Page, int Logs) RunCheck(string url, string state = null)
{
    string tag = null, page = null;
    int logs = 0;
    bool finished = UpdateCheck.Start(state, (t, p) => { tag = t; page = p; }, _ => logs++, url).Wait(30000);
    if (!finished) logs = -1;
    return (tag, page, logs);
}
var offline = RunCheck("http://127.0.0.1:1/");
Check(offline.Tag == null && offline.Logs == 1, "update: no connection is only logged");
var html = RunCheck(Serve("200 OK", "<html>Captive portal</html>"));
Check(html.Tag == null && html.Logs == 1, "update: an HTML page instead of JSON is only logged");
var limited = RunCheck(Serve("403 Forbidden", "{\"message\":\"API rate limit exceeded\"}"));
Check(limited.Tag == null && limited.Logs == 1, "update: the rate limit is only logged");
var huge = RunCheck(Serve("200 OK", "{\"tag_name\":\"" + new string('9', 2 * 1024 * 1024) + "\"}"));
Check(huge.Tag == null && huge.Logs == 1, "update: an oversized answer is not read");
var odd = RunCheck(Serve("200 OK", "[1,2,3]"));
Check(odd.Tag == null && odd.Logs == 1, "update: an unexpected JSON shape is only logged");
var newer = RunCheck(Serve("200 OK", "{\"tag_name\":\"99.0\",\"html_url\":\"https://evil.example/x\"}"));
Check(newer.Tag == "99.0" && newer.Page == UpdateCheck.ReleasesPage && newer.Logs == 0, "update: a newer release is announced with a safe link");
string stateFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "splashplus-update-test", "update-check.txt");
if (System.IO.File.Exists(stateFile)) System.IO.File.Delete(stateFile);
var same = RunCheck(Serve("200 OK", "{\"tag_name\":\"2.1\",\"html_url\":\"https://github.com/Ad1Bb/Splash-Plus/releases/tag/2.1\"}"), stateFile);
Check(same.Tag == null && same.Logs == 0 && System.IO.File.Exists(stateFile), "update: the same version is not announced; the time is saved");
var skipped = RunCheck("http://127.0.0.1:1/", stateFile);
Check(skipped.Tag == null && skipped.Logs == 0, "update: no second request on the same day");
System.IO.File.WriteAllText(stateFile, "damaged");
var afterDamage = RunCheck(Serve("200 OK", "{\"tag_name\":\"2.1\"}"), stateFile);
Check(afterDamage.Logs == 0 && UpdateCheck.IsDue(System.IO.File.ReadAllText(stateFile), DateTime.UtcNow) == false, "update: a damaged saved time is replaced");
// Main menu texts: every language has its own, short enough for Fullscreen mode's fixed-width menu.
foreach (string code in new[] { "de_DE", "fr_FR", "es_ES", "it_IT", "pt_BR", "pl_PL", "ru_RU", "zh_CN" })
{
    Loc.SetLanguage(code);
    string style = Loc.MenuDefaultStyle, preview = Loc.MenuDefaultStylePreview;
    Loc.SetLanguage("en_US");
    Check(!string.IsNullOrEmpty(style) && style != Loc.MenuDefaultStyle && style.Length <= 30
          && !string.IsNullOrEmpty(preview) && preview != Loc.MenuDefaultStylePreview && preview.Length <= 30, $"main menu texts translated ({code})");
}
Check(Loc.MenuDefaultStyle == "Default style" && Loc.MenuDefaultStylePreview == "Default style preview", "main menu texts in English");
// Warm-up: compiling the add-on's methods ahead never throws, even where some of them cannot be prepared.
int warmed = -1;
try { warmed = CodeWarmUp.CompileAll(typeof(UpdateCheck).Assembly); } catch { warmed = -2; }
Check(warmed > 100, $"warm-up compiles the add-on's methods ({warmed})");
Check(CodeWarmUp.CompileAll(typeof(UpdateCheck).Assembly) >= 0, "warm-up can run twice");
Console.WriteLine(failures == 0 ? "ALL PASSED" : $"{failures} FAILED");
return failures;
