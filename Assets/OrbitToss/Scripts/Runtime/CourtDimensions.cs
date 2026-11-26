using UnityEngine;

namespace OrbitToss
{
    /// <summary>
    /// Official Boccia court geometry, taken from the World Boccia International Sport Rules
    /// 2025-2028 (v1.2.1), rule 3.1 "The Court" and the "Boccia Court Layout" /
    /// "Taping and Measurement Guidelines" page.
    ///
    /// SCALE: 1 Unity unit = 1 metre.
    ///
    /// COORDINATE SYSTEM used by the whole game:
    ///   - Origin (0,0,0) = inside edge of the BACK line (behind the athletes), at the centre of the court width.
    ///   - +Z points from the throwing boxes towards the FRONT line (the far end of the court).
    ///   - X runs across the court width: -3 m (left side line) to +3 m (right side line).
    ///   - Y is up. The court surface is at Y = 0.
    /// All official measurements are to the INSIDE of boundary lines, so boundary tape sits just outside the
    /// 12.5 m x 6 m rectangle.
    /// </summary>
    public static class CourtDimensions
    {
        // ---- Overall court (rule 3.1) ----
        public const float CourtLength = 12.5f;          // back line to front line (inside edges)
        public const float CourtWidth = 6f;              // side line to side line (inside edges)
        public const float ThrowingAreaDepth = 2.5f;     // back line to the back of the throwing line
        public const float PlayingAreaLength = 10f;      // front line to the back of the throwing line

        // ---- Throwing boxes ----
        public const int ThrowingBoxCount = 6;
        public const float ThrowingBoxWidth = 1f;

        // ---- Cross / target box ----
        public const float CrossDistanceFromFrontLine = 5f;  // inside of front line to centre of the cross
        public const float TargetBoxInsideSize = 0.35f;      // 35 cm x 35 cm (inside dimension)
        public const float CrossLegLength = 0.25f;           // each leg must be 15-35 cm; we use 25 cm

        // ---- V line ----
        public const float VLineDistanceAtSides = 3f;    // back of throwing line to front of V line (at the side lines)
        public const float VLineDistanceAtApex = 1.5f;   // back of throwing line to the front vertex of the V

        // ---- Tape widths (rule 3.1: wide tape 4-7 cm, narrow tape 1.9-2.6 cm) ----
        public const float WideTape = 0.05f;    // boundary lines, throwing line, V line
        public const float NarrowTape = 0.02f;  // throwing-box dividers, target box, cross

        // ---- Ball specification (rule 4: 275 g +/- 12 g, circumference 270 mm +/- 8 mm) ----
        public const float BallMassKg = 0.275f;
        public const float BallCircumference = 0.27f;
        public static float BallRadius => BallCircumference / (2f * Mathf.PI); // ~0.043 m

        // ---- Derived positions ----
        public static float BackLineZ => 0f;
        public static float ThrowingLineZ => ThrowingAreaDepth;          // 2.5 m
        public static float FrontLineZ => CourtLength;                   // 12.5 m
        public static float HalfWidth => CourtWidth * 0.5f;              // 3 m
        public static float VLineSideZ => ThrowingLineZ + VLineDistanceAtSides;  // 5.5 m
        public static float VLineApexZ => ThrowingLineZ + VLineDistanceAtApex;   // 4.0 m

        /// <summary>Centre of the cross (5 m from the front line, 3 m from each side line) = 7.5 m from the back line.</summary>
        public static Vector3 CrossCentre => new Vector3(0f, 0f, FrontLineZ - CrossDistanceFromFrontLine);

        /// <summary>X coordinate of the centre of throwing box 1..6 (numbered left to right as seen from the throwing end).</summary>
        public static float ThrowingBoxCentreX(int boxNumber)
        {
            boxNumber = Mathf.Clamp(boxNumber, 1, ThrowingBoxCount);
            return -HalfWidth + (boxNumber - 0.5f) * ThrowingBoxWidth;
        }

        /// <summary>True if a court-plane position lies inside the playing area (in front of the throwing line, inside the boundary lines).</summary>
        public static bool IsInsidePlayingArea(Vector3 worldPosition)
        {
            return Mathf.Abs(worldPosition.x) < HalfWidth
                && worldPosition.z > ThrowingLineZ
                && worldPosition.z < FrontLineZ;
        }

        /// <summary>True if the position has crossed the V line (the Jack must cross it completely to be valid).</summary>
        public static bool IsBeyondVLine(Vector3 worldPosition)
        {
            float t = Mathf.Clamp01(Mathf.Abs(worldPosition.x) / HalfWidth);
            float vLineZAtX = Mathf.Lerp(VLineApexZ, VLineSideZ, t);
            return worldPosition.z > vLineZAtX;
        }
    }
}
