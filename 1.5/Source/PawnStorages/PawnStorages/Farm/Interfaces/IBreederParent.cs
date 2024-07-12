using System.Collections.Generic;
using Verse;

namespace PawnStorages.Farm.Interfaces
{
    public interface IBreederParent : IActive
    {
        public List<Pawn> BreedablePawns { get; }
        public List<Pawn> AllPawns { get; }
        public int TickInterval { get; }

        public void Notify_PawnBorn(Pawn newPawn);

        public void Release(Pawn pawn);

        public Map Map { get; }
    }
}
