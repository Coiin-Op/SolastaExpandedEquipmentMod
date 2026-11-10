using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>
    /// Temporary, observational startup diagnostics for SEE.
    /// Does not alter registration or patch behavior.
    /// </summary>
    internal static class StartupDiagnostic
    {
        private static UnityModManager.ModEntry mod;
        private static readonly Stopwatch Clock = Stopwatch.StartNew();
        private static readonly object Sync = new object();
        private static readonly HashSet<string> LoggedExceptions =
            new HashSet<string>();
        private static int relevantExceptionCount;
        private const int MaxUniqueExceptions = 100;
        private static bool initialized;

        internal static void Initialize(UnityModManager.ModEntry entry)
        {
            if (initialized)
                return;

            initialized = true;
            mod = entry;

            AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;

            Log("DIAGNOSTIC STARTED. First-chance exception capture is active.");
            Log("Runtime=" + Environment.Version +
                " | 64BitProcess=" + Environment.Is64BitProcess +
                " | BaseDirectory=" + AppDomain.CurrentDomain.BaseDirectory);
            Checkpoint("SEE Load entered");
        }

        internal static void Checkpoint(string name)
        {
            Log("CHECKPOINT | " + name);
        }

        internal static void RunStep(string name, Action action)
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

        internal static void LogException(string area, Exception ex)
        {
            if (ex == null)
                return;

            Log("EXCEPTION | " + area + " | " + ex);
        }

        private static void OnFirstChanceException(
            object sender,
            FirstChanceExceptionEventArgs args)
        {
            Exception ex = args.Exception;
            if (ex == null)
                return;

            string details = ex.ToString();
            string searchable = details.ToLowerInvariant();

            // Keep the log focused on the startup failure family already seen.
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

            string key = ex.GetType().FullName + "|" + ex.Message + "|" +
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

            Log("FIRST-CHANCE EXCEPTION #" + relevantExceptionCount +
                " | " + details);
        }

        private static string FirstStackLine(string stack)
        {
            if (String.IsNullOrEmpty(stack))
                return String.Empty;

            string[] lines = stack.Split('\n');
            return lines.Length > 0 ? lines[0].Trim() : String.Empty;
        }

        private static void OnUnhandledException(
            object sender,
            UnhandledExceptionEventArgs args)
        {
            Exception ex = args.ExceptionObject as Exception;

            Log("UNHANDLED EXCEPTION | IsTerminating=" +
                args.IsTerminating + " | " +
                (ex != null ? ex.ToString() : Convert.ToString(args.ExceptionObject)));
        }

        private static void OnAssemblyLoad(
            object sender,
            AssemblyLoadEventArgs args)
        {
            try
            {
                Assembly assembly = args.LoadedAssembly;
                string name = assembly == null || assembly.GetName() == null
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
            catch (Exception ex)
            {
                // A diagnostic must never interfere with normal assembly loading.
                Log("ASSEMBLY-LOAD DIAGNOSTIC ERROR | " + ex.GetType().FullName);
            }
        }

        private static void Log(string message)
        {
            try
            {
                if (mod != null && mod.Logger != null)
                {
                    mod.Logger.Log(
                        "[SEE StartupDiag] +" +
                        Clock.Elapsed.TotalSeconds.ToString("F3") +
                        "s | " + message);
                }
            }
            catch
            {
                // Never allow logging failures to affect game startup.
            }
        }
    }
}
