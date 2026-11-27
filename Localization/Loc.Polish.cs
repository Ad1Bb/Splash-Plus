using System.Collections.Generic;

namespace SplashAddonPlus
{
    internal static partial class Loc
    {
        private static readonly Dictionary<string, string> Polish = new Dictionary<string, string>
        {
            ["SectionAutoDetect"] = "Wykrywanie gry",
            ["WindowedToggle"] = "Wykrywaj też gry działające w oknie",
            ["WindowedHint"] = "Działa w grach ze znanym folderem instalacji (nie w emulatorach).",
            ["MinimumDisplay"] = "Minimalny czas wyświetlania (s)",
            ["MinimumDisplayHint"] = "Zapobiega krótkiemu mignięciu, gdy gra uruchamia się niemal od razu.",
            ["DetectionDelay"] = "Opóźnienie po wykryciu gry (s)",
            ["DetectionDelayHint"] = "Daje grze czas na przejście przez czarne ekrany i intro po pojawieniu się jej okna.",
            ["IgnoredProcesses"] = "Programy, których okna nigdy nie są grą (jedna nazwa procesu w wierszu)",
            ["IgnoredProcessesHint"] = "Dodaj tu launcher, jeśli zamyka ekran powitalny za wcześnie. Nazwę jego procesu znajdziesz w logu (zob. Diagnostyka).",

            ["SectionClosing"] = "Zamykanie ekranu powitalnego",
            ["CloseWhenAppears"] = "Gdy pojawi się gra (zalecane)",
            ["CloseFixed"] = "Po stałym czasie",
            ["FixedTimeSeconds"] = "Stały czas (s)",
            ["MaximumWait"] = "Maksymalny czas oczekiwania (s)",
            ["MaximumWaitHint"] = "Po tym czasie ekran powitalny zamyka się w każdym przypadku.",
            ["ManualCloseHint"] = "Esc, kliknięcie myszą lub przycisk B na padzie zamyka ekran powitalny w dowolnej chwili.",

            ["SectionAppearance"] = "Wygląd",
            ["StyleLabel"] = "Styl",
            ["StyleClassic"] = "Własny",
            ["StyleClassicHint"] = "Tło i logo gry, ustawiane opcjami poniżej.",
            ["StyleRandom"] = "Losowy",
            ["StyleRandomHint"] = "Inny gotowy styl przy każdym uruchomieniu gry (style Retro tylko z wyboru).",
            ["StyleCinemaHint"] = "Czarne pasy, przesuwające się tło i tytuł odsłaniany jak w czołówce filmu.",
            ["StyleShowcaseHint"] = "Okładka stoi pośrodku grafiki, z odbiciem i połyskiem.",
            ["StyleGalleryHint"] = "Grafika i okładka wiszą obok siebie na ciemnej ścianie w świetle reflektora.",
            ["StylePlinthHint"] = "Okładka wznosi się nad linią horyzontu, na matowej szklanej podłodze pod grafiką.",
            ["StyleDrawerHint"] = "Panel z matowego szkła wysuwa się zza okładki na grafikę.",
            ["StyleBandHint"] = "Pas matowego szkła biegnie przez grafikę, a okładka go przecina.",
            ["StyleTextModeHint"] = "Retro ekran instalatora w trybie tekstowym na rozmytych kolorach gry.",
            ["StyleTextModeDarkHint"] = "Czarny ekran instalatora w trybie tekstowym na rozmytych kolorach gry.",
            ["StylePopFrostHint"] = "Okładka i tytuł pośrodku tafli matowego szkła nad ostrą grafiką.",
            ["StyleSwingFrostHint"] = "Zawieszona okładka i jej tytuł na tafli matowego szkła nad ostrą grafiką.",
            ["StyleMeetFrostHint"] = "Okładka i szczegóły wsuwają się z obu stron na taflę matowego szkła.",
            ["StyleRiseFrostHint"] = "Pas matowego szkła u dołu; okładka wznosi się nad nim i unosi w powietrzu.",
            ["StyleLootDropHint"] = "Okładka spada w snopie światła i obraca się jak karta rzadkiego przedmiotu.",
            ["StyleLockOnHint"] = "Narożniki celownika namierzają okładkę, a obok niej pojawiają się odczyty HUD.",
            ["StyleShowdownHint"] = "Plansza jak przy wejściu bossa: ukośny pas, linie prędkości i uderzający tytuł.",
            ["StyleFocusHint"] = "Grafika gry łapie ostrość, a tytuł leży na pastylce z matowego szkła.",
            ["StyleSlantHint"] = "Dwie równoległe tafle szkła zderzają się, a okładka stoi na czubku jednej z nich.",
            ["StyleIrisGlossHint"] = "Grafika łapie ostrość, a okładka leży w błyszczącym dysku z przydymionego szkła.",
            ["StyleSlateGlossHint"] = "Grafika łapie ostrość, a z lewej wsuwa się błyszcząca tafla z przydymionego szkła.",
            ["Preview"] ="Podgląd",
            ["PreviewTooltip"] = "Pokazuje ekran powitalny z losową grą z biblioteki.",
            ["PreviewNoGames"] = "Dodaj grę do biblioteki, aby zobaczyć podgląd.",
            ["CustomizeStyle"] = "Dostosuj ten styl",
            ["LogoPositionLabel"] = "Położenie logo",
            ["LogoBottomLeft"] = "Na dole po lewej",
            ["LogoBottomCenter"] = "Na dole pośrodku",
            ["LogoBottomRight"] = "Na dole po prawej",
            ["LogoCenter"] = "Pośrodku",
            ["LogoTopLeft"] = "Na górze po lewej",
            ["LogoTopRight"] = "Na górze po prawej",
            ["LogoWidth"] = "Rozmiar logo",
            ["LogoWidthHint"] = "0% ukrywa logo.",
            ["BackgroundDim"] = "Przyciemnienie tła",
            ["VignetteToggle"] = "Przyciemnij krawędzie (winieta)",
            ["ZoomToggle"] = "Powolne przybliżanie tła",

            ["Starting"] = "Uruchamianie...",
            ["PleaseWait"] = "Proszę czekać",

            ["SectionGames"] = "Gry z własnymi ustawieniami",
            ["ExceptionsTip"] = "Gry dodasz, klikając je prawym przyciskiem w bibliotece i otwierając „Splash Plus”.",
            ["GameColumn"] = "Gra",
            ["SplashColumn"] = "Ekran powitalny",
            ["GameFixedTime"] = "Stały czas",
            ["GameSplashOff"] = "Wyłączony",
            ["GameDefault"] = "Domyślny",
            ["NoGames"] = "Na razie brak.",
            ["UnknownGame"] = "Nieznana gra ({0})",
            ["UpdateAvailable"] = "Dostępna jest wersja Splash Plus {0}. Kliknij, aby otworzyć stronę pobierania.",
            ["UpdateAvailableFullscreen"] = "Na GitHubie jest dostępna wersja Splash Plus {0}.",

            ["SectionAdvanced"] = "Zaawansowane",

            ["DesktopSplashSize"] = "Rozmiar ekranu powitalnego w trybie pulpitu",

            ["SizeFullScreen"] = "Pełny ekran",

            ["SizeLarge"] = "Duże okno (75% ekranu)",

            ["SizeSmall"] = "Małe okno (50% ekranu)",

            ["SizePlayniteWindow"] = "Jak okno Playnite",

            ["DesktopSplashSizeHint"] = "Dotyczy tylko trybu pulpitu. Tryb pełnoekranowy się nie zmienia: tam ekran powitalny zawsze zajmuje cały ekran.",
            ["SectionPlatformTimes"] = "Stały czas dla platform",
            ["PlatformTimersToggle"] = "Osobne czasy dla platform",
            ["PlatformColumn"] = "Platforma",
            ["TimeColumn"] = "Czas (s)",
            ["PriorityHint"] = "Kolejność: czas gry, potem czas platformy, potem stały czas.",
            ["AddPlatform"] = "Dodaj platformę",
            ["Remove"] = "Usuń",

            ["SectionDiagnostics"] = "Diagnostyka",
            ["DiagnosticsToggle"] = "Zapisuj szczegóły w pliku logu",
            ["DiagnosticsHint"] = "Włączaj tylko przy szukaniu problemów. Log to %AppData%\\Playnite\\extensions.log (wersja przenośna: w folderze Playnite).",

            ["ErrorDuration"] = "Stały czas musi być większy od 0.",
            ["ErrorLogoSize"] = "Rozmiar logo musi wynosić od 0 do 90%.",
            ["ErrorBackgroundDim"] = "Przyciemnienie tła musi wynosić od 0 do 90%.",
            ["ErrorMinimumDisplay"] = "Minimalny czas wyświetlania nie może być ujemny.",
            ["ErrorDetectionDelay"] = "Opóźnienie po wykryciu gry nie może być ujemne.",
            ["ErrorMaximumWait"] = "Maksymalny czas oczekiwania musi być większy od 0.",
            ["ErrorMaximumBelowMinimum"] = "Maksymalny czas oczekiwania nie może być krótszy niż minimalny czas wyświetlania.",
            ["ErrorGameDuration"] = "Czas dla gry o ID „{0}” musi być większy od 0.",

            ["MenuDisableOne"] = "Wyłącz ekran powitalny dla tej gry",
            ["MenuDisableMany"] = "Wyłącz ekran powitalny dla zaznaczonych gier",
            ["MenuEnable"] = "Włącz ponownie ekran powitalny",
            ["MenuSetTime"] = "Ustaw stały czas wyświetlania…",
            ["MenuRemoveTime"] = "Usuń stały czas",
            ["MenuPreview"] = "Pokaż podgląd ekranu powitalnego",
            ["MenuDefaultStyle"] = "Domyślny styl",
            ["MenuDefaultStylePreview"] = "Podgląd domyślnego stylu",
            ["AskDurationOne"] = "Ile sekund ma trwać ekran powitalny dla „{0}”?",
            ["AskDurationMany"] = "Ile sekund ma trwać ekran powitalny dla zaznaczonych gier?",
            ["AskDurationNote"] = "Automatyczne wykrywanie nie działa w grach ze stałym czasem.",
            ["InvalidNumber"] = "Wpisz liczbę całkowitą większą od zera."
        };
    }
}
