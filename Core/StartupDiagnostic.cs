
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>
    /// Observational startup diagnostics for SEE.
    /// Must never interfere with normal startup.
    /// </summary>
    internal static class StartupDiagnostic
    {
        private static UnityModManager.ModEntry mod;
        private static readonly Stopwatch Clock = Stopwatch.StartNew();
        private static readonly object Sync = new object();

        private static readonly HashSet<string> LoggedExceptions =
            new HashSet<string>();

        private const int MaxUniqueExceptions = 100;

        private static int relevantExceptionCount;
        private static bool initialized;

        internal static void Initialize(
            UnityModManager.ModEntry entry)
        {
            if (initialized)
                return;

            initialized = true;
            mod = entry;

            AppDomain.CurrentDomain.FirstChanceException +=
                OnFirstChanceException;

            AppDomain.CurrentDomain.UnhandledException +=
                OnUnhandledException;

            AppDomain.CurrentDomain.AssemblyLoad +=
                OnAssemblyLoad;

            Log("DIAGNOSTIC STARTED.");
            Log("First-chance exception capture is active.");

            Log("Runtime=" + Environment.Version +
                " | 64BitProcess=" + Environment.Is64BitProcess +
                " | BaseDirectory=" +
                AppDomain.CurrentDomain.BaseDirectory);

            Checkpoint("SEE Load entered");
        }

        internal static void Checkpoint(string name)
        {
            Log("CHECKPOINT | " + name);
        }

        internal static void RunStep(
            string name,
            Action action)
        {
            Checkpoint("BEGIN | " + name);

            try
            {
                action();
                Checkpoint("SUCCESS | " + name);
            }
            catch (Exception ex)
            {
                Log("STEP FAILED | " + name + " | " + ex);
                throw;
            }
        }

        internal static void LogException(
            string area,
            Exception ex)
        {
            if (ex == null)
                return;

            Log("EXCEPTION | " + area + " | " + ex);
        }

        private static void OnFirstChanceException(
            object sender,
            FirstChanceExceptionEventArgs args)
        {
            try
            {
                Exception ex = args.Exception;

                if (ex == null)
                    return;

                string details = ex.ToString();
                string searchable = details.ToLowerInvariant();

                bool relevant =
                    searchable.Contains("databasehelper") ||
                    searchable.Contains("conditiondefinitions") ||
                    searchable.Contains("fixescontext") ||
                    searchable.Contains("bootcontext") ||
                    searchable.Contains("asyncloaddatabases") ||
                    searchable.Contains("solastaunfinishedbusiness") ||
                    ex is TypeInitializationException ||
                    ex is ReflectionTypeLoadException ||
                    ex is MissingMethodException ||
                    ex is TypeLoadException;

                if (!relevant)
                    return;

                string key =
                    ex.GetType().FullName + "|" +
                    ex.Message + "|" +
                    FirstStackLine(ex.StackTrace);

                lock (Sync)
                {
                    if (LoggedExceptions.Contains(key))
                        return;

                    if (relevantExceptionCount >= MaxUniqueExceptions)
                        return;

                    LoggedExceptions.Add(key);
                    relevantExceptionCount++;
                }

                Log("FIRST-CHANCE EXCEPTION #" +
                    relevantExceptionCount + " | " + details);
            }
            catch
            {
                // Diagnostics must never disrupt exception handling.
            }
        }

        private static string FirstStackLine(string stack)
        {
            if (String.IsNullOrEmpty(stack))
                return String.Empty;

            string[] lines = stack.Split('\n');

            return lines.Length > 0
                ? lines[0].Trim()
                : String.Empty;
        }

        private static void OnUnhandledException(
            object sender,
            UnhandledExceptionEventArgs args)
        {
            try
            {
                Exception ex = args.ExceptionObject as Exception;

                Log("UNHANDLED EXCEPTION | IsTerminating=" +
                    args.IsTerminating + " | " +
                    (ex != null
                        ? ex.ToString()
                        : Convert.ToString(args.ExceptionObject)));
            }
            catch
            {
                // Never interfere with shutdown.
            }
        }

        private static void OnAssemblyLoad(
            object sender,
            AssemblyLoadEventArgs args)
        {
            try
            {
                Assembly assembly = args.LoadedAssembly;

                string name =
                    assembly == null || assembly.GetName() == null
                        ? "<unknown>"
                        : assembly.GetName().Name;

                if (String.IsNullOrEmpty(name))
                    return;

                string lower = name.ToLowerInvariant();

                if (lower.Contains("solasta") ||
                    lower.Contains("unfinishedbusiness") ||
                    lower.Contains("harmony") ||
                    lower.Contains("unitymodmanager"))
                {
                    Log("ASSEMBLY LOAD | " + name);
                }
            }
            catch
            {
                // Assembly diagnostics must never affect assembly loading.
            }
        }

        private static void Log(string message)
        {
            try
            {
                if (mod == null || mod.Logger == null)
                    return;

                mod.Logger.Log(
                    "[SEE StartupDiag] +" +
                    Clock.Elapsed.TotalSeconds.ToString("F3") +
                    "s | " + message);
            }
            catch
            {
                // Logging failures must never affect game startup.
            }
        }
    }
}
