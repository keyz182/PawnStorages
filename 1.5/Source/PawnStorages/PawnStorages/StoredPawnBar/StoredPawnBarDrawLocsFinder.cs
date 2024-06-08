using RimWorld;
using UnityEngine;
using Verse;

namespace PawnStorages.StoredPawnBar;

public class StoredPawnBarDrawLocsFinder : ColonistBarDrawLocsFinder
{
    public ColonistBar _ColonistBar;

    public StoredPawnBarDrawLocsFinder(ColonistBar bar)
    {
        _ColonistBar = bar;
    }

    public ColonistBar ColonistBarOverride => _ColonistBar;

    public Vector2 GetDrawLoc_Override(
        float groupStartX,
        float groupStartY,
        int group,
        int numInGroup,
        float scale)
    {
        var offsetY = Find.ColonistBar.Size.y + 30f;
        var x = groupStartX + (float)(numInGroup % horizontalSlotsPerGroup[group] * (double)scale *
                                      (ColonistBar.BaseSize.x + 24.0));
        var y = groupStartY +
                (float)(numInGroup / horizontalSlotsPerGroup[group] * (double)scale * (ColonistBar.BaseSize.y + 32.0)) +
                offsetY;
        if (numInGroup >= entriesInGroup[group] - entriesInGroup[group] % horizontalSlotsPerGroup[group])
        {
            var num = horizontalSlotsPerGroup[group] - entriesInGroup[group] % horizontalSlotsPerGroup[group];
            x += (float)(num * (double)scale * (ColonistBar.BaseSize.x + 24.0) * 0.5);
        }

        return new Vector2(x, y);
    }
}