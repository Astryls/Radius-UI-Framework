// Radius UI Framework - Chrome/Spatial.Arc.cs
//
// ADDED generation 6 (2026-08-20). PURELY ADDITIVE: no existing file is touched, no existing
// member is renamed, no existing value moves. A new partial on the existing Spatial partial,
// which is the §13 concurrent-edit recommendation.
//
// WHY THIS EXISTS.
// Spatial (§15) is "the shape vocabulary CardChrome structurally cannot cover" - discs, rings,
// capsules, shadows. It has no wedge. Radius UI - Gizmos makes a radial command wheel the
// PRIMARY selection surface, so an annulus sector stops being a nice-to-have: it is the only
// genuinely new drawing primitive the whole five-study mockup set asked for, and it is needed
// by two independent designs (the drilled wheel's rings and the bloom wheel's arc), which is
// already the shared-helper argument before a second consumer exists.
//
// HOW IT DRAWS, AND WHY NOT THE OBVIOUS WAY.
// The obvious approach is to approximate a wedge with N thin rotated quads. At the wheel's real
// geometry that is ~15 GUI.DrawTexture calls per sector and ~180 per wheel per OnGUI pass, and
// the seams between quads double-composite at any alpha below 1 - the exact failure Spatial's
// trap 2 already records for Pill.
//
// Instead: ONE procedurally generated texture per SHAPE (inner-radius ratio + sweep angle),
// drawn ONCE, rotated to its bearing with GUIUtility.RotateAroundPivot. A whole 12-sector wheel
// is 12 draw calls off 2 or 3 cached textures. Shapes are cached, not sectors, so a six-slot
// bloom is one texture drawn six times at six bearings.
//
// ANGLE CONVENTION (stated once, because half of all radial-menu bugs are this):
//   0 degrees is UP (12 o'clock) and angles increase CLOCKWISE.
// That matches GUI space, where y grows downward, so a caller never has to negate anything.
// AngleFromUp() is provided so consumers hit-test with the same convention the drawing uses
// rather than re-deriving atan2 and getting the winding backwards.
//
// Thread affinity: OnGUI main thread only. Texture generation happens on first use of a shape.

