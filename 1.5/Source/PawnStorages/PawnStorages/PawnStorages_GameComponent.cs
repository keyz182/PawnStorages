using System.Collections.Generic;
using Verse;

namespace PawnStorages;

public class PawnStorages_GameComponent : GameComponent
{
    public static HashSet<CompAssignableToPawn_PawnStorage> CompAssignables = [];

    public PawnStorages_GameComponent(Game game)
    {
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        // this runs _after_ everything is set up, so it just clears out CompAssignables immediately after filling it
        // CompAssignables.Clear();
    }
}
