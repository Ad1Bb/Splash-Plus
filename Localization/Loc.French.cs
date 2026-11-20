using System.Collections.Generic;

namespace SplashAddonPlus
{
    internal static partial class Loc
    {
        private static readonly Dictionary<string, string> French = new Dictionary<string, string>
        {
            ["SectionAutoDetect"] = "Détection du jeu",
            ["WindowedToggle"] = "Détecter aussi les jeux en mode fenêtré",
            ["WindowedHint"] = "Fonctionne pour les jeux dont le dossier d'installation est connu (pas pour les émulateurs).",
            ["MinimumDisplay"] = "Durée d'affichage minimale (s)",
            ["MinimumDisplayHint"] = "Évite un bref flash quand un jeu démarre presque instantanément.",
            ["DetectionDelay"] = "Délai après la détection du jeu (s)",
            ["DetectionDelayHint"] = "Laisse au jeu le temps de passer les écrans noirs et les intros après l'apparition de sa fenêtre.",
            ["IgnoredProcesses"] = "Programmes dont les fenêtres ne sont jamais le jeu (un nom de processus par ligne)",
            ["IgnoredProcessesHint"] = "Ajoutez ici un lanceur s'il ferme l'écran de démarrage trop tôt. Son nom de processus figure dans le journal (voir Diagnostic).",

            ["SectionClosing"] = "Fermer l'écran de démarrage",
            ["CloseWhenAppears"] = "Quand le jeu apparaît (recommandé)",
            ["CloseFixed"] = "Après une durée fixe",
            ["FixedTimeSeconds"] = "Durée fixe (s)",
            ["MaximumWait"] = "Temps d'attente maximal (s)",
            ["MaximumWaitHint"] = "Passé ce délai, l'écran de démarrage se ferme dans tous les cas.",
            ["ManualCloseHint"] = "Échap, un clic de souris ou le bouton B de la manette ferment l'écran de démarrage à tout moment.",

            ["SectionAppearance"] = "Apparence",
            ["StyleLabel"] = "Style",
            ["StyleClassic"] = "Personnalisé",
            ["StyleClassicHint"] = "L'arrière-plan et le logo du jeu, réglés avec les options ci-dessous.",
            ["StyleCinemaHint"] = "Bandes noires, arrière-plan qui glisse et titre révélé comme au générique d'un film.",
            ["StyleShowcaseHint"] = "La jaquette se dresse au milieu de l'illustration, avec un reflet et un éclat.",
            ["StyleGalleryHint"] = "L'illustration et la jaquette sont accrochées côte à côte sur un mur sombre, sous un projecteur.",
            ["StylePlinthHint"] = "La jaquette s'élève au-dessus d'une ligne d'horizon, sur un sol en verre dépoli sous l'illustration.",
            ["StyleDrawerHint"] = "Un panneau en verre dépoli glisse de derrière la jaquette par-dessus l'illustration.",
            ["StyleBandHint"] = "Une bande de verre dépoli traverse l'illustration ; la jaquette la coupe.",
            ["StyleTextModeHint"] = "Écran d'installation rétro en mode texte sur un flou des couleurs du jeu.",
            ["StyleTextModeDarkHint"] = "Écran d'installation noir en mode texte sur un flou des couleurs du jeu.",
            ["StylePopFrostHint"] = "Jaquette et titre centrés sur une vitre en verre dépoli, au-dessus de l'illustration nette.",
            ["StyleSwingFrostHint"] = "Une jaquette suspendue et son titre sur une vitre en verre dépoli, au-dessus de l'illustration nette.",
            ["StyleMeetFrostHint"] = "La jaquette et les détails arrivent des deux côtés sur une vitre en verre dépoli.",
            ["StyleRiseFrostHint"] = "Une bande de verre dépoli en bas ; la jaquette s'élève au-dessus et flotte.",
            ["StyleLootDropHint"] = "La jaquette tombe dans un rayon de lumière et se retourne comme une carte d'objet rare.",
            ["StyleLockOnHint"] = "Des repères de visée se verrouillent sur la jaquette ; des données HUD à côté.",
            ["StyleShowdownHint"] = "Carte d'entrée de boss : une bande en diagonale, des lignes de vitesse et un titre qui s'abat.",
            ["StyleFocusHint"] = "L'illustration du jeu fait la mise au point ; une pastille en verre dépoli porte le titre.",
            ["StyleSlantHint"] = "Deux plaques de verre parallèles se heurtent ; la jaquette se tient à une pointe.",
            ["StyleIrisGlossHint"] = "L'illustration fait la mise au point ; la jaquette repose dans un disque de verre fumé brillant.",
            ["StyleSlateGlossHint"] = "L'illustration fait la mise au point ; une plaque de verre fumé brillant glisse depuis la gauche.",
            ["Preview"] ="Aperçu",
            ["PreviewTooltip"] = "Affiche l'écran de démarrage avec un jeu aléatoire de votre bibliothèque.",
            ["PreviewNoGames"] = "Ajoutez un jeu à votre bibliothèque pour voir un aperçu.",
            ["CustomizeStyle"] = "Personnaliser ce style",
            ["LogoPositionLabel"] = "Position du logo",
            ["LogoBottomLeft"] = "En bas à gauche",
            ["LogoBottomCenter"] = "En bas au centre",
            ["LogoBottomRight"] = "En bas à droite",
            ["LogoCenter"] = "Au centre",
            ["LogoTopLeft"] = "En haut à gauche",
            ["LogoTopRight"] = "En haut à droite",
            ["LogoWidth"] = "Taille du logo",
            ["LogoWidthHint"] = "0 % masque le logo.",
            ["BackgroundDim"] = "Assombrissement de l'arrière-plan",
            ["VignetteToggle"] = "Assombrir les bords (vignette)",
            ["ZoomToggle"] = "Zoom lent sur l'arrière-plan",

            ["Starting"] = "Démarrage...",
            ["PleaseWait"] = "Veuillez patienter",

            ["SectionGames"] = "Jeux avec leurs propres réglages",
            ["ExceptionsTip"] = "Ajoutez des jeux par un clic droit dans la bibliothèque, puis « Splash Plus ».",
            ["GameColumn"] = "Jeu",
            ["SplashColumn"] = "Écran de démarrage",
            ["GameFixedTime"] = "Durée fixe",
            ["GameSplashOff"] = "Désactivé",
            ["NoGames"] = "Aucun pour l'instant.",
            ["UnknownGame"] = "Jeu inconnu ({0})",

            ["SectionAdvanced"] = "Avancé",

            ["DesktopSplashSize"] = "Taille de l'écran de démarrage en mode Bureau",

            ["SizeFullScreen"] = "Plein écran",

            ["SizeLarge"] = "Grande fenêtre (75 % de l'écran)",

            ["SizeSmall"] = "Petite fenêtre (50 % de l'écran)",

            ["DesktopSplashSizeHint"] = "Uniquement pour le mode Bureau. Le mode Plein écran n'est pas concerné : l'écran de démarrage y occupe toujours tout l'écran.",
            ["SectionPlatformTimes"] = "Durée fixe par plateforme",
            ["PlatformTimersToggle"] = "Durées distinctes par plateforme",
            ["PlatformColumn"] = "Plateforme",
            ["TimeColumn"] = "Durée (s)",
            ["PriorityHint"] = "Priorité : la durée du jeu, puis celle de la plateforme, puis la durée fixe.",
            ["AddPlatform"] = "Ajouter une plateforme",
            ["Remove"] = "Supprimer",

            ["SectionDiagnostics"] = "Diagnostic",
            ["DiagnosticsToggle"] = "Écrire les détails dans le fichier journal",
            ["DiagnosticsHint"] = "À activer uniquement en cas de problème. Le journal se trouve dans %AppData%\\Playnite\\extensions.log (version portable : dans le dossier de Playnite).",

            ["ErrorDuration"] = "La durée fixe doit être supérieure à 0.",
            ["ErrorLogoSize"] = "La taille du logo doit être comprise entre 0 et 90 %.",
            ["ErrorBackgroundDim"] = "L'assombrissement de l'arrière-plan doit être compris entre 0 et 90 %.",
            ["ErrorMinimumDisplay"] = "La durée d'affichage minimale ne peut pas être négative.",
            ["ErrorDetectionDelay"] = "Le délai après la détection du jeu ne peut pas être négatif.",
            ["ErrorMaximumWait"] = "Le temps d'attente maximal doit être supérieur à 0.",
            ["ErrorMaximumBelowMinimum"] = "Le temps d'attente maximal ne peut pas être inférieur à la durée d'affichage minimale.",
            ["ErrorGameDuration"] = "La durée pour l'ID de jeu « {0} » doit être supérieure à 0.",

            ["MenuDisableOne"] = "Désactiver l'écran de démarrage pour ce jeu",
            ["MenuDisableMany"] = "Désactiver l'écran de démarrage pour les jeux sélectionnés",
            ["MenuEnable"] = "Réactiver l'écran de démarrage",
            ["MenuSetTime"] = "Définir une durée d'affichage fixe…",
            ["MenuRemoveTime"] = "Supprimer la durée fixe",
            ["MenuPreview"] = "Afficher un aperçu de l'écran de démarrage",
            ["AskDurationOne"] = "Combien de secondes l'écran de démarrage doit-il durer pour « {0} » ?",
            ["AskDurationMany"] = "Combien de secondes l'écran de démarrage doit-il durer pour les jeux sélectionnés ?",
            ["AskDurationNote"] = "La détection automatique n'est pas utilisée pour les jeux à durée fixe.",
            ["InvalidNumber"] = "Saisissez un nombre entier supérieur à zéro."
        };
    }
}