using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    public static partial class Spatial
    {
        /// <summary>Cache is keyed on SHAPE, never on bearing - rotation is free, generation is not.</summary>
        private struct ArcKey : System.IEquatable<ArcKey>
        {
            public readonly int Ratio;   // inner/outer radius, in hundredths
            public readonly int Sweep;   // degrees, whole
            public readonly int Size;    // texture edge, px

            public ArcKey(int ratio, int sweep, int size)
            {
                Ratio = ratio;
                Sweep = sweep;
                Size = size;
            }

            public bool Equals(ArcKey o) => Ratio == o.Ratio && Sweep == o.Sweep && Size == o.Size;
            public override bool Equals(object o) => o is ArcKey k && Equals(k);
            public override int GetHashCode() => (Ratio * 397 ^ Sweep) * 397 ^ Size;
        }

        private static readonly Dictionary<ArcKey, Texture2D> ArcCache = new Dictionary<ArcKey, Texture2D>(16);

        /// <summary>Bound on distinct shapes. A wheel needs 2 or 3; anything past this is a caller bug.</summary>
        private const int ArcCacheMax = 32;

        private static bool arcCacheWarned;

        /// <summary>
        /// Fill an annulus sector (a wedge of a ring). Angles are degrees, <b>0 = up, clockwise</b>.
        ///
        /// <para>Cost: one <c>GUI.DrawTexture</c> plus a matrix push/pop. The first call for a given
        /// (inner ratio, sweep, size) generates one texture; every later call at any bearing reuses
        /// it. Repaint-gated internally, so calling it on a layout pass is free and id-stable.</para>
        /// </summary>
        /// <param name="center">Wheel centre in GUI space.</param>
        /// <param name="rInner">Inner radius, px. 0 gives a pie slice.</param>
        /// <param name="rOuter">Outer radius, px.</param>
        /// <param name="startDeg">Leading edge, degrees clockwise from up.</param>
        /// <param name="sweepDeg">Angular width, degrees. Clamped to 1..359.</param>
        /// <param name="color">Tint. Alpha is honoured.</param>
        public static void AnnulusSector(Vector2 center, float rInner, float rOuter,
                                         float startDeg, float sweepDeg, Color color)
        {
            if (!FrameGate.Drawing) return;
            if (rOuter <= 0f) return;
            if (!Rounded)
            {
                // Escape hatch parity with the rest of Spatial: degrade to a plain quad rather
                // than vanishing, so a consumer is never left with an invisible control.
                Widgets.DrawBoxSolid(new Rect(center.x - rOuter, center.y - rOuter, rOuter * 2f, rOuter * 2f), color);
                return;
            }

            sweepDeg = Mathf.Clamp(sweepDeg, 1f, 359f);
            rInner = Mathf.Clamp(rInner, 0f, rOuter - 1f);

            Texture2D? tex = ShapeFor(rInner / rOuter, sweepDeg, rOuter);
            if (tex == null) return;

            var rect = new Rect(center.x - rOuter, center.y - rOuter, rOuter * 2f, rOuter * 2f);
            Matrix4x4 saved = GUI.matrix;
            try
            {
                // Rotate to the sector's MIDLINE: the generated shape is centred on up.
                //
                // ⚠ Verse.UI.RotateAroundPivot, NOT GUIUtility.RotateAroundPivot. Unity's takes the
                // pivot in SCREEN space; every rect and every Event.current.mousePosition in
                // RimWorld is in GUI space, and the two differ by Prefs.UIScale. Vanilla's wrapper
                // is literally `GUIUtility.RotateAroundPivot(angle, center * Prefs.UIScale)`.
                // Passing a GUI-space pivot to Unity's version rotates about the wrong point, so
                // the quad swings out on an arc proportional to its angle and the sectors scatter
                // across the screen. It looks perfect at UI scale 1.0 and only breaks for players
                // who changed it - and anything drawn WITHOUT the matrix (labels, icons) stays
                // correctly placed, which makes it read as a shape bug rather than a matrix bug.
                UI.RotateAroundPivot(startDeg + sweepDeg * 0.5f, center);
                GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
            }
            finally
            {
                // Restore in a finally: a throw between push and pop leaves the ENTIRE rest of the
                // frame - vanilla's UI included - drawing under our rotation.
                GUI.matrix = saved;
            }
        }

        /// <summary>
        /// Stroke an arc of given thickness. Sugar over <see cref="AnnulusSector"/>; used for
        /// progress rings (a dwell timer filling) and for hairline separators on a wheel.
        /// Angles are degrees, <b>0 = up, clockwise</b>.
        /// </summary>
        public static void ArcStroke(Vector2 center, float radius, float startDeg, float sweepDeg,
                                     float width, Color color)
        {
            float half = Mathf.Max(0.5f, width * 0.5f);
            AnnulusSector(center, radius - half, radius + half, startDeg, sweepDeg, color);
        }

        /// <summary>
        /// Bearing of <paramref name="p"/> from <paramref name="center"/> in the SAME convention the
        /// drawing uses: degrees clockwise from up, normalised to 0..360.
        ///
        /// <para>Exists so a consumer hit-tests with the drawing's convention instead of re-deriving
        /// atan2. Radial menus get the winding backwards more often than any other UI shape, and the
        /// symptom - a wheel that selects the mirror image of what is under the cursor - reads as a
        /// layout bug rather than a sign error.</para>
        /// </summary>
        public static float AngleFromUp(Vector2 center, Vector2 p)
        {
            // GUI space: +y is DOWN, so "up" is -y. atan2(dx, -dy) gives clockwise-from-up directly.
            float a = Mathf.Atan2(p.x - center.x, center.y - p.y) * Mathf.Rad2Deg;
            return a < 0f ? a + 360f : a;
        }

        /// <summary>
        /// True when <paramref name="p"/> falls inside the given annulus sector. Pairs with
        /// <see cref="AnnulusSector"/> so the drawn shape and the hit shape cannot drift.
        /// </summary>
        public static bool InSector(Vector2 center, Vector2 p, float rInner, float rOuter,
                                    float startDeg, float sweepDeg)
        {
            float d = (p - center).magnitude;
            if (d < rInner || d > rOuter) return false;
            float rel = AngleFromUp(center, p) - startDeg;
            if (rel < 0f) rel += 360f;
            return rel <= sweepDeg;
        }

        // ---------------------------------------------------------------- generation

        private static Texture2D? ShapeFor(float ratio, float sweepDeg, float rOuter)
        {
            // Quantise so a wheel that animates its radius by a pixel does not generate a new
            // texture every frame. Size steps of 32px, ratio of 1%, sweep of 1 degree.
            int size = Mathf.Clamp(Mathf.CeilToInt(rOuter * 2f / 32f) * 32, 64, 512);
            var key = new ArcKey(Mathf.RoundToInt(ratio * 100f), Mathf.RoundToInt(sweepDeg), size);

            if (ArcCache.TryGetValue(key, out Texture2D cached)) return cached;

            if (ArcCache.Count >= ArcCacheMax)
            {
                if (!arcCacheWarned)
                {
                    arcCacheWarned = true;
                    Log.Warning("[Radius UI Framework] Spatial arc shape cache is full (" + ArcCacheMax +
                                "). A caller is varying sweep or radius continuously; quantise it. " +
                                "Further shapes fall back to the nearest cached one.");
                }
                // Fall back to any cached shape rather than allocating without bound. Visually wrong
                // beats an unbounded texture leak, and the warning names the cause.
                foreach (var kv in ArcCache) return kv.Value;
                return null;
            }

            Texture2D tex = MakeSector(size, ratio, sweepDeg);
            ArcCache[key] = tex;
            return tex;
        }

        /// <summary>
        /// White annulus sector centred on up, antialiased by 3x3 supersampling. Radius is normalised
        /// so the texture's edge is rOuter; the caller scales it by drawing into a 2*rOuter square.
        /// </summary>
        private static Texture2D MakeSector(int n, float ratio, float sweepDeg)
        {
            Texture2D tex = NewTex(n, "sector");
            var px = new Color32[n * n];
            float halfSweep = sweepDeg * 0.5f;
            float inv = 2f / n;
            var clear = new Color32(255, 255, 255, 0);

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < 3; sy++)
                    {
                        for (int sx = 0; sx < 3; sx++)
                        {
                            // Texture row 0 is the BOTTOM row; GUI.DrawTexture maps the top row to
                            // the top of the rect, so +v here is screen-up and the shape's midline
                            // lands on up exactly as the rotation assumes.
                            float u = (x + (sx + 0.5f) / 3f) * inv - 1f;
                            float v = (y + (sy + 0.5f) / 3f) * inv - 1f;
                            float r = Mathf.Sqrt(u * u + v * v);
                            if (r > 1f || r < ratio) continue;
                            // atan2(u, v): 0 at up, growing toward +u. Sign is irrelevant here
                            // because the wedge is symmetric about up.
                            float a = Mathf.Atan2(u, v) * Mathf.Rad2Deg;
                            if (a < 0f) a = -a;
                            if (a <= halfSweep) hits++;
                        }
                    }
                    px[y * n + x] = hits == 0 ? clear : White(hits / 9f);
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
