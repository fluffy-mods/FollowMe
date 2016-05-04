using CommunityCoreLibrary;
using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace FollowMe
{
    public class FollowMe : MonoBehaviour
    {
        #region Fields

        public static bool CurrentlyFollowing;
        public static Thing followedThing;
        public static string GameObjectName = "FollowMeController";

        private KeyBindingDef[] followBreakingKeyBindingDefs = {
            KeyBindingDefOf.MapDollyDown,
            KeyBindingDefOf.MapDollyUp,
            KeyBindingDefOf.MapDollyRight,
            KeyBindingDefOf.MapDollyLeft
        };

        private KeyBindingDef FollowKey = KeyBindingDef.Named( "FollowSelected" );

        #endregion Fields

        #region Properties

        public static string followedLabel
        {
            get
            {
                if ( followedThing == null )
                {
                    return String.Empty;
                }
                Pawn pawn = followedThing as Pawn;
                if ( pawn != null )
                {
                    return pawn.NameStringShort;
                }
                return followedThing.LabelCap;
            }
        }

        #endregion Properties

        #region Methods

        // Called every frame when the mod is enabled.
        public virtual void Update()
        {
            try
            {
                // shut it off if we're manually scrolling (keys)
                if ( CurrentlyFollowing )
                {
                    if ( followBreakingKeyBindingDefs.Any( key => key.IsDown ) )
                    {
                        Messages.Message( "FollowMe.Cancel".Translate( followedLabel ), MessageSound.Negative );
                        followedThing = null;
                        CurrentlyFollowing = false;
                    }
                }

                // TODO: figure out how to shut it off when scrolling by mouse?

                // get selection
                Thing newFollowedThing = Find.Selector.SingleSelectedObject as Thing;

                // start/stop following thing on key press
                if ( FollowKey.KeyDownEvent )
                {
                    Log.Message( "FollowMe :: Follow key pressed" );
                    // nothing to cancel or start following
                    if ( !CurrentlyFollowing && newFollowedThing == null )
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
                    else if ( CurrentlyFollowing && newFollowedThing == null || newFollowedThing == followedThing )
                    {
                        Messages.Message( "FollowMe.Cancel".Translate( followedLabel ), MessageSound.Negative );
                        followedThing = null;
                        CurrentlyFollowing = false;
                    }

                    // follow new thing
                    else if ( newFollowedThing != null )
                    {
                        followedThing = newFollowedThing;
                        CurrentlyFollowing = true;
                        Messages.Message( "FollowMe.Follow".Translate( followedLabel ), MessageSound.Negative );
                    }
                }

                // try follow whatever thing is selected
                if ( CurrentlyFollowing && followedThing != null )
                {
                    if ( !followedThing.Spawned && followedThing.holder != null )
                    {
                        // thing is in some sort of container
                        IThingContainerOwner holder = followedThing.holder.owner;

                        // if holder is a pawn's carrytracker we can use the smoother positions of the pawns's drawposition
                        Pawn_CarryTracker tracker = holder as Pawn_CarryTracker;
                        if ( tracker != null )
                        {
                            Find.CameraMap.JumpTo( tracker.pawn.DrawPos );
                        }

                        // otherwise the holder int location will have to do
                        else
                        {
                            Find.CameraMap.JumpTo( holder.GetPosition() );
                        }
                    }
                    else if ( followedThing.Spawned )
                    {
                        // thing is spawned in world, just use the things drawPos
                        Find.CameraMap.JumpTo( followedThing.DrawPos );
                    }
                    else
                    {
                        // we've lost track of whatever it was we were following
                        Log.Message( "FollowMe.Cancel".Translate( followedLabel ) );
                        CurrentlyFollowing = false;
                        followedThing = null;
                    }
                }
            }

            // catch exception to avoid error spam
            catch ( Exception e )
            {
                enabled = false;
                Log.Error( e.ToString() );
            }
        }

        #endregion Methods
    }

    public class Injector : SpecialInjector
    {
        #region Methods

        public override bool Inject()
        {
            LongEventHandler.ExecuteWhenFinished( delegate
            {
                // create a game object.
                GameObject gameObject = new GameObject( FollowMe.GameObjectName );
                MonoBehaviour.DontDestroyOnLoad( gameObject );
                gameObject.AddComponent<FollowMe>();
                Log.Message( "FollowMe :: GameObject initialized." );
            } );

            return true;
        }

        #endregion Methods
    }
}