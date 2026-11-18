using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class BuildGame
{
    const string ScenePath="Assets/Scenes/Main.unity";
    [MenuItem("Starfall/1. Prepare Game")]
    public static void Prepare()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Main Camera").AddComponent<Camera>(); camera.tag="MainCamera"; camera.backgroundColor=new Color(.025f,.045f,.105f); camera.clearFlags=CameraClearFlags.SolidColor;
        camera.gameObject.AddComponent<AudioListener>();
        new GameObject("Starfall Game").AddComponent<StarfallGame>();
        Directory.CreateDirectory("Assets/Scenes"); EditorSceneManager.SaveScene(scene,ScenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        PlayerSettings.companyName="Student Studio"; PlayerSettings.productName="Starfall Defender";
        PlayerSettings.defaultScreenWidth=1200; PlayerSettings.defaultScreenHeight=800;
        PlayerSettings.runInBackground=false;
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback=true;
        PlayerSettings.stripEngineCode=true;
        PlayerSettings.WebGL.dataCaching=true;
        PlayerSettings.WebGL.template="PROJECT:Starfall";
        AssetDatabase.SaveAssets(); Debug.Log("STARFALL: Project ready. Press Play to test.");
    }
    [MenuItem("Starfall/2. Build WebGL")]
    public static void WebGL()
    {
        Prepare();
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Build/WebGL",target=BuildTarget.WebGL,options=BuildOptions.None});
        if(result.summary.result!=BuildResult.Succeeded) throw new System.Exception("WebGL build failed: "+result.summary.result);
        File.WriteAllText("Build/WebGL/.nojekyll","");
        Debug.Log("STARFALL: WebGL build successful: Build/WebGL/index.html");
    }
}
