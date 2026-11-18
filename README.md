# 星雲防線 · Starfall Defender

以 Unity 與 C# 開發的單人縱向射擊遊戲，參考課堂範例《Cloud Raiders》的縱向射擊、護盾、武器升級與頭目戰玩法，改為太空防衛主題。美術由 Unity 即時繪製，音效以程式合成，不需下載付費素材。

## 遊戲目標

駕駛戰機守住最後的星域。擊落來襲敵機並收集升級核心，於 85 秒後迎戰無畏艦；擊敗頭目即完成任務，裝甲歸零則任務失敗。

- 三種敵機：直進巡邏機、左右蛇行攔截機、三向射擊重裝機。
- 三級武器：單發、雙發、三向擴散，自動射擊。
- 護盾：受擊後 4 秒未再受擊，開始自動回復。
- EMP：初始 3 次，清除敵彈與一般敵機，對頭目造成傷害。
- 蓄力砲：能量滿 100% 後按 X，發射五道強力脈衝。
- 道具：金色 W 升級武器；青色 + 修復裝甲與護盾，補充 EMP。
- 頭目：扇形彈幕、橫向移動、半血加速攻擊。
- 分數與最高分保存在瀏覽器本機。
- 開始、暫停、勝利、失敗與重新挑戰畫面。

## 操作

| 按鍵 | 功能 |
| --- | --- |
| WASD / 方向鍵 | 移動 |
| 按住滑鼠左鍵並拖曳 | 移動 |
| Shift | 低速精準移動 |
| Space | EMP 清場 |
| X | 滿蓄力時發射脈衝 |
| P / Esc | 暫停 / 繼續 |
| M | 音效開關 |
| Enter | 開始 / 重新挑戰 |

## Unity 開啟與建置

1. 安裝 Unity 6000.6.9f1，勾選 **Web Build Support**（WebGL）。
2. 在 Unity Hub 選擇 Add，指定本專案根目錄（含 Assets、Packages、ProjectSettings）。
3. 選單 **Starfall → 1. Prepare Game** 會建立正式場景、相機並設定發佈參數。
4. 按 Play 試玩。
5. 選單 **Starfall → 2. Build WebGL** 輸出至 `Build/WebGL`。
6. 發佈時需上傳整個 WebGL 資料夾，包括 `index.html`、`Build` 和 `.nojekyll`。

WebGL 使用不壓縮輸出，避免 GitHub Pages 無法自訂壓縮標頭的相容性問題。請透過 HTTP / HTTPS 網址開啟，勿直接雙擊 index.html。

## 程式結構

- `Assets/Scripts/StarfallGame.cs`：遊戲狀態、角色移動、子彈、敵機、頭目、碰撞、繪圖、音效與介面。
- `Assets/Editor/BuildGame.cs`：場景建立與 WebGL 建置。
- `Assets/Scenes/Main.unity`：進入遊戲場景。
- `Assets/WebGLTemplates/Starfall/index.html`：載入進度、全螢幕與中文網頁操作說明。

## 參考與素材

- 課堂參考：https://zycayang.github.io/CloudRaiders/
- Unity 內建字型 LegacyRuntime。
- 幾何圖形、星空、粒子與音效均由本專案程式產生，未複製範例遊戲程式或圖像。
