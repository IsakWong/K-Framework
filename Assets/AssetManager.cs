using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using Object = UnityEngine.Object;

public class AssetManager : KSingleton<AssetManager>
{
    public Dictionary<string, AssetBundle> LoadBundles = new Dictionary<string, AssetBundle>();
    public Dictionary<string, Object> LoadAssets = new Dictionary<string, Object>();
    public bool UsingAssetBundles = false;

#if UNITY_EDITOR
    [MenuItem("Assets/Build AssetBundle")]
    public static void BuildAssetBundle()
    {
        string assetBundleDirectory = "Assets/AssetBundles";
        if (!System.IO.Directory.Exists(assetBundleDirectory))
        {
            System.IO.Directory.CreateDirectory(assetBundleDirectory);
        }

        BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows);
    }
#endif

    public AssetBundle LoadBundle(string bundleName)
    {
        if (UsingAssetBundles)
        {
            if (LoadBundles.TryGetValue(bundleName, out AssetBundle bundle))
            {
                return bundle;
            }

            bundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, bundleName));
            LoadBundles.Add(bundleName, bundle);
            // TODO AssetBundle Dependency
            AssetBundleManifest manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (manifest == null)
            {
                Debug.LogError("Failed to load AssetBundleManifest!");
                return null;
            }

            string[] dependencies = manifest.GetAllDependencies(bundleName);
            foreach (string dependency in dependencies)
            {
                Debug.Log("Dependency: " + dependency);
                LoadBundle(dependency);
            }

            return bundle;
        }

        return null;
    }

    public void Unload()
    {
        if (UsingAssetBundles)
        {
            foreach (var bundle in LoadBundles)
            {
                bundle.Value.Unload(true);
            }

            LoadBundles.Clear();
            LoadAssets.Clear();
        }
    }

    public T LoadAsset<T>(string path) where T : Object
    {
        if (path.StartsWith("Assets"))
        {
            if (UsingAssetBundles)
            {
                if (LoadAssets.TryGetValue(path, out Object asset))
                {
                    return asset as T;
                }

                string bundleName = Path.GetDirectoryName(path);
                AssetBundle bundle = LoadBundle(bundleName);
                if (bundle == null)
                {
                    Debug.LogError("Failed to load AssetBundle: " + bundleName);
                    return null;
                }

                asset = bundle.LoadAsset<T>(Path.GetFileName(path));
                LoadAssets.Add(path, asset);
                return asset as T;
            }
            else
            {
#if UNITY_EDITOR
                return AssetDatabase.LoadAssetAtPath<T>(path);
#else
                var newPath = Path.GetRelativePath("Assets/Resources", path);
                var paths = newPath.Split('.');
                newPath = paths[0];
                Debug.Log($"Load Resources Asset: {newPath}");
                return Resources.Load<T>(newPath);
#endif
            }
        }
        else
        {
            var newPath = Path.GetRelativePath("Assets/Resources", path);
            var paths = newPath.Split('.');
            newPath = paths[0];
            Debug.Log($"Load Asset: {newPath}");
            return Resources.Load<T>(newPath);
        }
    }
#if UNITY_EDITOR
    private static string GetSelectedPathOrFallback()
    {
        string path = "Assets";

        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
        {
            path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                path = Path.GetDirectoryName(path);
                break;
            }
        }

        return path;
    }

    [MenuItem("Assets/Map AssetBundle Name")]
    private static void MapExtensionAssets()
    {
        var path = GetSelectedPathOrFallback();
        string[] allAssets = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        foreach (var assetPath in allAssets)
        {
            string bundlePath = Path.GetDirectoryName(assetPath);
            bundlePath = bundlePath.Replace("\\", "/");
            bundlePath = bundlePath.Replace("/", "_");
            AssetImporter.GetAtPath(assetPath).SetAssetBundleNameAndVariant(bundlePath, "");
            string name = Path.GetFileNameWithoutExtension(assetPath);
        }
    }
#endif
}