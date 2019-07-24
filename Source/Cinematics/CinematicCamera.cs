// CinematicCamera.cs
// Copyright Karel Kroeze, -2019

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FollowMe
{
    public class CinematicCamera : Def
    {
        private Thing                  _currentSubject;
        private Dictionary<Thing, int> _pastSubjects = new Dictionary<Thing, int>();
        private int                    _ticksOfFame;
        private InterestWorker         _worker;


        public int      fameCooldown = 30;
        public Type     interestWorkerType;
        public IntRange secondsOfFame = new IntRange( 5, 15 );

        public InterestWorker Worker
        {
            get
            {
                if ( _worker == null )
                    _worker = (InterestWorker) Activator.CreateInstance( interestWorkerType );
                return _worker;
            }
        }

        public virtual Thing Subject
        {
            get => _currentSubject;
            protected set
            {
                _currentSubject = value;
                _pastSubjects[value] = Find.TickManager.TicksAbs;
                FollowMe.TryStartFollow( value );
            }
        }

        public virtual void Start()
        {
            Messages.Message( $"Fluffy.FollowMe.CameraStart".Translate( LabelCap ), MessageTypeDefOf.PositiveEvent, false );
            FollowNewSubject();
        }

        public virtual void Stop( bool notify = true )
        {
            Messages.Message( $"Fluffy.FollowMe.CameraStop".Translate( LabelCap ), MessageTypeDefOf.NeutralEvent, false );
            _currentSubject = null;
            _ticksOfFame    = -1;
            _pastSubjects.Clear();
        }

        public virtual void Tick()
        {
            if ( _ticksOfFame-- <= 0 )
                FollowNewSubject();
        }

        public virtual void FollowNewSubject()
        {
            var targets = Find.CurrentMap.listerThings
                              .ThingsInGroup( Worker.PotentiallyInteresting )
                              .Where( t => t != Subject && Worker.Interesting( t ) );
            if ( !targets.Any() )
                CinematicCameraManager.Stop( "Fluffy.FollowMe.Cinematics.NoValidTargets".Translate() );
            var target = targets.MaxBy( t => Worker.InterestFor( t ) * CooldownFactor( t ) );

            Subject = target;
            _ticksOfFame = Rand.Range( GenTicks.TicksPerRealSecond * secondsOfFame.min,
                                       GenTicks.TicksPerRealSecond * secondsOfFame.max );
        }

        public virtual float CooldownFactor( Thing thing )
        {
            var lastSpotlight = 0;
            if ( _pastSubjects.ContainsKey( thing ) )
                lastSpotlight = _pastSubjects[thing];

            return Mathf.Min( Find.TickManager.TicksAbs - lastSpotlight, fameCooldown * GenTicks.TicksPerRealSecond ) /
                   fameCooldown * GenTicks.TicksPerRealSecond;
        }
    }
}