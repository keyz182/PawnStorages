using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PawnStorages;

public class PawnStorages_GameComponent : GameComponent
{
    private static HashSet<CompAssignableToPawn_PawnStorage> _CompAssignables = [];
    private static HashSet<CompPawnStorage> _CompPawnStorage = [];

    public static PawnStorageBar PawnStorageBar = new PawnStorageBar();

    public static bool AssignablesDirty = false;

    public static HashSet<CompAssignableToPawn_PawnStorage> CompAssignables
    {
        get
        {
            if (AssignablesDirty)
            {
                _CompAssignables.Clear();
                foreach (var comp in Find.CurrentMap.spawnedThings.Select(thing =>
                             thing.TryGetComp<CompAssignableToPawn_PawnStorage>()))
                {
                    if (comp != null)
                        _CompAssignables.Add(comp);
                }

                AssignablesDirty = false;
            }

            return _CompAssignables;
        }
    }

    public static HashSet<CompPawnStorage> CompPawnStorage
    {
        get
        {
            if (AssignablesDirty)
            {
                _CompPawnStorage.Clear();
                foreach (var comp in Find.CurrentMap.spawnedThings.Select(thing =>
                             thing.TryGetComp<CompPawnStorage>()))
                {
                    if (comp != null)
                        _CompPawnStorage.Add(comp);
                }

                AssignablesDirty = false;
            }

            return _CompPawnStorage;
        }
    }

    public PawnStorages_GameComponent(Game game)
    {
    }

    public override void LoadedGame()
    {
        base.LoadedGame();
        AssignablesDirty = true;
    }

    public override void StartedNewGame()
    {
        base.LoadedGame();
        AssignablesDirty = true;
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        AssignablesDirty = true;
        // this runs _after_ everything is set up, so it just clears out CompAssignables immediately after filling it
        // CompAssignables.Clear();
    }


    public override void GameComponentOnGUI()
    {
        base.GameComponentOnGUI();
        PawnStorageBar.PawnStorageBarOnGui();
    }

}
