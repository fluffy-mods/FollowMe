// Karel Kroeze
// FollowMe.cs
// 2016-12-27

using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace FollowMe
{
    // TODO: Refactor into GameObject so we can be independent of maps.
    public class FollowMe : MapComponent
    {
        #region Fields

        private static readonly FieldInfo _cameraDriverRootPosField = typeof(CameraDriver).GetField("rootPos",
                                                                                                       BindingFlags
                                                                                                           .Instance |
                                                                                                       BindingFlags
                                                                                                           .NonPublic);

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

        #region Constructors

        public FollowMe(Map map) : base(map)
        {
        }

        #endregion Constructors

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

        public static void StopFollow()
        {
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
                StopFollow();

            // follow new thing
            else if (thing != null)
                StartFollow(thing);
        }

        // OnGUI is only called if the current map is active.
        public override void MapComponentOnGUI()
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

        // Called every frame when the mod is enabled, regardless of which map we're looking at
        public override void MapComponentUpdate()
        {
            if (!_enabled)
                return;

            // cop out if this is not the visible map.
            if (Find.VisibleMap != map)
                return;

            try
            {
                CheckFollowBreakingKeys();
                CheckFollowCameraDolly();

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

            Vector3 newCameraPosition;
            Map newMap;
            if (!_followedThing.Spawned && _followedThing.holdingContainer != null)
            {
                // thing is in some sort of container
                IThingContainerOwner holder = _followedThing.holdingContainer.owner;

                // if holder is a pawn's carrytracker we can use the smoother positions of the pawns's drawposition
                var tracker = holder as Pawn_CarryTracker;
                if (tracker != null)
                {
                    newCameraPosition = tracker.pawn.DrawPos;
                    newMap = tracker.pawn.MapHeld;
                }

                // otherwise the holder int location will have to do
                else
                {
                    newCameraPosition = holder.GetPosition().ToVector3Shifted();
                    newMap = holder.GetMap();
                }
            }

            // thing is spawned in world, just use the things drawPos
            else if (_followedThing.Spawned)
            {
                newCameraPosition = _followedThing.DrawPos;
                newMap = _followedThing.MapHeld;
            }

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
                IntVec3 currentCameraPosition = Find.CameraDriver.MapPosition;

                // the location the camera has been requested to be at
                IntVec3 requestedCameraPosition = GetRequestedCameraPosition().ToIntVec3();

                // these normally stay in sync while following is active, since we were the last to request where the camera should go.
                // If they get out of sync, it's because the camera has been asked to jump to somewhere else, and we should stop
                // following our thing.
                if (Math.Abs(currentCameraPosition.x - requestedCameraPosition.x) > 1 ||
                     Math.Abs(currentCameraPosition.z - requestedCameraPosition.z) > 1)
                {
                    StopFollow();
                    return;
                }
            }

            Find.CameraDriver.JumpTo(newCameraPosition);
            _cameraHasJumpedAtLeastOnce = true;
        }

        private static Vector3 GetRequestedCameraPosition()
        {
            if (_cameraDriverRootPosField == null)
                throw new NullReferenceException("CameraDriver.rootPos field info NULL");

            return (Vector3)_cameraDriverRootPosField.GetValue(Find.CameraDriver);
        }

        private static void StartFollow(Thing thing)
        {
            if (thing == null)
                throw new ArgumentNullException(nameof(thing));

            _followedThing = thing;
            _currentlyFollowing = true;

            Messages.Message("FollowMe.Follow".Translate(FollowedLabel), MessageSound.Benefit);
        }

        private void CheckFollowBreakingKeys()
        {
            if (!_currentlyFollowing)
                return;

            if (_followBreakingKeyBindingDefs.Any(key => key.IsDown))
                StopFollow();
        }

        private void CheckFollowCameraDolly()
        {
            if (!_currentlyFollowing)
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
                StopFollow();
            }
        }

        #endregion Methods
    }
}
