using System;
using System.Globalization;
using System.Linq;
using PerfectRandom.Sulfur.Core.CharacterStats;
using PerfectRandom.Sulfur.Core.Stats;
using System.Collections.Generic;

namespace SulfurWeaponTiers
{
    public static class WeaponTierSystem
    {
        public const uint Source = 0x53575431; // Runtime tag (the save marker lives separately so vanilla can ignore it...)
        public const int SaveSignature = 537200;  // Tags our saved tier data...

        public static readonly string[] Names =
        {
            "Standard",
            "Blessed",
            "Hallowed",
            "Sacred",
            "Divine"
        };

        public static readonly string[] Colors =
        {
            "FFFFFF",
            "469247",
            "48CAE4",
            "EC78CB",
            "FFD700"
        };

        public static readonly double[] DefaultWeights =
        {
            70,
            20,
            7,
            2.5,
            0.5
        };

        public static readonly float[] DamageMultipliers =
        {
            1f,
            1.25f,
            1.5f,
            1.75f,
            2f
        };

        public static readonly float[] FireRateMultipliers = 
        { 
            1f, 
            1.05f, 
            1.10f, 
            1.15f, 
            1.20f 
        };

        public static readonly float[] ReloadSpeedMultipliers = 
        { 
            1f, 
            1.05f,
            1.10f,
            1.15f,
            1.20f 
        };

        public static readonly float[] DurabilityMultipliers = 
        { 
            1f, 
            1.10f, 
            1.20f,
            1.35f, 
            1.50f 
        };

        public static int Roll(double sample, double[] weights)
        {
            if(weights.Length != 5 || weights.Any(w => double.IsNaN(w) || double.IsInfinity(w) || w < 0) || weights.Sum() <= 0)
            {
                throw new ArgumentException("Invalid weights!");
            }

            // Clamp sample and scale by the total weight...
            double target = Math.Max(0, Math.Min(sample, 0.9999999999999999)) * weights.Sum();

            for(int i = 0; i < weights.Length; i++)
            {
                target -= weights[i];

                if(target < 0)
                {
                    return i;
                }
            }

            return 4;
        }

        public static int Read(CharacterStat damage)
        {
            StatModifier marker = damage?.statModifiers?.FirstOrDefault(m => m.SourceId == Source && m.Order >= 201 && m.Order <= 204);
            return marker == null ? 0 : marker.Order - 200;
        }

        public static int Read(ItemStats stats)
        {
            return stats == null ? 0 : Read(stats.itemAttributes[(int)ItemAttributes.Damage]);
        }

        private static bool IsMarker(ItemAttributeData data)
        {
            return data.id == ItemAttributes.None && data.value != null && data.value.BaseValue >= SaveSignature + 1 && data.value.BaseValue <= SaveSignature + 4 && data.value.BaseValue == (int)data.value.BaseValue;
        }

        public static int Read(ItemAttributeCollectionData data)
        {
            ItemAttributeData marker = data?.itemAttributes?.FirstOrDefault(IsMarker);
            return marker == null ? 0 : (int)marker.value.BaseValue - SaveSignature;
        }

        public static void Write(ItemAttributeCollectionData data, int tier)
        {
            if(tier < 1 || tier > 4)
            {
                return;
            }

            List<ItemAttributeData> entriesList = data.itemAttributes.Where(a => !IsMarker(a)).ToList();

            // Replace marker instead of stuffing another one into the save...
            entriesList.Add(new ItemAttributeData(ItemAttributes.None, new CharacterStat(SaveSignature + tier)));
            data.itemAttributes = entriesList.ToArray();
        }

        public static string Label(int tier)
        {
            return $"<color=#{ Colors[tier] }>({ Names[tier] })</color>";
        }

        public static float Factor(ItemAttributes attribute, int tier)
        {
            switch(attribute)
            {
                case ItemAttributes.Damage:
                {
                    return DamageMultipliers[tier];
                }

                case ItemAttributes.RPM:
                {
                    return FireRateMultipliers[tier];
                }

                case ItemAttributes.ReloadSpeed:
                {
                    return ReloadSpeedMultipliers[tier];
                }

                case ItemAttributes.MaxDurability:
                {
                    return DurabilityMultipliers[tier];
                }

                default:
                {
                    return 1f;
                }
            }
        }

        public static string ColoredMultiplier(ItemAttributes attribute, int tier)
        {
            if(tier == 0)
            {
                return string.Empty;
            }

            // Use actual stat multiplier so tooltip never makes up different bonus...
            string multiplier = Factor(attribute, tier).ToString("0.##", CultureInfo.InvariantCulture);
            return $"<color=#{ Colors[tier] }>x{ multiplier }</color>";
        }

        public static void Apply(ItemStats stats, int tier)
        {
            if(tier < 1 || tier > 4)
            {
                return;
            }

            for(int i = 0; i < stats.activeItemAttributes.Count; i++)
            {
                Refresh(stats, (ItemAttributes)stats.activeItemAttributes[i], tier);
            }
        }

        public static void Refresh(ItemStats stats, ItemAttributes attribute, int tier)
        {
            CharacterStat characterStat = stats.itemAttributes[(int)attribute];

            if(characterStat == null || tier == 0)
            {
                return;
            }

            // Strip old bonus first, repeated updates would stack obviously...
            characterStat.RemoveAllModifiersFromSource(Source);
            float factor = Factor(attribute, tier);

            if(factor != 1f || attribute == ItemAttributes.Damage)
            {
                characterStat.AddModifier(new StatModifier(characterStat.BaseValue * (factor - 1f), StatModType.Flat, 200 + tier, Source));
            }
        }
    }
}
