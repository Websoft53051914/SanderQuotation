<?php
define('MOUSER_SEARCH_KEY', 'dd5b8025-4864-4d41-ad13-02a9eda26738');
define('MOUSER_CART_KEY',   'aba3c4f9-447d-4396-bf0e-a25f111e01a1');
define('MOUSER_BASE',       'https://api.mouser.com/api/v1.0');
define('DK_CLIENT_ID',     'ne2O64rACKSSbbA8mOK1MMYD3HSRAQCwIJG9XrZXp9am2veA');
define('DK_CLIENT_SECRET', '5PC64yZUghbxYXAsPCtQNly05UCOHMGj3gFiA5BTSBAtAVYfNaCUuotNq3NtafNB');

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    header('Content-Type: application/json; charset=utf-8');

    function curl_json(string $url, $body = null, bool $isGet = false, array $extra = []): array {
        $ch = curl_init($url);
        curl_setopt_array($ch, [
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_TIMEOUT        => 20,
            CURLOPT_SSL_VERIFYPEER => false,
            CURLOPT_SSL_VERIFYHOST => false,
            CURLOPT_HTTPHEADER     => array_merge(['Content-Type: application/json','Accept: application/json'], $extra),
        ] + ($isGet
            ? [CURLOPT_HTTPGET => true]
            : [CURLOPT_POST => true, CURLOPT_POSTFIELDS => $body ? json_encode($body) : '']
        ));
        $raw  = curl_exec($ch);
        $code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $err  = curl_error($ch);
        curl_close($ch);
        return ['code'=>$code,'errno'=>(int)($err!=''),'error'=>$err,
                'data'=>($raw!==false&&$raw!='') ? json_decode($raw,true) : null];
    }

    function getDKToken(): ?string {
        $ch = curl_init('https://api.digikey.com/v1/oauth2/token');
        curl_setopt_array($ch,[CURLOPT_RETURNTRANSFER=>true,CURLOPT_POST=>true,
            CURLOPT_SSL_VERIFYPEER=>false,CURLOPT_SSL_VERIFYHOST=>false,
            CURLOPT_HTTPHEADER=>['Content-Type: application/x-www-form-urlencoded'],
            CURLOPT_POSTFIELDS=>http_build_query(['grant_type'=>'client_credentials',
                'client_id'=>DK_CLIENT_ID,'client_secret'=>DK_CLIENT_SECRET])]);
        $raw = curl_exec($ch); curl_close($ch);
        return json_decode($raw,true)['access_token'] ?? null;
    }

    $action = $_POST['action'] ?? '';

    if ($action === 'mouser_search') {
        $pn  = trim($_POST['pn'] ?? '');
        $mfr = trim($_POST['mfr'] ?? '');
        $res = curl_json(MOUSER_BASE.'/search/partnumber?apiKey='.MOUSER_SEARCH_KEY,
            ['SearchByPartRequest'=>['mouserPartNumber'=>$pn,'partSearchOptions'=>'Exact']]);
        
        if (isset($res['data']['SearchResults']['Parts'])) {
            $res['data']['SearchResults']['Parts'] = array_values(array_filter(
                $res['data']['SearchResults']['Parts'],
                function($p) use ($mfr) { 
                    $hasStock = (int)($p['Availability'] ?? 0) > 0;
                    if (!$hasStock) return false;
                    if ($mfr !== '' && stripos($p['Manufacturer'] ?? '', $mfr) === false) return false;
                    return true;
                }
            ));
        }
        echo json_encode($res, JSON_UNESCAPED_UNICODE); exit;
    }

    if ($action === 'mouser_cart_price') {
        $mouserPN = trim($_POST['mouserPN'] ?? '');
        $qty      = max(1,(int)($_POST['qty'] ?? 1));
        $cartKey  = trim($_POST['cartKey'] ?? '');
        $url = MOUSER_BASE.'/cart/items/insert?apiKey='.MOUSER_CART_KEY.'&countryCode=US';
        if ($cartKey !== '') $url .= '&cartKey='.urlencode($cartKey);
        $res = curl_json($url,['CartItems'=>[['MouserPartNumber'=>$mouserPN,'Quantity'=>$qty,'CustomerPartNumber'=>'']]]);
        echo json_encode($res, JSON_UNESCAPED_UNICODE); exit;
    }

    if ($action === 'mouser_cart_remove') {
        $mouserPN = trim($_POST['mouserPN'] ?? '');
        $cartKey  = trim($_POST['cartKey'] ?? '');
        $res = curl_json(MOUSER_BASE.'/cart/item/remove?apiKey='.MOUSER_CART_KEY
            .'&cartKey='.urlencode($cartKey).'&mouserPartNumber='.urlencode($mouserPN), null, false);
        echo json_encode($res, JSON_UNESCAPED_UNICODE); exit;
    }

    if ($action === 'dk_search') {
        $pn    = trim($_POST['pn'] ?? '');
        $mfr   = trim($_POST['mfr'] ?? '');
        $token = getDKToken();
        if (!$token) { echo json_encode(['errno'=>1,'error'=>'Token 取得失敗'],JSON_UNESCAPED_UNICODE); exit; }
        $h = [
            'Authorization: Bearer '.$token,
            'X-DIGIKEY-Client-Id: '.DK_CLIENT_ID,
            'X-DIGIKEY-Locale-Site: US',
            'X-DIGIKEY-Locale-Currency: USD',
            'X-DIGIKEY-Locale-Language: en',
            'X-DIGIKEY-Customer-Id: ',
        ];
        $s = curl_json('https://api.digikey.com/products/v4/search/keyword',
            ['Keywords'=>$pn,'RecordCount'=>20,'RecordStartPosition'=>0,'MarketPlaceOptions'=>'IncludeMarketPlace'],
            false, $h);
        $r = $s['data'] ?? [];
        $exact = $r['ExactMatches'] ?? [];
        $prods = count($exact) > 0 ? $exact : ($r['Products'] ?? []);
        
        $prods = array_values(array_filter($prods, function($p) use ($mfr) { 
            $hasStock = ($p['QuantityAvailable'] ?? 0) > 0;
            if (!$hasStock) return false;
            if ($mfr !== '' && stripos($p['Manufacturer']['Name'] ?? '', $mfr) === false) return false;
            return true; 
        }));
        
        $final = [];
        foreach ($prods as $p) {
            $dkn = $p['ProductVariations'][0]['DigiKeyProductNumber'] ?? null;
            if (!$dkn) { $final[] = $p; continue; }
            $d = curl_json('https://api.digikey.com/products/v4/search/'.rawurlencode($dkn).'/productdetails',null,true,$h);
            $final[] = !empty($d['data']['Product']) ? $d['data']['Product'] : $p;
        }
        echo json_encode(['errno'=>0,'code'=>200,'products'=>$final],JSON_UNESCAPED_UNICODE|JSON_PRETTY_PRINT); exit;
    }

    echo json_encode(['error'=>'unknown action']); exit;
}
?>
<!doctype html>
<html lang="zh-Hant">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>零件比價工具</title>
<style>
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap');

