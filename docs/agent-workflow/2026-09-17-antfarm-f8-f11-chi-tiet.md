# AntFarm — F9 Bài học + quiz · F8 Luyện viết · F10 Quản trị nội dung · F11 Tổng quan — Hợp đồng thực thi chi tiết

- Ngày: 2026-09-17 · Loại: tạo mới (chi tiết hoá §5.1.6, §5.2.4, §5.3.3, §5.4, §6.7, §6.8, §7 của hợp đồng gốc) · Service/app: `chinese-backend`, `frontend/apps/chinese`, `content/chinese/` · Module: lessons, writing, admin-content, progress
- Hợp đồng gốc: `docs/agent-workflow/2026-09-16-antfarm-nen-tang-tieng-trung-mvp-hop-dong-thuc-thi.md` (gọi tắt **HĐG**). File này **bổ sung**, không thay HĐG; chỗ nào khác HĐG thì file này thắng và ghi rõ "(thay HĐG …)".
- Tài liệu song song: `2026-09-17-antfarm-f4-f5-chi-tiet.md` (F4, F5) và `2026-09-17-antfarm-f6-f7-chi-tiet.md` (F6, F7) — đã đối chiếu ngày 17/09/2026 (bản F4–F5 đầy đủ; bản F6–F7 lúc đối chiếu **chưa có phần backend F7**), tên đã khớp ghi ở **§4.2 Bảng điểm cần khớp** (cột "Trạng thái"). Agent thực thi phải đối chiếu §4.2 với hai tài liệu kia trước khi code; lệch thì **tài liệu F4–F7 + code đã commit thắng**, sửa tên ở đây cho khớp (không đổi nghiệp vụ).
- **Thứ tự làm: F9 → F8 → F10 → F11.** Mỗi feature là một commit local riêng, dừng chờ người dùng OK (trừ khi Orchestrator được giao chạy một mạch).
- Quyết định: người dùng giao chạy một mạch, các quyết định mở dùng **mặc định BA**, ghi **[BA-mặc định]** (§10.2).

> Điểm tựa khi context bị nén: đọc §3 (quy tắc) → §7 (feature đang làm) → mục §5/§6 mà feature trỏ tới → §4.2 (điểm khớp).

---

## 1. Bối cảnh & mục tiêu

### 1.1 Bối cảnh

Code hiện tại mới có F0 (khung backend) và F1 (khung frontend). F2–F7 sẽ được làm trước bốn feature trong file này (thứ tự HĐG §8: F2 → F3 → F5 → F4 → F6 → F7 → **F9 → F8 → F10 → F11**). Vì vậy file này giả định lúc bắt đầu F9 đã có:

- F2/F3: `AntFarm.Auth`, `[RequirePermission]`, `access.users` (có `time_zone`), `/api/me`, `TestTokenFactory`, `@af/auth` (`RequireAuth`, `RequirePermission`, `useAuth().hasPermission`), trang `/401` `/403` `/404`.
- F4: `@af/ui` có `AppDialog`, `AppDrawer`, `AppAutocomplete`, `ConfirmProvider/useConfirm`, `ToastProvider`.
- F5: `learning.study_events` + `IStudyActivityRecorder`; `@af/ui` có `useTabParam`, `speech` (`useSpeech`); `apps/chinese/src/lib/pinyin.ts` (`numberedToMarked`), component `Hanzi`/`Pinyin`; vitest trong app; khung `content/` + `validate.mjs`; csproj Api link `content/chinese/data/**/*.json`; Dockerfile chinese-backend đã đổi build context để chứa học liệu.
- F6: `content.words`, `content.characters`, `content.word_characters`, `content.import_runs`, `ContentImporter`, `VietnameseText.RemoveDiacritics`, `GET /api/dictionary/*`, `hsk-words.json`, `characters.json`.
- F7: `learning.srs_cards` (có cột `source` = `path|manual|lesson`), `learning.learner_settings`, `GET /api/srs/summary`, dịch vụ thêm thẻ (`POST /api/srs/cards`), `Microsoft.Extensions.TimeProvider.Testing`.

### 1.2 Mục tiêu phần mềm

| Feature | Kết quả người dùng thấy |
|---|---|
| **F9** | Danh sách bài học chủ đề HSK 1; đọc nội dung (hội thoại có pinyin + nghĩa, ngữ pháp, mẹo), danh sách từ của bài, làm quiz chấm ở server; đạt ≥ 80% là hoàn thành, từ của bài tự vào ôn tập. 5 bài seed tự soạn. |
| **F8** | Luyện viết từng chữ Hán theo 3 bước Xem → Tô theo → Tự viết trên điện thoại; chọn chữ theo bộ HSK 1, theo bài học, hoặc "cần luyện"; hệ thống nhớ số lỗi và chữ đã thuộc. Dữ liệu nét đóng gói sẵn trong app, không gọi CDN. |
| **F10** | Người có `content.manage` soạn/sửa/xuất bản bài học + quiz, duyệt bài seed, duyệt nghĩa Việt dịch máy (`machine` → `reviewed`) và Hán Việt; dữ liệu đã sửa/duyệt không bị lần nạp học liệu sau đè. |
| **F11** | Trang chủ thành bảng tổng quan: chuỗi ngày học, việc hôm nay, thẻ đến hạn, tiến độ từ vựng/bài học/viết/thanh điệu, lịch hoạt động 90 ngày — tất cả theo múi giờ hồ sơ. |

### 1.3 Mục tiêu học tập (nghiệp vụ sư phạm)

Người học số 0, sau G0 (pinyin, F5) và song song G1 (SRS, F7):

| Bước | Hoạt động | Vì sao ở vị trí này | Đo tiến bộ |
|---|---|---|---|
| 1. Bài học chủ đề (F9) | Đọc hội thoại ngắn → xem ngữ pháp → nghe/đọc từ → quiz | Từ học rời trong SRS dễ quên nghĩa ngữ cảnh; hội thoại đặt từ vào tình huống thật. Quiz kiểm **hiểu** (nghe chọn nghĩa, chọn câu đúng), không chỉ nhận mặt chữ. ≥ 30% câu nghe vì người Việt hay nhận mặt chữ nhưng không nghe ra thanh | Điểm quiz, bài hoàn thành |
| 2. Từ của bài vào SRS (F9→F7) | Hoàn thành bài ⇒ từ của bài thành thẻ ôn | Học xong mà không ôn thì sau 2–3 ngày quên phần lớn; bài vừa học là ngữ cảnh ghi nhớ mạnh nhất nên ưu tiên các thẻ này trước thẻ lộ trình | Thẻ `source='lesson'` được ôn |
| 3. Viết chữ của bài (F8) | Xem thứ tự nét → tô theo → tự viết không khung | Viết buộc chú ý từng bộ phận (bộ thủ), giúp phân biệt chữ gần giống; tô trước rồi mới tự viết để không tập thói quen sai thứ tự nét. Hiện **Hán Việt** làm cầu nối nghĩa riêng cho người Việt | Số lỗi mỗi lần, chữ "đã thuộc" (tự viết sạch ở ≥ 2 ngày khác nhau) |
| 4. Duyệt nội dung (F10) | Chủ dự án (kiêm admin) sửa nghĩa dịch máy, duyệt bài seed | Người số 0 không tự phát hiện nghĩa sai (RK4) — ưu tiên duyệt theo thứ tự lộ trình (`path_order`) để từ sắp học được duyệt trước | Số từ `reviewed` |
| 5. Tổng quan hằng ngày (F11) | Mở app ⇒ thấy việc hôm nay + chuỗi ngày | Thói quen hằng ngày quan trọng hơn cường độ; chuỗi ngày là động lực nhẹ, không gamification | Streak, lịch 90 ngày |

Thứ tự 5 bài seed: **chào hỏi → bản thân → số đếm → gia đình → thời gian**. Lý do: chào hỏi dùng được ngay; giới thiệu bản thân dùng lại đại từ bài 1 + thêm câu "A 是 B"; số đếm là nền cho tuổi, số người, giờ, ngày; gia đình dùng số (几口人); thời gian dùng số cho ngày/giờ và tổng hợp các bài trước.

---

## 2. Phạm vi

### 2.1 In-scope

- **F9**: migration `F9_Lessons`; nạp bài từ `content/chinese/data/lessons/*.json` idempotent theo `slug`; API học viên (danh sách, chi tiết, bắt đầu, nộp quiz, lịch sử lần làm); chấm ở server; hoàn thành ⇒ thêm thẻ SRS + ghi `study_events`; màn `/bai-hoc`, `/bai-hoc/:slug`; 5 bài tự soạn + `lesson.schema.json` + kiểm trong `validate.mjs`.
- **F8**: script đóng gói tập con `hanzi-writer-data` 2.0.1 vào `apps/chinese/public/hanzi-data/`; `hanzi-writer` 3.7.3; migration `F8_Writing`; API ghi lần viết, danh sách chữ theo bộ, chi tiết chữ, tóm tắt; màn `/luyen-viet`, `/luyen-viet/:hanzi`; nút "Luyện viết chữ của bài" ở F9.
- **F10**: migration `F10_ContentAdmin` (thêm cột, không bảng mới); API quản trị bài học (CRUD, thay khối/từ/quiz theo lô, xuất bản/gỡ, duyệt, lưu trữ) + duyệt từ vựng; màn `/quan-tri/bai-hoc`, `/quan-tri/bai-hoc/:id`, `/quan-tri/tu-vung`; bảo vệ dữ liệu đã sửa/duyệt khỏi lần nạp sau.
- **F11**: không migration; `StreakCalculator`; `GET /api/progress/overview`; trang chủ `/` mới.

### 2.2 Out-of-scope

- Quiz dạng khác (sắp xếp câu, điền từ, viết pinyin), chấm phát âm, âm thanh người thật (chỉ TTS trình duyệt).
- Khoá tuần tự bài học, chứng chỉ, huy hiệu, bảng xếp hạng, "đóng băng streak", nhắc nhở/thông báo.
- Luyện viết chữ phồn thể; chấm nét tự viết bằng thuật toán riêng (dùng chấm của `hanzi-writer`); luyện viết chữ không có trong `characters.json`.
- Sửa `content.characters` (Hán Việt cấp chữ, số nét) qua giao diện — sửa file học liệu rồi nạp lại.
- Tải ảnh/âm thanh cho bài học; trình soạn thảo rich-text (khối văn bản là chuỗi thuần + cú pháp chữ Hán nội dòng §5.4.3).
- Lịch sử phiên bản bài học / hoàn tác; nhiều người soạn đồng thời ngoài chặn xung đột `xmin`.
- Thống kê cho admin về học viên khác (F11 chỉ xem tiến độ của chính mình).

---

## 3. Quy tắc nghiệp vụ (đã chốt — gồm mặc định BA)

### 3.1 Bài học (F9)

- **R-LS1.** Bài học có `status`: `draft` (chỉ admin thấy), `published` (học viên thấy), `archived` (ẩn với học viên, admin thấy ở bộ lọc "Lưu trữ", chỉ xem). Học viên gọi chi tiết bài không `published` ⇒ `404 NOT_FOUND`.
- **R-LS2.** Bài có `review_status`: `machine` (do agent soạn/nhập, chưa người duyệt) hoặc `reviewed`. Bài seed luôn nạp với `machine`. Học viên thấy chip "Nội dung chưa được duyệt" khi `machine` (giống R-C4 cho nghĩa từ). **[BA-mặc định]**
- **R-LS3. Hoàn thành bài = một lần nộp quiz đạt ≥ 80%**, tính bằng số nguyên `correct * 100 >= 80 * total` (không làm tròn). `scorePercent` hiển thị = `floor(correct * 100 / total)`. Ví dụ 4/5 = 80 đạt; 7/9 = 77 không đạt. **[BA-mặc định — D7]**
- **R-LS4. Không khoá tuần tự**: mọi bài `published` đều mở. Danh sách gợi ý "bài tiếp theo" = bài `published` có `order_index` nhỏ nhất mà người học chưa `completed`. **[BA-mặc định — D7]**
- **R-LS5. Hoàn thành lần đầu** (chuyển `lesson_progress.status` sang `completed`) ⇒ trong **cùng transaction**: (a) tạo thẻ SRS `hanzi_to_meaning` cho mọi từ của bài chưa có thẻ, `source='lesson'`, trạng thái `new`; thẻ đã có (bất kể nguồn) giữ nguyên, không đổi `source`, không bỏ tạm dừng; (b) ghi `study_events(kind='lesson_complete', quantity=1, correct=NULL, ref_id=lesson_id)`. Lần đạt sau chỉ cập nhật `best_score_percent`, không tạo thêm sự kiện `lesson_complete`, không thêm thẻ. **[BA-mặc định — D7]**
- **R-LS6.** Thẻ `source='lesson'` vẫn chịu hạn mức thẻ mới/ngày (R-L3 HĐG). Hàng đợi thẻ mới của F7 ưu tiên **thẻ `new` đã tồn tại** (nguồn `lesson`/`manual`, theo `created_at` tăng dần) **trước** từ lộ trình chưa có thẻ (theo `path_order`). → điểm khớp K12 (§4.2). **[BA-mặc định]**
- **R-LS7. Mọi lần nộp quiz** (đạt hay không) ghi `study_events(kind='quiz_submit', quantity=<số câu>, correct=<số câu đúng>, ref_id=<quiz_attempt_id>)` ⇒ tính là "ngày có học" (R-PG1).
- **R-LS8.** Nộp quiz phải trả lời **đủ và đúng tập câu hỏi hiện tại** của bài (mỗi câu đúng một lần). Tập `questionId` khác tập câu hiện có (admin vừa sửa quiz) ⇒ `422 QUIZ_CHANGED`; frontend tải lại bài. Lựa chọn không thuộc câu ⇒ `400 VALIDATION`.
- **R-LS9.** Nộp quiz idempotent theo `clientAttemptId` (uuid do frontend sinh bằng `crypto.randomUUID()`): trùng ⇒ `200` trả lại đúng kết quả đã lưu, không ghi thêm gì.
- **R-LS10.** Đáp án (`correctOptionId`) và lời giải **không bao giờ** có trong API chi tiết bài cho học viên; chỉ trả trong kết quả nộp quiz và lịch sử lần làm.
- **R-LS11.** `quiz_attempts.answers` lưu **ảnh chụp** (câu hỏi, lựa chọn đã chọn, đáp án đúng, đúng/sai) để lịch sử vẫn đọc được khi admin sửa/xoá câu hỏi sau đó.
- **R-LS12.** "Bắt đầu bài" (`POST /api/lessons/{id}/start`) tạo `lesson_progress(status='in_progress')` nếu chưa có; idempotent; **không** ghi `study_events` (chưa có kết quả).
- **R-LS13. Sư phạm soạn bài seed** (content-implement, `validate.mjs` kiểm): 8–15 từ/bài (>15 FAIL; <8 WARN; 0 FAIL); mọi từ phải có trong `hsk-words.json` (khoá `simplified`+`pinyin`); 5–10 câu quiz; **≥ 30% câu `listen_choice`**; mỗi câu 3–4 lựa chọn; có ít nhất 1 khối `dialogue` và 1 khối `grammar`; chữ Hán xuất hiện trong hội thoại/ví dụ phải thuộc từ của bài hiện tại hoặc các bài trước (theo `orderIndex`) hoặc `glossary` của bài — thiếu ⇒ WARN (liệt kê chữ); nội dung **tự soạn** (`sources: ["original"]`), không chép giáo trình.
- **R-LS14. Nạp bài học** (thay HĐG §5.1.6 "chỉ khi bảng trống" — lý do: cho phép thêm bài 6…N bằng file về sau mà vẫn không đè dữ liệu người dùng sửa): idempotent theo `slug`:
  | Trạng thái trong DB | Hành động |
  |---|---|
  | Chưa có `slug` | Chèn bài (`source='seed'`, `review_status='machine'`, `status` theo file, mặc định `published`) + khối + từ + quiz |
  | Có, `edited_at IS NULL` **và** `review_status='machine'` **và** `source_hash` khác | Cập nhật tại chỗ: trường bài (giữ nguyên `status`), thay khối, thay từ, quiz upsert theo `key` (xoá câu có `key` không còn trong file) |
  | Có, `source_hash` bằng | Bỏ qua |
  | Có, `edited_at IS NOT NULL` hoặc `review_status='reviewed'` | Bỏ qua, log Information "Bài {slug} đã sửa/duyệt tay — không nạp đè" |
  | Có trong DB nhưng không có file (bài admin tạo, hoặc file bị gỡ) | Giữ nguyên, không log Warning |
  Bài bị admin "xoá" mà là bài seed ⇒ chuyển `archived` (không xoá cứng) nên lần nạp sau vẫn thấy `slug` và không tạo lại (R-CA7). **[BA-mặc định]**
- **R-LS15.** File bài có từ không tìm thấy trong `content.words` ⇒ **bỏ qua cả bài** (không nạp dở), log Error nêu slug + từ thiếu; các bài khác vẫn nạp. Lỗi JSON/validator ⇒ như trên. Importer **không bao giờ ném** ra ngoài.

### 3.2 Luyện viết (F8)

- **R-W1. Dữ liệu nét đóng gói tập con** `hanzi-writer-data@2.0.1` — chỉ chữ có trong `content/chinese/data/characters/characters.json` — vào `frontend/apps/chinese/public/hanzi-data/`, sinh bằng script, **chép nguyên từng byte** (không minify, không sửa nội dung), kèm `ARPHICPL.TXT` nguyên văn + `NOTICE.md`. Frontend **luôn** truyền `charDataLoader` đọc file cục bộ; **cấm** để `hanzi-writer` dùng loader mặc định (gọi `cdn.jsdelivr.net`). **[BA-mặc định — D3]** Xác minh 17/09/2026: gói npm `hanzi-writer-data@2.0.1` khai `"license": "SEE LICENSE IN ARPHICPL.TXT"`; README nêu dữ liệu từ Make Me a Hanzi, trích từ phông Arphic, phân phối lại/sửa theo Arphic Public License; ARPHICPL §1 cho phép sao chép nguyên văn với điều kiện **giữ nguyên file `ARPHICPL.TXT`** trong mọi bản sao; §2 yêu cầu ghi chú nổi bật nếu sửa file ⇒ ta không sửa nội dung, chỉ chọn tập con + đổi tên file, và ghi rõ việc đó trong `NOTICE.md`. `hanzi-writer@3.7.3` giấy phép MIT, có sẵn kiểu TS (`dist/types/index.esm.d.ts`).
- **R-W2. Ba bước** cho mỗi chữ:
  | Bước | `mode` ghi DB | Cách chơi | Gợi ý |
  |---|---|---|---|
  | Xem (`xem`) | không ghi | Hoạt hình thứ tự nét (lặp được), hiện số nét, bộ thủ, pinyin (dạng dấu), Hán Việt, nghĩa các từ chứa chữ, nút nghe | — |
  | Tô theo (`to-theo`) | `guided` | `quiz()` có `showOutline: true` | Tự hiện nét đúng sau **2** lần sai (`showHintAfterMisses: 2`) |
  | Tự viết (`tu-viet`) | `recall` | `quiz()` với `showOutline: false`, `showCharacter: false` | Tự hiện sau **3** lần sai; nút "Gợi ý nét" (`highlightStroke(nét hiện tại)`) |
- **R-W3.** Một **lần viết** = một lần `quiz()` chạy tới `onComplete`. Chỉ lần viết hoàn tất mới gửi server; bỏ giữa chừng không ghi. `hintsUsed` = số nét đạt ngưỡng tự hiện gợi ý (bắt ở `onMistake` khi `mistakesOnStroke === ngưỡng`) + số lần bấm "Gợi ý nét". `totalMistakes` lấy từ `onComplete`.
- **R-W4.** Mỗi lần viết ghi `study_events(kind='writing', quantity=1, correct=<recall: 1 nếu totalMistakes=0 và hintsUsed=0, ngược lại 0; guided: NULL>, ref_id=<writing_attempt_id>)` ⇒ "viết xong ≥ 1 chữ" là ngày có học.
- **R-W5. Trạng thái thuộc chữ** (`masteryStatus`):
  - `new`: chưa có lần viết nào.
  - `mastered`: `clean_recall_days ≥ 2` — số **ngày lịch khác nhau** (theo múi giờ người dùng) có ít nhất một lần `recall` sạch (0 lỗi, 0 gợi ý). Lần sạch thứ hai trong cùng ngày không tăng.
  - `practicing`: còn lại.
  - **Cần luyện** (`weak`): có lần viết, chưa `mastered`, và (`last_mistakes ≥ 2` hoặc `last_hints ≥ 1`); sắp theo `last_practiced_at` tăng dần (lâu chưa luyện lên trước), tối đa 50.
  **[BA-mặc định]**
- **R-W6.** Bộ chữ:
  - `hsk1`: chữ xuất hiện trong từ có `hsk3_level = 1`, thứ tự = `min(path_order)` của các từ chứa chữ đó, rồi `hanzi`.
  - `lesson:<slug>`: chữ trong từ của bài (bài `published`), theo thứ tự từ trong bài rồi vị trí chữ trong từ, không trùng.
  - `weak`: theo R-W5.
  - `practiced`: chữ đã viết, sắp `last_practiced_at` giảm dần.
