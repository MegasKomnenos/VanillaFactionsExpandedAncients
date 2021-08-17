﻿// Operation.cs by Joshua Bennett
// 
// Created 2021-08-16

using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEAncients
{
    public abstract class Operation : IExposable
    {
        public CompGeneTailoringPod Pod;

        public Operation(CompGeneTailoringPod pod)
        {
            Pod = pod;
        }

        public virtual int TicksRequired => Mathf.RoundToInt(0.01f * 60000 * Pod.parent.GetStatValue(VFEA_DefOf.VFEA_InjectingTimeFactor));

        public abstract string Label { get; }

        public void ExposeData()
        {
        }

        public virtual bool CanRunOnPawn(Pawn pawn)
        {
            return true;
        }

        public virtual int StartOnPawnGetDuration()
        {
            return TicksRequired;
        }

        public virtual float FailChanceOnPawn(Pawn pawn)
        {
            return pawn.GetPowerTracker().HasPower(VFEA_DefOf.PromisingCandidate)
                ? 0f
                : Pod.parent.GetStatValue(VFEA_DefOf.VFEA_FailChance) + pawn.GetPowerTracker().AllPowers.Count(power => power.powerType == PowerType.Superpower) * 0.1f;
        }

        public virtual void Failure()
        {
            var failType = typeof(Fail).AllSubclassesNonAbstract().RandomElement();
            var fail = (Fail) Activator.CreateInstance(failType);
            var pawn = Pod.Occupant;
            Pod.EjectContents();
            fail.RunOnPawn(pawn);
        }

        public abstract void Success();
    }

    public class Operation_Empower : Operation
    {
        public Operation_Empower(CompGeneTailoringPod pod) : base(pod)
        {
        }

        public override string Label => "VFEAncients.Empower".Translate();

        public virtual int MaxPowerLevel =>
            Pod.parent.TryGetComp<CompAffectedByFacilities>().LinkedFacilitiesListForReading.OfType<ThingWithComps>().SelectMany(t => t.AllComps).Select(comp => comp.props)
                .OfType<CompProperties_Facility_PowerUnlock>().Append(new CompProperties_Facility_PowerUnlock {unlockedLevel = 3})
                .Max(props => props.unlockedLevel);

        public override bool CanRunOnPawn(Pawn pawn)
        {
            return base.CanRunOnPawn(pawn) && pawn.GetPowerTracker().AllPowers.Count(power => power.powerType == PowerType.Superpower) < MaxPowerLevel;
        }

        public override void Success()
        {
            DefDatabase<PowerDef>.AllDefs.Split(out var superpowers, out var weaknessess, def => def.powerType == PowerType.Superpower);
            var superpower = superpowers.RandomElement();
            var weakness = weaknessess.RandomElement();
            var tracker = Pod.Occupant.GetPowerTracker();
            tracker.AddPower(superpower);
            tracker.AddPower(weakness);
            Pod.EjectContents();
            Find.LetterStack.ReceiveLetter("VFEAncients.Empowered.Label".Translate(tracker.Pawn.LabelShortCap),
                "VFEAncients.Empowered.Text".Translate(tracker.Pawn.LabelShortCap, superpower.LabelCap, weakness.LabelCap), LetterDefOf.PositiveEvent, tracker.Pawn);
        }
    }

    public class Operation_RemoveWeakness : Operation
    {
        public Operation_RemoveWeakness(CompGeneTailoringPod pod) : base(pod)
        {
        }

        public override string Label => "VFEAncients.RemoveWeakness".Translate();

        public override float FailChanceOnPawn(Pawn pawn)
        {
            if (pawn.GetPowerTracker().HasPower(VFEA_DefOf.PromisingCandidate)) return 0f;
            return base.FailChanceOnPawn(pawn) + 0.5f;
        }

        public override bool CanRunOnPawn(Pawn pawn)
        {
            return base.CanRunOnPawn(pawn) && Pod.parent.GetComp<CompAffectedByFacilities>().LinkedFacilitiesListForReading.Any(t => t.def == VFEA_DefOf.VFEA_NanotechRetractor) &&
                   pawn.GetPowerTracker().AllPowers.Any(power => power.powerType == PowerType.Weakness);
        }

        public override void Success()
        {
            var tracker = Pod.Occupant.GetPowerTracker();
            var weakness = tracker.AllPowers.LastOrDefault(power => power.powerType == PowerType.Weakness);
            if (weakness == null) return;
            tracker.RemovePower(weakness);
            Pod.EjectContents();
            Find.LetterStack.ReceiveLetter("VFEAncients.RemoveWeakness.Label".Translate(tracker.Pawn.LabelShortCap),
                "VFEAncients.RemoveWeakness.Text".Translate(tracker.Pawn.LabelShortCap, weakness.LabelCap), LetterDefOf.PositiveEvent, tracker.Pawn);
        }
    }
}