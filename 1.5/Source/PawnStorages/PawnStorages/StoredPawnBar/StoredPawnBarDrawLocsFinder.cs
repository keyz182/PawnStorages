using UnityEngine;
using RimWorld;
using Verse;

namespace PawnStorages.StoredPawnBar;

public class StoredPawnBarDrawLocsFinder : ColonistBarDrawLocsFinder
{
    public ColonistBar _ColonistBar;
    public ColonistBar ColonistBarOverride => _ColonistBar;
    public StoredPawnBarDrawLocsFinder(ColonistBar bar)
    {
        _ColonistBar = bar;
    }

    public Vector2 GetDrawLoc_Override(
        float groupStartX,
        float groupStartY,
        int group,
        int numInGroup,
        float scale)
    {
        var offsetY = Find.ColonistBar.Size.y + 30f;
        float x = groupStartX + (float) ((double) (numInGroup % this.horizontalSlotsPerGroup[group]) * (double) scale * ((double) ColonistBar.BaseSize.x + 24.0));
        float y = groupStartY + (float) ((double) (numInGroup / this.horizontalSlotsPerGroup[group]) * (double) scale * ((double) ColonistBar.BaseSize.y + 32.0)) + offsetY;
        if (numInGroup >= this.entriesInGroup[group] - this.entriesInGroup[group] % this.horizontalSlotsPerGroup[group])
        {
            int num = this.horizontalSlotsPerGroup[group] - this.entriesInGroup[group] % this.horizontalSlotsPerGroup[group];
            x += (float) ((double) num * (double) scale * ((double) ColonistBar.BaseSize.x + 24.0) * 0.5);
        }
        return new Vector2(x, y);
    }
    
}