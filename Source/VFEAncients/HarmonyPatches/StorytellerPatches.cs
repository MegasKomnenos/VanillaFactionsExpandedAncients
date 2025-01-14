﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEAncients.HarmonyPatches
{
    public static class StorytellerPatches
    {
        public static void Do(Harmony harm)
        {
            harm.Patch(AccessTools.Method(typeof(InteractionWorker_RecruitAttempt), nameof(InteractionWorker_RecruitAttempt.Interacted)),
                transpiler: new HarmonyMethod(typeof(StorytellerPatches), nameof(IncreaseRecruitDifficulty)));
            harm.Patch(AccessTools.Method(typeof(SkillRecord), nameof(SkillRecord.Interval)), new HarmonyMethod(typeof(StorytellerPatches), nameof(NoSkillDecay)));
            harm.Patch(AccessTools.Method(typeof(IncidentWorker), nameof(IncidentWorker.CanFireNow)),
                new HarmonyMethod(typeof(StorytellerPatches), nameof(AdditionalIncidentReqs)));
            harm.Patch(AccessTools.Method(typeof(IncidentWorker_PawnsArrive), nameof(IncidentWorker_PawnsArrive.FactionCanBeGroupSource)),
                null, new HarmonyMethod(typeof(StorytellerPatches), nameof(AncientsShouldNotArrive)));
        }

        public static IEnumerable<CodeInstruction> IncreaseRecruitDifficulty(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var list = instructions.ToList();
            var idx1 = list.FindIndex(ins => ins.opcode == OpCodes.Stloc_S && ins.operand is LocalBuilder {LocalIndex: 8});
            var idx2 = list.FindLastIndex(idx1 - 1, ins => ins.opcode == OpCodes.Stloc_S && ins.operand is LocalBuilder {LocalIndex: 7});
            var label1 = generator.DefineLabel();
            list[idx2 + 1].labels.Add(label1);
            list.InsertRange(idx2 + 1, new[]
            {
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Find), nameof(Find.Storyteller))),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(Utils), nameof(Utils.TryGetComp), new[] {typeof(Storyteller)}, new[] {typeof(StorytellerComp_IncreaseRecruitDifficulty)})),
                new CodeInstruction(OpCodes.Brfalse, label1),
                new CodeInstruction(OpCodes.Ldloc, 7),
                new CodeInstruction(OpCodes.Ldc_R4, 5f),
                new CodeInstruction(OpCodes.Div),
                new CodeInstruction(OpCodes.Stloc, 7)
            });
            return list;
        }

        public static bool NoSkillDecay() => Find.Storyteller.TryGetComp<StorytellerComp_NoSkilLDecay>() == null;

        public static bool AdditionalIncidentReqs(IncidentWorker __instance, IncidentParms parms, ref bool __result)
        {
            if (parms.forced) return true;
            if (Faction.OfPlayer.ideos.PrimaryIdeo is not { } ideo) return true;
            if (ideo.precepts.SelectMany(precept => precept.def.comps).OfType<PreceptComp_DisableIncident>()
                .Any(disableIncident => disableIncident.Incident == __instance.def)) return __result = false;

            return true;
        }

        public static void AncientsShouldNotArrive(ref bool __result, Faction f, Map map, bool desperate = false)
        {
            if (f != null && f.def == VFEA_DefOf.VFEA_AncientSoldiers)
            {
                __result = false;
            }
        }
    }
}