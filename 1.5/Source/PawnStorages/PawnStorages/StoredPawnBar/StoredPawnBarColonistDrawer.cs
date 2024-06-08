using RimWorld;

namespace PawnStorages.StoredPawnBar;

public class StoredPawnBarColonistDrawer : ColonistBarColonistDrawer
{
    public ColonistBar _ColonistBar;
    public ColonistBar ColonistBarOverride => _ColonistBar;
    public StoredPawnBarColonistDrawer(ColonistBar bar)
    {
        _ColonistBar = bar;
    }
    
}