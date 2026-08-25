// Radius UI Framework · World/OrbitalSection.cs
// The altitude cross-section: an edge-on strip showing off-surface holdings by LONGITUDE
// across and ALTITUDE up. Concept K of the orbital-map studies.
//
// WHY THIS EXISTS. Odyssey puts content on separate PlanetLayers - real tile spheres with
// their own radius. A world map is a projection of ONE sphere, so off-surface objects have no
// honest place on it: projecting them onto the surface plane drops orbital platforms into
// fields, distinguishable only by colour. This strip gives them somewhere true to live.
//
// WHY IT NEEDS NO BAKE. Every orbit tile is the Space biome, so colouring terrain up there
// would render one flat field - an orbital view is about objects and altitude, not ground.
// That makes this pure layout over world objects: no texture, no BFS, no projection cache.
//
// THE VERTICAL AXIS IS NOT TO SCALE, DELIBERATELY. Surface, 200km and 384,400km cannot share
// a linear axis - the moon would be kilometres off-screen. Rows are evenly spaced and labelled
// with each layer's own elevationString, so the compression is declared rather than hidden.
// Latitude is NOT represented at all; this is a companion to a map, never a replacement.

using System;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>One off-surface object to place in the cross-section.</summary>
    public struct OrbitalItem
    {
        public PlanetTile tile;
        public Color color;
        /// <summary>Shown under the marker. Optional.</summary>
        public string? label;
        /// <summary>Tooltip body. Optional.</summary>
        public string? tip;
        public bool selected;
        /// <summary>Unclaimed or derelict - drawn muted rather than in a faction colour.</summary>
        public bool ghost;
        /// <summary>Opaque caller data, handed back to onClick.</summary>
        public object? payload;
    }

    public static class OrbitalSection
    {
        private const float RowH = 38f;
        private const float GroundH = 16f;
        private const float PadV = 8f;
        private const float LabelW = 86f;
        private const float EdgePad = 20f;

        private static readonly List<PlanetLayer> LayerBuf = new List<PlanetLayer>();

        /// <summary>Distinct layers present, highest first. Reused buffer - do not retain.</summary>
        private static List<PlanetLayer> LayersOf(List<OrbitalItem> items)
        {
            LayerBuf.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                PlanetTile t = items[i].tile;
                if (!t.Valid || t.Layer == null || t.Layer.IsRootSurface) continue;
                if (!LayerBuf.Contains(t.Layer)) LayerBuf.Add(t.Layer);
            }
            LayerBuf.Sort((a, b) => b.Radius.CompareTo(a.Radius));   // furthest out on top
            return LayerBuf;
        }

        /// <summary>Height this strip needs, or 0 when there is nothing off-surface to show.</summary>
        public static float HeightFor(List<OrbitalItem> items)
        {
            int rows = LayersOf(items).Count;
            return rows == 0 ? 0f : PadV * 2f + rows * RowH + GroundH;
        }

        /// <summary>
        /// Draw the strip. <paramref name="origin"/> plus <paramref name="east"/> is the host
        /// map's projection basis; <paramref name="pxPerWorld"/> and
        /// <paramref name="originScreenX"/> map world units to the host's screen X, so an item
        /// sits at the same horizontal position as the ground it is above and pans with it.
        /// </summary>
        public static void Draw(Rect rect, List<OrbitalItem> items, PlanetTile origin,
            Vector3 east, float pxPerWorld, float originScreenX, Action<OrbitalItem>? onClick = null)
        {
            List<PlanetLayer> layers = LayersOf(items);
            if (layers.Count == 0) return;
            WorldGrid? grid = Find.WorldGrid;
            if (grid == null || !origin.Valid) return;

            Spatial.Well(rect, Palette.Surface0);
            Starfield(rect);

            Vector3 c = grid.GetTileCenter(origin);
            Vector3 cn = c.normalized;
            float cMag = c.magnitude;

            float groundY = rect.yMax - PadV - GroundH * 0.5f;
            float x0 = rect.x + LabelW, x1 = rect.xMax - 10f;

            // Rows, furthest out first.
            for (int r = 0; r < layers.Count; r++)
            {
                PlanetLayer layer = layers[r];
                float y = rect.y + PadV + RowH * r + RowH * 0.5f;
                DashRule(x0, x1, y, Palette.BorderFaint);

                RadiusFont.Label(new Rect(rect.x + 8f, y - 15f, LabelW - 12f, 15f),
                    layer.Def.LabelCap, GameFont.Small, heading: false, color: Palette.TextDim,
                    anchor: TextAnchor.MiddleRight, wrap: false);
                string alt = AltitudeLabel(layer);
                if (!alt.NullOrEmpty())
                    RadiusFont.Label(new Rect(rect.x + 8f, y, LabelW - 12f, 15f), alt,
                        GameFont.Small, heading: false, color: Palette.TextGhost,
                        anchor: TextAnchor.MiddleRight, wrap: false);
            }

            // The ground: a solid rule, so "down" is unambiguous against the dashed layers.
            Widgets.DrawBoxSolid(new Rect(x0, groundY, x1 - x0, 1f), Palette.Border);
            RadiusFont.Label(new Rect(rect.x + 8f, groundY - 8f, LabelW - 12f, 16f),
                grid.Surface?.Def?.LabelCap ?? "", GameFont.Small, heading: false,
                color: Palette.TextGhost, anchor: TextAnchor.MiddleRight, wrap: false);

            // Tethers first so markers sit on top of them.
            for (int i = 0; i < items.Count; i++)
            {
                if (!TryPlace(items[i], grid, c, cn, cMag, east, pxPerWorld, originScreenX,
                        x0, x1, layers, rect, out float sx, out float sy, out int _)) continue;
                Color t = items[i].ghost ? Palette.TextGhost : items[i].color;
                t.a = 0.22f;
                Widgets.DrawBoxSolid(new Rect(sx - 0.5f, sy + 9f, 1f, groundY - sy - 9f), t);
            }

            for (int i = 0; i < items.Count; i++)
            {
                OrbitalItem it = items[i];
                if (!TryPlace(it, grid, c, cn, cMag, east, pxPerWorld, originScreenX,
                        x0, x1, layers, rect, out float sx, out float sy, out int off)) continue;

                Color col = it.ghost ? Palette.TextFaint : it.color;
                float d = it.selected ? 15f : 12f;
                var dot = new Rect(sx - d * 0.5f, sy - d * 0.5f, d, d);
                Spatial.Ring(new Rect(sx - d - 3f, sy - d * 0.42f, (d + 3f) * 2f, d * 0.84f),
                    it.selected ? col : col * new Color(1f, 1f, 1f, 0.5f));
                Spatial.Dot(dot, Palette.Surface0);
                Spatial.Ring(dot, col);
                if (it.selected) Spatial.Dot(dot.ContractedBy(d * 0.32f), col);

                if (off != 0)
                    (off < 0 ? IconSet.Action.Prev : IconSet.Action.Next)
                        .Draw(new Rect(sx + (off < 0 ? -d - 11f : d + 1f), sy - 5f, 11f, 11f), Palette.TextFaint);

                var hit = new Rect(sx - 12f, sy - 12f, 24f, 24f);
                // The widget draws the chevron, so the widget explains it - the caller has no
                // way to know an item was clamped.
                string tip = it.tip ?? "";
                if (off != 0)
                    tip = (tip.NullOrEmpty() ? "" : tip + "\n") + "RadiusUI.Orbital.OutsideView".Translate();
                if (!tip.NullOrEmpty()) TooltipHandler.TipRegion(hit, tip);
                if (onClick != null && Widgets.ButtonInvisible(hit)) onClick(it);
            }
        }

        /// <summary>
        /// Horizontal placement: drop the object to the SURFACE radius before projecting, so its
        /// longitude is directly comparable with the ground beneath it. Objects on the far side
        /// of the planet are refused rather than clamped - clamping asserts a direction that is
        /// not true.
        /// </summary>
        private static bool TryPlace(OrbitalItem it, WorldGrid grid, Vector3 c, Vector3 cn, float cMag,
            Vector3 east, float pxPerWorld, float originScreenX, float x0, float x1,
            List<PlanetLayer> layers, Rect rect, out float sx, out float sy, out int off)
        {
            sx = 0f; sy = 0f; off = 0;
            PlanetTile t = it.tile;
            if (!t.Valid || t.Layer == null || t.Layer.IsRootSurface) return false;
            int row = layers.IndexOf(t.Layer);
            if (row < 0) return false;

            Vector3 p = grid.GetTileCenter(t);
            if (p == Vector3.zero) return false;
            Vector3 dir = p.normalized;
            if (Vector3.Dot(dir, cn) < 0.02f) return false;   // far side of the planet

            sx = originScreenX + Vector3.Dot(dir * cMag - c, east) * pxPerWorld;
            if (sx < x0 + EdgePad) { sx = x0 + EdgePad; off = -1; }
            else if (sx > x1 - EdgePad) { sx = x1 - EdgePad; off = 1; }
            sy = rect.y + PadV + RowH * row + RowH * 0.5f;
            return true;
        }

        /// <summary>
        /// A layer-wide altitude, only when the def gives a literal. The field is a FORMAT
        /// string filled per tile ("{0}m"), so a surface-style value says nothing about a layer
        /// and is better omitted than printed with a hole in it.
        /// </summary>
        private static string AltitudeLabel(PlanetLayer layer)
        {
            string s = layer.Def?.elevationString ?? "";
            return s.NullOrEmpty() || s.Contains("{0}") ? "" : s;
        }

        private static void DashRule(float x0, float x1, float y, Color c)
        {
            for (float x = x0; x < x1; x += 9f)
                Widgets.DrawBoxSolid(new Rect(x, y, Mathf.Min(5f, x1 - x), 1f), c);
        }

        /// <summary>Deterministic, so it never shimmers between frames.</summary>
        private static void Starfield(Rect r)
        {
            int spanX = Mathf.Max(1, (int)(r.width - 16f));
            int spanY = Mathf.Max(1, (int)(r.height - 12f));
            for (int i = 0; i < 60; i++)
            {
                float sx = r.x + 8f + (i * 97) % spanX;
                float sy = r.y + 6f + (i * 53) % spanY;
                float s = i % 9 == 0 ? 2.2f : 1.5f;
                Spatial.Dot(new Rect(sx, sy, s, s), i % 3 == 0 ? Palette.TextFaint : Palette.TextGhost);
            }
        }
    }
}
