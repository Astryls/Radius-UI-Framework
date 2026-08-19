// Radius UI Framework - World/WorldSnapshot.cs
//
// MOVED TO THE FRAMEWORK 2026-06-09. Third occurrence of this technique in the suite -
// Wayfarers wrote it, Radius UI - Faction Menu ported and enhanced it, Radius UI - Ideology
// needs the same atlas for its spread map - so per GLOBAL_RULES §9 it becomes a shared helper
// instead of a third copy. The logic below is Faction Menu's, unchanged: only the namespace
// moved, so the two mods cannot drift.
//
// OUTSTANDING (§9 says the first two are refactored in the same change): Faction Menu still
// carries its own copy at Source/Core/WorldSnapshot.cs and must be re-pointed at this one.
// Not done here because that mod is active and its StagePane wiring was not fully reviewed;
// doing it blind is how you break a shipping consumer.
//
// Terrain colours are map CONTENT, not UI chrome, so this file deliberately does NOT route
// through Palette - the same reason Palette.Ideology carries vanilla's impact ramp verbatim.
//
// ---------------------------------------------------------------------------------------
// Original header follows.
// ---------------------------------------------------------------------------------------
//
// Real-terrain minimap bake. Technique ported from Wayfarers' UI/WorldSnapshot.cs (same
// author): a bounded BFS from a centre tile rasterising each tile's ACTUAL polygon
// (WorldGrid.GetTileVertices) onto the tangent plane, so cells tessellate exactly like the
// vanilla world map. Biome-tinted (live DrawMaterial tint when real, hand-picked fallback),
// hills read rockier, rivers blend, local relief shading.
//
// Changes from the Wayfarers original, driven by this mod's zoom range:
//  - SQUARE texture and a WORLD-UNIT range. The original's fixed 480x312 aspect had to be
//    threaded through every overlay projection (AspectYX) and stretched on non-matching
//    rects; a square bake plus a single px-per-world scale removes both problems.
//  - LIMB CULL. Orthographic tangent projection folds the far hemisphere back into the same
//    disc, so at wide zoom the far side drew garbage on top of the near side. Tiles past the
//    horizon are skipped AND not propagated, which also bounds the BFS naturally.
//  - Sub-pixel fast path: when a tile projects smaller than ~2px, plot a block instead of
//    fetching vertices and scan-filling - that is the whole-planet zoom case.
//  - Color32 buffer reused across bakes (a Color[] of this size was 4x the bytes and a fresh
//    allocation every re-bake).
//
// Terrain colours are map CONTENT (like faction colours), not UI chrome - they deliberately
// do not route through Palette.

