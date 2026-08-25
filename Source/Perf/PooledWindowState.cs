using System;
using System.Collections.Generic;
using UnityEngine;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Base for a card's per-window draw state. Exists to kill the per-frame closure
    /// allocation at <c>Find.WindowStack.ImmediateWindow(...)</c> call sites.
    ///
    /// THE PROBLEM: passing a lambda that captures locals allocates a compiler display class
    /// AND a delegate on every call. HUD has 14 such call sites; with ~12 alert cards, ~4
    /// docked messages and a hover pane on screen that measured ~34 allocations per frame,
    /// ~2000/second - steady garbage on a non-generational Boehm heap, which is exactly how an
    /// OnGUI mod's frame cost creeps up over a long session.
    ///
    /// THE CONSTRAINT THAT KILLS THE OBVIOUS FIX: <c>ImmediateWindow</c> does NOT invoke the
    /// delegate where you call it. It stores the delegate on a Window and WindowStack invokes
    /// it LATER in the frame. So a single shared static state object plus one cached delegate
    /// would make every card render the LAST card's content. State must be per-window-id.
    ///
    /// THE FIX: one state object per window id, each owning a delegate bound once in its
    /// constructor to its own <see cref="Body"/>. Per frame you mutate the pooled object's
    /// fields in place and hand ImmediateWindow the already-allocated delegate. Steady state
    /// is zero allocations and byte-identical output.
    ///
    /// <code>
    ///   sealed class CardState : PooledWindowState {
    ///       public string? label;
    ///       protected override void Body() { RadiusFont.Label(new Rect(...), label); }
    ///   }
    ///   static readonly WindowStatePool&lt;CardState&gt; pool = new WindowStatePool&lt;CardState&gt;();
    ///   var st = pool.Get(windowId);
    ///   st.label = text;                                   // mutate in place
    ///   Find.WindowStack.ImmediateWindow(windowId, rect, layer, st.Draw);   // cached delegate
    /// </code>
    ///
    /// Thread affinity: OnGUI main thread only.
    /// </summary>
    public abstract class PooledWindowState
    {
        /// <summary>
        /// The delegate handed to <c>ImmediateWindow</c>. Allocated ONCE here, in the
        /// constructor - never per frame. This field being pre-bound is the whole point of
        /// the class.
        /// </summary>
        public readonly Action Draw;

        protected PooledWindowState()
        {
            Draw = Body;
        }

        /// <summary>
        /// Draw this window's contents, reading the fields the caller mutated this frame.
        /// Runs later in the frame than the call that scheduled it, so do not assume any
        /// ambient GUI state set up at schedule time still holds.
        /// </summary>
        protected abstract void Body();
    }

    /// <summary>
    /// Pool of <typeparamref name="T"/> keyed by window id. One live instance per id, so two
    /// concurrently-open windows never share state.
    ///
    /// Thread affinity: OnGUI main thread only. Not thread safe.
    /// </summary>
    public sealed class WindowStatePool<T> where T : PooledWindowState, new()
    {
        private readonly Dictionary<int, T> byId;

        public WindowStatePool(int capacity = 16)
        {
            byId = new Dictionary<int, T>(capacity);
        }

        /// <summary>
        /// The state object for <paramref name="windowId"/>, created on first use. Use the
        /// SAME id you pass to <c>ImmediateWindow</c>, or two windows will fight over one
        /// state object and render each other's content.
        /// </summary>
        public T Get(int windowId)
        {
            if (!byId.TryGetValue(windowId, out T? s))
            {
                s = new T();
                byId[windowId] = s;
            }
            return s;
        }

        /// <summary>Live state count (diagnostics only).</summary>
        public int Count => byId.Count;

        /// <summary>Drop every pooled state (map change, settings rebuild).</summary>
        public void Clear()
        {
            byId.Clear();
        }
    }
}
