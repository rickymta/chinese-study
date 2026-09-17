# AntFarm F6 (Học liệu HSK 3.0 cấp 1 + tra từ) & F7 (Flashcard SRS FSRS-6) — Hợp đồng thực thi chi tiết

- Ngày: 2026-09-17 · Loại: tạo mới (bổ sung chi tiết cho hợp đồng gốc) · Service/app: `chinese-backend`, `frontend/apps/chinese`, `frontend/packages/ui`, `content/chinese/` · Module: `content` (từ vựng, chữ, nhập học liệu), `dictionary` (tra từ), `learning/srs` (ôn thẻ)
- Hợp đồng gốc: `docs/agent-workflow/2026-09-16-antfarm-nen-tang-tieng-trung-mvp-hop-dong-thuc-thi.md` (§3.5, §3.6, §5.1.4, §5.1.5, §5.2.4, §5.3.3, §5.4, §6.5, §6.6, §7, §10). **File này thay thế phần "mức thiết kế" của F6/F7 trong hợp đồng gốc**; chỗ nào khác nhau thì file này thắng (các điểm lệch có ghi "**Lệch hợp đồng gốc**").
- Tiền đề F4/F5: `docs/agent-workflow/2026-09-17-antfarm-f4-f5-chi-tiet.md` (đã đối chiếu tên thành phần lúc hoàn tất file này — §4). Tên nào trong code thật khác thì **theo code thật**, giữ nguyên hành vi mô tả ở đây.
- Người soạn: agent business-analysis (Opus). Người dùng giao chạy một mạch, không hỏi lại ⇒ các quyết định mở dùng **mặc định BA**, ghi **[BA-mặc định]**.
- Nguồn dữ liệu đã **xác minh bằng mạng ngày 17/09/2026** (commit, giấy phép, SHA-256 ở §5.4.1). Vector vàng FSRS **đã chạy thật** bằng `fsrs==6.3.2` (Python 3.12) — §5.2.7.

> Khi context bị nén: đọc §3 (quy tắc), §7 (feature đang làm), rồi mục §5/§6 mà feature trỏ tới.

---

## 1. Bối cảnh & mục tiêu

### 1.1 Bối cảnh

F0–F1 đã commit; F2/F3 đang/sẽ code; F4/F5 đang được BA chi tiết hoá. F6 là feature đầu tiên đưa **học liệu thật** vào hệ thống; F7 dùng kho từ đó để lập lịch ôn bằng FSRS-6. Hợp đồng gốc chặn F6 bởi D5, D6 — nay giải bằng mặc định BA (§3.1).

### 1.2 Mục tiêu phần mềm

- F6: kho **500 mục từ HSK 3.0 cấp 1** (chuẩn GF0025-2021) + khoảng **300 chữ Hán** có pinyin, phồn thể, Hán Việt, nghĩa Việt (CVDICT, nhãn "chưa duyệt"), nghĩa Anh (CC-CEDICT); nạp idempotent lúc khởi động; tra được bằng chữ Hán, pinyin số, pinyin dấu, pinyin không dấu, nghĩa Việt có/không dấu, âm Hán Việt; màn tra từ + chi tiết từ + chi tiết chữ dùng tốt ở 375px.
- F7: thẻ `hanzi_to_meaning` cho mỗi từ theo lộ trình; lập lịch **FSRS-6** (21 trọng số mặc định, độ nhớ mục tiêu 0,9, **tắt fuzz**); 10 thẻ mới/ngày; "đến hạn hôm nay" theo múi giờ người học; ôn thẻ trên điện thoại (nghe TTS, lật thẻ, 4 nút Quên/Khó/Được/Dễ); ghi `study_events` để F11 tính streak.

### 1.3 Mục tiêu học tập (nghiệp vụ sư phạm)

| Bước | Người học làm gì | Vì sao thứ tự này | Đo tiến bộ |
|---|---|---|---|
| Sau G0 (pinyin, F5) | Tra từ: gõ pinyin/nghĩa Việt/Hán Việt để tìm chữ | Đã đọc được pinyin ⇒ tra từ là công cụ tự học đầu tiên; **Hán Việt** là cầu nối riêng của người Việt (爱 = *ái* ⇒ "ái mộ") giúp nhớ mặt chữ và đoán nghĩa | — |
| G1 bắt đầu | Mỗi ngày ~10 thẻ mới + toàn bộ thẻ đến hạn | Lặp lại ngắt quãng rẻ nhất để nhớ lâu; 10 thẻ/ngày ⇒ ~50 ngày hết 500 từ, tải ôn ổn định ~10–15 phút/ngày | Thẻ đã ôn/ngày, số thẻ "vững" (`review` và độ ổn định ≥ 21 ngày) |
| Thứ tự thẻ mới | Tầng 1: từ thuộc **cả** HSK 3.0–1 và HSK 2.0–1 (136 từ) → tầng 2: thuộc đề cương thi HSK 2026 cấp 1 (131 từ) → tầng 3: còn lại (233 từ); trong tầng theo tần suất | Từ lõi xuất hiện ở mọi giáo trình/đề thi, tần suất cao ⇒ dùng được ngay trong bài học F9 và gặp lại nhiều nhất (RK15) | `path_order` |
| Mặt thẻ | Trước: chữ Hán (+ tự đọc TTS). Sau: pinyin dấu, Hán Việt, nghĩa Việt | Người mới cần nhận **mặt chữ + âm** trước; nghe cùng lúc để khớp âm–chữ; tự chấm 4 mức là đầu vào chuẩn của FSRS | Tỉ lệ "Quên" giảm dần |
| Nhãn "chưa duyệt" | Nghĩa dịch máy (CVDICT) hiện nhãn | Người số 0 không tự phát hiện nghĩa sai (RK4); nhãn nhắc đối chiếu nghĩa Anh; duyệt ở F10 | Số từ `reviewed` (F10) |

---

## 2. Phạm vi

### 2.1 In-scope

- **F6.1 Học liệu**: `content/chinese/scripts/fetch-sources.mjs`, `build-hsk.mjs`, mở rộng `validate.mjs`; tệp biên soạn `content/chinese/sources/*.json`; đầu ra `data/vocabulary/hsk-words.json`, `data/characters/characters.json`; schema JSON; `SOURCES.md`, `LICENSES/`.
- **F6.2 Backend + DB**: migration `F6_Vocabulary` (schema `content`), `ContentImporter`, `DictionaryService`, `DictionaryController` (§6.1).
- **F6.3 Frontend**: `/tu-dien`, `/tu-dien/:id`, `/tu-dien/chu/:hanzi`; `useBackTo`, `useScrollRestore` trong `@af/ui`.
- **F7.1 Backend + DB**: migration `F7_Srs`, `FsrsScheduler` + vector vàng, hàng đợi, chấm thẻ idempotent, cài đặt học tập, `study_events`.
- **F7.2 Frontend**: `/on-tap`, `/on-tap/phien`, tab `/ho-so?tab=hoc-tap`, nút "Thêm vào ôn tập" ở chi tiết từ, huy hiệu số thẻ đến hạn trên menu.

### 2.2 Out-of-scope

- HSK 3.0 cấp 2 trở lên (dữ liệu nguồn có sẵn nhưng không nạp); câu ví dụ; âm thanh người thật (chỉ TTS trình duyệt).
- Tìm theo nghĩa **tiếng Anh** (hiển thị có, không tìm) — để sau.
- Duyệt/sửa nghĩa (F10 — F6 chỉ chuẩn bị cột `edited_at`, `edited_by`, trạng thái).
- Loại thẻ khác `hanzi_to_meaning` (nghe→chữ, nghĩa→chữ); optimizer tham số FSRS; fuzz; lên lịch lại (reschedule) khi đổi độ nhớ mục tiêu; hoàn tác (undo) lượt chấm; ôn offline qua PWA (chỉ giữ hàng đợi gửi lại trong phiên).
- Thẻ tự thêm khi hoàn thành bài học (`source='lesson'`) — F9 (F7 chỉ chuẩn bị giá trị `lesson`).

---

## 3. Quy tắc nghiệp vụ

### 3.1 Quyết định mở đã giải bằng mặc định

- **D5 — nguồn nghĩa Việt [BA-mặc định]**: **CVDICT** (github.com/ph0ngp/CVDICT) — giấy phép **đã xác minh**: README và đầu tệp `CVDICT.u8` ghi rõ *Creative Commons Attribution-ShareAlike 4.0 International* (tác giả Phong Phan; dẫn xuất CC-CEDICT; dịch bằng mô hình ChatGPT-4o đã fine-tune rồi rà tay). Mục không có trong CVDICT hoặc bị lọc rỗng ⇒ **dịch máy** từ nghĩa Anh (tệp `sources/meaning-vi-machine.json`). **Mọi nghĩa đều `meaningViStatus = "machine"`** tới khi được duyệt ở F10; `meaningViSource` = `cvdict` | `machine`.
- **D6 — CC BY-SA 4.0 [BA-mặc định]**: **chấp nhận** cho dữ liệu dẫn xuất CC-CEDICT/CVDICT/Wiktionary. Phạm vi áp dụng: **chỉ dữ liệu học liệu trong `content/chinese/data/` và `content/chinese/sources/`**; không lan sang mã nguồn (script, backend, frontend). Nghĩa vụ: ghi công, ghi đường dẫn giấy phép, ghi "đã chỉnh sửa", phân phối dữ liệu dẫn xuất cùng giấy phép (§5.4.6). App hiển thị dòng ghi nguồn ở chi tiết từ (§5.3.1).

### 3.2 Học liệu (bổ sung R-C1…R-C9)

- R6-1. **Danh sách chuẩn cấp 1 = danh sách chính thức 500 dòng** (`elkmovie/hsk30/wordlist.txt`, mục `一级词汇表`, OCR từ PDF của Bộ Giáo dục TQ). Mỗi dòng = **một mục từ**. Build phải ra **đúng 500** mục `hsk3Level = 1`; khác ⇒ build lỗi.
- R6-2. Dòng có biến thể `爸爸｜爸` ⇒ mục chính `爸爸`, `variants = ["爸"]`. Dòng `有（一）些` (ngoặc **giữa** từ) ⇒ mục chính `有些`, `variants = ["有一些"]`. Dòng `第（第二）`, `们（朋友们）`, `子（桌子）` (ngoặc **cuối**, nội dung không phải nhãn từ loại) ⇒ `usageNote = "第二"`… Ngoặc cuối là nhãn từ loại (ký tự thuộc `名动形副代量数介连助叹` và `、`) ⇒ chỉ là gợi ý chọn cách đọc, không lưu.
- R6-3. Chữ trùng khác cách đọc là **hai mục khác nhau** theo khoá `(simplified, pinyin)`: 地 `de5`/`di4`, 干 `gan1`/`gan4`, 还 `hai2`/`huan2`.
- R6-4. **Cách đọc chuẩn**: mục có đúng một cách đọc chữ thường trong nguồn ⇒ lấy luôn (438 mục). Mục đa âm (62 mục) ⇒ **bắt buộc** có trong `sources/hsk1-overrides.json` (bảng §5.4.4). Mục chỉ có cách đọc viết hoa (北京, 汉语, 中国, 中文, 星期日, 星期天 — quy ước CC-CEDICT) ⇒ giữ nguyên chữ hoa (R-C2).
- R6-5. Âm tiết thiếu số thanh trong nguồn (vd `zhen1 de`) ⇒ thêm `5` + WARN.
- R6-6. **Phồn thể**: lấy `traditional` từ các dạng (form) cùng cách đọc, lọc bằng Unihan `kTraditionalVariant`; còn >1 ứng viên ⇒ lấy từ override (§5.4.4); vẫn không có ⇒ ứng viên đầu + WARN. `traditional = null` khi trùng `simplified`.
- R6-7. **Hán Việt cấp chữ** lấy từ tệp biên soạn `sources/han-viet.json` (bắt buộc phủ **mọi** chữ trong kho). Lý do không lấy thẳng Unihan `kVietnamese` như hợp đồng gốc (**Lệch hợp đồng gốc**, đo ngày 17/09): `kVietnamese` phủ 263/300 chữ nhưng lẫn âm Nôm (吧 = *và*, 吃 = *khật*, 冷 = *lạnh*, 姐 = *thư*), thiếu 爱/很/您/这; Wiktionary `hanviet=` chỉ phủ 157/300 và có giá trị lệch (坏 = *phôi*). Hai nguồn này là **tư liệu đối chiếu** cho người/agent biên soạn (script sinh báo cáo ứng viên — §5.4.3).
- R6-8. **Hán Việt cấp từ** = nối âm Hán Việt từng chữ theo cách đọc của âm tiết tương ứng (`byPinyin`), bỏ âm rỗng (儿 trong 儿化 `r5`); `hanVietStatus = "derived"`.
- R6-9. **`path_order`** (R-C9 tinh chỉnh) **[BA-mặc định]**: tầng 1 = có `old-1` (HSK 2.0–1); tầng 2 = có `newest-1` (đề cương thi HSK 2026 cấp 1); tầng 3 = còn lại; trong tầng sắp `frequencyRank` tăng dần, hoà thì `officialIndex`. Đánh số 1..500 liên tục, duy nhất.
- R6-10. Lưu thêm `hskExam2026Level` (cấp theo đề cương thi 2026 của nguồn, 1..7) để hiển thị và dùng cho R6-9. **Không** đổi chuẩn từ vựng (vẫn HSK 3.0 GF0025-2021 — D1).
- R6-11. **Nạp** (R-C8 chi tiết): upsert theo `(simplified, pinyin)` (từ) và `hanzi` (chữ); **không ghi đè** nhóm trường "được duyệt" khi dòng đã `reviewed` hoặc `edited_at IS NOT NULL`: từ ⇒ `meanings_vi`, `meaning_vi_status`, `meaning_vi_source`, `han_viet`, `han_viet_plain`, `han_viet_status`; chữ ⇒ `han_viet`, `han_viet_by_pinyin`, `han_viet_status`. Trường khác luôn cập nhật theo tệp. Mục có trong DB mà tệp không còn ⇒ **giữ nguyên**, log Warning (không xoá — có thể đang có thẻ SRS).
- R6-12. Tệp sinh ra phải **tất định**: không nhúng thời điểm build; chạy lại build không đổi nguồn ⇒ tệp giống hệt byte ⇒ backend bỏ qua nhờ hash.

### 3.3 Tra từ

- R6-20. Cần quyền `study.use` cho mọi endpoint từ điển.
- R6-21. Truy vấn `q` sau chuẩn hoá (NFC, trim, gộp khoảng trắng) dài ≤ 64 ký tự; rỗng ⇒ liệt kê theo lộ trình (`hsk3_level`, `path_order`).
- R6-22. Một truy vấn chạy **mọi cách hiểu áp dụng được** rồi hợp kết quả, xếp hạng theo §5.2.3 (vd `ái` vừa là pinyin `ai2` vừa là Hán Việt *ái*).
- R6-23. Pinyin có thanh (số hoặc dấu) ⇒ **chỉ** khớp đúng thanh; không thanh ⇒ khớp mọi thanh. Thanh nhẹ nhập `5` hoặc `0`.
- R6-24. Tiếng Việt có dấu ⇒ khớp **đúng dấu** (`yêu` không ra `yếu`); không dấu ⇒ khớp bỏ dấu (`yeu` ra cả `yêu`, `yếu`). `đ` ≡ `d` khi bỏ dấu.

### 3.4 SRS (bổ sung R-L2, R-L3)

- R7-1. FSRS-6 đúng thuật toán `py-fsrs` **v6.3.2** (§5.2.6): 21 trọng số mặc định, `desired_retention` theo cài đặt (mặc định 0,90; 0,80–0,97), bước học `[1 phút, 10 phút]`, học lại `[10 phút]`, `maximum_interval = 36500`, **tắt fuzz [BA-mặc định]** (tất định, test được; tải 10 thẻ/ngày nên dồn ngày không đáng kể).
- R7-2. Một từ = một thẻ `hanzi_to_meaning` / người học (`UNIQUE (user_id, word_id, card_type)`). Thẻ lộ trình (`source='path'`) được **tạo lười** khi lấy hàng đợi; thẻ tự thêm `source='manual'`.
- R7-3. **"Hôm nay"** = ngày lịch theo `access.users.time_zone` tại thời điểm tính; `endOfTodayUtc` = 00:00 ngày mai giờ địa phương đổi sang UTC (giờ không hợp lệ do DST ⇒ cộng 1 giờ tới khi hợp lệ).
- R7-4. `dueToday` = thẻ `state <> 'new'`, không tạm dừng, `due_at < endOfTodayUtc`. `dueNow` = như trên nhưng `due_at <= nowUtc`.
- R7-5. `newIntroducedToday` = số thẻ có `first_reviewed_local_date = hôm nay`. `newAvailableToday = max(0, min(daily_new_cards − newIntroducedToday, số thẻ mới khả dụng))`; thẻ mới khả dụng = thẻ `state='new'` không tạm dừng + từ trong lộ trình (`hsk3_level=1 AND path_order IS NOT NULL`) chưa có thẻ.
- R7-6. Giới hạn ôn: `reviewsDoneToday` = số log hôm nay có `state_before = 'review'`; `reviewLimitRemaining = max(0, daily_review_limit − reviewsDoneToday)`. Chỉ áp ở **hàng đợi** (không chặn POST chấm).
- R7-7. Thứ tự hàng đợi: (1) `learning`/`relearning` có `due_at <= now` (theo `due_at`); (2) `review` có `due_at < endOfTodayUtc` (theo `due_at`, tối đa `reviewLimitRemaining`); (3) thẻ mới (thẻ `new` có sẵn theo `created_at`, rồi từ lộ trình theo `path_order`) tối đa `newAvailableToday`; (4) "học trước" `learning`/`relearning` có `due_at <= now + 20 phút`. Cắt theo `limit` (1..50, mặc định 20).
- R7-8. Chấm thẻ **idempotent** theo `clientReviewId` (uuid do client sinh bằng `crypto.randomUUID()`): trùng id cùng thẻ ⇒ trả lại kết quả cũ (`duplicate: true`), không tạo log thứ hai; trùng id khác thẻ ⇒ 409 `CLIENT_REVIEW_ID_CONFLICT`.
- R7-9. Thời điểm chấm = **giờ server** lúc nhận (không tin giờ client). `durationMs` kẹp 0..600000.
- R7-10. Thẻ mới được chấm khi `newIntroducedToday >= daily_new_cards` ⇒ 422 `NEW_CARD_LIMIT_REACHED`. Thẻ tạm dừng ⇒ 422 `CARD_SUSPENDED`.
- R7-11. `lapses + 1` khi `state_before = 'review'` và rating `again`. `reps + 1` mọi lượt.
- R7-12. Mỗi lượt chấm ghi **một** `study_events` (`kind='srs_review'`, `quantity=1`, `correct = rating<>again ? 1 : 0`, `ref_id = card_id`) **cùng transaction** qua `IStudyActivityRecorder` (F5).
- R7-13. "Vững" (`mature`) = `state='review' AND stability >= 21`.
- R7-14. Đổi `desired_retention` chỉ ảnh hưởng các lượt chấm sau; không lên lịch lại thẻ cũ.

---

