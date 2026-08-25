using System.Runtime.CompilerServices;
using UnityEngine;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Per-frame gating for OnGUI code. Extracted from the two suite mods that are true
    /// per-frame drawers and had independently hand-rolled these idioms: HUD (12 open-coded
    /// <c>Time.frameCount</c> compares) and Colonist Bar (55).
    ///
    /// Thread affinity: OnGUI main thread only. Deliberately NOT thread safe - RimWorld's UI
    /// is single-threaded, so a lock would be pure overhead. Never call from a worker thread
    /// (e.g. a <c>PawnRenderTree.ParallelPreDraw</c> postfix).
    /// </summary>
    public static class FrameGate
    {
        /// <summary>
        /// Once-per-frame gate. Returns true exactly once per Unity frame for the given
        /// <paramref name="stamp"/> field (and stamps it), false on every later call in the
        /// same frame. Collapses both hot idioms into one call:
        /// <code>
        ///   if (FrameGate.NewFrame(ref _f)) { /* do work once this frame */ }
        ///   if (!FrameGate.NewFrame(ref _f)) return _cached;   // compute-once, return cached
        /// </code>
        /// Semantically identical to the open-coded <c>Time.frameCount</c> compares it
        /// replaces: a stamp sitting at its default value still fires on any frame whose count
        /// differs from that default, so existing field initialisers keep working when a
        /// consumer converts to this.
        ///
        /// Cost: one static read, one compare, one store.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NewFrame(ref int stamp)
        {
            int f = Time.frameCount;
            if (stamp == f)
            {
                return false;
            }
            stamp = f;
            return true;
        }

        /// <summary>
        /// True only on the pass that actually produces pixels.
        ///
        /// THE POINT: OnGUI runs for EVERY event pass (~1.9 per frame - the Repaint plus
        /// whichever mouse/key pass arrived), but only Repaint draws anything. Labels, plates,
        /// dividers and textures consume NO IMGUI control id - only Button/ButtonInvisible/
        /// TextField/scroll views do - so pure drawing can be skipped on the other passes with
        /// no risk to control-count stability, which is the one invariant IMGUI cannot break.
        ///
        /// Guard the DRAW, never the LAYOUT and never a control: rect math must still run on
        /// every pass so controls land in the same places, or IMGUI mismatches its control ids
        /// and input silently goes to the wrong widget.
        ///
        /// Null-safe: returns false outside an OnGUI frame rather than throwing.
        /// </summary>
        public static bool Drawing
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Event? e = Event.current;
                return e != null && e.type == EventType.Repaint;
            }
        }

        /// <summary>
        /// True on the layout pass. Rarely needed directly - prefer running layout
        /// unconditionally and gating only the draw with <see cref="Drawing"/>.
        /// </summary>
        public static bool LayoutPass
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Event? e = Event.current;
                return e != null && e.type == EventType.Layout;
            }
        }
    }
}
