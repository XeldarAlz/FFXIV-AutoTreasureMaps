using AutoTreasureMaps.Core.Hunts;

namespace AutoTreasureMaps.Core.Marks;

public readonly record struct MarkAchievement(
    uint AchievementId,
    HuntMarkRank Rank,
    ExpansionKind Expansion,
    string Name,
    uint IconId,
    ushort FirstMark,
    byte MarkCount);
