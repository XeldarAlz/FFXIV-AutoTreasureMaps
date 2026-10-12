using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Windows.Components;
using AutoTreasureMaps.Windows.Sections;
using AutoTreasureMaps.Windows.Shell;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoTreasureMaps.Windows.Pages;

internal sealed class TreasurePage
{
    private const float SwitchRevealMs = 320f;
    private const float EmptyCardHeight = 118f;
    private const float EmptyCardPadX = 24f;

    public void Draw(Plugin plugin, AppWindow window)
    {
        var controller = plugin.Controller;
        var running = controller.Running;

        using var reveal = Motion.PushSwitch("##atm_treasure_state", running, SwitchRevealMs);
        if (running)
        {
            RunningPanel.Draw(controller);
            return;
        }

        DrawIdle(plugin, window);
    }

    private static void DrawIdle(Plugin plugin, AppWindow window)
    {
        if (Headline.Draw(plugin.Controller, plugin.History))
        {
            window.Show(AppWindow.Page.Plugins);
        }

        Styling.VSpace(20f);
        SectionTitle(Loc.T(L.Treasure.CurrentMap));
        EmptyCard(FontAwesomeIcon.Map, Loc.T(L.Treasure.CurrentMapEmpty));
        Styling.VSpace(12f);
    }

    private static void SectionTitle(string text)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var size = TextDraw.SectionTitleSize(text);
        TextDraw.SectionTitle(text, origin, Styling.TextStrong);
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, size.Y + 8f * scale));
    }

    private static void EmptyCard(FontAwesomeIcon icon, string text)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, EmptyCardHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        Paint.Surface(drawList, origin, end, Styling.CardRounding * scale, Styling.WithAlpha(Styling.Surface0, 0.6f), Styling.WithAlpha(Styling.BorderDim, 0.5f), topLight: false);

        var padX = EmptyCardPadX * scale;
        var wrapWidth = size.X - padX * 2f;
        var textSize = TextDraw.MeasureWrapped(text, wrapWidth);
        var iconHeight = 26f * scale;
        var gap = 12f * scale;
        var top = origin.Y + (size.Y - iconHeight - gap - textSize.Y) * 0.5f;
        var center = new Vector2((origin.X + end.X) * 0.5f, top + iconHeight * 0.5f);
        ProgressRing.CenterIcon(center, icon, Styling.TextMuted, iconHeight);
        TextDraw.Wrapped(text, new Vector2(center.X - textSize.X * 0.5f, top + iconHeight + gap), wrapWidth, Styling.TextMuted);
        ImGui.Dummy(size);
    }
}
