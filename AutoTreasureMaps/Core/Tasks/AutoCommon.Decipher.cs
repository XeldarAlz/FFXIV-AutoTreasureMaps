using AutoTreasureMaps.Core.Game.Ops;
using AutoTreasureMaps.Core.Treasure;
using System.Threading.Tasks;

namespace AutoTreasureMaps.Core.Tasks;

public abstract partial class AutoCommon
{
    private const int DecipherAttempts = 2;
    private const int DecipherMenuTimeoutMs = 5_000;
    private const int DecipherPickTimeoutMs = 2_000;
    private const int DecipherPromptTimeoutMs = 5_000;
    // The cast takes 2 s; the rest is room for the server's answer.
    private const int DecipherResultTimeoutMs = 10_000;
    private const int DecipherCloseTimeoutMs = 3_000;
    private const int DecipherRetryDelayMs = 1_500;
    private const int DecipherPollFrames = 3;

    // Deciphers one map of the tier. The caller checks DecipherReadiness first; this tries once more after a window
    // or a confirmation that never came, and closes the decipher windows a failed attempt leaves open.
    private protected async Task<DecipherIssue> DecipherMap(TreasureTier tier, DecipherRoute route)
    {
        var catalog = Plugin.Instance.Catalog;
        // A stop throws out of the frame waits below, so the window it interrupts is answered or closed from the cancel.
        using var stopRegistration = CancelToken.Register(() => CloseDecipherWindowOnStop(catalog, tier));
        var issue = DecipherIssue.None;
        for (var attempt = 1; attempt <= DecipherAttempts; attempt++)
        {
            Diag($"Decipher: {tier.Name} (rank {tier.Rank}, item {tier.TimewornItemId}) through the {route} route, attempt {attempt} of {DecipherAttempts}.");
            issue = route == DecipherRoute.Direct ? await DecipherDirect(catalog, tier) : await DecipherThroughMenu(catalog, tier);
            if (issue == DecipherIssue.None)
            {
                return DecipherIssue.None;
            }

            await CloseDecipherWindows(catalog, tier);
            if (CancelToken.IsCancellationRequested)
            {
                return DecipherIssue.Cancelled;
            }

            if (HoldsTier(catalog, tier))
            {
                Diag($"Decipher: {issue}, but {tier.Name} is held now, so it went through.");
                return DecipherIssue.None;
            }

            if (!DecipherIssues.Retryable(issue) || attempt == DecipherAttempts)
            {
                break;
            }

            Diag($"Decipher: {issue}; trying once more.");
            await DelayMs(DecipherRetryDelayMs);
        }

        Warn($"Decipher: gave up on {tier.Name}: {issue}.");
        return issue;
    }

    private async Task<DecipherIssue> DecipherThroughMenu(TreasureCatalog catalog, TreasureTier tier)
    {
        var since = TreasureMessageWatcher.Now;
        var status = DecipherOps.ActionStatus();
        var used = DecipherOps.UseFromMenu();
        Diag($"Decipher: General Action 19 used (returned {used}, action status {status}).");
        var opened = await WaitUntilTimed(() => DecipherOps.MenuOpen() || FailureSince(since) != DecipherIssue.None,
            DecipherMenuTimeoutMs, "decipher list", DecipherPollFrames);
        var refusal = FailureSince(since);
        if (refusal != DecipherIssue.None)
        {
            return refusal;
        }

        if (!opened)
        {
            return DecipherIssue.ListNotShown;
        }

        var menu = DecipherOps.IdentifyMenu(catalog);
        Diag($"Decipher: list open ({menu}) with {DecipherOps.DescribeMenu()}.");
        if (menu is not (DecipherMenuMatch.Titled or DecipherMenuMatch.ListsMaps))
        {
            return DecipherIssue.ListNotShown;
        }

        var entry = DecipherOps.FindMenuEntry(tier);
        var pickedByName = entry != DecipherOps.NoEntry;
        if (!pickedByName)
        {
            entry = DecipherOps.FindListedItem(tier.TimewornItemId);
            Diag($"Decipher: no entry reads '{tier.Name}'; the agent's list holds item {tier.TimewornItemId} at {entry}.");
        }

        if (entry == DecipherOps.NoEntry)
        {
            return DecipherIssue.MapNotListed;
        }

        if (!await WaitUntilTimed(() => DecipherOps.SelectMenuEntry(entry), DecipherPickTimeoutMs, "decipher list pick", 1))
        {
            return DecipherIssue.ListNotShown;
        }

        var prompted = await WaitUntilTimed(() => NpcInteraction.SelectYesnoOpen() || FailureSince(since) != DecipherIssue.None,
            DecipherPromptTimeoutMs, "decipher prompt", DecipherPollFrames);
        refusal = FailureSince(since);
        if (refusal != DecipherIssue.None)
        {
            return refusal;
        }

        if (!prompted)
        {
            return DecipherIssue.PromptNotShown;
        }

        var prompt = NpcInteraction.SelectYesnoText();
        var match = DecipherOps.MatchPrompt(prompt, tier, catalog);
        Diag($"Decipher: the prompt '{prompt}' reads as {match}.");
        // Only a pick made by name vouches for a prompt whose item name the game inflected past recognition.
        if (match is DecipherPromptMatch.NamesOtherMap or DecipherPromptMatch.Unrelated || (match == DecipherPromptMatch.TemplateOnly && !pickedByName))
        {
            return DecipherIssue.PromptMismatch;
        }

        if (!DialogDriver.Confirm())
        {
            return DecipherIssue.PromptNotShown;
        }

        return await AwaitDeciphered(catalog, tier, since);
    }

