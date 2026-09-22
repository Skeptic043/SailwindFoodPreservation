using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SailwindFoodPreservation
{
    internal static class FoodClassifier
    {
        private static readonly FieldInfo OceanFish = AccessTools.Field(typeof(OceanFishes), "fishPrefabs");
        private static readonly FieldInfo LocalRegions = AccessTools.Field(typeof(OceanFishes), "localFishesRegions");
        private static readonly FieldInfo LocalFish = AccessTools.Field(typeof(LocalFishesRegion), "localFishPrefabs");
        private static PrefabsDirectory cachedDirectory;
        private static OceanFishes cachedOcean;
        private static Dictionary<int, FoodCategory> categories;
        private static int nextBuildFrame;

        internal static FoodCategory Classify(FoodState state)
        {
            if (!state) return FoodCategory.Unknown;
            var directory = PrefabsDirectory.instance;
            var ocean = OceanFishes.instance;
            if (!directory || !ocean || directory.directory == null) return FoodCategory.Unknown;
            bool sameSources = ReferenceEquals(cachedDirectory, directory) &&
                               ReferenceEquals(cachedOcean, ocean);
            if (categories == null && sameSources && Time.frameCount < nextBuildFrame)
                return FoodCategory.Unknown;
            if (categories == null || !sameSources)
            {
                cachedDirectory = directory;
                cachedOcean = ocean;
                try
                {
                    categories = Build(directory.directory, ocean);
                    if (categories == null) nextBuildFrame = Time.frameCount + 300;
                }
                catch (Exception error)
                {
                    // Keep an empty map for this scene so a changed prefab layout
                    // does not throw on every FoodState tick.
                    categories = new Dictionary<int, FoodCategory>();
                    Debug.LogWarning($"Food Preservation: food classification unavailable; using vanilla rates. {error.Message}");
                }
            }
            if (categories == null) return FoodCategory.Unknown;
            var saveable = state.GetComponent<SaveablePrefab>();
            if (!saveable) return FoodCategory.Unknown;
            return categories.TryGetValue(saveable.prefabIndex, out FoodCategory result)
                ? result : FoodCategory.Unknown;
        }

        private static Dictionary<int, FoodCategory> Build(GameObject[] prefabs, OceanFishes ocean)
        {
            // An incomplete fish catalog makes the meat/produce fallback unsafe.
            if (OceanFish == null || LocalRegions == null || LocalFish == null)
                throw new MissingFieldException("OceanFishes/LocalFishesRegion fish catalog fields");
            GameObject[] oceanFish;
            LocalFishesRegion[] regions;
            try
            {
                oceanFish = OceanFish.GetValue(ocean) as GameObject[];
                regions = LocalRegions.GetValue(ocean) as LocalFishesRegion[];
            }
            catch (Exception) { return null; }
            if (oceanFish == null || oceanFish.Length == 0 || regions == null) return null;

            var fishIndices = new HashSet<int>();
            if (!AddFish(oceanFish, fishIndices)) return null;
            foreach (var region in regions)
            {
                if (!region) return null;
                var localFish = LocalFish.GetValue(region) as GameObject[];
                if (localFish == null || !AddFish(localFish, fishIndices)) return null;
            }

            var slices = new HashSet<int>();
            for (int i = 0; i < prefabs.Length; i++)
            {
                var prefab = prefabs[i];
                if (!prefab) continue;
                var state = prefab.GetComponent<FoodState>();
                if (state && state.slicePrefabIndex > 0) slices.Add(state.slicePrefabIndex);
            }

            var result = new Dictionary<int, FoodCategory>();
            for (int i = 0; i < prefabs.Length; i++)
            {
                var prefab = prefabs[i];
                if (!prefab || slices.Contains(i)) continue;
                var state = prefab.GetComponent<FoodState>();
                var food = prefab.GetComponent<ShipItemFood>();
                if (!state || !food) continue;
                FoodCategory category = ClassifyWhole(fishIndices.Contains(i), food.GetProtein(),
                    food.GetVitamins(), food.ShowRaw());
                if (category != FoodCategory.Unknown) result[i] = category;
            }

            // Parent identity wins over a slice's own nutrition fields; slices
            // can have zero protein/vitamins even when their whole item does not.
            for (int i = 0; i < prefabs.Length; i++)
            {
                var prefab = prefabs[i];
                if (!prefab || !result.TryGetValue(i, out FoodCategory category)) continue;
                var state = prefab.GetComponent<FoodState>();
                if (!state || state.slicePrefabIndex <= 0 || state.slicePrefabIndex >= prefabs.Length)
                    continue;
                int slice = state.slicePrefabIndex;
                if (result.TryGetValue(slice, out FoodCategory existing) && existing != category)
                    result[slice] = FoodCategory.Unknown;
                else
                    result[slice] = category;
            }
            return result;
        }

        private static bool AddFish(GameObject[] fish, HashSet<int> indices)
        {
            foreach (var prefab in fish)
            {
                if (!prefab) return false;
                var saveable = prefab.GetComponent<SaveablePrefab>();
                if (!saveable) return false;
                indices.Add(saveable.prefabIndex);
            }
            return true;
        }

        internal static FoodCategory ClassifyWhole(bool fish, float protein, float vitamins, bool raw)
        {
            if (fish) return FoodCategory.Fish;
            if (protein > 0f && vitamins <= 0f && raw) return FoodCategory.Meat;
            if (vitamins > 0f || (protein <= 0f && raw)) return FoodCategory.Produce;
            return FoodCategory.Unknown;
        }
    }
}