## 4. Hiện trạng liên quan (kiểm chứng 17/09/2026, `develop` @ `fbbe48a`)

| Thứ | Hiện trạng | F6/F7 cần |
|---|---|---|
| `backend/services/chinese-backend/src/AntFarm.Chinese.Infrastructure/Persistence/ChineseDbContext.cs` | DbContext rỗng (F3 thêm `access.*`) | Thêm DbSet `content.*` (F6), `learning.*` SRS (F7) |
| `.../Application/Common/Abstractions/IChineseDbContext.cs` | Chỉ `SaveChangesAsync` | Thêm DbSet tương ứng |
| `.../Api/Program.cs` | Khối `AutoMigrate` có chỗ "F3+: seeder chạy ở đây — bắt mọi exception" | Gọi `ContentImportRunner` sau seeder F3, trước `RunAsync` |
| `.../Infrastructure/DependencyInjection.cs` | `UseNpgsql` + snake_case + history `public.__ef_migrations_history` | Đăng ký importer; `HasPostgresExtension("pg_trgm")` trong model |
| `backend/Directory.Packages.props` | EF 10.0.7, Npgsql EF 10.0.0, xunit 2.9.3, FluentAssertions 7.2.0 | F7 thêm `Microsoft.Extensions.TimeProvider.Testing` **10.10.0** (chỉ project test) |
| `deploy/postgres/init/01-create-databases.sh` | DB `af_chinese` **OWNER af_chinese** | `pg_trgm` là extension *trusted* (PG ≥ 13) ⇒ chủ DB tự `CREATE EXTENSION` được |
| `.gitignore` | Đã có `content/**/.raw/`, `content/node_modules/` | Không đổi |
| `content/` | **Chưa có** (F5 tạo khung, `package.json` ajv, `validate.mjs`, `SOURCES.md`) | F6.1 mở rộng |
| `frontend/apps/chinese/src/api/clients.ts` | `chineseApi` (`/chinese/api`) từ F2 | Dùng cho mọi lời gọi |
| `frontend/packages/ui/src/index.ts` | theme, `AppLayout`, `PageContainer`, `ErrorPage`, `NotFoundPage`, `LangText` | F4 thêm `AppDialog/AppDrawer/Confirm/Toast`; F5 thêm `speech`/`useSpeech`, `useTabParam`; F6 thêm `useBackTo`, `useScrollRestore`; F7 thêm prop `hideBottomNav` cho `AppLayout` nếu chưa có |
| `frontend/apps/chinese/src/router.tsx` | `/`, `/404`, `*` | Thêm route F6/F7 |

**Thành phần F3–F5 dùng lại (theo file F4/F5; kiểm trước khi code, thiếu thì dừng báo Orchestrator):** `access.users(time_zone)`, `[RequirePermission("study.use")]`, `ClaimsPrincipal.GetAccountId()`; `learning.study_events` + `Learning/StudyEventKinds.SrsReview` + `IStudyActivityRecorder.RecordAsync(Guid userId, string kind, DateTime occurredAtUtc, int quantity, int? correct, Guid? refId, CancellationToken ct)` (chỉ `Add`, người gọi `SaveChanges` trong transaction của mình); `IChineseDbContext.BeginTransactionAsync`; Domain `Time/UserLocalDate` (`From(utc, tzId)`, `DayRange(date, tzId)`, `DefaultTimeZoneId`); Domain `Pinyin/PinyinText` (`NormalizeNumbered`, `FromToneMarks` — chỉ tách theo khoảng trắng/`'`, `ToSearchKey`, `TryParseSyllable`, `ToMarked`) + `Pinyin/PinyinSyllable.IsValidKey`; `AntFarm.Core/Errors/ServiceUnavailableException` (503 `CONTENT_UNAVAILABLE`); `IPinyinCatalog`; `Content:RootPath` + csproj Api link `content/chinese/data/**/*.json`; Dockerfile chinese-backend đã đưa `content/chinese/data` vào ảnh (RK24); `apps/chinese/src/lib/pinyin.ts` (số → dấu) + vitest; `useSpeech`; `useTabParam`; trang `/ho-so` dạng tab (F4).

---
## 5. Thiết kế giải pháp

### 5.1 Database

Quy ước chung: PK `uuid` sinh ở ứng dụng bằng `Guid.CreateVersion7()`; snake_case tự động; `timestamptz` chỉ nhận `DateTime Kind=Utc`; cột `date` ⇄ `DateOnly`; `text[]` ⇄ `List<string>`; enum lưu chuỗi (`varchar` + `CHECK`). Mỗi feature **một** migration trong chinese-backend.

#### 5.1.1 F6 — migration `F6_Vocabulary` (schema `content`)

Bật extension trong model: `modelBuilder.HasPostgresExtension("pg_trgm");` **[BA-mặc định]** — dùng cho `LIKE '%...%'` trên cột tìm kiếm (500 dòng thì chưa cần, nhưng khi mở rộng HSK 2–9 ~11.000 từ thì có ích; chi phí thấp). **Không dùng `unaccent`**: bỏ dấu tiếng Việt làm ở C# (`VietnameseText`) lúc ghi, vì `unaccent()` không `IMMUTABLE` (không đánh chỉ mục trực tiếp được) và muốn so khớp cùng một hàm chuẩn hoá ở cả lúc ghi lẫn lúc truy vấn.

```
content.words
  id                   uuid PK
  simplified           varchar(32)  NOT NULL
  traditional          varchar(32)  NULL                 -- null khi trùng simplified
  variants             text[]       NOT NULL DEFAULT '{}' -- ['爸'] cho 爸爸
  pinyin               varchar(128) NOT NULL             -- số thanh, cách nhau 1 dấu cách, giữ hoa: 'ai4', 'Bei3 jing1'
  pinyin_compact       varchar(128) NOT NULL             -- lower, bỏ cách, ü=v, còn số: 'ai4', 'bei3jing1'
  pinyin_search        varchar(128) NOT NULL             -- lower, bỏ cách, bỏ số: 'ai', 'beijing'
  hsk3_level           smallint NULL  CHECK (hsk3_level BETWEEN 1 AND 7)      -- 7 = "7-9"
  hsk2_level           smallint NULL  CHECK (hsk2_level BETWEEN 1 AND 6)
  hsk_exam2026_level   smallint NULL  CHECK (hsk_exam2026_level BETWEEN 1 AND 7)
  official_index       smallint NULL                     -- số thứ tự trong danh sách chính thức cấp hsk3_level
  path_order           int NULL
  frequency_rank       int NULL
  pos                  text[] NOT NULL DEFAULT '{}'
  usage_note           varchar(64) NULL                  -- '第二', '朋友们', '桌子'
  meanings_en          text[] NOT NULL
  meanings_vi          text[] NOT NULL
  meaning_vi_status    varchar(16) NOT NULL CHECK (meaning_vi_status IN ('machine','reviewed'))
  meaning_vi_source    varchar(16) NOT NULL CHECK (meaning_vi_source IN ('cvdict','machine','manual'))
  han_viet             varchar(64) NULL                  -- 'ái', 'ái hiếu'
  han_viet_plain       varchar(64) NULL                  -- lower, bỏ dấu: 'ai'
  han_viet_status      varchar(16) NULL CHECK (han_viet_status IN ('derived','reviewed'))
  search_vi            text NOT NULL                     -- '| yêu | thích | tình cảm | ái |' (§5.2.2)
  search_vi_plain      text NOT NULL                     -- '| yeu | thich | tinh cam | ai |'
  sources              text[] NOT NULL DEFAULT '{}'
  edited_at            timestamptz NULL                  -- F10 ghi
  edited_by            uuid NULL                         -- F10 ghi (không FK — chỉ dấu vết)
  created_at           timestamptz NOT NULL
  updated_at           timestamptz NOT NULL

  UNIQUE  ux_words_simplified_pinyin (simplified, pinyin)
  INDEX   ix_words_simplified_pattern (simplified varchar_pattern_ops)
  INDEX   ix_words_traditional (traditional)
  INDEX   ix_words_pinyin_compact (pinyin_compact varchar_pattern_ops)
  INDEX   ix_words_pinyin_search (pinyin_search varchar_pattern_ops)
  INDEX   ix_words_han_viet_plain (han_viet_plain)
  INDEX   ix_words_level_path (hsk3_level, path_order)      -- KHÔNG unique (đổi thứ tự giữa chừng upsert)
  INDEX   ix_words_hsk2 (hsk2_level)
  GIN     ix_words_search_vi_trgm (search_vi gin_trgm_ops)
  GIN     ix_words_search_vi_plain_trgm (search_vi_plain gin_trgm_ops)

content.characters
  id                   uuid PK
  hanzi                varchar(4) NOT NULL UNIQUE        -- đúng 1 code point (kiểm ở importer)
  traditional_variants text[] NOT NULL DEFAULT '{}'      -- **Lệch hợp đồng gốc** (gốc: traditional varchar(4)); 干 ⇒ {乾,幹}
  pinyin_readings      text[] NOT NULL                   -- số thanh, lower
  han_viet             text[] NOT NULL DEFAULT '{}'      -- ['hảo','hiếu'], phần tử đầu = phổ biến nhất
  han_viet_by_pinyin   jsonb NULL                        -- {"hao3":"hảo","hao4":"hiếu"}; chỉ đọc/ghi cả khối, không lọc SQL
  han_viet_status      varchar(16) NOT NULL DEFAULT 'derived' CHECK (han_viet_status IN ('derived','reviewed'))
  stroke_count         smallint NULL
  radical              varchar(4) NULL                   -- chữ bộ thủ dạng CJK Unified (爪)
  radical_number       smallint NULL                     -- 87
  sources              text[] NOT NULL DEFAULT '{}'
  edited_at            timestamptz NULL
  created_at, updated_at timestamptz NOT NULL

content.word_characters
  word_id       uuid NOT NULL FK → content.words(id) ON DELETE CASCADE
  position      smallint NOT NULL                        -- 0-based, theo code point của simplified
  character_id  uuid NOT NULL FK → content.characters(id) ON DELETE RESTRICT
  PRIMARY KEY (word_id, position)
  INDEX ix_word_characters_character (character_id)

content.import_runs
  id                uuid PK
  dataset           varchar(64) NOT NULL                 -- 'characters' | 'hsk-words' (test: '*-test')
  file_hash         char(64) NOT NULL                    -- SHA-256 hex lower của byte tệp
  importer_version  int NOT NULL                         -- hằng ContentImporter.Version (tăng khi đổi logic nạp)
  status            varchar(16) NOT NULL CHECK (status IN ('succeeded','failed'))
  inserted, updated, unchanged, invalid, protected int NOT NULL DEFAULT 0
  error             text NULL
  started_at        timestamptz NOT NULL
  finished_at       timestamptz NOT NULL
  INDEX ix_import_runs_dataset_started (dataset, started_at DESC)
```

Chỉ mục `varchar_pattern_ops` cần cho `LIKE 'x%'` khi collation DB không phải `C`. Cấu hình EF: `.HasIndex(x => x.PinyinCompact).HasOperators("varchar_pattern_ops")`; trgm: `.HasIndex(x => x.SearchVi).HasMethod("gin").HasOperators("gin_trgm_ops")`. `CHECK` bằng `ToTable(t => t.HasCheckConstraint(...))`.

#### 5.1.2 F7 — migration `F7_Srs` (schema `learning`)

```
learning.learner_settings
  user_id              uuid PK FK → access.users(id) ON DELETE CASCADE
  daily_new_cards      smallint NOT NULL DEFAULT 10   CHECK (daily_new_cards BETWEEN 0 AND 50)
  daily_review_limit   smallint NOT NULL DEFAULT 200  CHECK (daily_review_limit BETWEEN 10 AND 1000)
  desired_retention    numeric(3,2) NOT NULL DEFAULT 0.90 CHECK (desired_retention BETWEEN 0.80 AND 0.97)
  tts_rate             numeric(3,2) NOT NULL DEFAULT 0.80 CHECK (tts_rate BETWEEN 0.50 AND 1.20)
  auto_play_audio      boolean NOT NULL DEFAULT true
  updated_at           timestamptz NOT NULL
  -- Không có dòng ⇒ dùng mặc định (GET không chèn; PUT upsert)

learning.srs_cards
  id                        uuid PK
  user_id                   uuid NOT NULL FK → access.users(id) ON DELETE CASCADE
  word_id                   uuid NOT NULL FK → content.words(id) ON DELETE CASCADE
  card_type                 varchar(24) NOT NULL DEFAULT 'hanzi_to_meaning' CHECK (card_type IN ('hanzi_to_meaning'))
  state                     varchar(12) NOT NULL CHECK (state IN ('new','learning','review','relearning'))
  step                      smallint NULL          -- chỉ số bước học/học lại; NULL khi review
  due_at                    timestamptz NOT NULL   -- thẻ new: = created_at
  stability                 double precision NULL  -- NULL khi new
  difficulty                double precision NULL  -- NULL khi new
  reps                      int NOT NULL DEFAULT 0
  lapses                    int NOT NULL DEFAULT 0
  last_review_at            timestamptz NULL
  first_reviewed_at         timestamptz NULL
  first_reviewed_local_date date NULL
  is_suspended              boolean NOT NULL DEFAULT false
  source                    varchar(12) NOT NULL CHECK (source IN ('path','manual','lesson'))
  created_at                timestamptz NOT NULL
  updated_at                timestamptz NOT NULL
  UNIQUE ux_srs_cards_user_word_type (user_id, word_id, card_type)
  INDEX  ix_srs_cards_user_state_due (user_id, state, due_at) WHERE is_suspended = false
  INDEX  ix_srs_cards_user_first_date (user_id, first_reviewed_local_date)

learning.srs_review_logs
  id                 uuid PK
  client_review_id   uuid NOT NULL UNIQUE
  card_id            uuid NOT NULL FK → learning.srs_cards(id) ON DELETE CASCADE
  user_id            uuid NOT NULL                       -- không FK (thẻ đã CASCADE theo user)
  rating             smallint NOT NULL CHECK (rating BETWEEN 1 AND 4)   -- 1 again, 2 hard, 3 good, 4 easy
  reviewed_at        timestamptz NOT NULL
  local_date         date NOT NULL                       -- ngày học theo múi giờ lúc ghi (R-T3)
  state_before       varchar(12) NOT NULL
  step_before        smallint NULL
  stability_before   double precision NULL
  difficulty_before  double precision NULL
  state_after        varchar(12) NOT NULL
  stability_after    double precision NOT NULL
  difficulty_after   double precision NOT NULL
  due_after          timestamptz NOT NULL
  elapsed_days       double precision NOT NULL           -- (reviewed_at − last_review_at) theo ngày thực; lần đầu 0
  scheduled_days     double precision NOT NULL           -- (due_after − reviewed_at) theo ngày thực
  duration_ms        int NULL
  INDEX ix_srs_review_logs_user_date (user_id, local_date)
  INDEX ix_srs_review_logs_card (card_id, reviewed_at)
```

**Lệch hợp đồng gốc:** thêm `local_date`, `step_before`, `*_after`, `due_after` vào log (để trả lời lại y hệt khi trùng `clientReviewId` và để đếm theo ngày không cần đổi múi giờ lúc đọc); `learner_settings.daily_review_limit` có CHECK; chỉ mục thẻ là chỉ mục một phần.

`learning.study_events` (F5) không đổi; F7 ghi `kind='srs_review'`.

### 5.2 Backend

#### 5.2.1 F6.2 — danh sách file

| Lớp | File (dưới `backend/services/chinese-backend/src/`) | Nội dung |
|---|---|---|
| Domain | `AntFarm.Chinese.Domain/Content/Word.cs` | Entity; phương thức `ApplyImport(WordImportData d, DateTime nowUtc) → WordImportOutcome` (Inserted/Updated/Unchanged/UpdatedProtected) thực thi R6-11; `IsReviewLocked => MeaningViStatus == "reviewed" \|\| EditedAt != null`; `IsHanVietLocked => HanVietStatus == "reviewed" \|\| EditedAt != null`; luôn gọi `RecomputeSearchKeys()` |
| Domain | `Content/Character.cs`, `Content/WordCharacter.cs`, `Content/ImportRun.cs` | Entity |
| Domain | `Content/ContentStatuses.cs` | Hằng `MeaningViStatus.{Machine,Reviewed}`, `MeaningViSource.{Cvdict,Machine,Manual}`, `HanVietStatus.{Derived,Reviewed}` |
| Domain | `Content/WordSearchKeys.cs` | Hàm thuần: `PinyinCompact(pinyin)`, `PinyinToneless(pinyin)`, `BuildSearchVi(meaningsVi, hanViet) → (withDiacritics, plain)` (§5.2.2) |
| Domain | `Text/VietnameseText.cs` | `RemoveDiacritics(s)` (NFD → bỏ `NonSpacingMark` → NFC; `đ→d`, `Đ→D`), `NormalizeForSearch(s)` (NFC, lower `vi-VN`-độc lập = `ToLowerInvariant`, bỏ nội dung trong ngoặc tròn, thay ký tự không phải chữ/số bằng cách, gộp cách, trim), `HasDiacritics(s) => RemoveDiacritics(s) != s` (sau lower) |
| Domain | `Pinyin/PinyinQuery.cs` (mới; dùng `PinyinText` + `PinyinSyllable.IsValidKey` của F5) | `TryParseNumbered(q, out compact)` (bọc `PinyinText.NormalizeNumbered` sau khi tách `ni3hao3` theo chữ số và đổi `0`→`5`), `TryParseToneMarked(q, out compact)` (chuỗi có khoảng trắng/`'` ⇒ `PinyinText.FromToneMarks`; chuỗi liền `nǐhǎo` ⇒ tách bằng `Segment` rồi mới đổi), `Toneless(q)`, `Segment(string toneless)` dùng `PinyinSyllable.IsValidKey` (bổ sung khoá `r` nếu `IsValidKey` chưa nhận) — §5.2.3 |
| Application | `Common/Abstractions/IChineseDbContext.cs` | Thêm `DbSet<Word> Words`, `DbSet<Character> Characters`, `DbSet<WordCharacter> WordCharacters`, `DbSet<ImportRun> ImportRuns` |
| Application | `Dictionary/DictionaryQuery.cs` | `record DictionaryQuery(string? Q, short? Hsk, int Page = 1, int PageSize = 20)` + `DictionaryQueryValidator` (Q ≤ 64 sau chuẩn hoá; Hsk 1..7; Page ≥ 1; PageSize 1..100) |
| Application | `Dictionary/DictionaryQueryParser.cs` | `ParsedQuery Parse(string? q)` → `{ Kind: Empty\|Hanzi\|Latin, Raw, HanziText?, PinyinCompact?, PinyinToneless?, ViText?, ViPlain?, ViHasDiacritics }` |
| Application | `Dictionary/DictionaryService.cs` | `SearchAsync`, `GetWordAsync(id, userId)`, `GetCharacterAsync(hanzi)` |
| Application | `Dictionary/DictionaryDtos.cs` | DTO §6.1 |
| Infrastructure | `Persistence/Configurations/Content/{Word,Character,WordCharacter,ImportRun}Configuration.cs` | Bảng/cột/chỉ mục §5.1.1 |
| Infrastructure | `Persistence/Migrations/<ts>_F6_Vocabulary.cs` | Sinh bằng `dotnet ef migrations add F6_Vocabulary ...`; kiểm có `CREATE EXTENSION IF NOT EXISTS pg_trgm` |
| Infrastructure | `Content/Files/HskWordsFile.cs`, `Content/Files/CharactersFile.cs` | Record ánh xạ JSON §5.4.5 (`System.Text.Json`, camelCase) |
| Infrastructure | `Content/ContentImporter.cs` | `const int Version = 1`; `Task<ImportSummary> ImportCharactersAsync(string path, string dataset, CancellationToken)`, `ImportWordsAsync(...)` — §5.2.4 |
| Infrastructure | `Content/ContentImportRunner.cs` | `static Task RunAsync(IServiceProvider sp, CancellationToken)` — đọc `Content:RootPath`, `Content:ImportOnStartup` (mặc định `true`), gọi tuần tự `characters` rồi `hsk-words`; **bắt mọi exception**, log Error, không ném |
| Infrastructure | `DependencyInjection.cs` | `services.AddScoped<ContentImporter>()` |
| Api | `Features/Dictionary/DictionaryController.cs` | `[RequirePermission(PermissionCodes.StudyUse)]`, route `api/dictionary` |
| Api | `Program.cs` | Trong khối `AutoMigrate`, sau seeder F3: `await ContentImportRunner.RunAsync(scope.ServiceProvider, CancellationToken.None);` (nạp học liệu chỉ khi DB đã migrate) |
| Api | `appsettings.json` | `"Content": { "RootPath": "content/chinese", "ImportOnStartup": true }` (F5 đã có `RootPath` thì chỉ thêm `ImportOnStartup`) |

