using System;
using System.Numerics;
using AutoTripleTriadGrind.Windows.Pages;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace AutoTripleTriadGrind.Windows.Shell;

public sealed class AppWindow : Window, IDisposable
{
    public enum Page
    {
        Triad,
        Settings,
        History,
        Plugins,
        Log,
        Changelog,
        About
    }

    private const float PageRevealMs = 260f;

    private const float PageSlide = 12f;

    private const float CollapseMs = 280f;

    private const float GripSize = 12f;

    private const float GripInset = 4f;

    private const float MinimumBodyHeight = 260f;

    private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse;

    private static readonly Vector2 DefaultSize = new(1040f, 780f);

    private static readonly Vector2 Unbounded = new(float.MaxValue, float.MaxValue);

    private static readonly WindowSizeConstraints CompactConstraints = new()
    {
        MinimumSize = new Vector2(HeaderBar.MinimumWidth, Layout.HeaderHeight),
        MaximumSize = Unbounded
    };

    private static readonly WindowSizeConstraints ExpandedConstraints = new()
    {
        MinimumSize = new Vector2(HeaderBar.MinimumWidth, Layout.HeaderHeight + MinimumBodyHeight + Layout.DockHeight),
        MaximumSize = Unbounded
    };

    private readonly Plugin plugin;

    private readonly TriadPage triadPage = new();

    private readonly SettingsPage settingsPage = new();

    private readonly HistoryPage historyPage = new();

    private readonly PluginsPage pluginsPage = new();

    private readonly LogPage logPage = new();

    private readonly ChangelogPage changelogPage = new();

    private readonly AboutPage aboutPage = new();

    private Page page;

    private long pageShownTick = Environment.TickCount64;

    private bool resetScroll;

    private bool compact;

    private float collapse;

    private float collapseFrom;

    private long collapseTick;

    private bool sizeDriven;

    private Vector2 expandedSize = DefaultSize;

    private IDisposable? chrome;

    private IDisposable? bodyFont;

    public Page Current => page;

    public bool Compact => compact;

    public AppWindow(Plugin plugin)
        : base("Triple Triad Autopilot###TripleTriadAutopilotMain", BaseFlags)
    {
        this.plugin = plugin;
        Size = DefaultSize;
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = ExpandedConstraints;
        AllowPinning = false;
        AllowClickthrough = false;
    }

    public void Dispose()
    {
    }

    public void Show(Page target)
    {
        if (page != target)
        {
            page = target;
            pageShownTick = Environment.TickCount64;
            resetScroll = true;
        }
        if (compact)
        {
            ToggleCompact();
        }
        IsOpen = true;
    }

    public void TogglePage(Page target)
    {
        if (IsOpen && page == target && !compact)
        {
            IsOpen = false;
        }
        else
        {
            Show(target);
        }
    }

    public void ToggleCompact()
    {
        compact = !compact;
        collapseFrom = collapse;
        collapseTick = Environment.TickCount64;
    }

    public override void OnOpen()
    {
        pageShownTick = Environment.TickCount64;
    }

    public override void PreDraw()
    {
        bodyFont = Fonts.PushBody();
        chrome = Styling.PushChrome(Vector2.Zero);
        AdvanceCollapse();
        var wasSizeDriven = sizeDriven;
        sizeDriven = compact || collapse > 0f;
        Flags = (sizeDriven ? (ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollWithMouse) : BaseFlags);
        SizeConstraints = (sizeDriven ? CompactConstraints : ExpandedConstraints);
        if (!plugin.Configuration.DragWindowFromAnywhere)
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
        if (sizeDriven)
        {
            var collapsedHeight = expandedSize.Y + (Layout.HeaderHeight - expandedSize.Y) * collapse;
            Size = new Vector2(expandedSize.X, collapsedHeight);
            SizeCondition = ImGuiCond.Always;
        }
        else if (wasSizeDriven)
        {
            Size = expandedSize;
            SizeCondition = ImGuiCond.Always;
        }
        else
        {
            SizeCondition = ImGuiCond.FirstUseEver;
        }
    }

    private void AdvanceCollapse()
    {
        var target = (compact ? 1f : 0f);
        if (Motion.Reduced)
        {
            collapse = target;
            return;
        }
        var distance = MathF.Abs(target - collapseFrom);
        if (distance <= 0.0001f)
        {
            collapse = target;
            return;
        }
        var progress = Math.Clamp((float)(Environment.TickCount64 - collapseTick) / (CollapseMs * distance), 0f, 1f);
        collapse = collapseFrom + (target - collapseFrom) * Motion.EaseInOutCubic(progress);
    }

    public override void PostDraw()
    {
        chrome?.Dispose();
        chrome = null;
        bodyFont?.Dispose();
        bodyFont = null;
    }

