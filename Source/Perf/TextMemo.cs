using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Memoized text measurement. <c>Text.CalcSize</c> / <c>Text.CalcHeight</c> are the
    /// dominant IMGUI CPU cost in suite UI - the two per-frame drawers call them 36 and 15
    /// times respectively per file sweep - and the results are pure functions of
    /// (string, font, weight, size, wrap width).
    ///
    /// BOTH suite drawers had independently built a cache for this; this is the merge of the
    /// better half of each:
    ///  - EVICTION from Colonist Bar: a generational two-dictionary flip. Capping one
    ///    dictionary and Clear()ing it (HUD's approach) throws away every hot entry at once
    ///    and produces a periodic full-recompute spike. The flip keeps the previous generation
    ///    alive as a fallback read, so a hot string is PROMOTED on its next use instead of
    ///    being recomputed for everyone on the same frame.
    ///  - COVERAGE from HUD: fit/truncation results are memoized too, not just raw metrics.
    ///    Ellipsis fitting is a binary search over CalcSize and is far more expensive than a
    ///    single measure.
    ///
    /// THE KEY MUST INCLUDE FONT, WEIGHT AND SIZE, not just the string and width: a font mod
    /// or a language switch changes sizes at runtime, and a width-only memo then serves stale
    /// metrics forever with no error. The key is a readonly struct implementing
    /// <see cref="IEquatable{T}"/> so Mono never boxes on lookup (a plain struct key without
    /// IEquatable boxes twice per dictionary probe on this runtime).
    ///
    /// Thread affinity: OnGUI main thread only. Not thread safe.
    /// </summary>
    public static class TextMemo
    {
        /// <summary>Entries per generation before the flip. Two generations may be live.</summary>
        private const int Cap = 1024;

        private readonly struct Key : IEquatable<Key>
        {
            private readonly string text;
            private readonly int font;      // GameFont
            private readonly int flags;     // bit0 bold, bit1 italic
            private readonly float wrapW;   // -1 = single-line measure

            public Key(string? text, int font, bool bold, bool italic, float wrapW)
            {
                this.text = text ?? string.Empty;
                this.font = font;
                this.flags = (bold ? 1 : 0) | (italic ? 2 : 0);
                this.wrapW = wrapW;
            }

            public bool Equals(Key o)
            {
                return font == o.font
                    && flags == o.flags
                    && wrapW.Equals(o.wrapW)
                    && string.Equals(text, o.text, StringComparison.Ordinal);
            }

            public override bool Equals(object? o)
            {
                return o is Key k && Equals(k);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = text.GetHashCode();
                    h = (h * 397) ^ font;
                    h = (h * 397) ^ flags;
                    h = (h * 397) ^ wrapW.GetHashCode();
                    return h;
                }
            }
        }

        // ---- metric store (Vector2: single-line size, or (height, 0) for wrapped) ----
        private static Dictionary<Key, Vector2> metricCur = new Dictionary<Key, Vector2>(256);
        private static Dictionary<Key, Vector2> metricOld = new Dictionary<Key, Vector2>(256);

        // ---- fit/truncation store ----
        private static Dictionary<Key, string> fitCur = new Dictionary<Key, string>(128);
        private static Dictionary<Key, string> fitOld = new Dictionary<Key, string>(128);

        private static bool TryGet<T>(Dictionary<Key, T> cur, Dictionary<Key, T> old, Key k, out T v)
        {
            if (cur.TryGetValue(k, out v))
            {
                return true;
            }
            if (old.TryGetValue(k, out v))
            {
                cur[k] = v;   // promote into the live generation
                return true;
            }
            return false;
        }

        private static void Flip<T>(ref Dictionary<Key, T> cur, ref Dictionary<Key, T> old)
        {
            Dictionary<Key, T> t = old;
            old = cur;
            cur = t;
            cur.Clear();
        }

        /// <summary>
        /// Single-line size lookup. Returns false when the caller must measure and then call
        /// <see cref="PutSize"/>. Cost on hit: one struct hash and one dictionary probe.
        /// </summary>
        public static bool TryGetSize(string? text, GameFont font, bool bold, bool italic, out Vector2 size)
        {
            return TryGet(metricCur, metricOld, new Key(text, (int)font, bold, italic, -1f), out size);
        }

        /// <summary>Store a measured single-line size.</summary>
        public static void PutSize(string? text, GameFont font, bool bold, bool italic, Vector2 size)
        {
            if (metricCur.Count >= Cap)
            {
                Flip(ref metricCur, ref metricOld);
            }
            metricCur[new Key(text, (int)font, bold, italic, -1f)] = size;
        }

        /// <summary>
        /// Wrapped-height lookup at a given wrap width. Returns false when the caller must
        /// measure and then call <see cref="PutHeight"/>.
        /// </summary>
        public static bool TryGetHeight(string? text, GameFont font, bool bold, bool italic,
                                        float width, out float height)
        {
            if (TryGet(metricCur, metricOld, new Key(text, (int)font, bold, italic, width), out Vector2 v))
            {
                height = v.x;
                return true;
            }
            height = 0f;
            return false;
        }

        /// <summary>Store a measured wrapped height.</summary>
        public static void PutHeight(string? text, GameFont font, bool bold, bool italic,
                                     float width, float height)
        {
            if (metricCur.Count >= Cap)
            {
                Flip(ref metricCur, ref metricOld);
            }
            metricCur[new Key(text, (int)font, bold, italic, width)] = new Vector2(height, 0f);
        }

        /// <summary>
        /// Ellipsis-fit lookup: the truncated string that fits <paramref name="maxWidth"/>.
        /// Returns false when the caller must compute and then call <see cref="PutFit"/>.
        /// </summary>
        public static bool TryGetFit(string? text, GameFont font, bool bold, bool italic,
                                     float maxWidth, out string? fitted)
        {
            return TryGet(fitCur, fitOld, new Key(text, (int)font, bold, italic, maxWidth), out fitted);
        }

        /// <summary>Store a computed ellipsis-fit result.</summary>
        public static void PutFit(string? text, GameFont font, bool bold, bool italic,
                                  float maxWidth, string fitted)
        {
            if (fitCur.Count >= Cap)
            {
                Flip(ref fitCur, ref fitOld);
            }
            fitCur[new Key(text, (int)font, bold, italic, maxWidth)] = fitted;
        }

        /// <summary>
        /// Drop every cached metric. Call when the font face itself changes (language switch,
        /// a font mod rebuilding Verse's styles) - stale metrics are otherwise invisible until
        /// a layout visibly drifts. RadiusFont's epoch guard calls this.
        /// </summary>
        public static void InvalidateAll()
        {
            metricCur.Clear();
            metricOld.Clear();
            fitCur.Clear();
            fitOld.Clear();
        }

        /// <summary>Live entry count across both generations (diagnostics only).</summary>
        public static int Count => metricCur.Count + metricOld.Count + fitCur.Count + fitOld.Count;
    }
}
