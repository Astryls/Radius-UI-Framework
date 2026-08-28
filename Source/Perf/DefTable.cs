// Radius UI Framework - Perf/DefTable.cs
//
// Def-keyed lookup as a dense array indexed by Def.index, per PERFORMANCE_PLAYBOOK B3.
//
// WHY NOT A DICTIONARY. Every Def carries a dense, contiguous `index`, so a T[] indexed by
// it is the fastest possible def-keyed map: no hashing, no comparer, no boxing, one bounds
// check and one array read. A Dictionary<TDef, T> hashes a reference and calls a comparer
// on every probe, and on Mono a struct value type in that dictionary can box as well.
//
// This is a UI framework, so the intended use is memoizing something EXPENSIVE that a draw
// path needs per def: resolved icons, formatted labels, colour classifications. It is not
// a place to cache game state.
//
// SIZING. DefDatabase<T>.DefCount is final after load, so build at
// [StaticConstructorOnStartup] and the array is exactly the right size forever. The table
// grows defensively anyway, because a def added later (a patch operation, a runtime-generated
// def) would otherwise index out of bounds - a crash in a draw loop is far worse than a
// resize nobody notices.

using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Dense <c>Def.index</c>-keyed table. O(1) with no hashing, no comparer and no boxing.
    /// <para>Thread affinity: build on the main thread at startup; reads are safe anywhere.
    /// Cost: one bounds check plus one array read per lookup.</para>
    /// </summary>
    /// <typeparam name="TDef">The def type whose <c>index</c> keys this table.</typeparam>
    /// <typeparam name="T">Value stored per def.</typeparam>
    public sealed class DefTable<TDef, T> where TDef : Def
    {
        private T[] values;
        private bool[] present;

        /// <summary>
        /// Build sized to the current def count, plus a little slack so a late-registered def
        /// does not force an immediate resize.
        /// </summary>
        public DefTable(int slack = 8)
        {
            int n = 0;
            try { n = DefDatabase<TDef>.DefCount; } catch { n = 0; }
            int cap = Mathf.Max(n + slack, 16);
            values = new T[cap];
            present = new bool[cap];
        }

        /// <summary>Number of slots currently allocated (not the number of entries set).</summary>
        public int Capacity => values.Length;

        /// <summary>Value for a def, or <c>default</c> when nothing has been stored.</summary>
        public T this[TDef def]
        {
            get => TryGet(def, out T v) ? v : default!;
            set => Set(def, value);
        }

        /// <summary>True when a value has been stored for this def. Distinguishes "cached a
        /// null/default answer" from "never computed", which a bare null check cannot.</summary>
        public bool Has(TDef? def) =>
            def != null && def.index >= 0 && def.index < present.Length && present[def.index];

        /// <summary>Read a stored value. Returns false when nothing was stored.</summary>
        public bool TryGet(TDef? def, out T value)
        {
            if (def != null)
            {
                int i = def.index;
                if (i >= 0 && i < present.Length && present[i]) { value = values[i]; return true; }
            }
            value = default!;
            return false;
        }

        /// <summary>Store a value, growing the table if a def indexes past the end.</summary>
        public void Set(TDef? def, T value)
        {
            if (def == null) return;
            int i = def.index;
            if (i < 0) return;
            if (i >= values.Length) Grow(i + 1);
            values[i] = value;
            present[i] = true;
        }

        /// <summary>
        /// Memoize: return the stored value, computing and storing it on first ask.
        /// The normal way to use this table from a draw path.
        /// </summary>
        public T GetOrCompute(TDef? def, System.Func<TDef, T> compute)
        {
            if (def == null) return default!;
            if (TryGet(def, out T existing)) return existing;
            T computed = compute(def);
            Set(def, computed);
            return computed;
        }

        /// <summary>Drop every entry, keeping the allocation. For a new game or a settings change.</summary>
        public void Clear()
        {
            System.Array.Clear(values, 0, values.Length);
            System.Array.Clear(present, 0, present.Length);
        }

        private void Grow(int needed)
        {
            int cap = values.Length;
            while (cap < needed) cap *= 2;
            System.Array.Resize(ref values, cap);
            System.Array.Resize(ref present, cap);
        }
    }
}
