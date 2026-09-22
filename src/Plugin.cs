using System;
using BepInEx;
using HarmonyLib;

namespace SailwindFoodPreservation
{
    [BepInPlugin(Id, "Sailwind Food Preservation", "0.1.0")]
    [BepInProcess("Sailwind.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal const string Id = "skeptic043.sailwind.foodpreservation";
        private static Plugin instance;
        private PluginSettings settings;
        private Harmony harmony;

        private void Awake()
        {
            instance = this;
            settings = new PluginSettings(Config);
            harmony = new Harmony(Id);

            try
            {
                var smoked = AccessTools.DeclaredMethod(typeof(FoodState), nameof(FoodState.AddSmoked),
                    new[] { typeof(float) });
                if (smoked == null) throw new MissingMethodException("FoodState.AddSmoked(float)");
                harmony.Patch(smoked, prefix: new HarmonyMethod(typeof(Plugin), nameof(BeforeAddSmoked)));
            }
            catch (Exception error)
            {
                Logger.LogWarning($"Smoking speed hook unavailable; vanilla smoking continues. {error.Message}");
            }

            try
            {
                var update = AccessTools.DeclaredMethod(typeof(FoodState), nameof(FoodState.Update), Type.EmptyTypes);
                if (update == null) throw new MissingMethodException("FoodState.Update()");
                harmony.Patch(update, transpiler: new HarmonyMethod(typeof(AmbientDryingPatch),
                    nameof(AmbientDryingPatch.Transpiler)));
                if (!AmbientDryingPatch.Matched)
                    Logger.LogWarning("Ambient drying layout changed; vanilla drying continues.");
            }
            catch (Exception error)
            {
                Logger.LogWarning($"Drying speed hook unavailable; vanilla drying continues. {error.Message}");
            }
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            if (ReferenceEquals(instance, this)) instance = null;
        }

        private static void BeforeAddSmoked(FoodState __instance, ref float amount)
        {
            var plugin = instance;
            if (plugin?.settings == null || amount <= 0f) return;
            FoodCategory category = FoodClassifier.Classify(__instance);
            float multiplier = plugin.settings.Smoking(category);
            float original = amount;
            amount = PreservationMath.ScaleSmoking(amount, multiplier);
            plugin.LogDelta("smoking", __instance, category, original, multiplier, amount);
        }

        internal static float ScaleAmbientDrying(FoodState food, float vanilla)
        {
            var plugin = instance;
            if (plugin?.settings == null || vanilla <= 0f) return vanilla;
            FoodCategory category = FoodClassifier.Classify(food);
            float drying = plugin.settings.Drying(category);
            float salted = plugin.settings.SaltedDrying(category);
            float result = PreservationMath.ScaleAmbientDrying(vanilla, drying, salted, food.salted);
            plugin.LogDelta("drying", food, category, vanilla, result / vanilla, result);
            return result;
        }

        private void LogDelta(string process, FoodState food, FoodCategory category,
            float vanilla, float multiplier, float result)
        {
            if (!settings.DebugLogging.Value) return;
            Logger.LogInfo($"{process}: {food.gameObject.name}; category={category}; " +
                $"vanilla={vanilla:R}; multiplier={multiplier:R}; final={result:R}");
        }
    }
}
