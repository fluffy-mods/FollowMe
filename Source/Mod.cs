// Mod.cs
// Copyright Karel Kroeze, 2017-2017

using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FollowMe
{
    public class Mod : Verse.Mod
    {
        public Mod( ModContentPack content ) : base( content )
        {
            GetSettings<Settings>();
        }

        public override string SettingsCategory()
        {
            return "Fluffy.FollowMe".Translate();
        }

        public override void DoSettingsWindowContents( Rect inRect )
        {
            Settings.DoWindowContents( inRect );
        }

        public static void DoMessage( string message, MessageTypeDef type )
        {
            DoMessage( message, type, TargetInfo.Invalid );
        }

        public static void DoMessage( string message, MessageTypeDef type, GlobalTargetInfo target )
        {
            if ( Settings.showNotifications )
                if ( Settings.playSounds )
                    Messages.Message( message, target, type );
                else
                    Messages.Message( message, target, MessageTypeDefOf.SilentInput );
            else if ( Settings.playSounds )
                type.sound.PlayOneShotOnCamera();
        }
    }

    public class Settings : ModSettings
    {
        public static bool showNotifications = true;
        public static bool playSounds        = true;
        public static bool edgeDetection     = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look( ref showNotifications, "showNotifications", true );
            Scribe_Values.Look( ref playSounds, "playSounds", true );
            Scribe_Values.Look( ref edgeDetection, "edgeDetection", true );
        }

        public static void DoWindowContents( Rect rect )
        {
            var list = new Listing_Standard();
            list.Begin( rect );
            list.CheckboxLabeled( "FollowMe.Notifications".Translate(), ref showNotifications,
                                  "FollowMe.Notifications.Tooltip".Translate() );
            list.CheckboxLabeled( "FollowMe.Sounds".Translate(), ref playSounds,
                                  "FollowMe.Sounds.Tooltip".Translate() );
            list.CheckboxLabeled( "FollowMe.EdgeDetection".Translate(), ref edgeDetection,
                                  "FollowMe.EdgeDetection.Tooltip".Translate() );
            list.End();
        }
    }
}