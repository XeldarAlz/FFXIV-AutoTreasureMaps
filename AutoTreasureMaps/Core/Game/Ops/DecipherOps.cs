using AutoTreasureMaps.Core.Treasure;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using System.Text;

namespace AutoTreasureMaps.Core.Game.Ops;

internal enum DecipherMenuMatch : byte
{
    NotOpen,
    Titled,
    ListsMaps,
    Unrelated,
}

internal enum DecipherPromptMatch : byte
{
    NamesTarget,
    TemplateOnly,
    NamesOtherMap,
    Unrelated,
}

// The game's decipher chain: General Action 19 opens a SelectIconString of every timeworn map in the bags (Addon 8102
// title, 8104 description), a pick opens a SelectYesno built from Addon 8101, and Yes casts Action 1694 on the picked
// slot. Every text is read from the sheets in the client's language.
internal static unsafe class DecipherOps
{
    public const string MenuAddon = "SelectIconString";
    public const int NoEntry = -1;

    private const uint DecipherGeneralActionId = 19;
    // GeneralAction 19's own Action and UnlockLink columns; these stand in only when the row cannot be read.
    private const uint KnownDecipherActionId = 1694;
    private const uint KnownUnlockLink = 25;
    private const uint PromptAddonRow = 8101;
    private const uint TitleAddonRow = 8102;
    private const uint DescriptionAddonRow = 8104;
    // The list's own close value; closing through it lets the agent lift the slot blocks it placed on every listed map.
    private const int CloseMenuValue = -1;
    // AgentTreasureHunt keeps one InventoryItem pointer per listed entry from +0x28, at most 32 of them.
    private const int AgentListOffset = 0x28;
    private const int AgentListCapacity = 32;
    private const int TextSearchDepth = 1;

    private static Texts? texts;
    private static string[] foldedTierNames = [];

    private static Texts Sheet => texts ??= Texts.Read();

    public static uint ActionId => Sheet.ActionId;

    public static bool Unlocked()
    {
        var state = UIState.Instance();
        return state is not null && state->IsUnlockLinkUnlocked(Sheet.UnlockLink);
    }

    // The LogMessage row the game would print if the action were used now, 0 when it would go through.
    public static uint ActionStatus()
    {
        var actions = ActionManager.Instance();
        return actions is null ? 0 : actions->GetActionStatus(ActionType.GeneralAction, DecipherGeneralActionId);
    }

    public static bool UseFromMenu()
    {
        var actions = ActionManager.Instance();
        return actions is not null && actions->UseAction(ActionType.GeneralAction, DecipherGeneralActionId);
    }

    // The same call the list's Yes makes, without the list: the action on the player, the map's slot packed with its bag.
    public static bool UseDirect(InventoryType container, ushort slot)
    {
        var actions = ActionManager.Instance();
        if (actions is null || Svc.Objects.LocalPlayer is not { } player)
        {
            return false;
        }

        var extraParam = ((uint)container << 16) | slot;
        return actions->UseAction(ActionType.Action, Sheet.ActionId, player.GameObjectId, extraParam);
    }

    public static bool MenuOpen() => TryGetMenu(out _);

    public static DecipherMenuMatch IdentifyMenu(TreasureCatalog catalog)
    {
        if (!TryGetMenu(out var menu))
        {
            return DecipherMenuMatch.NotOpen;
        }

        var unit = (AtkUnitBase*)menu;
        if (AddonShows(unit, Sheet.Title, whole: true) || AddonShows(unit, Sheet.Description, whole: false))
        {
            return DecipherMenuMatch.Titled;
        }

        return ListsAnyMap(menu, catalog) ? DecipherMenuMatch.ListsMaps : DecipherMenuMatch.Unrelated;
    }

    public static int FindMenuEntry(in TreasureTier tier)
    {
        if (!TryGetMenu(out var menu))
        {
            return NoEntry;
        }

        var name = GameText.Fold(tier.Name);
        var count = menu->PopupMenu.PopupMenu.EntryCount;
        for (var index = 0; index < count; index++)
        {
            if (GameText.Same(name, EntryText(menu, index)))
            {
                return index;
            }
        }

        return NoEntry;
    }

