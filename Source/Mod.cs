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
        public static Settings Settings { get; private set; }
        public Mod( ModContentPack content ) : base( content )
        {
            Settings = GetSettings<Settings>();
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
}