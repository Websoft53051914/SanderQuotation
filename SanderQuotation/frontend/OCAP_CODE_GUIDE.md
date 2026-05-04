# OCAP 流程圖 — 程式說明文件

> **目的**：讓 AI 或開發者快速了解架構與修改方式，避免改壞現有邏輯。

---

## 一、檔案結構

| 檔案                            | 說明                                                   |
| ------------------------------- | ------------------------------------------------------ |
| `wwwroot/js/ocap-core.js`       | **共用引擎**（IIFE，暴露 `OcapCore` 全域物件）         |
| `wwwroot/css/ocap-flow.css`     | **共用樣式**（設計模式 + 執行模式共用）                |
| `Views/OcapDesign/Index.cshtml` | **設計模式**頁面（拖拉節點、編輯屬性、匯出/匯入 JSON） |
| `Views/Ocap/Index.cshtml`       | **執行模式**頁面（點節點前進流程、歷史時間軸）         |

---

## 二、共用引擎 `OcapCore`（`ocap-core.js`）

### 2-1 內部狀態變數

| 變數                   | 說明                                                                      |
| ---------------------- | ------------------------------------------------------------------------- |
| `nodes`                | `{ [nodeId]: { id, type, label, desc, x, y } }`                           |
| `nodeEndpoints`        | `{ [nodeId]: ep[] }` jsPlumb endpoint 陣列（順序：Top/Right/Bottom/Left） |
| `jp`                   | jsPlumb instance                                                          |
| `selectedId`           | 目前選取的節點 ID（設計模式用）                                           |
| `selectedConn`         | 目前選取的連線物件（設計模式用）                                          |
| `isRunMode`            | `true` = 執行模式，`false` = 設計模式                                     |
| `_edgeClickHandler`    | 連線點擊回呼（由頁面注入）                                                |
| `_runNodeClickHandler` | 執行模式節點點擊回呼（由頁面注入）                                        |

### 2-2 Public API

```js
OcapCore.init(canvasId, canvasWrapId, isRunMode, readyFn)
// 初始化 jsPlumb，完成後呼叫 readyFn()

OcapCore.addNode(type, label, x, y)          // 新增節點，回傳 nodeId
OcapCore.removeNode(id)                       // 刪除節點及其連線
OcapCore.clearAll()                           // 清除全部
OcapCore.selectNode(id | null)                // 選取/取消選取節點（設計模式）
OcapCore.selectConn(conn | null)              // 選取/取消選取連線（設計模式）
OcapCore.deleteSelectedConn()                 // 刪除選取連線

OcapCore.exportFlow()                         // 回傳 { nodes, edges } JSON 物件
OcapCore.importFlow(jsonString)               // 匯入 JSON 字串，重建圖

OcapCore.openJsonModal(title, content, isImport)  // 開啟 JSON modal
OcapCore.bindJsonModal(importDoneFn?)             // 綁定 modal 確定按鈕

OcapCore.setEdgeClickHandler(fn)              // fn(conn) — 連線點擊
OcapCore.setRunNodeClickHandler(fn)           // fn(nodeId) — 執行模式節點點擊

OcapCore.getJp()                              // 取得 jsPlumb instance
OcapCore.getNodes()                           // 取得 nodes 物件
OcapCore.getSelectedId()                      // 目前選取節點 ID
OcapCore.getSelectedConn()                    // 目前選取連線
OcapCore.refreshInfoBtn(id)                   // 重整 ℹ️ 按鈕顯示狀態
```

### 2-3 關鍵設計決策（不能亂改）

#### 連線點擊綁定方式

```js
// ❌ 不能用 jp.bind("click", ...) — 會阻擋執行模式節點點擊
// ✅ 正確：在 jp.bind("connection") 內，對 conn.canvas 直接 addEventListener
conn.canvas.addEventListener("click", function (e) { ... });
```

#### 執行模式節點點擊

```js
// 使用 capture phase 確保比連線點擊先觸發
canvas.addEventListener(
    "click",
    function (e) {
        if (!isRunMode) return;
        var nodeEl = e.target.closest(".ocap-node");
        if (nodeEl && _runNodeClickHandler) _runNodeClickHandler(nodeEl.id);
    },
    true
); // ← true = capture phase，不可改為 false
```

#### `.node-inner` 必須有 `position:relative`

```js
// nodeInnerHTML() 中，style="position:relative;" 必須存在
// 原因：.node-info-btn 是 position:absolute，依附於此定位
'<div class="node-inner" style="position:relative;">';
```

---

## 三、設計模式（`Views/OcapDesign/Index.cshtml`）

### 3-1 HTML 結構