- **R-W7.** Server chỉ nhận kết quả (không phục vụ dữ liệu nét). `hanzi` phải có trong `content.characters` ⇒ không có: `422 UNKNOWN_CHARACTER`. Không kiểm `totalStrokes` bằng `stroke_count` (Unihan và Make Me a Hanzi có thể lệch).
- **R-W8.** Ghi lần viết idempotent theo `clientAttemptId` (trùng ⇒ 200 trả kết quả cũ).
- **R-W9.** Chữ không có dữ liệu nét (không nằm trong `hanzi-data/index.json`) ⇒ hiện trong danh sách nhưng mờ + nhãn "Chưa có dữ liệu nét", không vào được bước Tô/Tự viết.

### 3.3 Quản trị nội dung (F10)

- **R-CA1.** Mọi API `/api/admin/lessons*`, `/api/admin/words*` yêu cầu `content.manage`. Menu "Quản trị nội dung" chỉ hiện khi có quyền; vào URL khi thiếu quyền ⇒ `/403`.
- **R-CA2.** Mọi thao tác ghi của admin lên bài (sửa thông tin, thay khối/từ/quiz, xuất bản, gỡ xuất bản, lưu trữ, khôi phục) đặt `edited_at = now`, `edited_by = user` **và** chạm dòng `content.lessons` (cập nhật `updated_at`) để `xmin` đổi ⇒ phiên bản bài tăng kể cả khi chỉ sửa bảng con.
- **R-CA3. Chống ghi đè đồng thời**: mọi `PUT`/`POST` ghi bài kèm `version` (giá trị `xmin` đọc được lần trước); lệch ⇒ `409 CONCURRENCY_CONFLICT`; frontend báo "Bài đã bị sửa ở nơi khác — tải lại" và **không** tự ghi đè. Tương tự cho từ vựng (`xmin` của `content.words`).
- **R-CA4. Điều kiện xuất bản** (`422 LESSON_NOT_PUBLISHABLE`, `details.problems` là mảng chuỗi tiếng Việt): tiêu đề không rỗng; ≥ 1 khối; mọi payload hợp lệ; ≥ 1 từ; ≥ 3 câu quiz; mỗi câu 2–4 lựa chọn, `correctOptionId` thuộc lựa chọn, câu `listen_choice` có `audioText`. **Cảnh báo không chặn** (`warnings`): < 5 câu, < 30% câu nghe, > 15 từ, không có khối `dialogue`.
- **R-CA5.** Sửa quiz của bài đang `published` được phép (người đang làm dở sẽ nhận `422 QUIZ_CHANGED`, R-LS8).
- **R-CA6. Duyệt bài**: `POST .../review` đặt `review_status='reviewed'`, `reviewed_at`, `reviewed_by` (và `edited_at` theo R-CA2). Từ đó lần nạp học liệu bỏ qua bài (R-LS14).
- **R-CA7. Xoá bài**: bài `source='admin'` **và** chưa có `quiz_attempts` ⇒ xoá cứng; ngược lại ⇒ chuyển `archived` (response `{ "result": "archived" }`). Bài `archived` khôi phục được (`POST .../restore` ⇒ `draft`). **[BA-mặc định]**
- **R-CA8. `slug`**: `^[a-z0-9]+(-[a-z0-9]+)*$`, 3–64 ký tự, duy nhất (`409 SLUG_TAKEN`); đổi slug được khi bài chưa từng `published` (`published_at IS NULL`), ngược lại `422 SLUG_LOCKED` (tránh gãy link/bộ chữ `lesson:<slug>`).
- **R-CA9. Duyệt nghĩa từ**: admin sửa `meaningsVi` (1–10 mục, mỗi mục 1–200 ký tự, trim, bỏ trùng) và/hoặc `hanViet` (≤ 64, chữ thường tiếng Việt có dấu, âm tiết cách nhau một dấu cách, được để rỗng ⇒ `NULL`).
  - Nội dung `meaningsVi` đổi so với DB ⇒ `meaning_vi_source='manual'`.
  - `meaningViStatus` và `hanVietStatus` do admin chọn tường minh trong request (`machine|reviewed`, `derived|reviewed`).
  - Mọi lần lưu ⇒ `edited_at=now`, `edited_by=user`, gọi `Word.RecomputeSearchKeys()` của F6 (tính lại `search_vi`, `search_vi_plain`, `han_viet_plain`).
  - **Duyệt hàng loạt** chỉ đổi `meaning_vi_status` sang `reviewed` (không đổi nội dung), cũng đặt `edited_at`.
- **R-CA10. Bảo vệ khỏi nạp lại** (khớp R-C8 HĐG; importer F6 phải tuân — điểm khớp K8): dòng từ có `edited_at IS NOT NULL` ⇒ importer **không** ghi đè `meanings_vi`, `meaning_vi_status`, `meaning_vi_source`, `han_viet`, `han_viet_status`, `search_vi`; dòng `meaning_vi_status='reviewed'` ⇒ không đè nhóm nghĩa Việt; `han_viet_status='reviewed'` ⇒ không đè Hán Việt. Các trường khác (cấp HSK, `path_order`, nghĩa Anh…) vẫn cập nhật theo file. F10 có test chứng minh.
- **R-CA11.** Danh sách duyệt từ mặc định lọc `meaningViStatus=machine`, `hsk=1`, sắp theo `path_order` tăng dần (từ sắp học lên trước — RK4).

### 3.4 Tổng quan & chuỗi ngày (F11)

- **R-PG1. Ngày có học** = ngày lịch (cột `study_events.local_date`, đã tính theo múi giờ người dùng **lúc ghi** — R-T3) có **≥ 1 dòng `study_events`** bất kỳ `kind` với `quantity > 0`. Các `kind` hiện có: `tone_drill`, `srs_review`, `writing`, `quiz_submit`, `lesson_complete` — mọi dòng đều sinh từ hoạt động có kết quả. **[BA-mặc định — D8]**
- **R-PG2. "Hôm nay"** = `DateOnly` hiện tại theo `access.users.time_zone` của người gọi (`TimeProvider` + `TimeZoneInfo.FindSystemTimeZoneById`); múi giờ không hợp lệ ⇒ fallback `Asia/Ho_Chi_Minh` + log Warning.
- **R-PG3. Chuỗi hiện tại** (`current`): nếu hôm nay có học ⇒ đếm lùi liên tiếp từ hôm nay; nếu chưa ⇒ đếm lùi từ hôm qua (chuỗi chưa đứt tới hết ngày hôm nay); hôm qua cũng không ⇒ 0. `studiedToday` cho biết hôm nay đã có chưa. **Chuỗi dài nhất** (`longest`) tính trên toàn bộ lịch sử, luôn ≥ `current`.
- **R-PG4. Không đóng băng streak**, không bù ngày. **[BA-mặc định — D8]**
- **R-PG5. Mục tiêu ngày hiển thị riêng, không ảnh hưởng streak** **[BA-mặc định — D8]**: đạt khi **cả hai**: không còn thẻ đến hạn hôm nay (`srs.dueToday == 0` sau khi ôn) **và** đã học đủ thẻ mới trong hạn mức (`srs.newAvailableToday == 0`). Hiển thị thanh tiến độ `(reviewedToday + newIntroducedToday) / (reviewedToday + newIntroducedToday + dueToday + newAvailableToday)`; mẫu số 0 ⇒ coi như đạt.
- **R-PG6.** Lịch hoạt động 90 ngày: `[today − 89, today]`, mỗi ngày `count = SUM(quantity)` mọi `kind`; ngày không có ⇒ 0 (backend trả đủ 90 phần tử, tăng dần theo ngày).
- **R-PG7. Khối ẩn**: khối nào nguồn dữ liệu không dùng được (vd học liệu pinyin nạp lỗi ⇒ F5 trả 503) thì backend trả khối đó `null` + log Warning, frontend **ẩn** khối. Khối có dữ liệu bằng 0 (người mới) **không** ẩn mà hiện lời mời bắt đầu (CTA).
- **R-PG8. Từ vựng**: `totalInPath` = số từ có `path_order IS NOT NULL`; `introduced` = số thẻ của người dùng có `first_reviewed_at IS NOT NULL`; `mature` = thẻ `state='review'` và `stability ≥ 21`; `learning` = `introduced − mature`.
- **R-PG9. Việc hôm nay** (thứ tự hiển thị, chỉ hiện mục còn việc): (1) ôn `dueToday` thẻ đến hạn → `/on-tap`; (2) học `newAvailableToday` thẻ mới → `/on-tap`; (3) bài tiếp theo (R-LS4) → `/bai-hoc/:slug`; (4) nếu có bài hoàn thành mà còn chữ của bài chưa từng viết → "Luyện viết chữ bài …" → `/luyen-viet?tab=bai-hoc&bai=<slug>`; (5) nếu `tone.recommendedFocus` không rỗng → "Luyện thanh {n}" → `/pinyin?tab=luyen`; (6) nếu `tone.totalAnswered < 40` → "Học pinyin trước" → `/pinyin`.

---

## 4. Hiện trạng liên quan

### 4.1 Code hiện có (kiểm 17/09/2026, commit `fbbe48a`)

| Thành phần | File | Ghi chú cho F8–F11 |
|---|---|---|
| DbContext | `backend/services/chinese-backend/src/AntFarm.Chinese.Infrastructure/Persistence/ChineseDbContext.cs` | Rỗng; F3/F5/F6/F7 thêm DbSet — F9/F8/F10 thêm tiếp, cấu hình từng entity ở `Persistence/Configurations/` |
| Abstraction | `.../AntFarm.Chinese.Application/Common/Abstractions/IChineseDbContext.cs` | Thêm `DbSet<>` tương ứng |
| Khởi động | `.../AntFarm.Chinese.Api/Program.cs` | Khối `AutoMigrate` có chỗ "seeder chạy ở đây — bắt mọi exception"; F9 thêm `LessonImporter` **sau** `ContentImporter` của F6 |
| JSON | `Program.cs` | camelCase, enum `SnakeCaseLower`, bỏ `null` khi ghi (`WhenWritingNull`) ⇒ trường nullable vắng mặt trong response; frontend coi vắng = `null` |
| Lỗi | `backend/shared/AntFarm.Core/Errors/*`, `AntFarm.Security/Errors/*` | Dùng `NotFoundException`, `ConflictException(code)`, `BusinessRuleException(code, details)` |
| Package | `backend/Directory.Packages.props` | EF Core 10.0.7, Npgsql EF 10.0.0, FluentValidation 11.11.0 — **không cần package mới** cho F8–F11 |
| Dockerfile BE | `.../AntFarm.Chinese.Api/Dockerfile` | F5 đã đổi context để chứa `content/`; F9 không đổi thêm (bài học nằm trong `content/chinese/data/lessons` đã được link) |
| App FE | `frontend/apps/chinese/src/{router.tsx,layout/AppShell.tsx,pages/HomePage.tsx}` | Router `createBrowserRouter` (data router ⇒ dùng được `useBlocker` cho F10) |
| Layout | `frontend/packages/ui/src/components/layout/AppLayout.tsx` | Bottom nav tối đa 5 mục, thừa gom vào "Thêm"; `NavItem.requiredPermission` |
| Dockerfile FE | `frontend/apps/chinese/Dockerfile`, `nginx.conf` | `COPY apps/chinese` đã gồm `public/` ⇒ `hanzi-data` vào ảnh; F8 thêm `location /hanzi-data/` |
| Vite | `frontend/apps/chinese/vite.config.ts` | `public/` phục vụ ở gốc ⇒ `/hanzi-data/...` (không trùng proxy `/chinese`, `/identity`) |

### 4.2 Bảng điểm cần khớp với F4–F7 (đối chiếu trước khi code)

| # | Thứ file này dùng | Tên chốt | Nguồn | Trạng thái | Dùng ở |
|---|---|---|---|---|---|
| K1 | Bảng sổ hoạt động | `learning.study_events(id, user_id, kind varchar(32), occurred_at, local_date, quantity int NOT NULL DEFAULT 1 CHECK ≥ 0, correct int NULL CHECK ≤ quantity, ref_id)`; chỉ mục `(user_id, local_date)`, `(user_id, kind, occurred_at DESC)` | F4–F5 §5.1 | **Đã khớp** | F8, F9, F11 |
| K2 | Giá trị `kind` | `StudyEventKinds.{ToneDrill, SrsReview, Writing, QuizSubmit, LessonComplete}` + `IsKnown`; **không có CHECK ở DB** (D31 của F4–F5) ⇒ F8/F9 không phải sửa constraint | F4–F5 | **Đã khớp** | F8, F9 |
| K3 | Ngữ nghĩa `quantity` | `tone_drill`: số câu, `correct` = số đúng (R5-12); `srs_review`: 1 mỗi lượt ôn (F7 — kiểm khi F7 backend có) | F4–F5, F6–F7 | Khớp phần F5; F7 chờ | F11 |
| K4 | Dịch vụ ghi sổ | `IStudyActivityRecorder.RecordAsync(Guid userId, string kind, DateTime occurredAtUtc, int quantity, int? correct, Guid? refId, CancellationToken ct)` — Add, **không** SaveChanges; tự tính `local_date`; `kind` lạ ⇒ `ArgumentException`. F8/F9 truyền `occurredAtUtc = now` (giờ server). ⚠ Tài liệu F6–F7 (mục "Giả định từ F3–F5") ghi chữ ký cũ **thiếu** `occurredAtUtc` — agent F7 phải theo F4–F5 | F4–F5 §5.2 | **Đã khớp** (lệch ở F6–F7) | F8, F9 |
| K5 | Ngày theo múi giờ | Domain `Time/UserLocalDate.From(DateTime utc, string timeZoneId) → DateOnly` (tz sai ⇒ `Asia/Ho_Chi_Minh`, không ném) và `UserLocalDate.DayRange(DateOnly, string tz) → (FromUtc, ToUtcExclusive)` | F4–F5 | **Đã khớp** | F8, F11 |
| K6 | Bảng từ | `content.words(…, traditional, pinyin, hsk3_level, path_order, meanings_en, meanings_vi, meaning_vi_status, meaning_vi_source ∈ cvdict/machine/manual, han_viet, han_viet_plain, han_viet_status, search_vi, search_vi_plain, edited_at, edited_by uuid **không FK**, …)`, `UNIQUE(simplified, pinyin)`; Domain `Word` có `RecomputeSearchKeys()`, `IsReviewLocked`, `IsHanVietLocked` | F6–F7 §5.1.1 | **Đã khớp** | F8–F11 |
| K7 | Bảng chữ | `content.characters(id, hanzi UNIQUE, traditional_variants text[], pinyin_readings text[], han_viet text[], han_viet_by_pinyin jsonb, han_viet_status, stroke_count, radical, radical_number, …)`; `content.word_characters(word_id, position 0-based, character_id)` | F6–F7 | **Đã khớp** (dùng `traditional_variants`, không có `traditional`) | F8 |
| K8 | Importer không đè dữ liệu sửa tay | R6-11 của F6: `edited_at IS NOT NULL` hoặc `reviewed` ⇒ không đè `meanings_vi, meaning_vi_status, meaning_vi_source, han_viet, han_viet_plain, han_viet_status` | F6–F7 | **Đã khớp** R-CA10 | F10 |
| K9 | Nhật ký nạp | `content.import_runs(dataset, file_hash, importer_version, status succeeded/failed, inserted, updated, unchanged, invalid, protected, error, started_at, finished_at)` | F6–F7 | **Đã khớp** | F9 (dataset `lessons`) |
| K10 | Nạp lúc khởi động | `ContentImportRunner.RunAsync(sp, ct)` (cờ `Content:ImportOnStartup`, `Content:RootPath`); ApiTests dùng **học liệu thật** (`Content__RootPath` tuyệt đối tới `content/chinese`) + thư mục fixture `TestData/content/` cho dataset `*-test` | F4–F5, F6–F7 | **Đã khớp** | F9 |
| K11 | Thẻ SRS | `learning.srs_cards(…, source ∈ path/manual/lesson, first_reviewed_at, first_reviewed_local_date, is_suspended, created_at)`, `UNIQUE(user_id, word_id, card_type)`; thẻ lộ trình tạo lười | F6–F7 §5.1.2 | **Đã khớp** | F9, F11 |
| K12 | Thêm thẻ + ưu tiên | Hàng đợi F7 (R7-7) đã lấy **thẻ `new` có sẵn theo `created_at` trước** từ lộ trình ⇒ R-LS6 thoả. F9 cần `ISrsCardService.EnsureCardsAsync(userId, wordIds, source, ct) → int` (không SaveChanges) — **F7 chưa đặt tên**; nếu F7 để logic thêm thẻ trong service của `POST /api/srs/cards` thì F9 tách ra hàm này (không nhân đôi logic) | F6–F7 | Chờ F7 backend | F9 |
| K13 | Tóm tắt SRS | `GET /api/srs/summary` → `{ localDate, dueToday, dueNow, newAvailableToday, newIntroducedToday, reviewedToday, nextDueAt }` (R7-5); F11 gọi service tương ứng | HĐG §6.6, F6–F7 R7-5 | Chờ F7 backend (tên service) | F11 |
| K14 | Thống kê thanh | `ToneStatsService` (F5) → `{ totalAnswered, accuracy, byTone, confusions, recommendedFocus }`; học liệu lỗi ⇒ `ServiceUnavailableException` (503, `AntFarm.Core`) | F4–F5 | **Đã khớp** | F11 |
| K15 | Frontend dùng chung | `@af/ui`: `AppDialog`, `AppDrawer`, `useTabParam(allowed, defaultValue, key = 'tab')`, `useSpeech(langPrefix)` → `{ status: 'loading' / 'ready' / 'unsupported' / 'no-voice', speak(text, { rate }), cancel, speaking }` (**iOS: chỉ gọi `speak` trong handler thao tác người dùng**), `useConfirm(opts) → Promise<boolean>`, `useToast`, `AppAutocomplete`; `@af/auth`: `useAuth().hasPermission`, `RequirePermission` | F4–F5 §5.3 | **Đã khớp** | F8–F11 |
| K16 | Pinyin FE | `apps/chinese/src/lib/pinyin.ts`: `numberedToMarked(pinyin)`, `normalizeNumbered`, `sandhiHints`; component `src/components/Hanzi.tsx`, `src/components/Pinyin.tsx` (`<Pinyin value="ni3 hao3" hanzi? showSandhi?>`) | F4–F5 | **Đã khớp** | F8–F11 |
| K17 | Từ điển FE | `features/dictionary`, route `/tu-dien/:id`, `/tu-dien/chu/:hanzi` | F6–F7 | Khớp route | F9, F10 |
| K18 | Cài đặt học | `learner_settings.tts_rate` (0.50–1.20), `auto_play_audio` qua `GET /api/me/learning-settings` (không dòng ⇒ mặc định) | F6–F7 | **Đã khớp** | F8, F9 |
| K19 | Test | `TestTokenFactory`, `[DbFact]` + `af_chinese_test`, `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing` 10.10.0) | F3, F6–F7 | **Đã khớp** | F8–F11 |

Tài liệu F4–F7 còn thay đổi sau 17/09 ⇒ **tài liệu F4–F7 + code đã commit thắng** về tên; quy ước **giá trị** tab, mã lỗi, route ở file này giữ nguyên.

---

## 5. Thiết kế giải pháp

### 5.1 Database (`af_chinese`)

Quy ước chung (HĐG §5.0.3): snake_case, PK `uuid` sinh bằng `Guid.CreateVersion7()` ở ứng dụng, `timestamptz` chỉ nhận `DateTime` `Kind=Utc`, `date` ↔ `DateOnly`. Cột `jsonb` ánh xạ là **`string`** trong entity (`HasColumnType("jsonb")`), (de)serialize bằng `LessonJson` (System.Text.Json, camelCase) ở Application — không dùng `JsonDocument` trong entity, không `ToJson()` owned (một cách làm duy nhất cho mọi cột jsonb). Lý do jsonb: cấu trúc lồng theo loại khối/câu, không bao giờ lọc SQL theo trường con.

`xmin` làm concurrency token (Npgsql EF 10): khai `public uint Version { get; set; }` + `builder.Property(x => x.Version).IsRowVersion();` (Npgsql tự ánh xạ sang cột hệ thống `xmin` kiểu `xid`, migration **không** tạo cột thật — kiểm SQL sinh ra bằng `dotnet ef migrations script` trước khi commit).

#### 5.1.1 F9 — migration `F9_Lessons`

