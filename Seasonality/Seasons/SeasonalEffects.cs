﻿using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using static Seasonality.SeasonalityPlugin;
using static Seasonality.Seasons.CustomTextures;

namespace Seasonality.Seasons;

public static class SeasonalEffects
{
    public static readonly List<SeasonEffect> SeasonEffectList = new();
    private static Season currentSeason = _Season.Value;
    private static int SeasonIndex = (int)_Season.Value; // Get index from config saved value
    private static DateTime LastSeasonChange = DateTime.Now;
    
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateStatusEffects))]
    static class HudUpdateStatusEffectPatch
    {
        private static void Postfix(Hud __instance, List<StatusEffect> statusEffects)
        {
            if (!__instance) return;
            StatusEffect? seasonEffect = statusEffects.Find(x => SeasonEffectList.Exists(y => y.name == x.name));
            if (!seasonEffect) return;
            int index = statusEffects.FindIndex(x => x.name == seasonEffect.name);
            RectTransform? rectTransform = __instance.m_statusEffects[index];
            if (!rectTransform) return;
            Transform? timeText = rectTransform.Find("TimeText");
            if (!timeText) return;
            timeText.TryGetComponent(out TMP_Text tmpText);
            if (!tmpText) return;
            
            if (_SeasonDuration.Value == 0)
            {
                tmpText.gameObject.SetActive(false);
                return;
            }
            
            int remainingDays = (EnvMan.instance.GetCurrentDay() % _SeasonDuration.Value) + 1;
            float fraction = EnvMan.instance.GetDayFraction(); // value between 0 - 1 - time of day
            float remainder = remainingDays - fraction;
            if (remainder < 0)
            {
                remainder += 1; // Adding a day if remainder is negative
                remainingDays -= 1;
            }
            int totalMinutes = (int)(remainder * 24 * 60);

            int hours = remainingDays - 1;
            int minutes = (totalMinutes % (24 * 60)) / 60;
            int seconds = totalMinutes % 60;

            string time = $"{hours:D2}:{minutes:D2}:{seconds:D2}";

            tmpText.gameObject.SetActive(true);
            tmpText.text = time;

            if (hours == 0 && totalMinutes < 3)
            {
                if (LastSeasonChange + TimeSpan.FromSeconds(5) > DateTime.Now) return;
                if (_Season.Value == (Season)SeasonIndex)
                {
                    SeasonIndex = (SeasonIndex + 1) % Enum.GetValues(typeof(Season)).Length;
                    _Season.Value = (Season)SeasonIndex;
                }
                else
                {
                    _Season.Value = (Season)SeasonIndex;
                    SeasonIndex = (SeasonIndex + 1) % Enum.GetValues(typeof(Season)).Length;
                }

                LastSeasonChange = DateTime.Now;
            }

            else if (_Season.Value != (Season)SeasonIndex)
            {
                // To switch it back to timer settings if configs changed
                _Season.Value = (Season)SeasonIndex;
            }
        }
    }


    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    static class PlayerUpdatePatch
    {
        private static void Postfix(Player __instance)
        {
            if (!__instance.IsPlayer() || !__instance) return;
            if (currentSeason == _Season.Value) return;
            // If season has changed, apply new seasonal effect
            ApplySeasonalEffects(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    static class PlayerAwakePatch
    {
        private static void Postfix(Player __instance)
        {
            if (!__instance.IsPlayer() || !__instance) return;
            if (!ZNetScene.instance) return;
            ApplySeasonalEffects(__instance);
        }
    }

    private static void ApplySeasonalEffects(Player __instance)
    {
        Toggle toggle = _SeasonalEffectsEnabled.Value;
        SEMan? SEMan = __instance.GetSEMan();
        if (SEMan == null) return;
        // Remove all seasonal effects
        foreach (SeasonEffect? effect in SeasonEffectList)
        {
            if (!SEMan.HaveStatusEffect(effect.name)) continue;
            SEMan.RemoveStatusEffect(effect);
        }
        ObjectDB.instance.m_StatusEffects.RemoveAll(effect => effect is SeasonEffect);
        SeasonEffectList.Clear();
        // Apply new seasonal effect
        StatusEffect? SeasonEffect = null!;
        switch (_Season.Value)
        {
            case Season.Fall:
                SeasonalEffect FallSeasonEffect = new SeasonalEffect();
                FallSeasonEffect.effectName = "fall_season";
                FallSeasonEffect.displayName = _FallName.Value;
                FallSeasonEffect.sprite = ValknutIcon;
                FallSeasonEffect.startEffectNames = new[] { "fx_DvergerMage_Support_start" };
                FallSeasonEffect.stopEffectNames = new[] { "fx_DvergerMage_Mistile_die" };
                FallSeasonEffect.startMsg = _FallStartMsg.Value;
                FallSeasonEffect.effectTooltip = _FallTooltip.Value;
                FallSeasonEffect.damageMod = _FallResistance.Value;
                FallSeasonEffect.Modifier = toggle is Toggle.On ? _FallModifier.Value : Modifier.None;
                FallSeasonEffect.m_newValue = _FallValue.Value;
                
                StatusEffect? FallEffect = FallSeasonEffect.Init();
                if (FallEffect) SeasonEffect = FallEffect;
                break;
            case Season.Spring:
                SeasonalEffect SpringSeasonEffect = new SeasonalEffect();
                SpringSeasonEffect.effectName = "spring_season";
                SpringSeasonEffect.displayName = _SpringName.Value;
                SpringSeasonEffect.sprite = ValknutIcon;
                SpringSeasonEffect.startEffectNames = new[] { "fx_DvergerMage_Support_start" };
                SpringSeasonEffect.stopEffectNames = new[] { "fx_DvergerMage_Mistile_die" };
                SpringSeasonEffect.startMsg = _SpringStartMsg.Value;
                SpringSeasonEffect.effectTooltip = _SpringTooltip.Value;
                SpringSeasonEffect.damageMod = _SpringResistance.Value;
                SpringSeasonEffect.Modifier = toggle is Toggle.On ? _SpringModifier.Value : Modifier.None;
                SpringSeasonEffect.m_newValue = toggle is Toggle.On ? _SpringValue.Value : 0;

                StatusEffect? SpringEffect = SpringSeasonEffect.Init();
                if (SpringEffect) SeasonEffect = SpringEffect;
                break;
            case Season.Winter:
                SeasonalEffect WinterSeasonEffect = new SeasonalEffect();
                WinterSeasonEffect.effectName = "winter_season";
                WinterSeasonEffect.displayName = _WinterName.Value;
                WinterSeasonEffect.sprite = ValknutIcon;
                WinterSeasonEffect.startEffectNames = new[] { "fx_DvergerMage_Support_start" };
                WinterSeasonEffect.stopEffectNames = new[] { "fx_DvergerMage_Mistile_die" };
                WinterSeasonEffect.startMsg = _WinterStartMsg.Value;
                WinterSeasonEffect.effectTooltip = _WinterTooltip.Value;
                WinterSeasonEffect.damageMod = toggle is Toggle.On ? _WinterResistance.Value : "";
                WinterSeasonEffect.Modifier = toggle is Toggle.On ? _WinterModifier.Value : Modifier.None;
                WinterSeasonEffect.m_newValue = _WinterValue.Value;

                StatusEffect? WinterEffect = WinterSeasonEffect.Init();
                if (WinterEffect) SeasonEffect = WinterEffect;
                break;
            case Season.Summer:
                SeasonalEffect SummerSeasonEffect = new SeasonalEffect();
                SummerSeasonEffect.effectName = "summer_season";
                SummerSeasonEffect.displayName = _SummerName.Value;
                SummerSeasonEffect.sprite = ValknutIcon;
                SummerSeasonEffect.startEffectNames = new[] { "fx_DvergerMage_Support_start" };
                SummerSeasonEffect.stopEffectNames = new[] { "fx_DvergerMage_Mistile_die" };
                SummerSeasonEffect.startMsg = _SummerStartMsg.Value;
                SummerSeasonEffect.effectTooltip = _SummerTooltip.Value;
                SummerSeasonEffect.Modifier = toggle is Toggle.On ? _SummerModifier.Value : Modifier.None;
                SummerSeasonEffect.m_newValue = _SummerValue.Value;
                SummerSeasonEffect.damageMod = _SummerResistance.Value;

                StatusEffect? SummerEffect = SummerSeasonEffect.Init();
                if (SummerEffect) SeasonEffect = SummerEffect;
                break;
        }

        if (SeasonEffect != null)
        {
            SEMan.AddStatusEffect(SeasonEffect);
        }

        currentSeason = _Season.Value;
    }
}