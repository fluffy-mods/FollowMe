using RimWorld;
using System;
using System.Linq;
using Verse;
using UnityEngine;
using System.Reflection;

namespace FollowMe
{
    public class FollowMe : MapComponent
    {
        #region Fields

        private static bool _currentlyFollowing;
        private static Thing _followedThing;
        private bool _enabled = true;
        private static bool _cameraHasJumpedAtLeastOnce = false;

        private KeyBindingDef[] _followBreakingKeyBindingDefs = {
            KeyBindingDefOf.MapDollyDown,
            KeyBindingDefOf.MapDollyUp,
            KeyBindingDefOf.MapDollyRight,
            KeyBindingDefOf.MapDollyLeft
        };

        private KeyBindingDef _followKey = KeyBindingDef.Named( "FollowSelected" );

        #endregion Fields

        #region Properties

        public static string FollowedLabel
        {
            get
            {
                if ( _followedThing == null )
                {
                    return String.Empty;
                }
                Pawn pawn = _followedThing as Pawn;
                if ( pawn != null )
                {
                    return pawn.NameStringShort;
                }
                return _followedThing.LabelCap;
            }
        }

        #endregion Properties

        public override void MapComponentOnGUI()
        {
            if ( Event.current.type == EventType.mouseDown &&
                 Event.current.button == 1 )
            {
                // get mouseposition, invert y axis (because UI has origing in top left, Input in bottom left).
                var pos = Input.mousePosition;
                pos.y = Screen.height - pos.y;
                Thing thing = Find.ColonistBar.ColonistAt( pos );
                if ( thing != null )
                {
                    // start following
                    TryStartFollow( thing );

                    // use event so it doesn't bubble through
                    Event.current.Use();
                }
            }
        }

        // Called every frame when the mod is enabled.
        public override void MapComponentUpdate()
        {
            if ( !_enabled )
                return;

            try
            {
                // TODO: figure out how to shut it off when scrolling by mouse?
                CheckFollowBreakingKeys();
                
                // start/stop following thing on key press
                if ( _followKey.KeyDownEvent )
                    TryStartFollow( Find.Selector.SingleSelectedObject as Thing );

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

            Vector3 newCameraPosition;
            if (!_followedThing.Spawned && _followedThing.holder != null)
            {
                // thing is in some sort of container
                IThingContainerOwner holder = _followedThing.holder.owner;

                // if holder is a pawn's carrytracker we can use the smoother positions of the pawns's drawposition
                Pawn_CarryTracker tracker = holder as Pawn_CarryTracker;
                if (tracker != null)
                    newCameraPosition = tracker.pawn.DrawPos;

                // otherwise the holder int location will have to do
                else
                    newCameraPosition = holder.GetPosition().ToVector3Shifted();
            }

            // thing is spawned in world, just use the things drawPos
            else if (_followedThing.Spawned)
                newCameraPosition = _followedThing.DrawPos;

            // we've lost track of whatever it was we were following
            else
            {
                StopFollow();
                return;
            }

            // to avoid cancelling the following immediately after it starts, allow the camera to move to the followed thing once
            // before starting to compare positions
            if (_cameraHasJumpedAtLeastOnce)
            {
                // the actual location of the camera right now
                var currentCameraPosition = Find.CameraDriver.MapPosition;

                // the location the camera has been requested to be at
                var requestedCameraPosition = GetRequestedCameraPosition().ToIntVec3();

                // these normally stay in sync while following is active, since we were the last to request where the camera should go.
                // If they get out of sync, it's because the camera has been asked to jump to somewhere else, and we should stop
                // following our thing.
                if (Math.Abs(currentCameraPosition.x - requestedCameraPosition.x) > 1 || Math.Abs(currentCameraPosition.z - requestedCameraPosition.z) > 1 )
                {
                    StopFollow();
                    return;
                }
            }

            Find.CameraDriver.JumpTo(newCameraPosition);
            _cameraHasJumpedAtLeastOnce = true;
        }

        private static readonly FieldInfo _cameraDriverRootPosField = typeof(CameraDriver).GetField("rootPos", BindingFlags.Instance | BindingFlags.NonPublic);
        private static Vector3 GetRequestedCameraPosition()
        {
            var cameraDriver = Find.CameraDriver;
            return (Vector3)_cameraDriverRootPosField.GetValue(cameraDriver);
        }

        public static void TryStartFollow( Thing thing )
        { 
            if ( !_currentlyFollowing && thing == null )
            {
                if ( Find.Selector.NumSelected > 1 )
                    Messages.Message( "FollowMe.RejectMultiple".Translate(), MessageSound.RejectInput );

                else if ( Find.Selector.NumSelected == 0 )
                    Messages.Message( "FollowMe.RejectNoSelection".Translate(), MessageSound.RejectInput );

                else
                    Messages.Message( "FollowMe.RejectNotAThing".Translate(), MessageSound.RejectInput );
            }

            // cancel current follow (toggle or thing == null)
            else if ( _currentlyFollowing && thing == null || thing == _followedThing )
                StopFollow();

            // follow new thing
            else if ( thing != null )
                StartFollow( thing );
        }

        private static void StartFollow( Thing thing )
        {
            _followedThing = thing;
            _currentlyFollowing = true;
            Messages.Message( "FollowMe.Follow".Translate( FollowedLabel ), MessageSound.Negative );
        }

        public static void StopFollow()
        {
            Messages.Message( "FollowMe.Cancel".Translate( FollowedLabel ), MessageSound.Negative );
            _followedThing = null;
            _currentlyFollowing = false;
            _cameraHasJumpedAtLeastOnce = false;
        }

        private void CheckFollowBreakingKeys()
        {
            if ( !_currentlyFollowing )
                return;
            if ( _followBreakingKeyBindingDefs.Any( key => key.IsDown ) )
                StopFollow();
        }
    }
}