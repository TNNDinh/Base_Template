---
name: featurehub
description: Version hoá & publish "bộ base" (package/asset loadout mà Feature Hub cài) lên GIT CỦA BẠN (origin), theo nhánh folder-style (featurehub/<ver>, giống release/… feature/…). Đây là nơi update các phiên bản base rồi push lên repo của bạn. Dùng khi user nói "featurehub", "publish base version", "update phiên bản base", "đẩy bản base lên git". TUYỆT ĐỐI không push lên ezg-packages / PackageStore.
---

# Feature Hub — version & publish bộ base lên git của bạn

Chốt (snapshot) bộ package/asset của base template ở một **phiên bản**, rồi commit + push lên **repo của bạn** (`origin`) dưới nhánh **folder-namespaced** `featurehub/<version>` — song song với quy ước nhánh sẵn có của bạn (`release/…`, `feature/…`, `agent/…`, `AnhNT/…`).

> Đây là phía **maintainer bộ base của BẠN**, KHÔNG phải upstream `PackageStore/ezg-packages`. Cài gói vào 1 project là việc của skill [[update-featurehub]].

## Guardrail — CHỈ push lên git của bạn
Trước khi push, **bắt buộc** kiểm tra remote đích:
```bash
git remote get-url origin
```
- Phải là repo của bạn (vd `github.com/TNNDinh/...`). 
- Nếu URL chứa `ezg`, `PackageStore`, hay `pub-*.r2.dev` → **DỪNG NGAY**, không push. Chỉ đẩy lên `origin` của bạn.

## "Phiên bản" gồm gì (snapshot)
Bộ file định nghĩa package/asset loadout của base:
- `Packages/manifest.json` (UPM dependencies + scopedRegistries)
- `ProjectSettings/EzgFeatureHub/` (install-record: các .unitypackage đã cài)
- `ProjectSettings/EzgFeatureHub/` version marker (nếu có)
- (tuỳ chọn) file catalog riêng của bạn nếu bạn tự quản danh sách gói

## Procedure

### 1 — Xác định version + base branch
- Hỏi/nhận `<version>` từ user (vd `1.0.0`). Nếu không có, đề xuất bump từ nhánh `featurehub/*` cao nhất hiện có: `git branch -a --list 'origin/featurehub/*'`.
- Đứng ở nhánh base sạch (thường `main`/`base`). Xác nhận working tree không lẫn rác: `git status --short` (discard churn nếu cần — xem cách đã làm với base).

### 2 — Tạo nhánh folder-style
```bash
git checkout -b "featurehub/<version>"
```
(Đúng kiểu `release/build_android_1.1.5` → `featurehub/1.0.0`.)

### 3 — Snapshot + commit
Đảm bảo các file loadout ở đúng trạng thái phiên bản này (đã cài đủ gói qua [[update-featurehub]] nếu cần), rồi:
```bash
git add Packages/manifest.json "ProjectSettings/EzgFeatureHub" <catalog-của-bạn-nếu-có>
git commit -m "featurehub <version>: chốt bộ package/asset base"
```

### 4 — Push lên GIT CỦA BẠN
Sau khi qua Guardrail ở trên:
```bash
git push -u origin "featurehub/<version>"
```
KHÔNG dùng `--force` trừ khi user yêu cầu rõ. KHÔNG push lên bất kỳ remote nào ngoài `origin` của bạn.

### 5 — Báo cáo
Liệt kê: nhánh `featurehub/<version>` đã tạo + push lên `origin`, các file đã snapshot, và nhắc user đây là bản base có thể checkout lại sau.

## Lưu ý
- Feature Hub upstream đọc catalog từ R2 của ezg — skill này KHÔNG sửa/đụng cái đó. Nếu sau này bạn muốn TỰ HOST catalog riêng, đó là bước infra khác (tự host R2/Pages) — hỏi user trước.
- Cài gói thực tế (tải + import .unitypackage) là [[update-featurehub]]; skill này chỉ version hoá & đẩy metadata bộ base lên git của bạn.
