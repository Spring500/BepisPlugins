﻿using ExtensibleSaveFormat;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Sideloader.AutoResolver
{
    public static class Hooks
    {
        private static ChaFileCustom lastLoadedInstance = null;

        public static void InstallHooks()
        {
            ExtendedSave.CardBeingLoaded += ExtendedCardLoad;
            ExtendedSave.CardBeingSaved += ExtendedCardSave;

            var harmony = HarmonyInstance.Create(UniversalAutoResolver.UARExtID);


            MethodInfo original = AccessTools.Method(typeof(ChaFileCustom), "LoadBytes");

            HarmonyMethod postfix = new HarmonyMethod(typeof(Hooks).GetMethod("LoadBytesPostHook"));

            harmony.Patch(original, null, postfix);

            BepInEx.BepInLogger.Log("Installed hooks");
        }

        private static void ExtendedCardLoad(ChaFile file)
        {
            UniversalAutoResolver.ResolveStructure(StructReference.ChaFileFaceProperties, lastLoadedInstance.face, file);
        }

        private static void ExtendedCardSave(ChaFile file)
        {
            List<ResolveInfo> resolutionInfo = new List<ResolveInfo>();

            foreach (var kv in StructReference.ChaFileFaceProperties)
            {
                var info = UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == kv.Key &&
                                                                                     x.Slot == (int)kv.Value.GetValue(file.custom.face, null));
            }

            resolutionInfo.Add(UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Pupil1" &&
                                                                                         x.Slot == file.custom.face.pupil[0].id));
            resolutionInfo.Add(UniversalAutoResolver.LoadedResolutionInfo.FirstOrDefault(x => x.Property == "Pupil2" &&
                                                                                         x.Slot == file.custom.face.pupil[1].id));
            
            

            ExtendedSave.SetExtendedDataById(file, UniversalAutoResolver.UARExtID, new PluginData
            {
                data = new Dictionary<string, object> 
                {
                    { "info",  resolutionInfo.Where(x => x != null).Select(x => x.Serialize()).ToList() } 
                }
            });
        }

        public static void LoadBytesPostHook(ChaFileCustom __instance)
        {
            lastLoadedInstance = __instance;
        }
    }
}
