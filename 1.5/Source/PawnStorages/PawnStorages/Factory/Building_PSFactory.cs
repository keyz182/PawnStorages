using System.Collections.Generic;
using System.Linq;
using System.Text;
using PawnStorages.Farm.Interfaces;
using RimWorld;
using Verse;
using Enumerable = System.Linq.Enumerable;

namespace PawnStorages.Factory;

public class Building_PSFactory : Building, IStoreSettingsParent, INutritionStorageParent, IProductionParent
{
    public CompPawnStorage pawnStorage;
    public CompPawnStorageNutrition pawnStorageNutrition;
    public CompFactoryProducer factoryProducer;
    private StorageSettings allowedNutritionSettings;

    protected Dictionary<ThingDef, bool> allowedThings;

    public Dictionary<ThingDef, bool> AllowedThings => allowedThings;
    public HashSet<ThingDef> AllowedThingDefs => [..allowedThings.Keys];

    public BillStack BillStack => factoryProducer.BillStack;

    public bool Allowed(ThingDef potentialDef)
    {
        return allowedThings.GetValueOrDefault(potentialDef, false);
    }

    public bool IsProducer => factoryProducer != null;

    public bool IsFull => pawnStorage?.IsFull ?? true;

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref allowedThings, "allowedThings", LookMode.Def);
        base.ExposeData();
    }

    public void AllowAll()
    {
        foreach (ThingDef allowedThingsKey in AllowedThingDefs)
        {
            AllowedThings[allowedThingsKey] = true;
        }
    }

    public void DenyAll()
    {
        foreach (ThingDef allowedThingsKey in AllowedThingDefs)
        {
            AllowedThings[allowedThingsKey] = false;
        }
    }

    public bool NutritionAvailable = true;
    private List<RecipeDef> allRecipesCached;

    public List<ThingDef> AllowableThing => Utility.Animals(IsProducer);

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        pawnStorage = GetComp<CompPawnStorage>();
        pawnStorageNutrition = GetComp<CompPawnStorageNutrition>();
        factoryProducer = GetComp<CompFactoryProducer>();
        base.SpawnSetup(map, respawningAfterLoad);
        allowedNutritionSettings = new StorageSettings(this);
        if (def.building.defaultStorageSettings == null)
            return;
        allowedNutritionSettings.CopyFrom(def.building.defaultStorageSettings);

        allowedThings ??= new Dictionary<ThingDef, bool>();

        foreach (ThingDef thingDef in AllowableThing.Where(t => !AllowedThingDefs.Contains(t)))
        {
            AllowedThings[thingDef] = true;
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
            yield return gizmo;
        Designator_Build allowedFactoryHopperDesignator = BuildCopyCommandUtility.FindAllowedDesignator(PS_DefOf.PS_FactoryHopper);
        if (allowedFactoryHopperDesignator != null) yield return allowedFactoryHopperDesignator;
        foreach (Thing thing in (IEnumerable<Thing>) StoredPawns)
        {
            Gizmo gizmo;
            if ((gizmo = SelectContainedItemGizmo(thing, thing)) != null)
                yield return gizmo;
        }
    }

    public override string GetInspectString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine(base.GetInspectString());
        stringBuilder.AppendLine("PS_CurrentBill".Translate(factoryProducer.CurrentBill?.LabelCap ?? "PS_NoBill".Translate()));
        stringBuilder.AppendLine("PS_NutritionPerDay".Translate(pawnStorage.NutritionRequiredPerDay().ToStringDecimalIfSmall()));
        if (!pawnStorageNutrition.HasAltStore)
            stringBuilder.AppendLine("PS_NutritionStored".Translate(pawnStorageNutrition.storedNutrition, pawnStorageNutrition.MaxNutrition));
        return stringBuilder.ToString().Trim();
    }

    public StorageSettings GetStoreSettings()
    {
        return allowedNutritionSettings;
    }

    public StorageSettings GetParentStoreSettings()
    {
        return def.building.fixedStorageSettings;
    }

    public void Notify_SettingsChanged()
    {
    }

    public bool StorageTabVisible => true;

    public bool IsActive => true;

    public void ReleasePawn(Pawn pawn)
    {
        pawnStorage.ReleaseSingle(Map, pawn, true, true);
    }

    public bool HasSuggestiveSilos => false;

    public bool HasStoredPawns => true;
    public List<Pawn> StoredPawns => pawnStorage?.GetDirectlyHeldThings()?.Select(p => p as Pawn).ToList() ?? [];

    public void Notify_NutritionEmpty() => NutritionAvailable = false;

    public void Notify_NutritionNotEmpty() => NutritionAvailable = true;

    public List<Pawn> AllHealthyPawns => StoredPawns.Select(p => p).Where(pawn => !pawn.health.Dead && !pawn.health.Downed).ToList();

    public List<Pawn> ProducingPawns =>
        !IsProducer
            ? []
            : StoredPawns.Select(p => p).Where(pawn => pawn.ageTracker.Adult && !pawn.health.Dead && !pawn.health.Downed).ToList();

    public int TickInterval => 250;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, pawnStorage.GetDirectlyHeldThings());
    }

    public List<RecipeDef> AllRecipesUnfiltered
    {
        get
        {
            if (allRecipesCached != null) return allRecipesCached;
            allRecipesCached = [];
            List<RecipeDef> defsListForReading = DefDatabase<RecipeDef>.AllDefsListForReading;
            foreach (RecipeDef t in defsListForReading)
            {
                if (t.recipeUsers != null) allRecipesCached.Add(t);
            }

            return allRecipesCached;
        }
    }
}
