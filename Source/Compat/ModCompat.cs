// Radius UI Framework - Compat/ModCompat.cs
//
// Soft-dependency binding, resolved ONCE at startup into cached bools and bound delegates.
//
// WHY THIS IS IN THE FRAMEWORK. Three consumers had already hand-rolled it (Health Tab binds
// 13 foreign mods, Colonist Bar and Controls & Alerts several each), which is well past
// GLOBAL_RULES §9's "third occurrence becomes a shared helper". More importantly they had
// each re-derived the same two hazards:
//
//   1. PERFORMANCE_PLAYBOOK B6: never ask ModsConfig.IsActive per call. It walks the mod
//      list and compares strings; from a draw loop that is pure waste. Resolve once, cache
//      a bool.
//   2. Reflection per call is worse still. A MethodInfo.Invoke allocates an object[] for the
//      arguments and boxes every value type in it. Bind once to a delegate and the call site
//      costs the same as an interface call.
//
// WHAT THIS DELIBERATELY DOES NOT DO. It does not reference a foreign assembly, declare a
// dependency, or ship anyone else's DLL. Everything is by name, at runtime, and every failure
// degrades to "that mod is not present" rather than throwing. A consumer must still VERIFY
// the foreign API by decompiling the installed mod - a signature typed from memory compiles
// and then silently binds nothing.

using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Startup-resolved soft-dependency registry. All lookups are cached; a miss is cached
    /// too, so a missing mod costs one failed probe for the process, not one per frame.
    /// Thread affinity: resolve from a <c>[StaticConstructorOnStartup]</c>; read anywhere.
    /// </summary>
    public static class ModCompat
    {
        private static readonly Dictionary<string, bool> activeById =
            new Dictionary<string, bool>(16, StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Type?> typeByName =
            new Dictionary<string, Type?>(32, StringComparer.Ordinal);

        /// <summary>
        /// Is a mod with this packageId loaded? Cached after the first call.
        /// <para>Cost: one dictionary hit after the first call. Safe in a draw loop, unlike
        /// <c>ModsConfig.IsActive</c>.</para>
        /// </summary>
        public static bool IsActive(string packageId)
        {
            if (string.IsNullOrEmpty(packageId)) return false;
            if (activeById.TryGetValue(packageId, out bool cached)) return cached;

            bool found = false;
            try
            {
                foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading)
                {
                    // ⚠ BOTH id forms, deliberately. A Steam-subscribed copy's EFFECTIVE id is
                    // "<id>_steam" (ModMetaData.SteamModPostfix), and pack.PackageId returns that
                    // suffixed form - so comparing it alone reports every Workshop copy as absent
                    // while a local copy of the same mod reports present. That asymmetry silently
                    // disabled 6 of 11 soft-dep bridges in this suite once; the player-facing id
                    // is the stable one, and the raw form is kept for callers who pass a suffixed
                    // id on purpose.
                    if (string.Equals(pack.PackageIdPlayerFacing, packageId, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(pack.PackageId, packageId, StringComparison.OrdinalIgnoreCase))
                    { found = true; break; }
                }
            }
            catch { found = false; }

            activeById[packageId] = found;
            return found;
        }

        /// <summary>
        /// Resolve a type by full name across every loaded assembly, or null. Cached, misses
        /// included.
        /// <para>Note the assembly name is usually NOT the packageId - bind on the type's
        /// namespace, which is stable, rather than on a mod folder or display name.</para>
        /// </summary>
        public static Type? FindType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            if (typeByName.TryGetValue(fullName, out Type? cached)) return cached;

            Type? found = null;
            // Verse's own resolver, which walks every loaded assembly.
            try { found = GenTypes.GetTypeInAnyAssembly(fullName); }
            catch { found = null; }

            typeByName[fullName] = found;
            return found;
        }

        /// <summary>True when <paramref name="fullName"/> resolves to a real type.</summary>
        public static bool HasType(string fullName) => FindType(fullName) != null;

        /// <summary>
        /// Bind a static method to a delegate once, or return null. The returned delegate
        /// costs an ordinary call; <c>MethodInfo.Invoke</c> would allocate an argument array
        /// and box every value type on every call.
        /// <para>Returns null when the type, the method, or the signature does not match -
        /// which is the normal path when the foreign mod is absent.</para>
        /// </summary>
        public static TDelegate? BindStatic<TDelegate>(string typeName, string methodName,
            params Type[] argTypes) where TDelegate : Delegate
        {
            Type? t = FindType(typeName);
            if (t == null) return null;
            try
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                MethodInfo? m = argTypes == null || argTypes.Length == 0
                    ? t.GetMethod(methodName, flags)
                    : t.GetMethod(methodName, flags, null, argTypes, null);
                if (m == null || !m.IsStatic) return null;
                return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), m);
            }
            catch (Exception e)
            {
                // A signature mismatch is a MODDING error worth seeing once - it means the
                // foreign API moved and this binding is now dead code that silently no-ops.
                Log.WarningOnce("[Radius UI] ModCompat could not bind " + typeName + "." + methodName
                                + ": " + e.Message, (typeName + methodName).GetHashCode());
                return null;
            }
        }

        /// <summary>
        /// Read a static field or property once. For capability probes and constants; do not
        /// call per frame, cache what it returns.
        /// <para>⚠ For <c>T = string</c>, a boxed struct result is COERCED via ToString(). This
        /// exists because reflected RimWorld getters very often return <c>TaggedString</c> - a
        /// struct - and <c>boxed as string</c> yields null silently, which reads as "the mod has
        /// no data" while the mod is answering fine. (The suite shipped exactly that bug once:
        /// an orientation row reading Unknown for every pawn.)</para>
        /// </summary>
        public static T? StaticValue<T>(string typeName, string memberName) where T : class
        {
            Type? t = FindType(typeName);
            if (t == null) return null;
            try
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                object? raw;
                FieldInfo? f = t.GetField(memberName, flags);
                if (f != null) raw = f.GetValue(null);
                else
                {
                    PropertyInfo? p = t.GetProperty(memberName, flags);
                    if (p == null) return null;
                    raw = p.GetValue(null, null);
                }
                if (raw is T typed) return typed;
                if (raw != null && typeof(T) == typeof(string)) return (T)(object)raw.ToString();
                return null;
            }
            catch { return null; }
        }

        /// <summary>Diagnostic: every packageId probed so far and whether it resolved.</summary>
        public static string Report()
        {
            System.Text.StringBuilder sb = ScratchText.Sb();
            sb.Append("[Radius UI] ModCompat: ").Append(activeById.Count).Append(" mods probed, ")
              .Append(typeByName.Count).Append(" types probed");
            foreach (KeyValuePair<string, bool> kv in activeById)
            {
                sb.AppendLine().Append("  ").Append(kv.Value ? "present " : "absent  ").Append(kv.Key);
            }
            return sb.ToString();
        }
    }
}
