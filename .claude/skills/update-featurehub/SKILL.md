---
name: update-featurehub
description: Mở Ezg Feature Hub và cài/cập nhật HẾT mọi thứ (tất cả .unitypackage mặc định + tất cả UPM package còn thiếu) qua Unity MCP. Dùng khi user nói "update featurehub", "cài hết gói", "install all packages", "update hết như feature hub". Tương đương bấm nút "Cài tất cả gói mặc định" + "Cài/cập nhật tất cả còn thiếu" trong Feature Hub.
---

# Update Feature Hub — cài/cập nhật hết mọi gói

Kích hoạt Feature Hub (`com.ezg.featurehub`) cài **tất cả .unitypackage mặc định** chưa cài + **tất cả UPM package** còn thiếu, qua Unity MCP. Feature Hub tự lo tải/verify sha256/import và **sống sót qua domain-reload** (nhờ `FeatureHubImportFinalizer` [InitializeOnLoad] ghi pending vào SessionState), nên không tự chế lại vòng lặp — luôn gọi chính method của window.

> Phím tắt mở Feature Hub trong Editor: **Ctrl+Shift+U**. Window class: `Ezg.FeatureHub.Editor.FeatureHubWindow` (methods private instance `InstallAllDefaults`, `InstallAllUpm`; fields `_catalog`, `_template`, `_busy`).

## Điều kiện
- Unity Editor đang mở + Unity MCP kết nối. Nếu `unity_list_instances` rỗng → báo user mở Unity rồi dừng (không fail).

## Procedure

### 1 — Chọn instance
`unity_list_instances` → nếu nhiều, `unity_select_instance` và truyền `port` cho mọi call sau.

### 2 — Đảm bảo Feature Hub mở + catalog đã load
Catalog/template load **bất đồng bộ** khi mở window; gọi install lúc `_catalog == null` sẽ im lặng return. Snippet: tìm window đang mở, nếu chưa có thì mở, rồi đợi catalog.

```csharp
// tìm window; mở nếu chưa có
UnityEditor.EditorWindow win = null;
foreach (var w in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>())
    if (w.GetType().FullName == "Ezg.FeatureHub.Editor.FeatureHubWindow") { win = w; break; }
if (win == null) {
    var wt = System.Type.GetType("Ezg.FeatureHub.Editor.FeatureHubWindow, Ezg.FeatureHub.Editor")
             ?? System.AppDomain.CurrentDomain.Load("Ezg.FeatureHub.Editor").GetType("Ezg.FeatureHub.Editor.FeatureHubWindow");
    win = UnityEditor.EditorWindow.GetWindow(wt);
}
var t = win.GetType();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var cat = t.GetField("_catalog", flags).GetValue(win);
var busy = t.GetField("_busy", flags).GetValue(win);
return "catalogLoaded=" + (cat != null) + " busy=" + busy;
```
Nếu `catalogLoaded=False` → đợi ~2–3s rồi chạy lại tới khi `True` (catalog tải xong).

### 3 — Trigger cài hết (defaults + UPM)
Gọi qua `EditorApplication.delayCall` (KHÔNG chặn MCP; các method có `EditorUtility.DisplayDialog` xác nhận — user bấm **"Cài"**). `InstallAllDefaults` lọc `installedByDefault && status != Installed`; `InstallAllUpm` ghi dependency thiếu vào `Packages/manifest.json` rồi resolve.

```csharp
UnityEditor.EditorWindow win = null;
foreach (var w in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>())
    if (w.GetType().FullName == "Ezg.FeatureHub.Editor.FeatureHubWindow") { win = w; break; }
if (win == null) return "WINDOW_CLOSED";
var t = win.GetType();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var miPkg = t.GetMethod("InstallAllDefaults", flags);
var miUpm = t.GetMethod("InstallAllUpm", flags);
win.Focus();
UnityEditor.EditorApplication.delayCall += () => { try { miPkg.Invoke(win, null); } catch (System.Exception e) { UnityEngine.Debug.LogError("[FeatureHub] " + e); } };
return "SCHEDULED InstallAllDefaults (bấm 'Cài' ở hộp thoại). Sau khi xong chạy InstallAllUpm.";
```
Báo user bấm **"Cài"** ở hộp thoại. Chạy `InstallAllUpm` tương tự (đổi `miPkg` → `miUpm`) — nên chạy **sau** khi defaults xong để tránh 2 thao tác nặng chồng nhau.

### 4 — Theo dõi + hội tụ
- `unity_console_log` xem tiến độ (`[FeatureHub] i/N ...`, "Hoàn tất N gói").
- Import gói CÓ SCRIPT gây **domain-reload** → MCP rớt ~30–60s rồi kết nối lại. Đó là bình thường; finalizer đã ghi record.
- Vì `InstallAllDefaults` luôn lọc `!= Installed`, **chạy lại bước 3 sẽ chỉ cài phần còn thiếu** → lặp tới khi console báo "Tất cả gói mặc định đã được cài" và "Tất cả UPM package đã khớp template".

## Lưu ý
- KHÔNG tự viết lại vòng cài bằng `FeatureHubService.InstallUnityPackage` trong 1 `execute_code` — closure sẽ chết theo domain-reload giữa chừng; để window + finalizer lo.
- Đây là thao tác **nặng & tải mạng** (hàng chục gói). Xác nhận scope với user nếu họ chỉ muốn 1 phần.
- Xem thêm: [[feature-hub-update-shortcut]] (Ctrl+Shift+U).
