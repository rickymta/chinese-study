# AntFarm — Frontend (`frontend/`)

Monorepo frontend của AntFarm: **Turborepo + Yarn Classic 1.22 workspaces + React 19 + MUI v9 + TypeScript 6 + Vite 8**.
Mỗi ngôn ngữ là một app riêng trong `apps/` dùng chung các package `@af/*` trong `packages/`.
Hợp đồng thực thi: `docs/agent-workflow/2026-09-16-antfarm-nen-tang-tieng-trung-mvp-hop-dong-thuc-thi.md` (§5.3, §5.6.4).

## Cấu trúc

```
frontend/
  package.json  turbo.json  yarn.lock  .prettierrc.json
  scripts/check-ui-conventions.mjs      # yarn lint:ui — cổng chặn quy ước giao diện
  packages/
    tsconfig/                           # @af/tsconfig — base.json, react-app.json, node.json
    ui/                                 # @af/ui — buildTheme, ThemeProvider, AppLayout, PageContainer, ErrorPage, NotFoundPage, LangText
    api/                                # @af/api — createApiClient (axios): Bearer, làm mới 401 single-flight, ApiError
    utils/                              # @af/utils — parseApiError, schema zod (emailSchema, passwordSchema, displayNameSchema, timeZoneSchema)
    auth/                               # @af/auth — createAuthSession, AuthProvider/useAuth, RequireAuth, RequirePermission, LoginPage/RegisterPage
    chinese-kit/                        # @af/chinese-kit — pinyin (số⇄dấu), Hanzi/Pinyin, ChineseSpeechProvider/SpeakButton, LessonContent + khối/quiz, kiểu bài học/từ điển (W12)
  apps/
    chinese/                            # @af/chinese — app tiếng Trung, cổng dev 3280
    admin/                              # @af/admin — Admin kiêm CMS chung (W2), cổng dev 3290, proxy /identity /cms /chinese → 5280
```

`apps/admin` (W2, hợp đồng `docs/agent-workflow/2026-09-17-antfarm-website-admin-cms-hop-dong-thuc-thi.md` §5.3.1):
đăng nhập bằng tài khoản nền tảng (không có link Đăng ký), `src/auth/loadMe.ts` gọi song song `GET /cms/api/me` +
`GET /<ngôn-ngữ>/api/me` của mọi module trong `src/modules/registry.ts` và gộp quyền **có tiền tố service**
(`cms:users.manage`, `chinese:content.manage` — `useAuth().can('cms:site.manage')`); service không phản hồi chỉ bị ẩn
phần đó (Dashboard cảnh báo), 0 quyền khi mọi service đều trả lời ⇒ `/403` có nút Đăng xuất. Hàm gộp là hàm thuần
`src/auth/mergeMe.ts` có test: `yarn workspace @af/admin test`. Route admin là tiếng Việt không dấu; module ngôn ngữ
nằm dưới `/ngon-ngu/<code>/...` (W13). W2 có màn **Người dùng CMS** (`/nguoi-dung-cms`, quyền `cms:users.manage`).

`@af/*` là workspace symlink, **import thẳng TS source** (không build/dist): sửa `packages/<pkg>/src` là vá cho mọi app.
`@af/chinese-kit` (W12) chỉ chứa thứ **không gọi API** — dùng chung giữa `apps/chinese` và module Tiếng Trung của admin (W13);
hook gọi máy chủ (`useTtsRate`, query key...) ở lại app và tiêm vào kit qua props (`ChineseSpeechProvider rate/onRateChange`).
Kit có test riêng: `yarn workspace @af/chinese-kit test` (pinyin, inlineZh) — `yarn test` ở root chạy cả app lẫn kit.
Package dùng chung khai thư viện ở `peerDependencies` ⇒ **app tiêu thụ phải khai đủ trong `dependencies`** (máy dev
hoisted nên không báo, chỉ Docker/CI lộ). Trước khi gỡ dependency khỏi app: `grep -l "<thu-vien>" packages/*/package.json`.

## Lệnh (chạy trong `frontend/`)

```bash
yarn install                          # cài toàn bộ workspace
yarn workspace @af/chinese dev        # http://localhost:3280 (Vite, strictPort)
yarn workspace @af/chinese tsc -b     # type-check — BẮT BUỘC -b (root tsconfig có files: [])
yarn workspace @af/chinese build      # tsc -b && vite build → apps/chinese/dist
yarn workspace @af/admin dev          # http://localhost:3290 (W2) — cần thêm cms-backend (5290) sau gateway
yarn workspace @af/admin tsc -b       # type-check admin; `build` / `test` tương tự
yarn lint:ui                          # quét apps/*/src; exit 1 khi vi phạm luật FAIL
```

Dev cần 3 backend chạy trước theo thứ tự **identity-service (5281) → chinese-backend (5282) → gateway (5280)**.
Trang chủ có hai chip trạng thái "Tiếng Trung" / "Tài khoản" tự kiểm mỗi 30 giây — chip đỏ nghĩa là tiến trình
tương ứng chưa lên (gateway trả 502).

