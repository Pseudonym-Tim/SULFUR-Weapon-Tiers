using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildGlintShaders
{
    public static void Build()
    {
        string output = Path.GetFullPath("BundleOutput");
        Directory.CreateDirectory(output);
        AssetBundleBuild bundle = new AssetBundleBuild();
        bundle.assetBundleName = "weapon-tier-glint.bundle";
        bundle.assetNames = new string[] { "Assets/WorldGlint.shader", "Assets/InventoryGlint.shader" };
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(output, new AssetBundleBuild[] { bundle }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
       
        if(manifest == null)
        {
            throw new InvalidOperationException("Shader bundle build failed!");
        }

        foreach(string assetPath in bundle.assetNames)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);

            if(shader == null || ShaderUtil.ShaderHasError(shader))
            {
                throw new InvalidOperationException("Shader compilation failed: " + assetPath);
            }
        }

        // Disabled AssetBundle module can spit out shaders without usable container...
        AssetBundle compiled = AssetBundle.LoadFromFile(Path.Combine(output, bundle.assetBundleName));

        if(compiled == null)
        {
            throw new InvalidOperationException("Compiled shader bundle could not be loaded!");
        }

        try
        {
            if(compiled.GetAllAssetNames().Length != bundle.assetNames.Length)
            {
                throw new InvalidOperationException("Compiled shader bundle is missing assets!");
            }
        }
        finally
        {
            compiled.Unload(true);
        }

        Debug.Log("GLINT BUNDLE BUILD PASS: " + output);
    }
}