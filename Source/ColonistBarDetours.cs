using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace FollowMe
{
    public class ColonistBarDetours
    {
        private float _clickedAt = Time.time;
        private Pawn _clickedColonist;

        private void HandleColonistClicks( Rect rect, Pawn colonist )
        {
            if ( !Mouse.IsOver( rect ) || Event.current.type != EventType.MouseDown )
                return;
            if ( _clickedColonist == colonist && (double)Time.time - (double)_clickedAt < 0.5 )
            {
                Event.current.Use();
                JumpToTargetUtility.TryJump( (Thing)colonist );
                _clickedColonist = (Pawn)null;
            }
            else
            {
                _clickedColonist = colonist;
                _clickedAt = Time.time;
            }
            //if ( Mouse.IsOver( rect ) && Event.current.type == EventType.MouseDown )
            //{
            //    // double click jump (Vanilla)
            //    if ( Event.current.button == 0 )
            //    {
            //        if ( _clickedColonist == colonist && Time.time - _clickedAt < 0.5 )
            //        {
            //            Event.current.Use();
            //            JumpToTargetUtility.TryJump( colonist );
            //            _clickedColonist = null;
            //        }
            //        else
            //        {
            //            _clickedColonist = colonist;
            //            _clickedAt = Time.time;
            //        }
            //    }

            //    // right-click follow
            //    if ( Event.current.button == 1 )
            //    {
            //        Find.Selector.ClearSelection();
            //        Find.Selector.Select( colonist );
            //        FollowMe.TryStartFollow( colonist );
            //    }
            //}
        }
    }
}
