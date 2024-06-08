using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PawnStorages.StoredPawnBar;

public class StoredPawnBar : ColonistBar
{
  public static StoredPawnBar Bar = new StoredPawnBar();
    public StoredPawnBar()
    {
        drawLocsFinder = new StoredPawnBarDrawLocsFinder(this);
        drawer = new StoredPawnBarColonistDrawer(this);
    }
    
    public new Caravan CaravanMemberCaravanAt(Vector2 at)
    {
      if (!this.Visible)
        return (Caravan) null;
      return this.ColonistOrCorpseAt(at) is Pawn pawn && pawn.IsCaravanMember() ? pawn.GetCaravan() : (Caravan) null;
    }
    public new bool TryGetEntryAt(Vector2 pos, out ColonistBar.Entry entry)
    {
      List<Vector2> drawLocs = this.DrawLocs;
      List<ColonistBar.Entry> entries = this.Entries;
      Vector2 size = this.Size;
      for (int index = 0; index < drawLocs.Count; ++index)
      {
        if (new Rect(drawLocs[index].x, drawLocs[index].y, size.x, size.y).Contains(pos))
        {
          entry = entries[index];
          return true;
        }
      }
      entry = new ColonistBar.Entry();
      return false;
    }
    
    public void CheckRecacheEntries_Override()
    {
      if (!this.entriesDirty)
        return;
      this.entriesDirty = false;
      this.cachedEntries.Clear();
      int num = 0;
      if (Find.PlaySettings.showColonistBar)
      {
        StoredPawnBar.tmpMaps.Clear();
        StoredPawnBar.tmpMaps.AddRange((IEnumerable<Map>) CompPawnStorage.AllStorages.Keys);
        StoredPawnBar.tmpMaps.SortBy<Map, bool, int>((Func<Map, bool>) (x => !x.IsPlayerHome), (Func<Map, int>) (x => x.uniqueID));
        for (int index1 = 0; index1 < StoredPawnBar.tmpMaps.Count; ++index1)
        {
          Map map = CompPawnStorage.AllStorages.Keys.ToArray()[index1];
          
          StoredPawnBar.tmpPawns.Clear();
          StoredPawnBar.tmpPawns.AddRange((IEnumerable<Pawn>) CompPawnStorage.AllStorages[map].SelectMany(store=>store.StoredPawns).Where(pawn => pawn.Faction == Faction.OfPlayer));
          
          foreach (Pawn tmpPawn in StoredPawnBar.tmpPawns)
          {
            if (tmpPawn.playerSettings.displayOrder == -9999999)
              tmpPawn.playerSettings.displayOrder = Mathf.Max(StoredPawnBar.tmpPawns.MaxBy<Pawn, int>((Func<Pawn, int>) (p => p.playerSettings.displayOrder)).playerSettings.displayOrder, 0) + 1;
          }
          PlayerPawnsDisplayOrderUtility.Sort(StoredPawnBar.tmpPawns);
          foreach (Pawn tmpPawn in StoredPawnBar.tmpPawns)
            this.cachedEntries.Add(new StoredPawnBar.Entry(tmpPawn, StoredPawnBar.tmpMaps[index1], num));
          if (!StoredPawnBar.tmpPawns.Any<Pawn>())
            this.cachedEntries.Add(new StoredPawnBar.Entry((Pawn) null, StoredPawnBar.tmpMaps[index1], num));
          ++num;
        }
      }
      this.cachedReorderableGroups.Clear();
      foreach (StoredPawnBar.Entry cachedEntry in this.cachedEntries)
        this.cachedReorderableGroups.Add(-1);
      this.drawer.Notify_RecachedEntries();
      StoredPawnBar.tmpPawns.Clear();
      StoredPawnBar.tmpMaps.Clear();
      this.drawLocsFinder.CalculateDrawLocs(this.cachedDrawLocs, out this.cachedScale, num);
    }
}