```
#ocap-wrapper
├── #ocap-toolbar         ← 左側工具列（拖曳節點、操作按鈕、JSON）
├── #ocap-canvas-wrap
│   └── #ocap-canvas      ← jsPlumb 容器
└── #ocap-panel           ← 右側屬性面板
    ├── #panel-none
    ├── #panel-fields      ← 節點屬性（label, desc, id, type）
    └── #panel-conn-fields ← 連線屬性（label）
```

### 3-2 主要功能

| 功能              | 實作位置                           | 說明                                                                |
| ----------------- | ---------------------------------- | ------------------------------------------------------------------- |
| 拖曳新增節點      | `dragstart` / `drop` on canvasWrap | 從 `.node-palette` 拖至畫布，計算座標後呼叫 `OcapCore.addNode()`    |
| 套用節點屬性      | `btn-apply-props` click            | 更新 `nodes[id].label`、`nodes[id].desc`，同步更新 DOM `.node-text` |
| 套用連線標籤      | `btn-apply-conn` click             | 移除舊 overlay，有值則加新 Label overlay                            |
| 刪除選取          | `deleteSelected()`                 | 檢查是否有選取連線或節點，呼叫對應刪除函式                          |
| **Delete 鍵刪除** | `document.keydown`                 | 若 active element 不是輸入欄位，呼叫 `deleteSelected()`             |
| 匯出 JSON         | `btn-export` click                 | `OcapCore.exportFlow()` → `openJsonModal(readonly)`                 |
| 匯入 JSON         | `btn-import` click                 | `openJsonModal(import)` → `OcapCore.importFlow()`                   |

### 3-3 Delete 鍵邏輯（重要）

```js
function deleteSelected() {
    if (OcapCore.getSelectedConn()) {
        OcapCore.deleteSelectedConn();
        return;
    }
    var id = OcapCore.getSelectedId();
    if (!id) return;
    OcapCore.removeNode(id);
    OcapCore.selectNode(null);
}

document.addEventListener("keydown", function (e) {
    if (e.key !== "Delete" && e.key !== "Backspace") return;
    // 防止在輸入框中誤觸
    var tag = document.activeElement && document.activeElement.tagName;
    if (tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT") return;
    deleteSelected();
});
```

---

## 四、執行模式（`Views/Ocap/Index.cshtml`）

### 4-1 HTML 結構

```
#ocap-page                       ← flex-column 容器（高度 = 100vh - 60px）
├── #run-history-top             ← 頂部歷史時間軸（flex, overflow-x: auto）
└── #ocap-wrapper
    ├── #ocap-toolbar            ← 左側（匯入、回上一步、重設）
    └── #ocap-canvas-wrap
        └── #ocap-canvas
```

### 4-2 狀態變數

| 變數               | 型別              | 說明                                              |
| ------------------ | ----------------- | ------------------------------------------------- |
| `currentRunNodeId` | `string \| null`  | 目前所在節點 ID                                   |
| `visitedNodes`     | `string[]`        | 依序走過的節點 ID                                 |
| `visitedTimes`     | `Date[]`          | 對應每個節點的抵達時間                            |
| `visitedConns`     | `conn \| null []` | 對應每步使用的連線（首步為 null）                 |
| `visitedEdges`     | `conn[]`          | 走過的連線（用於 `conn-visited` 樣式）            |
| `autoScroll`       | `boolean`         | `true` = 點節點時自動捲動至畫面中央，預設 `false` |

### 4-3 核心函式

#### `activateNode(id, fromConn)`

-   將前一節點加 `visited-node`，目標節點加 `active-node`
-   記錄 `visitedNodes`、`visitedTimes`、`visitedConns`
-   將 `fromConn` 標記為 `conn-visited`（紫色）
-   對目前節點的出口連線：先移除 `conn-visited`，再加 `conn-active-animate`（綠色）
    -   **注意**：必須先 remove `conn-visited` 再 add `conn-active-animate`，否則迴圈流程時紫色會蓋住綠色（CSS specificity 問題）
-   若 `autoScroll = true`，捲動至目標節點

#### `goBack()`

-   pop `visitedNodes`、`visitedTimes`、`visitedConns`
-   還原樣式：前一步節點改回 `active-node`，清除其出口連線的 `conn-visited`
-   重新對前一步出口連線加 `conn-active-animate`

#### `updateHistoryBar()`

-   根據 `visitedNodes` + `visitedTimes` 渲染 `#run-history-top`
-   每個卡片 class = `hist-item hist-type-{type} hist-item-{visited|cur}`
-   時間格式：`HH:MM:SS`
-   自動 `scrollLeft = scrollWidth` 捲至最右側

#### `clearRunHighlights()`

-   移除所有 `active-node`、`visited-node`、`conn-active-animate`、`conn-visited`
-   清空所有狀態陣列
-   清空歷史時間軸