Không thêm package NuGet cho F6.

#### 5.2.2 Khoá tìm kiếm (tính lúc ghi, cùng hàm dùng lúc truy vấn)

```
PinyinCompact("Bei3 jing1") = "bei3jing1"      // lower, bỏ cách, 'ü'/'u:' → 'v', thanh 0 → 5
PinyinToneless("Bei3 jing1") = "beijing"       // như trên rồi bỏ số
BuildSearchVi(meaningsVi, hanViet):
  terms = meaningsVi
          .SelectMany(m => m.Split([';', ',', '/']))
          .Select(VietnameseText.NormalizeForSearch)      // "(trợ từ ...)" bị bỏ vì nằm trong ngoặc
          .Append(NormalizeForSearch(hanViet ?? ""))
          .Where(t => t.Length > 0).Distinct()
  withDiacritics = "| " + string.Join(" | ", terms) + " |"
  plain          = VietnameseText.RemoveDiacritics(withDiacritics)
// 爱: meaningsVi ["yêu; thích","tình cảm","có khuynh hướng (làm gì đó); có xu hướng (xảy ra)"], hanViet "ái"
//   ⇒ "| yêu | thích | tình cảm | có khuynh hướng | có xu hướng | ái |"
//   ⇒ "| yeu | thich | tinh cam | co khuynh huong | co xu huong | ai |"
han_viet_plain = RemoveDiacritics(NormalizeForSearch(hanViet))    // "ai"
```

