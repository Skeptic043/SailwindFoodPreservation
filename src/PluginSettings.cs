using System;
using BepInEx.Configuration;

namespace SailwindFoodPreservation
{
    internal enum FoodCategory { Unknown, Fish, Meat, Produce }

    internal sealed class PluginSettings
    {
        private readonly ConfigEntry<float>[] smoking = new ConfigEntry<float>[3];
        private readonly ConfigEntry<float>[] drying = new ConfigEntry<float>[3];
        private readonly ConfigEntry<float>[] saltedDrying = new ConfigEntry<float>[3];

        internal ConfigEntry<bool> DebugLogging { get; }

        internal PluginSettings(ConfigFile config)
        {
            string[] names = { "Fish", "Meat", "Produce" };
            for (int i = 0; i < names.Length; i++)
            {
                smoking[i] = Bind(config, "Smoking", names[i]);
                drying[i] = Bind(config, "Drying", names[i]);
                saltedDrying[i] = Bind(config, "SaltedDrying", names[i]);
            }
            DebugLogging = config.Bind("Diagnostics", "DebugLogging", false,
                "Log food category and preservation increments. May write frequently while food is drying.");
        }

        private static ConfigEntry<float> Bind(ConfigFile config, string section, string name)
        {
            return config.Bind(section, name + "Multiplier", 1f,
                new ConfigDescription("Multiplier from 0.1 to 5.0; 1.0 keeps vanilla speed.",
                    new AcceptableValueRange<float>(0.1f, 5f)));
        }

        internal float Smoking(FoodCategory category) => Read(smoking, category);
        internal float Drying(FoodCategory category) => Read(drying, category);
        internal float SaltedDrying(FoodCategory category) => Read(saltedDrying, category);

        private static float Read(ConfigEntry<float>[] entries, FoodCategory category)
        {
            int index = (int)category - 1;
            if (index < 0 || index >= entries.Length) return 1f;
            float value = entries[index].Value;
            if (float.IsNaN(value) || float.IsInfinity(value)) return 1f;
            return Math.Max(0.1f, Math.Min(5f, value));
        }
    }
}