### 4-4 節點點擊邏輯

```js
OcapCore.setRunNodeClickHandler(function (nodeId) {
    if (currentRunNodeId === null) {
        activateNode(nodeId, null); // 尚未開始，任意節點啟動
        return;
    }
    // 只允許點與當前節點有連線的下一個節點
    var conns = jp.getConnections({ source: currentRunNodeId, target: nodeId });
    if (conns.length > 0) {
        activateNode(nodeId, conns[0]);
    }
    // 沒有連線 → 不反應
});
```

### 4-5 連線點擊（已停用）

```js
function handleEdgeClick(conn) {
    // 停用：僅允許點節點前進
}
OcapCore.setEdgeClickHandler(handleEdgeClick);
```

---

## 五、CSS 重點（`ocap-flow.css`）

### 5-1 顏色語意

| CSS class              | 顏色                  | 用途             |
| ---------------------- | --------------------- | ---------------- |
| `.active-node`         | 綠色 glow + pulse     | 目前所在節點     |
| `.visited-node`        | opacity 0.65 + 淡紫框 | 已走過節點       |
| `.conn-active-animate` | 綠色虛線動畫          | 目前節點出口連線 |
| `.conn-visited`        | 淡紫色 opacity 0.6    | 已走過連線       |

### 5-2 歷史時間軸顏色（按節點類型）

| class                 | 顏色                   |
| --------------------- | ---------------------- |
| `.hist-type-start`    | 綠色（`#52c41a`）      |
| `.hist-type-end`      | 紅色（`#ff4d4f`）      |
| `.hist-type-action`   | 藍色（`#1677ff`）      |
| `.hist-type-decision` | 橙色（`#fa8c16`）      |
| `.hist-item-visited`  | opacity 0.55（淡化）   |
| `.hist-item-cur`      | opacity 1 + box-shadow |

### 5-3 layout 說明

-   **設計模式**：`#ocap-wrapper { height: calc(100vh - 120px) }` 三欄（工具列 / 畫布 / 屬性面板）
-   **執行模式**：`#ocap-page { flex-direction: column }` → `#run-history-top` 置頂，`#ocap-wrapper` 佔剩餘高度；無右側屬性面板

---

## 六、節點 JSON 格式

```json
{
    "nodes": [
        {
            "id": "node_1",
            "type": "start",
            "label": "開始",
            "desc": "",
            "x": 100,
            "y": 100
        },
        {
            "id": "node_2",
            "type": "action",
            "label": "動作",
            "desc": "說明文字",
            "x": 300,
            "y": 100
        },
        {
            "id": "node_3",
            "type": "decision",
            "label": "判斷",
            "desc": "",
            "x": 500,
            "y": 100
        },
        {
            "id": "node_4",
            "type": "end",
            "label": "結束",
            "desc": "",
            "x": 700,
            "y": 100
        }
    ],
    "edges": [
        {
            "id": "con_1",
            "source": "node_1",
            "target": "node_2",
            "label": "",
            "sourceEpIdx": 1,
            "targetEpIdx": 3
        }
    ]
}
```

| type       | 形狀           |
| ---------- | -------------- |
| `start`    | 圓角膠囊，綠色 |
| `end`      | 圓角膠囊，紅色 |
| `action`   | 矩形，藍色     |
| `decision` | 菱形，橙色     |

---

## 七、常見修改指引

### 新增節點類型

1. `ocap-core.js` → `nodeInnerHTML()` 加 case（或使用預設 action 樣式）
2. `ocap-flow.css` → 加 `.node-XXX .node-inner { ... }` 樣式
3. `OcapDesign/Index.cshtml` → HTML 工具列加 `.node-palette[data-type="XXX"]`
4. `ocap-flow.css` → 加 `.hist-type-XXX` 顏色

### 修改歷史時間軸外觀

-   CSS：`#run-history-top`、`.hist-item`、`.hist-item-cur`、`.hist-item-visited`、`.hist-type-*`
-   JS：`updateHistoryBar()` 函式（`Views/Ocap/Index.cshtml`）

### 啟用/關閉自動捲動

```js
// Views/Ocap/Index.cshtml 頂部狀態變數區
var autoScroll = true; // 改為 true 啟用
```

### 重新啟用連線點擊前進

```js
// Views/Ocap/Index.cshtml — handleEdgeClick 改回：
function handleEdgeClick(conn) {
    var jp = OcapCore.getJp();
    var conns = jp.getConnections({ source: currentRunNodeId });
    if (!conns.includes(conn)) return;
    activateNode(conn.targetId, conn);
}
```

### 停用 Delete 鍵刪除（設計模式）

-   `Views/OcapDesign/Index.cshtml` → 移除 `document.addEventListener("keydown", ...)` 區塊即可
