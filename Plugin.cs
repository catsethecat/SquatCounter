using IPA;
using IPALogger = IPA.Logging.Logger;

using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using TMPro;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace SquatCounter
{
    public class Config
    {
        public static Config Instance { get; set; }
        public virtual long Timestamp { get; set; } = 0;
    }

    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }

        float wallHeight = 1.1f;
        int squatCount = 0;

        [Init]
        public Plugin(IPALogger logger, IPA.Config.Config config)
        {
            Instance = this;
            Log = logger;
            Config.Instance = config.Generated<Config>();
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Plugin.Log.Info("meow");
            new Thread(new ThreadStart(SquatCounterThread)).Start();
            Harmony harmony = new Harmony("Catse.BeatSaber.SquatCounter");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            if(DateTimeOffset.Now.ToUnixTimeSeconds() > Config.Instance.Timestamp + 3600)
                File.WriteAllText("UserData\\SquatCount.txt", "0");
            squatCount = Convert.ToInt32(File.ReadAllText("UserData\\SquatCount.txt"));
        }

        [OnExit]
        public void OnApplicationQuit()
        {

        }

        [HarmonyPatch(typeof(PlayerHeightSettingsController), nameof(PlayerHeightSettingsController.RefreshUI))]
        class Patch1
        {
            static void Postfix(ref TextMeshProUGUI ____text, ref float ____value)
            {
                Plugin.Instance.wallHeight = Mathf.Max(1.1f, 1.1f + ((____value - 1.4f) * 0.5f));
            }
        }

        [HarmonyPatch(typeof(LevelStatsView), "ShowStats")]
        class Patch2
        {
            static void Postfix()
            {
                Config.Instance.Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            }
        }

        void SquatCounterThread()
        {
            while (true)
            {
                var inputDevices = new List<UnityEngine.XR.InputDevice>();
                UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.Head, inputDevices);
                if (inputDevices.Count == 1)
                {
                    bool squatting = true;
                    while (inputDevices[0].isValid)
                    {
                        inputDevices[0].TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 pos);
                        inputDevices[0].TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion ang);
                        float headY = pos.y + (ang * Vector3.forward * -0.1f).y;
                        if (headY > wallHeight+0.1f)
                            squatting = false;
                        if (headY < (wallHeight-0.1f) && !squatting)
                        {
                            squatting = true;
                            squatCount++;
                            File.WriteAllText("UserData\\SquatCount.txt", "" + squatCount);
                        }
                        Thread.Sleep(11);
                    }
                }
                Thread.Sleep(1000);
            }
        }


    }
}