Ký tự `|`, `%`, `_`, `\` trong truy vấn bị thay bằng cách trước khi dựng mẫu `LIKE` (không cần `ESCAPE`).

#### 5.2.3 Tra từ — phân tích truy vấn & xếp hạng

**Phân tích (`DictionaryQueryParser`)** — `q` → NFC → trim → gộp khoảng trắng:

1. Rỗng ⇒ `Empty`.
2. Có ít nhất một ký tự Hán (`\p{IsCJKUnifiedIdeographs}`, `\p{IsCJKUnifiedIdeographsExtensionA}`, `〇`) ⇒ `Hanzi` (`HanziText = q` bỏ khoảng trắng). Không chạy nhánh Latin.
3. Ngược lại `Latin`, `l = q.ToLowerInvariant()`; tính **độc lập** các khoá sau:
   - **Pinyin số**: `l` khớp `^([a-zü:v]+[0-5]['\s]*)+$` ⇒ chuẩn hoá từng âm tiết (`ü|u:` → `v`, `0` → `5`), phần chữ phải là âm tiết hợp lệ ⇒ `PinyinCompact` (vd `ni3hao3`). Có chữ số nhưng không khớp ⇒ bỏ chữ số, coi như không thanh.
   - **Pinyin dấu**: mọi ký tự thuộc `[a-z üv'\s]` ∪ `āáǎàēéěèīíǐìōóǒòūúǔùǖǘǚǜ`, **và** có ít nhất một nguyên âm mang dấu thanh ⇒ tách nguyên âm mang dấu thành (chữ gốc, thanh); chuỗi gốc tách theo khoảng trắng/`'`, mỗi đoạn tách âm tiết bằng `Segment` (quy hoạch động, ưu tiên ít âm tiết nhất; hoà ⇒ âm tiết đầu dài nhất); thanh của âm tiết = thanh của nguyên âm mang dấu nằm trong nó (không có ⇒ 5; >1 dấu trong một âm tiết ⇒ không hợp lệ) ⇒ `PinyinCompact` (vd `nǐhǎo` → `ni3hao3`, `ài` → `ai4`, `ái` → `ai2`). Tách thất bại ⇒ bỏ khoá này.
   - **Pinyin không thanh**: chỉ khi **không** có số và **không** có nguyên âm mang dấu thanh, và `l` khớp `^[a-züv'\s]+$` ⇒ `PinyinToneless` = bỏ cách/`'`, `ü` → `v` (vd `ni hao` → `nihao`).
   - **Tiếng Việt**: luôn tính `ViText = NormalizeForSearch(q)`, `ViPlain = RemoveDiacritics(ViText)`, `ViHasDiacritics = ViText != ViPlain`. Lưu ý `á` (U+00E1) vừa là dấu thanh 2 pinyin vừa là dấu sắc tiếng Việt ⇒ `ái` sinh **cả** `PinyinCompact = ai2` và `ViText = ái` (R6-22).

**Điều kiện & hạng** (số nhỏ = tốt hơn; một từ khớp nhiều điều kiện lấy hạng nhỏ nhất). `col_vi` = `search_vi` nếu `ViHasDiacritics` ngược lại `search_vi_plain`; `hv` = `lower(han_viet)` nếu có dấu ngược lại `han_viet_plain`; `v` = `ViText` hoặc `ViPlain` tương ứng.

| Hạng | Điều kiện | Áp dụng khi |
|---|---|---|
| 0 | `simplified = h` OR `traditional = h` OR `h = ANY(variants)` | Hanzi |
| 1 | `pinyin_compact = c` | có `PinyinCompact` |
| 2 | `pinyin_search = t` | có `PinyinToneless` |
| 3 | `simplified LIKE h || '%'` | Hanzi |
| 4 | `pinyin_compact LIKE c || '%'` OR `pinyin_search LIKE t || '%'` | có khoá tương ứng; độ dài khoá ≥ 2 |
| 5 | `hv = v` | Latin |
| 6 | `col_vi LIKE '%| ' || v || ' |%'` (khớp trọn một nghĩa) | Latin |
| 7 | `simplified LIKE '%' || h || '%'` OR `traditional LIKE '%' || h || '%'` | Hanzi |
| 8 | `col_vi LIKE '% ' || v || '%'` (khớp đầu một từ trong nghĩa/Hán Việt) | Latin, `len(v) ≥ 2` |

Lọc thêm `hsk3_level = @hsk` khi có. Sắp: `rank ASC, hsk3_level ASC NULLS LAST, path_order ASC NULLS LAST, frequency_rank ASC NULLS LAST, simplified ASC`. `totalCount` = số từ khớp. `Empty` ⇒ không điều kiện, sắp `hsk3_level, path_order, frequency_rank`.

Hiện thực: LINQ EF Core với biểu thức hạng dạng `x.Simplified == h ? 0 : x.PinyinCompact == c ? 1 : ...` và `EF.Functions.Like`; bỏ nhánh không áp dụng bằng cờ hằng ở C# (tránh `NULL LIKE`). Được phép dùng `FromSql` có tham số nếu LINQ quá rườm — **cấm nối chuỗi SQL**. `matchKind` trả về: 0/3/7 ⇒ `hanzi`; 1/2/4 ⇒ `pinyin`; 5 ⇒ `han_viet`; 6/8 ⇒ `meaning`; `Empty` ⇒ `browse`.

Kiểm chứng bắt buộc (ApiTests với dữ liệu thật): `爱`, `ai4`, `ài`, `ai`, `yêu`, `yeu`, `ái` ⇒ trang 1 có 爱 (`ai4`); `ai4` ⇒ 爱 ở vị trí 1; `yêu` ⇒ không có mục nào chỉ khớp `yếu`; `ni3hao3`, `nǐhǎo`, `nihao`, `ni hao` ⇒ 你好 ở vị trí 1; `爸` ⇒ 爸爸 (qua `variants`); `Beijing`/`beijing` ⇒ 北京; `100%` ⇒ 200 không lỗi.

#### 5.2.4 `ContentImporter` — thuật toán nạp

```
ImportXxxAsync(path, dataset, ct):
  startedAt = time.GetUtcNow().UtcDateTime
  nếu !File.Exists(path) ⇒ log Warning "Không thấy tệp học liệu", trả Skipped (không ghi import_runs)
  bytes = File.ReadAllBytes(path); hash = SHA256 hex lower
  last = import_runs WHERE dataset ORDER BY started_at DESC LIMIT 1 (status succeeded)
  nếu last.file_hash == hash && last.importer_version == Version ⇒ log Information "không đổi", trả Skipped
  parse JSON (lỗi ⇒ ghi import_runs failed + error, log Error, trả Failed)
  BEGIN TRANSACTION
    SELECT pg_advisory_xact_lock(727006)              -- hai tiến trình khởi động cùng lúc không nạp chồng
    kiểm lại hash sau khi có khoá (tiến trình kia có thể vừa nạp xong) ⇒ Skipped
    nạp toàn bộ bản ghi hiện có của dataset vào Dictionary theo khoá tự nhiên (500 dòng — không cần phân trang)
    với từng mục trong tệp:
      kiểm hợp lệ (§5.4.5 luật) — sai ⇒ invalid++, log Warning (khoá + lý do), bỏ qua
      chưa có ⇒ tạo mới (Guid.CreateVersion7), inserted++
      có ⇒ entity.ApplyImport(...) ⇒ updated++ | unchanged++ ; nếu có trường bị giữ vì đã duyệt ⇒ protected++
      (từ) đồng bộ word_characters: danh sách chữ mong đợi = code point của simplified;
           chữ không có trong content.characters ⇒ invalid++ (bỏ cả từ); khác hiện trạng ⇒ xoá + chèn lại dòng của từ đó
    đếm bản ghi trong DB không còn trong tệp ⇒ log Warning (số lượng + tối đa 20 khoá); KHÔNG xoá
    SaveChanges; chèn import_runs succeeded
  COMMIT
  lỗi bất kỳ trong transaction ⇒ ROLLBACK, chèn import_runs failed (transaction mới), log Error, trả Failed
```

- `ApplyImport` so sánh từng trường (mảng so theo thứ tự phần tử) — chỉ đặt `updated_at` khi có thay đổi thật.
- Mục mới từ tệp luôn nhận `meaning_vi_status`/`han_viet_status` như tệp.
- Không bao giờ ném ra ngoài `ContentImportRunner`. Log mỗi dataset một dòng Information: `Nạp {Dataset}: +{Inserted} ~{Updated} ={Unchanged} !{Invalid} khoá{Protected} ({ElapsedMs} ms)`.
- Thứ tự: `characters` trước `hsk-words`. Nếu `characters` Failed ⇒ vẫn thử `hsk-words` (từ thiếu chữ sẽ vào `invalid`).

#### 5.2.5 F6.2 — test

UnitTests (`tests/AntFarm.Chinese.UnitTests/`):
- `Text/VietnameseTextTests`: `Đường đi` → `duong di`; `yêu` ≠ `yếu` khi còn dấu, cùng `yeu` khi bỏ; `NormalizeForSearch("(trợ từ) Của; ~hậu tố")` → `của hậu tố`; NFD đầu vào cho kết quả như NFC.
- `Pinyin/PinyinQueryTests` (≥ 20 ca): `ni3hao3`, `ni3 hao3`, `NI3HAO3` → `ni3hao3`; `nǐhǎo`, `nǐ hǎo`, `Nǐ'hǎo` → `ni3hao3`; `ài` → `ai4`; `ái` → `ai2`; `lǜ` → `lv4`; `lü4`, `lu:4` → `lv4`; `ma0` → `ma5`; `xian` (không thanh) → toneless `xian`; `xīān` → `xi1an1`; `ni3hao` → chỉ toneless `nihao`; `yêu` → không có khoá pinyin; `abc1` → không hợp lệ.
- `Dictionary/DictionaryQueryParserTests`: `爱` → Hanzi; `ái` → có cả `PinyinCompact=ai2` và `ViHasDiacritics=true`; `yeu` → `PinyinToneless = yeu` (hợp lệ về ký tự; không từ nào có `pinyin_search = yeu` nên vô hại) và `ViPlain = yeu`, `ViHasDiacritics = false`; `yêu` → không có khoá pinyin, `ViHasDiacritics = true`.
- `Content/WordSearchKeysTests`: ví dụ 爱 ở §5.2.2 khớp từng ký tự.
- `Content/WordApplyImportTests`: dòng `reviewed` giữ nghĩa khi tệp đổi nghĩa (outcome `UpdatedProtected`); `edited_at` khác null giữ Hán Việt; tệp giống hệt ⇒ `Unchanged`, `updated_at` không đổi.

ApiTests `[DbFact]` (`tests/AntFarm.Chinese.ApiTests/`):
- `Content/ContentImporterTests` — dùng thư mục fixture `TestData/content/` (copy ra output) với dataset `characters-test`/`hsk-words-test`, từ giả không trùng dữ liệu thật (vd `测试` `ce4 shi4`, `hsk3Level: null`): nạp 2 lần ⇒ lần 2 `Skipped` và số dòng không đổi; đổi `Version` giả lập bằng tham số test ⇒ chạy lại, `unchanged` = tổng; đặt `meaning_vi_status='reviewed'` rồi nạp tệp có nghĩa khác ⇒ nghĩa giữ nguyên, `protected = 1`; tệp JSON hỏng ⇒ `import_runs.status = failed`, không ném.
- `Content/RealContentTests`: sau khi factory khởi động (importer chạy với `content/chinese` thật): `COUNT(*) WHERE hsk3_level = 1` = **500**; `path_order` 1..500 liên tục; khởi động factory **thêm 2 lần** (tạo `ChineseApiFactory` mới) ⇒ số dòng `content.words`, `content.characters`, `content.word_characters` không đổi và `import_runs` lần sau không thêm dòng succeeded mới.
- `Dictionary/DictionarySearchTests`: các ca kiểm chứng §5.2.3; phân trang `pageSize=5` ⇒ `totalCount` đúng; `q` 65 ký tự ⇒ 400 `VALIDATION`; learner có `study.use` ⇒ 200; token không quyền (user bị gỡ hết vai trò) ⇒ 403.
- `Dictionary/WordDetailTests`: `GET words/{id}` của 爱 có `characters[0].hanzi = "爱"`, `hanViet = "ái"`; id lạ ⇒ 404; `GET characters/爱` (URL-encode) ⇒ `words` chứa 爱, 爱好; `characters/ab` ⇒ 400; chữ không có ⇒ 404.

#### 5.2.6 F7.1 — FSRS-6 (chép đúng `py-fsrs` v6.3.2)

**Nguồn:** https://github.com/open-spaced-repetition/py-fsrs — MIT — tag `v6.3.2` = commit `9446cb06605c597a063aeee49f7d188d42e34dc2` (08/2026), tệp `fsrs/scheduler.py`, `tests/test_basic.py`. Comment đầu `FsrsScheduler.cs` phải ghi URL + commit + "MIT, Copyright (c) Open Spaced Repetition" và thêm dòng vào `content/chinese/SOURCES.md` mục "Mã nguồn tham chiếu" (hoặc `THIRD-PARTY-NOTICES.md` gốc repo nếu F0 đã có).

**Trọng số mặc định** (`w[0]..w[20]`, đúng thứ tự):

```
0.212, 1.2931, 2.3065, 8.2956, 6.4133, 0.8334, 3.0194, 0.001, 1.8722, 0.1666,
0.796, 1.4835, 0.0614, 0.2629, 1.6483, 0.6014, 1.8729, 0.5425, 0.0912, 0.0658,
0.1542
```

Hằng: `STABILITY_MIN = 0.001`, `D ∈ [1, 10]`, `DECAY = −w20 = −0.1542`, `FACTOR = 0.9^(1/DECAY) − 1 ≈ 0.9803464944134797`. `G` = rating 1..4 (Again, Hard, Good, Easy). Kiểm 21 trọng số nằm trong cận `LOWER/UPPER_BOUNDS_PARAMETERS` của py-fsrs (chép cả hai mảng; sai ⇒ `ArgumentOutOfRangeException` lúc tạo).

**Công thức** (dùng `double`, `Math.Pow`, `Math.Exp`):

| Tên | Công thức |
|---|---|
| Khả năng nhớ | `R(t, S) = (1 + FACTOR · t / S)^DECAY`; `t = max(0, floor((now − last_review) / 1 ngày))` — **số ngày nguyên** như `timedelta.days` của Python; thẻ chưa có `last_review`/`S` ⇒ `R = 0` |
| Khoảng ôn | `I(S) = round(S / FACTOR · (r^(1/DECAY) − 1))`, `r` = `desired_retention`; `round` = **làm tròn về chẵn** (`Math.Round(x, MidpointRounding.ToEven)`, giống `round()` Python); kẹp `[1, maximum_interval]`; đơn vị ngày |
| S ban đầu | `S0(G) = max(w[G−1], 0.001)` |
| D ban đầu | `D0(G) = w4 − e^(w5·(G−1)) + 1`, kẹp `[1,10]` |
| D kế tiếp | `ΔD = −w6·(G−3)`; `D' = D + (10 − D)·ΔD/9`; `D'' = w7·D0(4)_không-kẹp + (1 − w7)·D'`; kẹp `[1,10]` |
| S khi nhớ (G ≥ 2) | `S'r = S · (1 + e^w8 · (11 − D) · S^(−w9) · (e^((1−R)·w10) − 1) · (G=2 ? w15 : 1) · (G=4 ? w16 : 1))` |
| S khi quên (G = 1) | `S'f = min( w11 · D^(−w12) · ((S+1)^w13 − 1) · e^((1−R)·w14),  S / e^(w17·w18) )` |
| S ngắn hạn (cùng ngày) | `inc = e^(w17·(G − 3 + w18)) · S^(−w19)`; nếu `G ≥ 2` thì `inc = max(inc, 1)`; `S' = S · inc` |
| Mọi S mới | kẹp `≥ 0.001` |

**Lưu ý thứ tự:** tính S mới **bằng D cũ**, rồi mới cập nhật D. "Cùng ngày" = `floor((now − last_review)/1 ngày) < 1`.

**Máy trạng thái** (`step` là chỉ số bước; `learning_steps = [1m, 10m]`, `relearning_steps = [10m]`). Thẻ `new` của ta ≡ `Learning, step 0, S = D = null` của py-fsrs.

```
new / learning:
  S,D null         ⇒ S = S0(G), D = D0(G)
  cùng ngày        ⇒ S = ngắn hạn, D = D kế tiếp
  khác ngày        ⇒ S = (G=1 ? S'f : S'r) với R hiện tại, D = D kế tiếp
  khoảng:
    step >= len(learning_steps) và G ≥ 2 ⇒ review, step null, I(S) ngày
    G=1 ⇒ step 0, 1 phút
    G=2 ⇒ step giữ; step 0 ⇒ (1m + 10m)/2 = 5m30s   (nếu chỉ 1 bước: bước·1.5); step khác ⇒ learning_steps[step]
    G=3 ⇒ step+1 == len ⇒ review, I(S) ngày; ngược lại step+1, learning_steps[step+1]
    G=4 ⇒ review, I(S) ngày
review:
  cùng ngày ⇒ S ngắn hạn; khác ngày ⇒ S'f/S'r ; D luôn = D kế tiếp
  G=1 ⇒ relearning, step 0, 10 phút ; lapses + 1
  G≥2 ⇒ review, I(S) ngày
relearning: như learning nhưng dùng relearning_steps = [10m]
  (G=2 ở step 0 với 1 bước ⇒ 15 phút; G=3 ở bước cuối ⇒ review I(S); G=4 ⇒ review I(S))
due_at = now + khoảng ; last_review_at = now
```

**Không fuzz** (R7-1). Nếu sau này bật fuzz thì chép `_get_fuzzed_interval` (chỉ áp khi state = review và khoảng ≥ 3 ngày; `FUZZ_RANGES` 2.5–7: 0.15, 7–20: 0.1, >20: 0.05) và vector vàng phải chạy với fuzz tắt.

**API Domain** (`AntFarm.Chinese.Domain/Srs/`):

```csharp
public enum SrsRating { Again = 1, Hard = 2, Good = 3, Easy = 4 }
public enum SrsState { New, Learning, Review, Relearning }          // lưu chuỗi snake_case
public sealed record FsrsOptions(IReadOnlyList<double> Weights, double DesiredRetention,
    IReadOnlyList<TimeSpan> LearningSteps, IReadOnlyList<TimeSpan> RelearningSteps, int MaximumInterval)
{ public static FsrsOptions Default(double desiredRetention = 0.9); }
public readonly record struct SrsMemory(SrsState State, int? Step, double? Stability, double? Difficulty,
    DateTime DueAt, DateTime? LastReviewAt);                        // mọi DateTime Kind=Utc
public readonly record struct SrsSchedulingResult(SrsMemory After, TimeSpan Interval);
public interface ISrsScheduler
{
    SrsSchedulingResult Review(SrsMemory card, SrsRating rating, DateTime nowUtc);
    IReadOnlyDictionary<SrsRating, TimeSpan> Preview(SrsMemory card, DateTime nowUtc); // 4 lần Review, không đổi trạng thái
    double Retrievability(SrsMemory card, DateTime nowUtc);
}
public sealed class FsrsScheduler(FsrsOptions options) : ISrsScheduler { ... } // thuần, không IO; ném nếu nowUtc.Kind != Utc
```

`FsrsScheduler` tạo mới mỗi request theo `desired_retention` của người học (rẻ). Entity `Srs/SrsCard.cs`: `ToMemory()`, `Apply(SrsSchedulingResult r, SrsRating g, DateTime nowUtc, DateOnly localDate)` (cập nhật state/step/S/D/due/last_review, `reps++`, `lapses++` theo R7-11, `first_reviewed_*` nếu null, `updated_at`). `Srs/SrsReviewLog.cs`, `Learning/LearnerSettings.cs` (`Defaults`).

#### 5.2.7 Vector vàng (đã chạy `fsrs==6.3.2`, `Scheduler(enable_fuzzing=False)`, trọng số mặc định, r = 0,9)

Test `Srs/FsrsGoldenTests.cs` phải chép **nguyên** các bảng dưới (kèm comment nguồn + commit). Sai số: S, D, R ≤ **1e-6** tuyệt đối (py-fsrs tự kiểm 1e-4; ta cùng phép tính `double` nên chặt hơn); `due_at` **bằng tuyệt đối**. Các giá trị V1/V2 trùng với assert trong `tests/test_basic.py` (`test_review_card`: khoảng `[0, 2, 11, 46, 163, 498, 0, 0, 2, 4, 7, 12, 21]`; `test_memo_state`: S ≈ 53.62691, D ≈ 6.3574867) — phần S/D/due chi tiết do BA sinh bằng script chạy thật.

**V1** — thẻ mới, bắt đầu `2022-11-29T12:30:00Z`, mỗi lượt chấm đúng lúc `due` của lượt trước; chuỗi G = `3,3,3,3,3,3,1,1,3,3,3,3,3`:

| # | G | state | step | S | D | due |
|---|---|---|---|---|---|---|
| 0 | 3 | learning | 1 | 2.3065 | 2.118103970459016 | 2022-11-29T12:40:00Z |
| 1 | 3 | review | – | 2.3065 | 2.111214235785395 | 2022-12-01T12:40:00Z |
| 2 | 3 | review | – | 10.971048263078135 | 2.1043313908464483 | 2022-12-12T12:40:00Z |
| 3 | 3 | review | – | 46.316858440073425 | 2.0974554287524403 | 2023-01-27T12:40:00Z |
| 4 | 3 | review | – | 162.99981577472244 | 2.0905863426205262 | 2023-07-09T12:40:00Z |
| 5 | 3 | review | – | 497.8765551245907 | 2.083724125574744 | 2024-11-18T12:40:00Z |
| 6 | 1 | relearning | 0 | 6.890412507565338 | 7.383202320049203 | 2024-11-18T12:50:00Z |
| 7 | 1 | relearning | 0 | 2.154598374301973 | 9.125104766121234 | 2024-11-18T13:00:00Z |
| 8 | 3 | review | – | 2.154598374301973 | 9.11120803065195 | 2024-11-20T13:00:00Z |
| 9 | 3 | review | – | 3.9831233795187773 | 9.097325191918136 | 2024-11-24T13:00:00Z |
| 10 | 3 | review | – | 7.236254476883319 | 9.083456236023055 | 2024-12-01T13:00:00Z |
| 11 | 3 | review | – | 12.483043578674472 | 9.06960114908387 | 2024-12-13T13:00:00Z |
| 12 | 3 | review | – | 20.77035728499242 | 9.055759917231622 | 2025-01-03T13:00:00Z |

**V2** — bắt đầu `2022-11-29T12:30:00Z`; trước mỗi lượt cộng thêm số ngày ở cột "+ngày" vào **thời điểm chấm** (không theo due):

| G | +ngày | state | step | S | D | due |
|---|---|---|---|---|---|---|
| 1 | 0 | learning | 0 | 0.212 | 6.4133 | 2022-11-29T12:31:00Z |
| 3 | 0 | learning | 1 | 0.24668918777567272 | 6.402115069296838 | 2022-11-29T12:40:00Z |
| 3 | 1 | review | – | 2.021477251638192 | 6.3909413235243795 | 2022-12-02T12:30:00Z |
| 3 | 3 | review | – | 7.863698841006282 | 6.379778751497693 | 2022-12-11T12:30:00Z |
| 3 | 8 | review | – | 21.917713153152434 | 6.368627342043034 | 2023-01-02T12:30:00Z |
| 3 | 21 | review | – | 53.626902917141365 | 6.357487083997829 | 2023-02-24T12:30:00Z |

**V3** — lượt đầu của thẻ mới lúc `2026-09-17T01:00:00Z` (khoảng dự kiến trên 4 nút của thẻ mới):

| G | state | step | S | D | due | khoảng |
|---|---|---|---|---|---|---|
| 1 | learning | 0 | 0.212 | 6.4133 | 2026-09-17T01:01:00Z | PT1M |
| 2 | learning | 0 | 1.2931 | 5.112170705601056 | 2026-09-17T01:05:30Z | PT5M30S |
| 3 | learning | 1 | 2.3065 | 2.118103970459016 | 2026-09-17T01:10:00Z | PT10M |
| 4 | review | – | 8.2956 | 1.0 | 2026-09-25T01:00:00Z | P8D |

**V4** — thẻ mới lúc `2026-09-17T01:00Z` chấm G=3, rồi G=3 lúc due ⇒ nền: review, S=2.3065, D=2.111214235785395, last=`2026-09-17T01:10Z`, due=`2026-09-19T01:10Z`. Từ nền, chấm một lần (a) đúng hạn `2026-09-19T01:10Z` và (b) trễ 3 ngày `2026-09-22T01:10Z`:

| G | (a) state / S / D / due | (b) state / S / D / due |
|---|---|---|
| 1 | relearning / 0.6077016626638644 / 7.392238132342694 / 2026-09-19T01:20Z | relearning / 0.6827348149809292 / 7.392238132342694 / 2026-09-22T01:20Z |
| 2 | review / 7.517359325415191 / 4.748284761594571 / 2026-09-27T01:10Z | review / 11.852915497576285 / 4.748284761594571 / 2026-10-04T01:10Z |
| 3 | review / 10.971048263078135 / 2.1043313908464483 / 2026-09-30T01:10Z | review / 18.180153970030403 / 2.1043313908464483 / 2026-10-10T01:10Z |
| 4 | review / 18.534332441919037 / 1.0 / 2026-10-08T01:10Z | review / 32.03626652046994 / 1.0 / 2026-10-24T01:10Z |

**V5** — từ V4(a) G=1 (relearning, step 0, due `2026-09-19T01:20Z`), chấm đúng hạn:

| G | state | step | S | D | due |
|---|---|---|---|---|---|
| 1 | relearning | 0 | 0.22294679606919743 | 9.128074776178583 | 2026-09-19T01:30Z |
| 2 | relearning | 0 | 0.6077016626638644 | 8.254074519842886 | 2026-09-19T01:35Z |
| 3 | review | – | 0.6597976257475758 | 7.38007426350719 | 2026-09-20T01:20Z |
| 4 | review | – | 1.1350513376989504 | 6.5060740071714935 | 2026-09-20T01:20Z |

**V6** — `R` với thẻ review S=10, last=`2026-09-17T01:00Z`: t=0 ⇒ 1.0; t=1 ⇒ 0.9856824087775146; t=10 ⇒ **0.9** (= `R(S,S)`); t=30 ⇒ 0.8093881035731708; t=100 ⇒ 0.6928266345726217.

**V7** — `I(S)`: r=0,9: S=1 ⇒ 1; S=2.3065 ⇒ 2; S=10 ⇒ 10; S=100 ⇒ 100. S=10: r=0,80 ⇒ 33; r=0,85 ⇒ 19; r=0,95 ⇒ 4; r=0,97 ⇒ 2. `D0` = `[6.4133, 5.112170705601056, 2.118103970459016, 1.0]`.

**V8 (thuộc tính, từ test py-fsrs):** chấm Easy 10 lần liên tiếp cách nhau 1 µs ⇒ D = 1.0; chấm Again 1000 lần, mỗi lần lúc `due + 1 ngày` ⇒ S luôn ≥ 0.001; mọi thẻ review: khoảng `Again < Hard ≤ Good ≤ Easy`; `due_at ≥ last_review_at`; `Review` với `nowUtc.Kind != Utc` ⇒ ném `ArgumentException`.

Script tái sinh (không commit, ghi vào comment test để người sau chạy lại): `uv run --python 3.12 --with "fsrs==6.3.2" python gold.py` với `Scheduler(enable_fuzzing=False)`, `Card(card_id=1, due=<mốc>)`, `review_card(card, Rating(g), t)`.

#### 5.2.8 F7.1 — dịch vụ & danh sách file

| Lớp | File | Nội dung |
|---|---|---|
| Domain | `Srs/SrsRating.cs`, `Srs/SrsState.cs`, `Srs/FsrsOptions.cs`, `Srs/SrsMemory.cs`, `Srs/ISrsScheduler.cs`, `Srs/FsrsScheduler.cs`, `Srs/SrsCard.cs`, `Srs/SrsReviewLog.cs`, `Learning/LearnerSettings.cs` | §5.2.6 |
| Domain | `Time/UserLocalDate.cs` (F5 — **dùng lại, không tạo lớp mới**) | `From(nowUtc, tzId)` ⇒ ngày học; `DayRange(date, tzId)` ⇒ `(FromUtc, ToUtcExclusive)` = mốc R7-3. Nếu `DayRange` của F5 chưa xử lý giờ không hợp lệ do DST (`tz.IsInvalidTime` ⇒ +1 giờ lặp) thì bổ sung vào chính hàm đó + test |
| Application | `Common/Abstractions/IChineseDbContext.cs` | Thêm `SrsCards`, `SrsReviewLogs`, `LearnerSettings` |
| Application | `Common/Time/IUserDayContext.cs` + hiện thực | `Task<UserDay> GetAsync(Guid userId, ct)` → `{ TimeZoneId, NowUtc, LocalDate, StartUtc, EndUtc }` — đọc `access.users.time_zone`, `NowUtc = TimeProvider.GetUtcNow().UtcDateTime`, tính bằng `UserLocalDate` |
| Application | `Learning/LearnerSettingsService.cs` + `UpdateLearnerSettingsValidator` | GET (mặc định nếu chưa có dòng), PUT (upsert) |
| Application | `Srs/SrsSummaryService.cs` | `GetAsync(userId)` → `SrsSummaryDto` (R7-4…R7-6, R7-13) |
| Application | `Srs/SrsQueueService.cs` | `GetQueueAsync(userId, limit)` theo R7-7; tạo lười thẻ `path` bằng `INSERT ... ON CONFLICT (user_id, word_id, card_type) DO NOTHING` (hoặc bắt `23505`) rồi đọc lại; mỗi thẻ kèm `intervals` = `scheduler.Preview` định dạng ISO-8601 (`XmlConvert.ToString(TimeSpan)` ⇒ `PT1M`, `PT5M30S`, `P8D`) |
| Application | `Srs/SrsReviewService.cs` | `ReviewAsync(userId, cardId, ReviewCardCommand)` — thuật toán dưới |
| Application | `Srs/SrsCardService.cs` | `AddAsync(userId, wordIds)` (`source='manual'`, `state='new'`, `due_at=now`), `SetSuspendedAsync(userId, cardId, bool)` |
| Application | `Srs/SrsDtos.cs`, `Srs/ReviewCardCommandValidator.cs`, `Srs/AddCardsCommandValidator.cs` | §6.2 |
| Application | `Dictionary/DictionaryService.cs` (sửa) | `GetWordAsync` thêm khối `srs` của người đang gọi |
| Infrastructure | `Persistence/Configurations/Learning/{SrsCard,SrsReviewLog,LearnerSettings}Configuration.cs`, migration `F7_Srs` | §5.1.2 |
| Api | `Features/Srs/SrsController.cs` (`api/srs`), `Features/Me/LearningSettingsController.cs` (`api/me/learning-settings`) | `[RequirePermission("study.use")]` cả hai |
| Tests | `Directory.Packages.props`: `<PackageVersion Include="Microsoft.Extensions.TimeProvider.Testing" Version="10.10.0" />`; tham chiếu **chỉ** ở `AntFarm.Chinese.UnitTests` và `AntFarm.Chinese.ApiTests` | `FakeTimeProvider` |

**Chấm thẻ (`SrsReviewService.ReviewAsync`)** — một transaction:

```
1. existing = srs_review_logs WHERE client_review_id = cmd.ClientReviewId
   có: existing.card_id != cardId || existing.user_id != userId ⇒ 409 CLIENT_REVIEW_ID_CONFLICT
       ngược lại ⇒ trả kết quả dựng từ log (state_after, due_after...) + card hiện tại + summary, duplicate = true
2. card = SELECT ... FROM learning.srs_cards WHERE id = @cardId AND user_id = @userId FOR UPDATE
   không có ⇒ 404 NOT_FOUND
3. card.is_suspended ⇒ 422 CARD_SUSPENDED
4. day = IUserDayContext ; settings = LearnerSettings ?? mặc định
5. card.state == new && count(first_reviewed_local_date == day.LocalDate) >= settings.daily_new_cards
   ⇒ 422 NEW_CARD_LIMIT_REACHED
6. scheduler = new FsrsScheduler(FsrsOptions.Default((double)settings.desired_retention))
   before = card.ToMemory(); r = scheduler.Review(before, rating, day.NowUtc)
7. card.Apply(r, rating, day.NowUtc, day.LocalDate)
8. thêm SrsReviewLog { ..., elapsed_days = before.LastReviewAt is null ? 0 : (now − last).TotalDays,
                        scheduled_days = r.Interval.TotalDays, duration_ms = clamp(cmd.DurationMs, 0, 600000) }
9. IStudyActivityRecorder.RecordAsync(userId, StudyEventKinds.SrsReview, day.NowUtc, 1, rating != Again ? 1 : 0, card.Id, ct)
10. SaveChanges; COMMIT
    DbUpdateException với PostgresException.SqlState == "23505" trên ux client_review_id
    ⇒ rollback, quay lại bước 1 (lượt song song đã ghi) — tối đa 1 lần
11. trả { reviewId, duplicate = false, card, summary }
```

`FOR UPDATE`: dùng `FromSql($"SELECT * FROM learning.srs_cards WHERE id = {cardId} AND user_id = {userId} FOR UPDATE")` (tham số hoá qua interpolated `FromSql`, không `FromSqlRaw` nối chuỗi).

#### 5.2.9 F7.1 — test

UnitTests:
- `Srs/FsrsGoldenTests` — V1–V7 (§5.2.7), mỗi bảng một `[Theory]`/`[Fact]`.
- `Srs/FsrsPropertyTests` — V8; `Preview` không làm đổi thẻ; trọng số ngoài cận ⇒ ném.
- `Srs/SrsCardApplyTests` — `lapses` chỉ tăng khi review→again; `first_reviewed_local_date` chỉ đặt lần đầu; `new` → `learning` sau G=1.
- `Time/UserLocalDateSrsTests` (bổ sung cho test F5) — `Asia/Ho_Chi_Minh`: `From(2026-09-17T16:30Z)` ⇒ 17/09 (23:30); `From(2026-09-17T17:30Z)` ⇒ 18/09 (00:30); `DayRange(17/09)` = `[2026-09-16T17:00Z, 2026-09-17T17:00Z)`; múi có DST `America/New_York`: `DayRange(08/03/2026).FromUtc` = `2026-03-08T05:00Z`, `DayRange(09/03/2026).FromUtc` = `2026-03-09T04:00Z`.

ApiTests `[DbFact]` (factory thay `TimeProvider` bằng `FakeTimeProvider` singleton; người dùng test có `time_zone = Asia/Ho_Chi_Minh`; dữ liệu học liệu thật đã nạp):
1. `GET /api/srs/queue` người mới ⇒ 10 thẻ `new` đúng 10 từ `path_order` 1..10; gọi lại ⇒ cùng 10 thẻ, không nhân đôi dòng `srs_cards`.
2. Chấm 10 thẻ mới lúc `2026-09-17T10:00Z` (17:00 VN) ⇒ `summary.newIntroducedToday = 10`, `newAvailableToday = 0`; queue không còn thẻ mới; chấm thêm một thẻ `new` khác (thêm qua `POST /api/srs/cards`) ⇒ 422 `NEW_CARD_LIMIT_REACHED`.
3. Đặt giờ `2026-09-17T23:30Z` (06:30 VN ngày 18/09) ⇒ `localDate = 2026-09-18`, `newAvailableToday = 10` (**cắt theo UTC sẽ ra 0 — test này bắt lỗi đó**); `study_events` của lượt chấm lúc này có `local_date = 2026-09-18`.
4. Ranh giới "đến hạn hôm nay": thẻ review có `due_at = 2026-09-17T16:30Z`; lúc `2026-09-17T16:00Z` (23:00 VN 17/09) ⇒ `dueToday` tính thẻ đó, `dueNow` không tính; lúc `2026-09-17T17:30Z` (00:30 VN 18/09) ⇒ `dueNow` tính. Thẻ `due_at = 2026-09-18T17:00Z` lúc `2026-09-17T17:30Z` ⇒ **không** tính `dueToday` (đúng mốc nửa hở).
5. Idempotent: POST hai lần cùng `clientReviewId` ⇒ lần 2 `200`, `duplicate: true`, `COUNT(srs_review_logs)` = 1, `COUNT(study_events kind=srs_review)` = 1, `reps` = 1; hai request song song (`Task.WhenAll`) cùng id ⇒ vẫn 1 log; cùng id cho thẻ khác ⇒ 409.
6. Thẻ của người khác ⇒ 404; thẻ tạm dừng ⇒ 422 `CARD_SUSPENDED`; `rating = "ok"` ⇒ 400.
7. Lượt đầu G=good ⇒ `card.state = learning`, `dueAt = now + 10 phút`; queue lúc `now + 5 phút` có thẻ đó ở nhóm học trước (R7-7 bước 4).
8. `daily_review_limit` = 10 và 15 thẻ review đến hạn ⇒ queue chỉ trả 10 thẻ review.
9. `PUT /api/me/learning-settings` `desiredRetention: 0.99` ⇒ 400; `0.85` ⇒ 200 và lượt chấm sau dùng r = 0,85 (so với V7).
10. `GET /api/dictionary/words/{id}` sau khi có thẻ ⇒ `srs.cardId` đúng; chưa có ⇒ `srs: null`.
11. Learner không có `study.use` ⇒ 403 cho `/api/srs/*`.

### 5.3 Frontend (agent `frontend-implement`, **model Fable**)

Quy ước chung: MUI v9 (`slotProps`, shorthand chỉ trong `sx`); `AppDialog`/`AppDrawer` thay `Dialog`/`Drawer` trần; `useTabParam` cho tab cấp trang; không `uuid` (dùng `crypto.randomUUID()`); mọi phần tử chứa chữ Hán dùng `LangText lang="zh-CN"` (hoặc `lang="zh-CN"` + `fontFamily` có `AF_FONT_CJK`); hiển thị pinyin dạng dấu bằng `apps/chinese/src/lib/pinyin.ts` (F5); gọi API qua `chineseApi` + React Query; mọi màn kiểm ở **375px** trước. Không thêm dependency mới.

#### 5.3.1 F6.3 — Tra từ

**File**

| File | Nội dung |
|---|---|
| `frontend/packages/ui/src/hooks/useBackTo.ts` | `useBackTo(fallback: string)` → `() => void`: nếu `location.state?.from` là đường dẫn nội bộ (bắt đầu `/`, không `//`) ⇒ `navigate(from)`; ngược lại `navigate(fallback)`. Kèm `linkState(location)` → `{ from: pathname + search }` để truyền khi mở trang con |
| `frontend/packages/ui/src/hooks/useScrollRestore.ts` | `useScrollRestore(key: string, ready: boolean)`: lưu `window.scrollY` vào `sessionStorage['af.scroll.' + key]` khi rời trang (cleanup + `pagehide`); khôi phục một lần khi `ready` = true |
| `frontend/packages/ui/src/index.ts` | export hai hook trên |
| `apps/chinese/src/features/dictionary/types.ts` | Kiểu theo §6.1 |
| `apps/chinese/src/features/dictionary/api.ts` | `searchWords(params)`, `getWord(id)`, `getCharacter(hanzi)` (nhớ `encodeURIComponent`) |
| `apps/chinese/src/features/dictionary/hooks.ts` | `useWordSearch` (`placeholderData: keepPreviousData`, `staleTime` 5 phút), `useWord`, `useCharacter` |
| `apps/chinese/src/features/dictionary/lib/pos.ts` | Bảng mã từ loại → nhãn Việt: `n` danh từ, `v` động từ, `a` tính từ, `d` phó từ, `r` đại từ, `m` số từ, `q` lượng từ, `p` giới từ, `c` liên từ, `u` trợ từ, `y` trợ từ ngữ khí, `e` thán từ, `t` từ chỉ thời gian, `f` từ phương vị, `s` từ chỉ nơi chốn, `nr` tên người, `ns` địa danh, `nz` danh từ riêng, `i` thành ngữ, `l` cụm cố định, `o` từ tượng thanh; mã khác ⇒ **ẩn** |
| `apps/chinese/src/features/dictionary/lib/sources.ts` | Khoá nguồn → `{ label, license, url }` (§5.4.6) |
| `apps/chinese/src/lib/useDebouncedValue.ts` | `useDebouncedValue(value, 300)` |
| `apps/chinese/src/features/dictionary/components/WordListItem.tsx` | Một dòng kết quả |
| `.../components/MeaningStatusChip.tsx` | `machine` ⇒ `Chip size="small" variant="outlined" color="warning" label="Chưa duyệt"` + `Tooltip` "Nghĩa dịch máy, có thể chưa chính xác — đối chiếu nghĩa tiếng Anh" (bọc `span` nếu phần tử bị disabled) |
| `.../components/SpeakButton.tsx` | `IconButton` loa dùng `useSpeech('zh')` (F5); `unsupported` ⇒ `IconButton disabled` bọc `span` + `Tooltip` "Máy chưa có giọng đọc tiếng Trung" |
| `.../components/SourceAttribution.tsx` | Dòng chữ nhỏ cuối trang: "Nguồn: HSK 3.0 (danh sách chính thức) · CC-CEDICT (CC BY-SA 4.0) · CVDICT – Phong Phan (CC BY-SA 4.0) · Unicode Unihan · Hán Việt do AntFarm biên soạn (CC BY-SA 4.0). Dữ liệu đã được chỉnh sửa." kèm liên kết giấy phép |
| `.../pages/DictionarySearchPage.tsx` | `/tu-dien` |
| `.../pages/WordDetailPage.tsx` | `/tu-dien/:id` |
| `.../pages/CharacterDetailPage.tsx` | `/tu-dien/chu/:hanzi` |
| `apps/chinese/src/router.tsx`, `layout/AppShell.tsx` | 3 route trong `RequireAuth` + `RequirePermission("study.use")`; mục menu "Từ điển" (icon `MenuBook`, `requiredPermission: 'study.use'`) |

**`/tu-dien?q=&hsk=&page=`**
- Ô tìm (`TextField` full width, `type="search"`, `autoFocus` chỉ trên màn ≥ md, placeholder "Chữ Hán, pinyin (ni3hao3 / nǐhǎo / nihao), nghĩa hoặc Hán Việt"), dính đầu trang (`position: sticky`, nền `background.default`). Gõ ⇒ debounce 300 ms ⇒ cập nhật URL bằng `setSearchParams(..., { replace: true })`, đặt `page` về 1. URL là nguồn sự thật (tải lại trang giữ kết quả).
- Chip lọc "HSK 1" (bật ⇒ `hsk=1`; mặc định tắt — kho hiện chỉ có cấp 1 nhưng giữ cho sau này).
- `q` rỗng ⇒ tiêu đề "Lộ trình HSK 1 (500 từ)" và liệt kê theo `path_order`.
- Dòng kết quả (`ListItemButton`, `component={Link}`, `state={linkState(location)}`, cao tối thiểu 64px): trái là chữ Hán cỡ 28px; giữa: dòng 1 pinyin dấu + Hán Việt in hoa nhỏ (`variant="caption"`, `textTransform: 'uppercase'`), dòng 2 tối đa 2 nghĩa Việt nối `; ` cắt một dòng (`noWrap`); phải: `MeaningStatusChip` khi `machine`. Không tràn ngang ở 375px.
- Phân trang: `Pagination` MUI `size="small"`, `siblingCount={0}` dưới danh sách; đổi trang ⇒ `page` trên URL + cuộn lên đầu. `useScrollRestore('tu-dien:' + search, !isLoading)` để quay lại từ chi tiết giữ vị trí.
- Trạng thái: đang tải ⇒ 6 `Skeleton`; lỗi ⇒ `Alert severity="error"` + nút "Thử lại"; rỗng ⇒ "Không tìm thấy “{q}”." + gợi ý 3 cách gõ; 503 `CONTENT_UNAVAILABLE` ⇒ `Alert` "Học liệu chưa sẵn sàng".

**`/tu-dien/:id`**
- Nút quay lại (`useBackTo('/tu-dien')`).
- Khối đầu: chữ Hán 56–72px (`fontSize: { xs: 56, sm: 72 }`) + `SpeakButton` (đọc `simplified`, `rate` = `ttsRate` nếu F7 đã có cài đặt, mặc định 0.8); pinyin dấu cỡ 20px; phồn thể "Phồn thể: 愛" khi khác; biến thể "Dạng khác: 爸"; `usageNote` "Ví dụ dùng: 朋友们"; Hán Việt in hoa + chip "Hán Việt suy ra" khi `derived`.
- Chip: "HSK 3.0 · cấp 1", "HSK 2.0 · cấp N" (nếu có), "Đề thi 2026 · cấp N" (nếu có); từ loại qua `pos.ts`.
- "Nghĩa tiếng Việt": danh sách đánh số; tiêu đề kèm `MeaningStatusChip` + chú thích nguồn ("Dịch từ CVDICT" / "Dịch máy").
- "Nghĩa tiếng Anh (CC-CEDICT)": `Accordion` mặc định đóng.
- "Chữ trong từ": lưới thẻ (mỗi thẻ 72×88px: chữ 32px, Hán Việt, pinyin) link `/tu-dien/chu/:hanzi`.
- F7.2 chèn vào đây `AddToSrsButton` (§5.3.2).
- Cuối trang `SourceAttribution`. 404 ⇒ `createApiClient` tự điều hướng `/404`.

**`/tu-dien/chu/:hanzi`**
- Chữ 96px + `SpeakButton`; cách đọc (pinyin dấu, nối ` · `); Hán Việt (tất cả, phần tử đầu đậm); số nét; bộ thủ "爪 (bộ số 87)"; phồn thể.
- "Từ có chữ này" (≤ 20, dùng `WordListItem`). F8 sẽ thêm nút "Luyện viết" (chưa làm).

#### 5.3.2 F7.2 — Ôn tập

**File**

| File | Nội dung |
|---|---|
| `apps/chinese/src/features/srs/{types.ts,api.ts,hooks.ts}` | §6.2; `useSrsSummary` (`refetchOnWindowFocus: true`, `staleTime` 30 s), `useLearningSettings`, `useUpdateLearningSettings`, `useAddCards` (invalidate `summary` + `word`) |
| `apps/chinese/src/features/srs/lib/formatInterval.ts` (+ `formatInterval.test.ts`) | ISO-8601 duration → nhãn Việt: `< 1 giờ` ⇒ "N phút" (dưới 10 phút giữ một chữ số thập phân, dấu phẩy, bỏ `,0`; từ 10 phút làm tròn số nguyên); `< 1 ngày` ⇒ "N giờ"; `< 30 ngày` ⇒ "N ngày"; `< 365 ngày` ⇒ "N tháng" (ngày/30, 1 chữ số thập phân, dấu phẩy, bỏ `,0`); còn lại "N năm" (ngày/365, như trên). Ca test bắt buộc: `PT1M`→"1 phút", `PT5M30S`→"5,5 phút", `PT10M`→"10 phút", `PT15M`→"15 phút", `P1D`→"1 ngày", `P8D`→"8 ngày", `P45D`→"1,5 tháng", `P60D`→"2 tháng", `P498D`→"1,4 năm" |
| `apps/chinese/src/features/srs/lib/reviewOutbox.ts` (+ test) | Hàng đợi gửi lại: phần tử `{ clientReviewId, cardId, rating, durationMs, attempts }`; lưu `sessionStorage['af.srs.outbox']`; `enqueue`, `flush(send)` tuần tự; lỗi mạng/5xx ⇒ giữ, thử lại sau 1 s, 2 s, 5 s, 10 s, rồi mỗi 30 s; 4xx (trừ 408/429) ⇒ bỏ phần tử + báo lỗi; 409/422 ⇒ bỏ + toast; trùng `clientReviewId` không thêm lần hai |
| `apps/chinese/src/features/srs/pages/ReviewHomePage.tsx` | `/on-tap` |
| `apps/chinese/src/features/srs/pages/ReviewSessionPage.tsx` | `/on-tap/phien` |
| `.../components/Flashcard.tsx` | Mặt trước/sau |
| `.../components/RatingBar.tsx` | 4 nút |
| `.../components/SessionHeader.tsx` | Nút đóng (X) + `LinearProgress` + "đã ôn/tổng" |
| `.../components/SessionSummary.tsx` | Màn kết thúc |
| `.../components/PendingReviewsBanner.tsx` | `Alert severity="warning"` "Đang chờ gửi N đánh giá — sẽ tự gửi lại khi có mạng" |
| `.../components/AddToSrsButton.tsx` | Dùng ở chi tiết từ |
| `apps/chinese/src/features/profile/components/LearningSettingsTab.tsx` | Tab "Học tập" của `/ho-so` (F4) |
| `frontend/packages/ui/src/components/layout/AppLayout.tsx` | Thêm prop `hideBottomNav?: boolean` nếu chưa có |
| `apps/chinese/src/router.tsx`, `layout/AppShell.tsx` | Route `/on-tap`, `/on-tap/phien`; mục menu "Ôn tập" (icon `Style`, `Badge badgeContent={dueNow + newAvailableToday}` tối đa 99); `hideBottomNav` khi `matchPath('/on-tap/phien')` |

**`/on-tap`**
- 3 ô số (`Card` xếp 1 cột ở xs, 3 cột ở sm+): "Đến hạn hôm nay" (`dueToday`), "Từ mới còn học được" (`newAvailableToday`/`dailyNewCards`), "Đã ôn hôm nay" (`reviewedToday`).
- Nút chính full width cao 56px: "Bắt đầu ôn ({dueNow + newAvailableToday})" ⇒ `/on-tap/phien`. Bằng 0 ⇒ ẩn nút, hiện "Hôm nay xong rồi!" + "Lượt ôn kế tiếp: {nextDueAt định dạng `HH:mm dd/MM` theo múi giờ người học (`summary.timeZone`, `Intl.DateTimeFormat('vi-VN', { timeZone })`)}".
- Dòng phụ: "Từ vững: {matureCards}/500" + liên kết "Cài đặt học tập" ⇒ `/ho-so?tab=hoc-tap`.
- `reviewLimitRemaining = 0` và còn thẻ đến hạn ⇒ `Alert info` "Đã đạt giới hạn {dailyReviewLimit} lượt ôn hôm nay".

**`/on-tap/phien`** (bố cục cột toàn chiều cao: `height: '100dvh'`, `display: 'flex'`, `flexDirection: 'column'`)
- Đầu: `SessionHeader`; `PendingReviewsBanner` khi outbox > 0.
- Thân (`flex: 1`, `overflowY: 'auto'`, căn giữa): **mặt trước** chữ Hán `fontSize: { xs: 72, sm: 96 }` + `SpeakButton`; `autoPlayAudio` bật ⇒ tự đọc khi thẻ hiện (không đọc lại khi lật). Chip nhỏ "Mới"/"Học lại" theo `state`. Nút "Hiện đáp án" (outlined, full width, cao 56px). **Mặt sau** (sau khi lật; mặt trước vẫn ở trên): pinyin dấu 24px, Hán Việt in hoa, tối đa 3 nghĩa Việt, `MeaningStatusChip` khi `machine`, liên kết "Xem chi tiết" mở `/tu-dien/:id` trong `AppDrawer` **không** rời phiên (hoặc tab mới — chọn `AppDrawer` kèm `closeOnBackdrop` có bình luận "chỉ đọc").
- Đáy (`position: sticky; bottom: 0`, `pb: 'calc(8px + env(safe-area-inset-bottom))'`): `RatingBar` chỉ hiện sau khi lật — lưới 4 cột bằng nhau (`gridTemplateColumns: 'repeat(4, 1fr)'`, `gap: 1`), mỗi nút cao ≥ 56px, hai dòng: nhãn đậm + khoảng dự kiến nhỏ (`formatInterval(card.intervals.x)`):

| Nút | rating | màu | phím |
|---|---|---|---|
| Quên | `again` | `error` | `1` |
| Khó | `hard` | `warning` | `2` |
| Được | `good` | `success` | `3` |
| Dễ | `easy` | `info` | `4` |

- Phím: `Space`/`Enter` ⇒ lật (khi chưa lật); `1–4` ⇒ chấm (chỉ khi đã lật); bỏ qua khi `event.repeat`, khi focus trong ô nhập, khi có `AppDrawer` mở. Nút bị khoá 300 ms sau khi chấm (chống bấm đúp).
- Chấm: `durationMs` = từ lúc thẻ hiện tới lúc bấm; sinh `clientReviewId = crypto.randomUUID()`; **lạc quan**: chuyển thẻ kế ngay, gửi qua `reviewOutbox`; thành công ⇒ cập nhật `summary` từ response.
- Bộ bài: lần đầu `GET queue?limit=20`; còn ≤ 5 thẻ chưa chấm ⇒ tải thêm, **bỏ** `cardId` đã có trong bộ hoặc đã chấm trong phiên mà `state` trả về không phải `learning/relearning` đến hạn; thẻ `again` sẽ quay lại qua nhóm "học trước" của server. Hết thẻ và tải thêm trả rỗng ⇒ `SessionSummary`.
- `SessionSummary`: tổng số thẻ, số lượt theo 4 mức (thanh ngang màu), thời gian phiên, nút "Về trang ôn tập" + "Ôn tiếp" (khi server còn thẻ).
- Đóng phiên khi outbox còn phần tử ⇒ `useConfirm` "Còn N đánh giá chưa gửi. Rời đi vẫn giữ để gửi lại khi quay lại phiên?" (outbox trong `sessionStorage` nên mở lại phiên sẽ gửi tiếp). Không có quyền `study.use` ⇒ route chặn.
- Lỗi tải hàng đợi ⇒ `Alert` + "Thử lại"; 503 ⇒ "Học liệu chưa sẵn sàng".

**Chi tiết từ — `AddToSrsButton`**: `word.srs == null` ⇒ nút "Thêm vào ôn tập" (`POST /srs/cards`), xong ⇒ toast "Đã thêm — thẻ sẽ xuất hiện trong lượt từ mới" ; có thẻ ⇒ chip "Đang ôn · đến hạn {dd/MM}" (`new` ⇒ "Chờ học"), `isSuspended` ⇒ chip "Đã tạm dừng" + nút "Tiếp tục ôn" (`PUT suspension`).

**`/ho-so?tab=hoc-tap`** (`LearningSettingsTab`): `Slider`/ô số "Từ mới mỗi ngày" (0–50, bước 1), "Giới hạn lượt ôn/ngày" (10–1000), "Độ nhớ mục tiêu" (80–97%, bước 1, giải thích: "Cao hơn ⇒ ôn dày hơn, nhớ chắc hơn"), "Tốc độ đọc" (0,5–1,2 + nút nghe thử 你好), `Switch` "Tự đọc khi hiện thẻ"; nút "Lưu" (invalidate `summary`); lỗi 400 hiện dưới ô theo `details`. Nếu F4 chưa có `/ho-so` dạng tab ⇒ tạo trang `/ho-so` tối thiểu có `useTabParam` với 1 tab và báo Orchestrator.

### 5.4 Học liệu (agent `content-implement`, Sonnet) — F6.1

#### 5.4.1 Nguồn đã chọn (xác minh 17/09/2026)

| Khoá | Nguồn | Phiên bản ghim | Giấy phép | Phần dùng | Nghĩa vụ |
|---|---|---|---|---|---|
| `hsk30-official` | github.com/elkmovie/hsk30 — `wordlist.txt` (OCR bởi Pleco từ PDF chính thức GF0025-2021 của Bộ Giáo dục TQ) | commit `7f3d4fdcfcb6e826001df062747c943d1fa8160e` | MIT, © 2021 Pleco Inc. | Mục `一级词汇表` (500 dòng): số thứ tự, mục từ, biến thể, nhãn | Kèm toàn văn MIT |
| `complete-hsk-vocabulary` | github.com/drkameleon/complete-hsk-vocabulary — `complete.json` | commit `7ac65bf1a6387d35f1ade478906172a19311c7f9` (23/03/2026, đã có cấp `newest-*` theo đề cương 2026) | MIT, © 2026 Yanis Zafirópulos (nghĩa Anh bên trong lấy từ CC-CEDICT ⇒ CC BY-SA 4.0) | Cách đọc (`numeric`), phồn thể, nghĩa Anh, `level` (`new-*`, `old-*`, `newest-*`), `frequency`, `pos` | Kèm MIT; nghĩa Anh theo `cc-cedict` |
| `cc-cedict` | CC-CEDICT (MDBG), qua `complete-hsk-vocabulary` và `cvdict` — **không tải riêng** | theo hai nguồn trên | CC BY-SA 4.0 | Nghĩa Anh, cách đọc | Ghi công "CC-CEDICT, MDBG", ghi đã chỉnh sửa, cùng giấy phép |
| `cvdict` | github.com/ph0ngp/CVDICT — `CVDICT.u8` (v1.0.1, 122.591 mục, định dạng CEDICT) | commit `c379d909e308343a247e51619f7839a2060a271c` | CC BY-SA 4.0 (README + đầu tệp), © Phong Phan | Nghĩa Việt (đã lọc §5.4.3) | Ghi công "CVDICT – Phong Phan", ghi đã chỉnh sửa, cùng giấy phép |
| `unihan` | Unicode Unihan 18.0.0 `https://www.unicode.org/Public/18.0.0/ucd/Unihan.zip` (`Unihan_Readings.txt`: `kMandarin`, `kVietnamese`; `Unihan_Variants.txt`: `kTraditionalVariant`; `Unihan_IRGSources.txt`: `kTotalStrokes`, `kRSUnicode`) + `CJKRadicals.txt` 18.0.0 | 18.0.0 (Unihan ghi ngày 2026-07-31) | Unicode License v3 (https://www.unicode.org/license.txt) | Số nét, bộ thủ, cách đọc chữ, lọc phồn thể; `kVietnamese` chỉ làm **tư liệu đối chiếu** Hán Việt | Kèm thông báo bản quyền + toàn văn giấy phép |
| `wiktionary` | English Wiktionary, mẫu `{{vi-readings|hanviet=...}}` qua MediaWiki API | lấy ngày chạy script (ghi vào lock) | CC BY-SA 4.0 | **Tư liệu đối chiếu** Hán Việt (không chép thẳng vào dữ liệu) | Ghi công "Wiktionary contributors" |
| `han-viet-curated` | `content/chinese/sources/han-viet.json` do dự án biên soạn, đối chiếu `wiktionary` + `unihan` | trong repo | CC BY-SA 4.0 (dẫn xuất) | Hán Việt cấp chữ + `byPinyin` | — |
| `machine` | `content/chinese/sources/meaning-vi-machine.json` — agent dịch từ nghĩa Anh CC-CEDICT | trong repo | CC BY-SA 4.0 (dẫn xuất) | Nghĩa Việt khi CVDICT thiếu/lọc rỗng | Đánh dấu `machine` |
| `hsk1-overrides` | `content/chinese/sources/hsk1-overrides.json` — dự án chọn cách đọc/phồn thể (dữ kiện ngôn ngữ) | trong repo | thuộc dự án | Chọn cách đọc cho 62 mục đa âm, phồn thể | — |

**Đã loại:** `KanjiDictVN` (nghĩa/âm lấy từ hvdic.thivien.net — không giấy phép); `go-hanviet` (dữ liệu từ vietnamtudien.org — không giấy phép); `makemeahanzi dictionary.txt` (bộ thủ — không cần, đã có Unihan); `krmanik/HSK-3.0-words-list` (không truy cập được qua API ngày kiểm).

`content/chinese/sources/sources.lock.json` (commit):

```json
{
  "datasetVersion": "2026-09-17",
  "files": {
    "complete-hsk-vocabulary": { "url": "https://raw.githubusercontent.com/drkameleon/complete-hsk-vocabulary/7ac65bf1a6387d35f1ade478906172a19311c7f9/complete.json", "sha256": "c869a0ce353279c9333d9b42c31fc3549785e8b40673dab57ee42bc99cd14131", "bytes": 9763716, "raw": "complete.json" },
    "hsk30-official":          { "url": "https://raw.githubusercontent.com/elkmovie/hsk30/7f3d4fdcfcb6e826001df062747c943d1fa8160e/wordlist.txt", "sha256": "4a8cfdc3a8fa85ced837c71aa187454c4d0ee36a1f70765655a93acb6a463af3", "bytes": 127807, "raw": "hsk30-wordlist.txt" },
    "cvdict":                  { "url": "https://raw.githubusercontent.com/ph0ngp/CVDICT/c379d909e308343a247e51619f7839a2060a271c/CVDICT.u8", "sha256": "4dde4b204193efa9c192d7f7daeab1bb579c8ccd7c41ed90d1b6caee22ba0948", "bytes": 10803314, "raw": "CVDICT.u8" },
    "unihan":                  { "url": "https://www.unicode.org/Public/18.0.0/ucd/Unihan.zip", "sha256": "4c93ea9c1f636451729a840978f1667a53886af37ba854fdcce109721c63d43e", "bytes": 8340649, "raw": "Unihan.zip" },
    "cjk-radicals":            { "url": "https://www.unicode.org/Public/18.0.0/ucd/CJKRadicals.txt", "sha256": "689e2e5852699e7115823dc0b6d3e615eb767c7f477b615678f4f989d1f06105", "raw": "CJKRadicals.txt" }
  },
  "wiktionary": { "fetchedAt": null, "titles": 0 }
}
```

(`sha256` do BA đo ngày 17/09/2026; `fetch-sources.mjs` so khớp, lệch ⇒ dừng báo lỗi. `datasetVersion` là giá trị ghi vào tệp đầu ra — đổi tay khi đổi dữ liệu.)

#### 5.4.2 Script & cấu trúc

```
content/
  package.json            # F5 tạo; F6 thêm devDependency "fflate": "^0.8.3" (MIT, giải nén Unihan.zip)
                          # scripts: "fetch:chinese": "node chinese/scripts/fetch-sources.mjs",
                          #          "hanviet:chinese": "node chinese/scripts/fetch-wiktionary-hanviet.mjs",
                          #          "build:chinese": "node chinese/scripts/build-hsk.mjs",
                          #          "validate:chinese": "node chinese/scripts/validate.mjs"
  chinese/
    sources/              # ĐẦU VÀO biên soạn — commit; backend KHÔNG nạp thư mục này
      sources.lock.json
      hsk1-overrides.json
      han-viet.json
      meaning-vi-machine.json
    .raw/                 # gitignore — tệp tải về + báo cáo
    scripts/
      lib/pinyin.mjs      # chuẩn hoá số thanh, dấu → số (dùng lại của F5 nếu có)
      lib/cedict.mjs      # đọc định dạng CEDICT
      lib/unihan.mjs      # đọc Unihan (tab-separated), CJKRadicals
      fetch-sources.mjs
      fetch-wiktionary-hanviet.mjs
      build-hsk.mjs
      validate.mjs        # F5 tạo; F6 thêm kiểm hsk-words/characters/sources
    schemas/
      hsk-words.schema.json
      characters.schema.json
      han-viet.schema.json
      hsk1-overrides.schema.json
      meaning-vi-machine.schema.json
    data/vocabulary/hsk-words.json        # ĐẦU RA (commit)
    data/characters/characters.json       # ĐẦU RA (commit)
```

Node ≥ 22, ESM, chỉ dùng thư viện chuẩn + `fflate`. Mọi tệp đọc/ghi UTF-8, chuẩn hoá **NFC**.

- **`fetch-sources.mjs`**: với mỗi mục `files` trong lock: tải về `.raw/<raw>` (bỏ qua nếu đã có và đúng hash), kiểm SHA-256; giải nén 3 tệp Unihan cần dùng vào `.raw/unihan/`. Tham số `--update-lock` (chỉ dùng khi chủ động nâng phiên bản) ghi hash mới.
- **`fetch-wiktionary-hanviet.mjs`**: danh sách tiêu đề = mọi chữ trong kho + `kTraditionalVariant` của chúng; gọi `https://en.wiktionary.org/w/api.php?action=query&prop=revisions&rvprop=content&rvslots=main&format=json&formatversion=2&titles=<≤50 tiêu đề>` tuần tự, nghỉ **1,5 s** giữa lô, header `User-Agent: AntFarm-content/0.1 (liên hệ qua repo)`; trích trong đoạn `==Vietnamese==`: `hanviet=` (bỏ hậu tố nguồn sau `-`, tách `,`) và `reading=` (tách `,`, bỏ `[[ ]]`); ghi `.raw/wiktionary-hanviet.json`; cập nhật `wiktionary.fetchedAt`, `titles` trong lock. Chạy tay, không bắt buộc cho build.
- **`build-hsk.mjs`**: §5.4.3. Ghi báo cáo `.raw/build-report.md` (WARN, ứng viên Hán Việt).

#### 5.4.3 `build-hsk.mjs` — thuật toán

```
1. Đọc lock, kiểm hash tệp .raw (thiếu ⇒ "Chạy yarn --cwd content fetch:chinese trước").
2. Danh sách chính thức: đọc hsk30-wordlist.txt, lấy các dòng giữa '一级词汇表' và '二级词汇表',
   mỗi dòng "<idx> <raw>". Phân tích raw (R6-2):
     raw có 'X（Y）Z' với Z khác rỗng ⇒ head = X+Z, variants += [X+Y+Z]
     raw kết thúc '（Y）': Y ⊂ [名动形副代量数介连助叹、] ⇒ posHint = Y ; ngược lại usageNote = Y
     phần trước ngoặc tách '｜' ⇒ head = phần đầu, variants += phần còn lại
   Phải ra đúng 500 dòng, idx 1..500 liên tục.
3. complete.json ⇒ Map theo simplified. Không có head ⇒ LỖI.
4. Cách đọc (R6-4, R6-5): readings = numeric của forms, 'ü'→'v', âm tiết thiếu số ⇒ +5 (WARN);
   ưu tiên overrides.byOfficialIndex[idx].pinyin (phải thuộc readings — nếu không ⇒ LỖI);
   ngược lại: readingsLower = readings chữ thường khác nhau; =1 ⇒ lấy; =0 ⇒ lấy dạng viết hoa đầu tiên;
   >1 ⇒ LỖI "cần override idx=… (head) các cách đọc: …".
5. Phồn thể (R6-6): cands = traditional của forms có numeric (đã chuẩn hoá) == pinyin (không phân biệt hoa);
   lọc giữ t mà mỗi ký tự t[i] ∈ kTraditionalVariant(head[i]) (chữ không có kTraditionalVariant ⇒ t[i] == head[i]);
   override.traditional có ⇒ lấy; còn đúng 1 ⇒ lấy; >1 ⇒ lấy cands đầu + WARN; 0 ⇒ cands gốc đầu + WARN.
   traditional == head ⇒ null.
6. Nghĩa Anh: gộp meanings của forms cùng pinyin (ưu tiên form có traditional đã chọn trước), bỏ trùng, tối đa 8.
7. Nghĩa Việt (D5): tra CVDICT theo (trad đã chọn ?? head, head, pinyin) — pinyin CVDICT chuẩn hoá 'u:'→'v',
   so khớp phân biệt hoa trước rồi không phân biệt; không thấy ⇒ theo (head, pinyin) chỉ lấy mục chữ thường;
   nhiều mục ⇒ mục cùng traditional, không có thì mục đầu + WARN.
   Tách nghĩa theo '/', trim, LỌC BỎ nghĩa khớp một trong các mẫu (không phân biệt hoa):
     ^họ\b.*\[                         (họ người: "họ [Gan1]")
     \((tục|lóng|tiếng lóng|thông tục)\)
     ^(biến thể của|viết tắt của|dùng trong|cũng viết là|cũng đọc là|xem |tiếng Đài Loan đọc là|phiên âm)
     ^\((cổ|cổ điển|văn học|văn viết|phương ngữ)\)
     ^LT:                              (lượng từ đi kèm)
     [A-Za-z]+[1-5]\]                  (tham chiếu pinyin kiểu "[gan1]")
   giữ tối đa 6 nghĩa theo thứ tự; nghĩa > 120 ký tự ⇒ bỏ.
   Sau lọc rỗng hoặc không có mục CVDICT ⇒ dùng meaning-vi-machine.json (khoá simplified+pinyin),
   thiếu ⇒ LỖI "cần dịch máy cho …" (in kèm nghĩa Anh để agent dịch).
   meaningViStatus = "machine"; meaningViSource = "cvdict" | "machine".
   (Đo ngày 17/09: CVDICT khớp 574/576 cặp (chữ, cách đọc) của tập cấp 1; thiếu 车上 che1 shang4, 真的 zhen1 de5.)
8. Cấp: hsk3Level = 1; hsk2Level = min(old-N) | null; hskExam2026Level = min(newest-N) | null;
   frequencyRank = frequency; pos = pos (giữ nguyên thứ tự).
9. Chữ: tập chữ = mọi code point Hán của head (+ variants KHÔNG tính). Với mỗi chữ:
   pinyinReadings = kMandarin (đổi dấu → số, lower) ∪ âm tiết tương ứng trong các từ chứa nó (thứ tự: kMandarin trước);
   traditionalVariants = kTraditionalVariant (bỏ chính nó);
   strokeCount = số đầu của kTotalStrokes; radicalNumber/radical = kRSUnicode đầu tiên "87.6" ⇒ 87 ⇒ CJKRadicals
     cột 3 (chữ CJK Unified); dạng "120'.5" ⇒ khoá "120'".
   Hán Việt ⇐ han-viet.json (R6-7): thiếu chữ ⇒ LỖI; readings.length > 1 thì byPinyin phải có mọi âm tiết
   (lower, có số) mà chữ đó mang trong các từ ⇒ thiếu ⇒ LỖI liệt kê.
   hanVietStatus = "derived"; sources = ["unihan","han-viet-curated"].
   Ghi .raw/build-report.md: bảng chữ | kVietnamese (chữ & phồn thể) | wiktionary hanviet | wiktionary reading | giá trị đang dùng
   — để người/agent đối chiếu.
10. Hán Việt cấp từ (R6-8): với chữ i, âm tiết s_i (lower): hv = byPinyin[s_i] ?? (readings.length==1 ? readings[0] : LỖI);
    bỏ chuỗi rỗng; nối ' '. hanVietStatus = "derived". Số âm tiết ≠ số chữ ⇒ chỉ chấp nhận khi âm tiết thừa là 'r5' ở cuối
    (erhua) — khi đó chữ 儿 ứng với 'r5'.
11. pathOrder (R6-9): tầng (old-1 ⇒ 1; newest-1 ⇒ 2; còn lại 3), rồi frequencyRank, rồi officialIndex ⇒ đánh 1..500.
    (Đo ngày 17/09: tầng 1 = 136, tầng 2 = 131, tầng 3 = 233.)
12. sources từ = ["hsk30-official","complete-hsk-vocabulary","cc-cedict", "cvdict"|"machine", "han-viet-curated"].
13. Ghi JSON: words sắp theo pathOrder; characters sắp theo code point; JSON.stringify(obj, null, 2) + "\n";
    KHÔNG có thời điểm build (R6-12). Kiểm: chạy build hai lần ⇒ hash đầu ra giống nhau.
14. Tự chạy validate; in tóm tắt: số từ, số chữ, số WARN, số nghĩa cvdict/machine.
```

#### 5.4.4 `sources/hsk1-overrides.json` — nội dung khởi tạo (BA soạn theo thứ tự chữ cái của danh sách chính thức + từ điển chuẩn; content-implement đối chiếu `.raw/build-report.md`, sửa nếu thấy sai và ghi lý do)

Định dạng: `{ "byOfficialIndex": { "<idx>": { "pinyin"?: "…", "traditional"?: "…", "note"?: "…" } } }`.

**Cách đọc (62 mục):**

| idx | từ | pinyin | idx | từ | pinyin | idx | từ | pinyin |
|---|---|---|---|---|---|---|---|---|
| 5 | 吧 | ba5 | 117 | 告诉 | gao4 su5 | 250 | 哪 | na3 |
| 24 | 比 | bi3 | 120 | 个 | ge4 | 254 | 那（代） | na4 |
| 25 | 别（副） | bie2 | 121 | 给 | gei3 | 268 | 难 | nan2 |
| 37 | 差 | cha4 | 131 | 过 | guo4 | 269 | 呢 | ne5 |
| 42 | 车 | che1 | 132 | 还 | hai2 | 276 | 女 | nv3 |
| 49 | 出来 | chu1 lai2 | 138 | 好（形） | hao3 | 280 | 女人 | nv3 ren2 |
| 56 | 打（动） | da3 | 139 | 好吃 | hao3 chi1 | 283 | 跑 | pao3 |
| 61 | 大 | da4 | 143 | 号 | hao4 | 289 | 起来 | qi3 lai5 |
| 66 | 地 | de5 | 144 | 喝 | he1 | 315 | 上 | shang4 |
| 67 | 的 | de5 | 145 | 和 | he2 | 324 | 少 | shao3 |
| 69 | 地 | di4 | 153 | 还 | huan2 | 349 | 说 | shuo1 |
| 71 | 地方 | di4 fang5 | 160 | 会（动） | hui4 | 361 | 听 | ting1 |
| 86 | 东西 | dong1 xi5 | 165 | 几 | ji3 | 416 | 行 | xing2 |
| 89 | 都 | dou1 | 172 | 间 | jian1 | 423 | 要（动） | yao4 |
| 90 | 读 | du2 | 173 | 见 | jian4 | 426 | 页 | ye4 |
| 95 | 多少 | duo1 shao5 | 175 | 教 | jiao1 | 449 | 雨 | yu3 |
| 109 | 分（名、量） | fen1 | 192 | 看 | kan4 | 451 | 远 | yuan3 |
| 111 | 干 | gan1 | 210 | 了 | le5 | 469 | 着 | zhe5 |
| 113 | 干 | gan4 | 211 | 累 | lei4 | 472 | 正（副） | zheng4 |
|  |  |  | 227 | 吗 | ma5 | 476 | 中 | zhong1 |
|  |  |  | 232 | 没 | mei2 | 483 | 重 | zhong4 |
|  |  |  |  |  |  | 489 | 子（桌子） | zi5 |

Căn cứ thứ tự chữ cái cho mục trùng: 66 地 (`de`, trước 67 的 `de`, 68 等 `deng`) ⇒ `de5`; 69 地 (sau 等) ⇒ `di4`; 111 干 (trước 干净) ⇒ `gan1`, 113 (trước 干什么) ⇒ `gan4`; 132 还 (trước 还是) ⇒ `hai2`, 153 还 (trước 回) ⇒ `huan2`; 469 着 (giữa 这些 và 真) ⇒ `zhe5`.

**Phồn thể (13 mục cần chỉ định):** 48 出 → `出` · 54 从 → `從` · 111 干 → `乾` · 112 干净 → `乾淨` · 147 后 → `後` · 154 回 → `回` · 169 家 → `家` · 213 里 → `裡` · 251 哪里 → `哪裡` · 256 那里 → `那裡` · 349 说 → `說` · 460 怎么 → `怎麼` · 466 这里 → `這裡`. (Theo chuẩn chữ phồn thể Đài Loan; 113 干 `gan4` tự ra `幹`.)

#### 5.4.5 Schema JSON (JSON Schema draft 2020-12, `additionalProperties: false` ở mọi object)

**`data/vocabulary/hsk-words.json`**

```json
{
  "dataset": "hsk-words",
  "version": "2026-09-17",
  "standard": "HSK 3.0 (GF0025-2021)",
  "license": "CC-BY-SA-4.0",
  "counts": { "words": 500, "byHsk3Level": { "1": 500 }, "meaningViSource": { "cvdict": 498, "machine": 2 } },
  "words": [
    {
      "officialIndex": 1,
      "simplified": "爱",
      "traditional": "愛",
      "variants": [],
      "pinyin": "ai4",
      "hsk3Level": 1,
      "hsk2Level": 1,
      "hskExam2026Level": 1,
      "pathOrder": 12,
      "frequencyRank": 130,
      "pos": ["v", "vn", "b"],
      "usageNote": null,
      "meaningsEn": ["to love; to be fond of; to like", "affection", "to be inclined (to do sth); to tend to (happen)"],
      "meaningsVi": ["yêu; thích", "tình cảm", "có khuynh hướng (làm gì đó); có xu hướng (xảy ra)"],
      "meaningViStatus": "machine",
      "meaningViSource": "cvdict",
      "hanViet": "ái",
      "hanVietStatus": "derived",
      "sources": ["hsk30-official", "complete-hsk-vocabulary", "cc-cedict", "cvdict", "han-viet-curated"]
    }
  ]
}
```

(Số `counts`/`pathOrder` trong ví dụ là minh hoạ — giá trị thật do build tính.)

| Trường | Kiểu / luật |
|---|---|
| `officialIndex` | integer 1..9999 \| null; duy nhất trong cùng `hsk3Level` |
| `simplified` | string 1..32, mọi ký tự là chữ Hán (`\p{Script=Han}`) |
| `traditional` | string 1..32 \| null; cùng số ký tự với `simplified`; khác `simplified` |
| `variants` | string[] (mỗi phần tử 1..32, không trùng `simplified`) |
| `pinyin` | string, khớp `^[A-Za-z]+[1-5]( [A-Za-z]+[1-5])*$`; phần chữ lower thuộc bảng âm tiết (F5 `syllables.json`) hoặc `r`; số âm tiết = số chữ, trừ `r5` cuối ứng với `儿` |
| `hsk3Level` | 1..7 \| null · `hsk2Level` 1..6 \| null · `hskExam2026Level` 1..7 \| null |
| `pathOrder` | integer ≥ 1 \| null; duy nhất; với `hsk3Level=1` phải là 1..N liên tục |
| `frequencyRank` | integer ≥ 1 \| null |
| `pos` | string[] khớp `^[a-z]{1,3}$` |
| `usageNote` | string 1..64 \| null |
| `meaningsEn` | string[] 1..8 phần tử |
| `meaningsVi` | string[] 1..6 phần tử, mỗi phần tử 1..120 ký tự, NFC |
| `meaningViStatus` | `machine` \| `reviewed` · `meaningViSource` `cvdict` \| `machine` \| `manual` |
| `hanViet` | string 1..64 \| null (chữ thường tiếng Việt, cách đơn) · `hanVietStatus` `derived` \| `reviewed` \| null (null ⇔ hanViet null) |
| `sources` | string[] ≥ 1; mỗi khoá phải xuất hiện trong `SOURCES.md` |
| Toàn tệp | khoá `(simplified, pinyin)` duy nhất; `counts.byHsk3Level["1"]` = số mục thật = **500** (hằng `EXPECTED_HSK3_L1 = 500` trong `build-hsk.mjs` và `validate.mjs`) |

**`data/characters/characters.json`**

```json
{
  "dataset": "characters",
  "version": "2026-09-17",
  "license": "CC-BY-SA-4.0 (Hán Việt); Unicode-3.0 (Unihan)",
  "characters": [
    { "hanzi": "好", "traditionalVariants": [], "pinyinReadings": ["hao3", "hao4"],
      "hanViet": ["hảo", "hiếu"], "hanVietByPinyin": { "hao3": "hảo", "hao4": "hiếu" },
      "hanVietStatus": "derived", "strokeCount": 6, "radical": "女", "radicalNumber": 38,
      "sources": ["unihan", "han-viet-curated"] }
  ]
}
```

Luật: `hanzi` đúng 1 code point Hán, duy nhất; `pinyinReadings` ≥ 1, mỗi phần tử khớp `^[a-z]+[1-5]$`; `hanViet` string[] (có thể rỗng — WARN); `hanVietByPinyin` object \| null, khoá khớp `^[a-z]+[1-5]$`, giá trị string (được rỗng); `strokeCount` 1..64 \| null; `radicalNumber` 1..214 \| null; mọi chữ xuất hiện trong `hsk-words.json` phải có ở đây.

**`sources/han-viet.json`**: `{ "characters": [ { "hanzi": "好", "readings": ["hảo","hiếu"], "byPinyin": { "hao3": "hảo", "hao4": "hiếu" }, "basis": ["wiktionary","unihan"], "note": null } ] }` — `basis` ⊂ `wiktionary|unihan|manual`. Chữ chỉ dùng trong 儿化 thì `byPinyin: { "er2": "nhi", "r5": "" }`. Nguyên tắc biên soạn: `readings[0]` là âm Hán Việt phổ biến nhất; **không** ghi âm Nôm; không chắc ⇒ ghi `note` và để giá trị phổ biến nhất (vẫn `derived`, duyệt ở F10).

**`sources/meaning-vi-machine.json`**: `{ "entries": [ { "simplified": "车上", "pinyin": "che1 shang4", "meaningsVi": ["trên xe"], "fromEn": ["on the vehicle"] } ] }`.

#### 5.4.6 `SOURCES.md`, `LICENSES/`, `validate.mjs`

- `content/chinese/SOURCES.md` thêm mỗi khoá ở §5.4.1 một mục đủ cột: khoá · tên · URL · giấy phép · ngày lấy (17/09/2026) · commit/phiên bản · phần đã dùng · nghĩa vụ · tệp bị ảnh hưởng (`data/vocabulary/hsk-words.json`, `data/characters/characters.json`, `sources/*.json`). Thêm đoạn: "Dữ liệu trong `content/chinese/data/` và `content/chinese/sources/` dẫn xuất từ CC-CEDICT/CVDICT/Wiktionary được phân phối theo **CC BY-SA 4.0**; đã chỉnh sửa (lọc nghĩa, chọn cách đọc, bổ sung Hán Việt). Mã nguồn dự án không chịu giấy phép này." Thêm mục "Mã nguồn tham chiếu": py-fsrs (MIT, commit §5.2.6).
- `content/chinese/LICENSES/`: `CC-BY-SA-4.0.txt` (legalcode đầy đủ), `MIT-complete-hsk-vocabulary.txt`, `MIT-elkmovie-hsk30.txt`, `Unicode-License-v3.txt`, `MIT-py-fsrs.txt`.
- `validate.mjs` thêm: kiểm 2 tệp đầu ra theo schema + luật bảng trên; kiểm 3 tệp `sources/`; mọi khoá `sources[]` có trong `SOURCES.md`; `EXPECTED_HSK3_L1`; thiếu `.raw/` ⇒ chỉ WARN phần cần dữ liệu thô.

## 6. Hợp đồng API

Đường dẫn service (trình duyệt gọi `/chinese/api/...`). Bearer JWT audience `af-chinese`; mọi endpoint cần `study.use`. Lỗi theo §6.0 hợp đồng gốc (`{ error, code, details? }`, thông điệp tiếng Việt). JSON camelCase, thời điểm ISO-8601 UTC có `Z`, ngày `YYYY-MM-DD`, enum chuỗi snake_case.

### 6.1 F6 — từ điển (thay §6.5 gốc)

**`GET /api/dictionary/search?q=&hsk=&page=1&pageSize=20`**

| Tham số | Kiểu | Luật |
|---|---|---|
| `q` | string? | ≤ 64 ký tự sau chuẩn hoá |
| `hsk` | int? | 1..7 |
| `page` | int | ≥ 1, mặc định 1 |
| `pageSize` | int | 1..100, mặc định 20 |

```json
// 200
{
  "items": [
    { "id": "01925f3a-…", "simplified": "爱", "traditional": "愛", "pinyin": "ai4",
      "hsk3Level": 1, "hsk2Level": 1, "hanViet": "ái",
      "meaningsVi": ["yêu; thích", "tình cảm", "có khuynh hướng (làm gì đó); có xu hướng (xảy ra)"],
      "meaningViStatus": "machine", "matchKind": "pinyin" }
  ],
  "page": 1, "pageSize": 20, "totalCount": 1
}
```

`meaningsVi` trong kết quả tìm: tối đa 3 phần tử. `matchKind`: `browse|hanzi|pinyin|han_viet|meaning`. Trường null bị lược (`WhenWritingNull`) — frontend coi thiếu = null. Lỗi: 400 `VALIDATION` (`details.q`, `details.pageSize`...), 401, 403 `FORBIDDEN`.

**`GET /api/dictionary/words/{id}`**

```json
// 200
{
  "id": "01925f3a-…", "simplified": "爱", "traditional": "愛", "variants": [], "pinyin": "ai4",
  "hsk3Level": 1, "hsk2Level": 1, "hskExam2026Level": 1, "officialIndex": 1, "pathOrder": 12, "frequencyRank": 130,
  "pos": ["v", "vn", "b"], "usageNote": null,
  "meaningsEn": ["to love; to be fond of; to like", "affection", "to be inclined (to do sth); to tend to (happen)"],
  "meaningsVi": ["yêu; thích", "tình cảm", "có khuynh hướng (làm gì đó); có xu hướng (xảy ra)"],
  "meaningViStatus": "machine", "meaningViSource": "cvdict",
  "hanViet": "ái", "hanVietStatus": "derived",
  "sources": ["hsk30-official", "complete-hsk-vocabulary", "cc-cedict", "cvdict", "han-viet-curated"],
  "characters": [ { "hanzi": "爱", "pinyinReadings": ["ai4"], "hanViet": ["ái"], "strokeCount": 10 } ],
  "srs": null
}
```

`srs` (F7 thêm; trước F7 luôn `null`): `{ "cardId": "…", "state": "review", "dueAt": "2026-09-20T01:10:00Z", "isSuspended": false }`. Lỗi: 400 (id không phải uuid — route constraint `{id:guid}` ⇒ 404 cũng chấp nhận), 404 `NOT_FOUND`.

**`GET /api/dictionary/characters/{hanzi}`** (`hanzi` URL-encoded, đúng 1 code point Hán)

```json
// 200
{
  "hanzi": "好", "traditionalVariants": [], "pinyinReadings": ["hao3", "hao4"],
  "hanViet": ["hảo", "hiếu"], "hanVietByPinyin": { "hao3": "hảo", "hao4": "hiếu" }, "hanVietStatus": "derived",
  "strokeCount": 6, "radical": "女", "radicalNumber": 38,
  "words": [ { "id": "…", "simplified": "好", "pinyin": "hao3", "hsk3Level": 1, "hanViet": "hảo",
               "meaningsVi": ["tốt", "thích hợp; đúng"], "meaningViStatus": "machine", "matchKind": "hanzi" } ]
}
```

`words`: tối đa 20, sắp `path_order`. Lỗi: 400 `VALIDATION` (`details.hanzi`: "Cần đúng một chữ Hán"), 404 `NOT_FOUND`.

Học liệu chưa nạp (bảng `content.words` rỗng) ⇒ search/words/characters ném `ServiceUnavailableException` (F5) ⇒ 503 `CONTENT_UNAVAILABLE` ("Học liệu chưa được nạp — kiểm log khởi động").

### 6.2 F7 — SRS (thay §6.6 gốc)

**`GET /api/srs/summary`**

```json
{
  "localDate": "2026-09-18", "timeZone": "Asia/Ho_Chi_Minh",
  "dueToday": 14, "dueNow": 9,
  "reviewedToday": 12, "reviewsDoneToday": 5, "reviewLimitRemaining": 195, "dailyReviewLimit": 200,
  "newIntroducedToday": 3, "newAvailableToday": 7, "dailyNewCards": 10,
  "totalCards": 58, "matureCards": 4,
  "nextDueAt": "2026-09-18T03:20:00Z"
}
```

`reviewedToday` = mọi log hôm nay; `reviewsDoneToday` = log hôm nay có `state_before='review'`; `totalCards` = thẻ khác `new`; `nextDueAt` = `min(due_at)` của thẻ khác `new`, không tạm dừng, `due_at > now` (null nếu không có).

**`GET /api/srs/queue?limit=20`** (`limit` 1..50)

```json
{
  "generatedAt": "2026-09-18T01:00:00Z",
  "cards": [
    { "cardId": "…", "state": "review", "queue": "review", "dueAt": "2026-09-18T00:40:00Z",
      "word": { "id": "…", "simplified": "爱", "traditional": "愛", "pinyin": "ai4", "hanViet": "ái",
                "meaningsVi": ["yêu; thích", "tình cảm", "có khuynh hướng (làm gì đó); có xu hướng (xảy ra)"],
                "meaningViStatus": "machine" },
      "intervals": { "again": "PT10M", "hard": "P8D", "good": "P11D", "easy": "P19D" } },
    { "cardId": "…", "state": "new", "queue": "new", "dueAt": "2026-09-18T01:00:00Z",
      "word": { "…": "…" },
      "intervals": { "again": "PT1M", "hard": "PT5M30S", "good": "PT10M", "easy": "P8D" } }
  ],
  "summary": { "…": "như /summary" }
}
```

`queue`: `learning` (nhóm 1) · `review` (nhóm 2) · `new` (nhóm 3) · `ahead` (nhóm 4, học trước). `meaningsVi` tối đa 3.

**`POST /api/srs/cards/{cardId}/reviews`**

```json
// request
{ "clientReviewId": "5b0c7f0e-8a8e-4c2b-9d7a-0f6b0c1d2e3f", "rating": "good", "durationMs": 4200 }
// 200
{
  "reviewId": "…", "duplicate": false,
  "card": { "cardId": "…", "state": "learning", "step": 1, "dueAt": "2026-09-18T01:10:00Z",
            "stability": 2.3065, "difficulty": 2.118103970459016, "reps": 1, "lapses": 0,
            "lastReviewAt": "2026-09-18T01:00:00Z", "isSuspended": false },
  "summary": { "…": "…" }
}
```

Validation: `clientReviewId` uuid khác `Guid.Empty` (bắt buộc); `rating` ∈ `again|hard|good|easy`; `durationMs` 0..600000 hoặc bỏ trống. Lỗi: 400 `VALIDATION` · 404 `NOT_FOUND` · 409 `CLIENT_REVIEW_ID_CONFLICT` ("Mã đánh giá đã dùng cho thẻ khác") · 422 `CARD_SUSPENDED` ("Thẻ đang tạm dừng") · 422 `NEW_CARD_LIMIT_REACHED` ("Đã đủ số từ mới hôm nay", `details.dailyNewCards`). Trùng id cùng thẻ ⇒ 200 `duplicate: true`, `card` = trạng thái hiện tại.

**`POST /api/srs/cards`**

```json
// request
{ "wordIds": ["…", "…"] }                  // 1..100 phần tử, không trùng
// 201
{ "added": 1, "skipped": 1, "cards": [ { "wordId": "…", "cardId": "…", "created": true },
                                        { "wordId": "…", "cardId": "…", "created": false } ] }
```

Lỗi: 400 `VALIDATION` · 422 `UNKNOWN_WORD` (`details.wordIds`: danh sách id không tồn tại — không thêm thẻ nào).

**`PUT /api/srs/cards/{cardId}/suspension`** `{ "suspended": true }` ⇒ 200 `card` (như trên) · 404.

**`GET /api/me/learning-settings`** ⇒ 200

```json
{ "dailyNewCards": 10, "dailyReviewLimit": 200, "desiredRetention": 0.9, "ttsRate": 0.8, "autoPlayAudio": true, "isDefault": true }
```

**`PUT /api/me/learning-settings`** body = 5 trường đầu (đủ cả 5) ⇒ 200 như GET (`isDefault: false`). Luật: `dailyNewCards` 0..50; `dailyReviewLimit` 10..1000; `desiredRetention` 0.80..0.97 (tối đa 2 chữ số thập phân); `ttsRate` 0.5..1.2 (2 chữ số); `autoPlayAudio` bool. Lỗi 400 `VALIDATION` với `details` theo tên trường.

---

## 7. Phân rã feature

> Hợp đồng gốc có F6, F7 mỗi cái một khối. Vì mỗi khối > 3 ngày công và có ranh giới rõ, BA **chẻ thành 5 đơn vị commit** theo thứ tự dưới. Mỗi đơn vị: code → cổng build/test → review → integration (commit local riêng, **không push**) → dừng chờ người dùng OK (trừ khi người dùng đã giao chạy một mạch). Tiền tố commit: `feat(content): F6.1 — …`, `feat(chinese): F6.2 — …`, `feat(web): F6.3 — …`, `feat(chinese): F7.1 — …`, `feat(web): F7.2 — …`.

### Feature F6.1: Học liệu HSK 3.0 cấp 1 (dữ liệu + script)
- Mục tiêu: có `hsk-words.json` (500 mục) và `characters.json` tất định, hợp lệ, đúng giấy phép.
- Phạm vi học liệu (agent `content-implement`, Sonnet): toàn bộ §5.4 — `package.json` (thêm `fflate`, 4 script), `scripts/lib/*`, `fetch-sources.mjs`, `fetch-wiktionary-hanviet.mjs`, `build-hsk.mjs`, mở rộng `validate.mjs`, 5 schema, `sources/sources.lock.json`, `sources/hsk1-overrides.json` (bảng §5.4.4), `sources/han-viet.json` (~300 chữ, biên soạn theo báo cáo ứng viên), `sources/meaning-vi-machine.json`, 2 tệp `data/`, `SOURCES.md`, `LICENSES/`. BE/FE/DB: không.
- Phụ thuộc: F5 (khung `content/`, `syllables.json`, `validate.mjs`).
- Tiêu chí hoàn thành + cách tự test:
  1. `yarn --cwd content install && yarn --cwd content fetch:chinese` tải đủ, hash khớp lock.
  2. `yarn --cwd content build:chinese` thành công, 0 LỖI; chạy lần 2 ⇒ `shasum -a 256 content/chinese/data/vocabulary/hsk-words.json content/chinese/data/characters/characters.json` không đổi.
  3. `yarn --cwd content validate:chinese` exit 0.
  4. Kiểm bằng `node -e`: 500 mục `hsk3Level=1`; `pathOrder` 1..500; 爱 = `ai4`, `hanViet` `ái`, `meaningsVi[0]` chứa "yêu"; 地 có 2 mục `de5`/`di4`; 爸爸 có `variants ["爸"]`; 有些 có `variants ["有一些"]`; 好 `hanVietByPinyin.hao3 = "hảo"`; không nghĩa nào chứa "(tục)" hoặc "(lóng)"; mọi `meaningViStatus = "machine"`.
  5. `git status` không có `.raw/`. `SOURCES.md` có đủ 9 khoá §5.4.1; `LICENSES/` có 5 tệp.
  6. Báo cáo cho người dùng: số nghĩa `cvdict`/`machine`, số WARN phồn thể, danh sách chữ có `note` trong `han-viet.json`.

### Feature F6.2: Schema `content` + nạp học liệu + API từ điển
- Mục tiêu: backend nạp F6.1 idempotent lúc khởi động và phục vụ §6.1.
- Phạm vi BE (`backend-implement`, Sonnet): §5.2.1–§5.2.5. DB (`database-implement`, Sonnet): §5.1.1, migration `F6_Vocabulary` (kiểm SQL sinh ra có `CREATE EXTENSION IF NOT EXISTS pg_trgm`, chỉ mục `varchar_pattern_ops`, GIN trgm, CHECK). Học liệu: không (dùng F6.1). FE: không. Docker: kiểm Dockerfile chinese-backend vẫn đưa `content/chinese/data/**` vào ảnh (không đưa `sources/`, `.raw/`) — ghi "chưa verify".
- Phụ thuộc: F6.1 (phát triển song song được với fixture; nghiệm thu cần dữ liệu thật), F3, F5.
- Tiêu chí hoàn thành + cách tự test:
  1. `dotnet build backend/backend.slnx -v q` 0 error; `AF_TEST_PG=… dotnet test backend/backend.slnx` xanh, báo số test chạy/skip (test §5.2.5 không skip).
  2. DB dev trống và DB dev đang có dữ liệu F3/F5: khởi động ⇒ migrate + log `Nạp characters: +N…`, `Nạp hsk-words: +500…`; restart **2 lần** ⇒ log "không đổi", `SELECT count(*) FROM content.words` = 500, `content.word_characters` không đổi.
  3. `UPDATE content.words SET meaning_vi_status='reviewed', meanings_vi='{thử}' WHERE simplified='爱'`; sửa `importer_version` (hoặc đổi 1 nghĩa trong tệp) rồi restart ⇒ 爱 vẫn `{thử}`, log `khoá1`.
  4. Qua gateway (`http://localhost:5280/chinese/api/dictionary/search?q=...`, token learner): `爱`, `ai4`, `ài`, `ai`, `yêu`, `yeu`, `ái` đều có 爱 ở trang 1; `ai4` ⇒ 爱 vị trí 1; `nǐhǎo` ⇒ 你好 vị trí 1.
  5. `/scalar/v1` hiện 3 endpoint; token không quyền ⇒ 403 JSON.

### Feature F6.3: Màn tra từ
- Mục tiêu: người học tra từ trên điện thoại.
- Phạm vi FE (`frontend-implement`, **model Fable**): §5.3.1. BE/DB/học liệu: không.
- Phụ thuộc: F6.2 (dựng song song theo §6.1 được), F5 (`pinyin.ts`, `useSpeech`), F3 (`RequirePermission`).
- Tiêu chí hoàn thành + cách tự test:
  1. `cd frontend && yarn workspace @af/chinese tsc -b && yarn workspace @af/chinese build && yarn lint:ui && yarn workspace @af/chinese test` sạch.
  2. Trình duyệt 375px (DevTools iPhone SE): `/tu-dien` không cuộn ngang; gõ `yeu` ⇒ URL `?q=yeu`, có 爱 kèm chip "Chưa duyệt"; F5 trang giữ kết quả; mở 爱 ⇒ chi tiết đủ khối; nút quay lại về đúng danh sách + vị trí cuộn; bấm loa nghe "ài" (hoặc tooltip thiếu giọng); bấm chữ 爱 ⇒ `/tu-dien/chu/%E7%88%B1`.
  3. 1366px: bố cục không vỡ; menu có "Từ điển".
  4. `/tu-dien/khong-ton-tai-uuid` ⇒ `/404`.
  5. Học thử ngay: tra 10 từ trong bài pinyin đã học bằng cả pinyin dấu và nghĩa Việt.

### Feature F7.1: FSRS-6 + SRS backend
- Mục tiêu: lập lịch ôn đúng FSRS-6, hàng đợi theo ngày của người học, chấm idempotent.
- Phạm vi BE (`backend-implement`, Sonnet): §5.2.6–§5.2.9 (kể cả sửa `DictionaryService` thêm `srs`). DB (`database-implement`): §5.1.2, migration `F7_Srs`. Package test `Microsoft.Extensions.TimeProvider.Testing` 10.10.0. FE/học liệu: không.
- Phụ thuộc: F6.2, F5 (`study_events`, `IStudyActivityRecorder`).
- Tiêu chí hoàn thành + cách tự test:
  1. Build/test sạch với `AF_TEST_PG`; `FsrsGoldenTests` V1–V7 xanh với dung sai 1e-6; ApiTests §5.2.9 mục 1–11 xanh.
  2. Reviewer đối chiếu từng dòng công thức §5.2.6 với `scheduler.py` tại commit `9446cb0…` (RK5).
  3. Scalar: `GET /api/srs/queue` ⇒ 10 thẻ mới; chấm 1 thẻ `good` ⇒ `dueAt` +10 phút; gửi lại cùng `clientReviewId` ⇒ `duplicate: true`; `SELECT kind, local_date FROM learning.study_events ORDER BY occurred_at DESC LIMIT 1` ⇒ `srs_review`, ngày VN.
  4. Migration chạy được trên DB đang có dữ liệu F6.

### Feature F7.2: Màn ôn tập
- Mục tiêu: ôn 20 thẻ thoải mái trên điện thoại.
- Phạm vi FE (`frontend-implement`, **model Fable**): §5.3.2 (kể cả `hideBottomNav`, `AddToSrsButton`, tab cài đặt). BE/DB/học liệu: không.
- Phụ thuộc: F7.1, F6.3, F4 (`/ho-so`, `useConfirm`, toast).
- Tiêu chí hoàn thành + cách tự test:
  1. Cổng FE như F6.3; `formatInterval.test.ts` và `reviewOutbox.test.ts` xanh.
  2. 375px: ôn trọn 20 thẻ chỉ bằng ngón cái (4 nút trong tầm với, không bị bottom nav/thanh an toàn che); thấy khoảng dự kiến trên nút (thẻ mới: 1 phút · 5,5 phút · 10 phút · 8 ngày); `Space` lật, `1–4` chấm trên máy tính.
  3. DevTools Offline giữa phiên: chấm 3 thẻ ⇒ banner "Đang chờ gửi 3 đánh giá"; bật mạng ⇒ tự gửi, banner tắt; DB có đúng 3 log (không trùng).
  4. Ôn xong 10 thẻ mới ⇒ `/on-tap` báo "Từ mới còn học được 0/10"; đổi "Từ mới mỗi ngày" = 12 ⇒ còn 2.
  5. Chi tiết từ: "Thêm vào ôn tập" ⇒ chip "Chờ học"; menu "Ôn tập" có huy hiệu.
  6. Học thử ngay: ôn 3 ngày liên tiếp (tiêu chí gốc F7), ngày 2 thấy thẻ `good` hôm trước đến hạn.

---

## 8. Thứ tự thực thi & phụ thuộc

```
F5 ─► F6.1 (content) ─┬─► F6.2 (BE+DB) ─┬─► F6.3 (FE) ───────────┐
                      │                 └─► F7.1 (BE+DB) ─► F7.2 (FE) ◄┘
F3 ───────────────────┘                                   F4 ──┘
```

- **Song song được:** F6.1 ‖ F6.2 (F6.2 viết migration/importer/search trên fixture; nghiệm thu mục 2–4 sau khi F6.1 commit). F6.3 dựng giao diện theo §6.1 song song F6.2. F7.1 bắt đầu phần Domain FSRS + vector vàng (thuần, không phụ thuộc DB) ngay khi hợp đồng này được duyệt.
- **Commit theo thứ tự:** F6.1 → F6.2 → F6.3 → F7.1 → F7.2 (mỗi cái một commit local).
- Trong F6.2/F7.1: `database-implement` chốt entity/cấu hình/migration trước ⇒ `backend-implement` viết service/controller/test.
- Orchestrator gọi `frontend-implement` luôn truyền `model: "fable"`.
- Trước khi bắt đầu: đọc §4 và code F3–F5 thật. F4/F5 làm theo thứ tự F5 → F4 (file F4/F5); `AppDialog`/`AppDrawer` có từ F5.

## 9. Tiêu chí hoàn thành + cách kiểm thử

### 9.1 Cổng bắt buộc (mỗi đơn vị, phần liên quan)

```bash
dotnet build backend/backend.slnx -v q                  # 0 error
AF_TEST_PG="Host=localhost;Port=5432;Username=…;Password=…" dotnet test backend/backend.slnx   # xanh; báo số chạy/skip
cd frontend
yarn workspace @af/chinese tsc -b                       # bắt buộc -b
yarn workspace @af/chinese build                        # vì đụng packages/ui
yarn lint:ui                                            # exit 0
yarn workspace @af/chinese test                         # vitest (F5+)
cd ..
yarn --cwd content validate:chinese                     # F6.1 trở đi
```

Kèm: `git status` không có bí mật/`.raw/`; migration chạy trên DB trống **và** DB đang có dữ liệu feature trước; Dockerfile chinese-backend cập nhật nếu đổi csproj/đường dẫn học liệu (ghi "chưa verify").

### 9.2 Nghiệm thu end-to-end (sau F7.2)

Chạy identity + chinese + gateway + app; tài khoản learner múi giờ `Asia/Ho_Chi_Minh`; 375px và 1366px:
1. Tra 7 cách gõ ra 爱 (`爱`, `ai4`, `ài`, `ai`, `yêu`, `yeu`, `ái`).
2. Restart chinese-backend 2 lần ⇒ không nhân đôi (`content.words` = 500).
3. Ôn 20 thẻ (10 mới) ⇒ thẻ mới dừng ở 10; `study_events` có 20 dòng `srs_review` đúng ngày VN.
4. Hôm sau (hoặc đặt giờ máy/`FakeTimeProvider` trong test) ⇒ có thẻ đến hạn, lại có 10 từ mới.

## 10. Rủi ro / quyết định / ràng buộc

### 10.1 Rủi ro mới

| # | Rủi ro | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| RK34 | **Unihan `kVietnamese` không phải bảng Hán Việt thuần** (lẫn Nôm, thiếu chữ giản thể) — hợp đồng gốc định dùng thẳng | Hán Việt sai (吧 = *và*, 冷 = *lạnh*) dạy sai người học | R6-7: tệp `han-viet.json` biên soạn có đối chiếu; `derived` + nhãn; duyệt ở F10 |
| RK35 | `han-viet.json` do agent biên soạn (~300 chữ) có thể chọn sai âm cho chữ đa âm (好 *hảo/hiếu*, 行 *hành/hàng*) | Hán Việt cấp từ lệch | `byPinyin` bắt buộc cho chữ nhiều âm; báo cáo ứng viên; người dùng duyệt các chữ có `note` trước |
| RK36 | CVDICT dịch bằng AI, chứa nghĩa tục/lóng và nghĩa hiếm (幹 *gan4* có nghĩa tục) | Học viên số 0 gặp nghĩa không phù hợp/sai | Bộ lọc §5.4.3 bước 7; tối đa 6 nghĩa; nhãn "Chưa duyệt"; nghĩa Anh đi kèm |
| RK37 | Danh sách chính thức là bản **OCR chưa soát kỹ** (README elkmovie); 62 mục đa âm do BA chọn cách đọc | Sai cách đọc ⇒ học sai thanh | Build đối chiếu với `complete.json` (lệch mục ⇒ LỖI); bảng §5.4.4 có căn cứ thứ tự chữ cái; reviewer soát lại |
| RK38 | **Đề cương thi HSK 2026** (nguồn gọi `newest`) đổi cấp 1 còn 294 mục — khác chuẩn từ vựng GF0025-2021 (500 mục) đã chốt (D1) | Nếu mục tiêu là thi HSK 1 kiểu mới, 206 mục ngoài đề thi làm chậm | Lưu `hskExam2026Level`, đưa từ trong đề thi lên tầng 2 của `path_order`; xem D21 |
| RK39 | `pg_trgm` cần quyền tạo extension (trusted từ PG 13, chủ DB tạo được) — DB test tạo bằng tài khoản khác chủ, hoặc máy chủ PG bị hạn chế | Migration F6 lỗi ⇒ service không khởi động | `AF_TEST_PG` dùng tài khoản có `CREATEDB`; `VERIFY-DOCKER.md` thêm dòng kiểm `\dx pg_trgm` trong `af_chinese`; nếu vẫn lỗi: bỏ 2 chỉ mục GIN + extension (tìm kiếm vẫn đúng, chỉ chậm hơn) |
| RK40 | Tắt fuzz ⇒ thẻ học cùng ngày đến hạn cùng ngày | Ngày ôn dồn cục bộ | Tải nhỏ (10 thẻ/ngày); bật fuzz sau MVP theo §5.2.6 |
| RK41 | `Math.Pow`/`Math.Exp` .NET khác Python ở bit cuối | Test vàng dung sai 1e-6 fail oan | Dung sai 1e-6 cho S/D/R; so `due` tuyệt đối (khoảng đã làm tròn); nếu fail vì ~1e-12 thì báo reviewer, **không** nới quá 1e-4 (mức py-fsrs) |
| RK42 | Wiktionary giới hạn tần suất API (đã gặp "too many requests" khi gọi từng trang) | Script đối chiếu lỗi | Gọi theo lô 50 tiêu đề, nghỉ 1,5 s, User-Agent rõ; script không bắt buộc cho build |
| RK43 | Hàng đợi gửi lại dùng giờ server lúc nhận ⇒ lượt chấm offline lâu bị tính muộn | Khoảng ôn lệch ít | Chấp nhận trong MVP (R7-9); chỉ giữ trong `sessionStorage` |
| RK44 | Tên thành phần F4/F5 trong code thật lệch với §4 (file F4/F5 viết song song) | Agent code sai chỗ | §4 đã khớp file F4/F5 ngày 17/09; lệch với code thật ⇒ theo code, hành vi theo file này |

### 10.2 Quyết định đã dùng mặc định BA (người dùng có thể phản đối)

- **D5** — CVDICT (CC BY-SA 4.0, đã xác minh) cho nghĩa Việt; thiếu ⇒ dịch máy; mọi nghĩa `machine`.
- **D6** — Chấp nhận CC BY-SA 4.0 cho `content/chinese/data/` + `content/chinese/sources/`.
- Hán Việt từ tệp biên soạn (thay vì Unihan trực tiếp) — RK34.
- `path_order` 3 tầng (HSK 2.0–1 → đề thi 2026 cấp 1 → còn lại), trong tầng theo tần suất.
- Chẻ F6 thành F6.1/F6.2/F6.3 và F7 thành F7.1/F7.2.
- `pg_trgm` có; `unaccent` không.
- FSRS: tắt fuzz; học trước 20 phút; giới hạn ôn chỉ áp ở hàng đợi; thời điểm chấm theo giờ server.
- Cách đọc 62 mục đa âm + phồn thể 13 mục theo §5.4.4.
- Tìm theo nghĩa tiếng Anh: chưa làm.

### 10.3 Quyết định mở mới (không chặn)

| Mã | Câu hỏi | Mặc định đang dùng |
|---|---|---|
| **D21** | Mục tiêu là **chuẩn từ vựng HSK 3.0 (500 từ cấp 1)** hay **đề thi HSK 2026 cấp 1 (294 từ)**? | Giữ D1 (500 từ), ưu tiên từ trong đề thi 2026 ở `path_order`. Nếu chọn đề thi 2026: chỉ cần đổi nguồn danh sách trong `build-hsk.mjs` sang `newest-1` + bỏ hằng 500 |
| **D22** | Bật fuzz FSRS? | Tắt trong MVP |

### 10.4 Ràng buộc dự án phải nhắc agent thực thi

- Build sạch: `dotnet build backend/backend.slnx -v q`, `dotnet test backend/backend.slnx` (đặt `AF_TEST_PG`), `yarn workspace @af/chinese tsc -b` (bắt buộc `-b`), `build`, `lint:ui`, `test`; `yarn --cwd content validate:chinese`.
- Commit local sau **mỗi** đơn vị, không push, nhánh `develop`.
- MUI v9 (`slotProps`, shorthand trong `sx`); `AppDialog`/`AppDrawer`; `useTabParam`; không `uuid` (dùng `crypto.randomUUID()`); `@af/*` là workspace source; tsconfig không `baseUrl`; không thêm dependency frontend.
- Npgsql `timestamptz` chỉ nhận `DateTime Kind=Utc` (`TimeProvider.GetUtcNow().UtcDateTime`); cột `date` ⇄ `DateOnly`; "hôm nay" theo `access.users.time_zone`, mốc nửa hở; không cắt ngày bằng UTC.
- DDD 4 lớp (Domain thuần — FSRS không IO); `[RequirePermission]` gán `Policy` trong constructor; quyền đọc từ DB cục bộ; frontend đọc quyền từ `/chinese/api/me`.
- Nạp học liệu idempotent, không ném, không ghi đè dòng đã duyệt; migration tự chạy theo `AutoMigrate`; một migration mỗi feature (`F6_Vocabulary`, `F7_Srs`).
- Học liệu: chỉ nguồn §5.4.1; ghi `content/chinese/SOURCES.md` + `LICENSES/`; nghĩa dịch máy đánh dấu `machine`; `lang="zh-CN"` cho chữ Hán; mobile-first 375px.
- Dockerfile cập nhật cùng feature khi cần, ghi "chưa verify".
