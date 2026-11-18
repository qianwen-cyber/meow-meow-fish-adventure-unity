using UnityEngine;
using System.Collections.Generic;

public class MeowGame : MonoBehaviour
{
    class Item { public GameObject go; public Vector2 p; public int kind; public float age, seed; public Item(GameObject g, Vector2 position, int type) { go=g; p=position; kind=type; seed=Random.value*6.28f; } }
    readonly List<Item> fish=new List<Item>(), hazards=new List<Item>(), particles=new List<Item>();
    readonly List<Transform> scenery=new List<Transform>();
    readonly int[] collected=new int[3], levels=new int[3];
    readonly Color[] colors={new Color(.96f,.46f,.43f),new Color(.31f,.68f,.9f),new Color(1,.76f,.27f)};
    Color brown=new Color(.29f,.22f,.19f), cream=new Color(1,.97f,.89f), teal=new Color(.16f,.49f,.48f);
    Camera gameCamera; GameObject cat; SpriteRenderer catRenderer; Sprite[] catFrames=new Sprite[4], fishSprites=new Sprite[3];
    Sprite white, circle, gullSprite, crateSprite; Texture2D dot; Font font; GUIStyle style=new GUIStyle();
    LineRenderer grabRing, meowRing; AudioSource effects,music; AudioClip catchClip,hurtClip,meowClip,upgradeClip;
    Vector2 position=new Vector2(0,-5.5f); float time, spawnFish, spawnHazard, hurtTime, eatTime, charge, grabTime, grabRadius, meowTime, meowCooldown, bannerTime;
    int hearts=5, score, total, best, lost; bool playing, finished, victory, paused, help, charging, moving, muted;
    string banner="";
    const float Duration=90; const int Goal=25;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot() { if(FindFirstObjectByType<MeowGame>()==null)new GameObject("Meow Fish Adventure").AddComponent<MeowGame>(); }
    void Awake()
    {
        Application.targetFrameRate=60; best=PlayerPrefs.GetInt("MeowFishBest",0);
        font=Resources.Load<Font>("NotoSansTC"); if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        white=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
        dot=new Texture2D(64,64); for(int y=0;y<64;y++)for(int x=0;x<64;x++)dot.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(32-Vector2.Distance(new Vector2(x+.5f,y+.5f),Vector2.one*32)))); dot.Apply();
        circle=Sprite.Create(dot,new Rect(0,0,64,64),Vector2.one*.5f,64);
        for(int i=0;i<3;i++)fishSprites[i]=MakeFish(colors[i]); gullSprite=MakeGull(); crateSprite=MakeCrate();
        Texture2D atlas=Resources.Load<Texture2D>("CatAtlas");
        if(atlas!=null)for(int i=0;i<4;i++){float w=atlas.width/2f,h=atlas.height/2f;catFrames[i]=Sprite.Create(atlas,new Rect((i%2)*w,(1-i/2)*h,w,h),Vector2.one*.5f,w);}
        gameCamera=Camera.main; if(gameCamera==null)gameCamera=new GameObject("Main Camera").AddComponent<Camera>();
        gameCamera.tag="MainCamera"; gameCamera.orthographic=true;gameCamera.orthographicSize=8;gameCamera.transform.position=new Vector3(0,0,-10);
        gameCamera.rect=new Rect(280/1280f,0,720/1280f,1);gameCamera.backgroundColor=new Color(.49f,.8f,.81f);gameCamera.clearFlags=CameraClearFlags.SolidColor;
        if(FindFirstObjectByType<AudioListener>()==null)gameCamera.gameObject.AddComponent<AudioListener>();
        BuildHarbor();cat=SpriteObject("Orange kitten",catFrames[0]!=null?catFrames[0]:circle,position,1.95f,20,Color.white);catRenderer=cat.GetComponent<SpriteRenderer>();
        cat.AddComponent<CircleCollider2D>().radius=.22f;
        grabRing=Ring("Fishing range",colors[1]);meowRing=Ring("Meow wave",colors[2]);
        effects=gameObject.AddComponent<AudioSource>();effects.volume=.35f;
        music=gameObject.AddComponent<AudioSource>();music.volume=.1f;music.loop=true;music.clip=Music();
        catchClip=Tone(720,.12f,1.7f);hurtClip=Tone(170,.25f,.5f);meowClip=Tone(640,.4f,.35f);upgradeClip=Tone(880,.3f,1.6f);
    }
    GameObject SpriteObject(string name,Sprite sprite,Vector2 p,float size,int order,Color color)
    {var go=new GameObject(name);go.transform.position=p;var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;go.transform.localScale=Vector3.one*size;return go;}
    Transform Block(string name,float x,float y,float w,float h,Color color,int order=0)
    {var go=SpriteObject(name,white,new Vector2(x,y),1,order,color);go.transform.localScale=new Vector3(w,h,1);return go.transform;}
    void BuildHarbor()
    {
        Block("Pier shadow",.12f,0,11.8f,40,new Color(.27f,.57f,.58f),-12);
        Block("Wooden pier",0,0,11.4f,40,new Color(.81f,.65f,.46f),-11);
        for(int i=0;i<8;i++)Block("Long wooden plank",-5.05f+i*1.45f,0,.035f,40,new Color(.67f,.51f,.36f),-10);
        for(int i=0;i<22;i++){float y=-11+i*1.15f;scenery.Add(Block("Plank seam",0,y,11.4f,.026f,new Color(.71f,.56f,.4f),-9));if(i%3==0){scenery.Add(Block("Left bollard",-5.62f,y,.26f,.42f,brown,-5));scenery.Add(Block("Right bollard",5.62f,y,.26f,.42f,brown,-5));}}
        for(int i=0;i<20;i++){float x=i%2==0?-6.55f:6.55f;scenery.Add(Block("Sea foam",x,-10+i*1.1f,.7f,.045f,new Color(.8f,.95f,.9f,.7f),-8));}
        Block("Pier left trim",-5.8f,0,.14f,40,new Color(.95f,.84f,.64f),-6);Block("Pier right trim",5.8f,0,.14f,40,new Color(.95f,.84f,.64f),-6);
    }
    Texture2D Canvas(){var t=new Texture2D(128,128,TextureFormat.RGBA32,false);var p=new Color[128*128];t.SetPixels(p);t.filterMode=FilterMode.Bilinear;return t;}
    void Ellipse(Texture2D t,float cx,float cy,float rx,float ry,Color c){for(int y=Mathf.Max(0,(int)(cy-ry));y<Mathf.Min(128,cy+ry+1);y++)for(int x=Mathf.Max(0,(int)(cx-rx));x<Mathf.Min(128,cx+rx+1);x++)if(Mathf.Pow((x-cx)/rx,2)+Mathf.Pow((y-cy)/ry,2)<1)t.SetPixel(x,y,c);}
    void Box(Texture2D t,int x,int y,int w,int h,Color c){for(int j=Mathf.Max(0,y);j<Mathf.Min(128,y+h);j++)for(int i=Mathf.Max(0,x);i<Mathf.Min(128,x+w);i++)t.SetPixel(i,j,c);}
    void Stroke(Texture2D t,Vector2 a,Vector2 b,float radius,Color c){int steps=Mathf.CeilToInt(Vector2.Distance(a,b)*1.5f);for(int i=0;i<=steps;i++){Vector2 p=Vector2.Lerp(a,b,steps==0?0:(float)i/steps);Ellipse(t,p.x,p.y,radius,radius,c);}}
    Sprite FinishSprite(Texture2D t){t.Apply();return Sprite.Create(t,new Rect(0,0,128,128),Vector2.one*.5f,128);}
    Sprite MakeFish(Color c){var t=Canvas();Stroke(t,new Vector2(30,64),new Vector2(13,81),10,brown);Stroke(t,new Vector2(30,64),new Vector2(13,47),10,brown);Stroke(t,new Vector2(30,64),new Vector2(13,78),6,c);Stroke(t,new Vector2(30,64),new Vector2(13,50),6,c);Ellipse(t,69,64,43,28,brown);Ellipse(t,69,64,39,24,c);Ellipse(t,79,73,14,5,new Color(1,1,1,.7f));Ellipse(t,92,68,4,5,brown);Stroke(t,new Vector2(61,54),new Vector2(69,63),2,new Color(1,1,1,.7f));return FinishSprite(t);}
    Sprite MakeGull(){var t=Canvas();Stroke(t,new Vector2(63,64),new Vector2(16,91),11,brown);Stroke(t,new Vector2(63,64),new Vector2(112,91),11,brown);Stroke(t,new Vector2(63,66),new Vector2(16,93),7,cream);Stroke(t,new Vector2(63,66),new Vector2(112,93),7,cream);Ellipse(t,64,63,19,28,brown);Ellipse(t,64,65,16,25,cream);Ellipse(t,64,44,7,10,colors[2]);Ellipse(t,57,69,3,4,brown);Ellipse(t,71,69,3,4,brown);return FinishSprite(t);}
    Sprite MakeCrate(){var t=Canvas();Box(t,14,14,100,100,brown);Box(t,19,19,90,90,new Color(.7f,.43f,.23f));for(int i=0;i<4;i++)Box(t,21,27+i*22,86,3,new Color(.5f,.31f,.2f));Stroke(t,new Vector2(24,24),new Vector2(104,104),5,new Color(.94f,.72f,.42f));Stroke(t,new Vector2(24,104),new Vector2(104,24),5,new Color(.94f,.72f,.42f));return FinishSprite(t);}
    LineRenderer Ring(string name,Color color){var go=new GameObject(name);var r=go.AddComponent<LineRenderer>();r.material=new Material(Shader.Find("Sprites/Default"));r.startColor=r.endColor=color;r.startWidth=r.endWidth=.045f;r.loop=true;r.positionCount=64;r.sortingOrder=25;r.enabled=false;return r;}
    void DrawRing(LineRenderer ring,Vector2 center,float radius,Color color){ring.enabled=true;ring.startColor=ring.endColor=color;for(int i=0;i<64;i++){float angle=i*Mathf.PI*2/64;ring.SetPosition(i,new Vector3(center.x+Mathf.Cos(angle)*radius,center.y+Mathf.Sin(angle)*radius,0));}}
    AudioClip Tone(float hz,float seconds,float slope){int n=(int)(22050*seconds);var samples=new float[n];float phase=0;for(int i=0;i<n;i++){float u=(float)i/n;phase+=Mathf.Lerp(hz,hz*slope,u)*2*Mathf.PI/22050;samples[i]=Mathf.Sin(phase)*.5f*Mathf.Sin(Mathf.PI*u);}var clip=AudioClip.Create("Harbor synth",n,1,22050,false);clip.SetData(samples,0);return clip;}
    AudioClip Music(){int[] notes={0,4,7,12,9,7,4,2,0,4,7,9,7,4,2,-1};int n=22050*8;var samples=new float[n];for(int i=0;i<n;i++){int note=(i/11025)%notes.Length;float t=(i%11025)/22050f;float hz=261.63f*Mathf.Pow(2,notes[note]/12f);samples[i]=Mathf.Sin(t*hz*2*Mathf.PI)*Mathf.Exp(-t*7)*.3f+Mathf.Sin(i/22050f*130.815f*2*Mathf.PI)*.04f;}var clip=AudioClip.Create("Harbor picnic",n,1,22050,false);clip.SetData(samples,0);return clip;}
    void Sound(AudioClip clip){if(!muted)effects.PlayOneShot(clip);}
    void Clear(List<Item> items){foreach(var i in items)if(i.go!=null)Destroy(i.go);items.Clear();}
    public void Begin()
    {
        Clear(fish);Clear(hazards);Clear(particles);for(int i=0;i<3;i++)collected[i]=levels[i]=0;
        hearts=5;score=total=lost=0;time=spawnFish=spawnHazard=hurtTime=eatTime=charge=grabTime=meowTime=meowCooldown=0;
        playing=true;finished=victory=paused=help=charging=false;position=new Vector2(0,-5.5f);cat.transform.position=position;
        Notice("第一關：漁港碼頭 · 收集 25 條魚！");if(!muted)music.Play();
        for(int i=0;i<3;i++)AddFish(new Vector2((i-1)*1.1f,-3),i);
    }
    void Notice(string value){banner=value;bannerTime=2.5f;}
    void AddFish(Vector2 p,int kind){fish.Add(new Item(SpriteObject("Fish "+kind,fishSprites[kind],p,.95f,5,Color.white),p,kind));}
    float Range(){return 1.2f+levels[1]*.28f;}
    void Grab(float power)
    {
        grabRadius=Range()+power*1.55f;grabTime=.3f;eatTime=.4f;int caught=0;
        for(int i=fish.Count-1;i>=0;i--){var f=fish[i];if(Vector2.Distance(f.p,position)>grabRadius)continue;int kind=f.kind;total++;collected[kind]++;score+=Mathf.RoundToInt(100*(1+.25f*levels[2]));caught++;Burst(f.p,colors[kind],8);Destroy(f.go);fish.RemoveAt(i);
            if(collected[kind]>=3&&levels[kind]<3){collected[kind]-=3;levels[kind]++;Notice((kind==0?"紅魚升級：跑得更快！":kind==1?"藍魚升級：抓魚範圍變大！":"黃魚升級：得分加成！")+" Lv."+levels[kind]);Sound(upgradeClip);}}
        if(caught>0)Sound(catchClip);
    }
    void Meow()
    {
        if(meowCooldown>0)return;meowCooldown=8;meowTime=.7f;Sound(meowClip);int scared=0;
        for(int i=hazards.Count-1;i>=0;i--){var h=hazards[i];if(h.kind==1&&Vector2.Distance(h.p,position)<4){Burst(h.p,cream,12);Destroy(h.go);hazards.RemoveAt(i);scared++;}}
        Notice(scared>0?"喵～！海鷗被嚇跑了！":"喵～！附近暫時安全。");
    }
    void Hit(){if(hurtTime>0||!playing)return;hearts--;hurtTime=1.8f;Sound(hurtClip);Burst(position,colors[0],14);if(hearts<=0)End(false);}
    void End(bool win){playing=false;finished=true;victory=win;charging=false;charge=0;grabRing.enabled=false;meowRing.enabled=false;music.Stop();if(win)score+=hearts*500;best=Mathf.Max(best,score);PlayerPrefs.SetInt("MeowFishBest",best);PlayerPrefs.Save();catRenderer.sprite=catFrames[win?2:3];Sound(win?upgradeClip:hurtClip);}
    void Burst(Vector2 p,Color c,int count){for(int i=0;i<count;i++){var item=new Item(SpriteObject("Sparkle",circle,p,Random.Range(.06f,.13f),30,c),p,0);item.seed=Random.value*6.28f;item.age=.5f;particles.Add(item);}}
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.M)){muted=!muted;music.mute=effects.mute=muted;if(!muted&&playing&&!music.isPlaying)music.Play();}
        if(!playing){if(!help&&Input.GetKeyDown(KeyCode.Return))Begin();return;}
        if(Input.GetKeyDown(KeyCode.P)||Input.GetKeyDown(KeyCode.Escape)){paused=!paused;charging=false;charge=0;if(paused)music.Pause();else music.UnPause();}
        if(paused)return;
        float dt=Mathf.Min(Time.deltaTime,.05f);time+=dt;spawnFish-=dt;spawnHazard-=dt;hurtTime=Mathf.Max(0,hurtTime-dt);eatTime-=dt;grabTime-=dt;meowTime-=dt;bannerTime-=dt;meowCooldown=Mathf.Max(0,meowCooldown-dt);
        Vector2 input=new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0));
        moving=input.sqrMagnitude>.01f;position+=input.normalized*(4.3f+levels[0]*.4f)*dt;position.x=Mathf.Clamp(position.x,-4.95f,4.95f);position.y=Mathf.Clamp(position.y,-6.7f,5.3f);
        if(Input.GetKeyDown(KeyCode.Space)){charging=true;charge=0;}if(charging)charge=Mathf.Min(1,charge+dt/1.2f);if(Input.GetKeyUp(KeyCode.Space)&&charging){Grab(charge);charging=false;charge=0;}if(Input.GetKeyDown(KeyCode.C))Meow();
        if(charging)DrawRing(grabRing,position,Range()+charge*1.55f,new Color(.28f,.65f,.8f,.6f));else if(grabTime>0)DrawRing(grabRing,position,grabRadius,new Color(.28f,.65f,.8f,grabTime*3));else grabRing.enabled=false;
        if(meowTime>0)DrawRing(meowRing,position,4*(1-meowTime/.7f),new Color(1,.83f,.36f,meowTime));else meowRing.enabled=false;
        float scroll=1.75f;foreach(var t in scenery){t.position+=Vector3.down*scroll*dt;if(t.position.y< -12)t.position+=Vector3.up*24;}
        if(spawnFish<=0&&time<Duration-3){spawnFish=1.15f;float x=Random.Range(-3.7f,3.7f);int kind=Random.Range(0,3);for(int i=0;i<3;i++)AddFish(new Vector2(x+(i-1)*.8f,8.6f+i*.18f),(kind+i)%3);}
        if(spawnHazard<=0&&time>5&&time<Duration-5){spawnHazard=Random.Range(1.5f,2.8f);int kind=time>12&&Random.value>.45f?1:0;Vector2 p=new Vector2(Random.Range(-4.5f,4.5f),9.5f);hazards.Add(new Item(SpriteObject(kind==0?"Wooden crate":"Seagull",kind==0?crateSprite:gullSprite,p,kind==0?1.45f:1.65f,10,Color.white),p,kind));}
        for(int i=fish.Count-1;i>=0;i--){var f=fish[i];f.age+=dt;f.p.y-=scroll*dt;f.go.transform.position=f.p+new Vector2(Mathf.Sin(f.age*3+f.seed)*.08f,0);if(f.p.y< -9){Destroy(f.go);fish.RemoveAt(i);lost++;}}
        for(int i=hazards.Count-1;i>=0;i--){var h=hazards[i];h.age+=dt;h.p.y-=(h.kind==0?scroll:3)*dt;if(h.kind==1)h.p.x+=Mathf.Cos(h.age*2+h.seed)*1.5f*dt;h.go.transform.position=h.p;if(h.kind==1)h.go.transform.localScale=new Vector3(1.65f,1.65f+Mathf.Sin(h.age*12)*.15f,1);
            float safe=h.kind==0?1.05f:.83f;float distance=Vector2.Distance(position,h.p);if(distance<safe){Hit();if(h.kind==0){Vector2 push=(position-h.p).normalized;if(push.sqrMagnitude<.1f)push=Vector2.down;position=h.p+push*safe;position.x=Mathf.Clamp(position.x,-4.95f,4.95f);position.y=Mathf.Clamp(position.y,-6.7f,5.3f);}}if(h.p.y< -10){Destroy(h.go);hazards.RemoveAt(i);}}
        for(int i=particles.Count-1;i>=0;i--){var p=particles[i];p.age-=dt;p.p+=new Vector2(Mathf.Cos(p.seed),Mathf.Sin(p.seed))*dt*2;p.go.transform.position=p.p;var r=p.go.GetComponent<SpriteRenderer>();Color c=r.color;c.a=Mathf.Max(0,p.age*2);r.color=c;if(p.age<=0){Destroy(p.go);particles.RemoveAt(i);}}
        cat.transform.position=position+Vector2.up*(moving?Mathf.Sin(time*16)*.05f:Mathf.Sin(time*3)*.025f);catRenderer.sprite=catFrames[hurtTime>1.1f?3:eatTime>0?2:moving&&(int)(time*7)%2==1?1:0];catRenderer.color=new Color(1,1,1,hurtTime>0&&((int)(hurtTime*12)%2==0)?.4f:1);
        if(playing&&time>=Duration)End(total>=Goal);
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
        Text(28,27,225,24,"MEOW MEOW / FISH ADVENTURE",10,teal);Text(26,70,230,110,"喵喵魚魚\n大冒險",35,brown);Text(28,193,225,30,"第一關 · 漁港碼頭",17,teal);
        SpriteGUI(catFrames[finished&&victory?2:0],new Rect(46,245,185,185));
        Text(28,464,225,26,"今日的魚魚收穫",15,brown);Text(26,498,230,60,score.ToString("D5"),40,teal);
        Text(28,575,225,28,"最高紀錄  "+best.ToString("D5"),15,brown);
        Round(22,647,236,112,new Color(.95f,.9f,.78f));Text(37,663,208,80,"小橘貓的美食任務\n收集 25 條魚，平安抵達\n碼頭盡頭的魚魚派對！",14,brown);
        Text(1026,32,230,30,"魚魚進化圖鑑",21,brown);
        for(int i=0;i<3;i++){float y=95+i*107;SpriteGUI(fishSprites[i],new Rect(1026,y-6,58,58));Text(1091,y,160,26,(i==0?"紅魚 · 移動速度":i==1?"藍魚 · 抓魚範圍":"黃魚 · 得分加成"),14,brown);Text(1091,y+32,160,24,"Lv."+levels[i]+"  "+(levels[i]>=3?"MAX":collected[i]+" / 3"),14,teal);Fill(1028,y+70,222,5,new Color(.88f,.86f,.77f));Fill(1028,y+70,222*(levels[i]>=3?1:collected[i]/3f),5,colors[i]);}
        Text(1028,429,225,28,"冒險小抄",20,brown);Text(1028,474,230,144,"WASD／方向鍵：移動\nSpace：放開時抓魚\n長按 Space：蓄力擴大範圍\nC：喵喵叫，嚇跑海鷗\nP：暫停　 M：音效",14,brown);
        if(playing){if(Button(1028,654,224,meowCooldown>0?"喵喵叫  "+Mathf.CeilToInt(meowCooldown)+" 秒":"喵喵叫  [ C ]",meowCooldown>0?new Color(.65f,.68f,.6f):teal)&&!paused)Meow();if(Button(1028,718,224,paused?"繼續冒險 [ P ]":"暫停一下 [ P ]",new Color(.67f,.47f,.32f)))PauseToggle();}
        else Text(1028,690,225,54,"UNITY 2D · WEBGL\n"+(muted?"音效關閉":"音效開啟"),12,teal);
        Round(300,17,680,86,new Color(1,.98f,.91f,.97f));Text(319,28,175,25,"愛心  "+new string('♥',Mathf.Max(0,hearts)),20,colors[0]);Text(511,28,210,25,"魚魚 "+total+" / "+Goal,19,teal);Text(800,31,155,25,Mathf.Max(0,Mathf.CeilToInt(Duration-time))+" 秒",17,brown,TextAnchor.MiddleRight);
        Fill(319,75,642,7,new Color(.87f,.85f,.73f));Fill(319,75,642*Mathf.Clamp01(time/Duration),7,teal);
        if(charging){Round(496,687,288,55,cream);Fill(511,721,258*charge,6,colors[1]);Text(500,695,280,23,"蓄力抓魚  "+Mathf.RoundToInt(charge*100)+"%",14,teal,TextAnchor.MiddleCenter);}
        if(bannerTime>0&&playing&&!paused){Round(339,116,602,46,new Color(1,.98f,.9f,.95f));Text(349,120,582,37,banner,16,brown,TextAnchor.MiddleCenter);}
        if(!playing||paused){Fill(280,0,720,800,new Color(.19f,.3f,.27f,.42f));Round(345,145,590,510,cream);
            if(help){Text(370,177,540,45,"小橘貓的冒險教室",28,brown,TextAnchor.MiddleCenter);Text(391,253,500,250,"① 用 WASD 或方向鍵靠近魚群。\n② 按下並放開空白鍵，就能抓到範圍內的魚。\n③ 長按空白鍵蓄力，再放開可抓更遠！\n④ 每吃 3 條同色魚，對應能力升一級。\n⑤ 木箱會擋路，海鷗會搶魚，碰到會掉愛心。\n⑥ 按 C 喵喵叫可嚇跑附近海鷗，冷卻 8 秒。\n⑦ 在 90 秒旅程結束時至少收集 25 條魚。",16,brown);if(Button(461,562,358,"知道了，出發吃魚！",teal))Begin();}
            else if(paused){SpriteGUI(catFrames[0],new Rect(560,205,160,160));Text(390,377,500,45,"休息一下，魚魚會等你。",25,brown,TextAnchor.MiddleCenter);if(Button(461,513,358,"繼續冒險 [ P ]",teal))PauseToggle();}
            else if(finished){SpriteGUI(catFrames[victory?2:3],new Rect(550,170,180,180));Text(380,358,520,42,victory?"魚魚派對，開動囉！":"小橘貓需要再試一次",28,brown,TextAnchor.MiddleCenter);Text(390,416,500,76,(victory?"成功抵達漁港終點！":hearts<=0?"愛心用完了，試著避開木箱和海鷗。":"魚魚還不夠，靠近魚群再蓄力抓取吧！")+"\n收集 "+total+" 條魚　總分 "+score+"　最高分 "+best,16,teal,TextAnchor.MiddleCenter);if(Button(461,540,358,"再來一場冒險 [ ENTER ]",teal))Begin();}
            else {Text(380,175,520,30,"一趟有魚、有風、有小橘貓的旅程",15,teal,TextAnchor.MiddleCenter);Text(380,223,520,60,"喵喵魚魚大冒險",34,brown,TextAnchor.MiddleCenter);SpriteGUI(catFrames[0],new Rect(540,291,200,200));Text(390,484,500,36,"準備好一起去漁港吃魚了嗎？",17,brown,TextAnchor.MiddleCenter);if(Button(390,553,255,"開始冒險 [ ENTER ]",teal))Begin();if(Button(661,553,228,"操作教學",new Color(.76f,.5f,.3f)))help=true;}
        }
    }
}
