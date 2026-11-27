# Splash Plus

Animated splash screens for [Playnite](https://playnite.link/). Pick a game and Splash Plus fills the screen with its artwork, cover and logo, then steps aside on its own the moment the game is actually on screen.

<p align="center">
  <img src="docs/styles-collage.gif" alt="All Splash Plus styles" width="100%">
</p>

## <a href="https://github.com/Ad1Bb/Splash-Plus/releases/latest"><b>Download the latest release</b></a>

## Features

- **20 styles.** Nineteen ready-made animated designs, from frosted glass and smoked-glass gloss to a retro setup screen and a boss-intro title card, plus a Custom style you set up yourself.
- **Closes when the game is ready.** Instead of a guessed number of seconds, Splash Plus waits for the game's window: exclusive fullscreen, borderless, or (optionally) a windowed game started from its installation folder. A short delay lets the game get past black screens and intros first.
- **Your library's artwork.** Each style uses the game's background, cover and logo, along with details such as the year, developer, genre, playtime or rating. A game without a background gets its blurred cover instead, and a game without a logo shows its name.
- **Best with logos.** The styles look their best with each game's logo downloaded. Logos come from the **Extra Metadata Loader** add-on, so install it too and download logos for your library.
- **Best with standard-size artwork.** Backgrounds and covers close to their original size, such as the ones downloaded from Steam, load almost instantly. Very large files (for example 8K wallpapers or huge PNGs) take longer to read and can delay the artwork on screen.
- **Close it any time.** Press **Esc**, click the mouse or press **B** on a gamepad. The click or button press that launched the game cannot close it by accident. The splash screen also closes by itself when the game exits or its startup is cancelled.
- **Random or a style for each game.** Choose **Random** and every launch gets a different ready-made style, never the same twice in a row. Or give single games their own style from the right-click menu; the rest keep the default.
- **Per-game settings.** Right-click games in your library and open **Splash Plus** to turn the splash screen off for them, give them a fixed display time or their own style, or preview it.
- **Quick style change.** **Extensions → Splash Plus** in Playnite's main menu sets the default style and shows a preview of it with a random game.
- **Fixed time when you need it.** A fixed display time for all games, or a separate one for each platform.
- **Desktop and Fullscreen mode.** The splash screen works in both modes, and so do the settings: every option can be changed right in Fullscreen mode, without going back to Desktop mode.
- **Full screen or a window in Desktop mode.** In Desktop mode the splash screen can also be a centered window with rounded corners, at 75% or 50% of the screen, so the desktop stays in view, or cover exactly the Playnite window, whatever its size and shape. The styles fit narrow windows too. Fullscreen mode always uses the whole screen.
- **Update notice.** When a new version is out, Splash Plus tells you with a Playnite notification. In Desktop mode a click opens the download page.
- **9 languages.** English, German, French, Spanish, Italian, Portuguese (Brazil), Polish, Russian and Chinese (Simplified). Splash Plus follows Playnite's language setting.

## Lightweight

- **Nothing runs in the background.** Between games there are no timers, threads or polling. The only other work is one quick look for a new version when Playnite starts, at most once a day. Splash Plus only waits for Playnite to report that a game is starting. The whole add-on is a single ~360 KB DLL.
- **Smooth animations.** Images are decoded off the UI thread, at screen resolution only. Every blur, shadow and glow is rendered once, so the animations only move, scale and fade finished images, composited on the GPU.
- **Tiny checks while it waits.** Looking for the game's window takes a fraction of a millisecond, four times a second. The gamepad is checked through XInput.
- **Gone without a trace.** When the splash screen closes, every timer and animation stops and the images are released. If the game is never detected, a safety limit (60 seconds by default) closes it anyway.
- **Robust.** A problem with a style or an image is caught and logged. It never takes down the splash screen or Playnite.

## Styles

Every style is chosen in the add-on settings, each with a **Preview** button that shows it with a random game from your library.

<table>
  <tr>
    <td width="50%"><img src="docs/styles/band.jpg" alt="Band"><br><b>Band</b><br>A frosted strip spans the artwork; the cover cuts across it.</td>
    <td width="50%"><img src="docs/styles/cinema.jpg" alt="Cinema"><br><b>Cinema</b><br>Letterbox bars, a drifting backdrop and a title revealed like a film opening.</td>
  </tr>
  <tr>
    <td><img src="docs/styles/clasp.jpg" alt="Clasp"><br><b>Clasp</b><br>The cover and details slide in from both sides onto a frosted glass pane.</td>
    <td><img src="docs/styles/drawer.jpg" alt="Drawer"><br><b>Drawer</b><br>A frosted glass panel slides out from behind the cover over the artwork.</td>
  </tr>
  <tr>
    <td><img src="docs/styles/focus.jpg" alt="Focus"><br><b>Focus</b><br>The game's art pulls into focus; a frosted glass pill holds the title.</td>
    <td><img src="docs/styles/gallery.jpg" alt="Gallery"><br><b>Gallery</b><br>The artwork and the cover hang side by side on a dark wall under a spotlight.</td>
  </tr>
  <tr>
    <td><img src="docs/styles/iris.jpg" alt="Iris"><br><b>Iris</b><br>The art pulls into focus; the cover sits in a glossy smoked-glass disc.</td>
    <td><img src="docs/styles/ledge.jpg" alt="Ledge"><br><b>Ledge</b><br>A frosted glass strip along the bottom; the cover rises above it and hovers.</td>
  </tr>
  <tr>
    <td><img src="docs/styles/lock-on.jpg" alt="Lock-On"><br><b>Lock-On</b><br>Targeting brackets lock onto the cover, with HUD readouts beside it.</td>
    <td><img src="docs/styles/loot-drop.jpg" alt="Loot Drop"><br><b>Loot Drop</b><br>The cover drops in a beam of light and flips like a rare item card.</td>
  </tr>
  <tr>
    <td><img src="docs/styles/pane.jpg" alt="Pane"><br><b>Pane</b><br>A centered cover and title on a frosted glass pane over the sharp artwork.</td>
    <td><img src="docs/styles/pendant.jpg" alt="Pendant"><br><b>Pendant</b><br>A hanging cover and its title on one frosted glass pane over the sharp artwork.</td>
  </tr>
  <tr>
    <td><img src="docs/styles/plinth.jpg" alt="Plinth"><br><b>Plinth</b><br>The cover rises over a horizon line onto a frosted floor below the artwork.</td>
    <td><img src="docs/styles/retro-black.jpg" alt="Retro Black"><br><b>Retro Black</b><br>A black text-mode setup screen over a blur of the game's colors.</td>
  </tr>
  <tr>
    <td><img src="docs/styles/retro-gray.jpg" alt="Retro Gray"><br><b>Retro Gray</b><br>A retro text-mode setup screen over a blur of the game's colors.</td>
    <td><img src="docs/styles/showcase.jpg" alt="Showcase"><br><b>Showcase</b><br>The cover stands in the middle of the artwork with a reflection and a sheen.</td>
  </tr>
  <tr>
    <td><img src="docs/styles/showdown.jpg" alt="Showdown"><br><b>Showdown</b><br>A boss-intro title card: a diagonal band, speed lines and a slamming title.</td>
    <td><img src="docs/styles/slant.jpg" alt="Slant"><br><b>Slant</b><br>Two parallel glass slabs knock together; the cover sits at one tip.</td>
  </tr>
  <tr>
    <td><img src="docs/styles/slate.jpg" alt="Slate"><br><b>Slate</b><br>The art pulls into focus; a glossy smoked-glass slate slides in from the left.</td>
    <td><img src="docs/styles/custom.jpg" alt="Custom"><br><b>Custom</b><br>The game's background and logo, set up your way: the logo in one of six positions (corners, bottom center or center) at 0–90% of the screen width, background dimming, a vignette and a slow zoom.</td>
  </tr>
</table>

## Settings

<p align="center">
  <img src="docs/options.png" alt="Splash Plus settings" width="760">
</p>

- **Desktop mode:** open **Add-ons… → Extension settings → Generic → Splash Plus**.
- **Fullscreen mode:** open **Extensions → Splash Plus**. All the same options are there.

- **Appearance:** the style (or Random), with a preview. Custom shows its own options here.
- **Close the splash screen:** when the game appears (recommended) or after a fixed time.
- **Games with their own settings:** games you turned off, gave a fixed time or their own style from the library's right-click menu, listed by name.
- **Advanced:**
  - the splash screen size in Desktop mode (full screen, a window at 75% or 50% of the screen, or the same as the Playnite window);
  - the maximum waiting time;
  - the minimum display time and the delay after the game is detected;
  - detection of windowed games;
  - programs whose windows are never the game (for launchers that fill the screen before the game starts);
  - fixed times per platform;
  - diagnostic logging.

## Installation

1. Download the latest `.pext` file from [Releases](https://github.com/Ad1Bb/Splash-Plus/releases/latest).
2. Double-click it, or drag it onto the Playnite window, and confirm the installation.
3. Restart Playnite.

Requires Playnite 10. Turn off any other splash-screen add-on, otherwise two splash screens will appear.

**Recommended:** install the **Extra Metadata Loader** add-on and download logos for your games. Splash Plus works without them (games without a logo show their name instead), but every style looks its best with the game's logo. For the fastest start, keep backgrounds and covers close to their original size, such as those downloaded from Steam, rather than very large wallpapers.

## Good to know

- **Pop-ups during startup stay under the splash screen.** If something shows a window while the game is starting (an update prompt, a login request, a runtime installer), close the splash screen with **Esc**, a click or **B**. If it happens every time with one game, turn the splash screen off for it or give it a fixed time.
- **Launchers that fill the screen** before the game can close the splash screen too early. Add their process name to *Programs whose windows are never the game*.
- **Games without a large window** (a small window, or only a launcher that stays open) are not detected; the splash screen closes after the maximum waiting time or by hand.
- **Gamepads:** XInput controllers. PlayStation controllers work through a tool such as DS4Windows.

## Troubleshooting

If the splash screen closes too early, too late, or not at all for a particular game:

1. Open the settings, expand **Advanced** and turn on **Write details to the log file**.
2. Start the game.
3. Open `%AppData%\Playnite\extensions.log` (for a portable installation, the file is in the Playnite folder) and search for `splash`. The log shows which windows appeared while the game was loading and why the splash screen closed.

Turn the option off again afterwards. Feel free to attach these lines to an [issue](https://github.com/Ad1Bb/Splash-Plus/issues).

## Building

The project is an SDK-style C# project targeting .NET Framework 4.8. The Playnite SDK and the reference assemblies come from NuGet, so only the .NET SDK is needed. The settings page and all styles are built in code (no XAML), so the project also builds on Linux and macOS.

```
dotnet build SplashAddonPlus.csproj -c Release
```

The output is `bin/Release/SplashAddonPlus.dll`. A `.pext` package is a ZIP archive containing `SplashAddonPlus.dll`, `extension.yaml` and `icon.png`.

The tests cover everything that does not need Windows: settings and their migration, detection rules, texts and translations, image sizes and the gamepad logic. They run on .NET 8 against the built DLL:

```
dotnet run --project Tests -c Release
```

GitHub Actions builds the add-on, runs the tests and creates the `.pext` package on every push. Pushing a version tag (for example `2.3`) publishes a release with the package attached.

---

*Started as a fork of [Splash Addon](https://github.com/artzox/Playnite-Splash-Addon) by Artzox.*

## <a href="https://github.com/Ad1Bb/Splash-Plus/releases/latest"><b>Download the latest release</b></a>

