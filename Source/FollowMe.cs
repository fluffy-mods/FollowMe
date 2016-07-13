using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using Verse;
using CommunityCoreLibrary;

namespace FollowMe
{
    public class FollowMe : MapComponent
    {
        public FollowMe()
        {
            // detour colonist bar click handler
            MethodInfo SourceMethod = typeof( ColonistBar ).GetMethod( "HandleColonistClicks",
                                                                       BindingFlags.NonPublic | BindingFlags.Instance );
            MethodInfo DestinationMethod = typeof( ColonistBarDetours ).GetMethod( "HandleColonistClicks",
                                                                       BindingFlags.NonPublic | BindingFlags.Instance );

            if ( Detours.TryDetourFromTo( SourceMethod, DestinationMethod ) )
                Log.Message( "FollowMe :: Succesfully injected right-click follow into Colonist Bar." );
            else
                Log.Error( "FollowMe :: Failed to inject right-click follow into Colonist Bar." );

        }

        #region Fields

        private static bool _currentlyFollowing;
        private static Thing _followedThing;
        private bool _enabled = true;

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

            if ( !_followedThing.Spawned && _followedThing.holder != null )
            {
                // thing is in some sort of container
                IThingContainerOwner holder = _followedThing.holder.owner;

                // if holder is a pawn's carrytracker we can use the smoother positions of the pawns's drawposition
                Pawn_CarryTracker tracker = holder as Pawn_CarryTracker;
                if ( tracker != null )
                    Find.CameraDriver.JumpTo( tracker.pawn.DrawPos );

                // otherwise the holder int location will have to do
                else
                    Find.CameraDriver.JumpTo( holder.GetPosition() );
            }

            // thing is spawned in world, just use the things drawPos
            else if ( _followedThing.Spawned )
                Find.CameraDriver.JumpTo( _followedThing.DrawPos );

            // we've lost track of whatever it was we were following
            else
                StopFollow();
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