using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace FollowMe
{
    public class FollowMe : MapComponent
    {
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
                // shut it off if we're manually scrolling (keys)
                if ( _currentlyFollowing )
                {
                    if ( _followBreakingKeyBindingDefs.Any( key => key.IsDown ) )
                    {
                        Messages.Message( "FollowMe.Cancel".Translate( FollowedLabel ), MessageSound.Negative );
                        _followedThing = null;
                        _currentlyFollowing = false;
                    }
                }

                // TODO: figure out how to shut it off when scrolling by mouse?

                // get selection
                Thing newFollowedThing = Find.Selector.SingleSelectedObject as Thing;

                // start/stop following thing on key press
                if ( _followKey.KeyDownEvent )
                {

#if DEBUG
                    Log.Message( "FollowMe :: Follow key pressed" );
#endif

                    // nothing to cancel or start following
                    if ( !_currentlyFollowing && newFollowedThing == null )
                    {
                        if ( Find.Selector.NumSelected > 1 )
                        {
                            Messages.Message( "FollowMe.RejectMultiple".Translate(), MessageSound.RejectInput );
                        }
                        else if ( Find.Selector.NumSelected == 0 )
                        {
                            Messages.Message( "FollowMe.RejectNoSelection".Translate(), MessageSound.RejectInput );
                        }
                        else
                        {
                            Messages.Message( "FollowMe.RejectNotAThing".Translate(), MessageSound.RejectInput );
                        }
                    }

                    // cancel current follow
                    else if ( _currentlyFollowing && newFollowedThing == null || newFollowedThing == _followedThing )
                    {
                        Messages.Message( "FollowMe.Cancel".Translate( FollowedLabel ), MessageSound.Negative );
                        _followedThing = null;
                        _currentlyFollowing = false;
                    }

                    // follow new thing
                    else if ( newFollowedThing != null )
                    {
                        _followedThing = newFollowedThing;
                        _currentlyFollowing = true;
                        Messages.Message( "FollowMe.Follow".Translate( FollowedLabel ), MessageSound.Negative );
                    }
                }

                // try follow whatever thing is selected
                if ( _currentlyFollowing && _followedThing != null )
                {
                    if ( !_followedThing.Spawned && _followedThing.holder != null )
                    {
                        // thing is in some sort of container
                        IThingContainerOwner holder = _followedThing.holder.owner;

                        // if holder is a pawn's carrytracker we can use the smoother positions of the pawns's drawposition
                        Pawn_CarryTracker tracker = holder as Pawn_CarryTracker;
                        if ( tracker != null )
                        {
                            Find.CameraDriver.JumpTo( tracker.pawn.DrawPos );
                        }

                        // otherwise the holder int location will have to do
                        else
                        {
                            Find.CameraDriver.JumpTo( holder.GetPosition() );
                        }
                    }
                    else if ( _followedThing.Spawned )
                    {
                        // thing is spawned in world, just use the things drawPos
                        Find.CameraDriver.JumpTo( _followedThing.DrawPos );
                    }
                    else
                    {
                        // we've lost track of whatever it was we were following
                        Log.Message( "FollowMe.Cancel".Translate( FollowedLabel ) );
                        _currentlyFollowing = false;
                        _followedThing = null;
                    }
                }
            }

            // catch exception to avoid error spam
            catch ( Exception e )
            {
                _enabled = false;
                Log.Error( e.ToString() );
            }
        }
    }
}