    private async Task<DecipherIssue> DecipherDirect(TreasureCatalog catalog, TreasureTier tier)
    {
        if (!MapBag.TryFind(catalog, tier.Rank, out var map))
        {
            return DecipherIssue.NoMaps;
        }

        var since = TreasureMessageWatcher.Now;
        var used = DecipherOps.UseDirect(map.Container, map.Slot);
        Diag($"Decipher: direct UseAction(Action {DecipherOps.ActionId}, the player, {map.Container} slot {map.Slot}) returned {used}.");
        return await AwaitDeciphered(catalog, tier, since);
    }

    private async Task<DecipherIssue> AwaitDeciphered(TreasureCatalog catalog, TreasureTier tier, long since)
    {
        await WaitUntilTimed(() => Deciphered(catalog, tier, since) || FailureSince(since) != DecipherIssue.None,
            DecipherResultTimeoutMs, "decipher result", DecipherPollFrames);
        if (Deciphered(catalog, tier, since))
        {
            var named = Messages.TryGetLatest(TreasureLogMessages.Deciphered, since, out var message) && message.TryGetParameter(0, out var itemId)
                ? $"the game names item {itemId}"
                : "no message yet";
            Diag($"Decipher: {tier.Name} deciphered, held now: {HoldsTier(catalog, tier)}, {named}.");
            return DecipherIssue.None;
        }

        var refusal = FailureSince(since);
        return refusal != DecipherIssue.None ? refusal : DecipherIssue.NotConfirmed;
    }

    private async Task CloseDecipherWindows(TreasureCatalog catalog, TreasureTier tier)
    {
        if (!CloseNextDecipherWindow(catalog, tier))
        {
            return;
        }

        Diag("Decipher: closing the decipher windows left open.");
        if (await WaitUntilTimed(() => !CloseNextDecipherWindow(catalog, tier), DecipherCloseTimeoutMs, "decipher windows closed", DecipherPollFrames)
            || CancelToken.IsCancellationRequested)
        {
            return;
        }

        NpcInteraction.Close(DecipherOps.MenuAddon);
        Warn("Decipher: a decipher window stayed open after its own close; closed the list directly.");
    }

    // Runs inside the stop's Cancel, where a throw would cut the stop short.
    private void CloseDecipherWindowOnStop(TreasureCatalog catalog, TreasureTier tier)
    {
        try
        {
            if (CloseNextDecipherWindow(catalog, tier))
            {
                Diag("Decipher: stopped with a decipher window open; answered or closed it.");
            }
        }
        catch (Exception exception)
        {
            Warn($"Decipher: closing the decipher window on stop threw: {exception.Message}");
        }
    }

    // True while a decipher window is still open. Each call answers or closes one, and the click helpers throttle the
    // repeats. A yes/no that is not about deciphering belongs to something else and is left alone.
    private static bool CloseNextDecipherWindow(TreasureCatalog catalog, in TreasureTier tier)
    {
        if (NpcInteraction.SelectYesnoOpen() && DecipherOps.MatchPrompt(NpcInteraction.SelectYesnoText(), tier, catalog) != DecipherPromptMatch.Unrelated)
        {
            NpcInteraction.AnswerNo();
            return true;
        }

        if (DecipherOps.IdentifyMenu(catalog) is DecipherMenuMatch.Titled or DecipherMenuMatch.ListsMaps)
        {
            DecipherOps.CloseMenu();
            return true;
        }

        return false;
    }

    private static bool Deciphered(TreasureCatalog catalog, in TreasureTier tier, long since)
        => HoldsTier(catalog, tier) || Messages.ArrivedSince(TreasureLogMessages.Deciphered, since);

    private static bool HoldsTier(TreasureCatalog catalog, in TreasureTier tier)
        => HeldMapReader.TryRead(catalog, out var held) && held.Rank == tier.Rank;

    private static DecipherIssue FailureSince(long since)
        => Messages.TryGetLatest(DecipherIssues.FailureMessages, since, out var message) ? DecipherIssues.FromMessage(message.Id) : DecipherIssue.None;
}
