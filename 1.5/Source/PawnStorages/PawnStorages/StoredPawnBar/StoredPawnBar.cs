using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PawnStorages.StoredPawnBar;

public class StoredPawnBar : ColonistBar
{
    public static StoredPawnBar Bar = new();

    public StoredPawnBar()
    {
        drawLocsFinder = new StoredPawnBarDrawLocsFinder(this);
        drawer = new StoredPawnBarColonistDrawer(this);
    }

    public new Caravan CaravanMemberCaravanAt(Vector2 at)
    {
        if (!Visible)
            return null;
        return ColonistOrCorpseAt(at) is Pawn pawn && pawn.IsCaravanMember() ? pawn.GetCaravan() : null;
    }

    public new bool TryGetEntryAt(Vector2 pos, out Entry entry)
    {
        var drawLocs = DrawLocs;
        var entries = Entries;
        var size = Size;
        for (var index = 0; index < drawLocs.Count; ++index)
            if (new Rect(drawLocs[index].x, drawLocs[index].y, size.x, size.y).Contains(pos))
            {
                entry = entries[index];
                return true;
            }

        entry = new Entry();
        return false;
    }

    public void CheckRecacheEntries_Override()
    {
        if (!entriesDirty)
            return;
        entriesDirty = false;
        cachedEntries.Clear();
        var groups = 0;
        if (PawnStoragesMod.settings.ShowStatueBar)
        {
            tmpMaps.Clear();
            tmpMaps.AddRange(CompPawnStorage.AllStorages.Keys);
            tmpMaps.SortBy(x => !x.IsPlayerHome, x => x.uniqueID);
            for (var index1 = 0; index1 < tmpMaps.Count; ++index1)
            {
                var map = CompPawnStorage.AllStorages.Keys.ToArray()[index1];

                tmpPawns.Clear();
                tmpPawns.AddRange(CompPawnStorage.AllStorages[map].SelectMany(store => store.StoredPawns)
                    .Where(pawn => pawn.Faction == Faction.OfPlayer));

                foreach (var tmpPawn in tmpPawns)
                    if (tmpPawn.playerSettings.displayOrder == -9999999)
                        tmpPawn.playerSettings.displayOrder =
                            Mathf.Max(tmpPawns.MaxBy(p => p.playerSettings.displayOrder).playerSettings.displayOrder,
                                0) + 1;
                PlayerPawnsDisplayOrderUtility.Sort(tmpPawns);
                foreach (var tmpPawn in tmpPawns)
                    cachedEntries.Add(new Entry(tmpPawn, tmpMaps[index1], groups));
                if (!tmpPawns.Any())
                    cachedEntries.Add(new Entry(null, tmpMaps[index1], groups));
                ++groups;
            }
        }

        cachedReorderableGroups.Clear();
        foreach (var cachedEntry in cachedEntries)
            cachedReorderableGroups.Add(-1);
        drawer.Notify_RecachedEntries();
        tmpPawns.Clear();
        tmpMaps.Clear();
        drawLocsFinder.CalculateDrawLocs(cachedDrawLocs, out cachedScale, groups);
    }
}