    // The fallback when no entry text matches: the agent lists the maps in the same order as the window.
    public static int FindListedItem(uint itemId)
    {
        if (!TryGetMenu(out var menu))
        {
            return NoEntry;
        }

        var agents = AgentModule.Instance();
        var agent = agents is null ? null : agents->GetAgentByInternalId(AgentId.TreasureHunt);
        if (agent is null)
        {
            return NoEntry;
        }

        var count = Math.Min(menu->PopupMenu.PopupMenu.EntryCount, AgentListCapacity);
        var list = (InventoryItem**)((byte*)agent + AgentListOffset);
        for (var index = 0; index < count; index++)
        {
            var item = list[index];
            if (item is not null && item->ItemId == itemId)
            {
                return index;
            }
        }

        return NoEntry;
    }

    public static string DescribeMenu()
    {
        if (!TryGetMenu(out var menu))
        {
            return "no list open";
        }

        var builder = new StringBuilder();
        var count = menu->PopupMenu.PopupMenu.EntryCount;
        for (var index = 0; index < count; index++)
        {
            builder.Append(index == 0 ? string.Empty : ", ").Append(index).Append(": ").Append(EntryText(menu, index));
        }

        return builder.Length == 0 ? "an empty list" : builder.ToString();
    }

    public static bool SelectMenuEntry(int index) => NpcInteraction.FireCallback(MenuAddon, index);

    public static bool CloseMenu() => NpcInteraction.FireCallback(MenuAddon, CloseMenuValue);

    // The longest tier name found in the prompt decides, because one name can sit inside another (in Japanese,
    // grade 1 inside grade 10). With no name found, the template's fixed words tell a decipher prompt whose item name
    // the game inflected.
    public static DecipherPromptMatch MatchPrompt(string promptText, in TreasureTier target, TreasureCatalog catalog)
    {
        var prompt = GameText.Fold(promptText);
        var tiers = catalog.Tiers;
        var names = FoldedTierNames(catalog);
        var namedRank = (byte)0;
        var namedLength = 0;
        for (var index = 0; index < tiers.Length; index++)
        {
            var name = names[index];
            if (name.Length > namedLength && GameText.Contains(prompt, name))
            {
                namedRank = tiers[index].Rank;
                namedLength = name.Length;
            }
        }

        if (namedLength > 0)
        {
            return namedRank == target.Rank ? DecipherPromptMatch.NamesTarget : DecipherPromptMatch.NamesOtherMap;
        }

        return ShowsTemplate(prompt) ? DecipherPromptMatch.TemplateOnly : DecipherPromptMatch.Unrelated;
    }

    private static bool ShowsTemplate(string prompt)
    {
        var fragments = Sheet.PromptFragments;
        if (fragments.Length == 0)
        {
            return false;
        }

        for (var index = 0; index < fragments.Length; index++)
        {
            if (!GameText.Contains(prompt, fragments[index]))
            {
                return false;
            }
        }

        return true;
    }

    // Indexed like catalog.Tiers. The catalog is built once per load, so the names are folded once.
    private static string[] FoldedTierNames(TreasureCatalog catalog)
    {
        var tiers = catalog.Tiers;
        if (foldedTierNames.Length == tiers.Length)
        {
            return foldedTierNames;
        }

        var names = new string[tiers.Length];
        for (var index = 0; index < tiers.Length; index++)
        {
            names[index] = GameText.Fold(tiers[index].Name);
        }

        foldedTierNames = names;
        return names;
    }

