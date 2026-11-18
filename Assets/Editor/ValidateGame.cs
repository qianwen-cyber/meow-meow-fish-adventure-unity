using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.Reflection;
using System.IO;

[InitializeOnLoad]
public static class ValidateGame
{
    static int frames;
    static ValidateGame(){if(SessionState.GetBool("MeowSmoke",false))EditorApplication.update+=Tick;}
    public static void Run(){BuildGame.Prepare();SessionState.SetBool("MeowSmoke",true);EditorApplication.isPlaying=true;}
    static FieldInfo Field(string name){return typeof(MeowGame).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);}
    static object Get(MeowGame g,string name){return Field(name).GetValue(g);}
    static void Set(MeowGame g,string name,object value){Field(name).SetValue(g,value);}
    static void Call(MeowGame g,string name,params object[] args){typeof(MeowGame).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,args);}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("MEOW CHECK PASSED: "+message);}
    static void Tick()
    {
        if(!EditorApplication.isPlaying||++frames<15)return;
        EditorApplication.update-=Tick;SessionState.SetBool("MeowSmoke",false);
        int oldBest=PlayerPrefs.GetInt("MeowFishBest",0);
        try{
            var g=UnityEngine.Object.FindFirstObjectByType<MeowGame>();Check(g!=null,"Scene starts with MeowGame");
            Check(Resources.Load<Font>("NotoSansTC")!=null,"Traditional Chinese font loads");
            Check(Resources.Load<Texture2D>("CatAtlas")!=null,"Four-pose kitten atlas loads");
            g.Begin();Check((bool)Get(g,"playing"),"Start enters gameplay");Check((int)Get(g,"hearts")==5,"New game has five hearts");
            Vector2 p=(Vector2)Get(g,"position");
            for(int kind=0;kind<3;kind++)for(int i=0;i<3;i++)Call(g,"AddFish",p,kind);
            Call(g,"Grab",1f);Check((int)Get(g,"total")>=9,"Charged grab collects nearby fish");
            var levels=(int[])Get(g,"levels");Check(levels[0]>=1&&levels[1]>=1&&levels[2]>=1,"Three fish colors upgrade corresponding abilities");
            Check((int)Get(g,"score")>0,"Fish collection awards score");
            Call(g,"Meow");Check((float)Get(g,"meowCooldown")>0,"Meow activates cooldown");
            Call(g,"Hit");Call(g,"Hit");Check((int)Get(g,"hearts")==4,"Damage reduces one heart and grants invulnerability");
            for(int i=0;i<4;i++){Set(g,"hurtTime",0f);Call(g,"Hit");}
            Check(!(bool)Get(g,"playing")&&(bool)Get(g,"finished")&&!(bool)Get(g,"victory"),"Zero hearts leads to failure screen");
            g.Begin();Check((int)Get(g,"total")==0&&(int)Get(g,"hearts")==5,"Restart clears previous run");
            Set(g,"total",25);Call(g,"End",true);Check((bool)Get(g,"victory")&&(int)Get(g,"score")==2500,"Successful finish applies remaining-heart bonus");
            g.Begin();PlayerPrefs.SetInt("MeowFishBest",oldBest);PlayerPrefs.Save();Set(g,"best",oldBest);
            File.WriteAllText(Path.GetFullPath("../../work/meow-smoke.json"),"{\"passed\":true,\"checks\":13}");
            Debug.Log("MEOW SMOKE TESTS PASSED");EditorApplication.Exit(0);
        }catch(Exception e){PlayerPrefs.SetInt("MeowFishBest",oldBest);PlayerPrefs.Save();Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
