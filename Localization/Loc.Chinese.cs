using System.Collections.Generic;

namespace SplashAddonPlus
{
    internal static partial class Loc
    {
        // Simplified Chinese (zh_CN).
        private static readonly Dictionary<string, string> Chinese = new Dictionary<string, string>
        {
            ["SectionAutoDetect"] = "游戏检测",
            ["WindowedToggle"] = "同时检测以窗口模式运行的游戏",
            ["WindowedHint"] = "适用于安装文件夹已知的游戏（不适用于模拟器）。",
            ["MinimumDisplay"] = "最短显示时间（秒）",
            ["MinimumDisplayHint"] = "防止游戏几乎立即启动时画面一闪而过。",
            ["DetectionDelay"] = "检测到游戏后的延迟（秒）",
            ["DetectionDelayHint"] = "游戏窗口出现后，留出时间让游戏度过黑屏和片头。",
            ["IgnoredProcesses"] = "其窗口绝不是游戏的程序（每行一个进程名）",
            ["IgnoredProcessesHint"] = "如果某个启动器过早关闭了启动画面，请将其添加到这里。其进程名可在日志中找到（见“诊断”）。",

            ["SectionClosing"] = "关闭启动画面",
            ["CloseWhenAppears"] = "游戏出现时（推荐）",
            ["CloseFixed"] = "固定时间后",
            ["FixedTimeSeconds"] = "固定时间（秒）",
            ["MaximumWait"] = "最长等待时间（秒）",
            ["MaximumWaitHint"] = "超过此时间后，启动画面无论如何都会关闭。",
            ["ManualCloseHint"] = "随时可按 Esc、点击鼠标或按手柄的 B 键关闭启动画面。",

            ["SectionAppearance"] = "外观",
            ["StyleLabel"] = "样式",
            ["StyleClassic"] = "自定义",
            ["StyleClassicHint"] = "游戏的背景和标志，可通过下方选项设置。",
            ["StyleCinemaHint"] = "宽银幕黑边、缓缓移动的背景，标题像电影片头一样显现。",
            ["StyleShowcaseHint"] = "封面立于美术图中央，带有倒影和光泽。",
            ["StyleGalleryHint"] = "美术图和封面并排挂在聚光灯下的深色墙上。",
            ["StylePlinthHint"] = "封面越过地平线升起，立于美术图下方的磨砂地面上。",
            ["StyleDrawerHint"] = "磨砂玻璃面板从封面后方滑出，覆盖在美术图上。",
            ["StyleBandHint"] = "一条磨砂玻璃带横跨美术图，封面穿过其中。",
            ["StyleTextModeHint"] = "复古文字模式安装界面，背景为游戏色彩的模糊效果。",
            ["StyleTextModeDarkHint"] = "黑色文字模式安装界面，背景为游戏色彩的模糊效果。",
            ["StylePopFrostHint"] = "封面和标题居中于磨砂玻璃面板上，下方是清晰的美术图。",
            ["StyleSwingFrostHint"] = "悬挂的封面及其标题位于磨砂玻璃面板上，下方是清晰的美术图。",
            ["StyleMeetFrostHint"] = "封面和详细信息从两侧滑入磨砂玻璃面板。",
            ["StyleRiseFrostHint"] = "底部一条磨砂玻璃带，封面从其上方升起并悬浮。",
            ["StyleLootDropHint"] = "封面随光柱落下，像稀有物品卡一样翻转。",
            ["StyleLockOnHint"] = "瞄准框锁定封面，旁边显示 HUD 数据。",
            ["StyleShowdownHint"] = "Boss 登场式标题卡：斜向条带、速度线和重击而出的标题。",
            ["StyleFocusHint"] = "游戏美术图逐渐对焦，磨砂玻璃胶囊中显示标题。",
            ["StyleSlantHint"] = "两块平行的玻璃板相互碰撞，封面位于其中一块的尖端。",
            ["StyleIrisGlossHint"] = "美术图逐渐对焦，封面嵌在光亮的烟熏玻璃圆盘中。",
            ["StyleSlateGlossHint"] = "美术图逐渐对焦，一块光亮的烟熏玻璃面板从左侧滑入。",
            ["Preview"] ="预览",
            ["PreviewTooltip"] = "使用游戏库中随机一款游戏显示启动画面。",
            ["PreviewNoGames"] = "向游戏库添加游戏后即可预览。",
            ["CustomizeStyle"] = "自定义此样式",
            ["LogoPositionLabel"] = "标志位置",
            ["LogoBottomLeft"] = "左下",
            ["LogoBottomCenter"] = "底部居中",
            ["LogoBottomRight"] = "右下",
            ["LogoCenter"] = "居中",
            ["LogoTopLeft"] = "左上",
            ["LogoTopRight"] = "右上",
            ["LogoWidth"] = "标志大小",
            ["LogoWidthHint"] = "设为 0% 时隐藏标志。",
            ["BackgroundDim"] = "背景变暗",
            ["VignetteToggle"] = "边缘变暗（暗角）",
            ["ZoomToggle"] = "背景缓慢放大",

            ["Starting"] = "正在启动...",
            ["PleaseWait"] = "请稍候",

            ["SectionGames"] = "单独设置的游戏",
            ["ExceptionsTip"] = "在游戏库中右键单击游戏并打开“Splash Plus”即可添加。",
            ["GameColumn"] = "游戏",
            ["SplashColumn"] = "启动画面",
            ["GameFixedTime"] = "固定时间",
            ["GameSplashOff"] = "关闭",
            ["NoGames"] = "暂无。",
            ["UnknownGame"] = "未知游戏（{0}）",
            ["UpdateAvailable"] = "Splash Plus {0} 已发布。点击打开下载页面。",
            ["UpdateAvailableFullscreen"] = "Splash Plus {0} 已在 GitHub 上发布。",

            ["SectionAdvanced"] = "高级",

            ["DesktopSplashSize"] = "桌面模式下的启动画面大小",

            ["SizeFullScreen"] = "全屏",

            ["SizeLarge"] = "大窗口（屏幕的 75%）",

            ["SizeSmall"] = "小窗口（屏幕的 50%）",

            ["SizePlayniteWindow"] = "与 Playnite 窗口相同",

            ["DesktopSplashSizeHint"] = "仅适用于桌面模式。全屏模式不受影响：在全屏模式下启动画面始终占满整个屏幕。",
            ["SectionPlatformTimes"] = "按平台设置固定时间",
            ["PlatformTimersToggle"] = "为各平台单独设置时间",
            ["PlatformColumn"] = "平台",
            ["TimeColumn"] = "时间（秒）",
            ["PriorityHint"] = "优先级：游戏自身的时间，其次是平台时间，最后是固定时间。",
            ["AddPlatform"] = "添加平台",
            ["Remove"] = "移除",

            ["SectionDiagnostics"] = "诊断",
            ["DiagnosticsToggle"] = "将详细信息写入日志文件",
            ["DiagnosticsHint"] = "仅在排查问题时开启。日志位于 %AppData%\\Playnite\\extensions.log（便携版：位于 Playnite 文件夹中）。",

            ["ErrorDuration"] = "固定时间必须大于 0。",
            ["ErrorLogoSize"] = "标志大小必须在 0 到 90% 之间。",
            ["ErrorBackgroundDim"] = "背景变暗程度必须在 0 到 90% 之间。",
            ["ErrorMinimumDisplay"] = "最短显示时间不能为负数。",
            ["ErrorDetectionDelay"] = "检测到游戏后的延迟不能为负数。",
            ["ErrorMaximumWait"] = "最长等待时间必须大于 0。",
            ["ErrorMaximumBelowMinimum"] = "最长等待时间不能短于最短显示时间。",
            ["ErrorGameDuration"] = "游戏 ID“{0}”的时间必须大于 0。",

            ["MenuDisableOne"] = "关闭此游戏的启动画面",
            ["MenuDisableMany"] = "关闭所选游戏的启动画面",
            ["MenuEnable"] = "重新开启启动画面",
            ["MenuSetTime"] = "设置固定显示时间…",
            ["MenuRemoveTime"] = "移除固定时间",
            ["MenuPreview"] = "显示启动画面预览",
            ["AskDurationOne"] = "“{0}”的启动画面应持续多少秒？",
            ["AskDurationMany"] = "所选游戏的启动画面应持续多少秒？",
            ["AskDurationNote"] = "设置了固定时间的游戏不使用自动检测。",
            ["InvalidNumber"] = "请输入大于零的整数。"
        };
    }
}
