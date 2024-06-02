using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace PawnStorages.Farm
{
    public class Building_PSFarm : Building
    {
        public CompFarmStorage pawnStorage;
        public CompStoredNutrition StoredNutrition;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            pawnStorage = GetComp<CompFarmStorage>();
            StoredNutrition = GetComp<CompStoredNutrition>();
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;
            Designator_Build allowedDesignator = BuildCopyCommandUtility.FindAllowedDesignator((BuildableDef)ThingDefOf.Hopper);
            if (allowedDesignator != null)
                yield return (Gizmo)allowedDesignator;
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(base.GetInspectString());
            stringBuilder.AppendLine("PS_NutritionPerDay".Translate(pawnStorage.NutritionRequiredPerDay()));
            stringBuilder.AppendLine("PS_NutritionStored".Translate(StoredNutrition.storedNutrition, StoredNutrition.Props.maxNutrtition));
            return stringBuilder.ToString().Trim();
        }

        public override void Tick()
        {
            base.Tick();
            if (Map.IsHashIntervalTick(pawnStorage.Props.ticksToProduce) && (pawnStorage.Props.alwaysProduce || StoredNutrition.IsWellFed))
            {
                pawnStorage.TryProduce();
            }

        }
    }
}
