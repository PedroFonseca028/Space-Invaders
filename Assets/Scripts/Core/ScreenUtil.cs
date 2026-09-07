using UnityEngine;

namespace SpaceInvaders
{
    // Central place to query the visible play area in world units.
    // Recomputed on demand (not cached) so the layout stays correct
    // even if the Game view is resized or the aspect ratio changes.
    public static class ScreenUtil
    {
        public static Camera Cam;

        public static float HalfHeight => Cam != null ? Cam.orthographicSize : 6.5f;
        public static float HalfWidth => Cam != null ? Cam.orthographicSize * Cam.aspect : 10f;
    }
}
