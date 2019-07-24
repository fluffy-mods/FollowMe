// CinematicCameraManager.cs
// Copyright Karel Kroeze

using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using RimWorld;
using Verse;

namespace FollowMe
{
    public class CinematicCameraManager : GameComponent
    {
        public static CinematicCamera currentCamera;
        public static KeyBindingDef toggleCamera;
        private static bool _patched;

        public static List<CinematicCamera> Cameras => DefDatabase<CinematicCamera>.AllDefsListForReading;

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            if ( !_patched )
            {
                var harmonyInstance = HarmonyInstance.Create( "Fluffy.FollowMe" );
                harmonyInstance.PatchAll( Assembly.GetExecutingAssembly() );
            }
        }

        public CinematicCameraManager( Game game )
        {
            toggleCamera = KeyBindingDef.Named( "CinematicCamera" );
        }

        public static void Stop( string reason )
        {
            FollowMe.StopFollow( reason );
            currentCamera.Stop();
            currentCamera = null;
        }

        public static void Start( CinematicCamera camera )
        {
            currentCamera?.Stop( false );
            currentCamera = camera;
            currentCamera.Start();
        }

        public static void CycleCameras()
        {
            var curIndex = Cameras.IndexOf( currentCamera );

            // stop current
            currentCamera?.Stop( false );

            if ( curIndex == Cameras.Count - 1 )
            {
                currentCamera = null;
                return;
            }

            // start next
            currentCamera = Cameras[curIndex + 1];
            currentCamera?.Start();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            currentCamera?.Tick();
        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();

            if ( toggleCamera.KeyDownEvent )
                CycleCameras();
        }
    }
}