//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System.Reflection;
//using System.Text;
//using UnityEngine;

//namespace SolastaExpandedEquipmentMod
//{
//    /// <summary>
//    /// One-shot diagnostic export of ItemDefinition entries loaded in the game.
//    /// </summary>
//    public static class ItemDatabaseExporter
//    {
//        private static bool attempted;

//        public static void ExportOnce(
//            Database<ItemDefinition> itemDb,
//            UnityModManagerNet.UnityModManager.ModEntry mod)
//        {
//            if (attempted || itemDb == null)
//                return;

//            attempted = true;

//            try
//            {
//                List<ItemDefinition> items = new List<ItemDefinition>();
//                HashSet<string> itemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//                Type dbType = itemDb.GetType();
//                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

//                // Prefer database enumeration methods where available.
//                foreach (MethodInfo method in dbType.GetMethods(flags))
//                {
//                    if (method.GetParameters().Length != 0 ||
//                        !typeof(IEnumerable).IsAssignableFrom(method.ReturnType) ||
//                        method.Name.IndexOf("Element", StringComparison.OrdinalIgnoreCase) < 0)
//                        continue;

//                    try
//                    {
//                        CollectItems(method.Invoke(itemDb, null), items, itemNames);
//                    }
//                    catch
//                    {
//                        // Continue with other enumeration surfaces.
//                    }
//                }

//                // Fall back to enumerable database fields/properties for builds without a public enumerator.
//                foreach (FieldInfo field in dbType.GetFields(flags))
//                {
//                    if (!typeof(IEnumerable).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(string))
//                        continue;
//                    try { CollectItems(field.GetValue(itemDb), items, itemNames); }
//                    catch { }
//                }

//                foreach (PropertyInfo property in dbType.GetProperties(flags))
//                {
//                    MethodInfo getter = property.GetGetMethod(true);
//                    if (property.GetIndexParameters().Length != 0 || getter == null ||
//                        !typeof(IEnumerable).IsAssignableFrom(property.PropertyType) ||
//                        property.PropertyType == typeof(string))
//                        continue;
//                    try { CollectItems(getter.Invoke(itemDb, null), items, itemNames); }
//                    catch { }
//                }

//                if (items.Count == 0)
//                    throw new InvalidOperationException("Could not enumerate ItemDefinition entries from the game database.");

//                items.Sort(delegate (ItemDefinition left, ItemDefinition right)
//                {
//                    return String.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
//                });

//                StringBuilder output = new StringBuilder();
//                output.AppendLine("InternalName\tDisplayName\tIsUserContent");
//                foreach (ItemDefinition item in items)
//                {
//                    output.Append(Clean(item.Name));
//                    output.Append('\t');
//                    output.Append(item.GuiPresentation == null ? "" : Clean(item.GuiPresentation.Title));
//                    output.Append('\t');
//                    output.AppendLine(item.IsUserContent ? "true" : "false");
//                }

//                string exportPath = Path.Combine(Application.persistentDataPath, "Solasta_ItemDatabase_Export.tsv");
//                Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
//                File.WriteAllText(exportPath, output.ToString(), Encoding.UTF8);

//                if (mod != null)
//                    mod.Logger.Log("[SolastaExpandedEquipmentMod] Exported " + items.Count + " item definitions to " + exportPath);
//            }
//            catch (Exception ex)
//            {
//                if (mod != null)
//                    mod.Logger.Error("[SolastaExpandedEquipmentMod] Item database export failed: " + ex);
//            }
//        }

//        private static void CollectItems(object source, List<ItemDefinition> items, HashSet<string> itemNames)
//        {
//            IEnumerable enumerable = source as IEnumerable;
//            if (enumerable == null)
//                return;

//            foreach (object entry in enumerable)
//            {
//                ItemDefinition item = entry as ItemDefinition;
//                if (item == null && entry != null)
//                {
//                    PropertyInfo valueProperty = entry.GetType().GetProperty(
//                        "Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                    if (valueProperty != null && valueProperty.GetIndexParameters().Length == 0)
//                    {
//                        try { item = valueProperty.GetValue(entry, null) as ItemDefinition; }
//                        catch { item = null; }
//                    }
//                }

//                if (item != null && !String.IsNullOrEmpty(item.Name) && itemNames.Add(item.Name))
//                    items.Add(item);
//            }
//        }

//        private static string Clean(string value)
//        {
//            if (value == null)
//                return "";
//            return value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
//        }
//    }
//}
