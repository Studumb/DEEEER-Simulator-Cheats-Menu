using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EnergyBarToolkit;
using HarmonyLib;
using InControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DSimCheats
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        static public ConfigEntry<bool> disableDolphin,
                                        disableTornado,
                                        invincible,
                                        disableRagdoll,
                                        OHKO;
        static public ConfigEntry<float> dmgMulti;

        static public DEquipChecker DEquipChecker_instance;
        static public DStatus DStatus_instance;

        static FieldInfo CurrEquipTypes_ref;
        static string hInput = "3";
        static RepeatedRendererUGUI obj;
        static GameObject lightSaber;
        static int index = 0;
        static List<string> itemNames = new List<string>();
        static List<DEquipType> itemList = new List<DEquipType>();

        private void ConfigInit()
        {
            // Stats section
            Config.Bind("Stats", "Hearts", "",
                new ConfigDescription("Scaling hearts.\nNote: Adding too many hearts can freeze the game", null,
                new ConfigurationManagerAttributes
                {
                    Order = 3,
                    HideDefaultButton = true,
                    CustomDrawer = entry =>
                {
                    if (DStatus_instance == null)
                        return;

                    if (obj == null)
                    {
                        obj = UnityEngine.Object.FindObjectOfType<RepeatedRendererUGUI>();
                        return;
                    }

                    hInput = GUILayout.TextField(hInput, GUILayout.ExpandWidth(true));

                    if (GUILayout.Button("Set", GUILayout.Width(45f), GUILayout.Height(20f)))
                    {
                        if (int.TryParse(hInput, out int value))
                        {
                            obj.repeatCount = value;
                            DStatus_instance.ChangeHeartSize(value);
                            DStatus_instance.ChangeHP(0f);
                        }
                    }
                }
                }));

            invincible = Config.Bind("Stats", "Invincible", false, 
                new ConfigDescription("Disable taking damage", null, 
                new ConfigurationManagerAttributes
                {
                    Order = 2
                }));
            disableRagdoll = Config.Bind("Stats", "Disable Ragdoll", false, "Disable ragdoll when taking damage");

            dmgMulti = Config.Bind("Stats", "Damage", 1f,
                new ConfigDescription("Damager multiplier", 
                new AcceptableValueRange<float>(1f, 999f),
                new ConfigurationManagerAttributes
                {
                    Order = 1,
                }));

            OHKO = Config.Bind("Stats", "OHKO", false, "One Hit K.O");
            
            // Equipments section
            disableDolphin = Config.Bind("Equipments", "Disable Dolphin", false, 
                new ConfigDescription("Disable Dolphins when equipping Gatling Gun", null,
                new ConfigurationManagerAttributes
                {
                    Order = 4
                }));
            disableTornado = Config.Bind("Equipments", "Disable Tornado", false, 
                new ConfigDescription("Disable Tornado when equipping Chainsaw", null,
                new ConfigurationManagerAttributes
                {
                    Order = 3
                }));

            Config.Bind("Equipments", "Guns", "",
                new ConfigDescription("", null,
                new ConfigurationManagerAttributes
                {
                    Order = 2,
                    HideSettingName = true,
                    HideDefaultButton = true,
                    CustomDrawer = entry =>
                    {
                        if (DEquipChecker_instance == null)
                            return;
                        CurrEquipTypes_ref = AccessTools.Field(typeof(DEquipChecker), "CurrentEquipTypes");
                        if (GUILayout.Button("Equip All", GUILayout.ExpandWidth(true)))
                        {
                            var inv = (List<DEquipType>)CurrEquipTypes_ref.GetValue(DEquipChecker_instance);
                            foreach (DEquipType key in Enum.GetValues(typeof(DEquipType)))
                            {
                                if (!DEquipChecker_instance.DEquipments.TryGetValue(key, out var info)
                                    || key == DEquipType.DLC_GoldenGun
                                    || key == DEquipType.Juke
                                    || key == DEquipType.NoDamage
                                    || key == DEquipType.MaxPower)
                                    continue;
                                if (info.Equipment != null)
                                {
                                    if (inv.Contains(key))
                                        continue;
                                    info.Equipment.OnEquip();
                                    inv.Add(key);
                                }
                                else
                                    continue;
                            }
                            DEquipChecker_instance.AntlersCheck();
                            SoundManager.PlaySound(9, 1f, default(Vector3));
                        }

                        if (GUILayout.Button("Unequip All", GUILayout.ExpandWidth(true)))
                        {
                            var inv = (List<DEquipType>)CurrEquipTypes_ref.GetValue(DEquipChecker_instance);
                            if (inv.Count > 0)
                            {
                                foreach (DEquipType key in inv.ToArray())
                                    DEquipChecker_instance.DEquipments[key].Equipment.OnUnequip();
                                inv.Clear();
                                DEquipChecker_instance.AntlersCheck();
                                SoundManager.PlaySound(8, 1f, default(Vector3));
                            }
                        }
                    }
                }));

            Config.Bind("Equipments", "Specific Equipments", "",
                new ConfigDescription("", null,
                new ConfigurationManagerAttributes
                {
                    Order = 1,
                    HideDefaultButton = true,
                    HideSettingName = true,
                    CustomDrawer = entry =>
                    {
                        if (DEquipChecker_instance == null)
                            return;
                        var inv = (List<DEquipType>)CurrEquipTypes_ref.GetValue(DEquipChecker_instance);
                        if (itemNames.Count == 0 && itemList.Count == 0)
                        {
                            itemNames.Add("None");
                            foreach (DEquipType key in Enum.GetValues(typeof(DEquipType)))
                            {
                                if (key == DEquipType.NoDamage
                                    || key == DEquipType.MaxPower
                                    || key == DEquipType.Other
                                    || key == DEquipType.Remington
                                    || key == DEquipType.MiniGun
                                    || key == DEquipType.Rpg)
                                    continue;
                                itemNames.Add(key.ToString());
                                itemList.Add(key);
                            }
                        }
                        var newIndex = GUILayout.SelectionGrid(index, itemNames.ToArray(), 4, GUILayout.ExpandHeight(true));
                        if (newIndex != index)
                        {
                            index = newIndex;
                            var item = itemList[index - 1];
                            if (inv.Contains(item))
                            {
                                inv.Remove(item);
                                DEquipChecker_instance.DEquipments[item].Equipment.OnUnequip();
                                DEquipChecker_instance.AntlersCheck();
                                SoundManager.PlaySound(8, 1f, default(Vector3));
                                index = 0;
                            }
                            else
                            {
                                inv.Add(item);
                                DEquipChecker_instance.DEquipments[item].Equipment.OnEquip();
                                DEquipChecker_instance.AntlersCheck();
                                SoundManager.PlaySound(9, 1f, default(Vector3));
                                index = 0;
                            }
                        }
                    }
                }));

            Config.Bind("Equipments", "Swords", "",
                new ConfigDescription("", null,
                new ConfigurationManagerAttributes 
                {
                    Order = 0,
                    HideSettingName = true,
                    HideDefaultButton = true,
                    CustomDrawer = entry =>
                    {
                        if (ItemDropManager.Instance == null)
                            return;

                        if (lightSaber == null)
                        {
                            lightSaber = Resources.FindObjectsOfTypeAll<GameObject>()
                                .FirstOrDefault(go => go.name == "Grabbable Lightsaber");
                            return;
                        }

                        if (GUILayout.Button("Spawn Katana", GUILayout.ExpandWidth(true)))
                            ItemDropManager.Instance.DropKatana(DeeeerManager.DPosition);

                        if (GUILayout.Button("Spawn Lightsaber", GUILayout.ExpandWidth(true)))
                            if (lightSaber != null)
                                UnityEngine.Object.Instantiate(lightSaber, DeeeerManager.DPosition, lightSaber.transform.rotation);
                    }
                }));
            // Teleport section
            Config.Bind("Locations", "", "",
                new ConfigDescription("Retry if scene was not loaded properly", null,
                new ConfigurationManagerAttributes
                {
                    HideDefaultButton = true,
                    HideSettingName = true,
                    CustomDrawer = entry =>
                    {
                        if (GUILayout.Button("City", GUILayout.ExpandWidth(true)))
                            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("City");

                        if (GUILayout.Button("Future City", GUILayout.ExpandWidth(true)))
                            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("City Future");

                        if (GUILayout.Button("Future Self Boss", GUILayout.ExpandWidth(true)))
                            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("City Future BossDeer");

                        if (GUILayout.Button("Future Self Boss 2", GUILayout.ExpandWidth(true)))
                            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("City Future BossDeer 2");

                        if (GUILayout.Button("Final Boss", GUILayout.ExpandWidth(true)))
                            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Space");
                    }
                }));
        }

        internal static new ManualLogSource Logger;
        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            var harmony = new Harmony("DSimCheats");
            harmony.PatchAll();

            ConfigInit();
        }
    }
}