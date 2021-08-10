// Patch_DoPlaySettingsGlobalControls.cs
// Copyright Karel Kroeze, 2019-2019

using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FollowMe {
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class Patch_DoPlaySettingsGlobalControls {
        public static void Postfix(WidgetRow row, bool worldView) {
            if (!worldView) {
                if (CinematicCameraManager.currentCamera != null) {
                    GUI.color = GenUI.MouseoverColor;
                }

                if (row.ButtonIcon(Resources.cameraIcon, "Fluffy.FollowMe.CinematicCamera".Translate())) {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (CinematicCamera camera in CinematicCameraManager.Cameras) {
                        options.Add(new FloatMenuOption(camera.LabelCap, () => CinematicCameraManager.Start(camera)));
                    }

                    options.Add(new FloatMenuOption("Fluffy.FollowMe.CinematicCamera.Off".Translate(),
                                                      () => CinematicCameraManager.Stop()));
                    Find.WindowStack.Add(new FloatMenu(options));
                }

                GUI.color = Color.white;
            }
        }
    }
}
