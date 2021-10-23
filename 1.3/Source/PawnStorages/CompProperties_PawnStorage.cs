using Verse;

namespace PawnStorages
{
    public class CompProperties_PawnStorage : CompProperties
    {
        public int maxStoredPawns = 1;
        public bool releaseOption;
        public bool releaseAllOption;
        public bool convertOption;
        public bool appendOfName;
        public bool showStoredPawn;
        public bool idleResearch;
        public bool lightEffect;
        public bool transformEffect;
        public CompProperties_PawnStorage()
        {
            this.compClass = typeof(CompPawnStorage);
        }
    }
}