## Luồng mạng — trình duyệt chỉ nói chuyện với gateway

| Lời gọi | Dev (Vite) | Production (nginx biên) |
|---|---|---|
| API tiếng Trung | `/chinese/api/...` → proxy → `localhost:5280` | `chinese.antfarms.xyz/chinese/api/...` → gateway (cùng origin) |
| API identity | `/identity/api/...` → proxy → `localhost:5280` | `https://id.antfarms.xyz/api/...` (CORS có credentials) |

Gốc API identity là biến build `VITE_IDENTITY_API_URL` (`apps/chinese/.env.example`; Dockerfile mặc định
`https://id.antfarms.xyz/api`). **Nướng vào bundle lúc build** — sai giá trị thì ảnh vẫn chạy, chỉ hỏng khi bấm đăng nhập.
Route SPA **không** được bắt đầu bằng `/identity` hoặc `/chinese` (bị proxy nuốt) — dùng slug tiếng Việt không dấu.

## Xác thực (F2 — `@af/auth`)

- **Access token chỉ trong bộ nhớ** (`createAuthSession`), không localStorage; refresh token là cookie HttpOnly `af_rt` do
  identity-service đặt (dev `Path=/identity/api/auth`, prod `Domain=.antfarms.xyz; Path=/api/auth`).
- Khi mở app: `POST /auth/refresh` (bọc `navigator.locks.request('af-auth-refresh')` để hai tab F5 cùng lúc xoay tuần tự)
  ⇒ dựng `account` từ claim JWT ⇒ `loadMe()` do app truyền (quyền lấy từ service ngôn ngữ, không suy từ JWT).
  Làm mới chủ động 60 giây trước hạn; `BroadcastChannel('af-auth')` đồng bộ đăng nhập/đăng xuất giữa tab.
- `createApiClient({ getAccessToken, refresh, onAuthLost })`: gắn Bearer; 401 (trừ `/auth/*`) ⇒ refresh single-flight rồi
  gửi lại **một** lần; hỏng ⇒ `onAuthLost` ⇒ `RequireAuth` đưa về `/dang-nhap?returnTo=...&reason=expired`.
  Interceptor này đăng ký TRƯỚC bước chuyển `AxiosError → ApiError` (cần `err.config` để gửi lại).
- App: `App.tsx` bọc `AuthProvider({ session, identity, loadMe })` giữa QueryClientProvider và RouterProvider; route `/`
  nằm dưới `RequireAuth`; `/dang-nhap`, `/dang-ky` dùng trang chung; `/401`, `/403`, `/404`.
- `apps/chinese/src/features/auth/loadMe.ts` là **stub F2** (`permissions: ['study.use']`) — F3 thay bằng `GET /chinese/api/me`.

## Quy ước bắt buộc (chi tiết ở `CLAUDE.md` gốc)

- MUI v9: mọi `*Props` cũ → `slotProps`; shorthand (`justifyContent`, `fontWeight`...) đặt trong `sx`.
- `renderInput` của Autocomplete: trải `...params.slotProps` **trước** rồi mới ghi đè slot con (lint `autocomplete-slotprops-override`).
- Dialog/Drawer dùng `AppDialog`/`AppDrawer` của `@af/ui` (F4) — không `<Dialog>` trần (lint `raw-dialog`, FAIL).
- Không `uuid` → `crypto.randomUUID()` (lint `uuid-import`, FAIL). Tab cấp trang lên URL bằng `useTabParam` (lint `tabs-no-url`, WARN).
- `tsconfig.app.json` không `baseUrl`; alias `@/*` → `./src/*`.
- Chữ Hán: `<LangText lang="zh-CN">` (phông `--af-font-cjk`); pinyin hiển thị dạng dấu.
- Mobile-first: mọi màn dùng tốt ở ~375px (AppLayout tự chuyển AppBar + BottomNavigation dưới `md`).
- Chế độ sáng/tối: nút trên AppBar/Drawer, lưu `localStorage['af.themeMode']`.

## Docker (⚠️ CHƯA VERIFY — máy dev không có Docker)

`apps/chinese/Dockerfile` (build context `./frontend`, node:22-alpine → nginx:1.27-alpine, `HEALTHCHECK` bằng `wget`) và
`apps/chinese/nginx.conf` (chỉ phục vụ tĩnh: khối `.mjs` → `application/javascript`, SPA fallback, **không proxy API**).
Checklist kiểm trên máy có Docker: `deploy/VERIFY-DOCKER.md`. Build tay:

```bash
docker compose -f deploy/docker-compose.yml build chinese-frontend
```

## Thêm app ngôn ngữ mới

Chép `apps/chinese` → `apps/<ngon-ngu>` (đổi `name` thành `@af/<ngon-ngu>`, cổng dev 3282+, proxy `/<ngon-ngu>`,
`baseURL: '/<ngon-ngu>/api'`, `accent` riêng trong `App.tsx`), thêm Dockerfile/nginx.conf tương tự. Dùng lại `@af/*`,
không tạo lại ThemeProvider/AuthProvider/axios wrapper. Checklist đầy đủ ở hợp đồng §5.5.
