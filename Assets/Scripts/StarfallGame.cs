using UnityEngine;
using System.Collections.Generic;

// Entire game runs inside Unity. Coordinates use a 1200 x 800 design canvas.
public class StarfallGame : MonoBehaviour
{
    const float Left = 300, Right = 900, Top = 30, Bottom = 770;
    class Entity { public Vector2 p, v; public float hp, max, t, next; public int kind; public Entity(float x,float y,int k=0) { p=new Vector2(x,y); kind=k; } }
    readonly List<Entity> enemies=new List<Entity>(), shots=new List<Entity>(), bullets=new List<Entity>(), drops=new List<Entity>(), sparks=new List<Entity>();
    Vector2 player; float elapsed, fire, spawn, invincible, shieldDelay, charge, flash, bossShot, shake;
    float shield=100, armor=100, bossHP=600, bossMax=600; int score, best, level=1, emp=3, kills;
    bool playing, ended, won, paused, bossActive, muted; Vector2 boss;
    Texture2D pixel, disc; GUIStyle text=new GUIStyle(); AudioSource audioSource; AudioClip shotSound, hitSound, powerSound;
    readonly Color cyan=new Color(.27f,.91f,1), gold=new Color(1,.78f,.35f), red=new Color(1,.35f,.46f), ink=new Color(.025f,.045f,.105f);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot() { if(FindFirstObjectByType<StarfallGame>()==null) new GameObject("Starfall Game").AddComponent<StarfallGame>(); }
    void Awake()
    {
        Application.targetFrameRate=60; pixel=Texture2D.whiteTexture;
        disc=new Texture2D(64,64,TextureFormat.RGBA32,false); disc.filterMode=FilterMode.Bilinear;
        for(int y=0;y<64;y++) for(int x=0;x<64;x++) { float a=Mathf.Clamp01(32-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32))); disc.SetPixel(x,y,new Color(1,1,1,a)); } disc.Apply();
        text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.normal.textColor=Color.white;
        audioSource=gameObject.AddComponent<AudioSource>(); audioSource.volume=.14f;
        shotSound=Tone(690,.055f); hitSound=Tone(110,.16f); powerSound=Tone(980,.22f);
        best=PlayerPrefs.GetInt("StarfallBest",0); player=new Vector2(600,660);
    }
    AudioClip Tone(float frequency,float duration) { int count=(int)(22050*duration); float[] data=new float[count]; for(int i=0;i<count;i++) data[i]=Mathf.Sin(i*frequency*2*Mathf.PI/22050)*Mathf.Pow(1-(float)i/count,2); var clip=AudioClip.Create("Synth",count,1,22050,false); clip.SetData(data,0); return clip; }
    void Sound(AudioClip clip) { if(!muted) audioSource.PlayOneShot(clip); }
    void StartGame()
    {
        enemies.Clear(); shots.Clear(); bullets.Clear(); drops.Clear(); sparks.Clear(); player=new Vector2(600,660);
        elapsed=fire=spawn=invincible=shieldDelay=charge=flash=bossShot=shake=0; shield=armor=100; score=kills=0; level=1; emp=3; bossHP=bossMax;
        playing=true; ended=won=paused=bossActive=false; boss=new Vector2(600,-100);
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.M)) muted=!muted;
        if(!playing) { if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Space)) StartGame(); return; }
        if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.P)) paused=!paused;
        if(paused) return;
        float dt=Mathf.Min(Time.deltaTime,.04f); elapsed+=dt; fire-=dt; spawn-=dt; invincible-=dt; shieldDelay-=dt; flash=Mathf.Max(0,flash-dt); shake=Mathf.Max(0,shake-dt);
        Vector2 move=new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0)-(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0));
        player+=move.normalized*(Input.GetKey(KeyCode.LeftShift)?190:350)*dt;
        if(Input.GetMouseButton(0)) { Vector2 target=new Vector2(Input.mousePosition.x/Screen.width*1200,(Screen.height-Input.mousePosition.y)/Screen.height*800); if(target.x>Left&&target.x<Right&&target.y>Top) player=Vector2.MoveTowards(player,target,650*dt); }
        player.x=Mathf.Clamp(player.x,Left+22,Right-22); player.y=Mathf.Clamp(player.y,Top+50,Bottom-24);
        if(shieldDelay<=0) shield=Mathf.Min(100,shield+8*dt);
        charge=Mathf.Min(100,charge+12*dt);
        if(Input.GetKeyDown(KeyCode.X)&&charge>=100) { charge=0; for(int j=-2;j<=2;j++) { var b=new Entity(player.x+j*12,player.y-28,1); b.v=new Vector2(j*35,-640); b.hp=18; shots.Add(b); } Sound(powerSound); }
        if(Input.GetKeyDown(KeyCode.Space)&&emp>0) EMP();
        if(fire<=0) { fire=.16f; for(int j=0;j<(level==1?1:level==2?2:3);j++) { float offset=level==1?0:level==2?(j-.5f)*18:(j-1)*17; var s=new Entity(player.x+offset,player.y-23); s.v=new Vector2(level==3?(j-1)*28:0,-640); s.hp=level==3?3:2.5f; shots.Add(s); } Sound(shotSound); }
        if(!bossActive&&elapsed<85&&spawn<=0) { spawn=Mathf.Max(.3f,1.05f-elapsed*.006f); int kind=Random.Range(0,elapsed>25?3:2); var e=new Entity(Random.Range(Left+35,Right-35),-35,kind); e.hp=e.max=kind==2?16:kind==1?7:4; e.v=new Vector2(0,kind==2?70:kind==1?100:135); e.next=Random.Range(.5f,1.8f); enemies.Add(e); }
        if(elapsed>=85&&!bossActive) { bossActive=true; enemies.Clear(); bullets.Clear(); flash=.2f; }
        for(int i=enemies.Count-1;i>=0;i--) { var e=enemies[i]; e.t+=dt; e.p+=e.v*dt; if(e.kind==1) e.p.x+=Mathf.Sin(e.t*3)*85*dt; e.next-=dt; if(e.next<=0) { e.next=e.kind==2?1.15f:2; Vector2 dir=(player-e.p).normalized; EnemyBullet(e.p,dir*(e.kind==2?185:150)); if(e.kind==2) { EnemyBullet(e.p,new Vector2(dir.x-.35f,dir.y).normalized*185); EnemyBullet(e.p,new Vector2(dir.x+.35f,dir.y).normalized*185); } }
            if(Vector2.Distance(e.p,player)<29) { Hurt(24); Burst(e.p,red,12); enemies.RemoveAt(i); } else if(e.p.y>Bottom+40) enemies.RemoveAt(i); }
        if(bossActive) UpdateBoss(dt);
        for(int i=shots.Count-1;i>=0;i--) { var s=shots[i]; s.p+=s.v*dt; bool hit=false;
            if(bossActive&&bossHP>0&&Mathf.Abs(s.p.x-boss.x)<85&&Mathf.Abs(s.p.y-boss.y)<44) { bossHP-=s.hp; Burst(s.p,cyan,2); hit=true; if(bossHP<=0) { Burst(boss,gold,75); Finish(true); } }
            if(!hit) for(int j=enemies.Count-1;j>=0;j--) { var e=enemies[j]; if(Vector2.Distance(s.p,e.p)<(e.kind==2?30:23)) { e.hp-=s.hp; Burst(s.p,cyan,2); hit=true; if(e.hp<=0) { score+=e.kind==2?250:100; kills++; Burst(e.p,e.kind==2?gold:red,15); Sound(hitSound); if(kills%5==0) drops.Add(new Entity(e.p.x,e.p.y,kills%15==0?1:0)); enemies.RemoveAt(j); } break; } }
            if(hit||s.p.y<0) shots.RemoveAt(i);
        }
        for(int i=bullets.Count-1;i>=0;i--) { var b=bullets[i]; b.p+=b.v*dt; if(Vector2.Distance(b.p,player)<13) { Hurt(16); bullets.RemoveAt(i); } else if(b.p.y>810||b.p.y<0||b.p.x<Left-20||b.p.x>Right+20) bullets.RemoveAt(i); }
        for(int i=drops.Count-1;i>=0;i--) { var d=drops[i]; d.t+=dt; d.p.y+=85*dt; if(Vector2.Distance(d.p,player)<34) { if(d.kind==0) { level=Mathf.Min(3,level+1); score+=150; } else { armor=Mathf.Min(100,armor+30); shield=100; emp=Mathf.Min(5,emp+1); } Sound(powerSound); Burst(d.p,gold,18); drops.RemoveAt(i); } else if(d.p.y>810) drops.RemoveAt(i); }
        for(int i=sparks.Count-1;i>=0;i--) { var p=sparks[i]; p.t-=dt; p.p+=p.v*dt; if(p.t<=0) sparks.RemoveAt(i); }
    }
    void UpdateBoss(float dt) { boss.y=Mathf.MoveTowards(boss.y,150,70*dt); boss.x=600+Mathf.Sin(elapsed*.9f)*175; bossShot-=dt; if(boss.y<110||bossShot>0) return; bossShot=bossHP<bossMax*.5f?.5f:.8f; for(int i=-3;i<=3;i++) { float a=i*.23f+Mathf.Sin(elapsed)*.2f; EnemyBullet(boss+new Vector2(0,35),new Vector2(Mathf.Sin(a),Mathf.Cos(a))*190); } }
    void EnemyBullet(Vector2 p,Vector2 v) { var b=new Entity(p.x,p.y); b.v=v; bullets.Add(b); }
    void Hurt(float damage) { if(invincible>0||!playing) return; float absorbed=Mathf.Min(shield,damage); shield-=absorbed; armor-=damage-absorbed; invincible=.65f; shieldDelay=4; shake=.2f; Burst(player,cyan,10); Sound(hitSound); if(armor<=0) Finish(false); }
    void EMP() { emp--; bullets.Clear(); flash=.4f; shake=.2f; foreach(var e in enemies) { Burst(e.p,gold,15); score+=100; kills++; } enemies.Clear(); if(bossActive) { bossHP-=65; if(bossHP<=0) Finish(true); } shield=Mathf.Min(100,shield+20); Sound(powerSound); }
    void Burst(Vector2 p,Color color,int count) { for(int i=0;i<count;i++) { var s=new Entity(p.x,p.y,color==gold?1:color==red?2:0); s.v=Random.insideUnitCircle*Random.Range(50,200); s.t=Random.Range(.15f,.65f); sparks.Add(s); } }
    void Finish(bool victory) { playing=false; ended=true; won=victory; if(victory) score+=5000+Mathf.RoundToInt(Mathf.Max(0,armor)*20); best=Mathf.Max(best,score); PlayerPrefs.SetInt("StarfallBest",best); PlayerPrefs.Save(); }
    void Rect(float x,float y,float w,float h,Color c) { GUI.color=c; GUI.DrawTexture(new Rect(x,y,w,h),pixel); GUI.color=Color.white; }
    void Circle(float x,float y,float r,Color c) { GUI.color=c; GUI.DrawTexture(new Rect(x-r,y-r,r*2,r*2),disc); GUI.color=Color.white; }
    void Line(Vector2 a,Vector2 b,float width,Color c) { Matrix4x4 old=GUI.matrix; GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a); Rect(a.x,a.y,Vector2.Distance(a,b),width,c); GUI.matrix=old; }
    void Label(float x,float y,float w,float h,string value,int size,Color color,TextAnchor align=TextAnchor.UpperLeft) { text.fontSize=size; text.normal.textColor=color; text.alignment=align; text.fontStyle=FontStyle.Normal; GUI.Label(new Rect(x,y,w,h),value,text); }
    void Bar(float x,float y,float w,float v,Color c) { Rect(x,y,w,6,new Color(.13f,.19f,.27f)); Rect(x,y,w*Mathf.Clamp01(v),6,c); }
    bool Button(float x,float y,float w,string value) { Rect(x,y,w,48,cyan); Label(x,y,w,48,value,17,ink,TextAnchor.MiddleCenter); return GUI.Button(new Rect(x,y,w,48),GUIContent.none,GUIStyle.none); }
    void Ship(Vector2 p,bool enemy,int kind=0,float scale=1)
    {
        Color c=enemy?(kind==2?gold:red):cyan; float k=enemy?1:-1; Matrix4x4 old=GUI.matrix; GUIUtility.ScaleAroundPivot(new Vector2(scale,scale),p);
        Circle(p.x,p.y-k*23,10,new Color(c.r,c.g,c.b,.16f)); Rect(p.x-4,p.y-k*28,8,19,c);
        Line(new Vector2(p.x-28,p.y-k*10),new Vector2(p.x,p.y+k*5),9,c); Line(new Vector2(p.x,p.y+k*5),new Vector2(p.x+28,p.y-k*10),9,c);
        Circle(p.x,p.y,12,new Color(.78f,.9f,1)); Rect(p.x-8,p.y-7,16,21,new Color(.14f,.24f,.35f)); Circle(p.x,p.y+k*3,5,c); Rect(p.x-3,p.y+(enemy?9:-25),6,16,c);
        GUI.matrix=old;
    }
    void OnGUI()
    {
        GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1200f,Screen.height/800f,1)); Rect(0,0,1200,800,ink);
        Rect(Left,Top,600,740,new Color(.035f,.075f,.14f));
        float clock=playing?elapsed:Time.realtimeSinceStartup;
        for(int i=0;i<85;i++) { float x=Left+((i*173.71f)%600); float y=Top+Mathf.Repeat(i*71.39f+clock*(20+i%3*22),740); Circle(x,y,i%9==0?1.8f:1,new Color(.5f,.72f,.9f,i%3==0?.6f:.25f)); }
        for(int i=0;i<13;i++) { float y=Top+Mathf.Repeat(i*62+clock*22,740); Rect(Left,y,600,1,new Color(.15f,.3f,.45f,.1f)); }
        Rect(Left,Top,1,740,new Color(.2f,.4f,.5f)); Rect(Right,Top,1,740,new Color(.2f,.4f,.5f));
        Label(36,35,240,20,"ORBITAL DEFENSE / 01",12,cyan); Label(34,76,240,90,"STARFALL\nDEFENDER",31,Color.white); Label(36,178,230,32,"HOLD THE LAST SKY",12,gold);
        Rect(36,234,226,1,new Color(.2f,.3f,.4f)); Label(36,260,230,22,"MISSION SCORE",12,new Color(.55f,.65f,.76f)); Label(36,287,230,55,score.ToString("D6"),38,Color.white);
        Label(36,358,220,22,"PERSONAL BEST",12,new Color(.55f,.65f,.76f)); Label(36,386,220,34,best.ToString("D6"),23,gold);
        Label(36,480,225,25,"MISSION PROGRESS",12,new Color(.55f,.65f,.76f)); Bar(36,516,226,elapsed/85,cyan); Label(36,536,226,30,bossActive?"DREADNOUGHT ENCOUNTER":Mathf.Min(100,Mathf.FloorToInt(elapsed/85*100))+"%  /  APPROACH",13,cyan);
        Label(36,664,230,80,"01  PATROL\n02  INTERCEPT\n03  DREADNOUGHT",13,new Color(.45f,.56f,.68f));
        Label(934,38,230,26,"PILOT TELEMETRY",13,cyan); Label(934,95,230,24,"SHIELD  /  "+Mathf.CeilToInt(shield),14,Color.white); Bar(934,130,226,shield/100,cyan);
        Label(934,163,230,24,"ARMOR  /  "+Mathf.Max(0,Mathf.CeilToInt(armor)),14,Color.white); Bar(934,198,226,armor/100,red);
        Label(934,239,230,24,"PULSE  /  "+Mathf.FloorToInt(charge)+"%",14,Color.white); Bar(934,274,226,charge/100,gold);
        Label(934,318,230,32,"WEAPON LEVEL  0"+level,17,gold); Label(934,365,230,32,"EMP CHARGES  "+emp,17,cyan);
        Rect(934,427,226,1,new Color(.2f,.3f,.4f)); Label(934,450,230,24,"FLIGHT MANUAL",12,new Color(.55f,.65f,.76f));
        Label(934,488,240,180,"WASD / ARROWS    Move\nMOUSE DRAG        Move\nSHIFT                   Precision\nSPACE                 EMP blast\nX                          Pulse shot\nP / ESC                Pause\nM                         Sound",13,Color.white);
        Label(934,717,225,35,muted?"AUDIO  OFF":"AUDIO  ON",12,muted?red:cyan); Label(934,744,225,24,"UNITY / WEBGL",11,new Color(.45f,.56f,.68f));
        Matrix4x4 world=GUI.matrix; if(shake>0) GUI.matrix=GUI.matrix*Matrix4x4.Translate(new Vector3(Mathf.Sin(elapsed*100)*4,Mathf.Cos(elapsed*93)*3,0));
        if(playing||ended) {
            foreach(var e in enemies) { Ship(e.p,true,e.kind,e.kind==2?1.3f:.9f); if(e.kind==2) Bar(e.p.x-25,e.p.y-35,50,e.hp/e.max,gold); }
            if(bossActive&&bossHP>0) { Ship(boss,true,2,3); Label(400,42,400,24,"WARNING / THE DREADNOUGHT",14,red,TextAnchor.MiddleCenter); Bar(390,74,420,bossHP/bossMax,red); }
            foreach(var s in shots) { if(s.kind==1) { Rect(s.p.x-8,s.p.y-15,16,38,new Color(.4f,.9f,1,.2f)); Rect(s.p.x-3,s.p.y-15,6,35,Color.white); } else { Rect(s.p.x-2,s.p.y-9,4,18,cyan); Circle(s.p.x,s.p.y,5,new Color(.2f,.9f,1,.2f)); } }
            foreach(var b in bullets) { Circle(b.p.x,b.p.y,8,new Color(1,.2f,.3f,.16f)); Circle(b.p.x,b.p.y,4,red); Circle(b.p.x,b.p.y,1.8f,Color.white); }
            foreach(var d in drops) { Circle(d.p.x,d.p.y,17,d.kind==0?gold:cyan); Circle(d.p.x,d.p.y,14,ink); Label(d.p.x-16,d.p.y-16,32,32,d.kind==0?"W":"+",18,d.kind==0?gold:cyan,TextAnchor.MiddleCenter); }
            foreach(var s in sparks) Circle(s.p.x,s.p.y,Mathf.Max(1,s.t*5),s.kind==1?gold:s.kind==2?red:cyan);
            if(!ended||won) { if(invincible<=0||(int)(invincible*14)%2==0) Ship(player,false); if(shield>0) { Circle(player.x,player.y,30,new Color(.2f,.8f,1,.07f)); Circle(player.x,player.y,3,Color.white); } }
        }
        GUI.matrix=world;
        if(flash>0) Rect(Left,Top,600,740,new Color(.4f,.9f,1,flash));
        if(!playing) {
            Rect(Left,Top,600,740,new Color(.015f,.035f,.08f,.9f));
            Label(350,140,500,30,ended?(won?"MISSION COMPLETE":"SIGNAL LOST"):"THE LAST SKY IS YOURS",13,ended&&!won?red:cyan,TextAnchor.MiddleCenter);
            Label(340,202,520,110,ended?(won?"SECTOR\nSECURED":"TRY AGAIN,\nPILOT"):"STARFALL\nDEFENDER",46,Color.white,TextAnchor.MiddleCenter);
            Label(365,345,470,80,ended?"SCORE  "+score.ToString("D6")+"     /     BEST  "+best.ToString("D6"):"Survive the enemy waves. Collect weapon cores.\nBreak through the dreadnought and reclaim the sky.",17,new Color(.65f,.77f,.86f),TextAnchor.MiddleCenter);
            if(!ended) { Ship(new Vector2(600,475),false,0,1.5f); Label(370,535,460,48,"AUTO FIRE  /  85s TO BOSS\nGold W = Weapon   /   Cyan + = Repair & EMP",14,gold,TextAnchor.MiddleCenter); }
            if(Button(435,ended?480:620,330,ended?"LAUNCH AGAIN  /  ENTER":"START MISSION  /  ENTER")) StartGame();
        }
        if(paused&&playing) { Rect(Left,Top,600,740,new Color(0,.02f,.05f,.88f)); Label(350,290,500,70,"FLIGHT PAUSED",36,Color.white,TextAnchor.MiddleCenter); if(Button(455,410,290,"RESUME  /  P")) paused=false; }
    }
}
