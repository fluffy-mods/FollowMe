using System.Collections.Generic;
using System.Linq;
// Cinematic.cs
// Copyright Karel Kroeze, 2019-2019

using RimWorld;
using Verse;

namespace FollowMe {
    public abstract class InterestWorker {
        public abstract ThingRequestGroup PotentiallyInteresting { get; }

        public virtual bool Interesting(Thing thing) {
            return !thing.Fogged();
        }

        public abstract float InterestFor(Thing thing);
    }

    public class InterestWorker_AmbulanceChaser: InterestWorker {
        public override ThingRequestGroup PotentiallyInteresting => ThingRequestGroup.Pawn;

        public override float InterestFor(Thing thing) {
            if (!(thing is Pawn pawn)) {
                return 0;
            }

            float interest = 1f;
            interest *= 1 + (pawn.health.hediffSet.BleedRateTotal * 5);
            JobDef job = pawn.CurJobDef;
            if (job == JobDefOf.ExtinguishSelf) {
                interest *= 5f;
            }

            if (job == JobDefOf.Rescue) {
                interest *= 3f;
            }

            if (job == JobDefOf.TendPatient) {
                interest *= 2f;
            }

            return interest;
        }
    }

    public class InterestWorker_Attenborough: InterestWorker {
        public override ThingRequestGroup PotentiallyInteresting => ThingRequestGroup.Pawn;

        public override float InterestFor(Thing thing) {
            if (!(thing is Pawn pawn)) {
                return 0f;
            }

            float interest = 1f;
            if (pawn.AnimalOrWildMan()) {
                interest *= 5f;
            }

            if (pawn.CurJobDef == JobDefOf.Hunt ||
                 pawn.CurJobDef == JobDefOf.PredatorHunt) {
                interest *= 5f;
            }

            if (pawn.CurJobDef == JobDefOf.LayEgg) {
                interest *= 2f;
            }

            if (pawn.CurJobDef == JobDefOf.Lovin) {
                interest *= 3f;
            }

            interest *= pawn.RaceProps.wildness;

            float? commonality = pawn.RaceProps.wildBiomes?.Find( br => br.biome == Find.CurrentMap.Biome )?.commonality;
            if (commonality.HasValue) {
                interest /= commonality.Value;
            }

            return interest;
        }
    }

    public class InterestWorker_FlogIt: InterestWorker {
        public override ThingRequestGroup PotentiallyInteresting => ThingRequestGroup.Everything;
        public override float InterestFor(Thing thing) {
            if (thing is Pawn) {
                return 0f;
            }

            float interest = thing.GetStatValue( StatDefOf.MarketValue );
            if (thing.TryGetQuality(out QualityCategory quality)) {
                interest *= (int) quality + 1;
            }

            return interest;
        }
    }

    public class InterestWorker_RoyaltyWatch: InterestWorker {
        public override ThingRequestGroup PotentiallyInteresting => ThingRequestGroup.Pawn;
        private static float? _maxRelationImportance;
        protected static float MaxRelationImportance => _maxRelationImportance ??= DefDatabase<PawnRelationDef>.AllDefsListForReading.Max(d => d.importance);

        public override float InterestFor(Thing thing) {
            if (!ModsConfig.RoyaltyActive) {
                return 0;
            }
            if (thing is not Pawn pawn) {
                return 0;
            }

            float interest = 0f;
            if (pawn.royalty?.MainTitle() is RoyalTitleDef title) {
                interest += title.favorCost;
            }

            List<DirectPawnRelation> affairs = pawn.GetLoveRelations(true);
            foreach (DirectPawnRelation affair in affairs) {
                if (affair.otherPawn.royalty?.MainTitle() is RoyalTitleDef otherTitle) {
                    interest += otherTitle.favorCost / 2f;
                }
            }

            IEnumerable<Pawn> family = pawn.relations.RelatedPawns;
            foreach (Pawn relative in family) {
                if (relative.royalty?.MainTitle() is RoyalTitleDef otherTitle) {
                    PawnRelationDef relation = pawn.GetMostImportantRelation(relative);
                    interest += relation.importance / MaxRelationImportance * otherTitle.favorCost / 3f;
                }
            }

            return interest;
        }
    }
}
