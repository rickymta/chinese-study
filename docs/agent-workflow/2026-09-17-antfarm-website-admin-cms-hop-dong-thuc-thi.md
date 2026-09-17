# AntFarm — Website giới thiệu `antfarms.xyz` + Admin kiêm CMS chung `admin.antfarms.xyz` — Hợp đồng thực thi

- Ngày: 2026-09-17 · Loại: tạo mới (+ nâng cấp identity-service, chuyển màn quản trị khỏi `apps/chinese`)
- Service/app: **`cms-backend` (mới)**, `identity-service` (API nội bộ), `gateway`, **`frontend/apps/admin` (mới)**, **`frontend/apps/website` (mới, Next.js)**, `frontend/apps/chinese` (gỡ màn quản trị), `deploy/`
- Mã feature đợt này: **W1 … W15** (tiền tố `W` = Website/admin; MVP đã dùng F0–F13, **F12** "lên server" vẫn giữ nguyên nghĩa, **F13 portal bị thay thế** bởi đợt này). Migration đợt này đặt tên `W<n>_<TenNgan>` (ngoại lệ có chủ đích của quy ước `F<n>_<Ten>`).
- Nguồn đọc trước: `CLAUDE.md`, `docs/HANDOFF-2026-09-17-MVP.md`, hợp đồng gốc `2026-09-16-antfarm-nen-tang-tieng-trung-mvp-hop-dong-thuc-thi.md` (§3, §5.5, §5.6), các file `2026-09-17-antfarm-*-chi-tiet.md`.
- **Mức chi tiết:** W1 và W2 CHI TIẾT LÀM NGAY. W3–W15 mức thiết kế đủ để làm; BA **bổ sung khi tới lượt** nếu agent thực thi thấy thiếu (ghi rõ ở từng feature). Người dùng yêu cầu làm liên tục tới 22:30 tối 17/09 **không dừng hỏi giữa feature** ⇒ mọi quyết định mở đều có mặc định không chặn (§10.2), người dùng duyệt sau.

---

## 1. Bối cảnh & mục tiêu

### 1.1 Bối cảnh

MVP tiếng Trung (F0–F11) đã xong trên nhánh `develop`. Nền tảng AntFarm được thiết kế đa ngôn ngữ nhưng chưa có:
- **mặt tiền công khai** ở tên miền gốc `antfarms.xyz` (hợp đồng gốc để dành cho "F13 portal" dạng mẫu comment trong nginx);
- **nơi quản trị tập trung**: quản trị nội dung tiếng Trung (F10) và vai trò người dùng tiếng Trung (F4) đang nằm trong chính app học viên `apps/chinese`; tài khoản identity **không có bất kỳ công cụ quản trị nào** (khoá tài khoản, đặt lại mật khẩu đều chưa làm được — D13 hợp đồng gốc dời sang F13); bật/tắt đăng ký là biến môi trường `AUTH_ALLOW_REGISTRATION` phải restart container.

### 1.2 Mục tiêu phần mềm

1. `antfarms.xyz`: website giới thiệu Next.js App Router + ISR, nội dung lấy từ CMS, SEO đầy đủ, dẫn người học sang `chinese.antfarms.xyz`, thu liên hệ + đăng ký nhận tin.
2. `admin.antfarms.xyz`: **một** app quản trị (React 19 + MUI v9 + `@af/*`) gom: nội dung website, tài khoản nền tảng, nội dung + vai trò của từng ngôn ngữ (cắm module), hộp thư liên hệ/nhận tin.
3. Giữ nguyên hai nguyên tắc nền tảng: **identity chỉ xác thực**; **mỗi service tự phân quyền trên DB của mình**.
4. Thêm ngôn ngữ thứ hai sau này chỉ là: thêm module trong `apps/admin` + một dòng trong danh mục ngôn ngữ của CMS.

### 1.3 Mục tiêu học tập (nghiệp vụ sư phạm liên quan)

Đợt này không đổi luồng học, nhưng website là **điểm vào đầu tiên** của người học số 0:
- Thông điệp trang chủ phải dẫn đúng thứ tự học đã thiết kế ở MVP: **pinyin & thanh điệu → từ HSK 3.0 cấp 1 qua ôn thẻ → bài học + quiz → luyện viết**. Nút chính "Bắt đầu học tiếng Trung" trỏ `https://chinese.antfarms.xyz/dang-ky` (người mới) — không trỏ vào trang giữa chừng.
- Ngôn ngữ chưa mở hiển thị "Sắp ra mắt" + nút "Nhận tin khi mở" (ghi đăng ký nhận tin kèm `interest = <mã ngôn ngữ>`) ⇒ đo được nhu cầu thật trước khi đầu tư ngôn ngữ kế tiếp.
- Bài viết/FAQ là học liệu **giới thiệu**, không phải học liệu chính: không đưa từ vựng/bài học vào blog; nội dung tự soạn, không chép giáo trình có bản quyền (R-C6 hợp đồng gốc áp nguyên).
- Việc gom quản trị tiếng Trung sang admin không được làm gián đoạn **duyệt nghĩa `machine` → `reviewed`** (RK4 gốc) — thứ tự W12→W13→W14 bảo đảm lúc nào cũng có đúng một màn duyệt chạy được.

---

## 2. Phạm vi

### 2.1 In-scope

- Service `cms-backend` (DDD 4 lớp + tests, DB `af_cms`, audience `af-cms`, route gateway `/cms/**`), phân quyền cục bộ, API công khai + API CMS, lưu ảnh MinIO, webhook revalidate ISR.
- identity-service: **API nội bộ** quản trị tài khoản trên **cổng nội bộ riêng**, cài đặt runtime "đăng ký mở", thống kê đăng ký.
- `apps/admin` (`@af/admin`): khung, đăng nhập (trang dùng chung `@af/auth`), gộp quyền nhiều service, các màn: người dùng/vai trò CMS, cấu hình site/SEO, ngôn ngữ, FAQ, trang tĩnh, banner/hero, bài viết + danh mục, thư viện ảnh, hộp thư liên hệ + nhận tin (xuất CSV), tài khoản nền tảng, module Tiếng Trung (bài học, duyệt nghĩa, vai trò).
- `apps/website` (`@af/website`): Next.js 15 App Router + Tailwind CSS 4, ISR theo tag, `/api/revalidate`, sitemap/robots/JSON-LD, route tiếng Việt không dấu.
- Package mới `@af/chinese-kit` (tách thành phần hiển thị tiếng Trung dùng chung giữa `apps/chinese` và module admin).
- Gỡ màn quản trị khỏi `apps/chinese`, để lối dẫn sang admin.
- Triển khai: Dockerfile + compose + nginx server block `antfarms.xyz`, `www.antfarms.xyz`, `admin.antfarms.xyz` + MinIO production + init DB `af_cms` + `get-cert.sh` đủ tên + `VERIFY-DOCKER.md` (ghi "chưa verify").
- Cập nhật `CLAUDE.md` phần kiến trúc (bảng domain, cổng, cây kiến trúc, gateway routes, mục lục hợp đồng) + một quy tắc mới về API nội bộ identity (§5.7).

### 2.2 Out-of-scope

- Gửi email (xác nhận nhận tin, quên mật khẩu tự phục vụ, bản tin định kỳ). Không có nhà cung cấp email ⇒ đặt lại mật khẩu do admin cấp mật khẩu tạm.
- Đa ngôn ngữ giao diện website (chỉ tiếng Việt — D-W2).
- Bình luận bài viết, tìm kiếm toàn văn website, phân tích truy cập (analytics), A/B.
- Quản trị ngôn ngữ chưa tồn tại (english/japanese chỉ là bản ghi "sắp ra mắt" trong danh mục).
- Vai trò/quyền trong identity-service (vẫn **không** có — quyền `accounts.manage` nằm ở cms-backend).
- CI/CD, registry; đưa lên server thật (thuộc F12 — đợt này chỉ chuẩn bị file).
- Sửa trực tuyến ảnh (crop/resize), CDN ảnh.

---

## 3. Quy tắc nghiệp vụ (đã chốt)

> **[ĐÃ CHỐT]** = người dùng/orchestrator chốt 17/09/2026. **[BA-mặc định]** = BA chọn để không chặn (liệt kê §10.3). **[MỞ — D-Wx]** = có mặc định, chờ người dùng duyệt (§10.2).

### 3.1 Ranh giới service

- R-W1. **[ĐÃ CHỐT]** Service mới `cms-backend` (thư mục `backend/services/cms-backend`, project `AntFarm.Cms.{Domain,Application,Infrastructure,Api}` + `tests/AntFarm.Cms.{UnitTests,ApiTests}`), DB riêng `af_cms`, schema **`access`** (người dùng/quyền cục bộ) và **`site`** (nội dung website, ảnh, hộp thư, nhật ký thao tác), audience `af-cms`, route gateway `/cms/**`.
- R-W2. **[ĐÃ CHỐT]** Phân quyền cms-backend **fail-closed**: người được provision **không** nhận vai trò mặc định (`CmsAccess:DefaultRoles = []`, không có cấu hình này thì cũng rỗng); admin bootstrap qua `CmsAdmin:BootstrapEmails`. Tài khoản 0 quyền mở admin ⇒ trang `/403` kèm nút Đăng xuất.
- R-W3. **[ĐÃ CHỐT]** identity-service **vẫn không có vai trò/quyền**. Quản trị tài khoản đi đường: admin app → `cms-backend` (kiểm `accounts.manage` cục bộ) → **API nội bộ** identity-service. Trình duyệt **không bao giờ** gọi API nội bộ.
- R-W4. **[ĐÃ CHỐT]** API nội bộ identity chỉ phục vụ trên **cổng nội bộ riêng** (Docker `8081`, dev `5291`) và kiểm **cả hai**: (a) `HttpContext.Connection.LocalPort == Internal:Port` (không dùng header `Host` — client giả được), (b) header `X-Service-Key` so sánh thời gian hằng (`CryptographicOperations.FixedTimeEquals`) với `Internal:ServiceKey`. Sai (a) ⇒ **404** (không lộ sự tồn tại); sai (b) ⇒ 401 `SERVICE_KEY_INVALID` + log Warning. `Internal:ServiceKey` rỗng ⇒ **mọi** route nội bộ trả 404 (tắt hẳn). Gateway **không** có cluster tới cổng 8081, nginx `id.antfarms.xyz` **không** định tuyến tới đó, compose **không** publish 8081. Có test chứng minh route nội bộ gọi qua cổng công khai trả 404.
- R-W5. **[ĐÃ CHỐT]** `apps/admin` gọi thẳng API quản trị của service ngôn ngữ (`/chinese/api/admin/*`) — quyền vẫn kiểm cục bộ ở chinese-backend (`users.manage`, `content.manage` giữ nguyên). chinese-backend **không đổi code** cho việc này.
- R-W6. **[BA-mặc định]** **Một token cho mọi audience**: thêm `af-cms` vào `Jwt:Audiences` của identity (token hiện là mảng `aud` tĩnh — kiểm chứng `TokenIssuer.cs`: `["aud"] = jwtOptions.Audiences`). Admin app dùng một access token gọi cả `/cms/api` lẫn `/chinese/api`. Lý do: đúng thiết kế R-A4 gốc (thêm service ⇒ thêm audience), không phải thêm luồng "xin token theo audience" vào identity + `@af/auth`; token mang audience thừa không mở thêm quyền vì mọi service kiểm quyền cục bộ trên DB. Phương án loại: (1) token theo audience — thêm endpoint + mỗi app giữ nhiều token, phức tạp refresh; (2) chinese-backend chấp nhận audience `af-admin` — vẫn phải sửa cấu hình mỗi service ngôn ngữ, không lợi gì hơn.
- R-W7. **[ĐÃ CHỐT]** Website gọi API CMS:
  - **Phía server Next.js** (ISR/SSR) gọi `CMS_INTERNAL_URL` — dev `http://localhost:5280/cms`, Docker `http://gateway:8080/cms` — chỉ các route `/api/public/*`.
  - **Phía trình duyệt** (form liên hệ, nhận tin) gọi **cùng origin** `antfarms.xyz/cms/api/public/...`; nginx biên `antfarms.xyz` chỉ mở `location ^~ /cms/api/public/` (mọi `/cms/` khác ⇒ 404). Dev: Next `rewrites` `/cms/:path*` → gateway 5280.
  - Admin gọi `/cms/api/*` cùng origin trên `admin.antfarms.xyz` (nginx mở `/cms/` và `/chinese/`).
- R-W8. **[ĐÃ CHỐT]** Ảnh lưu **MinIO**, bucket `af-cms`, **không public bucket**. Ảnh công khai phục vụ qua cms-backend `GET /api/public/media/{id}/{fileName}` (stream, `Cache-Control: public, max-age=31536000, immutable` — id ảnh bất biến; thay ảnh = tải file mới). Ảnh chưa dùng vẫn truy cập được nếu biết id (uuid v7 — chấp nhận, ảnh website vốn công khai).

### 3.2 Phân quyền cms-backend

- R-W9. Danh mục quyền (seed "chèn bù mã thiếu", idempotent, không ném):

| Mã quyền | Ý nghĩa |
|---|---|
| `site.manage` | Cấu hình site/SEO, ngôn ngữ, FAQ, trang tĩnh, banner/hero |
| `posts.manage` | Bài viết + danh mục (gồm xuất bản/gỡ) |
| `media.manage` | Tải lên / sửa alt / xoá ảnh |
| `inbox.manage` | Xem, đánh dấu, xuất CSV liên hệ + nhận tin |
| `accounts.manage` | Quản trị tài khoản nền tảng (identity) + công tắc đăng ký + thống kê |
| `users.manage` | Xem người dùng CMS, gán vai trò CMS |

| Vai trò | Quyền |
|---|---|
| `admin` — Quản trị viên | tất cả |
| `editor` — Biên tập website | `site.manage`, `posts.manage`, `media.manage` |
| `support` — Hỗ trợ người học | `inbox.manage`, `accounts.manage` |

- R-W10. Bootstrap admin, provision, `LAST_ADMIN`, `[RequirePermission]` gán `Policy` trong constructor — **y hệt** R-P4/R-P6/R-P7/R-P9 của chinese-backend (chép khuôn, xem §4.2). Khác duy nhất: R-W2 (không vai trò mặc định).
- R-W11. Quyền của admin app **gộp từ nhiều service, có tiền tố service** để không đụng tên (`users.manage` có ở cả cms lẫn chinese): `cms:site.manage`, `chinese:content.manage`... (§5.3.2).
- R-W12. Mở admin sẽ gọi `GET /chinese/api/me` ⇒ chinese-backend provision người đó với vai trò mặc định `learner` (R-P5 gốc). **Chấp nhận** (tài khoản dùng chung nền tảng, có `learner` ở tiếng Trung là vô hại). Ghi vào §10.1.

### 3.3 Tài khoản nền tảng (qua API nội bộ identity)

- R-W13. **Khoá tài khoản** (`is_active=false`) ⇒ thu hồi mọi refresh token (lý do `admin_disabled`), không đăng nhập/làm mới được (R-A11 gốc). Access token đang sống còn tối đa 15 phút — chấp nhận, ghi rõ trên UI ("hiệu lực hoàn toàn sau tối đa 15 phút").
- R-W14. **Mở khoá**: `is_active=true` + xoá `LockoutUntil`, `FailedLoginCount=0`. Nút riêng "Gỡ khoá tạm do sai mật khẩu" = chỉ xoá lockout (R-A8).
- R-W15. **Đặt lại mật khẩu** (chưa có email): admin bấm ⇒ identity sinh **mật khẩu tạm** 16 ký tự (bảng chữ không nhầm lẫn, `RandomNumberGenerator`) **hoặc** admin tự nhập (8–128, R-A2) ⇒ băm, đổi `PasswordChangedAt`, **thu hồi mọi refresh token**; mật khẩu tạm trả về **đúng một lần** trong response, **không log, không lưu nhật ký**. UI hiện một lần kèm nút sao chép + lời nhắc "gửi qua kênh riêng, dặn người dùng đổi ngay ở Hồ sơ". Không ép đổi mật khẩu lần đăng nhập kế (D-W6).
- R-W16. **Thu hồi mọi phiên**: thu hồi toàn bộ refresh token (lý do `admin_revoked`), không đổi mật khẩu.
- R-W17. Không được khoá / đặt lại mật khẩu / thu hồi phiên **của chính mình** qua admin (422 `SELF_ACTION_FORBIDDEN`) — tránh tự khoá mình ra ngoài; đổi mật khẩu của mình dùng Hồ sơ ở app học.
- R-W18. **Đăng ký mở là cài đặt runtime lưu DB identity** [MỞ — D-W4, mặc định này]: bảng `identity.settings`, khoá `registration.enabled`. Chưa có dòng ⇒ dùng `Auth:AllowRegistration` (env `AUTH_ALLOW_REGISTRATION`) làm giá trị ban đầu. Đổi qua admin có hiệu lực ≤ 30 giây (cache). Env chỉ còn là giá trị khởi tạo — ghi rõ trong `.env.example`.
- R-W19. **Thống kê đăng ký**: số tài khoản theo **ngày lịch `Asia/Ho_Chi_Minh`** (cố định múi giờ vận hành, không theo từng người), khoảng nửa hở `[from 00:00 VN, to+1 00:00 VN)`, tối đa 366 ngày; kèm tổng tài khoản, số đang khoá, số đăng nhập trong 7/30 ngày (`LastLoginAt`).
- R-W20. Mọi thao tác ghi trên tài khoản được cms-backend ghi **nhật ký** `site.audit_logs` (ai, lúc nào, hành động, tài khoản đích, kết quả) — **không** ghi mật khẩu.

### 3.4 Nội dung website

- R-W21. Trạng thái nội dung có xuất bản (trang tĩnh, bài viết): `draft | published`. API công khai **chỉ** trả `published` và `published_at <= now`. Không có quy trình duyệt nhiều bước (một người biên tập).
- R-W22. Nội dung dài (thân bài, trang tĩnh, câu trả lời FAQ) lưu **Markdown** [MỞ — D-W1, mặc định này]; website render bằng `react-markdown` + `remark-gfm`, **không** cho HTML thô (`skipHtml`) ⇒ an toàn XSS không cần sanitizer phía server. Ảnh trong thân bài chèn bằng cú pháp Markdown trỏ URL `/cms/api/public/media/...` (MediaPicker chèn sẵn).
- R-W23. Slug: `^[a-z0-9]+(-[a-z0-9]+)*$`, 1–120 ký tự, duy nhất trong loại; admin gợi ý slug từ tiêu đề (bỏ dấu tiếng Việt, `đ→d`). Slug trang tĩnh **không** được trùng từ khoá dành riêng: `bai-viet`, `cau-hoi-thuong-gap`, `lien-he`, `ngon-ngu`, `api`, `cms`, `_next`, `sitemap.xml`, `robots.txt`, `404`, `500` (422 `SLUG_RESERVED`).
- R-W24. Đổi slug của nội dung đã xuất bản: cho phép, **không** tự tạo chuyển hướng (bảng redirect để sau — ghi nợ N-W3); UI cảnh báo "link cũ sẽ 404".
- R-W25. Danh mục ngôn ngữ `site.languages`: `status = open | coming_soon | hidden`. `open` bắt buộc có `app_url` https (dev cho http localhost); `coming_soon` hiện "Sắp ra mắt" + nút nhận tin. Seed **chỉ khi bảng trống** (người dùng sửa/xoá được): `chinese` (open), `english`, `japanese` (coming_soon).
- R-W26. Mọi thao tác ghi nội dung thành công ⇒ cms-backend gọi webhook revalidate theo **tag** (fail-soft: lỗi chỉ log Warning, không làm hỏng thao tác). Website còn `revalidate` dự phòng **3600 giây** mỗi fetch để tự lành khi webhook hỏng.
- R-W27. Sửa đồng thời: bản ghi nội dung có `xmin` (concurrency token, mẫu F10) ⇒ 409 `CONCURRENCY_CONFLICT`; form admin **gửi nguyên bản ghi** (PUT đầy đủ), không PATCH từng trường.
- R-W28. Ảnh: chỉ nhận `image/jpeg`, `image/png`, `image/webp`, `image/gif`; **không nhận `image/svg+xml`** (có thể nhúng script) [BA-mặc định]; tối đa **5 MB**; kiểm magic bytes, không tin `Content-Type` client. Xoá ảnh đang được tham chiếu (banner/bài viết/cấu hình OG) ⇒ 409 `MEDIA_IN_USE` kèm danh sách nơi dùng. Ảnh chèn trong thân Markdown **không** được kiểm (chấp nhận, ghi rủi ro).

### 3.5 Liên hệ & nhận tin

- R-W29. Form liên hệ: `name` 1–100, `email` hợp lệ (bắt buộc), `phone` tuỳ chọn ≤ 20, `message` 10–4000, `sourcePath` (trang gửi). Nhận tin: `email`, `interest` (mã ngôn ngữ hoặc `general`).
- R-W30. Chống spam [MỞ — D-W3, mặc định]: **honeypot** (trường ẩn `website` phải rỗng) + **thời gian điền tối thiểu 3 giây** (`renderedAt` do trang đặt, lệch > 1 ngày coi như bot) + **rate limit 5 yêu cầu/10 phút/IP** cho nhóm `public-forms`. Bot trúng honeypot/thời gian ⇒ trả **202 giả thành công**, lưu với `status=spam` (để admin thấy tỉ lệ). IP lưu dạng `SHA-256(ip + CmsInbox:IpSalt)`, không lưu IP thô.
- R-W31. Nhận tin idempotent theo `email_normalized` + `interest`: đăng ký lại ⇒ 202, cập nhật `updated_at`, chuyển `unsubscribed → subscribed`. Không có email xác nhận (không double opt-in — ghi nợ N-W2); admin có nút "Huỷ đăng ký" theo yêu cầu người dùng.
- R-W32. Trạng thái liên hệ: `new → handled` (kèm ghi chú xử lý, người xử lý, thời điểm), `spam`; mở lại được `handled → new`. Xuất CSV: UTF-8 **có BOM** (Excel mở đúng tiếng Việt), lọc theo trạng thái + khoảng ngày (ngày VN, nửa hở), tối đa 10.000 dòng/lần; chống CSV injection (ô bắt đầu `= + - @` thêm tiền tố `'`).
- R-W33. Dữ liệu cá nhân: website hiện một dòng đồng ý ngắn dưới form + link trang `chinh-sach-bao-mat` (trang tĩnh seed nháp, người dùng tự soạn nội dung).

