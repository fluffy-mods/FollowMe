// Resources.cs
// Copyright Karel Kroeze, 2019-2019

using UnityEngine;
using Verse;

namespace FollowMe {
    [StaticConstructorOnStartup]
    public static class Resources {
        public static Texture2D cameraIcon;

        static Resources() {
            cameraIcon = ContentFinder<Texture2D>.Get("UI/Icons/Camera");
        }
    }
}
