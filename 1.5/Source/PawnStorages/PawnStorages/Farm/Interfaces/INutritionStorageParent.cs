using System.Collections.Generic;
using PawnStorages.Farm.Interfaces;
using Verse;

namespace PawnStorages.Farm.Interfaces
{
    public interface INutritionStorageParent : IActive, IPawnRelease
    {
        public bool HasSuggestiveSilos { get; }
        public bool HasStoredPawns { get; }
        public List<Pawn> StoredPawns { get; }
    }
}
