// Radius UI Framework : Source/Map/MapCapture.cs
//
// A LIVE CAMERA FEED of a piece of the playing map, rendered into a RenderTexture that any
// panel can draw with GUI.DrawTexture. Promoted here from Radius UI - Colonist Bar's
// LiveViewManager (2026-08-26), which had the same machinery welded to per-pawn portraits.
//
// HOW IT WORKS. RimWorld draws the map by submitting Graphics.DrawMesh calls during
// Map.MapUpdate. Those submissions are frame-scoped and are picked up by EVERY camera that
// renders in that frame, so a second, disabled camera pointed anywhere on the map and told
// to Render() will draw whatever the engine already submitted - no re-render of the world,
// no PawnCacheRenderer (which thrashes the shared render tree and flickers the in-world
// pawn), just a second view of the same submissions.
//
// WHY THE DRIVER IS A MapComponent. The capture has to happen AFTER the map's submissions
// and BEFORE Unity renders. Map.MapUpdate ends with MapComponentUtility.MapComponentUpdate,
// which is exactly that window - and it needs no Harmony at all. The frame order is:
//     UIRoot.UIRootUpdate  (windows update, selection overlays submitted)
//     Game.UpdatePlay      -> Map.MapUpdate: mesh + things + overlays -> MapComponentUpdate
//     OnGUI                (panels draw; they read the texture captured moments earlier)
// Note that a Window's own WindowUpdate is NOT usable: Root_Play.Update calls base.Update()
// (which pumps the UI root) BEFORE Current.Game.UpdatePlay(), so at WindowUpdate time this
// frame's map geometry has not been submitted yet and the capture would come back empty.
//
// THE OFF-SCREEN PROBLEM, AND WHY THIS ONE SOLVES IT. Both halves of the map draw are culled
// to the player's own camera: MapDrawer.DrawMapMesh only draws sections overlapping
// Find.CameraDriver.CurrentViewRect, and DynamicDrawManager.ComputeCulledThings culls things
// to that same rect expanded by 1. A naive second camera therefore shows EMPTY SPACE for
// anything the player is not already looking at - which is most of the time, and is the
// limitation the Colonist Bar lives with (it simply declares off-view pawns ineligible).
// Here we submit the missing geometry ourselves before rendering: Section.DrawSection() is
// public and regenerates its own dirty layers, and Thing.DynamicDrawPhase(DrawPhase.Draw)
// self-heals its pre-render results (PawnRenderer.RenderPawnAt re-runs ParallelPreRenderPawnAt
// when results are invalid). Cells already inside the engine's cull rect are SKIPPED, so
// nothing is ever submitted twice - a double draw would re-run every other mod's postfix on
// the pawn draw path for the same pawn in the same frame.
//
// FOG IS HONOURED: things standing in fogged cells are not force-submitted, so a feed cannot
// be used to see what a colonist cannot.