### 3.6 Chuyển quản trị tiếng Trung sang admin

- R-W34. **[ĐÃ CHỐT]** Không để hai nơi cùng quản trị lâu dài. Thứ tự bắt buộc: W12 tách `@af/chinese-kit` (apps/chinese không đổi hành vi) → W13 admin có module Tiếng Trung chạy đủ (lúc này **hai nơi cùng chạy, chỉ trong khoảng giữa hai commit**) → W14 gỡ màn quản trị khỏi `apps/chinese`.
- R-W35. Sau W14, `apps/chinese` giữ **lối dẫn**: người có `content.manage` hoặc `users.manage` thấy mục "Quản trị" dẫn ra `ADMIN_URL` (biến build `VITE_ADMIN_URL`, dev `http://localhost:3290`, prod `https://admin.antfarms.xyz`) — link mở tab mới; các route cũ `/quan-tri/*` chuyển thành trang "Quản trị đã chuyển sang trang Admin" + nút mở (không 404 cho người có bookmark).

---

## 4. Hiện trạng liên quan (đã kiểm chứng bằng Read/Grep ngày 17/09/2026)

### 4.1 identity-service

| Điểm | Thực tế trong code |
|---|---|
| Phát token | `Infrastructure/Security/TokenIssuer.cs`: `aud` = **mảng tĩnh** `JwtOptions.Audiences` (đặt qua `Claims["aud"]`), không theo client. `appsettings.json` `Jwt:Audiences = ["af-identity","af-chinese"]`; compose **không** ghi đè ⇒ sửa appsettings là đủ |
| Test khoá cứng audience | `tests/AntFarm.Identity.ApiTests/Auth/JwksAndTokenTests.cs:57` dùng `Contain([...])` (thêm `af-cms` không vỡ); `UnitTests/Security/TokenIssuerTests.cs:45` dùng `BeEquivalentTo` trên options tự dựng trong test (không đọc appsettings — không vỡ); `IdentityApiFactory.cs:50-51` đặt biến `Jwt__Audiences__0/1` |
| Đăng ký mở | `AuthOptions.AllowRegistration` (POCO singleton), đọc ở `Application/Accounts/AuthService.cs:29`; compose `Auth__AllowRegistration: ${AUTH_ALLOW_REGISTRATION:-false}` |
| Tài khoản | `Domain/Accounts/Account.cs`: `IsActive`, `FailedLoginCount`, `LockoutUntil`, `LastLoginAt`, `PasswordChangedAt`, `CreatedAt`; **chưa có** method khoá/mở/đặt mật khẩu bởi admin |
| Refresh token | `Domain/Accounts/RefreshToken.cs`: `RevokedAt`, `RevokeReason`, `FamilyId` |
| Kiểm token của chính mình | `Program.cs` `AddAfJwtBearer(new AfAuthOptions{ Audience = "af-identity" }, localKeys)` |
| Không có | vai trò, quyền, route nội bộ, bảng settings, cổng thứ hai |
| CORS | `Api/Configuration/IdentityCorsExtensions.cs` + `ValidateOriginAttribute.cs`, danh sách `Auth:AllowedOrigins` (dev: 3280, 3281; compose prod: `https://chinese.${APP_DOMAIN}`, dòng `https://${APP_DOMAIN}` đang comment) |

### 4.2 chinese-backend (khuôn để chép cho cms-backend)

- Domain `Access/{User,Role,Permission,UserRole,RolePermission,PermissionCodes,RoleCodes}.cs`; Application `Access/{UserProvisioningService,PermissionResolver,MeService,UserAdminService,DefaultRoleAssignmentPolicy,*Validator}.cs`, `Common/Options/{ChineseAccessOptions,ChineseAdminOptions}.cs`, `Common/Abstractions/IChineseDbContext.cs`; Infrastructure `Persistence/ChineseDbContext.cs`, `Persistence/Configurations/*`, `Seeding/AccessSeeder.cs`, migration `F3_Access`; Api `Middleware/UserProvisioningMiddleware.cs`, `Features/Me/MeController.cs`, `Features/Admin/{UsersController,RolesController,PingController}.cs`, `Program.cs` (dòng 33–37 options, 105 middleware, 118–121 seeder).
- Test: `tests/AntFarm.Chinese.ApiTests/Infrastructure/{ChineseApiFactory,ChineseDbApiFactory,ChineseDbFixture,TestDbContextFactory}.cs` (mẫu `StaticConfigurationManager` đúng cho ASP.NET Core 10), `UnitTests/Access/DefaultRoleAssignmentPolicyTests.cs`.
- Schema `access` (hợp đồng gốc §5.1.2): `users(id=sub, email, display_name, time_zone, first_seen_at, last_seen_at)`, `roles(id, code, name)`, `permissions(code PK, description)`, `role_permissions`, `user_roles(assigned_at)`.
- API quản trị (giữ nguyên, admin sẽ gọi): `GET /api/me`; `GET /api/admin/users?q&page&pageSize`, `GET /api/admin/users/{id}`, `PUT /api/admin/users/{id}/roles`, `GET /api/admin/roles` (`users.manage`); `api/admin/lessons` (GET, POST, GET/PUT `{id}`, PUT `{id}/blocks|words|quiz`, POST `{id}/publish|unpublish|review|restore`, DELETE `{id}`) và `api/admin/words` (GET, GET/PUT `{id}`, POST `review`) (`content.manage`).

### 4.3 Shared

- `backend/shared/AntFarm.Auth`: `AfAuthOptions{Issuer, Audience, JwksUrl, RequireHttpsMetadata}`, `JwtBearerExtensions.AddAfJwtBearer`, `Authorization/{RequirePermissionAttribute, PermissionPolicyProvider, PermissionRequirement, PermissionAuthorizationHandler, IPermissionResolver, AuthorizationExtensions}.cs`. **Không có** thư viện access dùng chung (mỗi service tự hiện thực provision/resolver) ⇒ cms-backend chép khuôn (nợ N-W1: tách `AntFarm.Access` khi có service thứ ba).

### 4.4 Gateway, deploy

- `backend/services/gateway/appsettings.json`: route `identity`, `chinese` với `PathRemovePrefix` + `X-Forwarded: Append`; cluster dev `localhost:5281`, `:5282`; compose ghi đè `ReverseProxy__Clusters__<x>__Destinations__primary__Address`.
- `deploy/docker-compose.yml`: postgres, identity-service, chinese-backend, gateway, chinese-frontend, nginx (duy nhất có `ports:`), certbot; ảnh `${REGISTRY:-localhost/antfarm}/<svc>:${<SVC>_TAG:-latest}`. **Chưa có MinIO production.**
- `deploy/dev/docker-compose.dev.yml`: postgres 18 + **MinIO** (`127.0.0.1:9000/9001`, root `minioadmin`) + `minio-init` tạo bucket `af-chinese` (chưa ai dùng).
- `deploy/postgres/init/01-create-databases.sh`: tạo role+DB `af_identity`, `af_chinese` từ biến `AF_*_DB_PASSWORD` (chỉ chạy khi volume trống).
- `deploy/conf/nginx.conf.example`: khối 80 ACME, `id.` (location `/` → gateway `/identity`), `chinese.` (`^~ /chinese/` → gateway, `/` → frontend), mẫu comment ngôn ngữ + portal.
- `deploy/scripts/get-cert.sh <email> <domain...>` — một chứng chỉ SAN, phải truyền đủ tên.

### 4.5 Frontend

- Workspace `frontend/` (`apps/*`, `packages/*`), app duy nhất `apps/chinese` (Vite 8, React ^19.2.6, MUI ^9.0.1, TS ~6.0.2, cổng 3280, proxy `/identity`,`/chinese` → 5280).
- `@af/auth` `AuthProvider({ session, identity, loadMe })` — **một** hàm `loadMe(): Promise<MeInfo>`, `MeInfo = { permissions: string[]; [key: string]: unknown }` ⇒ admin gộp nhiều `/me` trong **một** `loadMe` (không cần sửa `@af/auth`). `RequirePermission({ permission })`, `useAuth().can()`.
- `apps/chinese/src/api/clients.ts`: `authSession = createAuthSession({ identityBaseURL })`, `createApiClient({ baseURL, getAccessToken, refresh, onAuthLost })`, `identity = createIdentityClient(identityApi)` — admin chép khuôn.
- `@af/ui` export: `AppLayout`/`NavItem` (có `requiredPermission`), `PageContainer`, `StickyActionBar`, `ConfirmProvider`/`useConfirm`, `ToastProvider`/`useToast`, `AppAutocomplete`, `ErrorPage`, `NotFoundPage`, `AppDialog`, `AppDrawer`, `useTabParam`, `useBackTo`, `useScrollRestore`, `LangText`, speech.
- **Độ dính của màn quản trị tiếng Trung** (grep import `features/admin-content`, `features/admin-users`, ~3.700 dòng tsx): phụ thuộc `@/lib/pinyin` (`numberedToMarked`, `normalizeNumbered`, `stripPunctuation`), `@/components/Hanzi`, `@/components/speech/{ChineseSpeech,SpeakButton}`, `@/features/lessons/components/{LessonContent,InlineZh,LessonDisplayContext,quiz/QuestionParts}`, `@/features/lessons/{types,hooks}`, `@/features/dictionary/{types,hooks,lib/sources,components/MeaningStatusChip,components/QueryErrorAlert}`, `@/features/auth/permissions`, `@/lib/searchParams`, `@/lib/useDebouncedValue`. ⇒ **không thể** chép thẳng sang app khác; cần W12 tách package.
- `apps/chinese/src/router.tsx`: nhánh `quan-tri` (`nguoi-dung`, `bai-hoc`, `bai-hoc/:id`, `tu-vung`); layout `buildNavItems` có mục Quản trị theo `PERMISSIONS.USERS_MANAGE`.
- Dockerfile frontend mẫu `apps/chinese/Dockerfile` (COPY từng `package.json` rồi `yarn install --frozen-lockfile`; `ARG VITE_IDENTITY_API_URL` mặc định prod).

### 4.6 Mẫu MedDental (tham khảo, `H:\Work\Meddental\mdt-re-construct`)

- `mdt-frontend/apps/website-public`: Next `^15.3.3` + React `^19.2.0` + `@tailwindcss/postcss ^4.1.10` + `tailwindcss ^4.1.10` + `@tailwindcss/typography` + TS `~6.0.2` **cùng yarn workspace với các app React 19 khác** ⇒ đã chứng minh chạy được. Dockerfile chỉ COPY `package.json` của app đó, runtime `node:22-alpine` chạy `.next/standalone` `server.js`, `HEALTHCHECK wget /api/health`, `ARG NEXT_PUBLIC_*` nướng lúc build.
- `backend/services/website-backend/.../Services/RevalidationService.cs`: POST `{ paths, tags }` + header `X-Revalidate-Secret`, fail-soft (chỉ log). Chép ý tưởng.
- `mdt-frontend/apps/website-cms`: MediaPicker modal dùng chung, dnd-kit sắp xếp, form mang nguyên bản ghi.

---

## 5. Thiết kế giải pháp

### 5.0 Quy ước chung đợt này

#### 5.0.1 Cổng dev & tên container

| Thành phần | Dev local | Docker (trong `af-net`) |
|---|---|---|
| gateway | 5280 (không đổi) | `gateway:8080` |
| identity-service công khai | 5281 (không đổi) | `identity-service:8080` |
| **identity-service nội bộ** | **5291** | **`identity-service:8081`** (không publish, không cluster gateway) |
| chinese-backend | 5282 (không đổi) | `chinese-backend:8080` |
| **cms-backend** | **5290** (Scalar `/scalar/v1`) | **`cms-backend:8080`** |
| apps/chinese | 3280 (không đổi) | `chinese-frontend:80` |
| **apps/website** (Next.js) | **3281** (dùng lại cổng dành cho portal) | **`website:3000`** |
| **apps/admin** | **3290** | **`admin-frontend:80`** |
| MinIO (dev compose) | 9000 API / 9001 console | `minio:9000` |
| Ngôn ngữ kế tiếp | backend 5283, app 3282 (không đổi dải) | — |

Dải `529x`/`3290` cho nền tảng quản trị để không chiếm dải tăng dần của ngôn ngữ (`5283+`, `3282+`). Không đụng dải MedDental (5290/5291/3290 chưa dùng).

#### 5.0.2 Đặt tên

| Thứ | Giá trị |
|---|---|
| Service | `backend/services/cms-backend`, `AntFarm.Cms.*`, namespace `AntFarm.Cms.<Layer>` |
| DB / role / test DB | `af_cms` / `af_cms` / `af_cms_test` |
| Audience | `af-cms` |
| Route gateway | `/cms/{**catch-all}` → cluster `cms` |
| Cấu hình | `CmsAccess:DefaultRoles` (mặc định `[]`), `CmsAdmin:BootstrapEmails`, `Storage:*`, `Revalidation:*`, `IdentityInternal:*`, `CmsInbox:IpSalt` |
| App | `frontend/apps/admin` → `@af/admin`; `frontend/apps/website` → `@af/website`; package `frontend/packages/chinese-kit` → `@af/chinese-kit` |
| Bucket | `af-cms` |
| API | service tiền tố `/api`; công khai `/api/public/*` (AllowAnonymous); CMS `/api/admin/*` (quyền); `/api/me` |
| Lỗi / phân trang / JSON / thời gian | như hợp đồng gốc §5.0.3 + §6.0 |

#### 5.0.3 Kiểm tra bắt buộc mọi feature

```powershell
dotnet build backend/backend.slnx -v q
dotnet test backend/backend.slnx            # đặt AF_TEST_PG; báo số chạy/skip
cd frontend
yarn workspace @af/admin tsc -b             # khi đụng admin (từ W2)
yarn workspace @af/admin build              # khi đụng dependency/packages
yarn workspace @af/website build            # từ W7 (next build = type-check + build)
yarn workspace @af/chinese tsc -b; yarn workspace @af/chinese build; yarn workspace @af/chinese test   # W12, W14
yarn lint:ui
```

---

### 5.1 Database

#### 5.1.1 W1 — `af_cms`, schema `access` (migration `W1_Access`)

Chép đúng cấu trúc `af_chinese.access` (§4.2), thêm cột `assigned_by`:

```
access.users
  id               uuid PK                      -- = sub
  email            varchar(254) NOT NULL
  display_name     varchar(100) NOT NULL
  time_zone        varchar(64)  NOT NULL        -- chép từ zoneinfo, mặc định Asia/Ho_Chi_Minh (dùng hiển thị giờ trong admin)
  first_seen_at    timestamptz  NOT NULL
  last_seen_at     timestamptz  NOT NULL
  INDEX ix_users_email_lower ON (lower(email))

access.roles             id uuid PK · code varchar(32) UNIQUE · name varchar(100) NOT NULL
access.permissions       code varchar(64) PK · description varchar(200) NOT NULL
access.role_permissions  role_id uuid FK→roles CASCADE · permission_code varchar(64) FK→permissions CASCADE · PK(role_id, permission_code)
access.user_roles        user_id uuid FK→users CASCADE · role_id uuid FK→roles RESTRICT · assigned_at timestamptz NOT NULL · assigned_by uuid NULL · PK(user_id, role_id)
```

snake_case qua `EFCore.NamingConventions` (như chinese), bảng lịch sử `public.__ef_migrations_history`.

**Init DB:** `deploy/postgres/init/01-create-databases.sh` thêm `: "${AF_CMS_DB_PASSWORD:?...}"`, `-v cms_pw=...`, `CREATE ROLE af_cms LOGIN PASSWORD :'cms_pw'; CREATE DATABASE af_cms OWNER af_cms ENCODING 'UTF8' TEMPLATE template0;`. `deploy/dev/docker-compose.dev.yml` + `deploy/dev/.env.example` + `deploy/.env.example` + `deploy/docker-compose.yml` (service postgres) thêm `AF_CMS_DB_PASSWORD` (dev mặc định `af_cms_dev`). ⚠️ Máy đã có volume ⇒ script không chạy lại ⇒ README/`VERIFY-DOCKER.md` ghi lệnh tạo tay (chạy bằng user `postgres`, **DB phải OWNER `af_cms`** — bài học HANDOFF: sai chủ sở hữu là migration hỏng):

```sql
CREATE ROLE af_cms LOGIN PASSWORD '<mật khẩu>';
CREATE DATABASE af_cms OWNER af_cms ENCODING 'UTF8' TEMPLATE template0;
```

Máy dev Windows cài Postgres trực tiếp: chạy hai câu trên bằng psql.

#### 5.1.2 W3 — schema `site` phần nền (chẻ 2 migration theo W3a/W3b)

> **Chẻ 17/09/2026:** W3 tách **W3a** (migration `W3a_SiteBasics`: `site.settings`, `site.languages`, `site.audit_logs`) và **W3b** (migration `W3b_Faqs`: `site.faqs`). `audit_logs` tạo ở W3a vì mọi thao tác ghi của W3a đã phải ghi nhật ký. Cột `xmin` ánh xạ thuộc tính `uint Version` + `IsRowVersion()` (khuôn chinese-backend F10). Chỉ mục `site.languages(sort_order)`. Kiểu `status` lưu chuỗi `open|coming_soon|hidden` (EF `HasConversion` chuỗi, CHECK constraint như dưới). **Không seed FAQ** [BA-mặc định — không bịa nội dung hỏi đáp; người dùng tự soạn].

```
site.settings        key varchar(64) PK · value text NOT NULL · updated_at timestamptz · updated_by uuid NULL
site.languages       id uuid PK · code varchar(32) UNIQUE · name varchar(60) · native_name varchar(60) · tagline varchar(160)
                     · description_markdown text · status varchar(16) CHECK IN ('open','coming_soon','hidden')
                     · app_url varchar(300) NULL · accent_color varchar(9) NULL · cover_media_id uuid NULL
                     · sort_order int · updated_at timestamptz · updated_by uuid NULL · xmin (concurrency)
site.faqs            id uuid PK · question varchar(300) · answer_markdown text · group_key varchar(32) DEFAULT 'general'
                     · sort_order int · is_published bool · updated_at · updated_by · xmin
                     INDEX (is_published, group_key, sort_order)
site.audit_logs      id uuid PK · at timestamptz · actor_id uuid · actor_email varchar(254) · action varchar(64)
                     · target_type varchar(32) · target_id varchar(64) · summary varchar(500) · success bool
                     INDEX (at DESC), INDEX (target_type, target_id)
```

Khoá `site.settings` hợp lệ (whitelist trong Domain `SiteSettingKeys`): `site.name`, `site.tagline`, `seo.default_title`, `seo.default_description`, `seo.og_image_media_id`, `contact.email`, `social.facebook`, `social.youtube`, `social.tiktok`, `footer.text`, `home.hero_mode` (`banners|static`). Seed chèn bù **khoá thiếu** với giá trị mặc định (settings là danh mục hệ thống, không xoá được). `languages`, `faqs` seed chỉ khi bảng trống (R-W25).
`cover_media_id` chưa có FK ở W3 (bảng media tạo ở W4) — W4 thêm FK `ON DELETE RESTRICT`.

#### 5.1.3 W4 — `site.media_files` (migration `W4_Media`)

```
site.media_files  id uuid PK · object_key varchar(200) UNIQUE   -- "yyyy/MM/<id><ext>"
                  · file_name varchar(200) · content_type varchar(64) · size_bytes bigint · width int NULL · height int NULL
                  · alt_text varchar(250) · created_at · created_by uuid · INDEX (created_at DESC)
```
FK từ `languages.cover_media_id`. Kiểm "đang dùng" (R-W28) bằng truy vấn các cột FK + `site.settings` khoá `seo.og_image_media_id`.

#### 5.1.4 W5 — trang tĩnh + banner (migration `W5_PagesBanners`)

```
site.pages    id · slug varchar(120) UNIQUE · title varchar(200) · body_markdown text · seo_title varchar(70) NULL
              · seo_description varchar(170) NULL · status ('draft','published') · published_at timestamptz NULL
              · show_in_footer bool · created_at/created_by/updated_at/updated_by · xmin
site.banners  id · title varchar(120) · subtitle varchar(250) · cta_label varchar(40) NULL · cta_url varchar(300) NULL
              · image_media_id uuid FK RESTRICT · mobile_image_media_id uuid NULL FK RESTRICT
              · starts_at timestamptz NULL · ends_at timestamptz NULL · is_active bool · sort_order int · updated_* · xmin
```
Seed trang (chỉ khi bảng trống, `draft`): `gioi-thieu`, `dieu-khoan-su-dung`, `chinh-sach-bao-mat` (thân là dàn ý "[Cần người dùng soạn]").

#### 5.1.5 W6 — bài viết (migration `W6_Posts`)

```
site.post_categories  id · slug UNIQUE · name varchar(80) · description varchar(300) · sort_order · xmin
site.posts            id · slug UNIQUE · title varchar(200) · excerpt varchar(300) · body_markdown text
                      · cover_media_id uuid NULL FK RESTRICT · category_id uuid NULL FK SET NULL
                      · status ('draft','published') · published_at timestamptz NULL · author_name varchar(100)
                      · seo_title/seo_description · reading_minutes int (tính lúc lưu: số từ/220, tối thiểu 1)
                      · created_*/updated_* · xmin
                      INDEX (status, published_at DESC), INDEX (category_id, published_at DESC)
```

#### 5.1.6 W9 — hộp thư (migration `W9_Inbox`)

```
site.contact_messages  id · name varchar(100) · email varchar(254) · phone varchar(20) NULL · message text
                       · source_path varchar(300) · status ('new','handled','spam') · handled_at NULL · handled_by uuid NULL
                       · handling_note varchar(1000) NULL · ip_hash char(64) · user_agent varchar(300) · created_at
                       INDEX (status, created_at DESC)
site.newsletter_subscribers  id · email varchar(254) · email_normalized varchar(254) · interest varchar(32)
                       · status ('subscribed','unsubscribed') · source_path · ip_hash · created_at · updated_at
                       UNIQUE (email_normalized, interest)
```

