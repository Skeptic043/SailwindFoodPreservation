using System;

namespace SailwindFoodPreservation
{
    internal static class PreservationMath
    {
        internal static float ScaleSmoking(float vanilla, float multiplier)
        {
            return vanilla > 0f ? vanilla * multiplier : vanilla;
        }

        internal static float ScaleAmbientDrying(float vanilla, float drying, float saltedDrying,
            float salted)
        {
            if (vanilla <= 0f) return vanilla;
            float saltStrength = float.IsNaN(salted) ? 0f : Math.Max(0f, Math.Min(1f, salted));
            float extraSaltFactor = 1f + (saltedDrying - 1f) * saltStrength;
            return vanilla * drying * extraSaltFactor;
        }
    }
}
