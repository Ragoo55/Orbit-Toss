using UnityEngine;

namespace OrbitToss
{
    /// <summary>
    /// Distance helpers. Distances are measured centre-to-centre between a ball and the Jack.
    /// Because both rest on the same flat court, the displayed value is the horizontal (court-plane, XZ) distance.
    /// </summary>
    public static class DistanceCalculator
    {
        /// <summary>Full 3D centre-to-centre distance.</summary>
        public static float Distance3D(Vector3 ballCentre, Vector3 jackCentre)
        {
            return Vector3.Distance(ballCentre, jackCentre);
        }

        /// <summary>Centre-to-centre distance on the court plane (ignores height).</summary>
        public static float CourtPlaneDistance(Vector3 ballCentre, Vector3 jackCentre)
        {
            float dx = ballCentre.x - jackCentre.x;
            float dz = ballCentre.z - jackCentre.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// Readable text: below 1 m shows whole centimetres ("42 cm"), otherwise metres with 2 decimals ("1.37 m").
        /// </summary>
        public static string Format(float metres)
        {
            if (float.IsNaN(metres) || float.IsInfinity(metres) || metres < 0f) return "--";
            if (metres < 1f) return $"{Mathf.RoundToInt(metres * 100f)} cm";
            return $"{metres:0.00} m";
        }
    }
}