    private static bool TryGetMenu(out AddonSelectIconString* menu)
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>(MenuAddon, out var unit) && GenericHelpers.IsAddonReady(unit))
        {
            menu = (AddonSelectIconString*)unit;
            return true;
        }

        menu = null;
        return false;
    }

    private static string EntryText(AddonSelectIconString* menu, int index)
    {
        var names = menu->PopupMenu.PopupMenu.EntryNames;
        if (names is null || !names[index].HasValue)
        {
            return string.Empty;
        }

        return new ReadOnlySeStringSpan(names[index].AsSpan()).ExtractText();
    }

    private static bool ListsAnyMap(AddonSelectIconString* menu, TreasureCatalog catalog)
    {
        var count = menu->PopupMenu.PopupMenu.EntryCount;
        var names = FoldedTierNames(catalog);
        for (var entryIndex = 0; entryIndex < count; entryIndex++)
        {
            var entry = GameText.Fold(EntryText(menu, entryIndex));
            for (var tierIndex = 0; tierIndex < names.Length; tierIndex++)
            {
                if (string.Equals(entry, names[tierIndex], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // The title can arrive as one of the addon's values or sit in a text node of the window frame, so both are read.
    private static bool AddonShows(AtkUnitBase* unit, string needle, bool whole)
    {
        if (needle.Length == 0)
        {
            return false;
        }

        var valueCount = unit->AtkValues is null ? 0 : unit->AtkValuesCount;
        for (var index = 0; index < valueCount; index++)
        {
            var value = unit->AtkValues[index];
            if (value.Type is AtkValueType.String or AtkValueType.ManagedString or AtkValueType.ConstString
                && value.String.HasValue
                && Matches(value.String.AsSpan(), needle, whole))
            {
                return true;
            }
        }

        return NodesShow(&unit->UldManager, needle, whole, TextSearchDepth);
    }

    private static bool NodesShow(AtkUldManager* manager, string needle, bool whole, int depth)
    {
        for (var index = 0; index < manager->NodeListCount; index++)
        {
            var node = manager->NodeList[index];
            if (node is null)
            {
                continue;
            }

            var textNode = node->GetAsAtkTextNode();
            if (textNode is not null)
            {
                if (Matches(textNode->NodeText.AsSpan(), needle, whole))
                {
                    return true;
                }

                continue;
            }

            var componentNode = node->GetAsAtkComponentNode();
            if (depth > 0 && componentNode is not null && componentNode->Component is not null
                && NodesShow(&componentNode->Component->UldManager, needle, whole, depth - 1))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Matches(ReadOnlySpan<byte> utf8, string needle, bool whole)
    {
        if (utf8.IsEmpty)
        {
            return false;
        }

        var text = GameText.Fold(new ReadOnlySeStringSpan(utf8).ExtractText());
        return whole ? string.Equals(text, needle, StringComparison.OrdinalIgnoreCase) : GameText.Contains(text, needle);
    }

    private sealed record Texts(uint ActionId, uint UnlockLink, string Title, string Description, string[] PromptFragments)
    {
        public static Texts Read()
        {
            var general = Svc.Data.GetExcelSheet<GeneralAction>().GetRowOrDefault(DecipherGeneralActionId);
            var actionId = general is { } action && action.Action.RowId != 0 ? action.Action.RowId : KnownDecipherActionId;
            var unlockLink = general is { } unlock && unlock.UnlockLink != 0 ? unlock.UnlockLink : KnownUnlockLink;
            var addons = Svc.Data.GetExcelSheet<Addon>();
            var title = GameText.Fold(addons.GetRowOrDefault(TitleAddonRow)?.Text.ExtractText() ?? string.Empty);
            var description = GameText.Fold(addons.GetRowOrDefault(DescriptionAddonRow)?.Text.ExtractText() ?? string.Empty);
            var fragments = addons.GetRowOrDefault(PromptAddonRow) is { } prompt ? GameText.FixedFragments(prompt.Text) : [];
            RunLog.Debug($"Decipher texts: action {actionId}, unlock link {unlockLink}, title '{title}', description '{description}', prompt fragments '{string.Join("' '", fragments)}'.");
            return new Texts(actionId, unlockLink, title, description, fragments);
        }
    }
}
