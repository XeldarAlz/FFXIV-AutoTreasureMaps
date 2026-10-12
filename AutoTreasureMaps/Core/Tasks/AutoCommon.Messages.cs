using AutoTreasureMaps.Core.Treasure;
using System.Threading.Tasks;

namespace AutoTreasureMaps.Core.Tasks;

public abstract partial class AutoCommon
{
    private const int MessagePollFrames = 5;

    private protected static TreasureMessageWatcher Messages => Plugin.Instance.Messages;

    // Take sinceMs before acting, so a reply that lands in the same frame as the action still counts.
    private protected Task<bool> WaitForMessage(uint logMessageId, long sinceMs, int timeoutMs, string scope)
        => WaitUntilTimed(() => Messages.ArrivedSince(logMessageId, sinceMs), timeoutMs, scope, MessagePollFrames);
}