    public override void Draw()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var headerHeight = Layout.HeaderHeight * globalScale;
        var windowSize = ImGui.GetWindowSize();
        if (HeaderBar.HandleDrag(ImGui.GetWindowPos(), windowSize.X, headerHeight))
        {
            ToggleCompact();
        }
        var windowPos = ImGui.GetWindowPos();
        var windowRounding = Styling.WindowRounding * globalScale;
        var windowDrawList = ImGui.GetWindowDrawList();
        if (collapse >= 0.999f)
        {
            HeaderBar.Draw(this, plugin, windowPos, windowSize.X, headerHeight, windowRounding, compact: true);
            return;
        }
        if (!sizeDriven)
        {
            expandedSize = windowSize / globalScale;
        }
        Ambient.Draw(windowDrawList, windowPos, windowPos + windowSize);
        HeaderBar.Draw(this, plugin, windowPos, windowSize.X, headerHeight, windowRounding, compact: false);
        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, MathF.Max(0.001f, 1f - collapse)))
        {
            DrawBody(windowPos, windowSize, headerHeight, windowRounding);
            if (!sizeDriven)
            {
                DrawResizeGrip(windowDrawList, windowPos + windowSize);
            }
        }
    }

    private static void DrawResizeGrip(ImDrawListPtr dl, Vector2 corner)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var gripSize = GripSize * globalScale;
        var inset = GripInset * globalScale;
        var gripCorner = corner - new Vector2(inset, inset);
        var hovered = ImGui.IsMouseHoveringRect(corner - new Vector2(gripSize + inset, gripSize + inset), corner);
        var lineColor = Paint.Col(Styling.WithAlpha(Styling.TextStrong, hovered ? 0.38f : 0.14f));
        for (var i = 1; i <= 3; i++)
        {
            var offset = i * gripSize / 3f;
            dl.AddLine(gripCorner - new Vector2(offset, 0f), gripCorner - new Vector2(0f, offset), lineColor, 1.4f * globalScale);
        }
    }

    private void DrawBody(Vector2 windowPos, Vector2 windowSize, float headerHeight, float windowRounding)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var windowDrawList = ImGui.GetWindowDrawList();
        var running = plugin.Controller.Running;
        var dockHeight = ((page == Page.Triad) ? (Layout.DockHeight * globalScale) : (running ? (Layout.MiniPlayerHeight * globalScale) : 0f));
        var railWidth = Layout.RailWidth * globalScale;
        var bodyTop = windowPos.Y + headerHeight;
        var bodyHeight = windowSize.Y - headerHeight - dockHeight;
        if (bodyHeight < 1f)
        {
            return;
        }
        ImGui.SetCursorScreenPos(new Vector2(windowPos.X, bodyTop));
        using (var railChild = ImRaii.Child("##tta_rail", new Vector2(railWidth, bodyHeight), border: false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if (railChild.Success)
            {
                if (NavRail.Draw(page, plugin) is { } clickedPage)
                {
                    Show(clickedPage);
                }
            }
        }
        Paint.Hairline(windowDrawList, new Vector2(windowPos.X + railWidth, bodyTop + 10f * globalScale), new Vector2(windowPos.X + railWidth, bodyTop + bodyHeight - 10f * globalScale));
        ImGui.SetCursorScreenPos(new Vector2(windowPos.X + railWidth, bodyTop));
        var pagePadding = Layout.ContentPadding * globalScale;
        var rightInset = Layout.ContentRightInset * globalScale;
        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(pagePadding, pagePadding * 0.8f)))
        {
            using var pageChild = ImRaii.Child("##tta_page", new Vector2(windowSize.X - railWidth - rightInset, bodyHeight), border: false, ImGuiWindowFlags.AlwaysUseWindowPadding);
            if ((bool)pageChild)
            {
                DrawPage();
            }
        }
        if (dockHeight <= 0f)
        {
            return;
        }
        ImGui.SetCursorScreenPos(new Vector2(windowPos.X, windowPos.Y + windowSize.Y - dockHeight));
        var dockSize = new Vector2(windowSize.X, dockHeight);
        if (page == Page.Triad)
        {
            ActionDock.Draw(plugin, dockSize, windowRounding);
        }
        else if (MiniPlayer.Draw(plugin, dockSize, windowRounding))
        {
            Show(Page.Triad);
        }
    }

    private void DrawPage()
    {
        if (resetScroll)
        {
            ImGui.SetScrollY(0f);
            resetScroll = false;
        }
        using (Motion.PushReveal(Motion.Reveal(pageShownTick, PageRevealMs), PageSlide))
        {
            switch (page)
            {
                case Page.Triad:
                    triadPage.Draw(plugin, this);
                    break;
                case Page.Settings:
                    settingsPage.Draw(plugin);
                    break;
                case Page.History:
                    historyPage.Draw(plugin);
                    break;
                case Page.Plugins:
                    pluginsPage.Draw();
                    break;
                case Page.Log:
                    logPage.Draw();
                    break;
                case Page.Changelog:
                    changelogPage.Draw(plugin.Configuration, pageShownTick);
                    break;
                case Page.About:
                    aboutPage.Draw(pageShownTick);
                    break;
            }
        }
    }
}