#### 5.1.7 W10 — identity `identity.settings` (migration `W10_Settings` trong `af_identity`)

```
identity.settings  key varchar(64) PK · value text NOT NULL · updated_at timestamptz · updated_by uuid NULL
```
Không seed (thiếu dòng ⇒ dùng env, R-W18). Không thêm cột vào `accounts`.
Entity Domain `AntFarm.Identity.Domain.Settings.PlatformSetting` (`Key`, `Value`, `UpdatedAt`, `UpdatedBy`; factory `Create(key, value, actorId, now)` + `Update(value, actorId, now)`), hằng `PlatformSettingKeys.RegistrationEnabled = "registration.enabled"` (giá trị `"true"|"false"`). Cấu hình EF `Persistence/Configurations/PlatformSettingConfiguration.cs`, bảng `settings` (schema mặc định `identity`). `IIdentityDbContext` + `IdentityDbContext` thêm `DbSet<PlatformSetting> Settings`. Migration: `dotnet ef migrations add W10_Settings --project backend/services/identity-service/src/AntFarm.Identity.Infrastructure --startup-project backend/services/identity-service/src/AntFarm.Identity.Api --output-dir Persistence/Migrations`.

---

### 5.2 Backend

#### 5.2.1 W1 — cms-backend khung + phân quyền cục bộ + `/api/me` (CHI TIẾT LÀM NGAY)

**Cây thư mục phải tạo** (chép khuôn chinese-backend, bỏ mọi thứ về học liệu/học tập):

```
backend/services/cms-backend/
  src/AntFarm.Cms.Domain/
    AntFarm.Cms.Domain.csproj                     (tham chiếu AntFarm.Core nếu chinese Domain có)
    Access/User.cs, Role.cs, Permission.cs, UserRole.cs, RolePermission.cs
    Access/PermissionCodes.cs                     SiteManage="site.manage", PostsManage, MediaManage, InboxManage, AccountsManage, UsersManage + All
    Access/RoleCodes.cs                           Admin="admin", Editor="editor", Support="support"
  src/AntFarm.Cms.Application/
    AntFarm.Cms.Application.csproj
    DependencyInjection.cs                        AddApplication()
    Common/Abstractions/ICmsDbContext.cs
    Common/Options/CmsAccessOptions.cs            string[] DefaultRoles = []
    Common/Options/CmsAdminOptions.cs             string[] BootstrapEmails = []
    Access/UserProvisioningService.cs             chép UserProvisioningService của chinese (cache ảnh chụp 5 phút, đồng bộ ngay khi claim đổi)
    Access/DefaultRoleAssignmentPolicy.cs         email ∈ BootstrapEmails ⇒ [admin]; ngược lại DefaultRoles (mặc định rỗng)
    Access/PermissionResolver.cs                  IPermissionResolver, cache 60s, Invalidate(userId)
    Access/MeService.cs, Access/Dtos/MeDto.cs
    Access/UserAdminService.cs, Access/Dtos/AdminUserDto.cs, AdminUsersQueryValidator.cs, SetUserRolesRequestValidator.cs
    Access/RoleCatalog.cs                         bảng vai trò→quyền + tên tiếng Việt (nguồn duy nhất cho seeder)
  src/AntFarm.Cms.Infrastructure/
    AntFarm.Cms.Infrastructure.csproj
    DependencyInjection.cs                        AddInfrastructure(config): DbContext Npgsql + snake_case; health "postgres" [ready]
    Persistence/CmsDbContext.cs                   ApplyConfigurationsFromAssembly; HasDefaultSchema không đặt (mỗi config tự ToTable(..., "access"))
    Persistence/Configurations/Access/*.cs        5 file IEntityTypeConfiguration
    Persistence/Migrations/<ts>_W1_Access.cs      sinh bằng dotnet ef
    Seeding/AccessSeeder.cs                       chèn bù permissions/roles/role_permissions theo RoleCatalog; bootstrap admin cho user đã tồn tại; KHÔNG ném (try/catch log Error)
  src/AntFarm.Cms.Api/
    AntFarm.Cms.Api.csproj, Program.cs, appsettings.json, appsettings.Development.json.example, Dockerfile
    Properties/launchSettings.json                profile "http" → http://localhost:5290
    Middleware/UserProvisioningMiddleware.cs
    Features/System/SystemController.cs           GET /api/system/info (AllowAnonymous) — chép identity/chinese
    Features/Me/MeController.cs                   GET /api/me [Authorize]
    Features/Admin/UsersController.cs             users.manage
    Features/Admin/RolesController.cs             users.manage
    Features/Admin/PingController.cs              users.manage — canh gác phân quyền
  tests/AntFarm.Cms.UnitTests/
    Access/DefaultRoleAssignmentPolicyTests.cs, Access/RoleCatalogTests.cs, Access/UserAdminServiceLastAdminTests.cs (nếu chinese có tương đương — chép)
  tests/AntFarm.Cms.ApiTests/
    Infrastructure/CmsApiFactory.cs, CmsDbApiFactory.cs, CmsDbFixture.cs, TestDbContextFactory.cs   (chép khuôn chinese, DB af_cms_test)
    Access/MeTests.cs, Access/AdminUsersTests.cs, Access/AuthorizationGuardTests.cs
    System/HealthAndInfoTests.cs
```

Thêm 6 project vào `backend/backend.slnx` trong `<Folder Name="/services/cms-backend/">` theo đúng thứ tự khối chinese.

**`Program.cs`** — chép `AntFarm.Chinese.Api/Program.cs`, **bỏ**: mọi thứ Content/import học liệu, `LearningSettings`, đăng ký service học tập, `UserDayContext`. **Giữ nguyên thứ tự pipeline** của chinese (ForwardedHeaders → SecurityHeaders → CorrelationId → SerilogRequestLogging → ExceptionHandler → OpenApi/Scalar dev → Authentication → `UserProvisioningMiddleware` → Authorization → Controllers → HealthChecks). `AddAfSerilog("cms-backend")`. `AddAfJwtBearer(builder.Configuration)` đọc section `Auth`. `FallbackPolicy = RequireAuthenticatedUser`. `SystemController` + health `AllowAnonymous`. AutoMigrate + `AccessSeeder.SeedAsync` sau `MigrateAsync` như chinese.

**`appsettings.json`:**

```json
{
  "Serilog": { "MinimumLevel": { "Default": "Information", "Override": { "Microsoft": "Warning", "Microsoft.Hosting.Lifetime": "Information", "Microsoft.EntityFrameworkCore": "Warning" } } },
  "AllowedHosts": "*",
  "ConnectionStrings": { "Default": "" },
  "AutoMigrate": true,
  "Auth": {
    "Issuer": "http://localhost:5280/identity",
    "Audience": "af-cms",
    "JwksUrl": "http://localhost:5281/.well-known/jwks.json",
    "RequireHttpsMetadata": false
  },
  "CmsAccess": { "DefaultRoles": [] },
  "CmsAdmin": { "BootstrapEmails": [] },
  "ForwardedHeaders": { "ForwardLimit": 2, "KnownIPNetworks": ["127.0.0.1/32", "::1/128"] }
}
```

`appsettings.Development.json.example`: `ConnectionStrings:Default = "Host=localhost;Port=5432;Database=af_cms;Username=af_cms;Password=af_cms_dev"`, `Serilog Debug`, `CmsAdmin:BootstrapEmails: ["<email admin dev của bạn>"]` kèm comment. Kiểm `.gitignore` đã bỏ qua `appsettings.Development.json` ở mọi thư mục (pattern hiện tại) — nếu pattern có đường dẫn cụ thể theo service thì thêm dòng cho cms.

**identity-service (một thay đổi cấu hình trong W1):** `appsettings.json` `Jwt:Audiences` → `["af-identity", "af-chinese", "af-cms"]`. `IdentityApiFactory.cs` thêm `Jwt__Audiences__2 = "af-cms"`. `JwksAndTokenTests` thêm assert token chứa `af-cms`. **Không** đổi code phát token. ⚠️ Token phát trước khi đổi không có `af-cms` ⇒ tối đa 15 phút cms-backend trả 401 cho phiên cũ — chấp nhận (dev).

**Gateway:** `appsettings.json` thêm route `cms` (`/cms/{**catch-all}`, `PathRemovePrefix /cms`, `X-Forwarded: Append`) + cluster `cms` → `http://localhost:5290`. Compose `gateway` thêm `ReverseProxy__Clusters__cms__Destinations__primary__Address: http://cms-backend:8080`.

**Dockerfile** `src/AntFarm.Cms.Api/Dockerfile`: chép `AntFarm.Chinese.Api/Dockerfile` (build context `./backend`, tách layer restore gồm props + `shared/` + 4 csproj cms, cache mount NuGet, `publish --no-restore /m:2`, **không** `# syntax=`, không `groupadd`, `HEALTHCHECK wget /health/live`, cổng 8080) **bỏ** phần copy `content/` (cms không có học liệu). Dòng đầu: `# ⚠️ CHƯA VERIFY bằng Docker`.

**Compose** `deploy/docker-compose.yml` thêm (không `ports:`):

```yaml
  cms-backend:
    image: ${REGISTRY:-localhost/antfarm}/cms-backend:${CMS_BACKEND_TAG:-latest}
    build: { context: ../backend, dockerfile: services/cms-backend/src/AntFarm.Cms.Api/Dockerfile }
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__Default: Host=postgres;Port=5432;Database=af_cms;Username=af_cms;Password=${AF_CMS_DB_PASSWORD}
      Auth__Issuer: https://id.${APP_DOMAIN}
      Auth__Audience: af-cms
      Auth__JwksUrl: http://identity-service:8080/.well-known/jwks.json
      Auth__RequireHttpsMetadata: "false"
      CmsAdmin__BootstrapEmails__0: ${CMS_BOOTSTRAP_ADMIN_EMAIL}
      ForwardedHeaders__KnownIPNetworks__0: 127.0.0.1/32
      ForwardedHeaders__KnownIPNetworks__1: <dải af-net y như chinese-backend>
    depends_on: <như chinese-backend>
    networks: [af-net]
    restart: unless-stopped
```
(Chép chính xác khối `chinese-backend` hiện có — kể cả `logging`, `healthcheck` nếu có — rồi đổi tên/biến. `build.context`/`dockerfile` lấy đúng kiểu đường dẫn đang dùng trong file.) `.env.example` thêm `AF_CMS_DB_PASSWORD=`, `CMS_BOOTSTRAP_ADMIN_EMAIL=`, `CMS_BACKEND_TAG=`. `gateway.depends_on` thêm `cms-backend` nếu khối gateway đang liệt kê service phía sau.

**`deploy/VERIFY-DOCKER.md`**: thêm mục "cms-backend (W1) — chưa verify": build ảnh, `wget -qO- http://localhost:8080/health/ready` trong container, tạo DB tay trên volume cũ (§5.1.1).

**README gốc** (mục chạy dev): thêm lệnh `dotnet run --project backend/services/cms-backend/src/AntFarm.Cms.Api --launch-profile http`, bước copy `appsettings.Development.json.example`, tạo DB `af_cms`.

**Test W1 bắt buộc**

UnitTests:
- `DefaultRoleAssignmentPolicy`: email bootstrap (khác hoa thường, có khoảng trắng) ⇒ `[admin]`; email thường + `DefaultRoles=[]` ⇒ rỗng; cấu hình `DefaultRoles=["editor"]` ⇒ `[editor]`.
- `RoleCatalog`: `admin` có đủ 6 quyền; `editor` đúng 3; `support` đúng 2; mọi mã quyền trong catalog ∈ `PermissionCodes.All`.
- `LAST_ADMIN` (nếu logic nằm ở service có thể test không DB; không thì chuyển ApiTests).

ApiTests (`[DbFact]`, `af_cms_test`, token `TestTokenFactory` audience `af-cms`):
1. `GET /api/me` lần đầu với email thường ⇒ 200, `roles: []`, `permissions: []`, tạo dòng `access.users`.
2. Email bootstrap ⇒ `roles: ["admin"]`, 6 quyền.
3. User thường gọi `GET /api/admin/ping` ⇒ 403 body `{ code: "FORBIDDEN" }`; admin ⇒ 200 `{ ok: true }`.
4. Token audience chỉ `af-chinese` ⇒ 401; token hết hạn ⇒ 401; không token gọi `/api/me` ⇒ 401 JSON.
5. `PUT /api/admin/users/{id}/roles` gỡ `admin` của admin cuối ⇒ 422 `LAST_ADMIN`; vai trò lạ ⇒ 422 `UNKNOWN_ROLE`; gán `editor` cho user thường ⇒ 200 và `/api/me` của user đó (sau `Invalidate`) có `posts.manage`.
6. `GET /api/admin/roles` ⇒ 3 vai trò có `name` tiếng Việt.
7. Seeder chạy hai lần ⇒ không nhân đôi, không ném; user đã tồn tại có email thêm vào bootstrap ⇒ được gán `admin` lúc khởi động.
8. `GET /health/live`, `/health/ready`, `/api/system/info` ẩn danh ⇒ 200, `service = "cms-backend"`.
9. Guard: `grep -rn "new string Policy" backend` rỗng (đã có ở chinese — thêm nếu test hiện quét theo service).

Tiêu chí tay: chạy identity 5281 + cms 5290 + gateway 5280; đăng nhập ở `apps/chinese` (3280), lấy access token trong DevTools (hoặc gọi `POST /identity/api/auth/login` qua Scalar), `GET http://localhost:5280/cms/api/me` với Bearer ⇒ thấy vai trò đúng.

#### 5.2.2 W2 — không đổi backend ngoài cấu hình

- identity `appsettings.Development.json.example` `Auth:AllowedOrigins` thêm `http://localhost:3290`; compose identity thêm `Auth__AllowedOrigins__1: https://admin.${APP_DOMAIN}` (giữ `__0` chinese; website **không** gọi identity nên không thêm apex). ⚠️ Máy dev có `appsettings.Development.json` thật (gitignore) ⇒ ghi rõ trong báo cáo feature: người dùng phải tự thêm `http://localhost:3290` vào file thật, nếu không đăng nhập admin trả 403 `ORIGIN_NOT_ALLOWED`.

#### 5.2.3 W3 — API site nền (CHI TIẾT LÀM NGAY — chẻ W3a / W3b)

**Lý do chẻ:** BE+FE đủ 4 nhóm (cấu hình, ngôn ngữ, FAQ, nhật ký) + trình soạn Markdown + thêm dependency vượt ~40 phút code một lượt. **W3a** = cấu hình site/SEO + ngôn ngữ + `GET /api/public/site` + hạ tầng nhật ký/revalidate (BE + FE). **W3b** = FAQ + màn nhật ký + `MarkdownEditor` (BE + FE). Mỗi phần commit riêng.

##### W3a — Backend (chỉ trong `backend/services/cms-backend/**`)

- **Domain** `AntFarm.Cms.Domain/Site/`:
  - `SiteSetting` (`Key`, `Value`, `UpdatedAt`, `UpdatedBy?`; `Create`, `Update(value, actorId, now)` — chỉ đổi `UpdatedAt` khi giá trị khác).
  - `SiteSettingKeys` — whitelist + giá trị mặc định + luật (nguồn DUY NHẤT, validator và seeder đọc từ đây) [BA-mặc định cho độ dài/mặc định]:

    | Khoá | Luật (sau `Trim`) | Mặc định seed |
    |---|---|---|
    | `site.name` | bắt buộc, 1–100 | `AntFarm` |
    | `site.tagline` | ≤ 160 | `Học ngoại ngữ trực tuyến cho người Việt` |
    | `seo.default_title` | ≤ 70 | `AntFarm — Học ngoại ngữ trực tuyến` |
    | `seo.default_description` | ≤ 160 | `""` |
    | `seo.og_image_media_id` | `""` hoặc GUID (W3a không kiểm tồn tại — W4 thêm) | `""` |
    | `contact.email` | `""` hoặc email ≤ 254 | `""` |
    | `social.facebook` · `social.youtube` · `social.tiktok` | `""` hoặc URL `https://` ≤ 300 | `""` |
    | `footer.text` | ≤ 500 | `""` |
    | `home.hero_mode` | `banners` \| `static` | `static` (banner chưa có tới W5) |

  - `Language` (`Id` uuid v7 `Guid.CreateVersion7()`, `Code`, `Name`, `NativeName`, `Tagline`, `DescriptionMarkdown`, `Status`, `AppUrl?`, `AccentColor?`, `CoverMediaId?` (W3a luôn null), `SortOrder`, `UpdatedAt`, `UpdatedBy?`, `uint Version`), enum `LanguageStatus { Open, ComingSoon, Hidden }` lưu `open|coming_soon|hidden`.
  - `AuditLog` (`Create(at, actorId, actorEmail, action, targetType, targetId, summary, success)`; cắt `summary` 500).
