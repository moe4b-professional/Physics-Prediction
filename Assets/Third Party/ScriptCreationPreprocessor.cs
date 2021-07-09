using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MB
{
#if UNITY_EDITOR
    /// <summary>
    /// A PreProcessor for scripts, used for replacing #VARIABLES like #NAMESPACE
    /// </summary>
    public class ScriptCreationPreprocessor : UnityEditor.AssetModificationProcessor
    {
        public static class Assemblies
        {
            public static bool Query(string file, out AssemblyDefinitionAsset asset)
            {
                if (QueryAssets(file, out asset))
                    return true;

                if (QueryReferences(file, out asset))
                    return true;

                return false;
            }

            static bool CheckFileHierarchy(string file, string assembly)
            {
                file = Path.GetDirectoryName(file);
                assembly = Path.GetDirectoryName(assembly);

                file = UnifyPathSeperator(file);
                assembly = UnifyPathSeperator(assembly);

                return file.StartsWith(assembly);
            }

            static bool QueryAssets(string file, out AssemblyDefinitionAsset asset)
            {
                var guids = AssetDatabase.FindAssets($"t:{nameof(AssemblyDefinitionAsset)}");

                for (int i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);

                    if (CheckFileHierarchy(file, path))
                    {
                        asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                        return true;
                    }
                }

                asset = default;
                return false;
            }

            static bool QueryReferences(string file, out AssemblyDefinitionAsset asset)
            {
                var guids = AssetDatabase.FindAssets($"t:{nameof(AssemblyDefinitionReferenceAsset)}");

                for (int i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);

                    if (CheckFileHierarchy(file, path))
                    {
                        var reference = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionReferenceAsset>(path);

                        asset = Resolve(reference);

                        if (asset == null) continue;

                        return true;
                    }
                }

                asset = default;
                return false;
            }

            static AssemblyDefinitionAsset Resolve(AssemblyDefinitionReferenceAsset reference)
            {
                var token = JObject.Parse(reference.text)["reference"];

                if (token == null) return null;

                var guid = token.ToObject<string>();
                if (string.IsNullOrEmpty(guid)) return null;

                var id = "GUID:";

                guid = guid.Remove(0, id.Length);

                var path = AssetDatabase.GUIDToAssetPath(guid);

                return AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
            }

            public static bool TryGetRootNamespace(AssemblyDefinitionAsset asset, out string name, string fallback)
            {
                if(asset == null)
                {
                    name = fallback;
                    return false;
                }

                var token = JObject.Parse(asset.text)["rootNamespace"];

                if (token == null)
                {
                    name = fallback;
                    return false;
                }

                name = token.ToObject<string>();

                if (string.IsNullOrEmpty(name))
                {
                    name = fallback;
                    return false;
                }

                return true;
            }
        }

        public static string GlobalNamespace
        {
            get
            {
                var value = EditorSettings.projectGenerationRootNamespace;

                if (string.IsNullOrEmpty(value)) value = "Default";

                return value;
            }
        }

        public static void OnWillCreateAsset(string path)
        {
            path = path.Replace(".meta", "");

            var extension = Path.GetExtension(path);
            if (extension != ".cs") return;

            Assemblies.Query(path, out var assembly);

            var text = File.ReadAllText(path);

            text = ProcessNamespace(assembly, text);

            File.WriteAllText(path, text);

            AssetDatabase.Refresh();
        }

        static string ProcessNamespace(AssemblyDefinitionAsset assembly, string text)
        {
            Assemblies.TryGetRootNamespace(assembly, out var name, GlobalNamespace);

            return text.Replace("#NAMESPACE#", name);
        }

        static ScriptCreationPreprocessor()
        {

        }

        //Utility

        public static string UnifyPathSeperator(string path) => path.Replace('\\', '/');
    }
#endif
}