```
content.lessons
  id                  uuid PK
  slug                varchar(64)  NOT NULL UNIQUE
  title               varchar(200) NOT NULL
  topic               varchar(64)  NOT NULL            -- mã chủ đề không dấu: giao-tiep | ban-than | so-dem | gia-dinh | thoi-gian | ...
  level               varchar(16)  NOT NULL DEFAULT 'hsk1'  CHECK (level IN ('hsk1'))
  order_index         int          NOT NULL            -- không UNIQUE (admin có thể tạm trùng khi sắp lại)
  summary             varchar(1000) NOT NULL DEFAULT ''
  objectives          text[]       NOT NULL DEFAULT '{}'   -- mục tiêu bài, mỗi dòng ≤ 200
  estimated_minutes   smallint     NOT NULL DEFAULT 15 CHECK (estimated_minutes BETWEEN 1 AND 120)
  glossary            jsonb        NOT NULL DEFAULT '[]'   -- [{hanzi,pinyin,vi}] từ bổ sung, KHÔNG vào SRS
  status              varchar(16)  NOT NULL CHECK (status IN ('draft','published','archived'))
  review_status       varchar(16)  NOT NULL DEFAULT 'machine' CHECK (review_status IN ('machine','reviewed'))
  source              varchar(8)   NOT NULL CHECK (source IN ('seed','admin'))
  source_hash         char(64)     NULL                -- SHA-256 hex của file JSON (seed); admin tạo ⇒ NULL
  published_at        timestamptz  NULL                -- lần xuất bản ĐẦU TIÊN (không xoá khi gỡ)
  reviewed_at         timestamptz  NULL
  reviewed_by         uuid NULL FK → access.users(id) ON DELETE SET NULL
  created_by          uuid NULL FK → access.users(id) ON DELETE SET NULL   -- seed ⇒ NULL
  edited_at           timestamptz  NULL                -- lần ghi GẦN NHẤT của admin (F10); seed ⇒ NULL
  edited_by           uuid NULL FK → access.users(id) ON DELETE SET NULL
  created_at, updated_at timestamptz NOT NULL
  (xmin)                                               -- concurrency token
  INDEX ix_lessons_status_order (status, order_index)

content.lesson_blocks
  id                  uuid PK
  lesson_id           uuid NOT NULL FK → content.lessons(id) ON DELETE CASCADE
  order_index         smallint NOT NULL                -- 0..n-1, KHÔNG unique (thay danh sách trong 1 transaction)
  type                varchar(16) NOT NULL CHECK (type IN ('text','dialogue','grammar','tip'))
  payload             jsonb NOT NULL
  INDEX ix_lesson_blocks_lesson (lesson_id, order_index)

content.lesson_words
  lesson_id           uuid NOT NULL FK → content.lessons(id) ON DELETE CASCADE
  word_id             uuid NOT NULL FK → content.words(id) ON DELETE RESTRICT
  order_index         smallint NOT NULL
  PRIMARY KEY (lesson_id, word_id)
  INDEX ix_lesson_words_word (word_id)

content.quiz_questions
  id                  uuid PK
  lesson_id           uuid NOT NULL FK → content.lessons(id) ON DELETE CASCADE
  key                 varchar(32) NOT NULL             -- khoá ổn định: seed 'q1'..; admin tạo 'm-<8 hex>'
  order_index         smallint NOT NULL
  type                varchar(16) NOT NULL CHECK (type IN ('single_choice','listen_choice'))
  prompt              varchar(300) NOT NULL
  prompt_lang         varchar(8)  NOT NULL CHECK (prompt_lang IN ('vi','zh'))
  prompt_pinyin       varchar(300) NULL                -- chỉ khi prompt_lang='zh', số thanh
  audio_text          varchar(100) NULL                -- bắt buộc với listen_choice (chữ Hán để TTS đọc)
  options             jsonb NOT NULL                   -- [{id:'a'..'d', text, lang:'vi'|'zh'|'pinyin'}]
  correct_option_id   varchar(1) NOT NULL
  explanation         varchar(500) NOT NULL DEFAULT ''
  UNIQUE (lesson_id, key)
  INDEX ix_quiz_questions_lesson (lesson_id, order_index)

learning.lesson_progress
  user_id             uuid NOT NULL FK → access.users(id) ON DELETE CASCADE
  lesson_id           uuid NOT NULL FK → content.lessons(id) ON DELETE CASCADE
  status              varchar(16) NOT NULL CHECK (status IN ('in_progress','completed'))
  started_at          timestamptz NOT NULL
  completed_at        timestamptz NULL                 -- lần ĐẠT đầu tiên
  best_score_percent  smallint NULL CHECK (best_score_percent BETWEEN 0 AND 100)
  attempts_count      int NOT NULL DEFAULT 0
  last_attempt_at     timestamptz NULL
  updated_at          timestamptz NOT NULL
  PRIMARY KEY (user_id, lesson_id)
  INDEX ix_lesson_progress_user_status (user_id, status)

learning.quiz_attempts
  id                  uuid PK
  client_attempt_id   uuid NOT NULL UNIQUE
  user_id             uuid NOT NULL FK → access.users(id) ON DELETE CASCADE
  lesson_id           uuid NOT NULL FK → content.lessons(id) ON DELETE RESTRICT   -- R-CA7: bài có lần làm không xoá cứng
  started_at          timestamptz NULL
  submitted_at        timestamptz NOT NULL
  duration_ms         int NULL
  total               smallint NOT NULL CHECK (total > 0)
  correct             smallint NOT NULL CHECK (correct BETWEEN 0 AND total)
  score_percent       smallint NOT NULL CHECK (score_percent BETWEEN 0 AND 100)
  passed              boolean NOT NULL
  answers             jsonb NOT NULL                   -- ảnh chụp R-LS11 (định dạng dưới)
  INDEX ix_quiz_attempts_user_lesson (user_id, lesson_id, submitted_at DESC)
```

`quiz_attempts.answers`:

```json
[ { "questionId": "0192…", "key": "q1", "type": "listen_choice", "prompt": "Nghe và chọn nghĩa đúng",
    "audioText": "谢谢", "optionId": "b", "optionText": "cảm ơn", "correctOptionId": "b",
    "correctOptionText": "cảm ơn", "correct": true } ]
```

Seed: **không** gieo trong migration; `LessonImporter` nạp lúc khởi động (§5.2.1.4). Migration chạy được trên DB trống và DB đã có dữ liệu F3–F7 (bảng mới, không đổi bảng cũ — trừ K2 nếu F5 có CHECK thiếu giá trị).

#### 5.1.2 F8 — migration `F8_Writing`

```
learning.writing_attempts
  id                  uuid PK
  client_attempt_id   uuid NOT NULL UNIQUE
  user_id             uuid NOT NULL FK → access.users(id) ON DELETE CASCADE
  hanzi               varchar(4) NOT NULL FK → content.characters(hanzi) ON DELETE RESTRICT   -- khoá thay thế (HasPrincipalKey)
  mode                varchar(8) NOT NULL CHECK (mode IN ('guided','recall'))
  total_strokes       smallint NOT NULL CHECK (total_strokes BETWEEN 1 AND 64)
  total_mistakes      smallint NOT NULL CHECK (total_mistakes BETWEEN 0 AND 500)
  hints_used          smallint NOT NULL CHECK (hints_used BETWEEN 0 AND 200)
  duration_ms         int NULL CHECK (duration_ms BETWEEN 0 AND 3600000)
  is_clean            boolean NOT NULL                 -- mode='recall' AND mistakes=0 AND hints=0
  completed_at        timestamptz NOT NULL             -- giờ server lúc nhận
  local_date          date NOT NULL                    -- theo múi giờ người dùng lúc ghi
  INDEX ix_writing_attempts_user_hanzi (user_id, hanzi, completed_at DESC)
  INDEX ix_writing_attempts_user_completed (user_id, completed_at DESC)

learning.character_writing_stats
  user_id             uuid NOT NULL FK → access.users(id) ON DELETE CASCADE
  hanzi               varchar(4) NOT NULL FK → content.characters(hanzi) ON DELETE RESTRICT
  attempts            int NOT NULL DEFAULT 0
  guided_attempts     int NOT NULL DEFAULT 0
  recall_attempts     int NOT NULL DEFAULT 0
  last_mode           varchar(8) NOT NULL
  last_mistakes       smallint NOT NULL
  last_hints          smallint NOT NULL
  best_recall_mistakes smallint NULL                   -- NULL khi chưa recall
  clean_recall_days   smallint NOT NULL DEFAULT 0      -- R-W5
  last_clean_recall_date date NULL
  first_practiced_at  timestamptz NOT NULL
  last_practiced_at   timestamptz NOT NULL
  PRIMARY KEY (user_id, hanzi)
  INDEX ix_char_writing_stats_user_last (user_id, last_practiced_at)
```

`content.characters.hanzi` đã UNIQUE (F6) ⇒ đủ điều kiện làm khoá thay thế cho FK. Nếu F6 không khai unique constraint mà chỉ unique index, EF `HasAlternateKey(c => c.Hanzi)` sẽ sinh constraint — kiểm migration không tạo trùng.

Cập nhật `character_writing_stats` trong cùng transaction với `writing_attempts`, khoá dòng: `SELECT … FROM learning.character_writing_stats WHERE user_id=@u AND hanzi=@h FOR UPDATE` (qua `FromSqlInterpolated`); không có ⇒ `INSERT … ON CONFLICT (user_id, hanzi) DO NOTHING` rồi đọc lại `FOR UPDATE` (hai lần gửi đồng thời cùng chữ không làm mất số đếm).

#### 5.1.3 F10 — migration `F10_ContentAdmin`

```
content.words: thêm concurrency token xmin (IsRowVersion — không tạo cột thật)
(edited_by đã có từ F6, KHÔNG FK — giữ nguyên)
INDEX ix_words_review (meaning_vi_status, hsk3_level, path_order)   -- màn duyệt nghĩa (R-CA11)
```

Không bảng mới, không cột mới (F6 đã có `edited_at`, `edited_by`). Migration F10 chỉ gồm chỉ mục (vẫn đặt tên `F10_ContentAdmin`). Nếu sau khi khai `IsRowVersion` mà EF không sinh thay đổi nào ngoài chỉ mục thì vẫn giữ migration (để `PendingModelChangesWarning` không nổ).

#### 5.1.4 F11 — không migration

Truy vấn dùng chỉ mục `study_events (user_id, local_date)` (K1), `srs_cards (user_id, state, due_at)`, `lesson_progress (user_id, status)`, `character_writing_stats (user_id, …)`. Nếu `EXPLAIN` trên dữ liệu 2 năm (≈ 20.000 dòng sự kiện/người) cho thấy quét tuần tự > 50 ms thì mới cân nhắc thêm chỉ mục — ghi vào báo cáo, **không** tự thêm migration ở F11.

### 5.2 Backend (`backend/services/chinese-backend`)

Quy ước: DDD 4 lớp; Application chỉ phụ thuộc `IChineseDbContext`; controller mỏng; validator FluentValidation; lỗi nghiệp vụ ném `AppException` con (mã ở §6.0); `TimeProvider` cho mọi "bây giờ"; mọi ghi nghiệp vụ + `study_events` trong **một** `SaveChangesAsync` (hoặc một `BeginTransactionAsync` khi có `FOR UPDATE`).

#### 5.2.1 F9 — bài học + quiz

##### 5.2.1.1 Domain (`AntFarm.Chinese.Domain`)

| File | Nội dung |
|---|---|
| `Lessons/Lesson.cs` | Entity (cột §5.1.1). Hằng `LessonStatuses { Draft="draft", Published="published", Archived="archived" }`, `ReviewStatuses { Machine, Reviewed }`, `LessonSources { Seed, Admin }`. Phương thức `TouchEdited(Guid userId, DateTime nowUtc)` (đặt `EditedAt`, `EditedBy`, `UpdatedAt`) — F10 dùng |
| `Lessons/LessonBlock.cs`, `Lessons/LessonWord.cs`, `Lessons/QuizQuestion.cs` | Entity; `LessonBlockTypes { Text, Dialogue, Grammar, Tip }`, `QuizQuestionTypes { SingleChoice="single_choice", ListenChoice="listen_choice" }` |
| `Lessons/Payloads.cs` | record: `TextPayload(IReadOnlyList<string> Paragraphs)`, `DialoguePayload(string? Title, IReadOnlyList<DialogueLine> Lines)`, `DialogueLine(string Speaker, string Hanzi, string Pinyin, string Vi)`, `GrammarPayload(string Title, string? Pattern, string Explanation, IReadOnlyList<GrammarExample> Examples)`, `GrammarExample(string Hanzi, string Pinyin, string Vi, string? Note)`, `TipPayload(string Text, string? Variant)`, `QuizOption(string Id, string Text, string Lang)`, `GlossaryItem(string Hanzi, string Pinyin, string Vi)` |
| `Learning/LessonProgress.cs` | Entity; `ApplyAttempt(int scorePercent, bool passed, DateTime nowUtc) → bool firstCompletion` (tăng `AttemptsCount`, cập nhật `BestScorePercent = max`, `LastAttemptAt`; lần đạt đầu ⇒ `Status=completed`, `CompletedAt`, trả `true`) |
| `Learning/QuizAttempt.cs` | Entity |
| `Learning/QuizGrader.cs` | Hàm thuần `static QuizGrade Grade(IReadOnlyList<QuizQuestion> questions, IReadOnlyDictionary<Guid,string> answers)` ⇒ `QuizGrade(int Total, int Correct, int ScorePercent, bool Passed, IReadOnlyList<GradedAnswer> Items)`; `Passed = Correct*100 >= 80*Total` (R-LS3); ném `ArgumentException` nếu tập câu lệch (Application đổi thành `QUIZ_CHANGED` trước khi gọi) |
| (dùng lại) `Learning/StudyEventKinds.cs` của F5 | `QuizSubmit`, `LessonComplete`, `Writing` đã có (K2) |

##### 5.2.1.2 Application (`AntFarm.Chinese.Application/Lessons/`)

| File | Nội dung |
|---|---|
| `LessonJson.cs` | `JsonSerializerOptions` camelCase dùng chung; `Serialize/Deserialize<T>` payload, options, glossary, answers; `ToElement(string json) → JsonElement` (Clone) cho DTO |
| `LessonContentValidator.cs` | **Validator dùng chung** cho importer (F9) và admin (F10): `ValidateBlock(type, JsonElement payload) → IReadOnlyList<ValidationProblem(path, message)>`, `ValidateQuestion(...)`, `ValidateGlossary(...)`. Luật §5.4.3. Pinyin kiểm bằng `PinyinText` (F5; K16 phía BE) — có dấu câu thì bỏ dấu câu trước khi kiểm; **số âm tiết = số chữ Hán** (đếm ký tự khối CJK U+4E00–U+9FFF, U+3400–U+4DBF), sai ⇒ lỗi |
| `LessonPublishRules.cs` | `Check(Lesson, blocks, words, questions) → (IReadOnlyList<string> problems, IReadOnlyList<string> warnings)` theo R-CA4 (F10 dùng; F9 importer dùng cho bài `published` từ file — lỗi ⇒ bỏ qua bài, R-LS15) |
| `LessonQueryService.cs` | `ListPublishedAsync(userId)`, `GetPublishedBySlugAsync(userId, slug)` (404 nếu không `published`), `ListAttemptsAsync(userId, lessonId, limit=10)` |
| `LessonProgressService.cs` | `StartAsync(userId, lessonId)` (R-LS12; bài không `published` ⇒ 404) |
| `QuizSubmissionService.cs` | `SubmitAsync(userId, lessonId, SubmitQuizRequest)` — luồng dưới |
| `Dtos/*.cs` | record theo §6.1 |
| `Validators/SubmitQuizRequestValidator.cs` | `clientAttemptId` khác `Guid.Empty`; `answers` 1–50 mục, `questionId` không trùng, `optionId` khớp `^[a-d]$`; `startedAt` nếu có: `≤ now + 5 phút` và `≥ now − 24 giờ` |

Luồng `QuizSubmissionService.SubmitAsync`:

```
1. Có quiz_attempts.client_attempt_id = req.ClientAttemptId (của CHÍNH user)? ⇒ dựng kết quả từ bản ghi, trả (isReplay=true → HTTP 200)
   (trùng id nhưng user khác ⇒ 409 CONFLICT code DUPLICATE_ATTEMPT_ID)
2. lesson = published theo id, không có ⇒ 404
3. questions = quiz_questions của bài (order_index); rỗng ⇒ 422 QUIZ_EMPTY
4. tập questionId trong req ≠ tập id câu hiện có ⇒ 422 QUIZ_CHANGED
5. optionId không thuộc options của câu ⇒ 400 VALIDATION (details["answers[i].optionId"])
6. grade = QuizGrader.Grade(...)
7. BEGIN TRANSACTION
   progress = lesson_progress (user, lesson) FOR UPDATE; chưa có ⇒ tạo in_progress, started_at = req.StartedAt ?? now
   firstCompletion = progress.ApplyAttempt(grade.ScorePercent, grade.Passed, now)
   attempt = new QuizAttempt(… answers = ảnh chụp R-LS11, duration_ms = startedAt? now−startedAt : null)
   recorder.RecordAsync(user, QuizSubmit, now, grade.Total, grade.Correct, attempt.Id)          (K4)
   srsAdded = 0
   nếu firstCompletion:
       wordIds = lesson_words của bài
       srsAdded = srsCardService.EnsureCardsAsync(user, wordIds, "lesson")                       (K12)
       recorder.RecordAsync(user, LessonComplete, now, 1, null, lesson.Id)
   SaveChanges; COMMIT
   bắt DbUpdateException do UNIQUE client_attempt_id (hai request trùng song song) ⇒ rollback, quay lại bước 1
8. trả { attemptId, total, correct, scorePercent, passed, firstCompletion, srsCardsAdded, results[], progress }  (HTTP 201)
```

##### 5.2.1.3 Infrastructure

| File | Nội dung |
|---|---|
| `Persistence/Configurations/LessonConfiguration.cs` … `QuizAttemptConfiguration.cs` | Cấu hình §5.1.1 (schema, CHECK bằng `ToTable(t => t.HasCheckConstraint(...))`, jsonb, `IsRowVersion`) |
| `Persistence/ChineseDbContext.cs` | `DbSet<Lesson> Lessons`, `LessonBlocks`, `LessonWords`, `QuizQuestions`, `LessonProgress`, `QuizAttempts` (+ khai trong `IChineseDbContext`) |
| `Content/LessonImporter.cs` | §5.2.1.4 |
| `Content/LessonFileModel.cs` | Mô hình file JSON (§5.4.2) |
| `Persistence/Migrations/<ts>_F9_Lessons.cs` | Sinh bằng `dotnet ef migrations add F9_Lessons …` |

##### 5.2.1.4 `LessonImporter` (Infrastructure/Content)

- Gọi ở cuối `ContentImportRunner.RunAsync` (F6) — sau `characters` và `hsk-words`, cùng cờ `Content:ImportOnStartup` — bằng `LessonImporter.ImportDirectoryAsync(string dir, string dataset, CancellationToken)`; bọc `try/catch` toàn phần: lỗi ⇒ log Error, **không ném**. Hằng `LessonImporter.Version = 1` ghi vào `import_runs.importer_version`.
- Đọc `Path.Combine(Content:RootPath, "data/lessons")`, file khớp `^\d{2}-[a-z0-9-]+\.json$`, sắp theo tên. Thư mục không có ⇒ log Warning, kết thúc.
- Mỗi file: đọc byte → `hash = SHA-256 hex` → parse (lỗi ⇒ log Error `{file}`, bỏ qua) → kiểm `slug` = phần sau `NN-` của tên file và `orderIndex` = `NN` (lệch ⇒ bỏ qua + Error) → validator + `LessonPublishRules` nếu `status` là `published` → tra từ theo `(simplified, pinyin)` (pinyin chuẩn hoá bằng `PinyinText.NormalizeNumbered` trước khi so) — thiếu ⇒ bỏ qua cả bài (R-LS15).
- Áp bảng R-LS14. Mỗi bài một transaction riêng (bài hỏng không kéo bài khác).
- Cập nhật tại chỗ: **diff** chứ không xoá-chèn cho `lesson_words` (xoá từ không còn, cập nhật `order_index`, thêm từ mới) để tránh xung đột khoá trong cùng `SaveChanges`; `lesson_blocks` xoá hết rồi thêm (khoá là uuid mới, không xung đột); `quiz_questions` upsert theo `key` (giữ nguyên `id` câu cũ), xoá câu có `key` không còn.
- Ghi `content.import_runs(dataset='lessons', file_hash=<SHA-256 của chuỗi nối các hash file theo tên>, importer_version, status, inserted, updated, unchanged, invalid, protected, error, started_at, finished_at)` — `protected` = số bài bỏ qua vì đã sửa/duyệt, `invalid` = số bài lỗi; lần `succeeded` gần nhất cùng hash **và** cùng `importer_version` ⇒ bỏ qua toàn bộ, không ghi dòng mới — trừ khi số bài `source='seed'` trong DB nhỏ hơn số file thì chạy đủ.
- Log Information cuối: `Nạp bài học: thêm {i}, cập nhật {u}, bỏ qua {s}, lỗi {e}`.

##### 5.2.1.5 Api

`Features/Lessons/LessonsController.cs` — `[Route("api/lessons")]`, `[RequirePermission("study.use")]`: `GET ""`, `GET "{slug}"`, `POST "{id:guid}/start"`, `POST "{id:guid}/quiz-attempts"`, `GET "{id:guid}/quiz-attempts"`. Lấy `userId` bằng `User.GetAccountId()` (F2). Chi tiết §6.1.

##### 5.2.1.6 Test F9

