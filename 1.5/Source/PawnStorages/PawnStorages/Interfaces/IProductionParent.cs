using System.Collections.Generic;
using Verse;

namespace PawnStorages.Interfaces
{
    public interface IProductionParent : IActive, IPawnRelease
    {
        public List<Pawn> ProducingPawns { get; }
        public int TickInterval { get; }
    }
}
