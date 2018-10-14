// Karel Kroeze
// FollowMe.cs
// 2016-12-27

using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FollowMe
{
    public class FollowMe : GameComponent
    {
        public FollowMe()
        {
            // scribe
        }

        public FollowMe( Game game )
        {
            // game init
        }

        #region Properties

        public static string FollowedLabel
        {
            get
            {
                if ( _followedThing == null )
                    return string.Empty;

                var pawn = _followedThing as Pawn;
                if ( pawn?.Name != null )
                    return pawn.Name.ToStringShort;

                return _followedThing.LabelCap;
            }
        }

        #endregion Properties

        #region Fields

        private static readonly FieldInfo _cameraDriverRootPosField = typeof( CameraDriver ).GetField( "rootPos",
                                                                                                       BindingFlags
                                                                                                           .Instance |
                                                                                                       BindingFlags
                                                                                                           .NonPublic );
        private static readonly FieldInfo _cameraDriverDesiredDollyField =
            typeof( CameraDriver ).GetField( "desiredDolly", BindingFlags.Instance | BindingFlags.NonPublic );

        private static bool _cameraHasJumpedAtLeastOnce;
        private static bool _currentlyFollowing;
        private static bool _enabled = true;
        private static Thing _followedThing;

        private KeyBindingDef[] _followBreakingKeyBindingDefs =
        {
            KeyBindingDefOf.MapDolly_Down,
            KeyBindingDefOf.MapDolly_Up,
            KeyBindingDefOf.MapDolly_Right,
            KeyBindingDefOf.MapDolly_Left
        };

        private KeyBindingDef _followKey = KeyBindingDef.Named( "FollowSelected" );

        #endregion Fields

        #region Methods

        public static void TryStartFollow( Thing thing )
        {
            _enabled = true;
            if ( !_currentlyFollowing && thing == null )
                if ( Find.Selector.NumSelected > 1 )
                    Mod.DoMessage( "FollowMe.RejectMultiple".Translate(), MessageTypeDefOf.RejectInput );
                else if ( Find.Selector.NumSelected == 0 )
                    Mod.DoMessage( "FollowMe.RejectNoSelection".Translate(), MessageTypeDefOf.RejectInput );
                else
                    Mod.DoMessage( "FollowMe.RejectNotAThing".Translate(), MessageTypeDefOf.RejectInput );

            // cancel current follow (toggle or thing == null)
            else if ( _currentlyFollowing && thing == null || thing == _followedThing )
                StopFollow( "toggled" );

            // follow new thing
            else if ( thing != null )
                StartFollow( thing );
        }

        private static void StartFollow( Thing thing )
        {
            if ( thing == null )
                throw new ArgumentNullException( nameof( thing ) );

            _followedThing = thing;
            _currentlyFollowing = true;

            Mod.DoMessage( "FollowMe.Follow".Translate( FollowedLabel ), MessageTypeDefOf.PositiveEvent );
        }

        public static void StopFollow( string reason )
        {
#if DEBUG
            Log.Message( $"FollowMe :: Stopped following {FollowedLabel} :: {reason}" );
#endif

            Mod.DoMessage( "FollowMe.Cancel".Translate( FollowedLabel ), MessageTypeDefOf.SituationResolved );
            _followedThing = null;
            _currentlyFollowing = false;
            _cameraHasJumpedAtLeastOnce = false;
        }

        public override void GameComponentOnGUI()
        {
            if ( Current.ProgramState != ProgramState.Playing )
                return; // gamecomp is already active in the 'setup' stage, but follow me shouldnt be.

            if ( Event.current.type == EventType.mouseUp &&
                 Event.current.button == 2 )
            {
                // Get entry at mouse position - UI.MousePositionOnUIInverted handles;
                //  - inverting y axis (UI starts top right, screen starts bottom right)
                //  - UI scale
                Thing thing = Find.ColonistBar.ColonistOrCorpseAt( UI.MousePositionOnUIInverted );
                if ( thing != null )
                {
                    // start following
                    TryStartFollow( thing );

                    // use event so it doesn't bubble through
                    Event.current.Use();
                }
            }
        }

        public override void GameComponentUpdate()
        {
            // start/stop following thing on key press
            if (_followKey.KeyDownEvent)
                TryStartFollow(Find.Selector.SingleSelectedObject as Thing);

            if ( !_enabled )
                return;

            try
            {
                if ( _currentlyFollowing )
                {
                    CheckKeyScroll();
                    CheckCameraJump();
                    CheckDolly();

                    if (Settings.edgeDetection)
                        CheckScreenEdgeScroll();
                }


                // move camera
                Follow();
            }

            // catch exception to avoid error spam
            catch ( Exception e )
            {
                _enabled = false;
                Log.Error( e.ToString() );
            }
        }

        private static void Follow()
        {
            if ( !_currentlyFollowing || _followedThing == null )
                return;

            TryJumpSmooth( _followedThing );
        }

        public static void TryJumpSmooth( GlobalTargetInfo target )
        {
            target = CameraJumper.GetAdjustedTarget( target );
            if ( !target.IsValid )
            {
                StopFollow( "invalid target" );
                return;
            }

            // we have to use our own logic for following spawned things, as CameraJumper
            // uses integer positions - which would be jerky.
            if ( target.HasThing )
                TryJumpSmoothInternal( target.Thing );
            // However, if we don't have a thing to follow, integer positions will do just fine.
            else
                CameraJumper.TryJump( target );

            _cameraHasJumpedAtLeastOnce = true;
        }

        private static void TryJumpSmoothInternal( Thing thing )
        {
            // copypasta from Verse.CameraJumper.TryJumpInternal( Thing ),
            // but with drawPos instead of PositionHeld.
            if ( Current.ProgramState != ProgramState.Playing )
                return;

            Map mapHeld = thing.MapHeld;
            if ( mapHeld != null && thing.PositionHeld.IsValid && thing.PositionHeld.InBounds( mapHeld ) )
            {
                bool flag = CameraJumper.TryHideWorld();
                if ( Find.CurrentMap != mapHeld )
                {
                    Current.Game.CurrentMap = mapHeld;
                    if ( !flag )
                        SoundDefOf.MapSelected.PlayOneShotOnCamera( null );
                }
                Find.CameraDriver.JumpToCurrentMapLoc( thing.DrawPos ); // <---
            }
            else
            {
                StopFollow( "invalid thing position" );
            }
        }

        private static Vector3 CameraRootPosition
        {
            get
            {
                if ( _cameraDriverRootPosField == null )
                    throw new NullReferenceException( "CameraDriver.rootPos field info NULL" );

                return (Vector3) _cameraDriverRootPosField.GetValue( Find.CameraDriver );
            }
        }

        private static Vector2 CameraDesiredDolly
        {
            get
            {
                if ( _cameraDriverDesiredDollyField == null )
                    throw new NullReferenceException( "CameraDriver.desiredDolly field info NULL" );

                return (Vector2) _cameraDriverDesiredDollyField.GetValue( Find.CameraDriver );
            }
        }

        private static void CheckDolly()
        {
            if ( CameraDesiredDolly != Vector2.zero )
                StopFollow( "dolly" );
        }

        private void CheckKeyScroll()
        {
            if ( _followBreakingKeyBindingDefs.Any( key => key.IsDown ) )
                StopFollow( "moved map (key)" );
        }

        private void CheckCameraJump()
        {
            // to avoid cancelling the following immediately after it starts, allow the camera to move to the followed thing once
            // before starting to compare positions
            if ( _cameraHasJumpedAtLeastOnce )
            {
                // the actual location of the camera right now
                IntVec3 currentCameraPosition = Find.CameraDriver.MapPosition;

                // the location the camera has been requested to be at
                IntVec3 requestedCameraPosition = CameraRootPosition.ToIntVec3();

                // these normally stay in sync while following is active, since we were the last to request where the camera should go.
                // If they get out of sync, it's because the camera has been asked to jump to somewhere else, and we should stop
                // following our thing.
                if ( ( currentCameraPosition - requestedCameraPosition ).LengthHorizontal > 1 )
                    StopFollow( "map moved (camera jump)" );
            }
        }

        private static bool MouseOverUI => Find.WindowStack.GetWindowAt( UI.MousePositionOnUIInverted ) != null;

        private void CheckScreenEdgeScroll()
        {
            if ( !Application.isFocused || !Prefs.EdgeScreenScroll || MouseOverUI )
                return;

            Vector3 mousePosition = Input.mousePosition;
            var screenCorners = new[]
                                {
                                    new Rect( 0f, 0f, 200f, 200f ),
                                    new Rect( Screen.width - 250, 0f, 255f, 255f ),
                                    new Rect( 0f, Screen.height - 250, 225f, 255f ),
                                    new Rect( Screen.width - 250, Screen.height - 250, 255f, 255f )
                                };
            if ( screenCorners.Any( e => e.Contains( mousePosition ) ) )
                return;

            if ( mousePosition.x < 20f 
                || mousePosition.x > Screen.width - 20
                || mousePosition.y > Screen.height - 20f 
                || mousePosition.y < ( Screen.fullScreen ? 6f : 20f ) )
                StopFollow( "moved map (mouse edge)" );
        }

        #endregion Methods
    }
}
