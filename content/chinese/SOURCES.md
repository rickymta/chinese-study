# Nguồn học liệu — tiếng Trung (`content/chinese/`)

Ghi mọi nguồn dữ liệu đã dùng, kể cả nguồn chỉ dùng để **kiểm tra/lựa chọn** (không sao chép nội dung).
Không rõ giấy phép ⇒ không dùng. CC BY-SA ⇒ ghi rõ nghĩa vụ chia sẻ tương tự.

| Khoá | Tên | URL | Giấy phép | Ngày lấy | Phiên bản/commit | Phần đã dùng | Nghĩa vụ | File bị ảnh hưởng |
|---|---|---|---|---|---|---|---|---|
| `pinyin-table` | Bảng âm tiết Hán ngữ pinyin (dữ kiện ngôn ngữ — quy tắc ghép thanh mẫu/vận mẫu, danh sách âm tiết chuẩn của Hán ngữ phổ thông) | — (kiến thức ngôn ngữ học phổ thông, không thuộc một ấn phẩm cụ thể) | Không bảo hộ (dữ kiện/fact) | 2026-09-17 | — | Danh sách 21 thanh mẫu + vận mẫu chuẩn, quy tắc ghép âm tiết (`spell`), quy tắc đặt dấu thanh, quy tắc y/w và ü sau j/q/x/y | Không | `initials.json`, `finals.json`, `syllables.json` (trường `syllable`/`initial`/`final`), `scripts/validate.mjs` (hàm `spell`) |
| `cc-cedict` | CC-CEDICT (MDBG) | https://www.mdbg.net/chinese/dictionary?page=cc-cedict (tải trực tiếp: https://www.mdbg.net/chinese/export/cedict/cedict_1_0_ts_utf-8_mdbg.txt.gz) | CC BY-SA 4.0 (https://creativecommons.org/licenses/by-sa/4.0/) | 2026-09-17 | dòng `#! date=2026-09-15T23:45:38Z` trong file gốc | **Chỉ dùng để kiểm tra/lựa chọn chữ Hán đơn âm và đối chiếu cách đọc** (script `scripts/lib/cedict.mjs` đọc `.raw/cedict_ts.u8`, không commit, không phân phối lại) — **không sao chép định nghĩa/nghĩa tiếng Anh** vào dữ liệu; mọi `meaningVi` do content-implement tự viết | Ghi công. *Ghi chú: việc CHỌN chữ đơn âm + cách đọc tương ứng là dữ kiện khách quan (đối chiếu với từ điển công khai), không phải nội dung sáng tạo sao chép từ CEDICT. Nếu review cho rằng danh sách (âm tiết, thanh, chữ) trong `syllables.json` là dữ liệu dẫn xuất từ CC-CEDICT thì áp dụng CC BY-SA 4.0 cho riêng `data/pinyin/syllables.json` và bổ sung toàn văn giấy phép vào `LICENSES/CC-BY-SA-4.0.txt` — xem D6 (hợp đồng gốc) đã mặc định chấp nhận hướng xử lý này.* | (không có dữ liệu dẫn xuất trực tiếp ở F5 — chỉ dùng làm công cụ kiểm tra cục bộ, xem ghi chú) |
| `original` | Tự soạn bởi dự án AntFarm (content-implement) | — | Thuộc dự án AntFarm | 2026-09-17 | — | Toàn bộ `noteVi`, `ipa`/`aspirated`/`group` trong `initials.json`, `group`/`standaloneSpelling`/`noteVi` trong `finals.json`, mọi `meaningVi` trong `syllables.json`, toàn bộ nội dung `guide.json` (8 chủ đề) | Không (thuộc dự án) | `initials.json`, `finals.json`, `syllables.json` (`meaningVi`), `guide.json` |

## Ghi chú kiểm tra đơn âm (CC-CEDICT)

`scripts/validate.mjs` đối chiếu mỗi chữ minh hoạ (`tones.<n>.hanzi`) với `content/chinese/.raw/cedict_ts.u8`
(không commit — `.gitignore` gốc đã chặn `content/**/.raw/`). Để tải lại:

```bash
curl -sS -o content/chinese/.raw/cedict.txt.gz \
  https://www.mdbg.net/chinese/export/cedict/cedict_1_0_ts_utf-8_mdbg.txt.gz
gunzip content/chinese/.raw/cedict.txt.gz
mv content/chinese/.raw/cedict.txt content/chinese/.raw/cedict_ts.u8
```

Không có file này, `validate.mjs` vẫn chạy nhưng bỏ qua kiểm tra đơn âm (in `WARN`) — nên tải về trước khi
bàn giao để có kiểm tra đầy đủ (đã dùng để soạn `syllables.json` trong đợt này).

## Quy trình chọn chữ minh hoạ (đã thực hiện cho đợt F5)

1. Dựng danh sách âm tiết chuẩn bằng cách ghép 21 thanh mẫu × 37 vận mẫu theo quy tắc chính tả, đối chiếu
   với tập âm tiết thực tế xuất hiện trong CC-CEDICT (loại các âm tiết chỉ tồn tại trong mục từ phương ngữ/
   tiếng lóng/không có chữ chính thức — vd `biang`, `fiao`, `ging`, `rua`, `sei`, `nun`, `chua`, `bia`,
   `biu`, `tei`, `eng` — không đưa vào `syllables.json`).
2. Với mỗi (âm tiết, thanh), lọc các chữ Hán **đơn ký tự, đơn âm** (đúng 1 cách đọc trong CC-CEDICT, bằng
   đúng âm tiết + thanh đang xét), loại các chữ trong danh sách cấm (một/不/了/的...) và các định nghĩa
   "variant of"/"surname"/"Kangxi radical"/tên địa danh cổ; ưu tiên chữ xuất hiện trong nhiều mục từ ghép
   của CC-CEDICT (đại diện độ thông dụng); bổ sung thủ công một số từ vựng cơ bản cực kỳ thông dụng nhưng
   có tần suất ghép từ thấp trong từ điển (đại từ nhân xưng, danh xưng gia đình: 你/我/他/爸/妈/骂).
3. Tự viết `meaningVi` (1–4 từ tiếng Việt) cho từng chữ đã chọn, dựa trên hiểu biết về nghĩa của chữ đó
   (không dịch máy hàng loạt từ định nghĩa tiếng Anh của CEDICT) — theo R5-4.
4. Chạy `validate.mjs` tới khi sạch lỗi; số liệu độ phủ cuối cùng ghi trong bàn giao của content-implement.