using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Territory paint for a bake: which tiles are claimed, by whom, and how to shade them.
    /// Built by the UI layer (Core never reads Palette) and treated as IMMUTABLE by the bake -
    /// <see cref="Key"/> is the cache identity, so a changed claim map must arrive as a new key.
    /// </summary>
    public sealed class TerritoryPaint
    {
        public Dictionary<int, HashSet<int>> Map = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, Color> Colors = new Dictionary<int, Color>();
        public int SelectedId = -1;
        /// <summary>Paint claim washes at all (false = airspace-only mode).</summary>
        public bool Claims = true;
        public bool Airspace;
        public bool Permitted;
        public Color PermitColor = Color.green;
        public Color NoFlyColor = Color.red;
        public int Key;
    }

    public class WorldSnapshot
    {
        /// <summary>Square bake resolution. 768² at Color32 is ~2.3 MB reused, ~5 ms to upload.</summary>
        public const int Tex = 768;

        private Texture2D? tex;
        private Color32[]? buf;
        private int builtTile = -1;
        private float builtRange = -1f;
        private Vector3 builtEast;
        private int builtPaintKey;

        public static readonly Color WaterColor = new Color(0.10f, 0.14f, 0.20f);
        public static readonly Color RiverColor = new Color(0.21f, 0.35f, 0.5f);
        private static readonly Color RockColor = new Color(0.42f, 0.4f, 0.37f);
        private static readonly int ColorPropId = Shader.PropertyToID("_Color");

        private static readonly List<Vector3> VertBuf = new List<Vector3>();
        private static readonly List<PlanetTile> NbBuf = new List<PlanetTile>();
        private static readonly Vector2[] Poly = new Vector2[10];

        /// <summary>Planet radius in world units (all tile centres sit on the sphere).</summary>
        public static float PlanetRadius
        {
            get
            {
                var grid = Find.WorldGrid;
                if (grid == null) return 100f;
                return grid.GetTileCenter(new PlanetTile(0)).magnitude;
            }
        }

        /// <summary>
        /// Bake covering ±<paramref name="rangeWorld"/> world units around <paramref name="center"/>,
        /// oriented by an EXPLICIT basis. The caller owns the basis so it can parallel-transport
        /// it across re-centres - deriving it from a fixed world axis makes north swing as the
        /// centre moves, which reads as the map rotating.
        /// Cached: re-bakes only when centre, range or orientation changes.
        /// </summary>
        public Texture2D? Get(PlanetTile center, Vector3 east, Vector3 north, float rangeWorld,
            TerritoryPaint? paint = null)
        {
            if (!center.Valid || Find.WorldGrid == null) return null;
            int paintKey = paint?.Key ?? 0;
            if (tex != null && builtTile == center.tileId
                && Mathf.Abs(builtRange - rangeWorld) < 0.001f
                && (builtEast - east).sqrMagnitude < 1e-8f
                && builtPaintKey == paintKey)
                return tex;
            Build(center, east, north, rangeWorld, paint);
            builtTile = center.tileId;
            builtRange = rangeWorld;
            builtEast = east;
            builtPaintKey = paintKey;
            return tex;
        }

        private void Build(PlanetTile center, Vector3 east, Vector3 north, float range, TerritoryPaint? paint)
        {
            if (tex == null)
            {
                tex = new Texture2D(Tex, Tex, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                buf = new Color32[Tex * Tex];
            }
            var grid = Find.WorldGrid;
            Vector3 c = grid.GetTileCenter(center);
            Vector3 normal = c.normalized;

            float ex = range * 1.15f;
            float pxPerWorld = Tex * 0.5f / range;
            float half = Tex * 0.5f;
            float tilePx = grid.AverageTileSize * pxPerWorld;
            bool cheap = tilePx < 2.2f;
            int blockR = Mathf.Max(1, Mathf.RoundToInt(tilePx * 0.62f));

            Color32 water = WaterColor;
            var px = buf!;
            for (int i = 0; i < px.Length; i++) px[i] = water;

            var visited = new HashSet<int>();
            var queue = new Queue<PlanetTile>();
            visited.Add(center);
            queue.Enqueue(center);
            int guard = 0;
            while (queue.Count > 0 && guard++ < 400000)
            {
                PlanetTile t = queue.Dequeue();
                Vector3 tc = grid.GetTileCenter(t);

                // Horizon cull: past the limb the orthographic projection folds back onto the
                // near side. Not propagating also bounds the BFS to one hemisphere.
                if (Vector3.Dot(tc.normalized, normal) < 0.02f) continue;

                Vector3 rel = tc - c;
                float x = Vector3.Dot(rel, east), y = Vector3.Dot(rel, north);
                if (x < -ex || x > ex || y < -ex || y > ex) continue;

                NbBuf.Clear();
                grid.GetTileNeighbors(t, NbBuf);

                if (x >= -range && x <= range && y >= -range && y <= range && !grid[t].WaterCovered)
                {
                    Color col = BiomeColor(grid[t].PrimaryBiome);
                    Hilliness hl = grid[t].hilliness;
                    float rock = hl == Hilliness.Mountainous ? 0.5f : hl == Hilliness.LargeHills ? 0.28f : hl == Hilliness.SmallHills ? 0.12f : 0f;
                    if (rock > 0f) col = Color.Lerp(col, RockColor, rock);
                    if (grid[t] is SurfaceTile st && st.Rivers != null && st.Rivers.Count > 0)
                        col = Color.Lerp(col, RiverColor, 0.5f);
                    float e0 = grid[t].elevation, avg = 0f;
                    for (int k = 0; k < NbBuf.Count; k++) avg += grid[NbBuf[k]].elevation;
                    if (NbBuf.Count > 0) { avg /= NbBuf.Count; col *= Mathf.Clamp(1f + (e0 - avg) * 0.0035f, 0.72f, 1.32f); }
                    col *= 0.95f + (Mathf.Abs(t.tileId * 37) % 100) / 100f * 0.1f;
                    col.a = 1f;

                    // Territory paint rides the fill we already do - one dictionary hit per
                    // tile inside an existing loop, so claims cost nothing per frame.
                    Color alt = col;
                    int hatch = 0;
                    if (paint != null)
                    {
                        paint.Map.TryGetValue(t.tileId, out HashSet<int> owners);
                        // Rim detection: claimed here, NOT claimed next door. The neighbour
                        // list is already in hand for the relief shading, so this is free.
                        bool claimEdge = false;
                        if (owners != null && paint.SelectedId >= 0 && owners.Contains(paint.SelectedId))
                        {
                            for (int k = 0; k < NbBuf.Count; k++)
                            {
                                if (paint.Map.TryGetValue(NbBuf[k].tileId, out HashSet<int> no)
                                    && no != null && no.Contains(paint.SelectedId)) continue;
                                claimEdge = true;
                                break;
                            }
                        }
                        Tint(paint, owners, claimEdge, ref col, ref alt, ref hatch);
                    }
                    Color32 col32 = col;
                    Color32 alt32 = alt;

                    if (cheap)
                    {
                        // Whole-planet zoom: the polygon is sub-pixel, so skip the vertex
                        // fetch and scan fill entirely (hatching is meaningless at this size).
                        int cxp = Mathf.RoundToInt(x * pxPerWorld + half);
                        int cyp = Mathf.RoundToInt(y * pxPerWorld + half);
                        FillBlock(px, cxp, cyp, blockR, col32);
                    }
                    else
                    {
                        VertBuf.Clear();
                        grid.GetTileVertices(t, VertBuf);
                        int cnt = Mathf.Min(VertBuf.Count, Poly.Length);
                        for (int i = 0; i < cnt; i++)
                        {
                            Vector3 vr = VertBuf[i] - c;
                            Poly[i] = new Vector2(
                                Vector3.Dot(vr, east) * pxPerWorld + half,
                                Vector3.Dot(vr, north) * pxPerWorld + half);
                        }
                        FillConvex(px, cnt, col32, alt32, hatch);
                    }
                }

                for (int k = 0; k < NbBuf.Count; k++)
                    if (visited.Add(NbBuf[k])) queue.Enqueue(NbBuf[k]);
            }

            tex.SetPixels32(px);
            tex.Apply(false);
        }

        private static void FillBlock(Color32[] px, int cx, int cy, int r, Color32 col)
        {
            int x0 = Mathf.Max(0, cx - r), x1 = Mathf.Min(Tex - 1, cx + r);
            int y0 = Mathf.Max(0, cy - r), y1 = Mathf.Min(Tex - 1, cy + r);
            for (int y = y0; y <= y1; y++)
            {
                int row = y * Tex;
                for (int x = x0; x <= x1; x++) px[row + x] = col;
            }
        }

        /// <summary>
        /// Blend a tile's claim into its terrain colour. Contested tiles (vassalage lets more
        /// than one faction claim a tile) HATCH rather than blend - a blend of two claims is
        /// mud that hides the fact it is contested at all.
        /// </summary>
        private static void Tint(TerritoryPaint p, HashSet<int>? owners, bool claimEdge,
            ref Color col, ref Color alt, ref int hatch)
        {
            if (owners == null || owners.Count == 0) return;

            bool selHere = p.SelectedId >= 0 && owners.Contains(p.SelectedId);
            if (!p.Claims && !(p.Airspace && selHere)) return;

            int ownerId = -1;
            if (selHere) ownerId = p.SelectedId;
            else foreach (int o in owners) { ownerId = o; break; }
            if (!p.Colors.TryGetValue(ownerId, out Color oc)) return;

            if (p.Claims)
            {
                if (selHere)
                {
                    // A flat wash vanishes into terrain of a similar hue (a navy faction over
                    // dark forest reads as nothing). Draw the claim as a solid fill plus a
                    // BRIGHT one-tile rim: a political-map border reads at any zoom.
                    col = claimEdge
                        ? Color.Lerp(col, Color.Lerp(oc, Color.white, 0.30f), 0.94f)
                        : Color.Lerp(col, oc, 0.55f);
                }
                else
                {
                    col = Color.Lerp(col, oc, 0.18f);
                }
                alt = col;

                if (owners.Count > 1)
                {
                    alt = Color.Lerp(col, Color.Lerp(oc, Color.white, 0.20f), selHere ? 0.85f : 0.45f);
                    hatch = 1;
                    return;
                }
            }

            // Airspace is transit STATE over the same tiles (Air Territories has no map of its
            // own), and only for the faction in focus.
            if (p.Airspace && selHere)
            {
                if (p.Permitted)
                {
                    col = Color.Lerp(col, p.PermitColor, claimEdge ? 0.78f : 0.42f);
                    alt = col;
                }
                else
                {
                    col = Color.Lerp(col, p.NoFlyColor, 0.30f);
                    alt = Color.Lerp(col, p.NoFlyColor, 0.90f);
                    hatch = 2;
                }
            }
        }

        private static void FillConvex(Color32[] px, int n, Color32 color) => FillConvex(px, n, color, color, 0);

        /// <summary>Scan-fill a tile polygon; hatch 1 = diagonal, 2 = anti-diagonal stripes of <paramref name="alt"/>.</summary>
        private static void FillConvex(Color32[] px, int n, Color32 color, Color32 alt, int hatch)
        {
            if (n < 3) return;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                var p = Poly[i];
                if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
            }
            int x0 = Mathf.Max(0, Mathf.FloorToInt(minX)), x1 = Mathf.Min(Tex - 1, Mathf.CeilToInt(maxX));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(minY)), y1 = Mathf.Min(Tex - 1, Mathf.CeilToInt(maxY));
            for (int y = y0; y <= y1; y++)
            {
                int row = y * Tex;
                for (int x = x0; x <= x1; x++)
                {
                    if (!InsideConvex(n, x + 0.5f, y + 0.5f)) continue;
                    if (hatch == 0) { px[row + x] = color; continue; }
                    int band = hatch == 1 ? ((x + y) >> 2) : ((x - y) >> 2);
                    px[row + x] = (band & 1) == 0 ? color : alt;
                }
            }
        }

        private static bool InsideConvex(int n, float px, float py)
        {
            bool pos = false, neg = false;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = Poly[i], b = Poly[(i + 1) % n];
                float cross = (b.x - a.x) * (py - a.y) - (b.y - a.y) * (px - a.x);
                if (cross > 0.0001f) pos = true;
                else if (cross < -0.0001f) neg = true;
                if (pos && neg) return false;
            }
            return true;
        }

        /// <summary>
        /// Seed basis at a centre tile, from a fixed world axis. Use ONCE to establish an
        /// orientation; carry it forward with <see cref="Transport"/> instead of re-seeding,
        /// or the map visibly rotates whenever the centre moves.
        /// </summary>
        public static bool Basis(PlanetTile center, out Vector3 centerPos, out Vector3 east, out Vector3 north)
        {
            centerPos = Vector3.zero; east = Vector3.right; north = Vector3.up;
            var grid = Find.WorldGrid;
            if (grid == null || !center.Valid) return false;
            centerPos = grid.GetTileCenter(center);
            Vector3 normal = centerPos.normalized;
            east = Vector3.Cross(Vector3.up, normal);
            if (east.sqrMagnitude < 0.0001f) east = Vector3.Cross(Vector3.forward, normal);
            east.Normalize();
            north = Vector3.Cross(normal, east).normalized;
            east = -east;   // match world-view handedness
            return true;
        }

        /// <summary>
        /// Parallel-transport a basis to a new centre: keep north pointing the same way by
        /// projecting it onto the new tangent plane, instead of re-deriving it from a world
        /// axis. This is what keeps the map from rotating when the view re-centres.
        /// </summary>
        public static void Transport(PlanetTile newCenter, ref Vector3 east, ref Vector3 north)
        {
            var grid = Find.WorldGrid;
            if (grid == null || !newCenter.Valid) return;
            Vector3 normal = grid.GetTileCenter(newCenter).normalized;
            Vector3 n2 = north - normal * Vector3.Dot(north, normal);
            if (n2.sqrMagnitude < 1e-6f)
            {
                // Degenerate only if we travelled a full quarter-turn in one step.
                Basis(newCenter, out _, out east, out north);
                return;
            }
            north = n2.normalized;
            east = Vector3.Cross(north, normal);   // consistent with north = cross(normal, east)
        }

        /// <summary>
        /// Tangent-plane offset of a tile from the centre, in WORLD units, under an explicit
        /// basis. False when the target is past the horizon (where the projection would fold
        /// it onto the near side).
        /// </summary>
        public static bool ProjectWorld(PlanetTile center, Vector3 east, Vector3 north, PlanetTile target, out Vector2 world)
        {
            world = Vector2.zero;
            var grid = Find.WorldGrid;
            if (grid == null || !center.Valid || !target.Valid) return false;
            Vector3 c = grid.GetTileCenter(center);
            Vector3 tc = grid.GetTileCenter(target);
            if (Vector3.Dot(tc.normalized, c.normalized) < 0.02f) return false;
            Vector3 rel = tc - c;
            world = new Vector2(Vector3.Dot(rel, east), Vector3.Dot(rel, north));
            return true;
        }

        /// <summary>World position on the sphere for a tangent-plane offset (drag/pan inverse).</summary>
        public static Vector3 Unproject(PlanetTile center, Vector3 east, Vector3 north, Vector2 world)
        {
            var grid = Find.WorldGrid;
            if (grid == null || !center.Valid) return Vector3.zero;
            return grid.GetTileCenter(center) + east * world.x + north * world.y;
        }

        public static Color BiomeColor(BiomeDef? b)
        {
            if (b == null) return Fallback("");
            try
            {
                // Only read .color when the shader actually has _Color - the vanilla
                // world-terrain shader does not, and the getter logs per call.
                var m = b.DrawMaterial;
                if (m != null && m.HasProperty(ColorPropId))
                {
                    Color c = m.color;
                    bool nearWhite = c.r > 0.85f && c.g > 0.85f && c.b > 0.85f;
                    bool nearBlack = c.r + c.g + c.b < 0.15f;
                    if (!nearWhite && !nearBlack && c.a > 0.05f) return new Color(c.r, c.g, c.b);
                }
            }
            catch { }
            return Fallback(b.defName);
        }

        private static Color Fallback(string dn)
        {
            if (dn.Contains("Ice") || dn.Contains("Sea")) return new Color(0.74f, 0.78f, 0.82f);
            if (dn.Contains("Tundra")) return new Color(0.46f, 0.5f, 0.49f);
            if (dn.Contains("ExtremeDesert")) return new Color(0.72f, 0.64f, 0.43f);
            if (dn.Contains("Desert")) return new Color(0.64f, 0.56f, 0.38f);
            if (dn.Contains("Arid")) return new Color(0.5f, 0.47f, 0.32f);
            if (dn.Contains("Tropical")) return new Color(0.22f, 0.4f, 0.22f);
            if (dn.Contains("Boreal")) return new Color(0.26f, 0.4f, 0.3f);
            if (dn.Contains("Forest")) return new Color(0.3f, 0.44f, 0.27f);
            if (dn.Contains("Swamp") || dn.Contains("Marsh")) return new Color(0.3f, 0.37f, 0.29f);
            return new Color(0.32f, 0.36f, 0.3f);
        }
    }
}