using System;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Live map-camera feeds, keyed by a caller-chosen id. Thread affinity: main thread only
    /// (it touches Graphics, Camera and the map's draw structures).
    /// </summary>
    public static class MapCapture
    {
        public const int MinResolution = 64;
        public const int MaxResolution = 512;

        /// <summary>Frames a feed may go unrequested before its texture is released.</summary>
        private const int StaleFrames = 6;

        private const float MinOrtho = 3.5f;    // ~7 cells tall: closer than this reads as a smear
        private const float MaxOrtho = 90f;
        private const int ThingScanPad = 3;     // pawns are grid-indexed at Position, drawn at DrawPos

        /// <summary>
        /// Default capture rate. NOT 60: a camera render is the single most expensive thing in
        /// here, the subject is a few pawns walking, and 24 is the rate an audience has accepted
        /// as "moving picture" for a century. The user's framework refresh multiplier scales the
        /// interval on top, so the existing perf slider governs this too.
        /// </summary>
        private const float DefaultRefreshHz = 24f;

        /// <summary>Seconds for the eased camera to close half the distance to its aim.</summary>
        private const float EaseHalfLife = 0.13f;

        /// <summary>Beyond this much movement the camera CUTS instead of gliding.</summary>
        private const float SnapCells = 22f;

        /// <summary>Aim changes smaller than this are ignored outright (anti-jitter).</summary>
        private const float DeadZoneCells = 0.75f;

        private const float MaxEaseStep = 0.25f;   // clamp dt across a stall / alt-tab

        private sealed class Feed
        {
            public Map? Map;
            public CellRect Focus;
            public float Aspect = 1f;
            public int Resolution = 224;
            public float RefreshHz = DefaultRefreshHz;
            public RenderTexture? Rt;
            public int LastRequestFrame = -1;
            public bool Ready;

            // ---- eased camera state ----
            // The AIM is what the caller asked for; these are where the camera actually is.
            // Keeping them apart is the whole of the stabilisation: a subject list whose
            // bounding box pops by a few cells as pawns shuffle moves the aim, not the shot.
            public bool Aimed;
            public Vector3 CurCentre;
            public float CurHalf = 10f;
            public Vector3 AimCentre;
            public float AimHalf = 10f;
            public float LastRenderTime = -1f;
        }

        private static readonly Dictionary<string, Feed> feeds = new Dictionary<string, Feed>();
        private static readonly List<string> expired = new List<string>();

        // Dedupe by id, NOT by Thing reference: a static set holding Things would root the
        // whole map graph if it were ever left non-empty (it is cleared after every use).
        private static readonly HashSet<int> submitted = new HashSet<int>();

        private static Camera? cam;

        /// <summary>
        /// Ask for a live view of <paramref name="focus"/> on <paramref name="map"/>, and get
        /// back the most recent capture (null until the first one lands, one frame later).
        /// Call this EVERY FRAME you intend to draw the feed; a feed that stops being asked
        /// for releases its texture within a few frames.
        /// </summary>
        /// <param name="key">Stable per-panel id, e.g. "RadiusUI.QuestMenu.Live".</param>
        /// <param name="aspect">width/height of the rect the texture will be drawn into.</param>
        /// <param name="refreshHz">Capture rate; 0 uses the default. Scaled by the user's
        /// framework refresh multiplier either way.</param>
        public static Texture? Request(string key, Map map, CellRect focus, float aspect,
            int resolution = 224, float refreshHz = 0f)
        {
            if (string.IsNullOrEmpty(key) || map == null || focus.Area <= 0)
            {
                return null;
            }
            if (!feeds.TryGetValue(key, out Feed f))
            {
                f = new Feed();
                feeds[key] = f;
            }
            if (f.Map != map)
            {
                f.Aimed = false;   // different map: cut, never glide across a map change
            }
            f.Map = map;
            f.Focus = focus.ClipInsideMap(map);
            f.Aspect = Mathf.Clamp(aspect, 0.2f, 6f);
            f.Resolution = Mathf.Clamp(resolution, MinResolution, MaxResolution);
            f.RefreshHz = refreshHz > 0f ? Mathf.Clamp(refreshHz, 1f, 60f) : DefaultRefreshHz;
            f.LastRequestFrame = Time.frameCount;
            return f.Ready ? f.Rt : null;
        }

        /// <summary>
        /// Cut rather than glide on the next capture. Call when the SUBJECT changes (a new
        /// quest selected, a different pawn) - easing across an unrelated jump reads as the
        /// camera flying over the map rather than as a new shot.
        /// </summary>
        public static void Cut(string key)
        {
            if (key != null && feeds.TryGetValue(key, out Feed f))
            {
                f.Aimed = false;
            }
        }

        /// <summary>Drop a feed and free its texture now. Safe to call for an unknown key.</summary>
        public static void Release(string key)
        {
            if (key != null && feeds.TryGetValue(key, out Feed f))
            {
                FreeRt(f);
                feeds.Remove(key);
            }
        }

        /// <summary>True when this feed has produced at least one frame.</summary>
        public static bool IsLive(string key)
        {
            return key != null && feeds.TryGetValue(key, out Feed f) && f.Ready;
        }

        // ------------------------------------------------------------------ driver

        /// <summary>
        /// Render every live feed on this map. Called once per frame per map from
        /// <see cref="MapCaptureDriver"/>, at the end of Map.MapUpdate.
        /// </summary>
        internal static void Pump(Map map)
        {
            if (feeds.Count == 0 || map == null)
            {
                return;
            }
            if (Current.ProgramState != ProgramState.Playing || map != Find.CurrentMap)
            {
                return;
            }
            // World view: the map submitted nothing this frame, so a capture would be empty
            // (and would freeze the last good frame instead, which is what we want).
            if (!WorldRendererUtility.DrawingMap)
            {
                return;
            }

            int frame = Time.frameCount;
            expired.Clear();
            foreach (KeyValuePair<string, Feed> kv in feeds)
            {
                Feed f = kv.Value;
                if (frame - f.LastRequestFrame > StaleFrames)
                {
                    expired.Add(kv.Key);
                    continue;
                }
                if (f.Map != map)
                {
                    continue;
                }
                try
                {
                    Render(f);
                }
                catch (Exception e)
                {
                    // One line per distinct failure: this runs every frame, and a feed that
                    // throws must not turn into a log flood.
                    Log.WarningOnce("[Radius UI] map capture failed: " + e, 0x5E1A_0001);
                    f.Ready = false;
                }
            }
            for (int i = 0; i < expired.Count; i++)
            {
                Release(expired[i]);
            }
            expired.Clear();
        }

        private static void Render(Feed f)
        {
            Map map = f.Map!;
            Camera main = Find.Camera;
            if (main == null)
            {
                return;
            }

            // ---- aim ----
            // Orthographic size is a HALF-HEIGHT in world units (= cells). Derive it from the
            // focus rect and the target aspect so the subject is never cropped and never
            // stretched, then leave a little air around it.
            Vector3 aimCentre = f.Focus.CenterVector3;
            float aimHalf = Mathf.Clamp(
                Mathf.Max(f.Focus.Height * 0.5f, f.Focus.Width * 0.5f / f.Aspect) * 1.08f,
                MinOrtho, MaxOrtho);

            // Dead zone: a bounding box that pops a cell as a pawn steps is not a new shot.
            if (f.Aimed
                && Mathf.Abs(aimCentre.x - f.AimCentre.x) < DeadZoneCells
                && Mathf.Abs(aimCentre.z - f.AimCentre.z) < DeadZoneCells
                && Mathf.Abs(aimHalf - f.AimHalf) < DeadZoneCells)
            {
                aimCentre = f.AimCentre;
                aimHalf = f.AimHalf;
            }
            f.AimCentre = aimCentre;
            f.AimHalf = aimHalf;

            // ---- rate gate ----
            float now = Time.realtimeSinceStartup;
            float interval = 1f / f.RefreshHz * Mathf.Max(0.1f, RadiusTheme.RefreshMult);
            float dt = f.LastRenderTime < 0f ? 0f : now - f.LastRenderTime;
            if (f.Ready && f.Aimed && dt < interval)
            {
                return;   // hold the last capture; it is still on screen and still correct
            }
            f.LastRenderTime = now;

            // ---- ease ----
            // Exponential smoothing on the ELAPSED time between captures, so the glide looks
            // the same at 24 Hz as at 60 and does not change speed with the refresh setting.
            float travel = (aimCentre - f.CurCentre).magnitude;
            if (!f.Aimed || travel > SnapCells)
            {
                f.CurCentre = aimCentre;
                f.CurHalf = aimHalf;
                f.Aimed = true;
            }
            else if (dt > 0f)
            {
                float k = 1f - Mathf.Pow(0.5f, Mathf.Min(dt, MaxEaseStep) / EaseHalfLife);
                f.CurCentre = Vector3.Lerp(f.CurCentre, aimCentre, k);
                f.CurHalf = Mathf.Lerp(f.CurHalf, aimHalf, k);
            }

            Vector3 centre = f.CurCentre;
            float half = f.CurHalf;

            // The cells the capture will actually see - which is what has to be submitted,
            // not merely the focus rect.
            int cx = Mathf.RoundToInt(centre.x);
            int cz = Mathf.RoundToInt(centre.z);
            int hw = Mathf.CeilToInt(half * f.Aspect) + 1;
            int hh = Mathf.CeilToInt(half) + 1;
            var visible = new CellRect(cx - hw, cz - hh, hw * 2 + 1, hh * 2 + 1).ClipInsideMap(map);
            SubmitOffscreen(map, visible);

            RenderTexture? rt = EnsureRt(f);
            if (rt == null)
            {
                return;
            }
            EnsureCam();
            Camera c = cam!;
            Vector3 mainPos = main.transform.position;

            c.CopyFrom(main);       // culling mask, clear flags, depth, HDR, ... then override
            c.enabled = false;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0f, 0f, 0f, 0f);
            c.aspect = f.Aspect;
            c.orthographic = true;
            c.orthographicSize = half;
            c.targetTexture = rt;
            c.transform.rotation = main.transform.rotation;
            c.transform.position = new Vector3(centre.x, mainPos.y, centre.z);

            // Push the near plane below the top altitude band so the capture excludes the
            // selection marquee - MapInterfaceUpdate submits it EARLIER in the frame than
            // this runs, so without the clip every selected pawn would come with a bracket.
            float clipAlt = (AltitudeLayer.MapDataOverlay.AltitudeFor()
                             + AltitudeLayer.MetaOverlays.AltitudeFor()) * 0.5f;
            c.nearClipPlane = Mathf.Max(0.05f, mainPos.y - clipAlt);

            c.Render();
            c.targetTexture = null;
            f.Ready = true;
        }

        // ------------------------------------------------------------------ off-screen submit

        /// <summary>
        /// Submit the terrain sections and dynamic things inside <paramref name="rect"/> that
        /// the engine culled away this frame because the player is not looking at them.
        /// Anything inside the engine's own cull rect is skipped - it is already submitted,
        /// and drawing it twice would run every draw-path postfix in the modlist twice.
        /// </summary>
        private static void SubmitOffscreen(Map map, CellRect rect)
        {
            CameraDriver driver = Find.CameraDriver;
            if (driver == null)
            {
                return;
            }
            // The exact rect DynamicDrawManager.ComputeCulledThings uses.
            CellRect view = driver.CurrentViewRect.ExpandedBy(1).ClipInsideMap(map);
            if (view.Contains(rect.Min) && view.Contains(rect.Max))
            {
                return;   // wholly on screen: the engine already did all of this
            }

            SubmitSections(map, rect, view);
            SubmitThings(map, rect.ExpandedBy(ThingScanPad).ClipInsideMap(map), view);
        }

        private static void SubmitSections(Map map, CellRect rect, CellRect view)
        {
            MapDrawer drawer = map.mapDrawer;
            if (drawer == null)
            {
                return;
            }
            const int SectionSize = 17;   // Verse.Section.Size
            int sx0 = rect.minX / SectionSize, sx1 = rect.maxX / SectionSize;
            int sz0 = rect.minZ / SectionSize, sz1 = rect.maxZ / SectionSize;
            for (int sx = sx0; sx <= sx1; sx++)
            {
                for (int sz = sz0; sz <= sz1; sz++)
                {
                    Section s = drawer.SectionAt(new IntVec3(sx * SectionSize, 0, sz * SectionSize));
                    // DrawMapMesh's own test: a section overlapping the view was drawn whole.
                    if (s != null && !view.Overlaps(s.Bounds))
                    {
                        s.DrawSection();
                    }
                }
            }
        }

        private static void SubmitThings(Map map, CellRect rect, CellRect view)
        {
            ThingGrid grid = map.thingGrid;
            FogGrid fog = map.fogGrid;
            if (grid == null)
            {
                return;
            }
            submitted.Clear();
            foreach (IntVec3 c in rect)
            {
                if (view.Contains(c) || fog.IsFogged(c))
                {
                    continue;
                }
                List<Thing> things = grid.ThingsListAtFast(c);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing t = things[i];
                    if (t == null || t.def == null || t.def.drawerType == DrawerType.MapMeshOnly)
                    {
                        continue;   // map-mesh things came with the section
                    }
                    if (!submitted.Add(t.thingIDNumber))
                    {
                        continue;   // multi-cell thing already handled
                    }
                    try
                    {
                        // Draw alone is enough: RenderPawnAt re-runs its own pre-render pass
                        // when the results are invalid, which they are for a culled thing.
                        t.DynamicDrawPhase(DrawPhase.EnsureInitialized);
                        t.DynamicDrawPhase(DrawPhase.Draw);
                    }
                    catch (Exception)
                    {
                        // One broken thing must not cost the whole feed.
                    }
                }
            }
            submitted.Clear();
        }

        // ------------------------------------------------------------------ resources

        private static void EnsureCam()
        {
            if (cam != null)
            {
                return;
            }
            var go = new GameObject("RadiusUI_MapCapture");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            cam = go.AddComponent<Camera>();
            cam.enabled = false;
        }

        private static RenderTexture? EnsureRt(Feed f)
        {
            int h = f.Resolution;
            int w = Mathf.Clamp(Mathf.RoundToInt(h * f.Aspect), 16, 1024);
            if (f.Rt != null && f.Rt.width == w && f.Rt.height == h && f.Rt.IsCreated())
            {
                return f.Rt;
            }
            FreeRt(f);
            var rt = new RenderTexture(w, h, 16)
            {
                name = "RadiusUI_MapCapture",
                antiAliasing = 1,
                // Bilinear + clamp: the feed is drawn at roughly its native size, and Point
                // sampling made the held frames between captures look harsher than the map.
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            rt.Create();
            f.Rt = rt;
            f.Ready = false;
            return rt;
        }

        private static void FreeRt(Feed f)
        {
            if (f.Rt != null)
            {
                f.Rt.Release();
                f.Rt = null;
            }
            f.Ready = false;
        }
    }

    /// <summary>
    /// Pumps <see cref="MapCapture"/> once per frame at the end of Map.MapUpdate - the only
    /// vanilla hook that sits after the map's draw submissions and before Unity renders,
    /// and it needs no Harmony patch. Holds no state and saves nothing.
    /// </summary>
    public class MapCaptureDriver : MapComponent
    {
        public MapCaptureDriver(Map map) : base(map)
        {
        }

        public override void MapComponentUpdate()
        {
            MapCapture.Pump(map);
        }
    }
}
