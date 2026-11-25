using UnityEngine;
using System.Collections.Generic;

public class MeowGame : MonoBehaviour
{
    class Item { public GameObject go; public Vector2 p; public int kind; public float age, seed, shotCooldown=.3f, windup, burstTimer; public int burstRemaining; public Vector2 aim; public LineRenderer warningRing, targetRing; public Item(GameObject g, Vector2 position, int type) { go=g; p=position; kind=type; seed=Random.value*6.28f; } }
    class Dropping { public GameObject go; public Vector2 p,velocity; public float age; }
    readonly List<Dropping> droppings=new List<Dropping>();
    readonly List<Item> fish=new List<Item>(), hazards=new List<Item>(), particles=new List<Item>();
    readonly List<Transform> scenery=new List<Transform>();
    readonly List<SpriteRenderer> harborRenderers=new List<SpriteRenderer>();
    readonly List<Color> harborDayColors=new List<Color>();
    readonly int[] collected=new int[3], levels=new int[3];
    readonly Color[] colors={new Color(.96f,.46f,.43f),new Color(.31f,.68f,.9f),new Color(1,.76f,.27f)};
    Color brown=new Color(.29f,.22f,.19f), cream=new Color(1,.97f,.89f), teal=new Color(.16f,.49f,.48f);
    Camera gameCamera; GameObject cat; SpriteRenderer catRenderer; Sprite[] catFrames=new Sprite[4], backFrames=new Sprite[4], fishSprites=new Sprite[3];
    Sprite white, circle, gullSprite, crateSprite, poopSprite; Texture2D dot, heartTexture; Font font; GUIStyle style=new GUIStyle();
    LineRenderer grabRing, meowRing; AudioSource effects,music; AudioClip catchClip,hurtClip,meowClip,upgradeClip;
    Vector2 position=new Vector2(0,-5.5f); float time, spawnFish, spawnHazard, hurtTime, eatTime, charge, grabTime, grabRadius, meowTime, meowCooldown, bannerTime;
    int levelOneSpawns;
    int healthHalves=10, score, total, best, lost; bool playing, finished, victory, paused, help, charging, moving, muted;
    string banner="";
    int difficultyTier, renderedSceneIndex;
    float harborDarkness, difficultyNoticeTime, moodElapsed, moodFrom, moodTarget;
    float sceneTransition=-1, phasePropFade=1;
    bool sceneSwapPending;
    string difficultyNotice="";
    readonly List<GameObject> phaseProps=new List<GameObject>();
    readonly List<SpriteRenderer> phasePropRenderers=new List<SpriteRenderer>();
    readonly List<Color> phasePropColors=new List<Color>();
    readonly string[] sceneNames={"漁港碼頭","熱鬧魚市場","海邊步道"};
    readonly string[] dayNames={"清晨","白天","傍晚","黑夜"};
    readonly List<Transform> detailChunks=new List<Transform>(), phaseChunks=new List<Transform>();
    Transform activeDetailChunk, activePhaseChunk;
    Color moodTint=Color.white, moodTintFrom=Color.white, moodTintTarget=Color.white;
    Color seaColor=new Color(.49f,.8f,.81f),seaFrom,seaTarget;
    const int MaxLevels=11, Goal=455;
    static readonly int[] FishTargets={25,25,25,30,35,40,45,50,55,60,65};
    const float BaseScrollSpeed=2.4f, SpeedPerDifficulty=.25f, MoodTransitionSeconds=.8f;
    public int ChallengeTier { get { return difficultyTier+1; } }
    public int DifficultyLevel { get { return difficultyTier+1; } }
    public int SceneIndex { get { return difficultyTier<3?0:difficultyTier<7?1:2; } }
    public int TimeOfDayIndex { get { return difficultyTier<3?difficultyTier+1:difficultyTier<7?difficultyTier-3:difficultyTier-7; } }
    public int CurrentFishTarget { get { return FishTargets[difficultyTier]; } }
    public static int FishBeforeLevel(int level) { int sum=0;for(int i=0;i<Mathf.Clamp(level-1,0,MaxLevels);i++)sum+=FishTargets[i];return sum; }
    public int FishInCurrentLevel { get { return Mathf.Clamp(total-FishBeforeLevel(DifficultyLevel),0,CurrentFishTarget); } }
    public int FishToNextDifficulty { get { return CurrentFishTarget-FishInCurrentLevel; } }
    public int NextFishTarget { get { return FishBeforeLevel(DifficultyLevel)+CurrentFishTarget; } }
    public float WorldSpeedMultiplier { get { return 1+SpeedPerDifficulty*ChallengeTier; } }
    public string HarborMoodName { get { return sceneNames[SceneIndex]+"・"+dayNames[TimeOfDayIndex]; } }
    public string LevelProgressText { get { return "本級 "+FishInCurrentLevel+" / "+CurrentFishTarget+" 條 · 再 "+FishToNextDifficulty+" 條"+(DifficultyLevel==MaxLevels?"完成冒險":"升 Lv."+(DifficultyLevel+1)); } }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot() { if(FindFirstObjectByType<MeowGame>()==null)new GameObject("Meow Fish Adventure").AddComponent<MeowGame>(); }
    void Awake()
    {
        Application.targetFrameRate=60; best=PlayerPrefs.GetInt("MeowFishBest",0);
        font=Resources.Load<Font>("NotoSansTC"); if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        white=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
        dot=new Texture2D(64,64); for(int y=0;y<64;y++)for(int x=0;x<64;x++)dot.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(32-Vector2.Distance(new Vector2(x+.5f,y+.5f),Vector2.one*32)))); dot.Apply();
        circle=Sprite.Create(dot,new Rect(0,0,64,64),Vector2.one*.5f,64);
        for(int i=0;i<3;i++)fishSprites[i]=MakeFish(colors[i]); gullSprite=MakeGull(); crateSprite=MakeCrate();poopSprite=MakeDropping();heartTexture=MakeHeart();
        Texture2D atlas=Resources.Load<Texture2D>("CatAtlas");
        if(atlas!=null)for(int i=0;i<4;i++){float w=atlas.width/2f,h=atlas.height/2f;catFrames[i]=Sprite.Create(atlas,new Rect((i%2)*w,(1-i/2)*h,w,h),Vector2.one*.5f,w);}
        Texture2D backAtlas=Resources.Load<Texture2D>("CatBackAtlas");
        if(backAtlas!=null)for(int i=0;i<4;i++){float w=backAtlas.width/2f,h=backAtlas.height/2f;backFrames[i]=Sprite.Create(backAtlas,new Rect((i%2)*w,(1-i/2)*h,w,h),Vector2.one*.5f,w);}
        gameCamera=Camera.main; if(gameCamera==null)gameCamera=new GameObject("Main Camera").AddComponent<Camera>();
        gameCamera.tag="MainCamera"; gameCamera.orthographic=true;gameCamera.orthographicSize=8;gameCamera.transform.position=new Vector3(0,0,-10);
        gameCamera.rect=new Rect(280/1280f,0,720/1280f,1);gameCamera.backgroundColor=new Color(.49f,.8f,.81f);gameCamera.clearFlags=CameraClearFlags.SolidColor;
        if(FindFirstObjectByType<AudioListener>()==null)gameCamera.gameObject.AddComponent<AudioListener>();
        BuildHarbor();cat=SpriteObject("Orange kitten",backFrames[0]!=null?backFrames[0]:circle,position,1.95f,20,Color.white);catRenderer=cat.GetComponent<SpriteRenderer>();
        cat.AddComponent<CircleCollider2D>().radius=.22f;
        grabRing=Ring("Fishing range",colors[1]);meowRing=Ring("Meow wave",colors[2]);
        effects=gameObject.AddComponent<AudioSource>();effects.volume=.35f;
        music=gameObject.AddComponent<AudioSource>();music.volume=.1f;music.loop=true;music.clip=Music();
        catchClip=Tone(720,.12f,1.7f);hurtClip=Tone(170,.25f,.5f);meowClip=Tone(640,.4f,.35f);upgradeClip=Tone(880,.3f,1.6f);
    }
    GameObject SpriteObject(string name,Sprite sprite,Vector2 p,float size,int order,Color color)
    {var go=new GameObject(name);go.transform.position=p;var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;go.transform.localScale=Vector3.one*size;return go;}
    Transform Block(string name,float x,float y,float w,float h,Color color,int order=0)
    {var go=SpriteObject(name,white,new Vector2(x,y),1,order,color);go.transform.localScale=new Vector3(w,h,1);if(activeDetailChunk!=null)go.transform.SetParent(activeDetailChunk,true);harborRenderers.Add(go.GetComponent<SpriteRenderer>());harborDayColors.Add(color);return go.transform;}
    void BuildHarbor()
    {
        foreach(var renderer in harborRenderers)if(renderer!=null){renderer.enabled=false;Destroy(renderer.gameObject);}
        harborRenderers.Clear();harborDayColors.Clear();scenery.Clear();
        foreach(var chunk in detailChunks)if(chunk!=null)Destroy(chunk.gameObject);detailChunks.Clear();activeDetailChunk=null;
        renderedSceneIndex=SceneIndex;
        Color ground=renderedSceneIndex==0?new Color(.81f,.65f,.46f):renderedSceneIndex==1?new Color(.78f,.75f,.65f):renderedSceneIndex==2?new Color(.94f,.83f,.61f):new Color(.67f,.73f,.76f);
        Block("Path shadow",.12f,0,11.8f,40,new Color(.27f,.49f,.5f),-12);
        Block("Journey ground",0,0,11.4f,40,ground,-11);
        if(renderedSceneIndex==2)Block("Beach boardwalk",0,0,8.7f,40,new Color(.72f,.72f,.65f),-10);
        for(int i=0;i<8;i++)Block("Path lines",-5.05f+i*1.45f,0,.035f,40,ground*.82f,-9);
        for(int i=0;i<22;i++){
            float y=-11+i*1.15f;
            scenery.Add(Block("Path seams",0,y,11.4f,.026f,ground*.85f,-8));
            if(i%3==0){scenery.Add(Block("Left post",-5.62f,y,.26f,.42f,brown,-5));scenery.Add(Block("Right post",5.62f,y,.26f,.42f,brown,-5));}
        }
        for(int i=0;i<20;i++)scenery.Add(Block("Sea foam",i%2==0?-6.55f:6.55f,-10+i*1.1f,.7f,.045f,new Color(.8f,.95f,.9f,.7f),-8));
        Block("Left path trim",-5.8f,0,.14f,40,new Color(.95f,.84f,.64f),-6);Block("Right path trim",5.8f,0,.14f,40,new Color(.95f,.84f,.64f),-6);
        for(int strip=0;strip<2;strip++){
            activeDetailChunk=new GameObject("Roadside strip "+strip).transform;detailChunks.Add(activeDetailChunk);
            BuildSceneDetails();activeDetailChunk.position=Vector3.up*(strip*24);
        }
        activeDetailChunk=null;BuildTimeProps();ApplyHarborColors();
    }
    void BackgroundSprite(string name,Sprite sprite,Vector2 p,float size,Color color,int order=-4)
    {
        var go=SpriteObject(name,sprite,p,size,order,color);
        if(activeDetailChunk!=null)go.transform.SetParent(activeDetailChunk,true);
        harborRenderers.Add(go.GetComponent<SpriteRenderer>());harborDayColors.Add(color);
    }
    void BuildSceneDetails()
    {
        if(renderedSceneIndex==0){
            for(int side=-1;side<=1;side+=2)for(int j=0;j<6;j++){
                float x=side*6.35f,y=-10+j*4;
                BackgroundSprite("Lifebuoy",circle,new Vector2(x,y),.65f,cream);
                BackgroundSprite("Lifebuoy center",circle,new Vector2(x,y),.35f,new Color(.83f,.42f,.28f),-3);
                Block("Mooring rope",x,y-1.1f,.06f,.9f,new Color(.74f,.57f,.32f),-4);
            }
        }else if(renderedSceneIndex==1){
            for(int side=-1;side<=1;side+=2)for(int j=0;j<6;j++){
                float x=side*6.15f,y=-10+j*4;
                Block("Market table",x,y,1.3f,.65f,new Color(.65f,.41f,.25f),-4);
                Block("Market canopy",x,y+.65f,1.55f,.52f,cream,-3);
                for(int stripe=0;stripe<4;stripe++)Block("Canopy stripe",x-.58f+stripe*.39f,y+.65f,.2f,.52f,new Color(.91f,.43f,.4f),-2);
                BackgroundSprite("Fish stall sign",fishSprites[j%3],new Vector2(x,y+.05f),.75f,Color.white,-2);
            }
        }else if(renderedSceneIndex==2){
            for(int side=-1;side<=1;side+=2)for(int j=0;j<6;j++){
                float x=side*6.15f,y=-10+j*4;
                Block("Umbrella pole",x,y-.55f,.08f,1.1f,brown,-4);
                BackgroundSprite("Beach umbrella",circle,new Vector2(x,y+.15f),1.12f,j%2==0?new Color(.4f,.78f,.69f):new Color(1,.65f,.46f),-3);
                Block("Umbrella stripe",x,y+.15f,.12f,.95f,cream,-2);
                BackgroundSprite("Seashell",circle,new Vector2(x-.3f,y-1.3f),.19f,new Color(1,.77f,.68f),-3);
                BackgroundSprite("Seashell",circle,new Vector2(x+.15f,y-1.15f),.13f,cream,-3);
            }
        }
    }
    void TimeProp(string name,Sprite sprite,Vector2 p,float size,Color color,bool sky=false)
    {
        var go=SpriteObject(name,sprite,p,size,-1,color);
        if(!sky&&activePhaseChunk!=null)go.transform.SetParent(activePhaseChunk,true);
        phaseProps.Add(go);phasePropRenderers.Add(go.GetComponent<SpriteRenderer>());phasePropColors.Add(color);
    }
    void TimePropRect(string name,Vector2 p,float w,float h,Color color)
    {
        TimeProp(name,white,p,1,color);phaseProps[phaseProps.Count-1].transform.localScale=new Vector3(w,h,1);
    }
    void BuildTimeProps()
    {
        foreach(var go in phaseProps)if(go!=null){go.SetActive(false);Destroy(go);}
        foreach(var chunk in phaseChunks)if(chunk!=null)Destroy(chunk.gameObject);
        phaseProps.Clear();phasePropRenderers.Clear();phasePropColors.Clear();phaseChunks.Clear();activePhaseChunk=null;
        if(TimeOfDayIndex==0){
            TimeProp("Dawn sun",circle,new Vector2(6.45f,5.7f),.55f,new Color(1,.8f,.7f),true);
            TimeProp("Dawn cloud",circle,new Vector2(-6.45f,6),.7f,new Color(.93f,.9f,1,.6f),true);
        }else if(TimeOfDayIndex==1)TimeProp("Day sun",circle,new Vector2(6.45f,6.3f),.65f,new Color(1,.88f,.4f),true);
        else if(TimeOfDayIndex==2)TimeProp("Setting sun",circle,new Vector2(6.45f,5.5f),.72f,new Color(1,.56f,.31f),true);
        else{
            TimeProp("Moon glow",circle,new Vector2(6.4f,6.2f),1f,new Color(.8f,.9f,1,.15f),true);
            TimeProp("Night moon",circle,new Vector2(6.4f,6.2f),.6f,new Color(1,.96f,.76f),true);
        }
        for(int strip=0;strip<2;strip++){
            activePhaseChunk=new GameObject("Time roadside strip "+strip).transform;phaseChunks.Add(activePhaseChunk);
            for(int side=-1;side<=1;side+=2){
                if(TimeOfDayIndex<=1){
                    for(int j=0;j<3;j++){
                        float y=-8+j*8;
                        TimeProp("Resting seagull",gullSprite,new Vector2(side*6.25f,y+.8f),.7f,cream);
                        TimePropRect("Fishing bucket",new Vector2(side*6.3f,y-1.3f),.55f,.5f,new Color(.38f,.67f,.78f));
                        TimeProp("Bucket fish",fishSprites[1],new Vector2(side*6.3f,y-1.05f),.45f,Color.white);
                        if(TimeOfDayIndex==0)TimeProp("Dawn lamp",circle,new Vector2(side*6.3f,y+2),.3f,new Color(1,.88f,.66f,.55f));
                    }
                }else{
                    for(int j=0;j<6;j++){
                        Vector2 point=new Vector2(side*6.3f,-10+j*4);
                        TimePropRect("Lantern hanging cord",point+Vector2.up*.5f,.04f,.65f,brown);
                        TimeProp("Lantern glow",circle,point,1f,new Color(1,.75f,.25f,TimeOfDayIndex==2?.16f:.25f));
                        TimeProp(TimeOfDayIndex==2?"Sunset lantern":"Night lantern",circle,point,.4f,new Color(1,.76f,.36f));
                        if(TimeOfDayIndex==3)TimeProp("Firefly",circle,point+new Vector2(side*.35f,.8f),.09f,new Color(.8f,1,.55f));
                    }
                }
            }
            activePhaseChunk.position=Vector3.up*(strip*24);
        }
        activePhaseChunk=null;phasePropFade=0;
    }
    void ScrollScenery(float dt)
    {
        float distance=BaseScrollSpeed*WorldSpeedMultiplier*dt;
        foreach(var item in scenery){item.position+=Vector3.down*distance;while(item.position.y< -12)item.position+=Vector3.up*24;}
        ScrollChunks(detailChunks,distance);ScrollChunks(phaseChunks,distance);
    }
    void ScrollChunks(List<Transform> chunks,float distance)
    {
        foreach(var chunk in chunks){chunk.position+=Vector3.down*distance;while(chunk.position.y< -24)chunk.position+=Vector3.up*48;}
    }
    Color TintForTime(int phase)
    {
        return phase==0?new Color(.86f,.83f,.97f,1):phase==1?Color.white:phase==2?new Color(1,.69f,.54f,1):new Color(.4f,.48f,.72f,1);
    }
    Color SeaForTime(int phase)
    {
        return phase==0?new Color(.62f,.64f,.79f):phase==1?(SceneIndex==2?new Color(.42f,.82f,.8f):new Color(.49f,.8f,.81f)):phase==2?new Color(.66f,.49f,.51f):new Color(.12f,.2f,.35f);
    }
    Texture2D Canvas(){var t=new Texture2D(128,128,TextureFormat.RGBA32,false);var p=new Color[128*128];t.SetPixels(p);t.filterMode=FilterMode.Bilinear;return t;}
    void Ellipse(Texture2D t,float cx,float cy,float rx,float ry,Color c){for(int y=Mathf.Max(0,(int)(cy-ry));y<Mathf.Min(128,cy+ry+1);y++)for(int x=Mathf.Max(0,(int)(cx-rx));x<Mathf.Min(128,cx+rx+1);x++)if(Mathf.Pow((x-cx)/rx,2)+Mathf.Pow((y-cy)/ry,2)<1)t.SetPixel(x,y,c);}
    void Box(Texture2D t,int x,int y,int w,int h,Color c){for(int j=Mathf.Max(0,y);j<Mathf.Min(128,y+h);j++)for(int i=Mathf.Max(0,x);i<Mathf.Min(128,x+w);i++)t.SetPixel(i,j,c);}
    void Stroke(Texture2D t,Vector2 a,Vector2 b,float radius,Color c){int steps=Mathf.CeilToInt(Vector2.Distance(a,b)*1.5f);for(int i=0;i<=steps;i++){Vector2 p=Vector2.Lerp(a,b,steps==0?0:(float)i/steps);Ellipse(t,p.x,p.y,radius,radius,c);}}
    Sprite FinishSprite(Texture2D t){t.Apply();return Sprite.Create(t,new Rect(0,0,128,128),Vector2.one*.5f,128);}
    Sprite MakeFish(Color c){var t=Canvas();Stroke(t,new Vector2(30,64),new Vector2(13,81),10,brown);Stroke(t,new Vector2(30,64),new Vector2(13,47),10,brown);Stroke(t,new Vector2(30,64),new Vector2(13,78),6,c);Stroke(t,new Vector2(30,64),new Vector2(13,50),6,c);Ellipse(t,69,64,43,28,brown);Ellipse(t,69,64,39,24,c);Ellipse(t,79,73,14,5,new Color(1,1,1,.7f));Ellipse(t,92,68,4,5,brown);Stroke(t,new Vector2(61,54),new Vector2(69,63),2,new Color(1,1,1,.7f));return FinishSprite(t);}
    Sprite MakeDropping()
    {
        var t=Canvas();
        Ellipse(t,64,47,39,19,brown);Ellipse(t,64,50,35,16,cream);
        Ellipse(t,64,65,29,19,brown);Ellipse(t,64,68,25,16,new Color(.92f,.87f,.72f));
        Ellipse(t,64,83,16,16,brown);Ellipse(t,65,85,12,13,cream);
        Stroke(t,new Vector2(66,91),new Vector2(72,103),5,cream);
        Ellipse(t,48,54,6,4,Color.white);return FinishSprite(t);
    }
    Texture2D MakeHeart()
    {
        var t=new Texture2D(64,64,TextureFormat.RGBA32,false);
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){
            float a=(x-31.5f)/29f,b=(y-29f)/29f,q=a*a+b*b-1;
            t.SetPixel(x,y,q*q*q-a*a*b*b*b<=0?Color.white:Color.clear);
        }
        t.Apply();t.filterMode=FilterMode.Bilinear;return t;
    }
    Sprite MakeGull(){var t=Canvas();Stroke(t,new Vector2(63,64),new Vector2(16,91),11,brown);Stroke(t,new Vector2(63,64),new Vector2(112,91),11,brown);Stroke(t,new Vector2(63,66),new Vector2(16,93),7,cream);Stroke(t,new Vector2(63,66),new Vector2(112,93),7,cream);Ellipse(t,64,63,19,28,brown);Ellipse(t,64,65,16,25,cream);Ellipse(t,64,44,7,10,colors[2]);Ellipse(t,57,69,3,4,brown);Ellipse(t,71,69,3,4,brown);return FinishSprite(t);}
    Sprite MakeCrate(){var t=Canvas();Box(t,14,14,100,100,brown);Box(t,19,19,90,90,new Color(.7f,.43f,.23f));for(int i=0;i<4;i++)Box(t,21,27+i*22,86,3,new Color(.5f,.31f,.2f));Stroke(t,new Vector2(24,24),new Vector2(104,104),5,new Color(.94f,.72f,.42f));Stroke(t,new Vector2(24,104),new Vector2(104,24),5,new Color(.94f,.72f,.42f));return FinishSprite(t);}
    LineRenderer Ring(string name,Color color){var go=new GameObject(name);var r=go.AddComponent<LineRenderer>();r.material=new Material(Shader.Find("Sprites/Default"));r.startColor=r.endColor=color;r.startWidth=r.endWidth=.045f;r.loop=true;r.positionCount=64;r.sortingOrder=25;r.enabled=false;return r;}
    void DrawRing(LineRenderer ring,Vector2 center,float radius,Color color){ring.enabled=true;ring.startColor=ring.endColor=color;for(int i=0;i<64;i++){float angle=i*Mathf.PI*2/64;ring.SetPosition(i,new Vector3(center.x+Mathf.Cos(angle)*radius,center.y+Mathf.Sin(angle)*radius,0));}}
    AudioClip Tone(float hz,float seconds,float slope){int n=(int)(22050*seconds);var samples=new float[n];float phase=0;for(int i=0;i<n;i++){float u=(float)i/n;phase+=Mathf.Lerp(hz,hz*slope,u)*2*Mathf.PI/22050;samples[i]=Mathf.Sin(phase)*.5f*Mathf.Sin(Mathf.PI*u);}var clip=AudioClip.Create("Harbor synth",n,1,22050,false);clip.SetData(samples,0);return clip;}
    AudioClip Music(){int[] notes={0,4,7,12,9,7,4,2,0,4,7,9,7,4,2,-1};int n=22050*8;var samples=new float[n];for(int i=0;i<n;i++){int note=(i/11025)%notes.Length;float t=(i%11025)/22050f;float hz=261.63f*Mathf.Pow(2,notes[note]/12f);samples[i]=Mathf.Sin(t*hz*2*Mathf.PI)*Mathf.Exp(-t*7)*.3f+Mathf.Sin(i/22050f*130.815f*2*Mathf.PI)*.04f;}var clip=AudioClip.Create("Harbor picnic",n,1,22050,false);clip.SetData(samples,0);return clip;}
    void Sound(AudioClip clip){if(!muted)effects.PlayOneShot(clip);}
    void DisposeHazard(Item item)
    {
        if(item.warningRing!=null){item.warningRing.enabled=false;Destroy(item.warningRing.gameObject);}
        if(item.targetRing!=null){item.targetRing.enabled=false;Destroy(item.targetRing.gameObject);}
        if(item.go!=null){item.go.SetActive(false);Destroy(item.go);}
    }
    void Clear(List<Item> items){foreach(var item in items)DisposeHazard(item);items.Clear();}
    void ClearDroppings(){foreach(var d in droppings)if(d.go!=null){d.go.SetActive(false);Destroy(d.go);}droppings.Clear();}
    public void Begin()
    {
        Clear(fish);Clear(hazards);Clear(particles);ClearDroppings();for(int i=0;i<3;i++)collected[i]=levels[i]=0;
        levelOneSpawns=0;healthHalves=10;score=total=lost=0;time=spawnFish=spawnHazard=hurtTime=eatTime=charge=grabTime=meowTime=meowCooldown=0;
        difficultyTier=0;harborDarkness=0;difficultyNoticeTime=0;difficultyNotice="";moodFrom=moodTarget=0;moodTint=moodTintFrom=moodTintTarget=Color.white;seaColor=seaFrom=seaTarget=SeaForTime(1);moodElapsed=MoodTransitionSeconds;sceneTransition=-1;sceneSwapPending=false;BuildHarbor();phasePropFade=1;ApplyHarborColors();
        playing=true;finished=victory=paused=help=charging=false;position=new Vector2(0,-5.5f);cat.transform.position=position;catRenderer.sprite=backFrames[0];catRenderer.color=Color.white;
        Notice("第 1 級 / 11 · 白天碼頭 · 本級目標 25 條魚！");if(!muted)music.Play();
        for(int i=0;i<3;i++)AddFish(new Vector2(Random.Range(-1.6f,1.6f),Random.Range(-3.4f,-2.3f)),Random.Range(0,3));
    }
    void Notice(string value){banner=value;bannerTime=2.5f;}
    void UpdateDifficulty()
    {
        int nextTier=0;while(nextTier<MaxLevels-1&&total>=FishBeforeLevel(nextTier+2))nextTier++;
        if(nextTier<=difficultyTier)return;
        int oldScene=SceneIndex;
        difficultyTier=nextTier;
        moodFrom=harborDarkness;moodTarget=TimeOfDayIndex==0?.28f:TimeOfDayIndex==1?0:TimeOfDayIndex==2?.5f:1;
        moodTintFrom=moodTint;moodTintTarget=TintForTime(TimeOfDayIndex);seaFrom=seaColor;seaTarget=SeaForTime(TimeOfDayIndex);moodElapsed=0;
        if(oldScene!=SceneIndex){sceneTransition=0;sceneSwapPending=true;hurtTime=Mathf.Max(hurtTime,1f);}
        else BuildTimeProps();
        difficultyNotice="升級 Lv."+DifficultyLevel+" / 11！"+HarborMoodName+" · 本級目標 "+CurrentFishTarget+" 條魚";
        difficultyNoticeTime=4f;Sound(upgradeClip);
    }
    void UpdateHarborMood(float dt)
    {
        if(sceneTransition>=0){
            sceneTransition+=dt;
            if(sceneSwapPending&&sceneTransition>=MoodTransitionSeconds*.5f){BuildHarbor();sceneSwapPending=false;}
            if(sceneTransition>=MoodTransitionSeconds)sceneTransition=-1;
        }
        if(moodElapsed<MoodTransitionSeconds){
            moodElapsed=Mathf.Min(MoodTransitionSeconds,moodElapsed+dt);
            float t=Mathf.SmoothStep(0,1,moodElapsed/MoodTransitionSeconds);
            harborDarkness=Mathf.Lerp(moodFrom,moodTarget,t);moodTint=Color.Lerp(moodTintFrom,moodTintTarget,t);seaColor=Color.Lerp(seaFrom,seaTarget,t);
        }
        phasePropFade=Mathf.Min(1,phasePropFade+dt/MoodTransitionSeconds);
        ApplyHarborColors();
    }
    void ApplyHarborColors()
    {
        gameCamera.backgroundColor=seaColor;
        for(int i=0;i<harborRenderers.Count;i++)if(harborRenderers[i]!=null)harborRenderers[i].color=harborDayColors[i]*moodTint;
        for(int i=0;i<phasePropRenderers.Count;i++)if(phasePropRenderers[i]!=null){Color c=phasePropColors[i];c.a*=phasePropFade;phasePropRenderers[i].color=c;}
    }
    void AddFish(Vector2 p,int kind){fish.Add(new Item(SpriteObject("Fish "+kind,fishSprites[kind],p,.95f,5,Color.white),p,kind));}
    float Range(){return 1.2f+levels[1]*.28f;}
    void Grab(float power)
    {
        grabRadius=Range()+power*1.55f;grabTime=.3f;eatTime=.4f;int caught=0;
        for(int i=fish.Count-1;i>=0;i--){if(total>=Goal)break;var f=fish[i];if(Vector2.Distance(f.p,position)>grabRadius)continue;int kind=f.kind;total++;collected[kind]++;score+=Mathf.RoundToInt(100*(1+.25f*levels[2]));caught++;Burst(f.p,colors[kind],8);Destroy(f.go);fish.RemoveAt(i);
            if(collected[kind]>=3&&levels[kind]<3){collected[kind]-=3;levels[kind]++;Notice((kind==0?"紅魚升級：跑得更快！":kind==1?"藍魚升級：抓魚範圍變大！":"黃魚升級：得分加成！")+" Lv."+levels[kind]);Sound(upgradeClip);}}
        if(caught>0){Sound(catchClip);UpdateDifficulty();if(total>=Goal)End(true);}
    }
    void Meow()
    {
        if(meowCooldown>0)return;meowCooldown=8;meowTime=.7f;Sound(meowClip);int scared=0;
        for(int i=hazards.Count-1;i>=0;i--){var h=hazards[i];if(h.kind==1&&Vector2.Distance(h.p,position)<4){Burst(h.p,cream,12);DisposeHazard(h);hazards.RemoveAt(i);scared++;}}
        int cleared=0;for(int i=droppings.Count-1;i>=0;i--){var d=droppings[i];if(Vector2.Distance(d.p,position)<=4){Burst(d.p,cream,6);d.go.SetActive(false);Destroy(d.go);droppings.RemoveAt(i);cleared++;}}
        Notice(cleared>0?"喵～！清除了 "+cleared+" 顆便便！":scared>0?"喵～！海鷗被嚇跑了！":"喵～！附近暫時安全。");
    }
    void Hit(){Damage(2);}
    void Damage(int halves)
    {
        if(hurtTime>0||!playing)return;
        healthHalves=Mathf.Max(0,healthHalves-halves);hurtTime=1.8f;Sound(hurtClip);Burst(position,colors[0],14);
        Notice(halves==1?"被便便打到了！－半顆心":"碰到障礙物了！－一顆心");if(healthHalves<=0)End(false);
    }
    public int DroppingsPerBurst { get { return 3+Mathf.Min(2,ChallengeTier/4); } }
    public float HazardSpawnScale { get { return 1+.065f*ChallengeTier; } }
    public float SeagullFlightMultiplier { get { return Mathf.Sqrt(WorldSpeedMultiplier); } }
    public float SeagullChance { get { return Mathf.Min(.75f,.55f+.02f*ChallengeTier); } }
    public float HazardStartTime { get { return difficultyTier==0?2.5f:5f; } }
    float NextHazardInterval(){return (difficultyTier==0?Random.Range(1.1f,1.9f):Random.Range(1.5f,2.8f))/HazardSpawnScale;}
    void SpawnHazard()
    {
        int kind;
        if(difficultyTier==0){
            kind=levelOneSpawns==0?1:levelOneSpawns==1?0:Random.value<SeagullChance?1:0;
            levelOneSpawns++;
        }else kind=time>12&&Random.value<SeagullChance?1:0;
        Vector2 p=new Vector2(Random.Range(-4.5f,4.5f),9.5f);
        hazards.Add(new Item(SpriteObject(kind==0?"Wooden crate":"Seagull",kind==0?crateSprite:gullSprite,p,kind==0?1.45f:1.65f,10,Color.white),p,kind));
    }
    const float BurstShotInterval=.18f;
    float NextFishInterval(){return Random.Range(1.04f,1.44f);}
    Vector2 GullShotDirection(Vector2 origin)
    {
        float x=(position-origin).normalized.x;
        if(Mathf.Abs(x)<.28f)x=origin.x>0?-.28f:.28f;
        x=Mathf.Clamp(x,-.65f,.65f);return new Vector2(x,-Mathf.Sqrt(1-x*x));
    }
    void DrawShotWarning(Item gull)
    {
        DrawRing(gull.warningRing,gull.p,.65f,new Color(1,.38f,.2f,.95f));
        var line=gull.targetRing;line.enabled=true;line.loop=false;line.positionCount=2;line.startWidth=line.endWidth=.06f;
        line.startColor=line.endColor=new Color(1,.4f,.2f,.65f);
        Vector2 origin=gull.p+Vector2.down*.3f;line.SetPosition(0,origin);line.SetPosition(1,origin+gull.aim*2.4f);
    }
    void CancelGullAttack(Item gull)
    {
        gull.windup=0;gull.burstRemaining=0;gull.shotCooldown=.5f;
        if(gull.warningRing!=null)gull.warningRing.enabled=false;
        if(gull.targetRing!=null)gull.targetRing.enabled=false;
    }
    void EmitGullShot(Item gull)
    {
        FireDropping(gull.p+Vector2.down*.3f,gull.aim);gull.burstRemaining--;gull.burstTimer=BurstShotInterval;
        if(gull.burstRemaining<=0)gull.shotCooldown=Mathf.Max(1.05f,1.45f-ChallengeTier*.04f);
    }
    void SpawnFishGroup()
    {
        int count=Random.Range(3,7),mainColor=Random.Range(0,3);float center=Random.Range(-3.2f,3.2f);
        var spots=new List<Vector2>();
        for(int i=0;i<count;i++){
            Vector2 spot=Vector2.zero;
            for(int attempt=0;attempt<16;attempt++){
                spot=new Vector2(Mathf.Clamp(center+Random.Range(-1.2f,1.2f),-4.5f,4.5f),8.9f+Random.Range(-.55f,1.15f));
                bool spaced=true;foreach(var other in spots)if(Vector2.Distance(other,spot)<.48f){spaced=false;break;}
                if(spaced)break;
            }
            spots.Add(spot);AddFish(spot,Random.value<.65f?mainColor:Random.Range(0,3));
        }
    }
    void UpdateGullAttack(Item gull,float dt)
    {
        if(gull.p.y<position.y+1.5f){CancelGullAttack(gull);return;}
        if(gull.p.y>7.1f||gull.p.y< -7.5f)return;
        if(gull.burstRemaining>0){
            gull.burstTimer-=dt;if(gull.burstTimer<=0)EmitGullShot(gull);return;
        }
        if(gull.windup>0){
            gull.windup=Mathf.Max(0,gull.windup-dt);DrawShotWarning(gull);
            if(gull.windup<=0){
                gull.warningRing.enabled=gull.targetRing.enabled=false;
                gull.burstRemaining=DroppingsPerBurst;EmitGullShot(gull);
            }
            return;
        }
        gull.shotCooldown-=dt;
        if(gull.shotCooldown>0||gull.p.y<position.y+2.2f)return;
        gull.aim=GullShotDirection(gull.p);gull.windup=.55f;
        if(gull.warningRing==null){gull.warningRing=Ring("Seagull attack warning",colors[0]);gull.targetRing=Ring("Diagonal burst warning",colors[0]);}
        DrawShotWarning(gull);
    }
    void FireDropping(Vector2 origin,Vector2 direction)
    {
        if(origin.y<=position.y+.75f||direction.y>=0)return;
        var d=new Dropping{p=origin,velocity=direction.normalized*(4.8f+.45f*ChallengeTier)};
        d.go=SpriteObject("Seagull dropping",poopSprite,origin,.62f,16,Color.white);droppings.Add(d);
    }
    void UpdateDroppings(float dt)
    {
        for(int i=droppings.Count-1;i>=0;i--){
            var d=droppings[i];Vector2 from=d.p;d.p+=d.velocity*dt;d.age+=dt;d.go.transform.position=d.p;
            Vector2 segment=d.p-from;float t=segment.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(position-from,segment)/segment.sqrMagnitude):0;
            bool hit=Vector2.Distance(position,from+segment*t)<.5f;
            if(hit){Damage(1);Burst(d.p,cream,5);}
            if(hit||d.p.y<position.y-.6f||d.age>6||Mathf.Abs(d.p.x)>8||Mathf.Abs(d.p.y)>11){d.go.SetActive(false);Destroy(d.go);droppings.RemoveAt(i);}
        }
    }
    void End(bool win){playing=false;finished=true;victory=win;charging=false;charge=0;grabRing.enabled=false;meowRing.enabled=false;music.Stop();if(win)score+=healthHalves*250;best=Mathf.Max(best,score);PlayerPrefs.SetInt("MeowFishBest",best);PlayerPrefs.Save();catRenderer.sprite=catFrames[win?2:3];Sound(win?upgradeClip:hurtClip);}
    void Burst(Vector2 p,Color c,int count){for(int i=0;i<count;i++){var item=new Item(SpriteObject("Sparkle",circle,p,Random.Range(.06f,.13f),30,c),p,0);item.seed=Random.value*6.28f;item.age=.5f;particles.Add(item);}}
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.M)){muted=!muted;music.mute=effects.mute=muted;if(!muted&&playing&&!music.isPlaying)music.Play();}
        if(!playing){if(!help&&Input.GetKeyDown(KeyCode.Return))Begin();return;}
        if(Input.GetKeyDown(KeyCode.P)||Input.GetKeyDown(KeyCode.Escape)){paused=!paused;charging=false;charge=0;if(paused)music.Pause();else music.UnPause();}
        if(paused)return;
        float dt=Mathf.Min(Time.deltaTime,.05f);time+=dt;spawnFish-=dt;spawnHazard-=dt;hurtTime=Mathf.Max(0,hurtTime-dt);eatTime-=dt;grabTime-=dt;meowTime-=dt;bannerTime-=dt;meowCooldown=Mathf.Max(0,meowCooldown-dt);
        difficultyNoticeTime=Mathf.Max(0,difficultyNoticeTime-dt);UpdateHarborMood(dt);
        Vector2 input=new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0));
        moving=input.sqrMagnitude>.01f;position+=input.normalized*(4.3f+levels[0]*.4f)*dt;position.x=Mathf.Clamp(position.x,-4.95f,4.95f);position.y=Mathf.Clamp(position.y,-6.7f,5.3f);
        if(Input.GetKeyDown(KeyCode.Space)){charging=true;charge=0;}if(charging)charge=Mathf.Min(1,charge+dt/1.2f);if(Input.GetKeyUp(KeyCode.Space)&&charging){Grab(charge);charging=false;charge=0;}if(Input.GetKeyDown(KeyCode.C))Meow();
        if(charging)DrawRing(grabRing,position,Range()+charge*1.55f,new Color(.28f,.65f,.8f,.6f));else if(grabTime>0)DrawRing(grabRing,position,grabRadius,new Color(.28f,.65f,.8f,grabTime*3));else grabRing.enabled=false;
        if(meowTime>0)DrawRing(meowRing,position,4*(1-meowTime/.7f),new Color(1,.83f,.36f,meowTime));else meowRing.enabled=false;
        float scroll=BaseScrollSpeed*WorldSpeedMultiplier;ScrollScenery(dt);
        if(spawnFish<=0){spawnFish=NextFishInterval();SpawnFishGroup();}
        if(spawnHazard<=0&&time>HazardStartTime){spawnHazard=NextHazardInterval();SpawnHazard();}
        for(int i=fish.Count-1;i>=0;i--){var f=fish[i];f.age+=dt;f.p.y-=scroll*dt;f.go.transform.position=f.p+new Vector2(Mathf.Sin(f.age*3+f.seed)*.08f,0);if(f.p.y< -9){Destroy(f.go);fish.RemoveAt(i);lost++;}}
        for(int i=hazards.Count-1;i>=0;i--){var h=hazards[i];h.age+=dt;bool hovering=h.kind==1&&(h.windup>0||h.burstRemaining>0);if(!hovering){h.p.y-=(h.kind==0?scroll:3.3f*SeagullFlightMultiplier)*dt;if(h.kind==1)h.p.x+=Mathf.Cos(h.age*2+h.seed)*1.5f*SeagullFlightMultiplier*dt;}h.go.transform.position=h.p;if(h.kind==1)UpdateGullAttack(h,dt);if(h.kind==1)h.go.transform.localScale=new Vector3(1.65f,1.65f+Mathf.Sin(h.age*12)*.15f,1);
            float safe=h.kind==0?1.05f:.83f;float distance=Vector2.Distance(position,h.p);if(distance<safe){Hit();if(h.kind==0){Vector2 push=(position-h.p).normalized;if(push.sqrMagnitude<.1f)push=Vector2.down;position=h.p+push*safe;position.x=Mathf.Clamp(position.x,-4.95f,4.95f);position.y=Mathf.Clamp(position.y,-6.7f,5.3f);}}if(h.p.y< -10){DisposeHazard(h);hazards.RemoveAt(i);}}
        UpdateDroppings(dt);
        for(int i=particles.Count-1;i>=0;i--){var p=particles[i];p.age-=dt;p.p+=new Vector2(Mathf.Cos(p.seed),Mathf.Sin(p.seed))*dt*2;p.go.transform.position=p.p;var r=p.go.GetComponent<SpriteRenderer>();Color c=r.color;c.a=Mathf.Max(0,p.age*2);r.color=c;if(p.age<=0){Destroy(p.go);particles.RemoveAt(i);}}
        cat.transform.position=position+Vector2.up*(moving?Mathf.Sin(time*16)*.05f:Mathf.Sin(time*3)*.025f);catRenderer.sprite=backFrames[hurtTime>1.1f?3:(int)(time*8)%2==0?1:2];catRenderer.color=new Color(1,1,1,hurtTime>0&&((int)(hurtTime*12)%2==0)?.4f:1);
    }
    void Fill(float x,float y,float w,float h,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Color.white;}
    void Text(float x,float y,float w,float h,string value,int size,Color color,TextAnchor anchor=TextAnchor.UpperLeft){style.font=font;style.fontSize=size;style.normal.textColor=color;style.alignment=anchor;style.wordWrap=true;GUI.Label(new Rect(x,y,w,h),value,style);}
    void Round(float x,float y,float w,float h,Color c){Fill(x+10,y,w-20,h,c);Fill(x,y+10,w,h-20,c);GUI.color=c;GUI.DrawTexture(new Rect(x,y,20,20),dot);GUI.DrawTexture(new Rect(x+w-20,y,20,20),dot);GUI.DrawTexture(new Rect(x,y+h-20,20,20),dot);GUI.DrawTexture(new Rect(x+w-20,y+h-20,20,20),dot);GUI.color=Color.white;}
    bool Button(float x,float y,float w,string title,Color color){Round(x,y,w,50,color);Text(x,y,w,50,title,18,cream,TextAnchor.MiddleCenter);return GUI.Button(new Rect(x,y,w,50),GUIContent.none,GUIStyle.none);}
    void SpriteGUI(Sprite sprite,Rect rect){if(sprite==null)return;Rect r=sprite.textureRect;GUI.DrawTextureWithTexCoords(rect,sprite.texture,new Rect(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height));}
    void PauseToggle(){paused=!paused;charging=false;charge=0;if(paused)music.Pause();else music.UnPause();}
    void OnGUI()
    {
        GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/800f,1));
        Fill(0,0,280,800,cream);Fill(1000,0,280,800,cream);
        Text(28,27,225,24,"MEOW MEOW / FISH ADVENTURE",10,teal);Text(26,70,230,110,"喵喵魚魚\n大冒險",35,brown);Text(28,193,225,30,"場景 "+(SceneIndex+1)+" / 3 · "+dayNames[TimeOfDayIndex],17,teal);
        SpriteGUI(catFrames[finished&&victory?2:0],new Rect(46,245,185,185));
        Text(28,464,225,26,"今日的魚魚收穫",15,brown);Text(26,498,230,60,score.ToString("D5"),40,teal);
        Text(28,575,225,28,"最高紀錄  "+best.ToString("D5"),15,brown);
        Text(28,610,230,26,"Lv."+DifficultyLevel+" / 11 · 速度 "+WorldSpeedMultiplier.ToString("0.00")+" 倍",14,teal);
        Round(22,647,236,112,new Color(.95f,.9f,.78f));Text(37,660,208,90,"11 級魚魚大冒險\n本級目標："+CurrentFishTarget+" 條魚\n"+sceneNames[SceneIndex]+" · "+dayNames[TimeOfDayIndex]+"\n通關總目標：455 條魚",13,brown);
        Text(1026,32,230,30,"魚魚進化圖鑑",21,brown);
        for(int i=0;i<3;i++){float y=95+i*107;SpriteGUI(fishSprites[i],new Rect(1026,y-6,58,58));Text(1091,y,160,26,(i==0?"紅魚 · 移動速度":i==1?"藍魚 · 抓魚範圍":"黃魚 · 得分加成"),14,brown);Text(1091,y+32,160,24,"Lv."+levels[i]+"  "+(levels[i]>=3?"MAX":collected[i]+" / 3"),14,teal);Fill(1028,y+70,222,5,new Color(.88f,.86f,.77f));Fill(1028,y+70,222*(levels[i]>=3?1:collected[i]/3f),5,colors[i]);}
        Text(1028,429,225,28,"冒險小抄",20,brown);Text(1028,474,230,144,"WASD／方向鍵：移動\nSpace：放開時抓魚\n長按 Space：蓄力擴大範圍\nC：喵喵叫，清除海鷗與便便\nP：暫停　 M：音效",14,brown);
        Round(1024,574,232,69,new Color(.95f,.9f,.78f));
        Text(1036,580,208,19,"受傷說明",14,brown);
        Text(1036,602,208,18,"撞木箱／海鷗：扣 1 顆心",13,brown);
        Text(1036,622,208,18,"海鷗便便：扣半顆心",13,brown);
        if(playing){if(Button(1028,654,224,meowCooldown>0?"喵喵叫  "+Mathf.CeilToInt(meowCooldown)+" 秒":"喵喵叫  [ C ]",meowCooldown>0?new Color(.65f,.68f,.6f):teal)&&!paused)Meow();if(Button(1028,718,224,paused?"繼續冒險 [ P ]":"暫停一下 [ P ]",new Color(.67f,.47f,.32f)))PauseToggle();}
        else Text(1028,690,225,54,"UNITY 2D · WEBGL\n"+(muted?"音效關閉":"音效開啟"),12,teal);
        if(sceneTransition>=0)Fill(280,0,720,800,new Color(.07f,.1f,.17f,Mathf.Sin(sceneTransition/MoodTransitionSeconds*Mathf.PI)));
        Round(300,17,680,110,new Color(1,.98f,.91f,.97f));
        Text(319,27,450,31,"Lv."+DifficultyLevel+" / 11  "+HarborMoodName,21,teal);
        for(int i=0;i<5;i++){
            Rect heart=new Rect(841+i*24,31,21,21);GUI.color=new Color(.8f,.78f,.72f);GUI.DrawTexture(heart,heartTexture);
            float fraction=Mathf.Clamp01((healthHalves-i*2)/2f);if(fraction>0){GUI.color=colors[0];GUI.DrawTextureWithTexCoords(new Rect(heart.x,heart.y,heart.width*fraction,heart.height),heartTexture,new Rect(0,0,fraction,1));}GUI.color=Color.white;
        }
        Text(319,65,470,28,LevelProgressText,17,brown);
        Text(795,67,165,25,"累計 "+total+" / 455",15,teal,TextAnchor.MiddleRight);
        Fill(319,107,642,7,new Color(.87f,.85f,.73f));Fill(319,107,642f*FishInCurrentLevel/CurrentFishTarget,7,teal);
        if(charging){Round(496,687,288,55,cream);Fill(511,721,258*charge,6,colors[1]);Text(500,695,280,23,"蓄力抓魚  "+Mathf.RoundToInt(charge*100)+"%",14,teal,TextAnchor.MiddleCenter);}
        if((difficultyNoticeTime>0||bannerTime>0)&&playing&&!paused){Round(324,140,632,56,new Color(1,.98f,.9f,.95f));Text(334,145,612,44,difficultyNoticeTime>0?difficultyNotice:banner,18,brown,TextAnchor.MiddleCenter);}
        if(!playing||paused){Fill(280,0,720,800,new Color(.19f,.3f,.27f,.42f));Round(345,145,590,510,cream);
            if(help){Text(370,177,540,45,"小橘貓的冒險教室",28,brown,TextAnchor.MiddleCenter);Text(391,253,500,250,"① 用 WASD 或方向鍵靠近魚群。\n② 按下並放開空白鍵，就能抓到範圍內的魚。\n③ 長按空白鍵蓄力，再放開可抓更遠！\n④ 每吃 3 條同色魚，對應能力升一級。\n⑤ 海鷗逐顆斜向連射便便，扣半心；碰撞扣一心。\n⑥ C 嚇跑海鷗並清除附近便便，冷卻 8 秒。\n⑦ 前 3 級各 25 條，第 4 級起目標逐級增加。\n⑧ 共 11 級，後兩個場景會先經過清晨。",16,brown);if(Button(461,562,358,"知道了，出發吃魚！",teal))Begin();}
            else if(paused){SpriteGUI(catFrames[0],new Rect(560,205,160,160));Text(390,377,500,45,"休息一下，魚魚會等你。",25,brown,TextAnchor.MiddleCenter);if(Button(461,513,358,"繼續冒險 [ P ]",teal))PauseToggle();}
            else if(finished){SpriteGUI(catFrames[victory?2:3],new Rect(550,170,180,180));Text(380,358,520,42,victory?"11 級挑戰完成！":"小橘貓需要再試一次",28,brown,TextAnchor.MiddleCenter);Text(390,416,500,76,(victory?"小橘貓抵達海邊魚魚派對！":"愛心用完了，試著避開木箱和海鷗。")+"\n收集 "+total+" 條魚　總分 "+score+"　最高分 "+best,16,teal,TextAnchor.MiddleCenter);if(Button(461,540,358,"再來一場冒險 [ ENTER ]",teal))Begin();}
            else {Text(380,175,520,30,"一趟有魚、有風、有小橘貓的旅程",15,teal,TextAnchor.MiddleCenter);Text(380,223,520,60,"喵喵魚魚大冒險",34,brown,TextAnchor.MiddleCenter);SpriteGUI(catFrames[0],new Rect(540,291,200,200));Text(390,484,500,36,"閃避海鷗斜向連射，收集魚魚闖過 11 級！",17,brown,TextAnchor.MiddleCenter);if(Button(390,553,255,"開始冒險 [ ENTER ]",teal))Begin();if(Button(661,553,228,"操作教學",new Color(.76f,.5f,.3f)))help=true;}
        }
    }
}
