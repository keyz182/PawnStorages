using PawnStorages.Farm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PawnStorages.Farm.Interfaces 
{
    public interface IBreederParent : IActive
    {
        public List<Pawn> BreedablePawns { get; }
        public int TickInterval { get; }

        public void Notify_PawnBorn(Pawn newPawn);
    }
}