- **UnitTests** (`tests/AntFarm.Chinese.UnitTests/Lessons/`):
  - `QuizGraderTests`: 4/5 đạt 80; 7/9 = 77 không đạt; 8/10 đạt; 0/5 = 0; 5/5 = 100; tập câu lệch ném.
  - `LessonProgressTests`: lần đạt đầu trả `true`, lần đạt sau `false`, `best` là max, trượt sau đạt không hạ `status`.
  - `LessonContentValidatorTests` (≥ 15 ca): `text` rỗng; `dialogue` pinyin thiếu âm tiết (`你好` + `ni3`) lỗi; pinyin có dấu câu `Ni3 hao3!` hợp lệ; `ü` viết `lv4` hợp lệ; `grammar` không ví dụ lỗi; `tip.variant` lạ lỗi; câu `listen_choice` thiếu `audioText` lỗi; `correctOptionId` không thuộc lựa chọn lỗi; id lựa chọn trùng lỗi; lựa chọn `lang='pinyin'` sai định dạng lỗi; cú pháp nội dòng `[[你好|ni3 hao3]]` hợp lệ, `[[你好|ni3]]` lỗi; `[[abc|a1]]` lỗi (không phải chữ Hán).
  - `LessonPublishRulesTests`: thiếu quiz ⇒ problem; 3 câu đều `single_choice` ⇒ chỉ warning.
- **ApiTests** `[DbFact]` (`tests/AntFarm.Chinese.ApiTests/Lessons/`). Factory dùng học liệu thật (K10) ⇒ có 5 bài seed + kho từ thật. Bài test đặt ở `TestData/content/lessons/` (copy ra output): `01-test-bai.json` (dùng từ thật 你/好/谢谢…), `02-test-nhap.json` (`status: draft`), `03-test-loi.json` (có từ không tồn tại); test gọi `LessonImporter.ImportDirectoryAsync(<thư mục tạm>, "lessons-test")` trực tiếp:
  0. Học liệu thật: sau khởi động có đúng 5 bài `source='seed'`, slug như §5.4.6; khởi động thêm 2 lần không đổi số dòng.
  1. Nạp thư mục test ⇒ có bài `test-bai`, không có `test-loi`, `import_runs.invalid = 1`; nạp lại ⇒ số dòng lessons/blocks/questions **không đổi**, id câu hỏi không đổi.
  2. Sửa file test (fixture ghi bản sửa vào thư mục tạm) ⇒ bài chưa sửa tay được cập nhật; đặt `edited_at` trong DB rồi nạp lại ⇒ không đổi.
  3. Learner `GET /api/lessons` chỉ có bài `published`; `GET /api/lessons/test-nhap` ⇒ 404; JSON chi tiết **không** chứa chuỗi `correctOptionId`/`explanation` (kiểm trên raw body).
  4. Nộp 5/5 ⇒ 201, `passed=true`, `firstCompletion=true`, `srsCardsAdded = số từ của bài`; `learning.srs_cards` có thẻ `source='lesson'`; `study_events` có 1 `quiz_submit` (`quantity=5, correct=5`) + 1 `lesson_complete`.
  5. Nộp lại (clientAttemptId mới) 5/5 ⇒ `firstCompletion=false`, `srsCardsAdded=0`, không thêm `lesson_complete`.
  6. Gửi lại đúng `clientAttemptId` ở bước 4 ⇒ 200, cùng `attemptId`, số dòng `quiz_attempts` không đổi.
  7. Thiếu 1 câu ⇒ 422 `QUIZ_CHANGED`; `optionId='z'` ⇒ 400.
  8. Người dùng đã có thẻ `manual` cho 1 từ của bài ⇒ sau hoàn thành thẻ đó vẫn `manual`, `srsCardsAdded` = số từ − 1.
  9. `FakeTimeProvider` = `2026-09-17T17:30:00Z` (00:30 ngày 18/09 giờ VN), user `Asia/Ho_Chi_Minh` ⇒ `study_events.local_date = 2026-09-18`.
  10. Token không có `study.use` (user 0 vai trò) ⇒ 403.

#### 5.2.2 F8 — luyện viết

| Lớp | File | Nội dung |
|---|---|---|
| Domain | `Learning/WritingAttempt.cs`, `Learning/CharacterWritingStats.cs` | `CharacterWritingStats.Apply(WritingAttempt a, DateOnly localDate)`: tăng đếm; `LastMode/LastMistakes/LastHints`; recall ⇒ `BestRecallMistakes = min`; `a.IsClean` và `localDate != LastCleanRecallDate` ⇒ `CleanRecallDays++`, `LastCleanRecallDate = localDate`; `MasteryStatus` (tính) theo R-W5; `IsWeak` theo R-W5 |
| Domain | `Learning/WritingModes.cs`, `Learning/MasteryStatuses.cs` | `guided|recall`; `new|practicing|mastered` |
| Application | `Writing/WritingService.cs` | `RecordAttemptAsync(userId, req)` (luồng dưới), `GetSummaryAsync(userId)` |
| Application | `Writing/WritingCharacterQueryService.cs` | `ListAsync(userId, set, page, pageSize)` theo R-W6 (set `lesson:<slug>` bài không `published` ⇒ 404; set lạ ⇒ 400); `GetAsync(userId, hanzi)` (không có ⇒ 404) |
| Application | `Writing/Validators/RecordWritingAttemptValidator.cs` | `hanzi`: đúng **một** code point thuộc khối CJK (dùng `Rune`), `mode ∈ {guided, recall}`, biên số §5.1.2, `clientAttemptId` khác rỗng |
| Infrastructure | `Persistence/Configurations/WritingAttemptConfiguration.cs`, `CharacterWritingStatsConfiguration.cs`; migration `F8_Writing` | §5.1.2 |
| Api | `Features/Writing/WritingController.cs` | `[Route("api/writing")]`, `study.use`: `POST attempts`, `GET characters`, `GET characters/{hanzi}`, `GET summary` |

Luồng `RecordAttemptAsync`:

```
1. trùng client_attempt_id của user ⇒ trả kết quả cũ (200); trùng của user khác ⇒ 409 DUPLICATE_ATTEMPT_ID
2. content.characters có hanzi? không ⇒ 422 UNKNOWN_CHARACTER
3. now = TimeProvider (UTC); localDate = UserLocalDate.From(now, users.time_zone) (K5)
4. BEGIN; stats = FOR UPDATE (tạo nếu chưa có, §5.1.2); attempt = new(... is_clean, local_date)
   stats.Apply(attempt, localDate)
   recorder.RecordAsync(user, Writing, now, 1, mode==recall ? (isClean?1:0) : null, attempt.Id)
   SaveChanges; COMMIT (UNIQUE client_attempt_id va chạm ⇒ quay lại bước 1)
5. 201 { attemptId, stats }
```

`hsk1` set (SQL gợi ý, viết bằng LINQ tương đương):

```sql
SELECT c.hanzi, MIN(w.path_order) AS first_order
FROM content.characters c
JOIN content.word_characters wc ON wc.character_id = c.id
JOIN content.words w ON w.id = wc.word_id
WHERE w.hsk3_level = 1
GROUP BY c.hanzi
ORDER BY first_order NULLS LAST, c.hanzi
```

Test F8:
- UnitTests `CharacterWritingStatsTests`: 2 lần recall sạch cùng ngày ⇒ `clean_recall_days=1`, `practicing`; ngày khác ⇒ 2, `mastered`; recall có gợi ý không tính sạch; guided sạch không tính; `IsWeak` khi `last_mistakes=2`; `best_recall_mistakes` là min.
- UnitTests validator: `hanzi="爱"` hợp lệ; `"爱我"` lỗi; `"a"` lỗi; chữ mở rộng B (surrogate pair) đúng một code point hợp lệ nếu thuộc khối CJK Ext B (U+20000–U+2A6DF).
- ApiTests `[DbFact]`: ghi 1 lần ⇒ 201 + `study_events(writing)`; gửi lại cùng id ⇒ 200, không thêm dòng; `hanzi` không có trong `characters` ⇒ 422 `UNKNOWN_CHARACTER`; `GET characters?set=hsk1` thứ tự theo `path_order`; `set=lesson:test-bai` ra đúng chữ của bài, không trùng; `set=weak` chỉ có chữ thoả R-W5; `set=abc` ⇒ 400; hai lần recall sạch ở 23:50 và 00:10 giờ VN (FakeTimeProvider) ⇒ `mastered`.

#### 5.2.3 F10 — quản trị nội dung

| Lớp | File | Nội dung |
|---|---|---|
| Application | `Admin/Content/LessonAdminService.cs` | `ListAsync(query)`, `GetAsync(id)`, `CreateAsync(req, userId)`, `UpdateMetaAsync(id, req, userId)`, `ReplaceBlocksAsync`, `ReplaceWordsAsync`, `ReplaceQuizAsync`, `PublishAsync`, `UnpublishAsync`, `ReviewAsync`, `DeleteAsync`, `RestoreAsync` |
| Application | `Admin/Content/WordReviewService.cs` | `ListAsync(query)`, `UpdateAsync(id, req, userId)`, `BulkReviewAsync(req, userId)` |
| Application | `Admin/Content/Validators/*` | Một validator mỗi request (§6.3); payload khối/câu dùng `LessonContentValidator` (F9), lỗi trả `400 VALIDATION` với `details` theo đường dẫn `blocks[2].payload.lines[0].pinyin` |
| Domain | `Content/Word.cs` (F6) | F10 thêm `Version` (xmin), phương thức `ApplyReview(meaningsVi, meaningViStatus, hanViet, hanVietStatus, userId, nowUtc)` (R-CA9) |
| Api | `Features/Admin/AdminLessonsController.cs`, `Features/Admin/AdminWordsController.cs` | `[RequirePermission("content.manage")]` ở mức class |

Chi tiết cài đặt:

- **Concurrency** (R-CA3): service nạp bài, gán `db.Entry(lesson).Property(x => x.Version).OriginalValue = req.Version`, thao tác, `lesson.TouchEdited(...)`, `SaveChanges` ⇒ `DbUpdateConcurrencyException` ⇒ `ConflictException("CONCURRENCY_CONFLICT", "Bài đã bị sửa ở nơi khác — hãy tải lại.")`. Response mọi lệnh ghi trả **toàn bộ bài** (như `GET`) kèm `version` mới (đọc lại sau `SaveChanges`).
- **Thay từ** (`ReplaceWordsAsync`): `wordIds` 0–30 mục, không trùng; id không tồn tại ⇒ `422 UNKNOWN_WORD` (`details.wordIds`); cập nhật theo diff như importer.
- **Thay quiz**: mục có `id` phải thuộc bài (không ⇒ 422 `UNKNOWN_QUESTION`) ⇒ cập nhật, giữ `key`; mục không `id` ⇒ tạo, `key = "m-" + 8 hex ngẫu nhiên`; câu cũ không có trong danh sách ⇒ xoá. `order_index` = vị trí trong mảng. Tối đa 30 câu.
- **Xuất bản**: `LessonPublishRules` có problem ⇒ `422 LESSON_NOT_PUBLISHABLE` (`details.problems`); đạt ⇒ `status=published`, `published_at ??= now`; trả kèm `warnings`. Bài `archived` không xuất bản trực tiếp ⇒ `422 LESSON_ARCHIVED` (phải restore trước). Gỡ xuất bản ⇒ `draft`.
- **Tạo bài**: `status=draft`, `source='admin'`, `review_status='machine'`, `created_by=user`, `edited_at=now`. `orderIndex` bỏ trống ⇒ `max+1`.
- **Xoá**: R-CA7. **Khôi phục**: `archived` ⇒ `draft`; trạng thái khác ⇒ `422 LESSON_NOT_ARCHIVED`.
- **Duyệt bài**: R-CA6; bài `draft` cũng duyệt được.
- **Danh sách admin** (`GET /api/admin/lessons`): mọi trạng thái; mặc định **ẩn** `archived` trừ khi `status=archived`; `q` khớp không dấu trên `title`/`slug` (`VietnameseText.RemoveDiacritics` cả hai phía; số bài nhỏ ⇒ lọc trong bộ nhớ sau khi tải tiêu đề là chấp nhận được); sắp `order_index`, `title`.
- **Duyệt từ**: `UpdateAsync` so `version`; `meaningsVi` chuẩn hoá (trim, bỏ rỗng, bỏ trùng giữ thứ tự) rồi so với DB để quyết `meaning_vi_source` (R-CA9); gọi `RecomputeSearchKeys()` (K6). `BulkReviewAsync`: 1–100 mục `{id, version}`; mục lệch version/không tồn tại đưa vào `conflicts`, mục còn lại vẫn lưu (mỗi mục một `SaveChanges` trong một transaction bọc ngoài là không được vì một xung đột huỷ cả lô ⇒ **xử lý từng mục**, chấp nhận không nguyên tử).
- **Danh sách từ**: lọc `meaningViStatus`, `hanVietStatus`, `hsk` (mặc định 1), `q` (dùng lại logic tìm của `DictionaryService` F6 nếu tách được; không thì khớp `simplified`/`pinyin_search`/`search_vi`), phân trang HĐG; sắp `path_order NULLS LAST`, `simplified`.

Test F10:
- UnitTests `WordReviewTests` (`ApplyReview`: đổi nghĩa ⇒ `manual`; giữ nghĩa, chỉ đổi status ⇒ giữ source; `hanViet=""` ⇒ null).
- ApiTests `[DbFact]`:
  1. Learner gọi mọi route admin ⇒ 403 (kiểm ít nhất `GET /api/admin/lessons`, `PUT /api/admin/words/{id}`).
  2. Tạo bài (201, `draft`) → thay khối/từ/quiz → xuất bản (200, có `warnings` vì < 5 câu) → learner thấy bài.
  3. Xuất bản bài chỉ có khối ⇒ 422 `LESSON_NOT_PUBLISHABLE`, `details.problems` không rỗng.
  4. Hai lần `PUT meta` cùng `version` ⇒ lần 2 409 `CONCURRENCY_CONFLICT`; `PUT blocks` làm `version` đổi.
  5. Slug trùng ⇒ 409 `SLUG_TAKEN`; đổi slug bài đã từng xuất bản ⇒ 422 `SLUG_LOCKED`.
  6. Xoá bài admin chưa có lần làm ⇒ 204 và mất khỏi DB; xoá bài seed ⇒ 200 `archived`; khởi động lại ⇒ importer **không** tạo lại / không đổi trạng thái.
  7. Sửa bài seed (`PUT meta`) rồi đổi nội dung file + khởi động lại ⇒ tiêu đề giữ bản admin. `POST review` rồi đổi file ⇒ giữ nguyên.
  8. Sửa nghĩa từ ⇒ `meaning_vi_source='manual'`, `edited_at` có; chạy lại `ContentImporter` với file có nghĩa khác (hash khác) ⇒ nghĩa giữ bản admin, `hsk3_level` vẫn cập nhật theo file (R-CA10).
  9. Tìm từ điển bằng nghĩa mới (không dấu) ra từ vừa sửa (`search_vi`/`search_vi_plain` đã tính lại).
  10. Bulk review 3 mục, 1 mục sai version ⇒ `updated=2`, `conflicts=[id]`.
  11. Admin sửa quiz của bài đang làm dở ⇒ learner nộp với tập câu cũ ⇒ 422 `QUIZ_CHANGED`.

#### 5.2.4 F11 — tổng quan

| Lớp | File | Nội dung |
|---|---|---|
| Domain | `Progress/StreakCalculator.cs` | `static StreakResult Calculate(IReadOnlyCollection<DateOnly> studiedDates, DateOnly today)` ⇒ `StreakResult(int Current, int Longest, bool StudiedToday)`; ngày tương lai (> today, do đổi múi giờ) bị bỏ qua khi tính `current` nhưng vẫn tính vào `longest` |
| Application | `Progress/ProgressOverviewService.cs` | `GetAsync(userId, ct)` — gọi tuần tự (một DbContext, không song song): streak, today, srs (K13), vocabulary, lessons, writing, tone (K14), activity. Mỗi khối ngoài streak/today/activity bọc `try/catch (ServiceUnavailableException)` ⇒ `null` + log Warning (R-PG7). Lỗi khác ném bình thường |
| Application | `Progress/Dtos/ProgressOverviewDto.cs` | §6.4 |
| Api | `Features/Progress/ProgressController.cs` | `[Route("api/progress")]`, `study.use`: `GET overview` |

Truy vấn:
- Streak: `SELECT DISTINCT local_date FROM learning.study_events WHERE user_id=@u AND quantity > 0`.
- Today: `SUM(quantity)` theo `kind` với `local_date = today`; `quizzes = COUNT(*) kind='quiz_submit'`; `lessonsCompleted = COUNT(*) kind='lesson_complete'`; `writingChars = SUM(quantity) kind='writing'`; `newCards` = `srs.newIntroducedToday` (K13).
- Activity: `GROUP BY local_date` trong `[today−89, today]`, lấp 0.
- Vocabulary: R-PG8 (một truy vấn `COUNT(*) FILTER (WHERE …)` trên `srs_cards`).
- Lessons: `published` = số bài published; `completed` = progress completed **của bài đang published**; `inProgress`; `next` theo R-LS4; `lastCompleted` = bài completed gần nhất `{slug,title,completedAt, unpracticedChars}` với `unpracticedChars` = số chữ của bài (R-W6) chưa có dòng `character_writing_stats` (R-PG9 mục 4).
- Writing: `practicedChars` = số dòng stats; `masteredChars` = `clean_recall_days ≥ 2`; `weakChars` (R-W5); `totalChars` = kích thước bộ `hsk1`.
- Tone: từ K14 — `totalAnswered`, `accuracy`, `recommendedFocus`.

Test F11:
- UnitTests `StreakCalculatorTests` (≥ 10 ca): rỗng ⇒ (0,0,false); chỉ hôm nay ⇒ (1,1,true); chỉ hôm qua ⇒ (1,1,false); hôm kia ⇒ (0,1,false); 3 ngày liên tiếp tới hôm qua + hôm nay ⇒ 4; chuỗi cũ 10 ngày, chuỗi hiện tại 2 ⇒ (2,10); ngày trùng lặp trong input không đếm đôi; ngày tương lai bị bỏ qua cho current; qua ranh giới tháng/năm (31/12 → 01/01); năm nhuận 28/02 → 29/02 → 01/03.
- ApiTests `[DbFact]` (user `Asia/Ho_Chi_Minh`, chèn `study_events` trực tiếp):
  1. Sự kiện 16/09 và 17/09 (local_date), FakeTimeProvider `2026-09-17T16:59:00Z` (23:59 VN 17/09) ⇒ `localDate=2026-09-17`, `current=2`, `studiedToday=true`.
  2. Cùng dữ liệu, thời gian `2026-09-17T17:01:00Z` (00:01 VN 18/09) ⇒ `localDate=2026-09-18`, `current=2`, `studiedToday=false`. (Nếu cắt ngày theo UTC thì sẽ ra 17/09 — ca này bắt lỗi đó.)
  3. `activity` đúng 90 phần tử, phần tử cuối là hôm nay, tổng khớp dữ liệu chèn.
  4. User mới không có dữ liệu ⇒ 200, mọi số 0, `lessons.next` là bài đầu, khối không `null`.
  5. Học liệu pinyin không dùng được (factory trỏ `Content:RootPath` sang thư mục thiếu pinyin) ⇒ `tone` vắng/`null`, các khối khác vẫn có, HTTP 200.
  6. Không có `study.use` ⇒ 403.

### 5.3 Frontend (`frontend/apps/chinese`) — agent `frontend-implement`, **model Fable**

Quy ước chung cho cả 4 feature:
- Cấu trúc `src/features/<module>/{api.ts,hooks.ts,types.ts,pages/,components/,lib/}`; TanStack Query, khoá query dạng mảng (`['lessons']`, `['lessons', slug]`, `['writing','characters',set,page]`, `['progress','overview']`, `['admin','lessons',…]`).
- Mọi phần tử chứa chữ Hán dùng `LangText lang="zh-CN"` (`@af/ui`); pinyin hiển thị bằng `numberedToMarked` / component `Pinyin` (K16) — **không** hiển thị dạng số cho học viên (admin nhập dạng số, xem trước dạng dấu).
- MUI v9: không shorthand prop (`<Stack sx={{ alignItems: 'center' }}>`), `slotProps` thay `*Props`; `AppDialog`/`AppDrawer` thay `Dialog`/`Drawer`; Autocomplete trải `params.slotProps` trước khi ghi đè; tab cấp trang bằng `useTabParam`; không `uuid` (dùng `crypto.randomUUID()`).
- **Mobile-first 375px**: nút chính cao ≥ 44px, không cuộn ngang ngoài vùng chủ ý, nội dung bài một cột; nút hành động chính của màn học đặt ở thanh dưới dính (`position: sticky; bottom: 0`, chừa chỗ bottom nav của `AppLayout`).
- Sau thao tác ghi thành công: invalidate `['progress','overview']` (F11), và query liên quan (`['lessons']`, `['srs','summary']`, `['writing', …]`).
- Lỗi ghi hiển thị tại chỗ bằng `parseApiError` (`@af/utils`) — `Alert` hoặc toast; lỗi GET 401/403/404 để `createApiClient` điều hướng.
- Nav (`layout/AppShell.tsx`) — thứ tự cuối cùng sau F11: **Trang chủ, Ôn tập, Bài học, Luyện viết, Pinyin, Từ điển, Hồ sơ, Quản trị nội dung (`content.manage`), Người dùng (`users.manage`)** (bottom nav hiện 4 mục đầu + "Thêm"). Mỗi feature chỉ thêm mục của mình vào đúng vị trí.

#### 5.3.1 F9 — bài học

Route (trong `router.tsx`, bọc `RequireAuth` như các route học khác):