*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
body{font-family:'Inter','Segoe UI',sans-serif;background:#f4f6f9;color:#1e293b;min-height:100vh;padding:40px 24px 80px}

/* ── Topbar ── */
.topbar{max-width:1200px;margin:0 auto 32px;display:flex;align-items:center;gap:16px}
.logo{width:48px;height:48px;border-radius:14px;flex-shrink:0;
  background:linear-gradient(135deg,#2563eb 0%,#0ea5e9 100%);
  display:flex;align-items:center;justify-content:center;font-size:24px;
  box-shadow:0 8px 16px rgba(37,99,235,.2)}
.topbar h1{font-size:24px;font-weight:800;letter-spacing:-.5px;color:#0f172a}
.topbar p{font-size:13px;color:#64748b;margin-top:4px;font-weight:500}

/* ── Search card ── */
.search-card{max-width:1200px;margin:0 auto 32px;background:#fff;
  border-radius:20px;padding:28px 32px;
  box-shadow:0 4px 20px rgba(0,0,0,.04),0 1px 4px rgba(0,0,0,.02);
  border:1px solid #e2e8f0; display:flex; flex-direction:column; gap:20px;}
.search-inputs {display:flex; gap:16px; flex-wrap:wrap;}
.input-group {flex:1; min-width:200px; position:relative;}
.input-group.qty-group {flex:0 0 180px; min-width:140px;}
.input-label {position:absolute; top:-10px; left:12px; background:#fff; padding:0 6px; font-size:12px; font-weight:600; color:#64748b;}
.search-input{width:100%; padding:14px 20px; font-size:15px;
  font-family:'Courier New',monospace; font-weight:600;
  border:2px solid #e2e8f0; border-radius:12px; outline:none;
  background:#f8fafc; color:#0f172a; transition:all .2s ease}
.search-input::placeholder{color:#94a3b8; font-weight:500; font-family:'Inter', sans-serif;}
.search-input:focus{border-color:#3b82f6; box-shadow:0 0 0 4px rgba(59,130,246,.15); background:#fff}

/* 目標數量欄位特殊樣式 */
.input-group.qty-group .input-label{color:#7c3aed}
.input-group.qty-group .search-input{border-color:#c4b5fd; background:#faf5ff}
.input-group.qty-group .search-input:focus{border-color:#7c3aed; box-shadow:0 0 0 4px rgba(124,58,237,.15); background:#fff}

.qty-hint{font-size:11px; color:#94a3b8; margin-top:6px; padding-left:4px; font-weight:500;}
.qty-hint b{color:#7c3aed}

.search-actions {display:flex; gap:12px; flex-wrap:wrap; align-items:center;}
.btn{padding:12px 24px; font-size:14px; font-weight:700; border:none; border-radius:12px;
  cursor:pointer; transition:all .2s; white-space:nowrap; display:flex; align-items:center; gap:8px;}
.btn:active{transform:scale(.96)}
.btn-m  {background:#0a8c5a; color:#fff; box-shadow:0 4px 12px rgba(10,140,90,.2)}
.btn-m:hover{background:#087048; transform:translateY(-1px);}
.btn-dk {background:#cc0000; color:#fff; box-shadow:0 4px 12px rgba(204,0,0,.2)}
.btn-dk:hover{background:#a30000; transform:translateY(-1px);}
.btn-all{background:#2563eb; color:#fff; box-shadow:0 4px 12px rgba(37,99,235,.25); margin-left:auto;}
.btn-all:hover{background:#1d4ed8; transform:translateY(-1px);}

@media (max-width: 600px) {
    .btn-all {margin-left:0; width:100%; justify-content:center;}
    .search-actions .btn {flex:1; justify-content:center;}
    .input-group.qty-group {flex:1 0 100%;}
}

/* ── Status ── */
.status-bar{max-width:1200px; margin:0 auto 16px; font-size:14px; color:#64748b; font-weight:500; min-height:22px}

/* ── Results ── */
.results{max-width:1200px; margin:0 auto}

/* ── Section header ── */
.section-hd{display:flex; align-items:center; gap:12px; margin:28px 0 16px}
.section-dot{width:12px; height:12px; border-radius:50%; flex-shrink:0}
.section-name{font-size:13px; font-weight:800; letter-spacing:.1em; text-transform:uppercase}
.section-hd.m  .section-dot{background:#0a8c5a; box-shadow: 0 0 8px rgba(10,140,90,.5)}
.section-hd.m  .section-name{color:#0a8c5a}
.section-hd.dk .section-dot{background:#cc0000; box-shadow: 0 0 8px rgba(204,0,0,.5)}
.section-hd.dk .section-name{color:#cc0000}
.section-count{font-size:12px; color:#64748b; font-weight:600;
  background:#e2e8f0; padding:3px 10px; border-radius:20px}

/* ── 目標數量標籤 ── */
.qty-mode-badge{font-size:12px; font-weight:700; padding:3px 12px; border-radius:20px;
  background:#ede9fe; color:#6d28d9; border:1px solid #c4b5fd;}

/* ── Product card ── */
.product-card{background:#fff; border:1px solid #e2e8f0; border-radius:16px;
  margin-bottom:20px; overflow:hidden;
  box-shadow:0 2px 8px rgba(0,0,0,.02),0 8px 24px rgba(0,0,0,.03);
  transition:transform .2s ease, box-shadow .2s ease}
.product-card:hover{transform:translateY(-2px); box-shadow:0 4px 12px rgba(0,0,0,.04),0 12px 32px rgba(0,0,0,.06)}

/* ── Card header ── */
.product-header{display:flex; align-items:center; gap:20px;
  padding:20px 24px; background:#f8fafc; border-bottom:1px solid #e2e8f0}
.product-header img{width:64px; height:64px; object-fit:contain;
  border:1px solid #e2e8f0; border-radius:10px; background:#fff; padding:4px; flex-shrink:0}
.product-title{flex:1; min-width:0}
.product-title .mpn{font-size:17px; font-weight:800; margin-bottom:4px; font-family:'Courier New', monospace;}
.mpn.m {color:#0a8c5a}
.mpn.dk{color:#cc0000}
.product-title .mfr {font-size:13px; color:#475569; font-weight:600; margin-bottom:4px}
.product-title .desc{font-size:13px; color:#64748b; line-height:1.5}
.product-meta{text-align:right; flex-shrink:0}
.unit-price{font-size:24px; font-weight:800; color:#0f172a; line-height:1; font-family:'Courier New', monospace;}

/* ── Badges ── */
.stock-badge{display:inline-block; font-size:12px; font-weight:700;
  padding:4px 12px; border-radius:20px; margin-top:8px}
.stock-ok{background:#dcfce7; color:#166534; border:1px solid #bbf7d0}
.stock-no{background:#fee2e2; color:#991b1b; border:1px solid #fecaca}
.marketplace-badge{display:inline-block; background:#f59e0b; color:#fff;
  font-size:11px; font-weight:700; padding:3px 10px; border-radius:12px;
  margin-top:6px; letter-spacing:.02em}
.tariff-warn{display:inline-block; background:#fef3c7; color:#92400e;
  border:1px solid #fde68a; border-radius:6px; padding:2px 8px;
  font-size:12px; font-weight:700; margin-left:6px}
.pkg-label{display:inline-block; background:#e0f2fe; color:#0369a1;
  border:1px solid #bae6fd; border-radius:6px; padding:3px 10px;
  font-size:12px; font-weight:700}

/* ── 目標數量提示列 ── */
.qty-target-info{background:#faf5ff; border:1px solid #e9d5ff; border-radius:10px;
  padding:10px 16px; margin-bottom:12px; font-size:13px; color:#6d28d9; font-weight:600;
  display:flex; align-items:center; gap:8px;}
.qty-target-info .break-applied{color:#059669; font-family:'Courier New',monospace; font-weight:800;}
.qty-target-info .actual-qty{color:#0f172a; font-family:'Courier New',monospace; font-weight:800;}

/* ── Variations ── */
.variations{padding:16px 24px 8px}
.variation-block{margin-bottom:24px}
.variation-meta{font-size:13px; color:#475569; margin-bottom:12px;
  display:flex; align-items:center; gap:10px; flex-wrap:wrap}
.variation-meta b{color:#0f172a; font-weight:700}

/* ── Price table ── */
table{width:100%; border-collapse:collapse; font-size:13px; border-radius:8px; overflow:hidden;}
thead tr{background:#f1f5f9}
th{padding:10px 14px; text-align:left; font-size:12px; font-weight:700; color:#475569;
  letter-spacing:.05em; text-transform:uppercase; border-bottom:2px solid #e2e8f0}
th.col-my  {background:#ecfdf5; color:#065f46}
th.col-cart{background:#eff6ff; color:#1e40af}
td{padding:10px 14px; border-bottom:1px solid #f1f5f9; vertical-align:middle}
tr:last-child td{border-bottom:none}
tr:hover td{background:#f8fafc}

/* 目標數量模式下，highlight 該列 */
tr.highlight-row td{background:#fdf4ff !important; border-left:3px solid #7c3aed;}

.td-qty {font-family:'Courier New',monospace; font-size:13px; color:#64748b; font-weight:700}
.td-std {font-family:'Courier New',monospace; font-size:13px; color:#94a3b8; text-decoration:line-through}
.td-tot {font-family:'Courier New',monospace; font-size:13px; color:#334155; font-weight:600}
.td-my  {font-family:'Courier New',monospace; font-size:14px; font-weight:800; color:#16a34a}
.td-my-tot{font-family:'Courier New',monospace; font-size:13px; font-weight:700; color:#16a34a}

.td-cart-green  {font-family:'Courier New',monospace; font-size:14px; font-weight:800; color:#0a8c5a}
.td-cart-same   {font-family:'Courier New',monospace; font-size:14px; color:#475569; font-weight:600}
.td-cart-loading{font-size:13px; color:#94a3b8}
.td-cart-error  {font-size:12px; color:#ef4444; font-weight:600}
.td-tot-green   {font-family:'Courier New',monospace; font-size:13px; font-weight:700; color:#0a8c5a}
.td-tot-same    {font-family:'Courier New',monospace; font-size:13px; color:#64748b}

/* ── Card links ── */
.card-links{padding:8px 24px 20px; display:flex; gap:12px; flex-wrap:wrap}
.card-link{font-size:13px; color:#2563eb; text-decoration:none; font-weight:600;
  padding:6px 14px; border-radius:8px; border:1px solid #bfdbfe;
  background:#eff6ff; transition:all .2s; display:inline-flex; align-items:center;}
.card-link:hover{background:#dbeafe; transform:translateY(-1px);}
.ds-link{display:inline-flex; align-items:center; margin-top:8px; font-size:12px; color:#2563eb; font-weight:700;
  text-decoration:none; padding:4px 12px; border:1px solid #bfdbfe;
  border-radius:6px; background:#eff6ff; transition:all .2s;}
.ds-link:hover{background:#dbeafe;}

/* ── Placeholder ── */
.placeholder{background:#fff; border:1px dashed #cbd5e1; border-radius:16px;
  padding:32px 24px; color:#64748b; font-size:14px; font-weight:500; text-align:center;}

/* ── Spinner ── */
.spin{display:inline-block; width:14px; height:14px;
  border:2px solid #e2e8f0; border-top-color:#3b82f6;
  border-radius:50%; animation:sp .65s linear infinite;
  vertical-align:middle; margin-right:8px}
@keyframes sp{to{transform:rotate(360deg)}}
</style>
</head>
<body>

<div class="topbar">
  <div class="logo">🔍</div>
  <div>
    <h1>零件即時比價系統</h1>
    <p>Mouser × DigiKey &nbsp;—&nbsp; USD / US Market</p>
  </div>
</div>

<div class="search-card">
  <div class="search-inputs">
    <div class="input-group">
      <label class="input-label">搜尋料號</label>
      <input class="search-input" type="text" id="pn" placeholder="例如: 1N4148 / TMP36GT9Z">
    </div>
    <div class="input-group">
      <label class="input-label">製造商篩選 (選填)</label>
      <input class="search-input" type="text" id="mfr" placeholder="例如: Texas Instruments 或 TI">
    </div>
    <div class="input-group qty-group">
      <label class="input-label">🎯 目標數量 (選填)</label>
      <input class="search-input" type="number" id="targetQty" placeholder="例如: 50" min="1">
      <div class="qty-hint">留空 → 顯示所有 break<br>填入 → 自動抓 <b>≤ 目標</b> 的最大 break</div>
    </div>
  </div>
  
  <div class="search-actions">
    <button class="btn btn-m"   onclick="doSearch('mouser')">🟢 搜尋 Mouser</button>
    <button class="btn btn-dk"  onclick="doSearch('dk')">🔴 搜尋 DigiKey</button>
    <button class="btn btn-all" onclick="doSearch('both')">⚡ 全部搜尋</button>
  </div>
</div>

<div class="status-bar" id="status"></div>
<div class="results" id="results"></div>

<script>
const sleep = ms => new Promise(r => setTimeout(r, ms));
let sessionCartKey = '';

function post(payload) {
  const fd = new FormData();
  for (const [k,v] of Object.entries(payload)) fd.append(k,v);
  return fetch(location.href,{method:'POST',body:fd}).then(r=>r.json());
}
function setStatus(msg){ document.getElementById('status').innerHTML = msg; }

// 取得目標數量（0 = 未設定，顯示全部）
function getTargetQty() {
  const v = parseInt(document.getElementById('targetQty').value);
  return isNaN(v) || v < 1 ? 0 : v;
}

// 從 price breaks 陣列中，找到 ≤ targetQty 的最大 break
// breaks 格式: [{Quantity, Price, ...}] (Mouser) 或 [{BreakQuantity, UnitPrice, ...}] (DK)
// qtyKey: 'Quantity' or 'BreakQuantity'
function findApplicableBreak(breaks, targetQty, qtyKey) {
  if (!breaks || !breaks.length) return null;
  if (!targetQty) return null; // 0 = 全部顯示
  let best = null;
  for (const b of breaks) {
    const bq = parseInt(b[qtyKey]) || 0;
    if (bq <= targetQty) best = b;
    else break; // 假設已排序
  }
  return best; // null 代表目標數量連最小 break 都不到
}

async function doSearch(mode) {
  const pn  = document.getElementById('pn').value.trim();
  const mfr = document.getElementById('mfr').value.trim();
  const tq  = getTargetQty();
  if (!pn) { setStatus('⚠️ 請先輸入料號'); return; }
  
  sessionCartKey = '';
  document.getElementById('results').innerHTML = '';
  const qtyLabel = tq ? ` &nbsp;<span class="qty-mode-badge">🎯 目標數量 ${tq.toLocaleString()} pcs</span>` : '';
  setStatus(`<span class="spin"></span>正在查詢料號 <b>${pn}</b>${mfr ? ` (限定製造商: ${mfr})` : ''}${qtyLabel} ...`);
  
  const tasks = [];
  if (mode==='mouser'||mode==='both') tasks.push(runMouser(pn, mfr, tq));
  if (mode==='dk'    ||mode==='both') tasks.push(runDK(pn, mfr, tq));
  
  await Promise.all(tasks);
  setStatus('✅ 查詢完成' + (tq ? ` &nbsp;<span class="qty-mode-badge">🎯 目標數量 ${tq.toLocaleString()} pcs</span>` : ''));
}

// ══════════════ MOUSER ══════════════
async function runMouser(pn, mfr, targetQty) {
  const container = document.getElementById('results');
  const sec = document.createElement('div');
  sec.id = 'mouser-section';
  sec.innerHTML = `
    <div class="section-hd m">
      <div class="section-dot"></div>
      <span class="section-name">Mouser Electronics</span>
    </div>
    <div id="m-cards"><div class="placeholder"><span class="spin"></span>正在與 Mouser 連線同步資料…</div></div>`;
  container.appendChild(sec);

  try {
    const r     = await post({action:'mouser_search', pn, mfr});
    const cards = document.getElementById('m-cards');
    if (r.errno!==0)              { cards.innerHTML=`<div class="placeholder">連線失敗：${r.error}</div>`; return; }
    if (r.data?.Errors?.length)   { cards.innerHTML=`<div class="placeholder">${r.data.Errors.map(e=>e.Message).join('；')}</div>`; return; }
    const parts = r.data?.SearchResults?.Parts ?? [];
    if (!parts.length) { cards.innerHTML=`<div class="placeholder">查無符合條件或有庫存的料件</div>`; return; }
    sec.querySelector('.section-hd').insertAdjacentHTML('beforeend',`<span class="section-count">${parts.length} 筆結果</span>`);
    cards.innerHTML = parts.map((p,i)=>buildMouserCard(p,i,targetQty)).join('');
    for (let i=0;i<parts.length;i++) await fetchMouserCartPrices(parts[i],i,targetQty);
  } catch(e) {
    const c=document.getElementById('m-cards'); if(c) c.innerHTML=`<div class="placeholder">錯誤：${e.message}</div>`;
  }
}

function buildMouserCard(p, ci, targetQty) {
  const breaks = p.PriceBreaks ?? [];
  const avail  = parseInt(p.Availability)||0;
  const cur    = breaks[0]?.Currency||'USD';
  const dsLink = p.DataSheetUrl ? `<a class="ds-link" href="${p.DataSheetUrl}" target="_blank">📄 Datasheet</a>` : '';

  let tbl = '<p style="font-size:13px;color:#94a3b8;padding:8px 0">無價格資訊</p>';
  if (breaks.length) {
    // 判斷是否啟用目標數量模式
    const applicable = findApplicableBreak(breaks, targetQty, 'Quantity');
    
    // 目標數量模式：只顯示符合的那個 break
    // 全部模式：顯示全部 break
    const displayBreaks = targetQty
      ? (applicable ? [applicable] : [])
      : breaks;

    let qtyInfo = '';
    if (targetQty) {
      if (applicable) {
        const breakQty = parseInt(applicable.Quantity);
        qtyInfo = `<div class="qty-target-info">
          🎯 目標數量 <span class="actual-qty">${targetQty.toLocaleString()}</span> pcs
          → 適用 break：<span class="break-applied">${breakQty.toLocaleString()} pcs</span> 的單價
          &nbsp;·&nbsp; 計算以實際採購量 <span class="actual-qty">${targetQty.toLocaleString()}</span> pcs
        </div>`;
      } else {
        qtyInfo = `<div class="qty-target-info" style="background:#fff7ed;border-color:#fed7aa;color:#c2410c;">
          ⚠️ 目標數量 ${targetQty.toLocaleString()} pcs 低於最小 break (${parseInt(breaks[0]?.Quantity||0).toLocaleString()} pcs)，請提高數量或確認 MOQ
        </div>`;
      }
    }

    if (!displayBreaks.length) {
      tbl = qtyInfo + '<p style="font-size:13px;color:#94a3b8;padding:8px 0">無符合的價格區間</p>';
    } else {
      const rows = displayBreaks.map((b) => {
        const breakQty = parseInt(b.Quantity);
        // 實際計算：使用目標數量（如有），否則用 break qty
        const calcQty  = targetQty || breakQty;
        const listP    = parseFloat(b.Price.replace(/[^0-9.]/g,''))||0;
        const rowClass = targetQty ? 'class="highlight-row"' : '';
        return `<tr ${rowClass}>
          <td class="td-qty">${breakQty.toLocaleString()}${targetQty ? ` <span style="color:#7c3aed;font-size:11px">(買 ${targetQty.toLocaleString()})</span>` : ''}</td>
          <td class="td-std">${cur} ${b.Price}</td>
          <td class="td-tot">${cur} ${(listP*calcQty).toFixed(2)}</td>
          <td class="td-cart-loading" id="mc-${ci}-0"><span class="spin"></span></td>
          <td class="td-tot-same"     id="mct-${ci}-0">—</td>
        </tr>`;
      }).join('');
      tbl = qtyInfo + `<table>
        <thead><tr>
          <th>Break 數量</th><th>定價</th><th>定價總計</th>
          <th class="col-cart">🛒 實際下單價</th><th class="col-cart">實際總計</th>
        </tr></thead><tbody>${rows}</tbody></table>`;
    }
  }

  return `<div class="product-card">
    <div class="product-header">
      <div class="product-title">
        <div class="mpn m">${p.ManufacturerPartNumber??''}</div>
        <div class="mfr">${p.Manufacturer??''} &nbsp;·&nbsp; ${p.MouserPartNumber??''}</div>
        <div class="desc">${p.Description??''}</div>
        ${dsLink}
      </div>
      <div class="product-meta">
        <div class="unit-price">${cur} ${breaks[0]?.Price??'-'}</div>
        <div><span class="stock-badge stock-ok">${avail.toLocaleString()} pcs</span></div>
      </div>
    </div>
    <div class="variations"><div class="variation-block">${tbl}</div></div>
  </div>`;
}

async function fetchMouserCartPrices(p, ci, targetQty) {
  const breaks   = p.PriceBreaks??[];
  const mouserPN = p.MouserPartNumber??'';
  if (!breaks.length) return;

  // 找到適用的 break
  const applicable = targetQty
    ? findApplicableBreak(breaks, targetQty, 'Quantity')
    : null; // null = 全部模式

  if (targetQty) {
    // 目標數量模式：只查一次，用目標數量下單（以實際購買量查詢，得到正確價格）
    const cell  = document.getElementById(`mc-${ci}-0`);
    const cellT = document.getElementById(`mct-${ci}-0`);
    if (!cell) return;
    if (!applicable) { cell.className='td-cart-error'; cell.textContent='N/A'; cellT.textContent='—'; return; }

    const cur    = applicable.Currency || 'USD';
    const listPx = parseFloat(applicable.Price.replace(/[^0-9.]/g,''))||0;
    // 用實際目標數量下單，才能取得正確的階梯價
    const qty    = targetQty;

    try {
      await sleep(300);
      const r      = await post({action:'mouser_cart_price', mouserPN, qty, cartKey:sessionCartKey});
      const newKey = r.data?.CartKey??r.data?.cartKey??'';
      if (newKey) sessionCartKey = newKey;
      const matched = (r.data?.CartItems??[]).find(i=>(i.MouserPartNumber??'').toLowerCase()===mouserPN.toLowerCase());
      if (matched) {
        const raw = matched.UnitPrice??matched.Price??matched.ExtendedPrice??null;
        if (raw!==null) {
          const cp  = parseFloat(String(raw).replace(/[^0-9.]/g,''))||0;
          const tot = (cp*qty).toFixed(2);
          if (listPx-cp>0.001) {
            cell.className='td-cart-green';  cell.textContent=`${cur} ${cp.toFixed(4)}`;
            cellT.className='td-tot-green'; cellT.textContent=`${cur} ${tot}`;
          } else {
            cell.className='td-cart-same';  cell.textContent=`${cur} ${cp.toFixed(4)}`;
            cellT.className='td-tot-same'; cellT.textContent=`${cur} ${tot}`;
          }
        } else { cell.className='td-cart-error'; cell.textContent='無法讀取'; cellT.textContent='—'; }
      } else {
        const apiErr=r.data?.Errors??[];
        cell.className='td-cart-error';
        cell.textContent=apiErr.length?apiErr[0].Message:'未找到料件';
        cellT.textContent='—';
      }
    } catch(e) { cell.className='td-cart-error'; cell.textContent='錯誤：'+e.message; cellT.textContent='—'; }

    if (sessionCartKey) {
      try { await sleep(300); await post({action:'mouser_cart_remove', mouserPN, cartKey:sessionCartKey}); }
      catch(e) { console.warn('cart_remove:',e.message); }
    }

  } else {
    // 全部模式（原本邏輯）：逐一查每個 break 的數量
    for (let ti=0;ti<breaks.length;ti++) {
      const b      = breaks[ti];
      const qty    = parseInt(b.Quantity);
      const listPx = parseFloat(b.Price.replace(/[^0-9.]/g,''))||0;
      const cur    = b.Currency||'USD';
      const cell   = document.getElementById(`mc-${ci}-${ti}`);
      const cellT  = document.getElementById(`mct-${ci}-${ti}`);
      if (!cell) continue;
      try {
        await sleep(600);
        const r      = await post({action:'mouser_cart_price', mouserPN, qty, cartKey:sessionCartKey});
        const newKey = r.data?.CartKey??r.data?.cartKey??'';
        if (newKey) sessionCartKey = newKey;
        const matched = (r.data?.CartItems??[]).find(i=>(i.MouserPartNumber??'').toLowerCase()===mouserPN.toLowerCase());
        if (matched) {
          const raw    = matched.UnitPrice??matched.Price??matched.ExtendedPrice??null;
          if (raw!==null) {
            const cp = parseFloat(String(raw).replace(/[^0-9.]/g,''))||0;
            const tot = (cp*qty).toFixed(2);
            if (listPx-cp>0.001) {
              cell.className='td-cart-green';  cell.textContent=`${cur} ${cp.toFixed(4)}`;
              cellT.className='td-tot-green'; cellT.textContent=`${cur} ${tot}`;
            } else {
              cell.className='td-cart-same';  cell.textContent=`${cur} ${cp.toFixed(4)}`;
              cellT.className='td-tot-same'; cellT.textContent=`${cur} ${tot}`;
            }
          } else { cell.className='td-cart-error';cell.textContent='無法讀取';cellT.textContent='—'; }
        } else {
          const apiErr=r.data?.Errors??[];
          cell.className='td-cart-error';
          cell.textContent=apiErr.length?apiErr[0].Message:'未找到料件';
          cellT.textContent='—';
        }
      } catch(e) { cell.className='td-cart-error';cell.textContent='錯誤：'+e.message;cellT.textContent='—'; }
    }
    if (sessionCartKey) {
      try { await sleep(400); await post({action:'mouser_cart_remove', mouserPN, cartKey:sessionCartKey}); }
      catch(e) { console.warn('cart_remove:',e.message); }
    }
  }
}

// ══════════════ DIGIKEY ══════════════
async function runDK(pn, mfr, targetQty) {
  const container = document.getElementById('results');
  const sec = document.createElement('div');
  sec.id = 'dk-section';
  sec.innerHTML = `
    <div class="section-hd dk">
      <div class="section-dot"></div>
      <span class="section-name">DigiKey Electronics</span>
    </div>
    <div id="dk-cards"><div class="placeholder"><span class="spin"></span>正在與 DigiKey 連線同步資料…</div></div>`;
  container.appendChild(sec);

  try {
    const r     = await post({action:'dk_search', pn, mfr});
    const cards = document.getElementById('dk-cards');
    if (r.errno!==0) { cards.innerHTML=`<div class="placeholder">連線失敗：${r.error}</div>`; return; }
    const products = r.products??[];
    if (!products.length) { cards.innerHTML=`<div class="placeholder">查無符合條件或目前無庫存的料件</div>`; return; }
    sec.querySelector('.section-hd').insertAdjacentHTML('beforeend',`<span class="section-count">${products.length} 筆結果</span>`);
    cards.innerHTML = products.map(p=>renderDKProduct(p, targetQty)).join('');
  } catch(e) {
    const c=document.getElementById('dk-cards'); if(c) c.innerHTML=`<div class="placeholder">錯誤：${e.message}</div>`;
  }
}

function getApplicablePrice(arr, qty) {
  if (!arr?.length) return null;
  let res=null;
  for (const pr of arr) { if (pr.BreakQuantity<=qty) res=pr; else break; }
  return res;
}

function renderDKProduct(p, targetQty) {
  const qty      = p.QuantityAvailable??0;
  const isMarket = p.ProductVariations?.some(v=>v.MarketPlace);
  const visVars  = (p.ProductVariations??[]).filter(v=>(v.QuantityAvailableforPackageType??0)>0);

  const varsHTML = visVars.map(v => {
    const std    = v.StandardPricing??[];
    const my     = v.MyPricing??[];
    const hasMy  = my.length>0;

    let qtyInfo = '';
    let rows    = '';

    if (targetQty) {
      // ── 目標數量模式 ──
      const sp = getApplicablePrice(std, targetQty);
      const mp = getApplicablePrice(my,  targetQty);
      const stdP = sp ? sp.UnitPrice : null;
      const myP  = mp ? mp.UnitPrice : null;
      const breakQty = sp ? sp.BreakQuantity : (my[0]?.BreakQuantity ?? null);

      if (!sp && !mp) {
        qtyInfo = `<div class="qty-target-info" style="background:#fff7ed;border-color:#fed7aa;color:#c2410c;">
          ⚠️ 目標數量 ${targetQty.toLocaleString()} pcs 低於最小 break (${(std[0]?.BreakQuantity||my[0]?.BreakQuantity||'?').toLocaleString()} pcs)
        </div>`;
      } else {
        qtyInfo = `<div class="qty-target-info">
          🎯 目標數量 <span class="actual-qty">${targetQty.toLocaleString()}</span> pcs
          → 適用 break：<span class="break-applied">${(breakQty??'?').toLocaleString()} pcs</span> 的單價
          &nbsp;·&nbsp; 總計以 <span class="actual-qty">${targetQty.toLocaleString()}</span> pcs 計算
        </div>`;
        rows = `<tr class="highlight-row">
          <td class="td-qty">${(breakQty??'-').toLocaleString()} <span style="color:#7c3aed;font-size:11px">(買 ${targetQty.toLocaleString()})</span></td>
          <td class="td-std">${stdP!=null?`$${stdP.toFixed(5)}`:'-'}</td>
          <td class="td-tot">${stdP!=null?`$${(targetQty*stdP).toFixed(3)}`:'-'}</td>
          ${hasMy?`
          <td class="td-my">${myP!=null?`$${myP.toFixed(5)}`:'-'}</td>
          <td class="td-my-tot">${myP!=null?`$${(targetQty*myP).toFixed(3)}`:'-'}</td>`:''}
        </tr>`;
      }
    } else {
      // ── 全部模式（原本邏輯）──
      const allBreaks = Array.from(new Set([...std,...my].map(pr=>pr.BreakQuantity))).sort((a,b)=>a-b);
      rows = allBreaks.map(bq => {
        const sp   = getApplicablePrice(std,bq);
        const mp   = getApplicablePrice(my, bq);
        const stdP = sp ? sp.UnitPrice : null;
        const myP  = mp ? mp.UnitPrice : null;
        return `<tr>
          <td class="td-qty">${bq.toLocaleString()}</td>
          <td class="td-std">${stdP!=null?`$${stdP.toFixed(5)}`:'-'}</td>
          <td class="td-tot">${stdP!=null?`$${(bq*stdP).toFixed(3)}`:'-'}</td>
          ${hasMy?`
          <td class="td-my">${myP!=null?`$${myP.toFixed(5)}`:'-'}</td>
          <td class="td-my-tot">${myP!=null?`$${(bq*myP).toFixed(3)}`:'-'}</td>`:''}
        </tr>`;
      }).join('');
    }

    const tariff   = v.TariffActive?`<span class="tariff-warn">⚠ 關稅 (Tariff)</span>`:'';
    const mp_badge = v.MarketPlace?`<span class="marketplace-badge">Marketplace</span>`:'';
    const thead    = hasMy
      ? `<tr><th>Break 數量</th><th>標準單價</th><th>標準總價</th><th class="col-my">✨ 合約單價</th><th class="col-my">合約總價</th></tr>`
      : `<tr><th>Break 數量</th><th>單價 (USD)</th><th>總價</th></tr>`;

    return `<div class="variation-block">
      <div class="variation-meta">
        <span class="pkg-label">📦 ${v.PackageType?.Name??'-'}</span>
        ${tariff}${mp_badge}
        <span>可用庫存：<b>${(v.QuantityAvailableforPackageType??0).toLocaleString()}</b></span>
        <span>最低訂購量：<b>${v.MinimumOrderQuantity??'-'}</b></span>
        <span style="color:#94a3b8">DK# ${v.DigiKeyProductNumber??'-'}</span>
      </div>
      ${qtyInfo}
      ${rows ? `<table><thead>${thead}</thead><tbody>${rows}</tbody></table>` : ''}
    </div>`;
  }).join('');

  return `<div class="product-card">
    <div class="product-header">
      <img src="${p.PhotoUrl||''}" alt="" onerror="this.style.display='none'">
      <div class="product-title">
        <div class="mpn dk">${p.ManufacturerProductNumber??''}</div>
        <div class="mfr">${p.Manufacturer?.Name??''}</div>
        <div class="desc">${p.Description?.DetailedDescription??p.Description?.ProductDescription??''}</div>
        ${p.DatasheetUrl?`<a class="ds-link" href="${p.DatasheetUrl}" target="_blank">📄 Datasheet</a>`:''}
      </div>
      <div class="product-meta">
        <div class="unit-price">$${(p.UnitPrice??0).toFixed(4)}</div>
        <div><span class="stock-badge ${qty>0?'stock-ok':'stock-no'}">${qty>0?qty.toLocaleString()+' pcs':'無庫存'}</span></div>
        ${isMarket?'<div><span class="marketplace-badge">Marketplace</span></div>':''}
      </div>
    </div>
    <div class="variations">${varsHTML}</div>
    <div class="card-links">
      ${p.ProductUrl  ?`<a class="card-link" href="${p.ProductUrl}"   target="_blank">🔗 DigiKey 商品頁面</a>`:''}
      ${p.DatasheetUrl?`<a class="card-link" href="${p.DatasheetUrl}" target="_blank">📄 官方技術文件 (Datasheet)</a>`:''}
    </div>
  </div>`;
}

// 支援 Enter 鍵搜尋
document.querySelectorAll('.search-input').forEach(input => {
    input.addEventListener('keydown', e => { if(e.key==='Enter') doSearch('both'); });
});
</script>
</body>
</html>