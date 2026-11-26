using System;
using System.Collections.Generic;

namespace OrbitToss
{
    /// <summary>One measured ball at the end of an end (or after it settles in practice).</summary>
    public struct BallMeasurement
    {
        public int Side;          // 0 = side A (e.g. red), 1 = side B (e.g. blue)
        public float Distance;    // centre-to-centre distance to the Jack, metres
        public bool IsValid;      // false for balls that are out of court / dead
        public string Id;

        public BallMeasurement(int side, float distance, bool isValid = true, string id = null)
        {
            Side = side;
            Distance = distance;
            IsValid = isValid;
            Id = id;
        }
    }

    /// <summary>Result of scoring one end.</summary>
    public struct EndResult
    {
        public int PointsSideA;
        public int PointsSideB;
        /// <summary>-1 when nobody scored, 0 or 1 for the scoring side, 2 when both sides scored (equidistant case).</summary>
        public int ScoringSide;
        public bool EquidistantTie;
    }

    /// <summary>Kinds of practice feedback, so UI can show a label + icon (not colour alone).</summary>
    public enum FeedbackKind { Perfect, Good, Close, TryAgain, Out }

    /// <summary>Practice feedback for a single throw.</summary>
    public struct PracticeFeedback
    {
        public FeedbackKind Kind;
        public string Label;
        public int PracticePoints;
    }

    /// <summary>
    /// Inspector-tunable distance thresholds for motivational practice feedback.
    /// These labels are NOT official referee decisions.
    /// </summary>
    [Serializable]
    public class FeedbackThresholds
    {
        [UnityEngine.Tooltip("Distance (m) at or below which a throw is 'Perfect'.")]
        public float perfectMaxDistance = 0.15f;
        [UnityEngine.Tooltip("Distance (m) at or below which a throw is 'Good Throw'.")]
        public float goodMaxDistance = 0.40f;
        [UnityEngine.Tooltip("Distance (m) at or below which a throw is 'Close'.")]
        public float closeMaxDistance = 0.80f;

        public string perfectLabel = "Perfect!";
        public string goodLabel = "Good Throw";
        public string closeLabel = "Close";
        public string tryAgainLabel = "Try Again";
        public string outLabel = "Out of Court";

        public int perfectPoints = 3;
        public int goodPoints = 2;
        public int closePoints = 1;
    }

    /// <summary>
    /// Boccia scoring service. Pure C# so it can be unit tested and later extended to full match rules.
    ///
    /// Official rule (World Boccia 2025-2028, rule 10.16.1): after both sides have played all balls, the side with the
    /// ball closest to the Jack scores one point for each of its balls closer to the Jack than the opponent's closest ball.
    /// Rule 10.16.2: if balls of different colours are equidistant and no other balls are closer, each side scores one
    /// point per equidistant ball.
    /// </summary>
    public class BocciaScoring
    {
        /// <summary>Two distances closer than this (metres) are treated as equidistant.</summary>
        public float EquidistantTolerance { get; set; } = 0.002f;

        public EndResult ScoreEnd(IReadOnlyList<BallMeasurement> balls)
        {
            var result = new EndResult { ScoringSide = -1 };
            if (balls == null || balls.Count == 0) return result;

            float closestA = ClosestDistance(balls, 0);
            float closestB = ClosestDistance(balls, 1);
            bool hasA = !float.IsPositiveInfinity(closestA);
            bool hasB = !float.IsPositiveInfinity(closestB);

            if (!hasA && !hasB) return result;

            // Prototype interpretation: if one side has no valid ball on court, every valid ball of the other side scores.
            if (hasA && !hasB) { result.PointsSideA = CountValid(balls, 0); result.ScoringSide = 0; return result; }
            if (hasB && !hasA) { result.PointsSideB = CountValid(balls, 1); result.ScoringSide = 1; return result; }

            if (Math.Abs(closestA - closestB) <= EquidistantTolerance)
            {
                // Equidistant closest balls: each side scores one point per ball at that shared closest distance.
                float tied = Math.Min(closestA, closestB);
                result.PointsSideA = CountWithin(balls, 0, tied + EquidistantTolerance);
                result.PointsSideB = CountWithin(balls, 1, tied + EquidistantTolerance);
                result.ScoringSide = 2;
                result.EquidistantTie = true;
                return result;
            }

            if (closestA < closestB)
            {
                result.ScoringSide = 0;
                result.PointsSideA = CountCloserThan(balls, 0, closestB - EquidistantTolerance);
            }
            else
            {
                result.ScoringSide = 1;
                result.PointsSideB = CountCloserThan(balls, 1, closestA - EquidistantTolerance);
            }
            return result;
        }

        /// <summary>Index of the closest valid ball in the list (any side), or -1.</summary>
        public static int IndexOfClosest(IReadOnlyList<BallMeasurement> balls)
        {
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < balls.Count; i++)
            {
                if (!balls[i].IsValid) continue;
                if (balls[i].Distance < bestDistance)
                {
                    bestDistance = balls[i].Distance;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>Practice feedback for one throw (motivational, not an official decision).</summary>
        public static PracticeFeedback EvaluatePractice(float distance, bool isInPlay, FeedbackThresholds t)
        {
            if (t == null) t = new FeedbackThresholds();
            if (!isInPlay) return new PracticeFeedback { Kind = FeedbackKind.Out, Label = t.outLabel, PracticePoints = 0 };
            if (distance <= t.perfectMaxDistance) return new PracticeFeedback { Kind = FeedbackKind.Perfect, Label = t.perfectLabel, PracticePoints = t.perfectPoints };
            if (distance <= t.goodMaxDistance) return new PracticeFeedback { Kind = FeedbackKind.Good, Label = t.goodLabel, PracticePoints = t.goodPoints };
            if (distance <= t.closeMaxDistance) return new PracticeFeedback { Kind = FeedbackKind.Close, Label = t.closeLabel, PracticePoints = t.closePoints };
            return new PracticeFeedback { Kind = FeedbackKind.TryAgain, Label = t.tryAgainLabel, PracticePoints = 0 };
        }

        // ---- helpers ----

        private static float ClosestDistance(IReadOnlyList<BallMeasurement> balls, int side)
        {
            float best = float.PositiveInfinity;
            foreach (var b in balls)
                if (b.IsValid && b.Side == side && b.Distance < best) best = b.Distance;
            return best;
        }

        private static int CountValid(IReadOnlyList<BallMeasurement> balls, int side)
        {
            int n = 0;
            foreach (var b in balls) if (b.IsValid && b.Side == side) n++;
            return n;
        }

        private static int CountCloserThan(IReadOnlyList<BallMeasurement> balls, int side, float limit)
        {
            int n = 0;
            foreach (var b in balls) if (b.IsValid && b.Side == side && b.Distance < limit) n++;
            return n;
        }

        private static int CountWithin(IReadOnlyList<BallMeasurement> balls, int side, float limit)
        {
            int n = 0;
            foreach (var b in balls) if (b.IsValid && b.Side == side && b.Distance <= limit) n++;
            return n;
        }
    }
}
