// Settings.cs
// Copyright Karel Kroeze, -2020

using BetterKeybinding;
using UnityEngine;
using Verse;

namespace FollowMe {
    public class Settings: ModSettings {
        public static  bool    showNotifications = true;
        public static  bool    playSounds        = true;
        public static  bool    edgeDetection     = true;
        private static KeyBind _followMeKey;

        public static KeyBind FollowMeKey {
            get {
                _followMeKey ??= new KeyBind("Fluffy.FollowMe.KeyBinds.FollowMe".Translate(), KeyCode.Home);
                return _followMeKey;
            }
        }

        private static KeyBind _cinematicCameraKey;

        public static KeyBind CinematicCameraKey {
            get {
                _cinematicCameraKey ??= new KeyBind("Fluffy.FollowMe.KeyBinds.CinematicCamera".Translate(), KeyCode.End);
                return _cinematicCameraKey;
            }
        }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref showNotifications, "showNotifications", true);
            Scribe_Values.Look(ref playSounds, "playSounds", true);
            Scribe_Values.Look(ref edgeDetection, "edgeDetection", true);
            Scribe_Deep.Look(ref _followMeKey, "followMeKey");
            Scribe_Deep.Look(ref _cinematicCameraKey, "cinematicCameraKey");
        }

        public static void DoWindowContents(Rect rect) {
            Listing_Standard list = new Listing_Standard();
            list.Begin(rect);
            list.CheckboxLabeled("FollowMe.Notifications".Translate(), ref showNotifications,
                                  "FollowMe.Notifications.Tooltip".Translate());
            list.CheckboxLabeled("FollowMe.Sounds".Translate(), ref playSounds,
                                  "FollowMe.Sounds.Tooltip".Translate());
            list.CheckboxLabeled("FollowMe.EdgeDetection".Translate(), ref edgeDetection,
                                  "FollowMe.EdgeDetection.Tooltip".Translate());
            list.Gap();
            list.Label("Fluffy.FollowMe.KeyBinds".Translate());
            FollowMeKey.Draw(list.GetRect(30));
            CinematicCameraKey.Draw(list.GetRect(30));
            list.End();
        }
    }
}
