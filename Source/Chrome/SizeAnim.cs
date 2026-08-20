// Radius UI Framework - Chrome/SizeAnim.cs
//
// ADDED generation 6 (2026-08-20). PURELY ADDITIVE: a new type. Nothing existing is touched.
//
// WHY THIS EXISTS.
// Third occurrence, and it is visual, so GLOBAL_RULES §9 puts it here rather than in a consumer:
//   1. Radius UI - Inspector animates its pane between CompactHeight and FullHeight.
//   2. Radius UI - HUD animates its cards folding.
//   3. Radius UI - Gizmos animates a drawer that collapses in BOTH axes, down to a nub.
//
// THE TRAP IT EXISTS TO CLOSE (this cost a session in the Inspector).
// A size/layout getter is evaluated SEVERAL TIMES PER FRAME - MainTabWindow_Inspect compares the
// requested size against its last one, then SetInitialSizeAndPosition asks again. If the animation
// advances on every call, two calls in one frame disagree, and if a "settled" flag is latched by
// one call while the value is driven by another, the animation can never converge: settled never
// becomes true and anything gated on it never draws. The symptom is a blank panel with NO
// exception in the log.
//
// So: Advance() moves the value AT MOST ONCE PER FRAME, via FrameGate, and every later read in the
// same frame returns the same number. Reading Current is idempotent within a frame by construction.
//
// The clock is REALTIME, not game time, so a panel still folds and unfolds while the game is
// paused - which is exactly when a player is reading a UI.
//
// Thread affinity: OnGUI main thread only.

using UnityEngine;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Frame-rate independent exponential ease toward a target size, advanced at most once per
    /// frame. Hold one per animated panel; it owns no global state.
    ///
    /// <para>Exponential smoothing rather than a fixed-duration tween because the target can change
    /// mid-flight (a drawer told to open while it is still collapsing). A duration tween has to
    /// restart, which visibly stutters; smoothing simply re-aims.</para>
    /// </summary>
    public sealed class SizeAnim
    {
        private Vector2 current;
        private Vector2 target;
        private float lastTime;
        private int frameStamp = -1;
        private readonly float tau;

        /// <summary>
        /// </summary>
        /// <param name="width">Starting width.</param>
        /// <param name="height">Starting height.</param>
        /// <param name="seconds">
        /// Time to close ~95% of the gap. 0.22 matches the suite's panel easing; below ~0.08 the
        /// motion reads as a jump and you may as well call <see cref="SnapTo"/>.
        /// </param>
        public SizeAnim(float width, float height, float seconds = 0.22f)
        {
            current = new Vector2(width, height);
            target = current;
            tau = Mathf.Max(0.02f, seconds) / 3f;   // 3 tau ~ 95% of the gap
            lastTime = Time.realtimeSinceStartup;
        }

        /// <summary>Aim at a new size. Cheap and idempotent; safe to call every pass.</summary>
        public void Target(float width, float height)
        {
            target = new Vector2(width, height);
        }

        /// <summary>Jump to a size with no motion. Use on first show and on selection changes.</summary>
        public void SnapTo(float width, float height)
        {
            current = new Vector2(width, height);
            target = current;
            lastTime = Time.realtimeSinceStartup;
        }

        /// <summary>The size to draw at. Advances the animation once per frame, then holds.</summary>
        public Vector2 Current
        {
            get
            {
                Advance();
                return current;
            }
        }

        /// <summary>Convenience accessors; both advance the animation exactly as <see cref="Current"/> does.</summary>
        public float Width => Current.x;

        /// <inheritdoc cref="Width"/>
        public float Height => Current.y;

        /// <summary>
        /// True once the value is within half a pixel of its target in both axes. Read AFTER
        /// <see cref="Current"/> in the same frame, or gate on it directly - it advances too.
        ///
        /// <para>Callers that hide content until settled should ALSO cap the wait in frames. An
        /// empty panel is worse than one early paint, and a cap turns a hang into a blink.</para>
        /// </summary>
        public bool Settled
        {
            get
            {
                Advance();
                return Mathf.Abs(current.x - target.x) < 0.5f && Mathf.Abs(current.y - target.y) < 0.5f;
            }
        }

        /// <summary>The size being aimed at, without advancing anything.</summary>
        public Vector2 TargetSize => target;

        private void Advance()
        {
            // AT MOST ONCE PER FRAME. Every later read this frame sees the same value, so a layout
            // getter called three times in one frame cannot disagree with itself.
            if (!FrameGate.NewFrame(ref frameStamp)) return;

            float now = Time.realtimeSinceStartup;
            float dt = now - lastTime;
            lastTime = now;

            // Guard the pathological deltas: a long event (map generation, save load) can leave a
            // multi-second gap, and an unclamped step would make the panel appear to teleport.
            if (dt <= 0f) return;
            if (dt > 0.25f) dt = 0.25f;

            float k = 1f - Mathf.Exp(-dt / tau);
            current.x += (target.x - current.x) * k;
            current.y += (target.y - current.y) * k;

            // Snap the last half pixel so Settled can actually become true and so a rounded plate
            // does not shimmer on a sub-pixel residue forever.
            if (Mathf.Abs(current.x - target.x) < 0.5f) current.x = target.x;
            if (Mathf.Abs(current.y - target.y) < 0.5f) current.y = target.y;
        }
    }
}
