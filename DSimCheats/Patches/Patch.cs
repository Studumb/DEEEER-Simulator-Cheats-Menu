using DSimCheats;
using HarmonyLib;
using InControl;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DSimCheats
{
    [HarmonyPatch]
    internal class DStatus_Patch
    {
        [HarmonyPatch(typeof(DStatus), "ChangeHP")]
        [HarmonyPrefix]
        public static void ChangeHP_Prefix(ref float changeValue)
        {
            if (Plugin.invincible.Value)
            {
                changeValue = 0f;
            }
        }

        [HarmonyPatch(typeof(DStatus), "Awake")]
        [HarmonyPostfix]
        public static void Awake_Postfix(DStatus __instance)
        {
            Plugin.DStatus_instance = __instance;
        }
    }
    [HarmonyPatch]
    internal class RagdollPatches
    {
        [HarmonyPatch(typeof(DHitbox), "GetDamage", new[] { typeof(float), typeof(float), typeof(bool) })]
        [HarmonyPrefix]
        public static void GetDamage_Prefix(ref bool activateRagdoll)
        {
            if (Plugin.disableRagdoll.Value)
            {
                activateRagdoll = false;
            }
        }
    }

    [HarmonyPatch]
    internal class EquipmentsPatches
    {
        [HarmonyPatch(typeof(DEquip_MiniGun), "OnEquip")]
        [HarmonyPostfix]
        private static void MiniGun_OnEquip_Postfix()
        {
            if (Plugin.disableDolphin.Value)
            {
                DLCParent instance = SingletonMonoBehaviour<DLCParent>.Instance;
                if (instance)
                    instance.DolphinAcitve(false);
                else
                    return;
            }
        }

        [HarmonyPatch(typeof(DEquip_ChainSaw), "OnEquip")]
        [HarmonyPostfix]
        private static void ChainSaw_OnEquip_Postfix()
        {
            if (Plugin.disableTornado.Value)
            {
                DLCParent instance = SingletonMonoBehaviour<DLCParent>.Instance;
                if (instance)
                    instance.TornadeActive(false, false);
                else
                    return;
            }
        }
    }

    [HarmonyPatch(typeof(DEquipChecker), "Awake")]
    internal class DEquipChecker_Instance
    {
        [HarmonyPostfix]
        public static void Awake_Postfix(DEquipChecker __instance)
        {
            Plugin.DEquipChecker_instance = __instance;
        }
    }

    [HarmonyPatch]
    internal class DamageManager_Patch
    {
        [HarmonyPatch(typeof(DamageManager), "DamageFromDeer", new[] { typeof(Transform) , typeof(float), typeof(float), typeof(bool) })]
        [HarmonyPrefix]
        public static void DamageFromDeer_Prefix(ref float damage)
        {
            if (Plugin.OHKO.Value)
                damage = 1000000f;
            else if (Plugin.dmgMulti.Value > 1f)
                damage *= Plugin.dmgMulti.Value;
        }
    }
}
