using System;

namespace Nutrion.Lib.GameLogic.Helpers
{
    /// <summary>
    /// Provides coordinate math utilities for axial hex grids.
    /// </summary>
    public static class HexHelper
    {
        /// <summary>
        /// Returns the hex distance between two axial coordinates.
        /// </summary>
        public static int Distance(int q1, int r1, int q2, int r2)
        {
            int dq = q1 - q2;
            int dr = r1 - r2;
            return (Math.Abs(dq) + Math.Abs(dq + dr) + Math.Abs(dr)) / 2;
        }

        /// <summary>
        /// Returns true if the second hex is within the given radius of the first.
        /// </summary>
        public static bool WithinRadius(int q1, int r1, int q2, int r2, int radius)
        {
            return Distance(q1, r1, q2, r2) <= radius;
        }
        public static IEnumerable<(int Q, int R)> GetHexCoordsInRadius(int q, int r, int radius)
        {
            for (int dq = -radius; dq <= radius; dq++)
            {
                int minDr = Math.Max(-radius, -dq - radius);
                int maxDr = Math.Min(radius, -dq + radius);
                for (int dr = minDr; dr <= maxDr; dr++)
                {
                    yield return (q + dq, r + dr);
                }
            }
        }

    }
}
