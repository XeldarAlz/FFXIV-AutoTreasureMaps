using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace AutoTreasureMaps.Core.Game.Ops;

// General Action 20 casts Dig (Action 1695, a 10 s recast) on the player, and the game answers in the log. The coffer it
// turns up asks with Addon 8105 before it opens, since opening it costs the map. Texts follow the client's language.
internal static unsafe class DigOps
{
    private const uint DigGeneralActionId = 20;
    // GeneralAction 20's own Action and UnlockLink columns; these stand in only when the row cannot be read.
    private const uint KnownDigActionId = 1695;
    private const uint KnownUnlockLink = 25;
    private const uint CofferPromptAddonRow = 8105;

    private static Texts? texts;

    private static Texts Sheet => texts ??= Texts.Read();

    public static uint ActionId => Sheet.ActionId;

    public static bool Unlocked()
    {
        var state = UIState.Instance();
        return state is not null && state->IsUnlockLinkUnlocked(Sheet.UnlockLink);
    }

    // Dig's recast is kept on its action, but the general action is asked too, so neither view can let a dig through early.
    public static bool RecastActive()
    {
        var actions = ActionManager.Instance();
        return actions is not null
            && (actions->IsRecastTimerActive(ActionType.Action, Sheet.ActionId) || actions->IsRecastTimerActive(ActionType.GeneralAction, DigGeneralActionId));
    }

    // The LogMessage row the game would print if Dig were used now, 0 when it would go through.
    public static uint ActionStatus()
    {
        var actions = ActionManager.Instance();
        return actions is null ? 0 : actions->GetActionStatus(ActionType.GeneralAction, DigGeneralActionId);
    }

    public static bool Use()
    {
        var actions = ActionManager.Instance();
        return actions is not null && actions->UseAction(ActionType.GeneralAction, DigGeneralActionId);
    }

    public static bool IsCofferPrompt(string promptText)
    {
        var fragments = Sheet.CofferPromptFragments;
        if (fragments.Length == 0)
        {
            return false;
        }

        var prompt = GameText.Fold(promptText);
        for (var index = 0; index < fragments.Length; index++)
        {
            if (!GameText.Contains(prompt, fragments[index]))
            {
                return false;
            }
        }

        return true;
    }

    private sealed record Texts(uint ActionId, uint UnlockLink, string[] CofferPromptFragments)
    {
        public static Texts Read()
        {
            var general = Svc.Data.GetExcelSheet<GeneralAction>().GetRowOrDefault(DigGeneralActionId);
            var actionId = general is { } action && action.Action.RowId != 0 ? action.Action.RowId : KnownDigActionId;
            var unlockLink = general is { } unlock && unlock.UnlockLink != 0 ? unlock.UnlockLink : KnownUnlockLink;
            var prompt = Svc.Data.GetExcelSheet<Addon>().GetRowOrDefault(CofferPromptAddonRow) is { } row ? GameText.FixedFragments(row.Text) : [];
            RunLog.Debug($"Dig texts: action {actionId}, unlock link {unlockLink}, coffer prompt fragments '{string.Join("' '", prompt)}'.");
            return new Texts(actionId, unlockLink, prompt);
        }
    }
}
