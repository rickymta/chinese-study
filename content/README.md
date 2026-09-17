# AntFarm — Học liệu (`content/`)

Dự án yarn riêng (không thuộc workspace `frontend/`), chứa **học liệu** (từ vựng, ngữ pháp, bảng pinyin, bài luyện...)
tách khỏi code. Mỗi service backend nạp học liệu của ngôn ngữ mình lúc khởi động (xem `Content:RootPath` trong
`appsettings.json` của service tương ứng) — **frontend không bao giờ import thẳng file JSON học liệu**.

## Quy ước chung

1. **Bản quyền trước tiên.** Chỉ dùng nguồn có giấy phép cho phép tái sử dụng (MIT, CC BY, CC BY-SA, public
   domain, dữ kiện ngôn ngữ không bảo hộ...). Mọi nguồn phải ghi vào `content/<ngôn-ngữ>/SOURCES.md`: tên,
   URL, giấy phép, ngày lấy, phiên bản/commit, phần đã dùng, nghĩa vụ (vd ghi công, chia sẻ tương tự), file
   bị ảnh hưởng. Không rõ giấy phép ⇒ **không dùng**.
2. **Mỗi ngôn ngữ một thư mục** `content/<ngôn-ngữ>/{schemas/,data/,scripts/,SOURCES.md,LICENSES/}`.
   `schemas/` chứa JSON Schema (2020-12) cho từng `dataset`; `data/` chứa file JSON dữ liệu thật;
   `scripts/validate.mjs` kiểm tra dữ liệu khớp schema + quy tắc nghiệp vụ; `LICENSES/` chứa toàn văn giấy
   phép của nguồn có nghĩa vụ ghi kèm (nếu có).
3. **Định dạng JSON chung:** mỗi file bọc `{ "dataset": "<tên>", "version": "YYYY-MM-DD", "items": [...] }`.
4. **Dữ liệu nào chưa rõ nghĩa tiếng Việt** (dịch từ nguồn tiếng Anh, chưa người duyệt) phải đánh dấu rõ
   (`meaningVi_status: "machine"` hay tương đương) — trừ khi nội dung do chính người soạn học liệu tự viết
   (đánh nguồn `original`), khi đó không cần nhãn `machine`.
5. **Thư mục `.raw/` của mỗi ngôn ngữ** (nếu có) chỉ chứa dữ liệu tải về để **đối chiếu/kiểm tra cục bộ**
   (vd CC-CEDICT), **không commit** (`.gitignore` gốc đã chặn `content/**/.raw/`) và **không phân phối lại**.

## Chạy kiểm tra

```bash
yarn --cwd content install
yarn --cwd content validate:chinese
```

## Ngôn ngữ đã có

- `chinese/` — tiếng Trung. Xem `chinese/SOURCES.md` và `chinese/README` (nếu có) cho chi tiết từng dataset.