- **Application** `AntFarm.Cms.Application/`:
  - `Common/Abstractions/ICmsDbContext` thêm `DbSet<SiteSetting> SiteSettings`, `DbSet<Language> Languages`, `DbSet<AuditLog> AuditLogs`, `void SetOriginalVersion(object entity, uint version)` (chép khuôn `IChineseDbContext`).
  - `Common/Audit/IAuditLogger` + `AuditLogger`: `void Add(Actor actor, string action, string targetType, string? targetId, string summary, bool success = true)` — chỉ `db.AuditLogs.Add`, **không** tự `SaveChanges` (ghi cùng giao dịch với thao tác). `record Actor(Guid Id, string Email)` — controller dựng từ `User.GetAccountId()` + `User.GetEmail()` (`AntFarm.Auth`).
  - `Common/Revalidation/IRevalidationNotifier { Task NotifyAsync(IReadOnlyCollection<string> tags, CancellationToken ct); }` + `NoopRevalidationNotifier` (log Debug) đăng ký trong `AddInfrastructure`. Gọi **sau** `SaveChanges` thành công, tag theo §5.4.3.
  - `Site/SiteSettingsService` (`GetAsync`, `UpdateAsync(actor, values)`), `Site/LanguageAdminService` (`ListAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `ReorderAsync`), `Site/PublicSiteService` (`GetAsync`), DTO trong `Site/Dtos/`, validator FluentValidation `UpdateSiteSettingsRequestValidator`, `CreateLanguageRequestValidator`, `UpdateLanguageRequestValidator`, `ReorderRequestValidator`.
  - Nghiệp vụ ngôn ngữ: `code` `^[a-z][a-z0-9-]{1,31}$`, **bất biến sau khi tạo** (PUT không có `code` — mã dùng làm khoá `interest` nhận tin + đường dẫn website); `name`/`nativeName` 1–60; `tagline` ≤ 160; `descriptionMarkdown` ≤ 4000; `appUrl` ≤ 300, `https://…` hoặc `http://localhost[:port]…`/`http://127.0.0.1[:port]…` (sai ⇒ 400); `status=open` mà `appUrl` rỗng ⇒ **422 `APP_URL_REQUIRED`**; `accentColor` `^#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$` hoặc null. Tạo: `sortOrder = max+1`; trùng `code` ⇒ 409 `CODE_TAKEN` (kiểm trước + bắt `unique_violation` 23505). Sửa: `SetOriginalVersion(entity, version)` ⇒ `DbUpdateConcurrencyException` ⇒ 409 `CONCURRENCY_CONFLICT`. Xoá: xoá cứng, 404 nếu không có. Sắp xếp: `ids` phải **đúng bằng** tập id hiện có (thiếu/thừa/trùng ⇒ 422 `ORDER_MISMATCH`), gán `sortOrder = 1..n`.
  - Cấu hình: PUT phải gửi **đủ 11 khoá**, thiếu/lạ ⇒ 400 `VALIDATION` (`details` theo khoá). Không kiểm concurrency cho cấu hình (một người biên tập — last-write-wins) [BA-mặc định]. Chỉ khoá đổi giá trị mới cập nhật. Không đổi gì ⇒ 200, không ghi nhật ký, không revalidate.
  - Nhật ký W3a — `action`/`targetType`/`targetId`/`summary` (**không** chép giá trị dài/Markdown vào `summary`):
    `site_settings.update` / `site_settings` / null / `Đổi: site.name, seo.default_title` · `language.create|update|delete` / `language` / `<code>` / `Tạo|Sửa|Xoá ngôn ngữ <code>` · `language.reorder` / `language` / null / `Sắp xếp lại ngôn ngữ`.
  - Public: `settings` bỏ khoá có giá trị rỗng; `ogImageUrl` = null (W4 điền); `languages` loại `hidden`, sắp `sortOrder`, `coverUrl` = null; `faqs` = `[]` ở W3a (W3b điền).
- **Infrastructure**: `Persistence/Configurations/Site/{SiteSettingConfiguration, LanguageConfiguration, AuditLogConfiguration}.cs` (schema `site`, snake_case, CHECK `status`, `xmin` row version, chỉ mục §5.1.2); `CmsDbContext` thêm DbSet + `SetOriginalVersion`; migration `W3a_SiteBasics`:
  `dotnet ef migrations add W3a_SiteBasics --project backend/services/cms-backend/src/AntFarm.Cms.Infrastructure --startup-project backend/services/cms-backend/src/AntFarm.Cms.Api --output-dir Persistence/Migrations`.
  `Seeding/SiteSeeder` chạy **sau** `AccessSeeder`, bọc `try/catch` log Error **không ném**: settings chèn bù **khoá thiếu** (không ghi đè giá trị có sẵn); languages **chỉ khi bảng trống**, đọc `CmsSeed:Languages` (mảng `{code,name,nativeName,tagline,status,appUrl,sortOrder}`), thiếu cấu hình ⇒ mặc định trong code: `chinese` "Tiếng Trung"/"中文"/open/`https://chinese.antfarms.xyz`/1, `english` "Tiếng Anh"/"English"/coming_soon/2, `japanese` "Tiếng Nhật"/"日本語"/coming_soon/3. `appsettings.Development.json.example` của cms ghi đè `appUrl` chinese = `http://localhost:3280`.
- **Api** `Features/Admin/SiteSettingsController` (`[RequirePermission(PermissionCodes.SiteManage)]`), `Features/Admin/LanguagesController` (cùng quyền; route `order` khai **trước** `{id:guid}` không xung đột vì ràng buộc `:guid`), `Features/Public/PublicSiteController` (`[AllowAnonymous]` tường minh — FallbackPolicy đang `RequireAuthenticatedUser`; `Response.Headers.CacheControl = "public, max-age=60"`). `UserProvisioningMiddleware` đã bỏ qua request ẩn danh (kiểm `IsAuthenticated`) — giữ nguyên.
- **Test bắt buộc W3a** (`tests/AntFarm.Cms.ApiTests/Site/`, `[DbFact]`; `tests/AntFarm.Cms.UnitTests/Site/`):
  1. Unit `SiteSettingKeysTests`: đủ 11 khoá, mặc định hợp lệ theo chính luật của nó; URL `http://` social bị từ chối; `home.hero_mode=abc` bị từ chối.
  2. Không token ⇒ 401; token không có `site.manage` (vai trò `support`) ⇒ 403 cho **mọi** endpoint admin W3a.
  3. GET settings sau seed ⇒ đủ 11 khoá; PUT thiếu khoá / khoá lạ / quá dài ⇒ 400; PUT hợp lệ ⇒ 200 + đúng 1 dòng `audit_logs` có `actor_email` người gọi, `summary` liệt kê khoá, không chứa giá trị.
  4. Languages: tạo ⇒ 201 `sortOrder` cuối; trùng code ⇒ 409 `CODE_TAKEN`; `open` thiếu `appUrl` ⇒ 422 `APP_URL_REQUIRED`; PUT `version` cũ ⇒ 409 `CONCURRENCY_CONFLICT`; order thiếu id ⇒ 422 `ORDER_MISMATCH`; order đúng ⇒ 204 + GET theo thứ tự mới; xoá ⇒ 204, xoá lại ⇒ 404.
  5. Public ẩn danh ⇒ 200, header `Cache-Control: public, max-age=60`, không có `hidden`, sắp đúng, khoá rỗng bị bỏ, `faqs` là mảng.
  6. Seeder chạy 2 lần ⇒ không nhân đôi; xoá 1 ngôn ngữ rồi chạy seeder ⇒ không mọc lại; xoá 1 dòng settings rồi chạy seeder ⇒ khoá được chèn bù, các khoá khác giữ giá trị đã sửa.

##### W3b — Backend (chỉ trong `backend/services/cms-backend/**`)

- Domain `Site/Faq` (`Id`, `Question`, `AnswerMarkdown`, `GroupKey`, `SortOrder`, `IsPublished`, `UpdatedAt`, `UpdatedBy?`, `uint Version`). `ICmsDbContext` + `DbSet<Faq> Faqs`. Migration `W3b_Faqs`. Không seed.
- `Site/FaqAdminService` + validators: `question` 1–300; `answerMarkdown` 1–10000; `groupKey` `^[a-z0-9-]{1,32}$` (mặc định `general`); `isPublished` bool. Tạo: `sortOrder = max+1` **trong nhóm**. Đổi `groupKey` khi sửa ⇒ đưa về cuối nhóm mới. PUT kèm `version` ⇒ 409 `CONCURRENCY_CONFLICT`. Sắp xếp `PUT /api/admin/faqs/order { groupKey, ids }` — `ids` đúng bằng tập FAQ của nhóm ⇒ nếu không 422 `ORDER_MISMATCH`.
- Nhật ký: `faq.create|update|delete|reorder`, `targetType=faq`, `targetId=<id>`, summary `Tạo/Sửa/Xoá FAQ: <60 ký tự đầu câu hỏi>`. Revalidate tag `site`, `faqs`.
- `Site/AuditLogQueryService` + `Features/Admin/AuditLogsController` `[RequirePermission(PermissionCodes.UsersManage)]`: `GET /api/admin/audit-logs?targetType=&targetId=&page=1&pageSize=50` (pageSize 1–100, `targetType` ≤ 32, `targetId` ≤ 64), sắp `at DESC, id DESC`.
- `PublicSiteService` điền `faqs`: `is_published = true`, sắp `group_key, sort_order`.
- **Test bắt buộc W3b:** CRUD FAQ + 403 thiếu quyền + concurrency 409 + order mismatch 422; public chỉ trả `isPublished=true`; audit-logs: `editor` ⇒ 403, `admin` ⇒ 200 có phân trang + lọc `targetType=faq` đúng; mọi thao tác ghi FAQ sinh 1 dòng nhật ký đúng actor.

#### 5.2.4 W4 — Media MinIO (thiết kế)

- Package `Minio` (kiểm version thật trên NuGet, khai vào `Directory.Packages.props`). `Storage` options: `Endpoint` (dev `localhost:9000`, Docker `minio:9000`), `AccessKey`, `SecretKey`, `Bucket=af-cms`, `UseSsl=false`. Khởi động: tạo bucket nếu thiếu (fail-soft: log Error, endpoint media trả 503 `STORAGE_UNAVAILABLE`, service vẫn chạy).
- `IMediaStorage` (Application) + `MinioMediaStorage` (Infrastructure). Kích thước ảnh đọc bằng thư viện nhẹ **chỉ đọc header** (vd `SixLabors.ImageSharp` `Image.Identify` — kiểm giấy phép: ImageSharp 3.x dùng Six Labors Split License, miễn phí cho dự án nguồn mở/doanh thu nhỏ ⇒ **BA-mặc định: không dùng**, tự đọc header PNG/JPEG/WebP/GIF bằng code ~80 dòng có unit test; không đọc được ⇒ `width/height = null`).
- `POST /api/admin/media` (multipart `file`, `altText`; `media.manage`; giới hạn `RequestSizeLimit 5 MB + overhead`) → 201 `MediaDto`. `GET /api/admin/media?q&page` · `PUT /api/admin/media/{id}` (alt) · `DELETE /api/admin/media/{id}` (409 `MEDIA_IN_USE`).
- `GET /api/public/media/{id}/{fileName}` ẩn danh: stream từ MinIO, `Content-Type` đã lưu, `X-Content-Type-Options: nosniff`, cache immutable, `ETag = id`; `fileName` sai ⇒ vẫn trả (chỉ để URL đẹp/SEO).
- Dev compose `minio-init` thêm `mc mb --ignore-existing antfarm/af-cms`.

#### 5.2.5 W5 — Trang tĩnh + banner (thiết kế)

`Features/Admin/PagesController` (`site.manage`: list/get/create/put đủ bản ghi/publish/unpublish/delete — xoá chỉ khi `draft`, 422 `PAGE_PUBLISHED` nếu đang xuất bản), `BannersController` (CRUD + order). Public: `GET /api/public/pages/{slug}`, `GET /api/public/pages?footer=true` (danh sách link chân trang), `GET /api/public/banners` (active, trong khoảng `starts_at/ends_at` so với `now` UTC). Tag revalidate §5.4.3.

#### 5.2.6 W6 — Bài viết (thiết kế)

`PostCategoriesController`, `PostsController` (`posts.manage`; `publish` đặt `published_at = now` nếu null, cho phép đặt ngày tương lai ⇒ API công khai ẩn tới lúc đó — website thấy khi ISR hết hạn 3600s hoặc lần revalidate kế). Public: `GET /api/public/posts?category=&page=&pageSize=12` (không kèm `bodyMarkdown`), `GET /api/public/posts/{slug}` (kèm bài liên quan: tối đa 3 cùng danh mục), `GET /api/public/post-categories`, `GET /api/public/sitemap` (slug + `updatedAt` của pages/posts published — cho `sitemap.ts`).

#### 5.2.7 W7 — Revalidation thật (thiết kế)

`Infrastructure/Revalidation/HttpRevalidationNotifier` (mẫu MedDental `RevalidationService`): `Revalidation:WebhookUrl` (dev `http://localhost:3281/api/revalidate`, Docker `http://website:3000/api/revalidate`), `Revalidation:Secret` (env `REVALIDATE_SECRET`, **cùng giá trị** ở website), `HttpClient` timeout 5s, POST `{ "tags": [...] }` header `X-Revalidate-Secret`; URL rỗng ⇒ bỏ qua (log Debug); lỗi ⇒ log Warning, **không ném**. Gọi **sau** commit DB, chạy nền không chặn response (`Task.Run` có log lỗi, hoặc `Channel` + `BackgroundService` — **BA-mặc định: Channel + BackgroundService**, tránh mất lỗi trong fire-and-forget).

#### 5.2.8 W9 — Hộp thư (thiết kế)

Public: `POST /api/public/contact`, `POST /api/public/newsletter` (rate limiter policy `public-forms` phân vùng theo IP thật sau `UseForwardedHeaders`, R-W30). Admin (`inbox.manage`): `GET /api/admin/contacts?status&from&to&q&page` (ngày: `DateOnly` query ⇒ quy về mốc UTC của 00:00 `Asia/Ho_Chi_Minh`, nửa hở — **không** dùng `DateTime` query trần, CLAUDE.md), `PUT /api/admin/contacts/{id}/status` `{ status, note }`, `GET /api/admin/contacts/export.csv?...`, tương tự `newsletter` (`PUT .../{id}/unsubscribe`, `export.csv`).

#### 5.2.9 W10 — identity-service API nội bộ (CHI TIẾT LÀM NGAY — bổ sung 17/09/2026, ưu tiên hơn các gạch đầu dòng "thiết kế" ngay dưới nếu lệch)

**Phạm vi file: CHỈ `backend/services/identity-service/**`** (kể cả `Dockerfile`, `launchSettings.json`, `appsettings.json`, `appsettings.Development.json.example`, tests). Không sửa `shared/`, gateway, cms-backend, `deploy/`, `CLAUDE.md` — phần đó ở §8.1.

- **Options** `Application/Common/Options/InternalOptions.cs`: `int Port` (mặc định 0), `string ServiceKey` (mặc định `""`). Bind section `Internal` trong `Program.cs`, singleton instance (khuôn `AuthOptions`). **Bật** khi `Port > 0` **và** `ServiceKey.Length >= 32`; `Port > 0` mà key ngắn ⇒ log **Warning** lúc khởi động "API nội bộ bị tắt: Internal:ServiceKey < 32 ký tự" và coi như tắt (không ném — service vẫn phải chạy). `appsettings.json`: `"Internal": { "Port": 8081, "ServiceKey": "" }`. `appsettings.Development.json.example`: `"Internal": { "Port": 5291, "ServiceKey": "dev-internal-key-change-me-0123456789abcdef" }` (cms-backend W11 dùng CÙNG chuỗi). `launchSettings.json` profile `http`: `"applicationUrl": "http://localhost:5281;http://localhost:5291"`. `Dockerfile`: `ENV ASPNETCORE_URLS=http://+:8080;http://+:8081` + `EXPOSE 8080 8081` (EXPOSE chỉ tài liệu; compose vẫn **không** `ports:`), HEALTHCHECK giữ cổng 8080. Ghi "chưa verify Docker".
- **Chốt chặn** `Api/Internal/InternalAccessPolicy.cs` — hàm THUẦN (unit test được):
  `static InternalAccessDecision Evaluate(bool isInternalPath, int localPort, InternalOptions o, string? providedKey)` ⇒ `PassThrough | NotFound | Unauthorized | Allow`:
  - Internal tắt: `isInternalPath` ⇒ `NotFound`; ngược lại `PassThrough`.
  - `localPort == o.Port` (cổng nội bộ): `!isInternalPath` ⇒ `NotFound` (cổng nội bộ không phục vụ API công khai/health/JWKS); key thiếu/sai (so bằng `CryptographicOperations.FixedTimeEquals` trên UTF-8 bytes, độ dài khác ⇒ sai) ⇒ `Unauthorized`; đúng ⇒ `Allow`.
  - `localPort != o.Port` (cổng công khai): `isInternalPath` ⇒ `NotFound`; ngược lại `PassThrough`.
  - `isInternalPath` = `Request.Path.StartsWithSegments("/internal")`.
- `Api/Internal/ILocalPortAccessor` (`int GetLocalPort(HttpContext)`), bản thật `ConnectionLocalPortAccessor` ⇒ `ctx.Connection.LocalPort` (singleton). **Không** đọc header `Host`. ApiTests thay bằng accessor đọc header test (§test) — TestServer có `LocalPort = 0`.
- `Api/Internal/InternalAccessMiddleware.cs`: `NotFound` ⇒ 404 body trống (không JSON lộ thông tin); `Unauthorized` ⇒ 401 `{ error: "Khoá dịch vụ không hợp lệ.", code: "SERVICE_KEY_INVALID" }` + log Warning (IP nguồn, path; **không** log key). Đặt trong pipeline **ngay sau `UseAfExceptionHandler()`**, TRƯỚC `UseIdentityCors`/`UseRateLimiter`/`UseAuthentication` (route nội bộ không CORS, không rate limit "auth", không JWT).
- **Actor**: `Api/Internal/InternalActor.cs` đọc `X-Actor-Id` (GUID) + `X-Actor-Email` (≤ 254) — **bắt buộc với mọi POST/PUT** `/internal/*`, thiếu/sai định dạng ⇒ 400 `{ code: "VALIDATION", details: { "X-Actor-Id": [...] } }` (ném `ValidationAppException`). GET không cần.
- **Controllers** `Api/Features/Internal/{InternalPingController, InternalAccountsController, InternalStatsController, InternalSettingsController}` — `[AllowAnonymous]`, `[ApiExplorerSettings(IgnoreApi = true)]` (không lộ trong Scalar), không `[ValidateOrigin]`. Route theo §6.5.
- **Domain `Account`** thêm: `Disable(now)` (`IsActive=false`, `UpdatedAt`); `Enable(now)` (`IsActive=true`, `LockoutUntil=null`, `FailedLoginCount=0`); `ClearLockout(now)` (`LockoutUntil=null`, `FailedLoginCount=0`); `ResetPasswordByAdmin(hash, now)` (đổi hash, `PasswordChangedAt=now`, xoá lockout + `FailedLoginCount=0`). Idempotent: Disable tài khoản đã khoá ⇒ không lỗi, vẫn 200. `RefreshToken.Revoke(now, reason)` **đã có** — dùng lại.
- **Application `Admin/`**:
  - `AccountAdminService`: `ListAsync(q, status, page, pageSize)` — `q` ≤ 100, khớp `email_normalized LIKE %q_lower%` hoặc `display_name ILIKE %q%` (escape `%`/`_`); `status`: `active` = `IsActive && !(LockoutUntil > now)`, `disabled` = `!IsActive`, `locked` = `IsActive && LockoutUntil > now`; page ≥ 1, pageSize 1–100 (mặc định 20); sắp `CreatedAt DESC`. `GetAsync(id)` kèm `activeSessionCount` = số **family** có token `RevokedAt == null && RotatedAt == null && ExpiresAt > now`. `DisableAsync`/`EnableAsync`/`ClearLockoutAsync`/`ResetPasswordAsync`/`RevokeSessionsAsync` (actor, id). Lý do thu hồi: `admin_disabled`, `admin_password_reset`, `admin_revoked` — thu hồi **mọi** token `RevokedAt == null` của tài khoản. `actor.Id == id` với disable/reset-password/revoke-sessions ⇒ 422 `SELF_ACTION_FORBIDDEN` (`BusinessRuleException`). Không có tài khoản ⇒ 404 `NotFoundException`. Log Information `"Admin {ActorEmail} {Action} tài khoản {AccountId}"` — **không bao giờ** đưa mật khẩu/hash vào log hay exception.
  - `TemporaryPasswordGenerator` (interface `ITemporaryPasswordGenerator` để test): 16 ký tự từ bảng `ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789` (bỏ `I l 1 O 0 o`), `RandomNumberGenerator.GetItems`. `newPassword` admin tự nhập: 8–128 ký tự (R-A2, dùng lại luật validator đăng ký) ⇒ response `temporaryPassword: null`.
  - `RegistrationStatsService.GetAsync(DateOnly? from, DateOnly? to)`: mặc định `to` = hôm nay theo `Asia/Ho_Chi_Minh` (`TimeZoneInfo.FindSystemTimeZoneById`), `from = to - 29 ngày`; `from > to` hoặc số ngày > 366 ⇒ 400 `VALIDATION`. Cận UTC: `fromUtc` = 00:00 VN của `from` đổi sang UTC (`DateTime.SpecifyKind(..., Utc)`), `toUtcExclusive` = 00:00 VN của `to+1`; truy vấn `CreatedAt >= fromUtc && < toUtcExclusive`, kéo **chỉ cột `CreatedAt`** về bộ nhớ, nhóm theo ngày VN; `days` đủ mọi ngày (0 ⇒ `count: 0`). `totalAccounts`, `disabledAccounts` (`!IsActive`), `activeLast7Days`/`activeLast30Days` (`LastLoginAt >= now - 7/30 ngày`). Tham số bind kiểu `DateOnly?` (không `DateTime` — CLAUDE.md).
  - `IRegistrationGate` (Application) + `RegistrationGate` (singleton, `IServiceScopeFactory` + `TimeProvider` + `AuthOptions`): `Task<RegistrationState> GetAsync(ct)` (`Enabled`, `Source: database|configuration`, `UpdatedAt?`), cache **30 giây**; `void Invalidate()`. Không có dòng ⇒ `authOptions.AllowRegistration`, `source=configuration`. Giá trị DB không phải `true|false` ⇒ log Warning, dùng cấu hình.
  - `PlatformSettingsService.SetRegistrationAsync(actor, enabled)`: upsert dòng `registration.enabled`, `SaveChanges`, `gate.Invalidate()`, trả trạng thái mới.
  - **Sửa `AuthService.RegisterAsync`**: thay `if (!authOptions.AllowRegistration)` bằng `if (!(await registrationGate.GetAsync(ct)).Enabled)` — giữ nguyên 403 `REGISTRATION_CLOSED`. Test F2 hiện có phải xanh không đổi.
- **Test bắt buộc W10** (`tests/AntFarm.Identity.UnitTests/Internal/`, `tests/AntFarm.Identity.ApiTests/Internal/`):
  1. Unit `InternalAccessPolicyTests` — bảng đủ ca: tắt (port 0 / key < 32) ⇒ `/internal` 404, route thường PassThrough; cổng công khai + `/internal/*` ⇒ `NotFound` **kể cả key đúng**; cổng nội bộ + route thường ⇒ `NotFound`; cổng nội bộ + key sai/thiếu/khác độ dài ⇒ `Unauthorized`; đúng ⇒ `Allow`.
  2. Unit `TemporaryPasswordGeneratorTests` (16 ký tự, chỉ ký tự trong bảng, 1000 lần không trùng), `AccountTests` (Disable/Enable/ClearLockout/ResetPasswordByAdmin), `RegistrationStatsServiceTests`/hàm thuần tính ngày: tài khoản tạo **23:30 VN ngày D** (16:30Z) và **00:30 VN ngày D+1** (17:30Z) rơi đúng 2 ngày khác nhau; ngày trống ⇒ 0; 367 ngày ⇒ lỗi.
  3. ApiTests: factory `IdentityDbApiFactory` con (vd `IdentityInternalApiFactory`) cấu hình `Internal:Port=5291`, key test ≥ 32 ký tự, thay `ILocalPortAccessor` bằng bản đọc header `X-Test-Local-Port` (chỉ tồn tại trong project test). Ca: **`GET /internal/ping` với cổng 5281 (công khai) + key đúng ⇒ 404**; cổng 5291 thiếu key ⇒ 401 `SERVICE_KEY_INVALID`; key sai ⇒ 401; đúng ⇒ 200; `GET /api/health`… (route công khai bất kỳ, vd `/.well-known/jwks.json`) qua cổng 5291 ⇒ 404; POST thiếu `X-Actor-Id` ⇒ 400.
  4. Luồng: đăng ký + đăng nhập tài khoản A (lấy cookie refresh) → `disable` ⇒ refresh bằng cookie cũ không cấp token mới (403 `ACCOUNT_DISABLED` hoặc 401 — khớp hành vi `RefreshAsync` hiện tại), login ⇒ 403 `ACCOUNT_DISABLED`; `enable` ⇒ login lại được. `reset-password` không body ⇒ 200 có `temporaryPassword` 16 ký tự + header `Cache-Control: no-store`, `revokedSessions ≥ 1`; login mật khẩu cũ ⇒ 401, mật khẩu tạm ⇒ 200; refresh cũ ⇒ không dùng được. `newPassword` 7 ký tự ⇒ 400. Actor = chính A ⇒ 422 `SELF_ACTION_FORBIDDEN` (disable, reset, revoke). `revoke-sessions` ⇒ `revokedSessions` đúng số, refresh cũ hỏng. `clear-lockout` sau khi đăng nhập sai đủ `MaxFailedLogins` ⇒ login đúng được ngay.
  5. Cài đặt: GET khi chưa có dòng ⇒ `source=configuration`; `PUT { enabled:false }` ⇒ `POST /api/auth/register` ⇒ 403 `REGISTRATION_CLOSED` **ngay** (PUT gọi `Invalidate`); `PUT true` ⇒ đăng ký lại được; list/get: `status=disabled` lọc đúng, 404 id lạ.
  6. Log mật khẩu: test dùng sink bộ nhớ (`Serilog.Sinks.InMemory` nếu đã có trong CPM; **không có thì không thêm package** — thay bằng review code + `grep -rn "temporaryPassword\|newPassword" backend/services/identity-service/src --include=*.cs` chỉ xuất hiện ở DTO/service trả về, không trong `Log.`/`logger.`).

- Cấu hình mới section `Internal`: `Port` (dev 5291, Docker 8081), `ServiceKey` (env `IDENTITY_INTERNAL_KEY`, ≥ 32 ký tự; rỗng ⇒ tắt). `launchSettings` profile http: `applicationUrl = "http://localhost:5281;http://localhost:5291"`; Docker `ASPNETCORE_URLS=http://+:8080;http://+:8081`.
- `Api/Internal/InternalEndpointFilter` (hoặc middleware nhánh `app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/internal"))`): kiểm R-W4 **trước** authentication; controller nội bộ `[AllowAnonymous]` đối với JWT (xác thực bằng service key) và **không** áp CORS/`ValidateOrigin`. Mọi request `/internal/*` tới cổng 8080 ⇒ 404; request không phải `/internal/*` tới 8081 ⇒ 404 (cổng nội bộ chỉ phục vụ nội bộ).
- Header `X-Actor-Id`, `X-Actor-Email` (bắt buộc với thao tác ghi, 400 nếu thiếu) — identity dùng để kiểm R-W17 (actor ≠ target) và ghi log Information (không ghi mật khẩu).
- Domain `Account`: thêm `Disable(now)`, `Enable(now)` (xoá lockout), `ClearLockout(now)`, `ResetPasswordByAdmin(hash, now)`; `RefreshToken.Revoke(now, reason)` nếu chưa có.
- Application `Admin/AccountAdminService`, `Admin/RegistrationStatsService`, `Admin/PlatformSettingsService` (+ `IRegistrationGate` dùng trong `AuthService.RegisterAsync` thay chỗ đọc `authOptions.AllowRegistration`, cache 30s, `Invalidate()` khi PUT), `Admin/TemporaryPasswordGenerator`.
- Endpoint: §6.5. Test: route nội bộ qua cổng công khai ⇒ 404 (TestServer: giả `Connection.LocalPort` bằng middleware test hoặc tách logic kiểm cổng ra hàm thuần để unit test + một ApiTest với `Internal:Port` khác cổng TestServer); thiếu/sai key ⇒ 401; khoá ⇒ refresh cũ trả 403 `ACCOUNT_DISABLED`/401; reset ⇒ đăng nhập bằng mật khẩu tạm được, mật khẩu cũ 401, refresh cũ bị thu hồi; tự khoá mình ⇒ 422; tắt đăng ký ⇒ `register` 403 `REGISTRATION_CLOSED` trong ≤ cache (dùng `TimeProvider` giả hoặc gọi `Invalidate`); thống kê ranh giới 23:30/00:30 VN.

#### 5.2.10 W11 — cms-backend proxy tài khoản (thiết kế)

`Infrastructure/Identity/IdentityInternalClient` (typed `HttpClient`, `IdentityInternal:BaseUrl` dev `http://localhost:5291` / Docker `http://identity-service:8081`, `IdentityInternal:ServiceKey` = cùng `IDENTITY_INTERNAL_KEY`, timeout 10s; gắn `X-Actor-*` từ claim người gọi). Lỗi mạng ⇒ 503 `IDENTITY_UNAVAILABLE`; mã lỗi nghiệp vụ của identity (422/404/409) **chuyển tiếp nguyên** `code`. `Features/Admin/AccountsController` (`accounts.manage`) + ghi `IAuditLogger` mọi thao tác ghi (R-W20; **không** ghi mật khẩu tạm; response reset-password đi thẳng về client và có `Cache-Control: no-store`).

#### 5.2.11 W12–W14 — không đổi backend

---

### 5.3 Frontend

#### 5.3.1 W2 — `apps/admin` khung + đăng nhập + gộp quyền + màn Người dùng CMS (CHI TIẾT LÀM NGAY)

**Cây thư mục:**

```
frontend/apps/admin/
  package.json, index.html, vite.config.ts, tsconfig.json, tsconfig.app.json, tsconfig.node.json
  Dockerfile, nginx.conf
  src/
    main.tsx                 ThemeProvider(@af/ui, màu nhấn trung tính khác chinese) → QueryClientProvider → ConfirmProvider/ToastProvider → AuthProvider → RouterProvider
    vite-env.d.ts            VITE_IDENTITY_API_URL
    api/clients.ts           authSession, identityApi, identity, cmsApi('/cms/api'), languageApis: Record<string, AxiosInstance> (chinese: '/chinese/api')
    auth/permissions.ts      hằng CMS_PERMS = { SITE_MANAGE: 'cms:site.manage', ... }, perm(service, code) => `${service}:${code}`
    auth/loadMe.ts           gộp /me (dưới)
    modules/registry.ts      danh sách module ngôn ngữ (W2: rỗng hoặc chỉ khai 'chinese' với nav rỗng; W13 thêm màn)
    layout/AdminLayout.tsx   AppLayout + buildNavItems(perms, meta)
    router.tsx
    pages/DashboardPage.tsx  thẻ chào + danh sách "bạn có quyền gì ở đâu" + cảnh báo service không phản hồi
    pages/LoginPage.tsx      bọc LoginPage của @af/auth (không có link Đăng ký — admin không cho đăng ký tại đây)
    pages/ForbiddenPage.tsx  ErrorPage 403 + nút Đăng xuất (tài khoản 0 quyền)
    features/cms-users/{api.ts,hooks.ts,types.ts,pages/CmsUsersPage.tsx,components/EditRolesDialog.tsx}
```

**`package.json`**: chép `apps/chinese/package.json`, đổi `name: "@af/admin"`, **bỏ** `hanzi-writer`, `vitest` + script test (thêm lại khi có test thuần), giữ mọi peer dependency của `@af/ui|api|auth|utils` (`@emotion/*`, `@mui/*`, `@tanstack/react-query`, `axios`, `react-hook-form`, `@hookform/resolvers`, `zod`, `react-router-dom`, `react-is`). Script `lint:ui` giống chinese. Chạy `yarn install` ở `frontend/` để cập nhật `yarn.lock`.

**`vite.config.ts`**: như chinese, `port: 3290`, `strictPort`, proxy `/identity`, `/cms`, `/chinese` → `http://localhost:5280`. Comment: route SPA không được bắt đầu bằng `/identity`, `/cms`, `/chinese` hoặc tiền tố ngôn ngữ tương lai ⇒ quy ước **mọi route admin tiếng Việt không dấu** và module ngôn ngữ dùng tiền tố `/ngon-ngu/<code>/...` (vd `/ngon-ngu/chinese/bai-hoc`).

**`auth/loadMe.ts`** (hợp đồng hành vi):

```ts
export interface ServiceMe { id: string; email: string; displayName: string; roles: string[]; permissions: string[] }
export type ServiceState = { status: 'ok'; me: ServiceMe } | { status: 'unavailable'; error: unknown }
export interface AdminMeInfo extends MeInfo {
  permissions: string[]                    // đã gắn tiền tố: 'cms:site.manage', 'chinese:content.manage'
  services: Record<string, ServiceState>   // 'cms', 'chinese', ...
}
// Gọi song song GET /cms/api/me và GET /<lang>/api/me cho mọi module trong registry — Promise.allSettled,
// mọi lời gọi truyền skipErrorRedirect: true (lời gọi nền lúc mở app, CLAUDE.md).
// 401 ⇒ để @af/api làm mới/đưa về đăng nhập như bình thường (ném lại).
// Lỗi khác (mạng, 5xx, 404) ⇒ services[x] = unavailable, không có quyền của service đó, KHÔNG ném.
// Chỉ ném khi TẤT CẢ service đều unavailable (để AuthProvider ghi meError và app hiện màn lỗi có nút thử lại).
```

**Điều hướng quyền (router):**
- `/dang-nhap` (công khai), `/401`, `/403`, `/404`, `*` → `/404`.
- `RequireAuth` bọc nhánh chính; ngay trong nhánh: `me.permissions.length === 0` và không có service nào `unavailable` ⇒ `<Navigate to="/403">` (R-W2). Có service unavailable mà 0 quyền ⇒ hiện Dashboard với cảnh báo (không đẩy 403 oan khi service chết).
- `/` Dashboard (mọi người đăng nhập có ≥1 quyền).
- `/nguoi-dung-cms` — `RequirePermission permission="cms:users.manage"`.
- Các route W3+ thêm dần theo §5.3.3.

**Nav (`buildNavItems`)**: nhóm "Website" (Cấu hình, Ngôn ngữ, FAQ, Trang, Banner, Bài viết, Ảnh — W3–W6), "Hộp thư" (W9), "Tài khoản nền tảng" (W11), "Ngôn ngữ › Tiếng Trung" (W13), "Hệ thống › Người dùng CMS, Nhật ký". W2 chỉ có Tổng quan + Người dùng CMS. Mục thiếu quyền **ẩn**; Dashboard liệt kê "Bạn chưa có quyền X — liên hệ quản trị viên" cho các nhóm bị ẩn (quy tắc nút ẩn phải có lời giải thích).

**Màn `CmsUsersPage`** (chép hành vi `apps/chinese/src/features/admin-users`): bảng email/tên/vai trò/lần đầu/lần cuối; ô tìm (debounce 300ms, `q` lên URL); phân trang lên URL; nút "Sửa vai trò" mở `AppDialog` checkbox 3 vai trò (tên tiếng Việt từ `GET /cms/api/admin/roles`, liệt kê quyền của từng vai trò bên dưới); lỗi 422 `LAST_ADMIN` báo tại chỗ; sau lưu `invalidate` danh sách, và nếu sửa chính mình ⇒ `reloadMe()`. Dải `Alert` info giải thích: "Người dùng xuất hiện ở đây sau lần đầu mở trang Admin. Tài khoản mới không có quyền nào cho tới khi được gán vai trò." Mobile 375px: bảng chuyển thành danh sách thẻ (như chinese).

**`Dockerfile`**: chép `apps/chinese/Dockerfile`, đổi `apps/chinese` → `apps/admin`, `@af/chinese` → `@af/admin`; COPY `package.json` của đúng 5 package `@af/*` hiện có (W13 thêm `chinese-kit`). `nginx.conf` chép chinese (SPA fallback + khối `.mjs`). Ghi "CHƯA VERIFY".

**Compose**: `admin-frontend` (image `${REGISTRY}/admin-frontend:${ADMIN_FRONTEND_TAG:-latest}`, `build.args.VITE_IDENTITY_API_URL: https://id.${APP_DOMAIN}/api`, không ports).

**nginx biên** `deploy/conf/nginx.conf.example` thêm khối (chép khối `chinese.`):

```nginx
# ── Admin kiêm CMS — admin.antfarms.xyz (W2) ──
server {
    listen 443 ssl; http2 on;
    server_name <ADMIN_DOMAIN>;              # admin.antfarms.xyz
    # ... ssl/HSTS/header y khối chinese ...
    add_header X-Robots-Tag "noindex, nofollow" always;   # trang quản trị không lên công cụ tìm kiếm
    client_max_body_size 6m;                 # tải ảnh 5 MB (W4)
    location ^~ /cms/     { set $upstream gateway:8080;  proxy_pass http://$upstream$request_uri; <khối proxy header như /chinese/> }
    location ^~ /chinese/ { set $upstream gateway:8080;  proxy_pass http://$upstream$request_uri; <như trên> }
    location / { set $upstream admin-frontend:80; proxy_pass http://$upstream$request_uri; <như khối / của chinese> }
}
```
`index.html` của admin có `<meta name="robots" content="noindex">`.

**Tiêu chí tay W2** (gateway + identity + chinese + cms + admin dev):
1. `http://localhost:3290` chưa đăng nhập ⇒ `/dang-nhap`; đăng nhập email bootstrap ⇒ Dashboard + mục Người dùng CMS.
2. Đăng nhập tài khoản thường (chưa gán gì ở CMS) ⇒ `/403` có nút Đăng xuất (dù tài khoản có `learner` ở chinese — `chinese:study.use` **không** tính là quyền admin: `loadMe` chỉ giữ từ chinese các quyền `users.manage`, `content.manage`; ghi hằng `ADMIN_RELEVANT_PERMISSIONS` trong registry).
3. Admin gán `editor` cho tài khoản thường ⇒ tài khoản đó F5 thấy Dashboard (chưa có mục website nào vì W3 chưa làm — Dashboard nói rõ).
4. Tắt chinese-backend ⇒ admin vẫn mở, Dashboard báo "Tiếng Trung: không phản hồi".
5. 375px: layout không vỡ, bảng người dùng thành thẻ.
6. `yarn workspace @af/admin tsc -b`, `build`, `yarn lint:ui` sạch.

#### 5.3.2 Quy ước quyền trong admin

- Hàm `can('cms:site.manage')` dùng thẳng `useAuth().can` (permissions đã có tiền tố).
- Module ngôn ngữ khai `{ code: 'chinese', label: 'Tiếng Trung', apiBase: '/chinese/api', adminPermissions: ['users.manage','content.manage'], nav(perms), routes }`. Thêm ngôn ngữ = thêm một mục registry + thư mục `src/modules/<code>/` + location nginx `/<code>/` trên `admin.` + proxy Vite.

#### 5.3.3 W3–W6, W9, W11 — màn admin (thiết kế)

| Route | Quyền | Màn |
|---|---|---|
| `/website/cau-hinh` | `cms:site.manage` | Form nhóm (Thông tin chung, SEO mặc định + ảnh OG, Liên hệ & mạng xã hội, Chân trang) — `DialogGrid` không cần vì là trang; lưu một lần cả bộ |
| `/website/ngon-ngu` | `cms:site.manage` | Danh sách thẻ kéo thả thứ tự (`@dnd-kit/sortable` — **BA-mặc định**: nút lên/xuống giống `ReorderButtons` của F10 để khỏi thêm dependency; dnd-kit để sau) + `AppDrawer` sửa |
| `/website/faq` | `cms:site.manage` | Danh sách theo nhóm, sửa trong `AppDialog` (`maxWidth="md"`), Markdown editor |
| `/website/trang`, `/website/trang/:id` | `cms:site.manage` | Danh sách + trang soạn toàn màn (tiêu đề, slug gợi ý, Markdown + xem trước tab, SEO, trạng thái), `StickyActionBar` Lưu/Xuất bản, chặn rời trang khi chưa lưu (chép `useUnsavedChangesGuard` của F10) |
| `/website/banner` | `cms:site.manage` | Danh sách + `AppDialog` với MediaPicker hai ảnh |
| `/website/bai-viet`, `/website/bai-viet/:id`, `/website/danh-muc` | `cms:posts.manage` | Như trang tĩnh + ảnh bìa, danh mục, tóm tắt, ngày xuất bản |
| `/website/anh` | `cms:media.manage` | Lưới ảnh, tải lên kéo thả/nhiều file, sửa alt, xoá (hiện nơi đang dùng khi 409) |
| `/hop-thu/lien-he`, `/hop-thu/nhan-tin` | `cms:inbox.manage` | Bảng lọc trạng thái/khoảng ngày (lên URL), chi tiết trong `AppDrawer`, nút Xuất CSV (tải file qua axios `responseType: 'blob'`) |
| `/tai-khoan`, `/tai-khoan/:id` | `cms:accounts.manage` | Tìm theo email/tên, lọc trạng thái; chi tiết: thông tin, số phiên đang sống, nút Khoá/Mở/Gỡ khoá tạm/Đặt lại mật khẩu/Thu hồi phiên (mỗi nút `useConfirm`; reset hiện mật khẩu tạm một lần trong `AppDialog` có nút sao chép, đóng là mất); thẻ công tắc "Cho phép đăng ký"; biểu đồ cột đăng ký 30 ngày (SVG tự vẽ, không thêm thư viện) |
| `/he-thong/nguoi-dung-cms` (đổi từ W2 `/nguoi-dung-cms` — W3 giữ redirect), `/he-thong/nhat-ky` | `cms:users.manage` | |

**Thành phần dùng chung trong `apps/admin/src/components/`** (tạo ở feature đầu cần): `MarkdownEditor` (W3: textarea `minRows` lớn + tab Xem trước dùng `react-markdown` + `remark-gfm` — cùng version với website; thanh công cụ chèn **đậm/nghiêng/tiêu đề/link/danh sách/ảnh**), `MediaPicker` (W4: `AppDialog` lưới ảnh + tải lên tại chỗ, trả `MediaDto`; `MediaField` hiển thị ảnh đã chọn), `SlugField` (W5: gợi ý từ tiêu đề, bỏ dấu), `StatusChip`. Không tạo package mới cho chúng (chỉ admin dùng).

#### 5.3.3a W3a / W3b — màn admin (CHI TIẾT LÀM NGAY, chỉ trong `frontend/apps/admin/**`)

Khuôn theo `src/features/cms-users/` (W2): `api.ts` (hàm gọi `cmsApi`, chuẩn hoá phản hồi thiếu trường; GET không `skipErrorRedirect`, ghi giữ lỗi báo tại chỗ bằng `parseApiError`), `types.ts`, `hooks.ts` (TanStack Query, khoá `['cms', '<nhóm>', ...]`), `pages/`, `components/`. Form: `react-hook-form` + `zod` **phản chiếu đúng luật BE §5.2.3**. Mọi màn dùng tốt ở 375px.

**W3a**
- `src/router.tsx`: thêm dưới `AdminLayout`: `website/cau-hinh` → `SiteSettingsPage`, `website/ngon-ngu` → `LanguagesPage` (bọc `RequirePermission permission={CMS_PERMS.SITE_MANAGE}`); chuyển `nguoi-dung-cms` sang `he-thong/nguoi-dung-cms`, route cũ `nguoi-dung-cms` → `<Navigate to="/he-thong/nguoi-dung-cms" replace />`.
- `src/layout/navigation.tsx`: nhóm `website` thêm mục "Cấu hình website" (`/website/cau-hinh`) + "Ngôn ngữ" (`/website/ngon-ngu`), `requiredPermission: CMS_PERMS.SITE_MANAGE`, sửa `planned` bỏ phần đã làm; nhóm `he-thong` đổi `to` sang `/he-thong/nguoi-dung-cms`. `DashboardPage` nếu có link cứng `/nguoi-dung-cms` thì đổi.
- `src/features/site-settings/`: `SiteSettingsPage` — `PageContainer`, các `Card` nhóm: **Thông tin chung** (`site.name`, `site.tagline`), **SEO mặc định** (`seo.default_title` có đếm ký tự /70, `seo.default_description` /160; ô ảnh OG **ẩn** tới W4), **Liên hệ & mạng xã hội** (`contact.email`, 3 URL), **Chân trang** (`footer.text` multiline), **Trang chủ** (`home.hero_mode` radio "Nội dung tĩnh" / "Banner (cần W5)"). `StickyActionBar` nút Lưu (disabled khi không `isDirty`), toast thành công, lỗi 400 map `details` về đúng ô. Chặn rời trang khi chưa lưu: `useBlocker` của react-router (chép ý `useUnsavedChangesGuard` từ `apps/chinese/src/features/admin-content` vào `src/hooks/useUnsavedChangesGuard.ts`).
- `src/features/site-languages/`: `LanguagesPage` — danh sách thẻ `LanguageCard` (tên, `nativeName` đặt `lang` phù hợp — chinese ⇒ `lang="zh-CN"`, `StatusChip` open/coming_soon/hidden với nhãn "Đang mở"/"Sắp ra mắt"/"Ẩn", appUrl), nút lên/xuống `src/components/ReorderButtons.tsx` (chép từ `apps/chinese/src/features/admin-content/components/ReorderButtons.tsx`) gọi `PUT order` với toàn bộ id; nút "Thêm ngôn ngữ"; `LanguageDrawer` (`AppDrawer` của `@af/ui`) form tạo/sửa (`code` chỉ nhập khi tạo, khi sửa hiển thị chỉ đọc kèm chú thích "Mã không đổi được sau khi tạo"), `descriptionMarkdown` dùng `TextField multiline minRows={6}` ở W3a (W3b thay `MarkdownEditor`); `appUrl` bắt buộc khi status `open` (lỗi 422 `APP_URL_REQUIRED` hiện ở ô). 409 `CONCURRENCY_CONFLICT` ⇒ `Alert` trong drawer "Có người vừa sửa ngôn ngữ này" + nút "Tải lại bản mới" (không đóng drawer). Xoá qua `useConfirm` ("Website sẽ không còn hiện ngôn ngữ này").
- `src/components/StatusChip.tsx` (dùng chung sau này).
- Tự test tay: sửa cấu hình ⇒ F5 giữ giá trị; thêm/sửa/xoá/sắp xếp ngôn ngữ; tài khoản `support` (không `site.manage`) không thấy mục Website và gõ URL ⇒ `/403`; `/nguoi-dung-cms` cũ chuyển hướng đúng; 375px không tràn ngang.

**W3b**
- Dependency: `react-markdown` + `remark-gfm` (bản mới nhất ổn định, kiểm peer React 19) vào `frontend/apps/admin/package.json` `dependencies`; chạy `yarn install` trong `frontend/` (đổi `frontend/yarn.lock` — W10 không đụng frontend nên không xung đột). Website (W7) sau này dùng **cùng version**.
- `src/components/MarkdownPreview.tsx`: `<ReactMarkdown remarkPlugins={[remarkGfm]} skipHtml>`; **không** dùng `rehype-raw`; giữ `urlTransform` mặc định (chặn `javascript:`/`data:`); link ngoài render `target="_blank" rel="noopener noreferrer"`; ảnh `max-width: 100%`. `src/components/MarkdownEditor.tsx` (props `value`, `onChange`, `label`, `error`, `helperText`, `minRows=12`): `Tabs` Soạn/Xem trước (state cục bộ — **không** `useTabParam` vì là tab trong form, ghi chú lý do để `lint:ui` WARN được hiểu), thanh công cụ chèn đậm `**…**`, nghiêng `*…*`, tiêu đề `## `, link `[chữ](https://)`, danh sách `- `, ảnh `![alt](url)` quanh vùng chọn của textarea (giữ con trỏ). Test vitest `MarkdownPreview.test.tsx`: `<script>alert(1)</script>` không sinh thẻ `script`; `[x](javascript:alert(1))` không có `href` chứa `javascript:`; bảng GFM render `table`.
- `LanguageDrawer`: đổi ô `descriptionMarkdown` sang `MarkdownEditor`.
- `src/features/site-faqs/`: route `website/faq` (`SITE_MANAGE`), nav "Câu hỏi thường gặp". `FaqsPage`: nhóm theo `groupKey` (tiêu đề nhóm), mỗi dòng: câu hỏi, chip "Đã xuất bản"/"Nháp", `ReorderButtons` trong nhóm, sửa/xoá. `FaqDialog` (`AppDialog maxWidth="md"`, `fullScreen` khi < sm): `question`, `groupKey` (Autocomplete `freeSolo` gợi ý nhóm đã có — trải `params.slotProps` trước khi ghi đè, CLAUDE.md), `answerMarkdown` (`MarkdownEditor`), `isPublished` switch. 409 như ngôn ngữ.
- `src/features/audit-logs/`: route `he-thong/nhat-ky` (`CMS_PERMS.USERS_MANAGE`), nav "Nhật ký thao tác" (bỏ `planned` nhóm Hệ thống). `AuditLogsPage`: lọc `targetType` (Tất cả / `site_settings` / `language` / `faq`) + trang **lên URL** (`useSearchParams`, `replace`); ≥ md bảng (Thời điểm theo giờ trình duyệt `dd/MM/yyyy HH:mm`, Người làm, Hành động, Đối tượng, Tóm tắt, Kết quả); < md danh sách thẻ.

**package.json** (mẫu MedDental, kiểm version mới nhất tương thích trước khi khoá): `next ^15.x`, `react`/`react-dom` **cùng dải `^19.2.6` với các app khác** (một bản React hoisted trong workspace), `react-markdown`, `remark-gfm`; dev `tailwindcss ^4`, `@tailwindcss/postcss ^4`, `@tailwindcss/typography`, `typescript ~6.0.2`, `@types/*`. **Không** dùng `@af/*` (MUI/emotion không cần cho site tĩnh) ⇒ Dockerfile chỉ COPY `package.json` của website. Scripts: `dev: next dev --port 3281`, `build: next build`, `start: next start --port 3281`, `typecheck: tsc --noEmit` ⚠️ **ngoại lệ có chủ đích** với quy tắc `tsc -b`: app Next không có project references, tsconfig riêng không `files: []`; cổng kiểm là `yarn workspace @af/website build` (next build tự type-check). Kiểm `turbo.json` không vỡ khi app không có `typecheck -b`.

**Cấu trúc:**
```
apps/website/
  next.config.ts        output: 'standalone'; outputFileTracingRoot = frontend/ (monorepo); rewrites dev '/cms/:path*' → CMS_PUBLIC_PROXY (http://localhost:5280/cms/:path*) chỉ khi NODE_ENV=development; images.unoptimized = true
  postcss.config.mjs    @tailwindcss/postcss
  src/app/layout.tsx    <html lang="vi">, font hệ thống + fallback CJK, Header/Footer từ getSite()
  src/app/page.tsx      trang chủ: hero (banners hoặc tĩnh), khối lộ trình học (§1.3, nội dung cứng trong code + tagline từ CMS), thẻ ngôn ngữ, bài viết mới (W8), FAQ nổi bật, CTA
  src/app/cau-hoi-thuong-gap/page.tsx     (W7) + JSON-LD FAQPage
  src/app/lien-he/page.tsx                 (W9)
  src/app/bai-viet/page.tsx, bai-viet/trang/[n]/page.tsx, bai-viet/danh-muc/[slug]/page.tsx, bai-viet/[slug]/page.tsx  (W8) + JSON-LD Article
  src/app/[slug]/page.tsx                  trang tĩnh (W8)
  src/app/sitemap.ts, robots.ts            (W8)
  src/app/not-found.tsx, error.tsx         nút "Về trang chủ"
  src/app/api/revalidate/route.ts          POST, so khớp X-Revalidate-Secret (timingSafeEqual), revalidateTag cho từng tag, 401 khi sai, 400 khi body sai
  src/app/api/health/route.ts              200 { ok: true } (HEALTHCHECK)
  src/lib/cms.ts        cmsFetch<T>(path, tags): fetch(`${CMS_INTERNAL_URL}/api/public${path}`, { next: { tags, revalidate: 3600 } }); lỗi ⇒ trả null + console.error, trang render trạng thái rỗng thân thiện (build KHÔNG được vỡ khi CMS tắt — dùng dynamicParams + generateStaticParams trả [] khi lỗi)
  src/lib/seo.ts        buildMetadata(page) dùng seo.default_* làm dự phòng; NEXT_PUBLIC_SITE_URL cho canonical/OG
  src/components/{Header,Footer,Markdown,LanguageCard,PostCard,Hero,Faq,ContactForm,NewsletterForm}.tsx
```
Biến: `CMS_INTERNAL_URL` (runtime server, **không** `NEXT_PUBLIC_`), `REVALIDATE_SECRET` (runtime), `NEXT_PUBLIC_SITE_URL` (build arg: dev `http://localhost:3281`, prod `https://antfarms.xyz`). ⚠️ `generateStaticParams` chạy lúc `next build` trong Docker **không** có cms-backend ⇒ phải trả `[]` êm (trang sinh lúc request đầu, ISR giữ) — ghi vào Dockerfile comment.
Giao diện: mobile-first 375px, menu mobile dạng ngăn kéo thuần CSS/React (không thư viện), Tailwind `prose` cho Markdown, màu nhấn nền tảng; chữ Hán (nếu có trong bài) bọc `lang="zh-CN"`.

#### 5.3.5 W12–W14 — gom quản trị tiếng Trung (thiết kế)

- **W12** tạo `frontend/packages/chinese-kit` (`@af/chinese-kit`, import thẳng TS source, peerDependencies giống `@af/ui` + phụ thuộc `@af/ui`): **dời** (không chép) từ `apps/chinese/src`: `lib/pinyin*` (kèm test vitest — package có script `test`), `components/Hanzi`, `components/speech/{ChineseSpeech,SpeakButton}`, `features/lessons/components/{LessonContent,InlineZh,LessonDisplayContext,quiz/QuestionParts}` + các block con chúng dùng, `features/lessons/types`, `features/dictionary/{types,lib/sources,components/MeaningStatusChip}`. `apps/chinese` đổi import sang `@af/chinese-kit`; hành vi **không đổi** (vitest, `tsc -b`, `build`, nghiệm thu nhanh các màn học). Dockerfile `apps/chinese` thêm dòng COPY `packages/chinese-kit/package.json`. Cân nhắc giữ re-export tạm trong `apps/chinese/src/lib/pinyin.ts` để diff nhỏ — **BA-mặc định: không giữ**, đổi import thẳng.
- **W13** `apps/admin/src/modules/chinese/` chép `features/admin-content` + `features/admin-users` của chinese, đổi `chineseApi` → `languageApis.chinese`, quyền `chinese:*`, route `/ngon-ngu/chinese/{bai-hoc,bai-hoc/:id,tu-vung,nguoi-dung}`; phụ thuộc nhỏ (`searchParams`, `useDebouncedValue`, `QueryErrorAlert`) chép vào `apps/admin/src/lib`. Hook `useWordSearch`/`LESSON_KEYS`/`DICTIONARY_KEYS` (query key học viên dùng để invalidate) — trong admin chỉ cần key của chính module; bỏ invalidation `PROGRESS_OVERVIEW_KEY` (khác app, không cùng cache). `package.json` admin thêm `@af/chinese-kit`; Dockerfile admin thêm COPY. Font CJK + `lang` giữ nguyên. Tiêu chí: mọi ca nghiệm thu F10 + F4 (tạo/sửa/xuất bản bài, duyệt nghĩa hàng loạt, gán vai trò, `LAST_ADMIN`, 409 concurrency) chạy trên admin.
- **W14** xoá `features/admin`, `admin-content`, `admin-users` khỏi `apps/chinese`; route `/quan-tri/*` → `AdminMovedPage` (R-W35); nav "Quản trị" thành link ngoài `VITE_ADMIN_URL` (Dockerfile chinese thêm `ARG VITE_ADMIN_URL=https://admin.antfarms.xyz`, compose truyền `https://admin.${APP_DOMAIN}`); dọn test/hằng không còn dùng; `grep -rn "admin-content\|admin-users" frontend/apps/chinese` rỗng.

### 5.4 Học liệu / nội dung

#### 5.4.1 Nguồn & giấy phép

- Không dùng học liệu bên thứ ba. Nội dung website (giới thiệu, FAQ, bài viết) do **người dùng soạn** trong CMS; seed chỉ là **dàn ý nháp** đánh dấu `[Cần người dùng soạn]`, trạng thái `draft` (trang, bài) hoặc `is_published=false` (FAQ) — tránh xuất bản chữ do máy viết.
- Ngoại lệ: seed ngôn ngữ (tên, tên bản địa 中文/English/日本語) và 3 câu FAQ vận hành có thật (Học phí? — **để trống chờ người dùng**; Cần tài khoản không? Có; Học trên điện thoại được không? Có, tối ưu 375px) — FAQ seed vẫn `is_published=false`.
- Ảnh: người dùng tự tải; không kéo ảnh stock. Nếu cần biểu tượng ngôn ngữ, dùng chữ bản địa trong thẻ (không cần ảnh cờ — cờ quốc gia ≠ ngôn ngữ).
- `content/` **không đổi** đợt này.

#### 5.4.2 Định dạng

Markdown CommonMark + GFM (bảng, danh sách việc, gạch ngang). Không HTML thô. Link ngoài website tự thêm `rel="noopener noreferrer"` + `target="_blank"`.

#### 5.4.3 Bảng tag revalidate (cms-backend phát ⇒ website nghe)

| Thao tác | Tag |
|---|---|
| settings | `site` |
| languages | `site`, `languages` |
| faqs | `site`, `faqs` |
| pages | `pages`, `page:<slug>` (cũ + mới khi đổi slug), `sitemap` |
| banners | `banners` |
| posts | `posts`, `post:<slug>` (cũ + mới), `sitemap` |
| categories | `posts`, `categories` |
| media (sửa alt/xoá) | không (URL ảnh bất biến) |

### 5.5 Checklist "thêm ngôn ngữ mới" — bổ sung vào §5.5 hợp đồng gốc

Thêm các mục (W15 chép vào `CLAUDE.md` mục tóm tắt):
17. [ ] CMS: thêm/đổi bản ghi `site.languages` (status `open`, `app_url`) trên admin — website tự cập nhật qua revalidate.
18. [ ] Admin: `src/modules/<code>/` + mục registry + proxy Vite `/<code>` + `location ^~ /<code>/` trong khối `admin.` nginx.
19. [ ] identity `Jwt:Audiences` đã gồm `af-<code>` (mục 5 cũ) — admin dùng chung token.

### 5.6 Triển khai

#### 5.6.1 Topology sau đợt

```
Internet ─► nginx biên (TLS SAN)
  ├── antfarms.xyz          / ─► website:3000 (Next standalone)   · ^~ /cms/api/public/ ─► gateway ─► cms-backend   · /cms/* khác ⇒ 404
  ├── www.antfarms.xyz      301 ─► https://antfarms.xyz$request_uri
  ├── admin.antfarms.xyz    / ─► admin-frontend:80   · ^~ /cms/ ─► gateway ─► cms-backend   · ^~ /chinese/ ─► gateway ─► chinese-backend
  ├── id.antfarms.xyz       (không đổi) ─► gateway /identity ─► identity-service:8080   (8081 KHÔNG có đường nào tới)
  └── chinese.antfarms.xyz  (không đổi)
Nội bộ af-net: website ─► gateway:8080/cms (ISR) · cms-backend ─► website:3000/api/revalidate · cms-backend ─► identity-service:8081/internal (X-Service-Key)
               cms-backend ─► minio:9000 · cms-backend ─► postgres (af_cms)
```

#### 5.6.2 Biến môi trường mới

| Khoá | Dev | Docker/prod |
|---|---|---|
| cms `ConnectionStrings:Default` | `...Database=af_cms;Username=af_cms;Password=af_cms_dev` | `.env AF_CMS_DB_PASSWORD` |
| cms `CmsAdmin:BootstrapEmails` | appsettings.Development.json | `CMS_BOOTSTRAP_ADMIN_EMAIL` |
| cms `Storage:Endpoint/AccessKey/SecretKey/Bucket` | `localhost:9000`/`minioadmin`/`minioadmin`/`af-cms` | `minio:9000`/`MINIO_ROOT_USER`/`MINIO_ROOT_PASSWORD`(**BA-mặc định**: dùng root; nợ N-W4 tạo user riêng)/`af-cms` |
| cms `Revalidation:WebhookUrl` / `Secret` | `http://localhost:3281/api/revalidate` / `dev-revalidate-secret` | `http://website:3000/api/revalidate` / `REVALIDATE_SECRET` |
| cms `IdentityInternal:BaseUrl` / `ServiceKey` | `http://localhost:5291` / chuỗi dev | `http://identity-service:8081` / `IDENTITY_INTERNAL_KEY` |
| cms `CmsInbox:IpSalt` | chuỗi dev | `CMS_INBOX_IP_SALT` |
| identity `Internal:Port` / `ServiceKey` | `5291` / chuỗi dev (khớp cms) | `8081` / `IDENTITY_INTERNAL_KEY` |
| identity `Auth:AllowedOrigins` | + `http://localhost:3290` | + `https://admin.${APP_DOMAIN}` |
| identity `Jwt:Audiences` | + `af-cms` | như dev (appsettings) |
| gateway cluster `cms` | `http://localhost:5290` | `http://cms-backend:8080` |
| website `CMS_INTERNAL_URL` / `REVALIDATE_SECRET` / `NEXT_PUBLIC_SITE_URL` | `http://localhost:5280/cms` / khớp cms / `http://localhost:3281` | `http://gateway:8080/cms` / `.env` / build arg `https://${APP_DOMAIN}` |
| admin `VITE_IDENTITY_API_URL` | `/identity/api` | build arg `https://id.${APP_DOMAIN}/api` |
| chinese `VITE_ADMIN_URL` (W14) | `http://localhost:3290` | build arg `https://admin.${APP_DOMAIN}` |

Sinh secret: `openssl rand -base64 48` — ghi lệnh trong `.env.example`.

#### 5.6.3 Compose/nginx/chứng chỉ (gom ở W15, từng phần đã thêm ở feature tạo ra nó)

- Service mới: `cms-backend` (W1), `admin-frontend` (W2), `minio` + `minio-init` production (W4 — volume `minio-data`, **không** ports, healthcheck `mc ready local`, image ghim đúng tag dev compose đang dùng), `website` (W7 — `node` standalone, `HEALTHCHECK wget /api/health`).
- `nginx.conf.example`: khối `admin.` (W2), khối `antfarms.xyz` + `www.` (W7) thay mẫu portal comment; khối `antfarms.xyz` có `location = /cms/api/public/contact` và `/newsletter` với `limit_req` nhẹ (**BA-mặc định**: `limit_req_zone $binary_remote_addr zone=forms:10m rate=10r/m` trong `http{}` — nginx.conf.example có `http{}` hay chỉ `server{}` thì agent kiểm; không có `http{}` thì bỏ, backend đã rate limit).
- `get-cert.sh` ví dụ đầu file cập nhật: `./scripts/get-cert.sh admin@antfarms.xyz antfarms.xyz www.antfarms.xyz id.antfarms.xyz chinese.antfarms.xyz admin.antfarms.xyz` + `deploy/README.md` bước DNS Cloudflare cho `@`, `www`, `admin`.
- HSTS giữ **không** `includeSubDomains` (RK27 gốc) — apex bật HSTS không ảnh hưởng subdomain khi không có cờ đó.
- `deploy/scripts/backup-db.sh`: thêm `af_cms` + ghi chú sao lưu volume `minio-data` (ảnh không dựng lại được, khác chỉ mục).
- `deploy/preflight.sh` (nếu kiểm biến bắt buộc): thêm `AF_CMS_DB_PASSWORD`, `IDENTITY_INTERNAL_KEY`, `REVALIDATE_SECRET`, `MINIO_ROOT_*`, `CMS_INBOX_IP_SALT`, `CMS_BOOTSTRAP_ADMIN_EMAIL`.
- `VERIFY-DOCKER.md`: mục mới — (a) `curl -I https://id.antfarms.xyz/internal/accounts` ⇒ **404**; (b) `docker exec cms-backend wget -qO- http://identity-service:8081/internal/ping` có key ⇒ 200; (c) `https://antfarms.xyz/cms/api/admin/ping` ⇒ 404 (nginx không mở); (d) sửa FAQ ở admin ⇒ website đổi trong vài giây; (e) `docker compose logs cms-backend | grep -i revalidat` không có Warning; (f) ảnh tải lên hiện trên website.

### 5.7 Cập nhật `CLAUDE.md` (chỉ kiến trúc + một quy tắc)

| Feature | Sửa |
|---|---|
| W1 | Sơ đồ kiến trúc thêm `cms-backend ─► af_cms`; mục Backend thêm dòng `services/cms-backend/`; bảng cổng thêm cms-backend 5290; gateway routes thêm `/cms/**`; câu "mỗi service một DATABASE" thêm `af_cms`; mục lục hợp đồng thêm file này |
| W2 | Bảng domain: `admin.antfarms.xyz`; bảng cổng: apps/admin 3290; mục Frontend thêm `apps/admin`; `Auth:AllowedOrigins` nhắc admin |
| W7 | Bảng domain: dòng `antfarms.xyz` đổi từ "portal — F13" thành website Next.js + `www` 301; bảng cổng: dòng `apps/portal 3281` → `apps/website 3281`; ghi ngoại lệ `tsc -b` cho app Next (dùng `build`) |
| W10 | **Quy tắc mới** (mục Quy tắc bắt buộc): "API nội bộ identity-service `/internal/*` chỉ trên cổng nội bộ (8081/5291), kiểm `Connection.LocalPort` + `X-Service-Key`; KHÔNG thêm cluster gateway, location nginx hay `ports:` nào tới cổng này; không kiểm bằng header `Host`. Trình duyệt không bao giờ gọi — đi qua cms-backend (`accounts.manage`)". Bảng cổng: identity nội bộ 5291/8081 |
| W12 | Mục Frontend thêm `packages/chinese-kit` |
| W14 | Mô tả `apps/chinese`: "học viên (quản trị đã chuyển sang apps/admin)" |
| W15 | Checklist thêm ngôn ngữ (tóm tắt) thêm mục CMS/admin (§5.5); rà toàn bộ các dòng trên khớp code |

---

## 6. Hợp đồng API

Đường dẫn ghi theo **service**; trình duyệt gọi qua tiền tố gateway `/cms`, `/chinese`. Lỗi theo §6.0 hợp đồng gốc + mã mới: `SLUG_TAKEN` (409), `SLUG_RESERVED` (422), `MEDIA_IN_USE` (409, `details.usages: [{type, id, title}]`), `MEDIA_TYPE_NOT_ALLOWED` (422), `MEDIA_TOO_LARGE` (413), `STORAGE_UNAVAILABLE` (503), `IDENTITY_UNAVAILABLE` (503), `SELF_ACTION_FORBIDDEN` (422), `SERVICE_KEY_INVALID` (401), `PAGE_PUBLISHED` (422).

### 6.1 W1 — cms-backend truy cập

```
GET  /api/me                       [Authorize]
  200 { "id": "0192…", "email": "quan@vidu.com", "displayName": "Quân", "timeZone": "Asia/Ho_Chi_Minh",
        "roles": ["admin"], "permissions": ["accounts.manage","inbox.manage","media.manage","posts.manage","site.manage","users.manage"],
        "firstSeenAt": "2026-09-17T14:00:00Z" }
GET  /api/admin/ping               users.manage   200 { "ok": true } · 403 { "error": "...", "code": "FORBIDDEN" }
GET  /api/admin/users?q=&page=1&pageSize=20       users.manage
  200 { "items": [ { "id", "email", "displayName", "roles": ["editor"], "firstSeenAt", "lastSeenAt" } ], "page": 1, "pageSize": 20, "totalCount": 3 }
GET  /api/admin/users/{id}         users.manage   200 item · 404
PUT  /api/admin/users/{id}/roles   users.manage   { "roles": ["editor","support"] } → 200 item · 422 LAST_ADMIN | UNKNOWN_ROLE · 404
GET  /api/admin/roles              users.manage
  200 [ { "code": "admin", "name": "Quản trị viên", "permissions": ["accounts.manage", "..."] },
        { "code": "editor", "name": "Biên tập website", "permissions": ["media.manage","posts.manage","site.manage"] },
        { "code": "support", "name": "Hỗ trợ người học", "permissions": ["accounts.manage","inbox.manage"] } ]
GET  /api/system/info · /health/live · /health/ready     ẩn danh
```
Sắp xếp danh sách user: `last_seen_at DESC`. `q` khớp `lower(email)` hoặc `display_name` ILIKE (escape `%`/`_`).

### 6.2 W3 — site nền

```
── W3a ──
GET  /api/admin/site-settings      site.manage   200 { "values": { "site.name": "AntFarm", "seo.default_title": "...", ... }, "updatedAt": "..." }
PUT  /api/admin/site-settings      site.manage   { "values": { ...đủ mọi khoá... } } → 200 như GET · 400 VALIDATION (khoá lạ/độ dài)
GET  /api/admin/languages          site.manage   200 [ LanguageDto ]
POST /api/admin/languages          site.manage   LanguageInput + "code" → 201 LanguageDto · 409 CODE_TAKEN · 422 APP_URL_REQUIRED · 400 VALIDATION
  LanguageInput { name, nativeName, tagline, descriptionMarkdown, status, appUrl|null, accentColor|null }   (W3a chưa có coverMediaId — W4 thêm)
PUT  /api/admin/languages/{id}     site.manage   LanguageInput (KHÔNG có code) + "version" → 200 · 409 CONCURRENCY_CONFLICT · 422 APP_URL_REQUIRED · 404
DELETE /api/admin/languages/{id}   site.manage   204 · 404
PUT  /api/admin/languages/order    site.manage   { "ids": ["…","…"] } (đủ mọi id) → 204 · 422 ORDER_MISMATCH
  LanguageDto { id, code, name, nativeName, tagline, descriptionMarkdown, status: "open|coming_soon|hidden", appUrl, accentColor, coverMedia: MediaDto|null, sortOrder, version, updatedAt }
  (version = xmin dạng chuỗi số; PUT gửi lại nguyên bản ghi kèm version)
── W3b ──
GET/POST/PUT/DELETE /api/admin/faqs[/{id}]    site.manage   FaqInput { question, answerMarkdown, groupKey, isPublished } (+ "version" khi PUT) · 409 CONCURRENCY_CONFLICT · 404
PUT  /api/admin/faqs/order         site.manage   { "groupKey": "general", "ids": [...] } (đủ id của nhóm) → 204 · 422 ORDER_MISMATCH
  FaqDto { id, question, answerMarkdown, groupKey, sortOrder, isPublished, version, updatedAt }
GET  /api/admin/audit-logs?targetType=&targetId=&page=&pageSize=50   users.manage   → { items: [ { id, at, actorId, actorEmail, action, targetType, targetId, summary, success } ], page, pageSize, totalCount }   (W3b; bảng tạo ở W3a)
── W3a (W3a trả "faqs": [] — W3b điền; "ogImageUrl"/"coverUrl" luôn null tới W4) ──
GET  /api/public/site              ẩn danh
  200 { "settings": { "site.name": "...", ... (bỏ khoá rỗng) }, "ogImageUrl": "/cms/api/public/media/…/og.png" | null,
        "languages": [ { "code": "chinese", "name": "Tiếng Trung", "nativeName": "中文", "tagline", "descriptionMarkdown", "status": "open", "appUrl": "https://chinese.antfarms.xyz", "accentColor", "coverUrl" } ],
        "faqs": [ { "id", "question", "answerMarkdown", "groupKey" } ] }
```

### 6.3 W4 — media

```
POST   /api/admin/media  multipart(file, altText)  media.manage → 201 MediaDto · 413 MEDIA_TOO_LARGE · 422 MEDIA_TYPE_NOT_ALLOWED · 503 STORAGE_UNAVAILABLE
GET    /api/admin/media?q=&page=&pageSize=24       → phân trang MediaDto
PUT    /api/admin/media/{id}   { "altText": "..." } → 200
DELETE /api/admin/media/{id}   → 204 · 409 MEDIA_IN_USE
GET    /api/public/media/{id}/{fileName}           ẩn danh → 200 stream · 404
  MediaDto { id, fileName, contentType, sizeBytes, width, height, altText, url: "/cms/api/public/media/{id}/{fileName}", createdAt }
```
`url` luôn **tương đối có tiền tố `/cms`** (đúng cho admin và trình duyệt ở website). Website phía server cần URL tuyệt đối cho OG ⇒ ghép `NEXT_PUBLIC_SITE_URL`.

### 6.4 W5/W6 — trang, banner, bài viết (tóm tắt)

```
Admin (site.manage): /api/admin/pages [GET list ?status&q&page | POST] · /api/admin/pages/{id} [GET | PUT đủ bản ghi + version | DELETE]
                     /api/admin/pages/{id}/publish · /unpublish [POST]
                     /api/admin/banners [GET|POST] · /{id} [PUT|DELETE] · /order [PUT]
Admin (posts.manage): /api/admin/post-categories [GET|POST] · /{id} [PUT|DELETE — 409 CATEGORY_IN_USE nếu còn bài]
                      /api/admin/posts [GET ?status&categoryId&q&page | POST] · /{id} [GET|PUT|DELETE] · /{id}/publish [POST { "publishedAt"?: ISO }] · /unpublish
Public: GET /api/public/pages/{slug} → { slug, title, bodyMarkdown, seoTitle, seoDescription, publishedAt, updatedAt } · 404
        GET /api/public/pages?footer=true → [ { slug, title } ]
        GET /api/public/banners → [ { id, title, subtitle, ctaLabel, ctaUrl, imageUrl, mobileImageUrl, alt } ]
        GET /api/public/posts?category=&page=1&pageSize=12 → { items: [ { slug, title, excerpt, coverUrl, category: {slug,name}|null, publishedAt, readingMinutes } ], page, pageSize, totalCount }
        GET /api/public/posts/{slug} → { ...item, bodyMarkdown, authorName, seoTitle, seoDescription, updatedAt, related: [item×≤3] } · 404
        GET /api/public/post-categories → [ { slug, name, description, postCount } ]
        GET /api/public/sitemap → { pages: [ { slug, updatedAt } ], posts: [ { slug, updatedAt } ], categories: [ { slug } ] }
```
PageInput `{ slug, title, bodyMarkdown, seoTitle, seoDescription, showInFooter, version? }`; PostInput `{ slug, title, excerpt, bodyMarkdown, coverMediaId, categoryId, authorName, seoTitle, seoDescription, version? }`.

### 6.5 W10 — identity API nội bộ (cổng 8081/5291, header `X-Service-Key`; ghi cần `X-Actor-Id`, `X-Actor-Email`)

```
GET  /internal/ping → 200 { "ok": true }
GET  /internal/accounts?q=&status=active|disabled|locked&page=1&pageSize=20
  200 { "items": [ { "id", "email", "displayName", "timeZone", "isActive": true, "lockedUntil": null, "lastLoginAt", "createdAt" } ], "page", "pageSize", "totalCount" }
GET  /internal/accounts/{id} → 200 { ...item, "passwordChangedAt", "activeSessionCount": 2 } · 404
POST /internal/accounts/{id}/disable        → 200 item · 422 SELF_ACTION_FORBIDDEN · 404
POST /internal/accounts/{id}/enable         → 200 item
POST /internal/accounts/{id}/clear-lockout  → 200 item
POST /internal/accounts/{id}/reset-password { "newPassword"?: "..." }
  200 { "temporaryPassword": "k7PqXw3mZr9TbN2h" (16 ký tự, không gạch nối) | null (null khi admin tự nhập), "revokedSessions": 2 }  + Cache-Control: no-store · 400 VALIDATION · 422 SELF_ACTION_FORBIDDEN
POST /internal/accounts/{id}/revoke-sessions → 200 { "revokedSessions": 3 } · 422 SELF_ACTION_FORBIDDEN
GET  /internal/stats/registrations?from=2026-08-19&to=2026-09-17
  200 { "timeZone": "Asia/Ho_Chi_Minh", "days": [ { "date": "2026-09-17", "count": 4 } ], "totalAccounts": 57, "disabledAccounts": 1, "activeLast7Days": 12, "activeLast30Days": 30 }
  · 400 VALIDATION (from>to, >366 ngày)
GET  /internal/settings/registration → 200 { "enabled": false, "source": "database" | "configuration", "updatedAt": null }
PUT  /internal/settings/registration { "enabled": true } → 200 như GET
```
`days` đủ mọi ngày trong khoảng (ngày 0 đăng ký ⇒ `count: 0`).

### 6.6 W11 — cms-backend tài khoản (`accounts.manage`)

Gương 1-1 của §6.5 dưới `/api/admin/accounts/*`, `/api/admin/account-stats/registrations`, `/api/admin/platform-settings/registration`; cms tự gắn `X-Actor-*`; chuyển tiếp mã lỗi; 503 `IDENTITY_UNAVAILABLE`.

### 6.7 W9 — hộp thư

```
POST /api/public/contact     { "name", "email", "phone"?, "message", "sourcePath", "website": "" (honeypot), "renderedAt": ISO }
  202 { "ok": true } (kể cả khi coi là spam) · 400 VALIDATION · 429 RATE_LIMITED
POST /api/public/newsletter  { "email", "interest": "general|chinese|english|…", "sourcePath", "website": "", "renderedAt" } → 202 · 400 · 429
GET  /api/admin/contacts?status=new&from=2026-09-01&to=2026-09-17&q=&page=   inbox.manage → phân trang { id, name, email, phone, messagePreview(200), sourcePath, status, createdAt, handledAt, handledByEmail }
GET  /api/admin/contacts/{id} → đủ message + handlingNote
PUT  /api/admin/contacts/{id}/status { "status": "handled|new|spam", "note"?: "..." } → 200
GET  /api/admin/contacts/export.csv?status&from&to → text/csv; charset=utf-8 (BOM), Content-Disposition: attachment; filename="lien-he-2026-09-17.csv"
GET  /api/admin/newsletter?status&interest&from&to&q&page · PUT /api/admin/newsletter/{id}/unsubscribe · GET /api/admin/newsletter/export.csv
```

### 6.8 W7 — website

```
POST /api/revalidate  (Next route)  header X-Revalidate-Secret; body { "tags": ["site","faqs"] } → 200 { "revalidated": ["site","faqs"] } · 401 · 400
GET  /api/health → 200 { "ok": true }
```

---

## 7. Phân rã feature

> Mỗi feature: commit local riêng (có thể 2 commit BE/FE như MVP nếu review muốn), cổng §5.0.3, cập nhật Dockerfile/compose/nginx/`VERIFY-DOCKER.md` phần của mình (ghi "chưa verify"), cập nhật `CLAUDE.md` theo §5.7.

### Feature W1: cms-backend khung + phân quyền cục bộ + `/api/me` + audience `af-cms` + route gateway
- Mục tiêu: có service CMS chạy, nhận token nền tảng, phân quyền fail-closed trên `af_cms`, bootstrap admin.
- Phạm vi BE: §5.2.1 toàn bộ (6 project, Program, options, provisioning, resolver, seeder, Me/Users/Roles/Ping controllers, tests); identity `Jwt:Audiences` + test; gateway route/cluster `cms`.
- DB: `af_cms` schema `access`, migration `W1_Access`; init script + `.env.example` dev/prod.
- FE / học liệu: không.
- Deploy: Dockerfile cms, mục compose `cms-backend`, cluster gateway compose, `VERIFY-DOCKER.md`, README dev.
- Phụ thuộc: không.
- Tiêu chí + tự test: 9 nhóm test §5.2.1 xanh với `AF_TEST_PG` (báo số chạy/skip); `dotnet build` 0 error; thao tác tay `GET localhost:5280/cms/api/me` với token thật ⇒ vai trò đúng; `grep -rn "new string Policy" backend` rỗng; `CLAUDE.md` §5.7 dòng W1.

### Feature W2: apps/admin khung + đăng nhập + gộp quyền đa service + màn Người dùng CMS
- Mục tiêu: `admin` chạy ở 3290, đăng nhập bằng tài khoản nền tảng, tài khoản 0 quyền ⇒ 403, admin gán vai trò CMS được.
- Phạm vi FE: §5.3.1 toàn bộ. BE: chỉ cấu hình `AllowedOrigins` (§5.2.2). Deploy: Dockerfile + nginx.conf admin, compose `admin-frontend`, khối nginx `admin.`, `AllowedOrigins` compose.
- Phụ thuộc: W1.
- Tiêu chí + tự test: 6 ca tay §5.3.1 ở 1366px và 375px; `tsc -b`/`build`/`lint:ui` sạch; `CLAUDE.md` dòng W2.

### Feature W3a: CMS nền — cấu hình site/SEO + ngôn ngữ + `GET /api/public/site` (BE + FE)
- Mục tiêu: biên tập được thông tin chung/SEO và danh mục ngôn ngữ; website (W7) có một API công khai để dựng layout; hạ tầng nhật ký thao tác + revalidate no-op sẵn cho mọi feature ghi sau.
- Phạm vi BE: §5.2.3 "W3a — Backend"; DB: §5.1.2 migration `W3a_SiteBasics` (`site.settings`, `site.languages`, `site.audit_logs`); API §6.2 phần W3a. FE: §5.3.3a "W3a" (2 màn + đổi route Người dùng CMS sang `/he-thong/...`). Học liệu: không. Chưa có ảnh (ô OG/ảnh bìa ẩn tới W4).
- **Ranh giới file:** BE chỉ `backend/services/cms-backend/**`; FE chỉ `frontend/apps/admin/**`. Không sửa `shared/`, `frontend/packages/**`, `deploy/**`, `CLAUDE.md` (W3a không đổi cổng/biến môi trường; `CmsSeed:Languages` tuỳ chọn, chỉ ghi trong `appsettings.Development.json.example` của cms).
- Phụ thuộc: W2. **Song song được với W10** (khác service, khác thư mục). BE ‖ FE song song (§6.2 đã chốt).
- Tiêu chí + tự test: 6 nhóm test W3a §5.2.3 xanh với `AF_TEST_PG` (báo số chạy/skip); `dotnet build` 0 error; `yarn workspace @af/admin tsc -b`, `yarn lint:ui` sạch; tay theo §5.3.3a W3a ở 1366px + 375px; `curl http://localhost:5280/cms/api/public/site` không token ⇒ 200 JSON.

### Feature W3b: CMS nền — FAQ + nhật ký thao tác + trình soạn Markdown (BE + FE)
- Mục tiêu: soạn FAQ bằng Markdown có xem trước an toàn XSS (D-W1); quản trị viên xem được ai đã sửa gì.
- Phạm vi BE: §5.2.3 "W3b — Backend"; DB: migration `W3b_Faqs`; API §6.2 phần W3b. FE: §5.3.3a "W3b" (`MarkdownEditor`/`MarkdownPreview` + dependency `react-markdown`/`remark-gfm`, màn FAQ, màn nhật ký, ô mô tả ngôn ngữ dùng `MarkdownEditor`).
- **Ranh giới file:** như W3a + `frontend/yarn.lock`.
- Phụ thuộc: W3a (bảng `audit_logs`, `IAuditLogger`, `PublicSiteService`, route/nav đã đổi).
- Tiêu chí + tự test: test W3b §5.2.3 xanh; vitest `MarkdownPreview.test.tsx` xanh (`yarn workspace @af/admin test`); `yarn workspace @af/admin build` sạch (đổi dependency); tay: soạn FAQ có bảng/danh sách, xem trước đúng, dán `<script>` không chạy; bỏ xuất bản ⇒ `/cms/api/public/site` không còn FAQ đó; tài khoản `editor` vào `/he-thong/nhat-ky` ⇒ `/403`.

### Feature W4: Thư viện ảnh MinIO + MediaPicker
- Mục tiêu: tải/quản lý ảnh, gắn ảnh OG + ảnh bìa ngôn ngữ, phục vụ ảnh công khai.
- BE §5.2.4, DB §5.1.3 (`W4_Media`), API §6.3; FE màn `/website/anh`, `MediaPicker`/`MediaField`, bật lại ô ảnh ở W3. Dev compose bucket `af-cms`; compose production `minio` + `minio-init` + volume; `client_max_body_size` nginx `admin.`.
- Phụ thuộc: W3.
- Tiêu chí: unit test đọc kích thước PNG/JPEG/WebP/GIF + từ chối SVG/đuôi giả (magic bytes); ApiTests upload (MinIO **giả** bằng `IMediaStorage` in-memory trong test — không cần MinIO chạy) + `MEDIA_IN_USE`; tay với MinIO dev thật: tải ảnh 4 MB, gắn OG, mở URL public ⇒ ảnh + header cache. *Bổ sung khi tới lượt.*

### Feature W5: Trang tĩnh + banner/hero
- BE §5.2.5, DB §5.1.4 (`W5_PagesBanners`), API §6.4 phần pages/banners; FE 2 màn (`SlugField`, chặn rời trang chưa lưu).
- Phụ thuộc: W4.
- Tiêu chí: `SLUG_RESERVED`, `SLUG_TAKEN`, public chỉ published, banner theo khoảng thời gian (TimeProvider giả), xoá trang đã xuất bản ⇒ 422. *Bổ sung khi tới lượt.*

### Feature W6: Bài viết + danh mục
- BE §5.2.6, DB §5.1.5 (`W6_Posts`), API §6.4 phần posts + sitemap; FE 3 màn.
- Phụ thuộc: W5 (dùng lại `SlugField`, `MarkdownEditor`, `MediaField`).
- Tiêu chí: xuất bản ngày tương lai ẩn khỏi public tới giờ đó; `reading_minutes`; `CATEGORY_IN_USE`; phân trang public không kèm body. *Bổ sung khi tới lượt.*

### Feature W7: Website Next.js khung + trang chủ + FAQ + revalidate thật
- Mục tiêu: `antfarms.xyz` hiển thị trang chủ/FAQ từ CMS, sửa ở admin ⇒ website đổi trong vài giây.
- FE (Fable) §5.3.4 phần khung + `/`, `/cau-hoi-thuong-gap`, not-found/error, `/api/revalidate`, `/api/health`, rewrites dev; nội dung khối "lộ trình học" §1.3. BE §5.2.7 thay no-op bằng `HttpRevalidationNotifier` (Channel + BackgroundService) + test (webhook lỗi không làm hỏng lưu).
- Deploy: Dockerfile website (standalone, build arg `NEXT_PUBLIC_SITE_URL`, runtime env), compose `website`, khối nginx `antfarms.xyz` + `www.` (thay mẫu portal), `get-cert.sh` ví dụ, `AllowedOrigins` **không** thêm apex; `CLAUDE.md` dòng W7.
- Phụ thuộc: W4 (hero cần ảnh; W5 banner nếu có — trang chủ dùng banners khi `home.hero_mode=banners`, W7 làm trước W5 được bằng chế độ `static` ⇒ **W7 chỉ phụ thuộc W4**; nếu W5 đã xong thì hiển thị banner).
- Tiêu chí: `yarn workspace @af/website build` sạch **khi cms-backend tắt** (không vỡ); dev: sửa tagline ⇒ F5 website thấy trong ≤ 5 giây; secret sai ⇒ 401; Lighthouse mobile SEO ≥ 90 (tham khảo, không chặn); 375px. *Bổ sung khi tới lượt.*

### Feature W8: Website — blog, trang tĩnh, SEO (sitemap/robots/JSON-LD/metadata)
- FE §5.3.4 các route bài viết + `[slug]`; `sitemap.ts` từ `/api/public/sitemap`; `robots.ts` (chặn `/api/`); JSON-LD `Organization` (layout), `FAQPage`, `Article`, `BreadcrumbList`; canonical; OG.
- Phụ thuộc: W6, W7.
- Tiêu chí: xuất bản bài ở admin ⇒ xuất hiện ở `/bai-viet` + sitemap sau revalidate; slug lạ ⇒ trang 404 thân thiện; validator JSON-LD (Rich Results test thủ công) không lỗi. *Bổ sung khi tới lượt.*

### Feature W9: Liên hệ + đăng ký nhận tin (form website + hộp thư admin + CSV)
- BE §5.2.8, DB §5.1.6 (`W9_Inbox`), API §6.7; FE website `/lien-he`, `ContactForm`, `NewsletterForm` (trang chủ + thẻ ngôn ngữ `coming_soon` gửi `interest`); FE admin 2 màn hộp thư. nginx `antfarms.xyz` mở đúng `/cms/api/public/`.
- Phụ thuộc: W7 (và W3 cho admin shell).
- Tiêu chí: ApiTests honeypot ⇒ 202 + `spam`; `renderedAt` < 3 giây ⇒ spam; rate limit 6 lần/10 phút ⇒ 429; nhận tin trùng ⇒ 1 dòng; CSV có BOM + chống injection (`=cmd` ⇒ `'=cmd`); lọc ngày VN nửa hở (ca 23:30 VN); tay: gửi form ở 375px, xử lý trong admin, mở CSV bằng Excel đúng dấu. *Bổ sung khi tới lượt.*

### Feature W10: identity-service — API nội bộ quản trị tài khoản + cài đặt đăng ký runtime + thống kê
- Mục tiêu: identity có API nội bộ (chỉ cổng 8081/5291 + `X-Service-Key`) để cms-backend (W11) khoá/mở, gỡ khoá tạm, đặt lại mật khẩu, thu hồi phiên, xem thống kê đăng ký, bật/tắt đăng ký lúc chạy — identity **vẫn không có vai trò/quyền** (R-W3).
- Phạm vi BE: §5.2.9 (phần "CHI TIẾT LÀM NGAY" ưu tiên); DB §5.1.7 migration `W10_Settings` trong `af_identity`; API §6.5. FE / học liệu: không. **W11 (cms-backend gọi API này + màn `/tai-khoan`) KHÔNG thuộc W10.**
- **Ranh giới file:** CHỈ `backend/services/identity-service/**` (src + tests + `Dockerfile` + `launchSettings.json` + `appsettings*.json(.example)`). Không sửa `deploy/**`, `CLAUDE.md`, gateway, `shared/`, `Directory.Packages.props` (không thêm package). Phần dùng chung ⇒ agent **ghi vào báo cáo**, orchestrator ráp theo §8.1.
- Phụ thuộc: không. Song song với W3a/W3b.
- Tiêu chí + tự test: 6 nhóm test W10 §5.2.9 xanh — bắt buộc có **`/internal/ping` qua cổng công khai + key đúng ⇒ 404**, key sai ⇒ 401, route công khai qua cổng nội bộ ⇒ 404, khoá ⇒ refresh cũ/đăng nhập hỏng, reset ⇒ mật khẩu tạm đăng nhập được + mật khẩu cũ 401 + `Cache-Control: no-store`, tự thao tác ⇒ 422, ranh giới ngày 23:30/00:30 VN, tắt đăng ký ⇒ `REGISTRATION_CLOSED`; **toàn bộ test F2 cũ vẫn xanh**. Tay (dev): `curl -i http://localhost:5281/internal/ping -H "X-Service-Key: <key dev>"` ⇒ 404; `curl -i http://localhost:5291/internal/ping -H "X-Service-Key: <key dev>"` ⇒ 200; `curl -i http://localhost:5280/identity/internal/ping` (qua gateway) ⇒ 404. Máy dev có `appsettings.Development.json` thật (gitignore) ⇒ báo cáo nhắc người dùng thêm section `Internal` vào file thật (thiếu ⇒ API nội bộ tắt, chỉ 404).

### Feature W11: Admin — quản trị tài khoản nền tảng
- BE §5.2.10, API §6.6; FE màn `/tai-khoan` (§5.3.3). Compose cms `IdentityInternal__*`.
- Phụ thuộc: W10, W2 (W3 nếu dùng nhật ký chung — `site.audit_logs` tạo ở W3 ⇒ **phụ thuộc W3**).
- Tiêu chí: ApiTests với identity giả (`HttpMessageHandler` stub): thiếu quyền ⇒ 403, chuyển tiếp 422, identity tắt ⇒ 503, audit ghi mọi thao tác không chứa mật khẩu; tay end-to-end: tạo tài khoản ở `apps/chinese`, khoá ở admin ⇒ ≤15 phút sau tài khoản mất phiên (hoặc F5 ngay nếu token đã hết), reset ⇒ đăng nhập mật khẩu tạm, tắt đăng ký ⇒ trang đăng ký chinese báo đóng. *Bổ sung khi tới lượt.*

### Feature W12: Tách `@af/chinese-kit` (apps/chinese không đổi hành vi)
- FE §5.3.5 W12; Dockerfile chinese COPY package mới.
- Phụ thuộc: không (nên ngay trước W13).
- Tiêu chí: `yarn workspace @af/chinese test` (vitest) số test giữ nguyên + test pinyin chạy ở package; `tsc -b`/`build` chinese sạch; nghiệm thu nhanh 1366/375: bài học, quiz, tra từ, quản trị bài học (vẫn ở chinese). *Bổ sung khi tới lượt.*

### Feature W13: Module Tiếng Trung trong admin (bài học, duyệt nghĩa, vai trò)
- FE §5.3.5 W13; nginx `admin.` đã có `/chinese/` từ W2.
- Phụ thuộc: W12, W2.
- Tiêu chí: toàn bộ ca nghiệm thu F4 (vai trò, `LAST_ADMIN`) + F10 (tạo/sửa/sắp xếp khối/quiz/xuất bản/gỡ/khôi phục, duyệt nghĩa hàng loạt, 409) chạy trên admin; tài khoản chỉ có `chinese:content.manage` thấy module nhưng không thấy mục vai trò. *Bổ sung khi tới lượt.*

### Feature W14: Gỡ quản trị khỏi apps/chinese + lối dẫn sang admin
- FE §5.3.5 W14 + R-W35; Dockerfile/compose `VITE_ADMIN_URL`.
- Phụ thuộc: W13 (**bắt buộc** — R-W34).
- Tiêu chí: grep rỗng; `/quan-tri/bai-hoc` ⇒ trang "đã chuyển" + nút mở admin; learner không thấy mục Quản trị; `tsc -b`/`build`/`test`/`lint:ui` chinese sạch; bundle chinese nhỏ đi (ghi số kB trước/sau). *Bổ sung khi tới lượt.*

### Feature W15: Rà soát triển khai tổng + tài liệu
- Mục tiêu: file triển khai nhất quán cho 3 host mới, sẵn cho F12.
- Phạm vi: §5.6.3 (gom/sửa những gì feature trước còn thiếu), `deploy/README.md` (DNS `@`/`www`/`admin`, lệnh `get-cert.sh` đủ 5 tên, tạo DB `af_cms` trên volume cũ, sinh secret, sao lưu `minio-data`), `preflight.sh`, `backup-db.sh`, `VERIFY-DOCKER.md` §5.6.3 (a)–(f), `CLAUDE.md` W15, HANDOFF mới.
- Phụ thuộc: W1–W14.
- Tiêu chí: `docker compose -f deploy/docker-compose.yml config` hợp lệ (nếu máy có Docker; không có thì review bằng mắt + ghi "chưa verify"); mọi biến `${...}` trong compose có trong `.env.example`; không service nào ngoài nginx có `ports:`; không đường nào tới 8081. *Mức thiết kế.*

---

## 8. Thứ tự thực thi & phụ thuộc

```
W1 ─► W2 ─► W3 ─► W4 ─┬─► W5 ─► W6 ─┐
                      └─► W7 ───────┴─► W8
                          W7 ─► W9
W10 ─► W11 (cần W3a)
W3 = W3a ─► W3b (chẻ 17/09/2026)
W12 ─► W13 (cần W2) ─► W14
W1…W14 ─► W15
```

- **Thứ tự đề xuất tuần tự (một mạch):** W1 → W2 → W3 → W4 → W5 → W6 → W7 → W8 → W9 → W10 → W11 → W12 → W13 → W14 → W15. Lý do: nền phân quyền trước; CMS có nội dung trước khi dựng website để website có dữ liệu thật; quản trị tài khoản sau vì đụng identity (vùng nhạy cảm, cần review kỹ); gom tiếng Trung cuối vì chỉ là chuyển chỗ, rủi ro hồi quy app học viên.
- **Có thể song song** (nếu Orchestrator muốn nhanh, vẫn commit tách): trong một feature, BE ‖ FE sau khi §6 chốt (W3–W6, W9, W11). **W10** độc lập hoàn toàn với W3–W9 (khác service). **W12** độc lập với mọi W khác (chỉ đụng `apps/chinese` + package mới). **W7 FE khung** làm được song song W5/W6 (dùng `hero_mode=static`).
- **Phân công agent:** mọi FRONTEND (`apps/admin`, `apps/website`, `packages/chinese-kit`, `apps/chinese`) → `frontend-implement` **model `fable`**; Backend/Database → Sonnet; Review/Integration → Opus.
- **Trong phiên tới 22:30 17/09:** mục tiêu thực tế W1 → W2 (→ W3 nếu kịp). Không dừng hỏi giữa feature; quyết định mở dùng mặc định §10.2.

### 8.1 Phiên 17/09/2026 tối (tới 23:33): W3a ‖ W10 song song, rồi W3b

```
Làn A (cms):      W3a BE ‖ W3a FE ─► review ─► commit ─► W3b BE ‖ W3b FE ─► review ─► commit
Làn B (identity): W10 BE ───────────► review ─► commit
```

- Hai làn **không chung file nào** (W3: `backend/services/cms-backend/**` + `frontend/apps/admin/**` + `frontend/yarn.lock` ở W3b; W10: `backend/services/identity-service/**`). File chung (`backend/backend.slnx`, `Directory.Packages.props`, `deploy/**`, `CLAUDE.md`, `docs/**`) **không agent nào sửa**. Commit tách; trước mỗi commit chạy lại `dotnet build`/`dotnet test` toàn solution vì làn kia có thể đã commit.
- Hết giờ: ưu tiên hoàn tất **W10** + **W3a**; W3b dời phiên sau (không commit dở).
- **Đoạn orchestrator ráp sau khi W10 qua review** (agent BE không tự sửa):
  1. `deploy/docker-compose.yml`, service `identity-service` → `environment:` thêm `Internal__Port: "8081"` và `Internal__ServiceKey: ${IDENTITY_INTERNAL_KEY:-}` (rỗng ⇒ API nội bộ tắt, service vẫn chạy); **không** thêm `ports:`; gateway **không** thêm cluster. `ASPNETCORE_URLS` hai cổng nằm trong Dockerfile; nếu compose đè `ASPNETCORE_URLS` thì sửa thành `http://+:8080;http://+:8081`.
  2. `deploy/.env.example` thêm:
     ```
     # Khoá dịch vụ cho API nội bộ identity-service (cổng 8081, KHÔNG public). cms-backend (W11) dùng CÙNG giá trị.
     # Sinh: openssl rand -base64 48   (>= 32 ký tự; rỗng/ngắn => API nội bộ tắt, trả 404)
     IDENTITY_INTERNAL_KEY=
     ```
     và chú thích `AUTH_ALLOW_REGISTRATION`: "Chỉ là giá trị KHỞI TẠO — khi admin đã bật/tắt đăng ký (bảng identity.settings) thì giá trị trong DB thắng."
  3. `deploy/VERIFY-DOCKER.md` thêm mục W10 ("chưa verify"): (a) `curl -sk -o /dev/null -w "%{http_code}" https://id.antfarms.xyz/internal/ping` ⇒ `404`; (b) từ một container trong `af-net` (gateway; từ W11 là cms-backend): `wget -qO- --header "X-Service-Key: $IDENTITY_INTERNAL_KEY" http://identity-service:8081/internal/ping` ⇒ `{"ok":true}`; (c) `docker compose port identity-service 8081` ⇒ không có ánh xạ; (d) `wget -qO- http://identity-service:8081/.well-known/jwks.json` ⇒ 404.
  4. `CLAUDE.md`: (i) Quy tắc bắt buộc thêm quy tắc §5.7 dòng W10 (nguyên văn); (ii) bảng cổng thêm `identity-service nội bộ (W10) | http://localhost:5291 (chỉ /internal/*, không qua gateway) | identity-service:8081 (không publish)`; (iii) mục identity-service thêm "`/internal/*` (W10): quản trị tài khoản + `identity.settings` (`registration.enabled`, cache 30s) — chỉ cms-backend gọi"; (iv) cập nhật dòng tiến độ "W1, W2, W12 xong".
- **Đoạn orchestrator ráp sau W3a/W3b:** `CLAUDE.md` mục cms-backend thêm "schema `site`: `settings`, `languages`, `audit_logs` (W3a), `faqs` (W3b); `GET /api/public/site` ẩn danh, `IAuditLogger` + `IRevalidationNotifier` (no-op tới W7)"; mục `apps/admin` thêm "`src/components/` (`MarkdownEditor`/`MarkdownPreview` — `react-markdown` + `remark-gfm`, `skipHtml`, không `rehype-raw`)"; dòng tiến độ. Không đổi `deploy/**` (không biến môi trường/cổng mới).

---

## 9. Tiêu chí hoàn thành + cách kiểm thử

### 9.1 Cổng mọi feature

§5.0.3 + `git status` không có bí mật (`appsettings.Development.json`, `.env`, secret) + migration chạy được trên DB dev trống **và** DB dev đang có dữ liệu feature trước + Integration báo số test chạy/skip (bắt buộc đặt `AF_TEST_PG`).

### 9.2 Test tích hợp

- cms-backend: `af_cms_test`, token `TestTokenFactory` audience `af-cms`; MinIO và identity nội bộ **giả** trong ApiTests (interface + stub), không cần dịch vụ thật.
- identity W10: `af_identity_test` như F2; kiểm cổng nội bộ bằng cấu hình `Internal:Port` khác cổng TestServer.
- website: không có test tự động đợt này (**BA-mặc định**; next build là cổng); hàm thuần (`lib/seo.ts`, slug) nếu có logic đáng kể thì thêm vitest ở W8.

### 9.3 Nghiệm thu end-to-end cuối đợt (sau W14)

Chạy gateway + identity (2 cổng) + chinese + cms + MinIO dev + website + admin + chinese app. Ở 1366px và 375px:
1. Admin bootstrap đăng nhập `admin` → cấu hình site, thêm ảnh, soạn trang giới thiệu + 1 bài viết + 3 FAQ, xuất bản.
2. Website: trang chủ hiện tagline/ngôn ngữ/FAQ/bài mới; `/bai-viet/<slug>` đúng; sitemap có bài; đổi tiêu đề bài ở admin ⇒ website đổi trong vài giây.
3. Bấm "Bắt đầu học tiếng Trung" ⇒ `chinese` `/dang-ky`.
4. Gửi liên hệ + nhận tin "Tiếng Nhật" ⇒ admin thấy, đánh dấu xử lý, xuất CSV.
5. Tài khoản học viên mới: admin khoá ⇒ mất phiên; mở + reset ⇒ đăng nhập bằng mật khẩu tạm; tắt đăng ký ⇒ đăng ký bị chặn.
6. Admin duyệt nghĩa một từ + xuất bản bài học trong module Tiếng Trung ⇒ học viên thấy ở `apps/chinese`.
7. `apps/chinese` không còn màn quản trị; mục Quản trị dẫn sang admin.
8. Tài khoản chỉ `editor`: không thấy Tài khoản/Hộp thư/Tiếng Trung, Dashboard giải thích.

---

## 10. Rủi ro / quyết định mở / ràng buộc

### 10.1 Rủi ro

| # | Rủi ro | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| RW1 | API nội bộ identity lộ ra Internet (thêm nhầm cluster gateway/location nginx/ports, hoặc kiểm bằng header `Host` giả được) | Ai cũng khoá/đặt mật khẩu tài khoản bất kỳ | R-W4 hai lớp (LocalPort + key), key rỗng ⇒ tắt; test 404 qua cổng công khai; quy tắc CLAUDE.md W10; VERIFY (a) |
| RW2 | `IDENTITY_INTERNAL_KEY` lệch giữa identity và cms | Mọi thao tác tài khoản 503/401 — admin tưởng hệ thống hỏng | cms dịch 401 `SERVICE_KEY_INVALID` thành thông điệp "cấu hình khoá nội bộ không khớp"; log Error kèm gợi ý; VERIFY (b) |
| RW3 | Quên thêm `af-cms` vào `Jwt:Audiences` ở môi trường có ghi đè cấu hình (vd `.env` thêm `Jwt__Audiences__*`) | cms trả 401 mọi request, build/test sạch | W1 test token có `af-cms`; compose không ghi đè audiences (giữ appsettings); ghi chú `.env.example` |
| RW4 | Quên `http://localhost:3290` / `https://admin.` trong `AllowedOrigins` (file dev thật gitignore) | Đăng nhập admin 403 `ORIGIN_NOT_ALLOWED` | Báo cáo W2 nhắc người dùng sửa file thật; log Warning của identity nêu origin |
| RW5 | Mở admin tạo bản ghi `learner` ở chinese cho nhân sự CMS (R-W12) | Thống kê người học tiếng Trung đếm dư | Chấp nhận; nếu cần sau: chinese bỏ qua provision khi header `X-Af-Client: admin` (không làm đợt này) |
| RW6 | Next.js standalone trong monorepo yarn: `outputFileTracingRoot` sai ⇒ thiếu `node_modules` trong ảnh; hoặc hai bản React | Ảnh chạy lỗi "Cannot find module"/hook lỗi | Dải React trùng các app; `outputFileTracingRoot` = `frontend/`; ảnh chạy thử `node server.js` khi có Docker; mẫu MedDental |
| RW7 | `next build` trong Docker gọi CMS không có ⇒ build vỡ hoặc nướng trang rỗng vĩnh viễn | Không deploy được / trang trống tới khi revalidate | `generateStaticParams` trả `[]` khi lỗi; trang chủ fetch lúc request (ISR) chứ không chỉ lúc build; `revalidate: 3600` tự lành |
| RW8 | Webhook revalidate hỏng im lặng (secret lệch, sai URL) | Website hiển thị nội dung cũ tới 1 giờ | Log Warning mỗi lần lỗi; VERIFY (d)(e); nhãn "Website cập nhật trong vài giây, tối đa 1 giờ" ở admin |
| RW9 | XSS qua Markdown/ảnh SVG | Chiếm phiên người xem website/admin | Không HTML thô, loại SVG, `nosniff`; admin xem trước bằng cùng renderer an toàn |
| RW10 | Mật khẩu tạm lọt log (request logging, audit, exception) | Lộ thông tin đăng nhập | Không log body; `no-store`; review soát; test W10 kiểm log sink |
| RW11 | Gom F10 sang admin làm hồi quy app học viên (W12 dời code dùng chung) | Học viên hỏng màn bài học/tra từ | W12 tách riêng, không đổi hành vi, vitest + nghiệm thu nhanh; W14 chỉ sau khi W13 đạt |
| RW12 | Spam form vượt honeypot | Hộp thư rác | Rate limit + `spam` thống kê; D-W3 nâng Turnstile khi cần |
| RW13 | MinIO dùng root key trong cms (N-W4) | Lộ key ⇒ toàn quyền mọi bucket | Chấp nhận giai đoạn đầu (một server, mạng nội bộ); nợ tạo user chỉ quyền bucket `af-cms` |
| RW14 | Mất volume `minio-data` | Mất ảnh (không dựng lại được) | `backup-db.sh` + README sao lưu volume |
| RW15 | Tham số ngày `from/to` hộp thư/thống kê bind `DateTime` Unspecified | 500 lúc lọc | Dùng `DateOnly` + quy đổi múi giờ VN; test ca biên (CLAUDE.md) |
| RW16 | HSTS apex bật `includeSubDomains` nhầm | Subdomain chưa HTTPS không vào được 1 năm | Giữ khuôn RK27 |
| RW17 | Đổi slug không redirect (R-W24) | Link chia sẻ cũ 404, mất SEO | Cảnh báo UI; nợ N-W3 |

### 10.2 QUYẾT ĐỊNH MỞ — đã có mặc định, không chặn; người dùng duyệt sau

| Mã | Câu hỏi | Mặc định đang áp + lý do | Ảnh hưởng nếu đổi |
|---|---|---|---|
| **D-W1** | Trình soạn thảo bài viết: TipTap rich text (HTML) hay Markdown? | **Markdown + xem trước** — an toàn XSS không cần sanitizer server, không thêm TipTap (~vài trăm kB) vào admin, nội dung dễ sao lưu/diff. TipTap thân thiện hơn cho người không quen Markdown (MedDental dùng) | Đổi sau W6 ⇒ phải chuyển dữ liệu + thêm sanitizer (HtmlSanitizer) — nên chốt **trước W3** (W3 dùng Markdown cho FAQ) |
| **D-W2** | Website có đa ngôn ngữ giao diện (EN...)? | **Chỉ tiếng Việt** — người học là người Việt | Đổi ⇒ thêm cột theo locale + route `/en` — ảnh hưởng W3–W8 |
| **D-W3** | Chống spam form | **Honeypot + thời gian điền ≥3s + rate limit 5/10 phút/IP** — không phụ thuộc bên thứ ba, không cookie/script ngoài | Cloudflare Turnstile: thêm site key/secret + script ngoài; ảnh hưởng W9 |
| **D-W4** | "Đăng ký mở" là cài đặt runtime (DB) hay giữ env? | **Runtime DB, env làm giá trị khởi tạo** — bật/tắt tức thì khi bị spam đăng ký, không restart | Giữ env ⇒ bỏ `identity.settings` + màn công tắc chỉ hiển thị; ảnh hưởng W10–W11 |
| **D-W5** | `www.antfarms.xyz`? | **Có, 301 về apex** (người dùng hay gõ www); cần bản ghi DNS + thêm vào chứng chỉ | Không dùng ⇒ bỏ khối + tên khỏi `get-cert.sh`; ảnh hưởng W7/W15 |
| **D-W6** | Sau khi admin đặt mật khẩu tạm có **ép đổi** ở lần đăng nhập kế? | **Không ép** — ép cần cờ trong token/`@af/auth` + màn đổi bắt buộc ở mọi app | Ép ⇒ thêm cột `password_reset_required`, lỗi `PASSWORD_CHANGE_REQUIRED` ở login, sửa `@af/auth`; W10 + thêm feature FE |
| **D-W7** | Vai trò CMS (`admin`/`editor`/`support`) và 6 quyền có đúng ý? | Như R-W9 | Đổi catalog ⇒ chỉ sửa `RoleCatalog` + seed (chèn bù); nên chốt trước W1 xong review, không chặn |
| **D-W8** | Cổng dev: website 3281, admin 3290, cms 5290, identity nội bộ 5291 | Như §5.0.1 | Đổi rẻ trước W2 |
| **D-W9** | Nội dung chữ trang chủ (thông điệp, học phí/miễn phí?) | Khối "lộ trình học" cố định trong code + tagline từ CMS; **không** nêu giá | Người dùng soạn nội dung thật trong CMS; câu FAQ học phí để trống chờ chốt |

### 10.3 Mặc định BA khác (không chặn)

- R-W6 một token nhiều audience; R-W8 ảnh phục vụ qua cms-backend, bucket private; R-W12 chấp nhận provision `learner`.
- Không dùng ImageSharp (giấy phép) — tự đọc header ảnh; không dnd-kit (nút lên/xuống như F10); biểu đồ SVG tự vẽ.
- Revalidate qua Channel + BackgroundService; tag §5.4.3; `revalidate` dự phòng 3600 giây.
- Website không có test tự động (next build là cổng); ngoại lệ `tsc -b` cho app Next.
- Admin `noindex`; admin không có trang đăng ký.
- `W<n>_<Ten>` cho tên migration đợt này.
- cms-backend chép khuôn access của chinese thay vì tách thư viện chung ngay (N-W1).

### 10.4 Nợ ghi nhận

- N-W1: tách `AntFarm.Access` (provision + resolver + seeder khuôn) khi có service thứ ba dùng phân quyền cục bộ.
- N-W2: email xác nhận nhận tin (double opt-in), quên mật khẩu tự phục vụ — cần nhà cung cấp email.
- N-W3: bảng chuyển hướng 301 khi đổi slug.
- N-W4: MinIO user riêng quyền bucket `af-cms`.
- N-W5: kiểm ảnh chèn trong thân Markdown khi xoá ảnh.
- N-W6: bỏ provision `learner` khi chỉ mở admin (RW5).

### 10.5 Ràng buộc dự án phải nhắc agent thực thi

- Build/test sạch (§5.0.3), commit local riêng từng feature, **không push**, nhánh `develop`.
- identity chỉ xác thực; mỗi service tự phân quyền DB riêng; JWT chỉ nhận diện; FE đọc quyền từ `/me` của từng service (admin gộp có tiền tố); `RequirePermissionAttribute` gán `Policy` trong constructor, không `new string Policy`.
- Mỗi service một DB (`af_cms` mới); không đọc chéo DB — cms lấy dữ liệu tài khoản qua HTTP API nội bộ.
- MUI v9 (`slotProps`, shorthand trong `sx`); `renderInput` trải `params.slotProps` trước (dùng `AppAutocomplete`); `AppDialog`/`AppDrawer`; `useTabParam` cho tab cấp trang; không `uuid` (`crypto.randomUUID()`); `tsconfig.app.json` không `baseUrl`; `@af/*` import thẳng TS source, peer dependency khai ở app; yarn.
- Npgsql `timestamptz` chỉ `Kind=Utc`; tham số ngày query ⇒ `DateOnly` hoặc `SpecifyKind` + nửa hở; ngày thống kê/lọc theo `Asia/Ho_Chi_Minh`.
- Seed idempotent, không ném; danh mục người dùng xoá được (ngôn ngữ, FAQ, trang, bài) chỉ gieo khi bảng trống; settings chèn bù khoá.
- Mọi truy cập qua reverse proxy; chỉ nginx có `ports:`; không `# syntax=`; `HEALTHCHECK wget`; Dockerfile/compose/nginx cập nhật cùng feature, ghi "chưa verify"; `get-cert.sh` truyền ĐỦ tên miền.
- Nút ẩn theo quyền kèm lời giải thích; trang lỗi 4xx thống nhất (`ErrorPage`, `/401 /403 /404`); lời gọi `/me` nền dùng `skipErrorRedirect`.
- Mobile-first 375px cho website và admin (admin ưu tiên 1366px nhưng không vỡ ở 375px).
- Học liệu/nội dung: không chép nội dung có bản quyền; seed là dàn ý nháp chưa xuất bản.
