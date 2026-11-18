# 喵喵魚魚大冒險 · Meow Meow Fish Adventure

[線上遊玩](https://qianwen-cyber.github.io/meow-meow-fish-adventure-unity/) · [GitHub 專案](https://github.com/qianwen-cyber/meow-meow-fish-adventure-unity)

以 Unity 2D 與 C# 實作的單人休閒收集遊戲。依照遊戲企劃書，期末版本完成第一關「漁港碼頭」。參考《雲端奇兵：星辰脈衝》的縱向捲動、道具收集與能力升級概念，改編成小橘貓的漁港美食冒險。

## 本次完成範圍

- 第一關漁港碼頭，90 秒縱向捲動旅程。
- WASD／方向鍵移動；空白鍵放開時抓取附近魚群，長按可蓄力擴大範圍。
- 紅魚提升速度、藍魚擴大抓魚範圍、黃魚增加得分，每 3 條同色魚升一級，最多 3 級。
- 五顆愛心、木箱阻擋、海鷗干擾，受傷後短暫無敵。
- C 鍵「喵喵叫」嚇跑附近海鷗，冷卻 8 秒。
- 在旅程結束時收集至少 25 條魚且仍有愛心，即可抵達魚魚派對。
- 主選單、操作教學、暫停、通關／失敗結算、重新挑戰、本機最高分。
- 小橘貓待機、走路、吃魚、受驚表情，收集粒子、音效及輕快背景音樂。
- 繁體中文介面、WebGL 載入進度及全螢幕。

第二關魚市場、第三關巨大海鷗與雙人模式依企劃保留為未來擴充，未列入本次完成內容。

## 操作

| 按鍵 | 功能 |
| --- | --- |
| WASD／方向鍵 | 控制小橘貓移動 |
| Space | 放開按鍵時抓取附近魚群 |
| 長按 Space | 蓄力擴大抓魚範圍，再放開抓魚 |
| C | 使用喵喵叫 |
| P／Esc | 暫停或繼續 |
| M | 音效與音樂開關 |
| Enter | 開始或重新挑戰 |

## Unity 開啟與輸出

1. 在 Unity Hub 加入此資料夾，安裝 Unity 6000.6.9f1與 WebGL Build Support。
2. 選單 **Meow Adventure → 1. Prepare Game** 建立場景並套用輸出設定。
3. 開啟 `Assets/Scenes/Main.unity`，按 Play 試玩。
4. 選單 **Meow Adventure → 2. Build WebGL**，輸出位於 `Build/WebGL`。
5. 將整個 WebGL 輸出放至 GitHub 儲存庫的 `docs` 資料夾。
6. GitHub Settings → Pages，選擇 main 分支的 `/docs` 資料夾發佈。

網頁版使用 Gzip 與 Decompression Fallback，適合 GitHub Pages。請透過 HTTP／HTTPS 網址開啟，不能直接雙擊本機 HTML 檔。

## 檔案結構

- `Assets/Scripts/MeowGame.cs`：角色、抓魚、魚種升級、障礙碰撞、音效、遊戲流程與介面。
- `Assets/Resources/CatAtlas.png`：橘貓四個動作表情，於 Unity 分割為 Sprite。
- `Assets/Resources/NotoSansTC.otf`：開源繁體中文字型子集。
- `Assets/Editor/BuildGame.cs`：場景準備與 WebGL 建置。
- `Assets/Editor/ValidateGame.cs`：遊戲狀態與核心玩法驗證。
- `Assets/WebGLTemplates/Meow/index.html`：中文網頁外框與載入畫面。

## 素材與參考

- 課堂參考遊戲：https://zycayang.github.io/CloudRaiders/
- 橘貓圖像：使用 OpenAI 圖像工具為本專案生成。
- 魚、木箱、海鷗、碼頭、海浪與特效：由 Unity 程式繪製。
- 音效與背景音樂：由本專案程式合成。
- 字型：Noto Sans CJK TC，SIL Open Font License 1.1，授權檔附於 `Assets/Resources/NotoFontLicense.txt`。子集僅包含遊戲所需字元。

## 驗證紀錄

2026-11-18：Unity 6000.6.9f1 編譯成功，13 項核心玩法檢查通過，WebGL 建置成功。已在瀏覽器實際驗證中文字型、角色、鍵盤移動、蓄力抓魚、得分與暫停，並經使用者確認畫面與玩法。
