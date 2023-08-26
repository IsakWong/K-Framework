using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using Object = UnityEngine.Object;

public class AssetSystem : KSingleton<AssetSystem>
{
    public Dictionary<string, AssetBundle> mLoadBundles = new Dictionary<string, AssetBundle>();
    public Dictionary<string, Object> mLoadAssets = new Dictionary<string, Object>();

    [MenuItem("Assets/Build AssetBundle")]
    public static void BuildAssetBundle()
    {
        string assetBundleDirectory = "Assets/AssetBundles";
        if (!System.IO.Directory.Exists(assetBundleDirectory))
        {
            System.IO.Directory.CreateDirectory(assetBundleDirectory);
        }

        BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
    }

    public AssetBundle LoadBundle(string bundleName)
    {
#if UNITY_EDITOR
        return null;
#endif
        if (mLoadBundles.TryGetValue(bundleName, out AssetBundle bundle))
        {
            return bundle;
        }

        bundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, bundleName));
        mLoadBundles.Add(bundleName, bundle);

        // TODO AssetBundle“¿¿µ
        /*AssetBundleManifest manifest = bundle.InternalLoadAsset<AssetBundleManifest>("AssetBundleManifest");
        if (manifest != null)
        {
            string[] dependencies = manifest.GetAllDependencies(bundleName);

            foreach (var dependency in dependencies)
            {
                LoadBundle(dependency);
            }
        }*/
        return bundle;
    }

    public void Unload()
    {

#if !UNITY_EDITOR
        foreach (var bundle in LoadBundles)
        {
            bundle.Value.Unload(true);
        }
#endif

        mLoadBundles.Clear();
        mLoadAssets.Clear();
    }

    public T LoadAsset<T>(string path) where T : Object
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<T>(path);
#endif
    }
}