| Route | Trang | Ghi chú |
|---|---|---|
| `/bai-hoc` | `LessonListPage` | Danh sách thẻ bài theo `orderIndex` |
| `/bai-hoc/:slug?tab=noi-dung\|tu-vung\|quiz` | `LessonDetailPage` | `useTabParam`, mặc định `noi-dung` |

File:

```
src/features/lessons/
  api.ts            getLessons(), getLesson(slug), startLesson(id), submitQuiz(id, body), getQuizAttempts(id)
  hooks.ts          useLessons, useLesson, useStartLesson, useSubmitQuiz, useQuizAttempts
  types.ts          theo §6.1 (LessonSummary, LessonDetail, LessonBlock (union theo type), QuizQuestion, QuizResult…)
  lib/inlineZh.ts           parseInlineZh(text) → Array<{kind:'text',text}|{kind:'zh',hanzi,pinyin}> (cú pháp §5.4.3)
  lib/inlineZh.test.ts      ≥ 8 ca (không token; 1 token; 2 token liền; token lỗi giữ nguyên văn bản; `[[` không đóng)
  lib/shuffle.ts            Fisher–Yates dùng Math.random (chỉ xáo thứ tự HIỂN THỊ lựa chọn)
  lib/displayPrefs.ts       đọc/ghi localStorage 'af.chinese.lesson.showPinyin' | '.showVi' (mặc định true)
  pages/LessonListPage.tsx
  pages/LessonDetailPage.tsx
  components/LessonCard.tsx         tiêu đề, tóm tắt, số từ, số câu, ~phút, chip trạng thái (Chưa học / Đang học / Hoàn thành {best}%), chip "Nội dung chưa được duyệt" khi reviewStatus=machine
  components/LessonContent.tsx      render danh sách khối — DÙNG LẠI ở xem trước F10 (prop `blocks`, `glossary`)
  components/blocks/TextBlock.tsx · DialogueBlock.tsx · GrammarBlock.tsx · TipBlock.tsx
  components/InlineZh.tsx           hiển thị token: chữ Hán (lang zh-CN) + pinyin dấu nhỏ phía sau trong ngoặc; bấm ⇒ đọc TTS
  components/SpeakButton.tsx        nút loa nhỏ dùng useSpeech('zh'); status `unsupported`/`no-voice` ⇒ disabled + Tooltip "Chưa có giọng tiếng Trung" (cùng lời F5)
  components/LessonWordList.tsx     danh sách từ: chữ Hán lớn, pinyin, Hán Việt, nghĩa (nhãn "chưa duyệt" khi machine), nút nghe, link /tu-dien/:id, chip "Đang ôn" khi inSrs
  components/GlossaryList.tsx       "Từ bổ sung (không vào ôn tập)"
  components/quiz/QuizRunner.tsx    máy trạng thái: intro → câu i/n → xác nhận nộp → kết quả
  components/quiz/QuizQuestionView.tsx
  components/quiz/QuizResultView.tsx
  components/quiz/QuizHistory.tsx   5 lần gần nhất (ngày giờ địa phương, điểm, đạt/không)
```

Hành vi `LessonDetailPage`:
- Tải bài; nếu `progress == null` gọi `startLesson` **một lần** (ref chặn StrictMode gọi đôi; API vốn idempotent).
- Đầu trang: tiêu đề, mục tiêu (danh sách), chip chưa duyệt, nút quay lại `/bai-hoc`. Người có `content.manage` thấy thêm nút "Sửa bài" → `/quan-tri/bai-hoc/:id` (F10 thêm; F9 chưa có route admin thì không hiện).
- Nếu `tone-stats.totalAnswered < 40` (gọi `GET /api/pinyin/tone-stats`, lỗi thì bỏ qua) ⇒ `Alert` info "Nên học xong phần Pinyin trước để nghe đúng thanh" + link `/pinyin` — **không chặn**.
- Tab **Nội dung**: công tắc "Pinyin" và "Nghĩa tiếng Việt" (lưu `displayPrefs`); `DialogueBlock` mỗi dòng: tên người nói, chữ Hán (cỡ 22–24px), pinyin, nghĩa, nút nghe dòng; nút "Nghe cả đoạn" đọc lần lượt (hàng đợi `speak`, dừng khi rời trang). Tốc độ đọc lấy `ttsRate` (K18), mặc định 0.8.
- Tab **Từ vựng**: `LessonWordList` + `GlossaryList`.
- Tab **Quiz**: `QuizRunner`:
  - Mỗi màn một câu, thanh tiến độ `i/n`; lựa chọn dạng nút to xếp dọc (thứ tự đã xáo một lần khi bắt đầu lượt, giữ nguyên khi quay lại câu trước); nút "Trước"/"Tiếp"; câu cuối ⇒ "Nộp bài" (mở `useConfirm` nếu còn câu chưa trả lời — nút nộp **disabled** cho tới khi đủ câu, kèm dòng giải thích "Còn N câu chưa trả lời").
  - `listen_choice`: nút loa lớn; nếu `autoPlayAudio` (K18) thì gọi `speak` **ngay trong handler** của nút "Bắt đầu"/"Tiếp"/"Trước" dẫn tới câu đó (không gọi trong `useEffect` — iOS Safari chặn, K15); không có giọng ⇒ `Alert` warning + nút "Hiện chữ" (hiện `audioText`, pinyin dấu).
  - `prompt_lang='zh'` ⇒ `LangText` + pinyin (nếu có) theo công tắc Pinyin; lựa chọn `lang='zh'` ⇒ `LangText`; `lang='pinyin'` ⇒ `numberedToMarked`.
  - Nộp: `clientAttemptId = crypto.randomUUID()` sinh **khi bắt đầu lượt** (không phải lúc bấm), `startedAt` ISO; lỗi mạng ⇒ giữ đáp án, nút "Thử lại" gửi **cùng** `clientAttemptId`; `422 QUIZ_CHANGED` ⇒ thông báo "Bài vừa được cập nhật — tải lại quiz", refetch, về intro.
  - Kết quả: điểm lớn, Đạt/Chưa đạt (ngưỡng 80% ghi rõ); `firstCompletion` ⇒ khối chúc mừng "Hoàn thành bài! Đã thêm {srsCardsAdded} từ vào ôn tập" + nút "Ôn tập ngay" (`/on-tap`) + (F8 thêm) "Luyện viết chữ của bài"; danh sách từng câu: đáp án đã chọn, đáp án đúng, lời giải; nút "Làm lại".
- Sau nộp: invalidate `['lessons']`, `['lessons', slug]`, `['srs','summary']`, `['progress','overview']`, `['lessons', id, 'attempts']`.

`LessonListPage`: thẻ bài một cột ở 375px, hai cột ≥ md; đầu trang "Bài tiếp theo" nổi bật (bài chưa completed có `orderIndex` nhỏ nhất); danh sách rỗng ⇒ "Chưa có bài học nào được xuất bản".

#### 5.3.2 F8 — luyện viết

Dependency: `"hanzi-writer": "3.7.3"` (**ghim chính xác**, không `^`) trong `apps/chinese/package.json`; chạy `yarn install` ở `frontend/`; `yarn workspace @af/chinese build` bắt buộc (đụng dependency).

Route:

| Route | Trang |
|---|---|
| `/luyen-viet?tab=hsk1\|bai-hoc\|can-luyen\|da-luyen&bai=<slug>&page=` | `WritingHomePage` |
| `/luyen-viet/:hanzi?tab=xem\|to-theo\|tu-viet&tu=<hsk1\|bai:slug\|can-luyen\|da-luyen>` | `WritingPracticePage` (`:hanzi` encode bằng `encodeURIComponent`; react-router tự giải mã) |

File:

```
src/features/writing/
  api.ts            getWritingCharacters(set, page, pageSize), getWritingCharacter(hanzi), postWritingAttempt(body), getWritingSummary()
  hooks.ts          useWritingCharacters, useWritingCharacter, useRecordWritingAttempt, useWritingSummary, useHanziDataManifest
  types.ts
  lib/charData.ts           codePointHex(ch) → '7231'; loadManifest() (fetch '/hanzi-data/index.json', cache module-level);
                            hasStrokeData(ch); charDataLoader: CharDataLoaderFn — fetch `/hanzi-data/${hex}.json`,
                            kiểm res.ok VÀ content-type chứa 'json' (dev Vite có thể trả index.html cho file thiếu), lỗi ⇒ onError
  lib/charData.test.ts      codePointHex('爱')='7231'; chữ Ext B ra 5 hex; hasStrokeData theo manifest giả
  lib/setParam.ts           ánh xạ tab ⇄ set API: hsk1→'hsk1', bai-hoc+bai→'lesson:<slug>', can-luyen→'weak', da-luyen→'practiced'
  components/MiZiGrid.tsx           SVG lưới 米字格 (viền + 2 đường giữa + 2 chéo, nét đứt, màu theo theme) đặt tuyệt đối dưới bảng viết
  components/HanziWriterBoard.tsx   bọc hanzi-writer (chi tiết dưới)
  components/CharacterInfoCard.tsx  chữ lớn, pinyin (các cách đọc), Hán Việt, số nét, bộ thủ, ≤ 5 từ chứa chữ (link /tu-dien/:id), nút nghe
  components/StepView.tsx           bước Xem: animateCharacter, nút Phát lại / Lặp / Tốc độ (0.5×, 1×)
  components/StepGuided.tsx         bước Tô theo
  components/StepRecall.tsx         bước Tự viết + nút "Gợi ý nét"
  components/AttemptResult.tsx      số lỗi, số gợi ý, trạng thái thuộc chữ, nút "Viết lại" / "Bước kế" / "Chữ tiếp"
  components/CharacterGrid.tsx      lưới ô chữ (ô 64px, 4–5 cột ở 375px): chữ, pinyin nhỏ, chấm màu trạng thái (new xám, practicing cam, mastered xanh), ô mờ khi không có dữ liệu nét
  components/LicenseNote.tsx        dòng nhỏ cuối trang: "Dữ liệu nét: Make Me a Hanzi / Arphic Public License" (link /hanzi-data/ARPHICPL.TXT) · "hanzi-writer (MIT)" (link /licenses/hanzi-writer.LICENSE.txt)
  pages/WritingHomePage.tsx
  pages/WritingPracticePage.tsx
public/licenses/hanzi-writer.LICENSE.txt   chép NGUYÊN VĂN node_modules/hanzi-writer/LICENSE
```

`HanziWriterBoard` (props: `hanzi`, `mode: 'view'|'guided'|'recall'`, `size`, `onComplete(summary)`, `onMistake`, `onCorrectStroke`, ref điều khiển `animate()`, `loop()`, `hint()`, `restart()`):
- `HanziWriter.create(divRef.current, hanzi, { width: size, height: size, padding: 12, renderer: 'svg', charDataLoader, showOutline, showCharacter, strokeColor/outlineColor/drawingColor/highlightColor từ theme, onLoadCharDataError })` — **luôn truyền `charDataLoader`** (R-W1).
- Tạo trong `useEffect` theo `[hanzi, mode]`; cleanup: `writer.cancelQuiz()` (bọc try) + `divRef.current.innerHTML = ''` (thư viện không có `destroy`; StrictMode React 19 chạy effect hai lần — cleanup phải để DOM sạch, không sinh 2 SVG chồng nhau).
- Kích thước `size = Math.min(window.innerWidth * 0.9, 360)` (theo `resize`), vùng chứa `touch-action: none; user-select: none` để vuốt không cuộn trang khi viết trên điện thoại.
- `guided`: `writer.quiz({ showHintAfterMisses: 2, onMistake, onCorrectStroke, onComplete })`; `recall`: `showOutline:false, showCharacter:false`, `showHintAfterMisses: 3`, `highlightOnComplete: true`.
- Đếm gợi ý (R-W3): `onMistake(d)` ⇒ nếu `d.mistakesOnStroke === threshold` thì `hints++`; nút "Gợi ý nét" ⇒ `writer.highlightStroke(currentStroke)` + `hints++`, `currentStroke` = `d.strokeNum + 1` cập nhật ở `onCorrectStroke` (bắt đầu 0).
- `onComplete({ totalMistakes })` ⇒ gọi `onComplete({ totalMistakes, hintsUsed, totalStrokes: <số nét từ dữ liệu: strokes.length giữ lại khi loader trả>, durationMs })`.

`WritingPracticePage`:
- Tải `useWritingCharacter(hanzi)` + manifest; chữ không có dữ liệu nét ⇒ `CharacterInfoCard` + `Alert` "Chưa có dữ liệu nét cho chữ này" (tab to-theo/tu-viet disabled).
- Tabs `xem | to-theo | tu-viet` (`useTabParam`). Gợi ý thứ tự: người học chưa có lần viết nào ⇒ mặc định `xem`; có ⇒ `tu-viet`.
- Hoàn tất lượt ⇒ gửi `postWritingAttempt` với `clientAttemptId` sinh **lúc bắt đầu lượt**; lỗi mạng ⇒ nút "Gửi lại" cùng id; kết quả hiện `AttemptResult` (mastered mới ⇒ "Đã thuộc chữ 爱!").
- "Chữ tiếp": dùng `tu` để lấy danh sách cùng bộ (đã cache) và đi tới chữ kế có dữ liệu nét; hết ⇒ về `/luyen-viet?tab=…`.
- Invalidate `['writing', …]`, `['progress','overview']` sau khi gửi.

`WritingHomePage`: tabs `hsk1 | bai-hoc | can-luyen | da-luyen`; `bai-hoc` có ô chọn bài (`Select` các bài published từ `useLessons`, lưu `?bai=`); đầu trang tóm tắt `useWritingSummary` (đã luyện / đã thuộc / cần luyện); phân trang 60 chữ/trang; rỗng ở `can-luyen` ⇒ "Chưa có chữ nào cần luyện thêm — tiếp tục giữ nhịp nhé" (chữ thuần, không emoji).

Nối F9: `QuizResultView` và tab Từ vựng thêm nút "Luyện viết chữ của bài" → `/luyen-viet?tab=bai-hoc&bai=<slug>`.

`nginx.conf` (apps/chinese) thêm **trước** `location /`:

```nginx
# Dữ liệu nét chữ (F8) — tên file không có hash ⇒ cache 7 ngày, không immutable.
# try_files =404: KHÔNG được rơi về index.html (hanzi-writer sẽ parse HTML như JSON).
location /hanzi-data/ {
    expires 7d;
    add_header Cache-Control "public, max-age=604800";
    try_files $uri =404;
}
location /licenses/ {
    try_files $uri =404;
}
```

(`gzip_types` đã có `application/json`.) Ghi chú trong commit: **chưa verify bằng Docker** — thêm mục vào `deploy/VERIFY-DOCKER.md`: "`curl -I https://chinese.antfarms.xyz/hanzi-data/7231.json` ⇒ 200 `application/json`; `/hanzi-data/ffff.json` ⇒ 404 (không phải index.html)".

#### 5.3.3 F10 — quản trị nội dung

Route (bọc `RequirePermission permission="content.manage"` — thiếu quyền ⇒ `/403`):

| Route | Trang |
|---|---|
| `/quan-tri/bai-hoc?trang-thai=&q=&page=` | `AdminLessonListPage` (`trang-thai` ∈ `tat-ca` (mặc định, không gồm lưu trữ) \| `nhap` \| `da-xuat-ban` \| `luu-tru`) |
| `/quan-tri/bai-hoc/:id?tab=thong-tin\|noi-dung\|tu-vung\|quiz\|xem-truoc` | `AdminLessonEditPage` |
| `/quan-tri/tu-vung?trang-thai=machine\|reviewed&han-viet=derived\|reviewed&hsk=1&q=&page=` | `AdminWordReviewPage` (mặc định `trang-thai=machine`, `hsk=1`) |

Nav: một mục "Quản trị nội dung" (`requiredPermission: 'content.manage'`) → `/quan-tri/bai-hoc`; hai trang admin có `Tabs` điều hướng liên kết "Bài học | Từ vựng" (link, không phải tab trạng thái).

File:

```
src/features/admin-content/
  api.ts / hooks.ts / types.ts     theo §6.3
  lib/slug.ts                      slugify(title): bỏ dấu (đ→d), lower, ký tự khác [a-z0-9] → '-', gộp '-', cắt 64 + test
  lib/useUnsavedChangesGuard.ts    useBlocker (react-router) + beforeunload; mở useConfirm "Rời trang? Thay đổi chưa lưu sẽ mất"
  lib/lessonDraft.ts               chuyển DTO ⇄ state form; chuẩn hoá trước khi gửi
  pages/AdminLessonListPage.tsx
  pages/AdminLessonEditPage.tsx
  pages/AdminWordReviewPage.tsx
  components/CreateLessonDialog.tsx     AppDialog: tiêu đề, slug (tự sinh từ tiêu đề, sửa được), chủ đề, thứ tự
  components/LessonMetaForm.tsx         RHF + zod: tiêu đề, slug (khoá khi publishedAt có — kèm helperText lý do), chủ đề, thứ tự, tóm tắt, mục tiêu (danh sách dòng), số phút, glossary (bảng dòng)
  components/LessonStatusBar.tsx        chip status + review; nút Xuất bản / Gỡ xuất bản / Duyệt nội dung / Lưu trữ(Xoá) / Khôi phục; hiện warnings sau xuất bản
  components/BlockListEditor.tsx        danh sách khối: thêm (menu 4 loại), xoá (useConfirm), lên/xuống (nút mũi tên — không kéo-thả), thu gọn/mở
  components/blocks/TextBlockForm.tsx · DialogueBlockForm.tsx · GrammarBlockForm.tsx · TipBlockForm.tsx
  components/PinyinField.tsx            TextField nhập pinyin số thanh; helperText hiện bản dấu (numberedToMarked) để soát
  components/LessonWordsEditor.tsx      AppAutocomplete tìm từ (gọi /api/dictionary/search, debounce 300ms) — renderInput TRẢI params.slotProps TRƯỚC;
                                        danh sách đã chọn: chữ, pinyin, nghĩa, lên/xuống/xoá; cảnh báo > 15 từ
  components/QuizEditor.tsx             danh sách câu; mỗi câu: loại, prompt, promptLang, promptPinyin, audioText (chỉ listen), 2–4 lựa chọn (id a–d tự gán theo vị trí), radio đáp án đúng, lời giải; lên/xuống/xoá
  components/LessonPreview.tsx          dùng LessonContent (F9) + danh sách câu có đáp án đánh dấu
  components/WordReviewList.tsx         desktop: bảng (Checkbox, chữ, pinyin dấu, Hán Việt, nghĩa, trạng thái); mobile: thẻ
  components/WordEditDrawer.tsx         AppDrawer: nghĩa (danh sách dòng thêm/xoá), Hán Việt, 2 lựa chọn trạng thái, nghĩa tiếng Anh (chỉ đọc) để đối chiếu, nút "Lưu" và "Lưu & đánh dấu đã duyệt"
```

Hành vi:
- `AdminLessonEditPage`: mỗi tab có nút "Lưu" riêng gọi đúng endpoint (`PUT meta|blocks|words|quiz`) với `version` hiện tại; thành công ⇒ cập nhật cache bằng bài trả về (version mới), toast "Đã lưu"; `409` ⇒ `Alert` error "Bài đã bị sửa ở nơi khác" + nút "Tải lại" (bỏ thay đổi cục bộ sau `useConfirm`); `400` ⇒ hiện lỗi theo đường dẫn `details` tại trường tương ứng (map `blocks[i].payload.lines[j].pinyin` → ô tương ứng; không map được ⇒ liệt kê trong `Alert`).
- Chuyển tab khi tab hiện tại có thay đổi chưa lưu ⇒ `useConfirm`. Rời trang ⇒ `useUnsavedChangesGuard`.
- Xuất bản lỗi `422 LESSON_NOT_PUBLISHABLE` ⇒ `Alert` liệt kê `details.problems`.
- Bài `archived` ⇒ `Alert` info "Bài đã lưu trữ — chỉ xem. Khôi phục để sửa." và mọi ô `disabled` (quy tắc "nút ẩn/khoá phải kèm giải thích").
- Nút "Xoá": `useConfirm` với nội dung khác nhau: bài admin chưa có người làm ⇒ "Xoá vĩnh viễn"; còn lại ⇒ "Bài sẽ được lưu trữ (có người đã học hoặc là bài có sẵn)".
- `AdminWordReviewPage`: bộ lọc trên URL; chọn nhiều ⇒ thanh hành động "Đánh dấu đã duyệt (N)" ⇒ `POST /api/admin/words/review`; có `conflicts` ⇒ toast cảnh báo + refetch. Bấm dòng ⇒ `WordEditDrawer`. Sau lưu, dòng không còn khớp bộ lọc vẫn giữ tới khi refetch trang (tránh nhảy danh sách khi đang duyệt liên tục) — refetch khi đổi trang/bộ lọc.
- Người có `content.manage`: trang chi tiết từ điển (F6) và `LessonDetailPage` (F9) hiện nút "Sửa nghĩa"/"Sửa bài" dẫn tới trang admin tương ứng (`/quan-tri/tu-vung?q=<simplified>` mở sẵn drawer theo `?sua=<id>`).

#### 5.3.4 F11 — tổng quan

Route `/` (index) ⇒ `DashboardPage` thay `pages/HomePage.tsx` (xoá file cũ; chuyển `ServiceStatusChip` vào khối "Trạng thái hệ thống" thu gọn cuối trang, **chỉ** hiện khi có `users.manage`).

File:

```
src/features/progress/
  api.ts            getProgressOverview()
  hooks.ts          useProgressOverview (staleTime 30s, refetchOnWindowFocus true)
  types.ts          §6.4
  lib/heatmap.ts          buildHeatmap(activity, today) → tuần (Thứ Hai đầu tuần) × 7, ô null cho ngày ngoài khoảng; level 0–4 theo ngưỡng [0, 1–9, 10–29, 30–59, ≥60]
  lib/heatmap.test.ts     today là Chủ nhật/Thứ Hai; 90 phần tử ⇒ 13–14 cột; level biên
  lib/todayTasks.ts       buildTodayTasks(overview) theo R-PG9 + test (thứ tự; mục ẩn khi 0; khối null bị bỏ)
  pages/DashboardPage.tsx
  components/StreakCard.tsx         số ngày lớn + "Dài nhất: N"; chưa học hôm nay ⇒ "Học 1 hoạt động để giữ chuỗi"
  components/DailyGoalCard.tsx      R-PG5 (LinearProgress) — ẩn khi srs null
  components/TodayTasks.tsx         danh sách thẻ bấm được (ListItemButton + icon)
  components/VocabularyCard.tsx     introduced/totalInPath, mature, learning (thanh xếp chồng tự vẽ bằng Box)
  components/LessonProgressCard.tsx completed/published + bài tiếp theo
  components/WritingProgressCard.tsx practiced/mastered/totalChars + cần luyện
  components/ToneAccuracyCard.tsx   accuracy % + thanh cần luyện
  components/ActivityHeatmap.tsx    lưới Box ô 16–18px, gap 3px (≈ 13×21 = 273px vừa 375px), nhãn tháng phía trên; chạm ô ⇒ Tooltip (slotProps.popper) "dd/MM: N lượt"
```

Bố cục: 375px một cột theo thứ tự StreakCard → DailyGoalCard → TodayTasks → ActivityHeatmap → VocabularyCard → LessonProgressCard → WritingProgressCard → ToneAccuracyCard; ≥ md lưới 2 cột (Grid MUI v9 `size={{ xs: 12, md: 6 }}`). Mỗi thẻ: khối `null` ⇒ ẩn; số liệu 0 ⇒ CTA (vd "Bắt đầu bài học đầu tiên"). Đầu trang: "Hôm nay, {thứ} {dd/MM}" theo `localDate` của server (không dùng ngày trình duyệt); nếu `Intl…timeZone` ≠ `overview.timeZone` ⇒ dòng nhỏ "Ngày học tính theo múi giờ hồ sơ ({timeZone}) — đổi ở Hồ sơ" link `/ho-so`. Đang tải ⇒ Skeleton; lỗi ⇒ `Alert` + nút Thử lại (trang không trắng).

### 5.4 Học liệu (`content/chinese/`) — agent `content-implement` (Sonnet)

#### 5.4.1 File mới/sửa

```
content/
  package.json                               F8: devDependencies thêm "hanzi-writer-data": "2.0.1" (GHIM chính xác)
                                             scripts thêm "build:hanzi-data:chinese": "node chinese/scripts/build-hanzi-data.mjs"
  yarn.lock
  chinese/
    schemas/lesson.schema.json               F9 (JSON Schema 2020-12, ajv)
    data/lessons/01-chao-hoi.json            F9
    data/lessons/02-ban-than.json            F9
    data/lessons/03-so-dem.json              F9
    data/lessons/04-gia-dinh.json            F9
    data/lessons/05-thoi-gian.json           F9
    scripts/validate.mjs                     F9: kiểm bài học (§5.4.3); F8: kiểm hanzi-data (§5.4.5)
    scripts/build-hanzi-data.mjs             F8 (§5.4.4)
    SOURCES.md                               F9: dòng `original` (bài học); F8: dòng `hanzi-writer`, `hanzi-writer-data`
    LICENSES/ARPHICPL.TXT                    F8: chép nguyên văn từ gói npm
    LICENSES/hanzi-writer-MIT.txt            F8: chép nguyên văn LICENSE của hanzi-writer 3.7.3
frontend/apps/chinese/public/hanzi-data/     F8: SINH bằng script, COMMIT vào git (ảnh Docker frontend build từ ./frontend, không có content/node_modules)
  index.json  ARPHICPL.TXT  NOTICE.md  <codepoint-hex>.json …
```

#### 5.4.2 Định dạng file bài học (F9)

Tên file `NN-<slug>.json`, `NN` = `orderIndex` hai chữ số.

```json
{
  "schemaVersion": 1,
  "slug": "chao-hoi",
  "title": "Chào hỏi",
  "topic": "giao-tiep",
  "level": "hsk1",
  "orderIndex": 1,
  "status": "published",
  "summary": "Chào, cảm ơn, xin lỗi và tạm biệt — những câu dùng được ngay hôm nay.",
  "objectives": ["Chào hỏi và đáp lại lời chào", "Nói cảm ơn, xin lỗi, tạm biệt", "Hỏi thăm bằng câu hỏi có 吗"],
  "estimatedMinutes": 15,
  "sources": ["original"],
  "words": [
    { "simplified": "你", "pinyin": "ni3" },
    { "simplified": "好", "pinyin": "hao3" }
  ],
  "glossary": [
    { "hanzi": "越南", "pinyin": "Yue4 nan2", "vi": "Việt Nam" }
  ],
  "blocks": [
    { "type": "text", "payload": { "paragraphs": ["Người Trung Quốc chào nhau bằng [[你好|ni3 hao3]] ..."] } },
    { "type": "dialogue", "payload": { "title": "Gặp nhau buổi sáng", "lines": [
      { "speaker": "Lan", "hanzi": "你好！", "pinyin": "Ni3 hao3!", "vi": "Chào bạn!" },
      { "speaker": "Minh", "hanzi": "你好！你好吗？", "pinyin": "Ni3 hao3! Ni3 hao3 ma5?", "vi": "Chào bạn! Bạn khoẻ không?" }
    ] } },
    { "type": "grammar", "payload": { "title": "Câu hỏi với 吗", "pattern": "Câu trần thuật + [[吗|ma5]] ?",
      "explanation": "Thêm [[吗|ma5]] vào cuối câu trần thuật để thành câu hỏi có/không.",
      "examples": [ { "hanzi": "你好吗？", "pinyin": "Ni3 hao3 ma5?", "vi": "Bạn khoẻ không?", "note": "Câu hỏi thăm quen thuộc" } ] } },
    { "type": "tip", "payload": { "variant": "pronunciation",
      "text": "Hai thanh 3 đứng liền: thanh đầu đọc gần thanh 2 — [[你好|ni3 hao3]] đọc như \"ní hǎo\". Pinyin vẫn ghi thanh gốc." } }
  ],
  "quiz": [
    { "key": "q1", "type": "listen_choice", "prompt": "Nghe và chọn nghĩa đúng", "promptLang": "vi",
      "audioText": "谢谢",
      "options": [ { "id": "a", "text": "xin lỗi", "lang": "vi" }, { "id": "b", "text": "cảm ơn", "lang": "vi" },
                   { "id": "c", "text": "tạm biệt", "lang": "vi" }, { "id": "d", "text": "không có gì", "lang": "vi" } ],
      "correctOptionId": "b", "explanation": "[[谢谢|xie4 xie5]] nghĩa là cảm ơn." },
    { "key": "q2", "type": "single_choice", "prompt": "你好吗？", "promptLang": "zh", "promptPinyin": "Ni3 hao3 ma5?",
      "options": [ { "id": "a", "text": "Bạn tên là gì?", "lang": "vi" }, { "id": "b", "text": "Bạn khoẻ không?", "lang": "vi" },
                   { "id": "c", "text": "Bạn là ai?", "lang": "vi" } ],
      "correctOptionId": "b", "explanation": "吗 cuối câu tạo câu hỏi có/không." }
  ]
}
```

(Ví dụ minh hoạ định dạng — nội dung thật do content-implement soạn theo §5.4.6 và kiểm từ có trong `hsk-words.json`.)

#### 5.4.3 Luật (schema + `validate.mjs`)

| Trường | Luật | Mức |
|---|---|---|
| `schemaVersion` | `1` | FAIL |
| `slug` | `^[a-z0-9]+(-[a-z0-9]+)*$`, 3–64, trùng phần tên file, duy nhất toàn thư mục | FAIL |
| `orderIndex` | số nguyên 1–999, = `NN` tên file, duy nhất | FAIL |
| `title` 1–200 · `summary` ≤ 1000 · `objectives` 1–6 mục ≤ 200 · `estimatedMinutes` 1–120 · `topic` `^[a-z0-9-]{2,64}$` · `level` = `hsk1` · `status` ∈ `draft|published` (mặc định `published`) | | FAIL |
| `sources` | mảng không rỗng, mọi khoá có trong `SOURCES.md`; bài tự soạn = `["original"]` | FAIL |
| `words` | 1–30 mục; `(simplified, pinyin)` có trong `hsk-words.json` (pinyin chuẩn hoá `ü/u:`→`v`); không trùng trong bài | FAIL |
| `words` số lượng | > 15 FAIL; < 8 WARN | FAIL/WARN |
| `glossary` | 0–10 mục; `hanzi` toàn chữ Hán ≤ 10; `pinyin` hợp lệ, số âm tiết = số chữ; `vi` 1–100 | FAIL |
| `blocks` | 1–30; có ≥ 1 `dialogue` và ≥ 1 `grammar` | FAIL (bài seed) |
| `text.paragraphs` | 1–10 mục, mỗi mục 1–2000 | FAIL |
| `dialogue` | `title?` ≤ 100; `lines` 2–20; `speaker` 1–20; `hanzi` 1–100 (chữ Hán + dấu câu `，。！？、：；“”…` hoặc ASCII tương ứng, **không** chữ Latin); `pinyin` hợp lệ sau khi bỏ dấu câu, số âm tiết = số chữ Hán; `vi` 1–300 | FAIL |
| `grammar` | `title` 1–100; `pattern?` ≤ 200; `explanation` 1–2000; `examples` 1–6 (mỗi ví dụ như dòng hội thoại, `note?` ≤ 200) | FAIL |
| `tip` | `text` 1–1000; `variant?` ∈ `pronunciation|culture|memory|grammar` | FAIL |
| Cú pháp chữ Hán nội dòng (trong `paragraphs`, `explanation`, `pattern`, `tip.text`, `explanation` của câu hỏi) | `[[<chữ Hán 1–10>|<pinyin số thanh, số âm tiết khớp>]]`; token hỏng | FAIL |
| `quiz` | 5–10 câu (seed); `key` `^[a-z0-9-]{1,32}$` duy nhất trong bài | FAIL |
| Tỉ lệ nghe | `listen_choice` ≥ 30% số câu (làm tròn lên: 5 câu ⇒ ≥ 2; 7 ⇒ ≥ 3; 10 ⇒ ≥ 3) | FAIL |
| Câu hỏi | `prompt` 1–300; `promptLang` ∈ `vi|zh`; `promptPinyin` chỉ khi `zh` (khuyến nghị có); `listen_choice` ⇒ `audioText` 1–100 toàn chữ Hán (+ dấu câu), `single_choice` ⇒ không có `audioText`; `options` 3–4 (seed), `id` lần lượt `a,b,c,d`; `text` 1–200; `lang` ∈ `vi|zh|pinyin` (`pinyin` ⇒ text hợp lệ số thanh); không hai lựa chọn trùng `text`; `correctOptionId` thuộc lựa chọn; `explanation` 1–500 | FAIL |
| Phân bố đáp án | đáp án đúng không trùng một `id` ở > 60% số câu (tránh "luôn chọn b") | WARN |
| Phủ chữ | Mọi chữ Hán trong `dialogue`, `grammar.examples`, `audioText`, lựa chọn `zh` thuộc: chữ của `words` bài này ∪ `words` các bài có `orderIndex` nhỏ hơn ∪ `glossary` bài này ∪ tập hư từ cho phép `{吗, 呢, 的, 了, 吧}` **chỉ khi** chúng có trong `words` của một bài ≤ hiện tại | WARN (liệt kê chữ) |
| Chữ viết được | Mọi chữ của `words` có trong `characters.json` | FAIL |

`validate.mjs` in bảng tổng: số bài, số từ/bài, số câu/bài, % câu nghe, số WARN.

#### 5.4.4 Script `build-hanzi-data.mjs` (F8)

```
Đầu vào : content/chinese/data/characters/characters.json (trường hanzi)
          content/node_modules/hanzi-writer-data/{<chữ>.json, ARPHICPL.TXT, package.json}
Đầu ra  : frontend/apps/chinese/public/hanzi-data/
Các bước:
 1. Kiểm package.json của hanzi-writer-data có version === "2.0.1" (khác ⇒ exit 1, thông báo rõ).
 2. Đọc danh sách chữ (duy nhất, sắp theo code point).
 3. Xoá mọi *.json trong thư mục đích (chạy lại không để sót file cũ), giữ nguyên thư mục.
 4. Với mỗi chữ: nguồn = node_modules/hanzi-writer-data/<chữ>.json; có ⇒ copyFile (byte-for-byte) sang <hex thường>.json
    (hex = codePointAt(0).toString(16), vd 爱 → 7231.json); không có ⇒ đưa vào missing.
 5. Chép ARPHICPL.TXT nguyên văn vào thư mục đích VÀ content/chinese/LICENSES/ARPHICPL.TXT.
 6. Ghi index.json (xuống dòng LF, khoá theo thứ tự cố định, KHÔNG có thời điểm sinh ⇒ chạy lại không tạo diff):
    { "dataset": "hanzi-writer-data", "version": "2.0.1", "license": "Arphic Public License (ARPHICPL.TXT)",
      "naming": "codepoint-hex-lower", "count": N, "characters": ["一", "七", …], "missing": [ … ] }
 7. Ghi NOTICE.md (nội dung cố định, tiếng Việt + tiếng Anh):
    - Nguồn: hanzi-writer-data 2.0.1 (npm, github.com/chanind/hanzi-writer-data), dữ liệu từ Make Me a Hanzi
      (github.com/skishore/makemeahanzi), trích từ phông Arphic PL KaitiM GB / UKai — Copyright 1999 Arphic Technology Co., Ltd.;
      Copyright 2016 Shaunak Kishore. Phân phối theo Arphic Public License, toàn văn ở ARPHICPL.TXT.
    - Thay đổi của AntFarm: CHỈ chọn tập con các chữ trong characters.json và đổi tên file theo mã Unicode; nội dung từng file
      giữ nguyên từng byte. Ngày sinh: ghi tay khi đổi phiên bản (không tự sinh).
 8. In: số chữ đã chép, số thiếu (liệt kê), tổng dung lượng.
Exit 1 khi: thiếu characters.json, thiếu node_modules (hướng dẫn `yarn --cwd content install`), sai version.
```

Kích thước dự kiến: HSK 3.0 cấp 1 ≈ 300 chữ × ~3,4 KB ≈ **1 MB chưa nén** (≈ 350 KB gzip); file chỉ tải khi mở chữ đó.

#### 5.4.5 Kiểm hanzi-data trong `validate.mjs` (F8)

- `index.json` tồn tại, `version = 2.0.1`; mọi chữ trong `characters.json` nằm ở `characters` hoặc `missing` (không có ⇒ FAIL "chạy lại build:hanzi-data:chinese"); `missing` không rỗng ⇒ WARN liệt kê.
- Mỗi chữ trong `characters` có file `<hex>.json` parse được, có mảng `strokes` và `medians` cùng độ dài > 0 ⇒ FAIL nếu sai.
- Không có file `.json` thừa ngoài `index.json` và các chữ trong danh sách ⇒ FAIL.
- `ARPHICPL.TXT` và `NOTICE.md` tồn tại; nếu có `node_modules/hanzi-writer-data` thì so byte `ARPHICPL.TXT` và 5 file ngẫu nhiên ⇒ FAIL nếu lệch.

#### 5.4.6 Nội dung 5 bài seed (tự soạn — `sources: ["original"]`) **[BA-mặc định — D15]**

Từ gợi ý dưới đây **phải** đối chiếu `hsk-words.json` (HSK 3.0 cấp 1); từ không có trong kho ⇒ thay từ khác cùng chủ đề hoặc chuyển vào `glossary` (không vào SRS). Mỗi bài: 1 khối `text` mở đầu (tình huống, 2–4 câu), 1–2 `dialogue` (4–10 dòng), 1–3 `grammar`, 1–2 `tip` (ít nhất một mẹo phát âm dành cho người Việt), 6–8 câu quiz (≥ 3 câu nghe). Hội thoại dùng tên người Việt (Lan, Minh, Hoa, Nam) và thầy/cô giáo — bối cảnh gần gũi.

| # | slug · tiêu đề | Từ gợi ý (8–15) | Ngữ pháp | Mẹo |
|---|---|---|---|---|
| 01 | `chao-hoi` · Chào hỏi | 你, 好, 您, 我, 他, 她, 们/我们, 老师, 谢谢, 不客气, 对不起, 没关系, 再见, 吗 | Câu hỏi 吗; 您 lịch sự; 们 số nhiều | Biến điệu hai thanh 3 (你好); 谢谢 thanh nhẹ ở âm sau |
| 02 | `ban-than` · Giới thiệu bản thân | 叫, 什么, 名字, 是, 不, 人, 中国, 学生, 哪, 国, 呢, 也, 很, 认识, 高兴 | A 是 B; phủ định 不 + biến điệu `bu2` trước thanh 4 (gợi ý, lưu `bu4`); câu hỏi 什么/哪; 呢 hỏi lại | `sh` cong lưỡi khác `s`; `r` trong 人 không đọc như "r" tiếng Việt. Glossary: 越南 |
| 03 | `so-dem` · Số đếm | 一…十, 零, 百, 几, 多少, 岁, 个, 两 | Số 11–99; 几 (dưới 10) và 多少; lượng từ 个; 两 và 二 | Biến điệu 一 (`yi1` gốc); `shi2` (十) và `si4` (四) người Việt hay lẫn |
| 04 | `gia-dinh` · Gia đình | 家, 爸爸, 妈妈, 哥哥, 姐姐, 弟弟, 妹妹, 儿子, 女儿, 有, 没有, 和, 口, 的 | 有/没有; 的 sở hữu (我的妈妈 / 我妈妈); 几口人; 和 nối danh từ | Thanh nhẹ ở âm lặp (爸爸 `ba4 ba5`); mẹo nhớ 口 = miệng = người trong nhà |
| 05 | `thoi-gian` · Ngày giờ | 今天, 明天, 昨天, 现在, 点, 分, 年, 月, 号, 星期, 上午, 下午, 晚上, 时候 | Thứ tự năm–tháng–ngày (lớn → nhỏ, ngược tiếng Việt); 星期几; 几点; trạng từ thời gian đứng trước động từ | Hán Việt giúp nhớ: 年 niên, 月 nguyệt, 今 kim, 明 minh |

Quy tắc số bài: tổng từ 5 bài ≈ 60–70 từ khác nhau; một từ có thể xuất hiện ở nhiều bài nhưng chỉ khai trong `words` của bài **đầu tiên** dùng nó (bài sau dựa vào luật phủ chữ).

#### 5.4.7 `SOURCES.md` — dòng bổ sung

| Khoá | Tên | URL | Giấy phép | Ngày lấy | Phiên bản | Phần đã dùng | Nghĩa vụ | File |
|---|---|---|---|---|---|---|---|---|
| `original` | Nội dung tự soạn AntFarm | — | Thuộc dự án | 2026-09-xx | — | 5 bài học, quiz, mẹo | — | `data/lessons/*.json` |
| `hanzi-writer` | Hanzi Writer | https://github.com/chanind/hanzi-writer | MIT | 2026-09-xx | npm 3.7.3 | Thư viện hoạt hình + chấm nét (frontend) | Kèm LICENSE | `LICENSES/hanzi-writer-MIT.txt`, `frontend/apps/chinese/public/licenses/hanzi-writer.LICENSE.txt` |
| `hanzi-writer-data` | Hanzi Writer Data (từ Make Me a Hanzi, phông Arphic) | https://github.com/chanind/hanzi-writer-data | Arphic Public License | 2026-09-xx | npm 2.0.1 | Dữ liệu nét của các chữ trong `characters.json` (tập con, nội dung nguyên vẹn, đổi tên file) | Giữ nguyên `ARPHICPL.TXT` trong mọi bản sao; ghi chú thay đổi (`NOTICE.md`) | `LICENSES/ARPHICPL.TXT`, `frontend/apps/chinese/public/hanzi-data/*` |

---

## 6. Hợp đồng API (đường dẫn service; trình duyệt gọi `/chinese/api/...`)

### 6.0 Mã lỗi mới (bổ sung HĐG §6.0; body `{ error, code, details? }`)

| HTTP | `code` | Khi nào | Feature |
|---|---|---|---|
| 400 | `VALIDATION` | Request/payload sai; `details` theo đường dẫn (`blocks[2].payload.lines[0].pinyin`) | tất cả |
| 404 | `NOT_FOUND` | Bài không tồn tại/không published (học viên); chữ không có; từ không có | F8–F10 |
| 409 | `DUPLICATE_ATTEMPT_ID` | `clientAttemptId` đã dùng bởi người khác | F8, F9 |
| 409 | `CONCURRENCY_CONFLICT` | `version` lệch | F10 |
| 409 | `SLUG_TAKEN` | slug trùng | F10 |
| 422 | `QUIZ_EMPTY` | Bài không có câu hỏi | F9 |
| 422 | `QUIZ_CHANGED` | Tập câu trong bài nộp khác tập câu hiện tại | F9 |
| 422 | `UNKNOWN_CHARACTER` | Chữ không có trong `content.characters` | F8 |
| 422 | `UNKNOWN_WORD` · `UNKNOWN_QUESTION` | id không tồn tại/không thuộc bài; `details.ids` | F10 |
| 422 | `LESSON_NOT_PUBLISHABLE` | `details.problems: string[]` | F10 |
| 422 | `LESSON_ARCHIVED` · `LESSON_NOT_ARCHIVED` · `SLUG_LOCKED` | R-CA7, R-CA8 | F10 |
| 503 | `CONTENT_UNAVAILABLE` | (F5) — F11 không trả mã này, chỉ ẩn khối | — |

Thời điểm ISO-8601 UTC `Z`; ngày `YYYY-MM-DD`; trường `null` bị **bỏ khỏi JSON** (cấu hình hiện tại) — frontend khai kiểu `field?: T | null`.

### 6.1 F9 — học viên (`study.use`)

**`GET /api/lessons`** → 200

```json
{ "items": [
  { "id": "0192…", "slug": "chao-hoi", "title": "Chào hỏi", "topic": "giao-tiep", "orderIndex": 1,
    "summary": "…", "estimatedMinutes": 15, "wordCount": 14, "questionCount": 7, "reviewStatus": "machine",
    "progress": { "status": "completed", "bestScorePercent": 85, "attemptsCount": 2,
                  "startedAt": "2026-09-20T01:00:00Z", "completedAt": "2026-09-20T01:20:00Z" } },
  { "id": "…", "slug": "ban-than", "…": "…" }
], "nextLessonSlug": "ban-than" }
```

`progress` vắng khi chưa bắt đầu. Không phân trang (số bài nhỏ). Sắp `orderIndex`, `title`.

**`GET /api/lessons/{slug}`** → 200 (404 nếu không published)

```json
{ "id": "…", "slug": "chao-hoi", "title": "Chào hỏi", "topic": "giao-tiep", "orderIndex": 1, "summary": "…",
  "objectives": ["…"], "estimatedMinutes": 15, "reviewStatus": "machine",
  "glossary": [ { "hanzi": "越南", "pinyin": "Yue4 nan2", "vi": "Việt Nam" } ],
  "blocks": [ { "id": "…", "type": "dialogue", "payload": { "title": "…", "lines": [ { "speaker": "Lan", "hanzi": "你好！", "pinyin": "Ni3 hao3!", "vi": "Chào bạn!" } ] } } ],
  "words": [ { "id": "…", "simplified": "你", "traditional": null, "pinyin": "ni3", "hanViet": "nhĩ",
               "meaningsVi": ["bạn", "anh, chị (ngôi thứ hai)"], "meaningViStatus": "machine", "inSrs": true } ],
  "quiz": [ { "id": "…", "type": "listen_choice", "prompt": "Nghe và chọn nghĩa đúng", "promptLang": "vi",
              "audioText": "谢谢",
              "options": [ { "id": "a", "text": "xin lỗi", "lang": "vi" }, { "id": "b", "text": "cảm ơn", "lang": "vi" } ] } ],
  "progress": { "status": "in_progress", "bestScorePercent": 60, "attemptsCount": 1, "startedAt": "…" } }
```

**Không** có `correctOptionId`, `explanation`, `key` trong `quiz` (R-LS10).

**`POST /api/lessons/{id}/start`** (body rỗng) → 200 `{ "status": "in_progress", "startedAt": "…", … }` (đã có ⇒ trả bản hiện có, kể cả `completed`) · 404.

**`POST /api/lessons/{id}/quiz-attempts`**

```json
// request
{ "clientAttemptId": "5b0f…", "startedAt": "2026-09-20T01:10:00Z",
  "answers": [ { "questionId": "…", "optionId": "b" } ] }
// 201 (lần đầu) · 200 (gửi lại cùng clientAttemptId)
{ "attemptId": "…", "submittedAt": "2026-09-20T01:14:02Z",
  "total": 7, "correct": 6, "scorePercent": 85, "passed": true, "passThresholdPercent": 80,
  "firstCompletion": true, "srsCardsAdded": 13,
  "results": [ { "questionId": "…", "optionId": "b", "correct": true, "correctOptionId": "b",
                 "explanation": "[[谢谢|xie4 xie5]] nghĩa là cảm ơn." } ],
  "progress": { "status": "completed", "bestScorePercent": 85, "attemptsCount": 2, "startedAt": "…", "completedAt": "…" } }
```

Lỗi: 400 `VALIDATION` · 404 · 409 `DUPLICATE_ATTEMPT_ID` · 422 `QUIZ_EMPTY` | `QUIZ_CHANGED`. Khi phát lại (200): `firstCompletion = (lesson_progress.completed_at == attempt.submitted_at)` (service gán `completed_at` đúng bằng `submitted_at` của lần đạt đầu để so được), `srsCardsAdded = 0`; frontend chỉ hiện "Đã thêm N từ" khi N > 0.

**`GET /api/lessons/{id}/quiz-attempts?limit=5`** (1–20, mặc định 5) → 200 `{ "items": [ { "attemptId", "submittedAt", "total", "correct", "scorePercent", "passed", "durationMs" } ] }` (mới nhất trước; không kèm chi tiết từng câu).

### 6.2 F8 — luyện viết (`study.use`)

**`POST /api/writing/attempts`**

```json
// request
{ "clientAttemptId": "…", "hanzi": "爱", "mode": "recall", "totalStrokes": 10, "totalMistakes": 0, "hintsUsed": 0, "durationMs": 21400 }
// 201 · 200 (phát lại)
{ "attemptId": "…", "completedAt": "…", "isClean": true,
  "stats": { "hanzi": "爱", "attempts": 3, "guidedAttempts": 2, "recallAttempts": 1, "lastMistakes": 0, "lastHints": 0,
             "bestRecallMistakes": 0, "cleanRecallDays": 1, "masteryStatus": "practicing", "isWeak": false,
             "firstPracticedAt": "…", "lastPracticedAt": "…" },
  "becameMastered": false }
```

Lỗi: 400 · 409 `DUPLICATE_ATTEMPT_ID` · 422 `UNKNOWN_CHARACTER`.

**`GET /api/writing/characters?set=hsk1|lesson:<slug>|weak|practiced&page=1&pageSize=60`** (pageSize tối đa 200) → 200

```json
{ "set": "hsk1", "items": [
  { "hanzi": "爱", "pinyinReadings": ["ai4"], "hanViet": ["ái"], "strokeCount": 10,
    "masteryStatus": "practicing", "lastMistakes": 1, "lastPracticedAt": "…" } ],
  "page": 1, "pageSize": 60, "totalCount": 297 }
```

`masteryStatus` luôn có (`new` khi chưa viết); `lastMistakes`/`lastPracticedAt` vắng khi `new`. Lỗi: 400 (set sai) · 404 (bài không có/không published).

**`GET /api/writing/characters/{hanzi}`** (hanzi URL-encoded) → 200 `{ hanzi, traditionalVariants, pinyinReadings, hanViet, strokeCount, radical, words: [≤ 5: { id, simplified, pinyin, meaningsVi, meaningViStatus, hsk3Level }], stats?: {…như trên} }` · 404.

**`GET /api/writing/summary`** → 200 `{ "practicedChars": 24, "masteredChars": 5, "weakChars": 3, "attemptsToday": 7, "totalChars": 297 }`.

### 6.3 F10 — quản trị (`content.manage`)

Kiểu dùng chung `AdminLesson`:

```json
{ "id": "…", "version": 123456, "slug": "chao-hoi", "title": "…", "topic": "giao-tiep", "level": "hsk1",
  "orderIndex": 1, "summary": "…", "objectives": ["…"], "estimatedMinutes": 15, "glossary": [ … ],
  "status": "published", "reviewStatus": "machine", "source": "seed",
  "publishedAt": "…", "reviewedAt": null, "editedAt": "…", "editedByName": "Quân", "createdAt": "…", "updatedAt": "…",
  "hasAttempts": true,
  "blocks": [ { "id": "…", "type": "text", "payload": { … } } ],
  "words": [ { "id": "…", "simplified": "你", "pinyin": "ni3", "meaningsVi": ["bạn"], "meaningViStatus": "machine" } ],
  "quiz": [ { "id": "…", "key": "q1", "type": "listen_choice", "prompt": "…", "promptLang": "vi", "promptPinyin": null,
              "audioText": "谢谢", "options": [ … ], "correctOptionId": "b", "explanation": "…" } ],
  "warnings": [] }
```

| Method + đường dẫn | Request | Response | Lỗi |
|---|---|---|---|
| `GET /api/admin/lessons?status=draft\|published\|archived&q=&page=&pageSize=` | — | `{ items: [{ id, slug, title, orderIndex, status, reviewStatus, source, wordCount, questionCount, editedAt, publishedAt }], page, pageSize, totalCount }` | — |
| `POST /api/admin/lessons` | `{ slug, title, topic, orderIndex?, summary? }` | 201 `AdminLesson` | 400, 409 `SLUG_TAKEN` |
| `GET /api/admin/lessons/{id}` | — | 200 `AdminLesson` | 404 |
| `PUT /api/admin/lessons/{id}` | `{ version, slug, title, topic, orderIndex, summary, objectives, estimatedMinutes, glossary }` | 200 `AdminLesson` | 400, 404, 409 `CONCURRENCY_CONFLICT`\|`SLUG_TAKEN`, 422 `SLUG_LOCKED`\|`LESSON_ARCHIVED` |
| `PUT /api/admin/lessons/{id}/blocks` | `{ version, blocks: [{ type, payload }] }` (0–30) | 200 | 400 (đường dẫn payload), 404, 409, 422 `LESSON_ARCHIVED` |
| `PUT /api/admin/lessons/{id}/words` | `{ version, wordIds: [uuid] }` (0–30, thứ tự = mảng) | 200 | 400, 404, 409, 422 `UNKNOWN_WORD`\|`LESSON_ARCHIVED` |
| `PUT /api/admin/lessons/{id}/quiz` | `{ version, questions: [{ id?, type, prompt, promptLang, promptPinyin?, audioText?, options: [{ text, lang }], correctIndex, explanation? }] }` (0–30; `options` 2–4, server gán id `a..d` theo vị trí; `correctIndex` 0-based) | 200 | 400, 404, 409, 422 `UNKNOWN_QUESTION`\|`LESSON_ARCHIVED` |
| `POST /api/admin/lessons/{id}/publish` | `{ version }` | 200 `AdminLesson` (có `warnings`) | 404, 409, 422 `LESSON_NOT_PUBLISHABLE`\|`LESSON_ARCHIVED` |
| `POST /api/admin/lessons/{id}/unpublish` | `{ version }` | 200 | 404, 409, 422 `LESSON_ARCHIVED` |
| `POST /api/admin/lessons/{id}/review` | `{ version }` | 200 | 404, 409 |
| `POST /api/admin/lessons/{id}/restore` | `{ version }` | 200 (`status=draft`) | 404, 409, 422 `LESSON_NOT_ARCHIVED` |
| `DELETE /api/admin/lessons/{id}?version=` | — | 204 (xoá cứng) · 200 `{ "result": "archived", "lesson": AdminLesson }` | 404, 409 |

Ghi chú: `PUT .../quiz` dùng `correctIndex` thay `correctOptionId` để form không phải quản id lựa chọn; `AdminLesson.quiz` trả `correctOptionId` (frontend đổi sang index = vị trí của id).

Từ vựng:

| Method + đường dẫn | Request | Response | Lỗi |
|---|---|---|---|
| `GET /api/admin/words?meaningViStatus=machine\|reviewed&hanVietStatus=derived\|reviewed&hsk=1&q=&page=&pageSize=` | — | `{ items: [AdminWord], page, pageSize, totalCount }` | 400 |
| `GET /api/admin/words/{id}` | — | `AdminWord` | 404 |
| `PUT /api/admin/words/{id}` | `{ version, meaningsVi: string[], meaningViStatus, hanViet: string\|null, hanVietStatus }` | 200 `AdminWord` | 400, 404, 409 |
| `POST /api/admin/words/review` | `{ items: [{ id, version }] }` (1–100) | 200 `{ "updated": 2, "conflicts": ["…"], "notFound": [] }` | 400 |

`AdminWord`: `{ id, version, simplified, traditional, pinyin, hsk3Level, pathOrder, pos, meaningsEn, meaningsVi, meaningViStatus, meaningViSource, hanViet, hanVietStatus, editedAt, editedByName }`.

### 6.4 F11 — tổng quan (`study.use`)

**`GET /api/progress/overview`** → 200 (thay HĐG §6.8, bổ sung trường)

```json
{ "localDate": "2026-09-18", "timeZone": "Asia/Ho_Chi_Minh",
  "streak": { "current": 5, "longest": 12, "studiedToday": false },
  "today": { "srsReviews": 0, "newCards": 0, "toneDrillItems": 0, "writingAttempts": 0, "quizzes": 0, "lessonsCompleted": 0, "activityCount": 0 },
  "srs": { "dueToday": 23, "dueNow": 20, "newAvailableToday": 10, "newIntroducedToday": 0, "reviewedToday": 0, "nextDueAt": "…" },
  "dailyGoal": { "done": 0, "total": 33, "achieved": false },
  "vocabulary": { "totalInPath": 500, "introduced": 64, "learning": 52, "mature": 12 },
  "lessons": { "published": 5, "completed": 2, "inProgress": 1,
               "next": { "slug": "so-dem", "title": "Số đếm" },
               "lastCompleted": { "slug": "ban-than", "title": "Giới thiệu bản thân", "completedAt": "…", "unpracticedChars": 9 } },
  "writing": { "practicedChars": 24, "masteredChars": 5, "weakChars": 3, "totalChars": 297 },
  "tone": { "totalAnswered": 240, "accuracy": 0.82, "recommendedFocus": [2, 3] },
  "activity": [ { "date": "2026-06-21", "count": 0 }, "… 90 phần tử …", { "date": "2026-09-18", "count": 0 } ] }
```

- `srs`, `dailyGoal`, `vocabulary`, `lessons`, `writing`, `tone` có thể **vắng** (R-PG7). `streak`, `today`, `activity` luôn có.
- `dailyGoal` tính ở backend theo R-PG5 (`done = reviewedToday + newIntroducedToday`, `total = done + dueToday + newAvailableToday`, `achieved = dueToday == 0 && newAvailableToday == 0`); vắng khi `srs` vắng.
- `lessons.next` / `lastCompleted` vắng khi không có.

---

## 7. Phân rã feature

> Mỗi feature: đối chiếu §4.2 → DB chốt tên (§5.1) → Backend ‖ Content ‖ Frontend song song → review (Opus) → integration (Opus, commit local riêng, **không push**) → dừng. Commit message dạng `feat(chinese): F9 — …`.

### Feature F9: Bài học + quiz (học viên)
- **Mục tiêu:** học viên đọc 5 bài chủ đề, làm quiz chấm ở server, hoàn thành ≥ 80% thì từ của bài vào ôn tập.
- **Phạm vi**
  - DB (database-implement, Sonnet): migration `F9_Lessons` (§5.1.1), cấu hình EF.
  - BE (backend-implement, Sonnet): §5.2.1 toàn bộ (Domain, Application, `LessonImporter`, `LessonsController`, test).
  - Học liệu (content-implement, Sonnet): §5.4.1–§5.4.3, §5.4.6, dòng `original` §5.4.7; bộ học liệu test cho ApiTests (§5.2.1.6) phối hợp với backend.
  - FE (frontend-implement, **Fable**): §5.3.1; mục nav "Bài học".
- **Phụ thuộc:** F5 (study_events, TTS, pinyin FE), F6 (words, importer), F7 (thẻ SRS, K12, R-LS6).
- **Tiêu chí hoàn thành + cách tự test:**
  1. Cổng §9.1 sạch; test §5.2.1.6 xanh với `AF_TEST_PG` (báo số chạy/skip).
  2. `yarn --cwd content validate:chinese` sạch FAIL; bảng tổng cho thấy 5 bài, mỗi bài 8–15 từ, 6–8 câu, ≥ 30% nghe.
  3. Khởi động backend 2 lần ⇒ log "Nạp bài học: thêm 5…" rồi "bỏ qua 5"; `SELECT count(*) FROM content.lessons` = 5, `quiz_questions` không nhân đôi.
  4. `http://localhost:3280/bai-hoc` ở 375px: 5 thẻ, "Bài tiếp theo" = Chào hỏi; mở bài: hội thoại có pinyin dạng dấu, bật/tắt pinyin/nghĩa giữ sau F5 trang; nghe từng dòng được (hoặc hiện hướng dẫn khi thiếu giọng).
  5. DevTools Network: response `GET /chinese/api/lessons/chao-hoi` không có `correctOptionId`.
  6. Làm quiz sai nhiều ⇒ "Chưa đạt"; làm lại đạt ⇒ khối chúc mừng + "Đã thêm N từ vào ôn tập"; `/on-tap` thấy thẻ mới của bài được ưu tiên (trong hạn mức ngày).
  7. Tắt mạng lúc bấm "Nộp bài" ⇒ báo lỗi, bật lại bấm "Thử lại" ⇒ chỉ 1 dòng `quiz_attempts`.
  8. Phần tử chữ Hán có `lang="zh-CN"` (kiểm bằng DevTools); `yarn lint:ui` exit 0.
  - **Học thử ngay:** học bài 1–2, đạt quiz cả hai, hôm sau ôn thẻ của bài.

### Feature F8: Luyện viết chữ Hán
- **Mục tiêu:** viết từng chữ theo 3 bước trên điện thoại, hệ thống nhớ lỗi và chữ đã thuộc; dữ liệu nét cục bộ.
- **Phạm vi**
  - Học liệu (content-implement): `hanzi-writer-data@2.0.1` vào `content/package.json`, `build-hanzi-data.mjs` (§5.4.4), chạy script và **commit** `frontend/apps/chinese/public/hanzi-data/`, kiểm §5.4.5, `LICENSES/`, dòng `SOURCES.md` §5.4.7.
  - DB: migration `F8_Writing` (§5.1.2).
  - BE: §5.2.2.
  - FE (**Fable**): §5.3.2 (gồm `hanzi-writer@3.7.3`, `nginx.conf`, `public/licenses/`, nút ở F9, mục nav "Luyện viết"); cập nhật `deploy/VERIFY-DOCKER.md` (mục hanzi-data, ghi "chưa verify").
- **Phụ thuộc:** F6 (characters), F9 (bộ `lesson:<slug>`, nút ở kết quả quiz), F5 (study_events).
- **Tiêu chí hoàn thành + cách tự test:**
  1. Cổng §9.1 sạch, **kể cả** `yarn workspace @af/chinese build` (đụng dependency); test §5.2.2 xanh.
  2. Chạy lại `yarn --cwd content build:hanzi-data:chinese` ⇒ `git status` không đổi (đầu ra xác định). `validate:chinese` sạch.
  3. `ls frontend/apps/chinese/public/hanzi-data | wc -l` ≈ số chữ + 3; có `ARPHICPL.TXT`, `NOTICE.md`.
  4. Mở `/luyen-viet/爱` ở 375px: hoạt hình chạy; bước Tô theo viết bằng ngón tay/chuột không làm trang cuộn; sai 2 lần hiện gợi ý; hoàn tất ⇒ kết quả + `study_events(writing)`.
  5. DevTools Network trong cả phiên luyện: **không** có request tới `cdn.jsdelivr.net`; `/hanzi-data/7231.json` 200.
  6. Tự viết sạch hôm nay và ngày mai (hoặc chỉnh DB `last_clean_recall_date` về hôm qua để thử) ⇒ "Đã thuộc".
  7. React StrictMode dev: chuyển qua lại các bước/chữ không sinh 2 bảng viết chồng nhau.
  8. Tab "Bài học" chọn `chao-hoi` ⇒ chỉ chữ của bài; chữ thiếu dữ liệu nét (nếu có) hiện mờ kèm nhãn.
  - **Học thử ngay:** luyện toàn bộ chữ bài 1 qua 3 bước.

### Feature F10: Quản trị nội dung & duyệt nghĩa
- **Mục tiêu:** admin soạn/sửa/xuất bản bài, duyệt bài seed, duyệt nghĩa Việt và Hán Việt; nạp học liệu không đè dữ liệu đã sửa.
- **Phạm vi**
  - DB: migration `F10_ContentAdmin` (§5.1.3 — chỉ chỉ mục).
  - BE: §5.2.3; nếu importer F6 chưa tuân đủ R-CA10 thì sửa trong feature này (ghi rõ trong commit).
  - FE (**Fable**): §5.3.3; nút "Sửa bài"/"Sửa nghĩa" ở F9/F6 cho người có quyền; mục nav "Quản trị nội dung".
  - Học liệu: không (trừ khi test cần file bài bổ sung).
- **Phụ thuộc:** F9 (bảng bài học, validator dùng chung), F6 (words, importer), F3/F4 (quyền, `AppAutocomplete`, `useConfirm`).
- **Tiêu chí hoàn thành + cách tự test:**
  1. Cổng §9.1 sạch; test §5.2.3 xanh.
  2. Learner: không thấy menu; vào `/quan-tri/bai-hoc` ⇒ `/403`; gọi API admin ⇒ 403 JSON.
  3. Admin tạo bài "Mua sắm" → thêm khối hội thoại (pinyin sai số âm tiết ⇒ lỗi hiện đúng ô) → thêm 10 từ bằng ô tìm (gõ "mai" ra gợi ý) → 3 câu quiz → Xuất bản (có cảnh báo < 5 câu) → learner thấy bài.
  4. Mở cùng bài ở 2 tab, lưu tab 1 rồi lưu tab 2 ⇒ tab 2 báo xung đột, không ghi đè.
  5. Sửa tiêu đề bài seed rồi khởi động lại backend ⇒ tiêu đề giữ nguyên. Duyệt bài seed ⇒ chip "chưa duyệt" biến mất ở phía học viên.
  6. `/quan-tri/tu-vung` mặc định lọc `machine`, sắp theo lộ trình; sửa nghĩa 1 từ + duyệt hàng loạt 5 từ ⇒ tra từ điển thấy nghĩa mới, nhãn "chưa duyệt" mất; khởi động lại ⇒ không bị đè.
  7. Có thay đổi chưa lưu, bấm menu khác ⇒ hỏi xác nhận; bài lưu trữ ⇒ form khoá kèm giải thích.
  8. Ở 375px mọi màn admin dùng được (bảng chuyển thành thẻ); `yarn lint:ui` exit 0 (đặc biệt `autocomplete-slotprops-override`).
  - **Học thử ngay:** duyệt nghĩa 30 từ đầu lộ trình + duyệt 5 bài seed.

### Feature F11: Tổng quan tiến độ & streak
- **Mục tiêu:** trang chủ cho biết hôm nay cần làm gì, chuỗi ngày học, tiến độ từng mảng — theo múi giờ hồ sơ.
- **Phạm vi**
  - DB: không migration.
  - BE: §5.2.4.
  - FE (**Fable**): §5.3.4 (thay `HomePage`).
  - Học liệu: không.
- **Phụ thuộc:** F5, F7, F8, F9 (dùng dịch vụ K13, K14 và bảng của F8/F9); F4 (múi giờ hồ sơ).
- **Tiêu chí hoàn thành + cách tự test:**
  1. Cổng §9.1 sạch; test §5.2.4 xanh, gồm hai ca 23:59 / 00:01 giờ VN.
  2. Tài khoản mới: trang chủ hiện chuỗi 0, CTA "Học pinyin trước", "Bắt đầu bài học đầu tiên", lịch 90 ô trống, không lỗi.
  3. Ôn 1 thẻ ⇒ quay lại trang chủ (không F5) chuỗi thành 1, `studiedToday` bật, lịch ô hôm nay có màu.
  4. Chèn `study_events` cho 3 ngày trước liên tiếp ⇒ chuỗi hiển thị đúng; bỏ ngày hôm qua ⇒ chuỗi hiện tại 0 (nếu hôm nay chưa học).
  5. Đổi múi giờ hồ sơ sang `America/New_York` ⇒ "Hôm nay" đổi theo server; trình duyệt ở VN thấy dòng nhắc múi giờ hồ sơ.
  6. Làm hỏng tạm `content/chinese/data/pinyin` (đổi tên thư mục ở bản build) ⇒ trang chủ vẫn hiện, thiếu khối thanh điệu.
  7. 375px: lịch 90 ngày không tràn ngang; thẻ việc hôm nay bấm được; ≥ md lưới 2 cột.
  8. Không còn file `pages/HomePage.tsx`; chip trạng thái hệ thống chỉ hiện với `users.manage`.
  - **Học thử ngay:** dùng trang chủ làm điểm vào hằng ngày trong 1 tuần.

---

## 8. Thứ tự thực thi & phụ thuộc

```
(F5, F6, F7 xong) ─► F9 ─► F8 ─► F10 ─► F11
                     │      ▲      ▲
                     └──────┴──────┘  (F8 dùng bộ chữ theo bài; F10 dùng bảng + validator của F9)
```

| Feature | Bước 1 (tuần tự) | Bước 2 (song song) | Bước 3 |
|---|---|---|---|
| F9 | DB: migration + entity (chốt tên cột) | BE (service, importer, API, test) ‖ Content (schema, validate, 5 bài, bộ test) ‖ FE (dựng theo §6.1, dùng dữ liệu thật khi BE xong) | Review → Integration → commit |
| F8 | Content: chạy `build-hanzi-data` + commit `public/hanzi-data` (FE cần file để thử) ‖ DB migration | BE ‖ FE | Review → Integration → commit |
| F10 | DB: migration nhỏ | BE ‖ FE | Review → Integration → commit |
| F11 | — | BE ‖ FE (FE dựng theo §6.4 với dữ liệu giả rồi nối thật) | Review → Integration → commit |

- Frontend luôn gọi `Agent` với `subagent_type: "frontend-implement"` và **`model: "fable"`**.
- Content và backend F9 phối hợp **bộ học liệu test** (`tests/AntFarm.Chinese.ApiTests/TestData/content/lessons/`): backend sở hữu file, content soạn nội dung hợp lệ.
- Không gộp hai feature vào một commit. Migration mỗi feature một cái, sinh trong cùng commit với entity.

---

## 9. Tiêu chí hoàn thành + cách kiểm thử

### 9.1 Cổng bắt buộc mỗi feature (chạy từ gốc repo, macOS)

```bash
dotnet build backend/backend.slnx -v q                       # 0 error
AF_TEST_PG="Host=localhost;Port=5432;Username=postgres;Password=<...>" \
  dotnet test backend/backend.slnx                           # xanh; integration BẮT BUỘC đặt AF_TEST_PG và báo số chạy/skip
cd frontend
yarn workspace @af/chinese tsc -b                            # bắt buộc -b
yarn workspace @af/chinese test                              # vitest (inlineZh, charData, heatmap, todayTasks, slug)
yarn workspace @af/chinese build                             # F8 bắt buộc (dependency mới); F9–F11 nên chạy
yarn lint:ui                                                 # exit 0
cd ..
yarn --cwd content validate:chinese                          # F8, F9
dotnet ef migrations script --idempotent \
  --project backend/services/chinese-backend/src/AntFarm.Chinese.Infrastructure \
  --startup-project backend/services/chinese-backend/src/AntFarm.Chinese.Api   # soát SQL: không có cột "xmin" thật
```

Kèm: `git status` không có bí mật (`appsettings.Development.json`, `.secrets/`, `.env`); migration chạy được trên DB trống **và** DB dev đang có dữ liệu F3–F7; khởi động backend 2 lần liên tiếp không nhân đôi dữ liệu học liệu.

### 9.2 Soát review (Opus) — điểm hay sai

- `DateTime` ghi vào `timestamptz` đều `Kind=Utc`; "hôm nay" lấy từ múi giờ người dùng (K5), không `DateTime.Today`/UTC.
- `study_events` ghi cùng transaction với dữ liệu nghiệp vụ; `kind`/`quantity` đúng R-LS5, R-LS7, R-W4.
- Chi tiết bài cho học viên không lộ đáp án (R-LS10).
- `[RequirePermission]` đúng quyền trên controller mới; không có `new string Policy`.
- Importer/seed không ném; bài lỗi không chặn bài khác; không đè dữ liệu sửa tay (R-LS14, R-CA10).
- Frontend: `charDataLoader` luôn được truyền; không `Dialog`/`Drawer` trần; `renderInput` trải `params.slotProps` trước; tab dùng `useTabParam`; `lang="zh-CN"` trên chữ Hán; không `uuid`; peer dependency khai ở app; `hanzi-writer` ghim `3.7.3`.
- Học liệu: nội dung tự soạn, không chép giáo trình; `ARPHICPL.TXT` nguyên văn; `SOURCES.md` đủ dòng.

### 9.3 Danh sách file theo feature (tạo/sửa)

**F9**
- `backend/services/chinese-backend/src/AntFarm.Chinese.Domain/Lessons/{Lesson,LessonBlock,LessonWord,QuizQuestion,Payloads}.cs`, `Learning/{LessonProgress,QuizAttempt,QuizGrader}.cs`
- `…/AntFarm.Chinese.Application/Lessons/{LessonJson,LessonContentValidator,LessonPublishRules,LessonQueryService,LessonProgressService,QuizSubmissionService}.cs`, `Lessons/Dtos/*.cs`, `Lessons/Validators/SubmitQuizRequestValidator.cs`, `Common/Abstractions/IChineseDbContext.cs`, `DependencyInjection.cs`
- `…/AntFarm.Chinese.Infrastructure/Persistence/Configurations/{Lesson,LessonBlock,LessonWord,QuizQuestion,LessonProgress,QuizAttempt}Configuration.cs`, `Persistence/ChineseDbContext.cs`, `Persistence/Migrations/*_F9_Lessons.cs` (+ snapshot), `Content/{LessonImporter,LessonFileModel}.cs`, `DependencyInjection.cs`
- `…/AntFarm.Chinese.Api/Features/Lessons/LessonsController.cs`, `Program.cs` (gọi importer)
- `backend/services/chinese-backend/tests/AntFarm.Chinese.UnitTests/Lessons/*.cs`, `tests/AntFarm.Chinese.ApiTests/Lessons/*.cs`, `tests/AntFarm.Chinese.ApiTests/TestData/content/lessons/*.json` (+ csproj copy)
- `content/chinese/schemas/lesson.schema.json`, `content/chinese/data/lessons/0{1..5}-*.json`, `content/chinese/scripts/validate.mjs`, `content/chinese/SOURCES.md`
- `frontend/apps/chinese/src/features/lessons/**`, `src/router.tsx`, `src/layout/AppShell.tsx`

**F8**
- `content/package.json`, `content/yarn.lock`, `content/chinese/scripts/{build-hanzi-data,validate}.mjs`, `content/chinese/LICENSES/{ARPHICPL.TXT,hanzi-writer-MIT.txt}`, `content/chinese/SOURCES.md`
- `frontend/apps/chinese/public/hanzi-data/**` (sinh), `frontend/apps/chinese/public/licenses/hanzi-writer.LICENSE.txt`
- Domain `Learning/{WritingAttempt,CharacterWritingStats,WritingModes,MasteryStatuses}.cs`; Application `Writing/{WritingService,WritingCharacterQueryService}.cs`, `Writing/Dtos/*`, `Writing/Validators/*`; Infrastructure `Persistence/Configurations/{WritingAttempt,CharacterWritingStats}Configuration.cs`, migration `*_F8_Writing.cs`; Api `Features/Writing/WritingController.cs`; test `UnitTests/Writing/*`, `ApiTests/Writing/*`
- `frontend/apps/chinese/package.json` (+ `frontend/yarn.lock`), `src/features/writing/**`, `src/features/lessons/components/quiz/QuizResultView.tsx`, `src/features/lessons/components/LessonWordList.tsx`, `src/router.tsx`, `src/layout/AppShell.tsx`, `frontend/apps/chinese/nginx.conf`
- `deploy/VERIFY-DOCKER.md`

**F10**
- Domain `Content/Word.cs` (bổ sung), `Lessons/Lesson.cs` (nếu cần); Application `Admin/Content/{LessonAdminService,WordReviewService}.cs`, `Admin/Content/Dtos/*`, `Admin/Content/Validators/*`; Infrastructure cấu hình `Word` (xmin, edited_by, chỉ mục), migration `*_F10_ContentAdmin.cs`, (nếu cần) `Content/ContentImporter.cs`; Api `Features/Admin/{AdminLessonsController,AdminWordsController}.cs`; test `UnitTests/Admin/*`, `ApiTests/Admin/*`
- `frontend/apps/chinese/src/features/admin-content/**`, `src/features/lessons/pages/LessonDetailPage.tsx` (nút Sửa bài), trang chi tiết từ điển F6 (nút Sửa nghĩa), `src/router.tsx`, `src/layout/AppShell.tsx`

**F11**
- Domain `Progress/StreakCalculator.cs`; Application `Progress/{ProgressOverviewService}.cs`, `Progress/Dtos/*`; Api `Features/Progress/ProgressController.cs`; test `UnitTests/Progress/StreakCalculatorTests.cs`, `ApiTests/Progress/*`
- `frontend/apps/chinese/src/features/progress/**`, `src/router.tsx`, xoá `src/pages/HomePage.tsx`, di chuyển dùng `features/system/components/ServiceStatusChip.tsx`

### 9.4 Nghiệm thu end-to-end

Chạy compose dev (Postgres) + identity + chinese + gateway + app (`README.md`), thao tác ở 1366px và 375px (DevTools thiết bị iPhone SE / cảm ứng), đối chiếu tiêu chí §7 từng feature; kèm "Học thử ngay".

---

## 10. Rủi ro / quyết định mở / ràng buộc

### 10.1 Rủi ro mới

| # | Rủi ro | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| RK34 | Tên bảng/cột/dịch vụ F5–F7 thực tế khác giả định §4.2 | Code F8–F11 không build hoặc ghi sai sổ hoạt động | Bước 0 mỗi feature: đối chiếu §4.2 với tài liệu F4–F7 + code; lệch thì theo code đã commit, ghi chú trong commit |
| RK35 | Tài liệu F6–F7 giả định `RecordAsync` **thiếu** tham số `occurredAtUtc` (khác F4–F5); F7 chưa đặt tên dịch vụ thêm thẻ/tóm tắt SRS (K12, K13) | F7 code lệch chữ ký ⇒ F9/F11 phải sửa theo; nguy cơ nhân đôi logic thêm thẻ | Orchestrator báo agent F7 theo chữ ký F4–F5; F9 tách `EnsureCardsAsync` nếu F7 chưa có (ghi trong commit). (`kind` không CHECK ở DB — rủi ro constraint đã hết) |
| RK36 | `hanzi-writer` tự gọi CDN khi quên `charDataLoader` | Phụ thuộc mạng ngoài, lệch phiên bản dữ liệu, rò IP người học tới bên thứ ba | R-W1; một component bọc duy nhất; tiêu chí §7 F8 mục 5 |
| RK37 | Vite dev/nginx trả `index.html` cho file nét không tồn tại | hanzi-writer lỗi parse khó hiểu | `try_files $uri =404`; loader kiểm `content-type`; manifest kiểm trước |
| RK38 | Vuốt để viết làm cuộn trang trên điện thoại | Không viết được trên mobile | `touch-action: none` vùng viết; tiêu chí F8 mục 4 |
| RK39 | StrictMode React 19 tạo hai bảng viết chồng nhau | Nét bị chấm sai, giao diện rối | Cleanup xoá DOM + `cancelQuiz`; tiêu chí F8 mục 7 |
| RK40 | Bài seed do agent soạn có lỗi tiếng Trung/nghĩa | Người số 0 học sai | `review_status='machine'` + chip; F10 duyệt; validator kiểm pinyin/số âm tiết/phủ chữ |
| RK41 | Admin sửa quiz khi học viên đang làm | Nộp bài lỗi | `422 QUIZ_CHANGED` + frontend tải lại (R-LS8) |
| RK42 | Importer bài học đè bài admin vừa sửa | Mất công soạn | R-LS14 kiểm `edited_at`/`review_status`; test F10 mục 7 |
| RK43 | Đổi múi giờ sau khi đã có lịch sử ⇒ ngày trong `study_events` (múi giờ cũ) lệch "hôm nay" (múi giờ mới) | Chuỗi có thể đứt/nối sai 1 ngày quanh thời điểm đổi | Chấp nhận (R-T3 HĐG: không viết lại lịch sử); `StreakCalculator` bỏ qua ngày tương lai cho `current` |
| RK44 | Ảnh frontend tăng ≈ 1 MB vì dữ liệu nét | Build/đẩy ảnh chậm hơn chút | Chỉ tập con HSK 1; gzip; cache 7 ngày |
| RK45 | Arphic PL hiểu sai (tập con + đổi tên file có tính là "sửa đổi"?) | Vi phạm nghĩa vụ giấy phép | Không sửa nội dung file; `NOTICE.md` ghi rõ thay đổi (thoả §2a nếu bị coi là sửa); `ARPHICPL.TXT` nguyên văn cạnh dữ liệu; tập con vẫn công khai trong repo/ảnh (thoả §2b) |
| RK46 | `xmin` + thay bảng con không đổi version bài | Hai admin ghi đè khối của nhau không báo | R-CA2 chạm dòng `lessons` mỗi lần ghi; test F10 mục 4 |
| RK47 | Thẻ `source='lesson'` bị chen sau thẻ lộ trình nếu F7 không ưu tiên | Học xong bài nhưng vài ngày sau mới gặp thẻ | R-LS6 / K12 — đưa vào tài liệu F7 hoặc sửa ở F9 (ghi rõ) |
| RK48 | Từ gợi ý bài seed không có trong HSK 3.0 cấp 1 thực tế | Validator FAIL, content phải đổi từ | §5.4.6 cho phép thay từ/đưa vào glossary; con số 8–15 giữ nguyên |

### 10.2 Quyết định đã dùng mặc định BA (người dùng giao chạy một mạch, 17/09/2026)

| Mã | Mặc định đã áp | Mục |
|---|---|---|
| **D3** | Đóng gói tập con `hanzi-writer-data@2.0.1` (chỉ chữ trong `characters.json`) vào `apps/chinese/public/hanzi-data/` bằng script, tên file theo mã Unicode hex, nội dung nguyên byte, kèm `ARPHICPL.TXT` + `NOTICE.md`; `hanzi-writer@3.7.3` (MIT). Giấy phép đã xác minh trên gói npm ngày 17/09/2026 | R-W1, §5.4.4 |
| **D7** | Quiz ≥ 80% (so nguyên) là hoàn thành; không khoá tuần tự; hoàn thành lần đầu ⇒ thêm thẻ SRS `source='lesson'` cho từ chưa có thẻ | R-LS3–R-LS5 |
| **D8** | Ngày có học = ≥ 1 dòng `study_events` (mọi kind, `quantity>0`); mục tiêu ngày hiển thị riêng (hết thẻ đến hạn + hết thẻ mới trong hạn mức); không đóng băng streak | R-PG1–R-PG5 |
| **D15** | 5 bài seed tự soạn `chao-hoi`, `ban-than`, `so-dem`, `gia-dinh`, `thoi-gian`, nạp `published` + `review_status='machine'`, duyệt ở F10 | §5.4.6, R-LS2 |

Mặc định BA phụ (không có mã trong HĐG): nạp bài theo `slug` thay vì "chỉ khi bảng trống" (R-LS14) · xoá bài seed = lưu trữ (R-CA7) · khoá slug sau lần xuất bản đầu (R-CA8) · định nghĩa "thuộc chữ" = 2 ngày tự viết sạch (R-W5) · ngưỡng gợi ý 2/3 lần sai (R-W2) · quiz phản hồi cuối bài, không từng câu (§5.3.1) · thẻ bài học ưu tiên trong hàng đợi thẻ mới (R-LS6) · khuyến nghị (không chặn) học pinyin trước khi `totalAnswered < 40`.

### 10.3 Câu hỏi còn mở (không chặn)

- Có muốn phản hồi đúng/sai **ngay từng câu** quiz không (cần API chấm từng câu, lộ đáp án dần)? Mặc định: chấm cuối bài.
- Có cần luyện viết cả chữ trong `glossary`/ngoài HSK 1 (phải mở rộng `characters.json` và tập dữ liệu nét)? Mặc định: không.

### 10.4 Ràng buộc dự án phải nhắc agent thực thi

- Build/test sạch theo §9.1 (`dotnet build backend/backend.slnx -v q`, `dotnet test`, `yarn workspace @af/chinese tsc -b`, `build` khi đụng dependency, `lint:ui`); commit local riêng từng feature, **không push**, nhánh `develop`.
- DDD 4 lớp; phân quyền cục bộ trong DB `af_chinese` (`study.use`, `content.manage`); frontend đọc quyền từ `/chinese/api/me`; `RequirePermissionAttribute` gán `Policy` trong constructor; nút ẩn/khoá theo quyền phải có lời giải thích.
- Npgsql `timestamptz` chỉ nhận `DateTime` `Kind=Utc`; tham số ngày từ query string là `Unspecified` ⇒ `SpecifyKind` + nửa hở; "hôm nay" theo `access.users.time_zone`.
- MUI v9 (`slotProps`, shorthand trong `sx`); `AppDialog`/`AppDrawer` thay `Dialog`/`Drawer`; Autocomplete trải `params.slotProps` trước; `useTabParam`; không `uuid` (dùng `crypto.randomUUID()`); `@af/*` là workspace source, peer dependency khai ở app; `tsconfig.app.json` không `baseUrl`.
- Seed/import idempotent, không ném; học liệu chỉ nguồn có giấy phép rõ, ghi `content/chinese/SOURCES.md` + `LICENSES/`; nội dung tự soạn không chép giáo trình; nghĩa dịch máy giữ `machine` tới khi duyệt.
- Mobile-first 375px; `lang="zh-CN"` cho chữ Hán; pinyin lưu số thanh, hiển thị dấu.
- Dockerfile/nginx cập nhật cùng feature và ghi "chưa verify" (F8: `nginx.conf` + `VERIFY-DOCKER.md`).
- Frontend do agent `frontend-implement` chạy **model Fable** (`model: "fable"`).
