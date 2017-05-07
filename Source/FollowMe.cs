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
        #region Fields
        // todo; can we replace all our custom follow breaking checks with a simple check?
        // CameraDriver.desiredDolly != Vector2.Zero?

        private static readonly FieldInfo _cameraDriverRootPosField = typeof(CameraDriver).GetField("rootPos", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _cameraDriverDesiredDollyField = typeof( CameraDriver ).GetField( "desiredDolly", BindingFlags.Instance | BindingFlags.NonPublic );

        private static bool _cameraHasJumpedAtLeastOnce;
        private static bool _currentlyFollowing;
        private static bool _enabled = true;
        private static Thing _followedThing;

        private KeyBindingDef[] _followBreakingKeyBindingDefs =
        {
            KeyBindingDefOf.MapDollyDown,
            KeyBindingDefOf.MapDollyUp,
            KeyBindingDefOf.MapDollyRight,
            KeyBindingDefOf.MapDollyLeft
        };

        private KeyBindingDef _followKey = KeyBindingDef.Named("FollowSelected");

        #endregion Fields
        
        #region Properties

        public static string FollowedLabel
        {
            get
            {
                if (_followedThing == null)
                {
                    return String.Empty;
                }

                var pawn = _followedThing as Pawn;
                if (pawn != null)
                {
                    return pawn.NameStringShort;
                }

                return _followedThing.LabelCap;
            }
        }

        #endregion Properties

        #region Methods

        public static void StopFollow( string reason )
        {
#if DEBUG
            Log.Message( $"FollowMe :: Stopped following {FollowedLabel} :: {reason}" );
#endif

            Messages.Message("FollowMe.Cancel".Translate(FollowedLabel), MessageSound.Negative);
            _followedThing = null;
            _currentlyFollowing = false;
            _cameraHasJumpedAtLeastOnce = false;
        }

        public static void TryStartFollow(Thing thing)
        {
            if (!_currentlyFollowing && thing == null)
            {
                if (Find.Selector.NumSelected > 1)
                    Messages.Message("FollowMe.RejectMultiple".Translate(), MessageSound.RejectInput);
                else if (Find.Selector.NumSelected == 0)
                    Messages.Message("FollowMe.RejectNoSelection".Translate(), MessageSound.RejectInput);
                else
                    Messages.Message("FollowMe.RejectNotAThing".Translate(), MessageSound.RejectInput);
            }

            // cancel current follow (toggle or thing == null)
            else if (_currentlyFollowing && thing == null || thing == _followedThing)
                StopFollow( "toggled" );

            // follow new thing
            else if (thing != null)
                StartFollow(thing);
        }

        public override void GameComponentOnGUI()
        {
            if (Event.current.type == EventType.mouseUp &&
                 Event.current.button == 1)
            {
                // get mouseposition, invert y axis (because UI has origin in top left, Input in bottom left).
                Vector3 pos = Input.mousePosition;
                pos.y = Screen.height - pos.y;
                Thing thing = Find.ColonistBar.ColonistOrCorpseAt(pos);
                if (thing != null)
                {
                    // start following
                    TryStartFollow(thing);

                    // use event so it doesn't bubble through
                    Event.current.Use();
                }
            }
        }

        public override void GameComponentUpdate()
        {
            if (!_enabled)
                return;

            try
            {
                if ( _currentlyFollowing )
                {
                    CheckKeyScroll();
                    CheckScreenEdgeScroll();
                    CheckCameraJump();
                    CheckDolly();
                }

                // start/stop following thing on key press
                if (_followKey.KeyDownEvent)
                    TryStartFollow(Find.Selector.SingleSelectedObject as Thing);

                // move camera
                Follow();
            }

            // catch exception to avoid error spam
            catch (Exception e)
            {
                _enabled = false;
                Log.Error(e.ToString());
            }
        }

        private static void Follow()
        {
            if (!_currentlyFollowing || _followedThing == null)
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
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }
            Map mapHeld = thing.MapHeld;
            if ( mapHeld != null && thing.PositionHeld.IsValid && thing.PositionHeld.InBounds( mapHeld ) )
            {
                bool flag = CameraJumper.TryHideWorld();
                if ( Current.Game.VisibleMap != mapHeld )
                {
                    Current.Game.VisibleMap = mapHeld;
                    if ( !flag )
                    {
                        SoundDefOf.MapSelected.PlayOneShotOnCamera( null );
                    }
                }
                Find.CameraDriver.JumpToVisibleMapLoc( thing.DrawPos ); // <---
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

                if (_cameraDriverRootPosField == null)
                    throw new NullReferenceException("CameraDriver.rootPos field info NULL");

                return (Vector3)_cameraDriverRootPosField.GetValue(Find.CameraDriver);
            }
        }

        private static Vector2 CameraDesiredDolly
        {
            get
            {
                if (_cameraDriverDesiredDollyField == null )
                    throw new NullReferenceException( "CameraDriver.desiredDolly field info NULL" );

                return (Vector2) _cameraDriverDesiredDollyField.GetValue( Find.CameraDriver );
            }
        }

        private static void CheckDolly()
        {
            if ( CameraDesiredDolly != Vector2.zero )
                StopFollow( "dolly" );
        }

        private static void StartFollow(Thing thing)
        {
            if (thing == null)
                throw new ArgumentNullException(nameof(thing));

            _followedThing = thing;
            _currentlyFollowing = true;

            Messages.Message("FollowMe.Follow".Translate(FollowedLabel), MessageSound.Benefit);
        }

        private void CheckKeyScroll()
        {
            if (_followBreakingKeyBindingDefs.Any(key => key.IsDown))
                StopFollow( "moved map (key)" );
        }

        private void CheckCameraJump()
        {
            // to avoid cancelling the following immediately after it starts, allow the camera to move to the followed thing once
            // before starting to compare positions
            if (_cameraHasJumpedAtLeastOnce)
            {
                // the actual location of the camera right now
                IntVec3 currentCameraPosition = Find.CameraDriver.MapPosition;

                // the location the camera has been requested to be at
                IntVec3 requestedCameraPosition = CameraRootPosition.ToIntVec3();

                // these normally stay in sync while following is active, since we were the last to request where the camera should go.
                // If they get out of sync, it's because the camera has been asked to jump to somewhere else, and we should stop
                // following our thing.
                if ((currentCameraPosition - requestedCameraPosition).LengthHorizontal > 1 ) 
                    StopFollow("map moved (camera jump)");
            }
        }

        private static bool MouseOverUI => ( Find.WindowStack.GetWindowAt( UI.MousePositionOnUIInverted ) != null );

        private void CheckScreenEdgeScroll()
        {
            if ( !Prefs.EdgeScreenScroll || MouseOverUI )
                return;

            Vector3 mousePosition = Input.mousePosition;
            var screenCorners = new[]
                                {
                                    new Rect( 0f, 0f, 200f, 200f ),
                                    new Rect( Screen.width - 250, 0f, 255f, 255f ),
                                    new Rect( 0f, Screen.height - 250, 225f, 255f ),
                                    new Rect( Screen.width - 250, Screen.height - 250, 255f, 255f )
                                };
            if (screenCorners.Any(e => e.Contains(mousePosition)))
                return;

            if (mousePosition.x < 20f || mousePosition.x > Screen.width - 20
                 || mousePosition.y > Screen.height - 20f || mousePosition.y < (Screen.fullScreen ? 6f : 20f))
            {
                StopFollow( "moved map (dolly)");
            }
        }

        #endregion Methods
    }
}
