using System.Collections.Generic;

namespace SplashAddonPlus
{
    internal static partial class Loc
    {
        private static readonly Dictionary<string, string> German = new Dictionary<string, string>
        {
            ["SectionAutoDetect"] = "Spielerkennung",
            ["WindowedToggle"] = "Auch Spiele im Fenstermodus erkennen",
            ["WindowedHint"] = "Funktioniert bei Spielen mit bekanntem Installationsordner (nicht bei Emulatoren).",
            ["MinimumDisplay"] = "Mindestanzeigedauer (s)",
            ["MinimumDisplayHint"] = "Verhindert ein kurzes Aufblitzen, wenn ein Spiel fast sofort startet.",
            ["DetectionDelay"] = "Verzögerung nach dem Erkennen des Spiels (s)",
            ["DetectionDelayHint"] = "Gibt dem Spiel Zeit, schwarze Bildschirme und Intros hinter sich zu lassen, nachdem sein Fenster erscheint.",
            ["IgnoredProcesses"] = "Programme, deren Fenster nie das Spiel sind (ein Prozessname pro Zeile)",
            ["IgnoredProcessesHint"] = "Hier einen Launcher eintragen, wenn er den Splash-Screen zu früh schließt. Sein Prozessname steht im Log (siehe Diagnose).",

            ["SectionClosing"] = "Splash-Screen schließen",
            ["CloseWhenAppears"] = "Wenn das Spiel erscheint (empfohlen)",
            ["CloseFixed"] = "Nach einer festen Zeit",
            ["FixedTimeSeconds"] = "Feste Zeit (s)",
            ["MaximumWait"] = "Maximale Wartezeit (s)",
            ["MaximumWaitHint"] = "Nach dieser Zeit schließt sich der Splash-Screen in jedem Fall.",
            ["ManualCloseHint"] = "Esc, ein Mausklick oder die B-Taste am Gamepad schließt den Splash-Screen jederzeit.",

            ["SectionAppearance"] = "Darstellung",
            ["StyleLabel"] = "Stil",
            ["StyleClassic"] = "Benutzerdefiniert",
            ["StyleClassicHint"] = "Hintergrund und Logo des Spiels, eingestellt mit den Optionen unten.",
            ["StyleCinemaHint"] = "Kinobalken, ein gleitender Hintergrund und ein Titel, der wie im Vorspann eines Films erscheint.",
            ["StyleShowcaseHint"] = "Das Cover steht mitten im Artwork, mit Spiegelung und Glanz.",
            ["StyleGalleryHint"] = "Artwork und Cover hängen nebeneinander an einer dunklen Wand im Spotlicht.",
            ["StylePlinthHint"] = "Das Cover steigt über eine Horizontlinie auf einen Milchglasboden unter dem Artwork.",
            ["StyleDrawerHint"] = "Ein Milchglaspanel gleitet hinter dem Cover hervor über das Artwork.",
            ["StyleBandHint"] = "Ein Milchglasstreifen zieht sich über das Artwork; das Cover durchschneidet ihn.",
            ["StyleTextModeHint"] = "Retro-Setup-Bildschirm im Textmodus über den verschwommenen Farben des Spiels.",
            ["StyleTextModeDarkHint"] = "Schwarzer Setup-Bildschirm im Textmodus über den verschwommenen Farben des Spiels.",
            ["StylePopFrostHint"] = "Cover und Titel zentriert auf einer Milchglasscheibe über dem scharfen Artwork.",
            ["StyleSwingFrostHint"] = "Ein hängendes Cover und sein Titel auf einer Milchglasscheibe über dem scharfen Artwork.",
            ["StyleMeetFrostHint"] = "Cover und Details gleiten von beiden Seiten auf eine Milchglasscheibe.",
            ["StyleRiseFrostHint"] = "Ein Milchglasstreifen am unteren Rand; das Cover steigt darüber auf und schwebt.",
            ["StyleLootDropHint"] = "Das Cover fällt in einem Lichtstrahl herab und dreht sich um wie eine seltene Gegenstandskarte.",
            ["StyleLockOnHint"] = "Zielmarkierungen erfassen das Cover; daneben HUD-Anzeigen.",
            ["StyleShowdownHint"] = "Boss-Intro als Titelkarte: ein schräges Band, Speedlines und ein einschlagender Titel.",
            ["StyleFocusHint"] = "Das Artwork des Spiels wird scharf gestellt; eine Milchglas-Kapsel trägt den Titel.",
            ["StyleSlantHint"] = "Zwei parallele Glasplatten stoßen zusammen; das Cover sitzt an einer Spitze.",
            ["StyleIrisGlossHint"] = "Das Artwork wird scharf gestellt; das Cover sitzt in einer glänzenden Rauchglasscheibe.",
            ["StyleSlateGlossHint"] = "Das Artwork wird scharf gestellt; eine glänzende Rauchglastafel gleitet von links herein.",
            ["Preview"] ="Vorschau",
            ["PreviewTooltip"] = "Zeigt den Splash-Screen mit einem zufälligen Spiel aus der Bibliothek.",
            ["PreviewNoGames"] = "Für eine Vorschau muss die Bibliothek mindestens ein Spiel enthalten.",
            ["CustomizeStyle"] = "Diesen Stil anpassen",
            ["LogoPositionLabel"] = "Logoposition",
            ["LogoBottomLeft"] = "Unten links",
            ["LogoBottomCenter"] = "Unten mittig",
            ["LogoBottomRight"] = "Unten rechts",
            ["LogoCenter"] = "Mitte",
            ["LogoTopLeft"] = "Oben links",
            ["LogoTopRight"] = "Oben rechts",
            ["LogoWidth"] = "Logogröße",
            ["LogoWidthHint"] = "Bei 0% wird das Logo ausgeblendet.",
            ["BackgroundDim"] = "Hintergrund abdunkeln",
            ["VignetteToggle"] = "Ränder abdunkeln (Vignette)",
            ["ZoomToggle"] = "Langsamer Zoom auf den Hintergrund",

            ["Starting"] = "Wird gestartet...",
            ["PleaseWait"] = "Bitte warten",

            ["SectionGames"] = "Spiele mit eigenen Einstellungen",
            ["ExceptionsTip"] = "Spiele in der Bibliothek mit der rechten Maustaste anklicken und „Splash Plus“ öffnen, um sie hinzuzufügen.",
            ["GameColumn"] = "Spiel",
            ["SplashColumn"] = "Splash-Screen",
            ["GameFixedTime"] = "Feste Zeit",
            ["GameSplashOff"] = "Aus",
            ["NoGames"] = "Noch keine.",
            ["UnknownGame"] = "Unbekanntes Spiel ({0})",
            ["UpdateAvailable"] = "Splash Plus {0} ist verfügbar. Klicke hier, um die Download-Seite zu öffnen.",
            ["UpdateAvailableFullscreen"] = "Splash Plus {0} ist auf GitHub verfügbar.",

            ["SectionAdvanced"] = "Erweitert",

            ["DesktopSplashSize"] = "Größe des Splash-Screens im Desktop-Modus",

            ["SizeFullScreen"] = "Vollbild",

            ["SizeLarge"] = "Großes Fenster (75% des Bildschirms)",

            ["SizeSmall"] = "Kleines Fenster (50% des Bildschirms)",

            ["SizePlayniteWindow"] = "Wie das Playnite-Fenster",

            ["DesktopSplashSizeHint"] = "Nur für den Desktop-Modus. Der Vollbildmodus bleibt unverändert: Dort füllt der Splash-Screen immer den ganzen Bildschirm.",
            ["SectionPlatformTimes"] = "Feste Zeit pro Plattform",
            ["PlatformTimersToggle"] = "Eigene Zeiten pro Plattform",
            ["PlatformColumn"] = "Plattform",
            ["TimeColumn"] = "Zeit (s)",
            ["PriorityHint"] = "Priorität: die Zeit des Spiels, dann die der Plattform, dann die feste Zeit.",
            ["AddPlatform"] = "Plattform hinzufügen",
            ["Remove"] = "Entfernen",

            ["SectionDiagnostics"] = "Diagnose",
            ["DiagnosticsToggle"] = "Details in die Logdatei schreiben",
            ["DiagnosticsHint"] = "Nur zur Fehlersuche einschalten. Das Log liegt unter %AppData%\\Playnite\\extensions.log (portable Version: im Playnite-Ordner).",

            ["ErrorDuration"] = "Die feste Zeit muss größer als 0 sein.",
            ["ErrorLogoSize"] = "Die Logogröße muss zwischen 0 und 90% liegen.",
            ["ErrorBackgroundDim"] = "Die Abdunklung des Hintergrunds muss zwischen 0 und 90% liegen.",
            ["ErrorMinimumDisplay"] = "Die Mindestanzeigedauer darf nicht negativ sein.",
            ["ErrorDetectionDelay"] = "Die Verzögerung nach dem Erkennen des Spiels darf nicht negativ sein.",
            ["ErrorMaximumWait"] = "Die maximale Wartezeit muss größer als 0 sein.",
            ["ErrorMaximumBelowMinimum"] = "Die maximale Wartezeit darf nicht kürzer als die Mindestanzeigedauer sein.",
            ["ErrorGameDuration"] = "Die Zeit für die Spiel-ID „{0}“ muss größer als 0 sein.",

            ["MenuDisableOne"] = "Splash-Screen für dieses Spiel ausschalten",
            ["MenuDisableMany"] = "Splash-Screen für die ausgewählten Spiele ausschalten",
            ["MenuEnable"] = "Splash-Screen wieder einschalten",
            ["MenuSetTime"] = "Feste Anzeigedauer festlegen…",
            ["MenuRemoveTime"] = "Feste Zeit entfernen",
            ["MenuPreview"] = "Vorschau des Splash-Screens zeigen",
            ["AskDurationOne"] = "Wie viele Sekunden soll der Splash-Screen für „{0}“ angezeigt werden?",
            ["AskDurationMany"] = "Wie viele Sekunden soll der Splash-Screen für die ausgewählten Spiele angezeigt werden?",
            ["AskDurationNote"] = "Bei Spielen mit fester Zeit wird die automatische Erkennung nicht verwendet.",
            ["InvalidNumber"] = "Bitte eine ganze Zahl größer als null eingeben."
        };
    }
}
