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

---

## F6.1 — Từ vựng HSK 3.0 cấp 1 (500 từ) + chữ Hán (300 chữ)

Nguồn xác minh (tải + đo sha256) ngày **17/09/2026**, ghim tại `sources/sources.lock.json`. Đầu ra:
`data/vocabulary/hsk-words.json` (500 từ, `hsk3Level=1`) và `data/characters/characters.json` (300 chữ).
Thuật toán dựng: `scripts/build-hsk.mjs` (chi tiết + lý do từng bước xem
`docs/agent-workflow/2026-09-17-antfarm-f6-f7-chi-tiet.md` §5.4.3).

| Khoá | Tên | URL | Giấy phép | Ngày lấy | Phiên bản/commit | Phần đã dùng | Nghĩa vụ | File bị ảnh hưởng |
|---|---|---|---|---|---|---|---|---|
| `hsk30-official` | github.com/elkmovie/hsk30 — `wordlist.txt` (OCR bởi Pleco từ PDF chính thức GF0025-2021 của Bộ Giáo dục Trung Quốc) | https://raw.githubusercontent.com/elkmovie/hsk30/7f3d4fdcfcb6e826001df062747c943d1fa8160e/wordlist.txt | MIT, © 2021 Pleco Inc. | 2026-09-17 | commit `7f3d4fdcfcb6e826001df062747c943d1fa8160e` | Mục `一级词汇表` (500 dòng): số thứ tự (`officialIndex`), mục từ (`simplified`), biến thể (`variants`), nhãn từ loại/ghi chú dùng (`usageNote`) | Kèm toàn văn MIT (`LICENSES/MIT-elkmovie-hsk30.txt`) | `data/vocabulary/hsk-words.json` |
| `complete-hsk-vocabulary` | github.com/drkameleon/complete-hsk-vocabulary — `complete.json` | https://raw.githubusercontent.com/drkameleon/complete-hsk-vocabulary/7ac65bf1a6387d35f1ade478906172a19311c7f9/complete.json | MIT, © 2026 Yanis Zafirópulos (nghĩa Anh bên trong lấy từ CC-CEDICT ⇒ CC BY-SA 4.0) | 2026-09-17 | commit `7ac65bf1a6387d35f1ade478906172a19311c7f9` | Cách đọc (`pinyin`), phồn thể (`traditional`, ứng viên lọc qua Unihan), nghĩa Anh (`meaningsEn`), cấp `hsk2Level`/`hskExam2026Level` (từ `level: new-*/old-*/newest-*`), `frequencyRank` (`frequency`), `pos` | Kèm toàn văn MIT (`LICENSES/MIT-complete-hsk-vocabulary.txt`); nghĩa Anh theo giấy phép `cc-cedict` bên dưới | `data/vocabulary/hsk-words.json` |
| `cc-cedict` | CC-CEDICT (MDBG) — lấy gián tiếp qua `complete-hsk-vocabulary` (nghĩa Anh) và `cvdict` (nghĩa Việt dẫn xuất) | https://www.mdbg.net/chinese/dictionary?page=cc-cedict | CC BY-SA 4.0 | 2026-09-17 (qua hai nguồn trên) | theo hai nguồn trên | `meaningsEn` (qua complete-hsk-vocabulary), gốc của nghĩa Việt CVDICT | Ghi công "CC-CEDICT, MDBG"; dữ liệu dẫn xuất phân phối theo CC BY-SA 4.0 (xem ghi chú cuối mục này) | `data/vocabulary/hsk-words.json` |
| `cvdict` | github.com/ph0ngp/CVDICT — `CVDICT.u8` (v1.0.1, 122.591 mục, định dạng CEDICT; dịch bằng mô hình đã fine-tune rồi rà tay, dẫn xuất CC-CEDICT) | https://raw.githubusercontent.com/ph0ngp/CVDICT/c379d909e308343a247e51619f7839a2060a271c/CVDICT.u8 | CC BY-SA 4.0, © Phong Phan | 2026-09-17 | commit `c379d909e308343a247e51619f7839a2060a271c` | Nghĩa Việt (`meaningsVi`) đã lọc bỏ chú thích họ người/tiếng lóng/biến thể/tham chiếu pinyin (xem `build-hsk.mjs` `WHOLE_REJECT_PATTERNS` + `stripPinyinReferenceSegments` — **lệch §5.4.3 bước 7**, xem mục riêng dưới) | Ghi công "CVDICT – Phong Phan", ghi "đã chỉnh sửa" (đã lọc/cắt đoạn + giới hạn 6 nghĩa), cùng giấy phép CC BY-SA 4.0 (`LICENSES/CC-BY-SA-4.0.txt`) | `data/vocabulary/hsk-words.json` |
| `unihan` | Unicode Unihan 18.0.0 (`Unihan_Readings.txt`: `kMandarin`, `kVietnamese`; `Unihan_Variants.txt`: `kTraditionalVariant`; `Unihan_IRGSources.txt`: `kTotalStrokes`, `kRSUnicode`) + `CJKRadicals.txt` 18.0.0 | https://www.unicode.org/Public/18.0.0/ucd/Unihan.zip · https://www.unicode.org/Public/18.0.0/ucd/CJKRadicals.txt | Unicode License v3 | 2026-09-17 | 18.0.0 | Số nét (`strokeCount`), bộ thủ (`radical`/`radicalNumber`), phồn thể ứng viên (`kTraditionalVariant`), cách đọc chữ (`pinyinReadings`, chuyển từ `kMandarin`); `kVietnamese` **chỉ** dùng làm tư liệu đối chiếu cho `han-viet-curated` (không chép thẳng — xem cảnh báo dưới) | Kèm thông báo bản quyền + toàn văn giấy phép (`LICENSES/Unicode-License-v3.txt`) | `data/characters/characters.json` |
| `wiktionary` | English Wiktionary, mẫu `{{vi-readings\|hanviet=...\|reading=...}}` (mục `==Vietnamese==`) — đã chạy `scripts/fetch-wiktionary-hanviet.mjs` | https://en.wiktionary.org/w/api.php | CC BY-SA 4.0 | 2026-09-16 (`sources.lock.json.wiktionary.fetchedAt = "2026-09-16T18:59:27.196Z"`) | API MediaWiki, ảnh chụp ngày lấy | **Tư liệu đối chiếu** Hán Việt bổ sung bên cạnh Unihan `kVietnamese` cho 416 tiêu đề (giản thể + phồn thể của 300 chữ); 340/416 có mục `==Vietnamese==` dùng được, đối chiếu thực tế cho 132/300 chữ (basis `"wiktionary"` trong `han-viet.json`) — phát hiện 1 giá trị sai đã biết (`坏`→"phôi", đúng phải "hoại") và xác nhận nhiều lựa chọn khác (vd `二`→nhị, `你`→nễ, `冷`→lãnh, `姐`→thư, `行`→hành, `年`→niên) | Ghi công "Wiktionary contributors", cùng giấy phép CC BY-SA 4.0 (`LICENSES/CC-BY-SA-4.0.txt`) — **không chép thẳng** vào dữ liệu, chỉ dùng để đối chiếu thủ công | `sources/han-viet.json` |
| `han-viet-curated` | `content/chinese/sources/han-viet.json` — do content-implement biên soạn thủ công, đối chiếu `unihan.kVietnamese` VÀ `wiktionary` (cả hai đều chỉ là tư liệu, **không** chép thẳng vì lẫn âm Nôm/dữ liệu nhiễu — vd Unihan `冷`→"lạnh", `你`→"nể" (lệch dấu), `准`→"chốn" (sai); Wiktionary `坏`→"phôi" (sai, đúng "hoại")) | — (trong repo) | CC BY-SA 4.0 (dẫn xuất từ tư liệu đối chiếu Unihan + Wiktionary) | 2026-09-17 | trong repo, xem lịch sử git | Hán Việt cấp chữ (`readings`, `byPinyin`) cho 300 chữ; mục còn nghi ngờ có `note` giải thích, giữ nguyên `hanVietStatus="derived"` chờ duyệt ở F10 | Ghi "đã chỉnh sửa/biên soạn", cùng giấy phép CC BY-SA 4.0 | `data/characters/characters.json`, `data/vocabulary/hsk-words.json` (trường `hanViet` nối từ cấp chữ) |
| `machine` | `content/chinese/sources/meaning-vi-machine.json` — content-implement tự dịch tiếng Việt từ nghĩa Anh (CC-CEDICT, qua `complete-hsk-vocabulary`) cho các mục CVDICT không phủ hoặc bị lọc rỗng | — (trong repo) | CC BY-SA 4.0 (dẫn xuất) | 2026-09-17 | trong repo | `meaningsVi` cho 10 mục dùng thật (sau review 2026-09-17 sửa bộ lọc, CVDICT phủ thêm 你/您/山 — 3 mục còn lại trong tệp không dùng tới, giữ làm dự phòng): 爸爸, 车上, 好玩儿, 面条儿, 男孩儿, 女孩儿, 小孩儿, 一块儿, 一下儿, 一点儿 | Đánh dấu `meaningViSource="machine"` (toàn bộ `meaningViStatus="machine"` theo D5, không riêng nhóm này) | `data/vocabulary/hsk-words.json` |
| `hsk1-overrides` | `content/chinese/sources/hsk1-overrides.json` — content-implement chọn cách đọc cho 62 mục đa âm + phồn thể cho 13 mục nhiều ứng viên + (thêm sau review 2026-09-17) ghi đè `meaningsVi` cho 6 mục có thứ tự/nội dung nghĩa lệch trọng tâm sư phạm sau khi qua CVDICT | — (trong repo) | Thuộc dự án AntFarm | 2026-09-17 | trong repo | `pinyin` ghi đè cho idx đa âm (vd 地=`de5`/`di4`, 干=`gan1`/`gan4`, 还=`hai2`/`huan2`) và cho 星期日/星期天 (ép chữ thường `xing1 qi1 ri4`/`xing1 qi1 tian1` — không phải danh từ riêng, khác 中国/北京/汉语/中文 vẫn giữ hoa); `traditional` ghi đè cho 13 mục (vd 干→乾, 说→說); `meaningsVi` ghi đè cho 行 (thêm "được; ổn"), 回 (đưa "trở về"/"lần" lên đầu), 太 (đưa "quá" lên đầu), 真的 (bỏ nghĩa toán học lạc chủ đề), 星期 (bỏ "Chủ nhật" — nghĩa chỉ đúng cho 星期天/星期日), 右 (đơn giản hoá "bên phải") — các mục này có `meaningViSource="manual"` | Không | `data/vocabulary/hsk-words.json` |

**Ghi chú bắt buộc (D6, đã chấp nhận CC BY-SA 4.0 cho dữ liệu dẫn xuất):** mọi nội dung trong
`content/chinese/data/` và `content/chinese/sources/` dẫn xuất từ CC-CEDICT/CVDICT/Wiktionary/Unihan được
phân phối lại theo **CC BY-SA 4.0** (Unihan riêng theo Unicode License v3 cho phần dữ kiện không dẫn xuất
ngữ nghĩa — số nét/bộ thủ/phồn thể); dữ liệu đã được **chỉnh sửa** (lọc nghĩa, chọn cách đọc, bổ sung Hán
Việt, giới hạn số lượng nghĩa). Mã nguồn dự án (`scripts/*.mjs`, backend, frontend) **không** chịu giấy phép
này. Toàn văn giấy phép ở `LICENSES/`.

**Đã loại (không dùng — không rõ giấy phép):** `KanjiDictVN` (hvdic.thivien.net); `go-hanviet`
(vietnamtudien.org); `makemeahanzi dictionary.txt` (không cần, đã có Unihan cho bộ thủ);
`krmanik/HSK-3.0-words-list` (không truy cập được qua API ngày kiểm 17/09/2026).

**Cảnh báo chất lượng `kVietnamese` (Unihan) đã gặp khi biên soạn `han-viet-curated`:** trường này lẫn âm
Nôm/thuần Việt hoặc dữ liệu nhiễu ở nhiều chữ — vd `冷`="lạnh" (thuần Việt, không phải Hán Việt — đúng phải
là "lãnh"), `你`="nể" (lệch dấu, đúng "nễ"), `准`="chốn" (sai hẳn, đúng "chuẩn"), `二`="nhì" (âm thứ tự thuần
Việt, đúng "nhị"), `女`="nữa" (Nôm, đúng "nữ"), `行`="hàng" (đúng cho nghĩa "cửa hàng" *hang2*, HSK1 chỉ dùng
nghĩa "được/đi" *xing2* → "hành"), và một số mục bị OCR/gộp nhiễu (`百`="bá bách trăm", `看`="khan khán khản
khăn khen khớn", `校`="chò giâu hiệu", `第`="đậy đệ", `里`="lịa"). **Wiktionary cũng không đáng tin tuyệt
đối** — vd `坏`="phôi" là sai (đúng "hoại", khớp Unihan + từ ghép "hư hoại"/"phá hoại"). **Không dùng trực
tiếp bất kỳ nguồn nào** — mọi giá trị trong `han-viet.json` đã được content-implement kiểm tra lại thủ công
(cross-check cả hai tư liệu + từ ghép Hán Việt phổ biến khi có thể) trước khi dùng; mục còn nghi ngờ có
`note` đề nghị F10 duyệt lại (~48 chữ). Sau review 2026-09-17, chỉ còn **1 chữ** (`吗` — trợ từ nghi vấn
cuối câu, không tìm được âm Hán Việt nào đáng tin ở cả hai tư liệu) để `readings: []`/rỗng thật sự; các chữ
trợ từ/ngữ pháp khác (`吧`→ba, `呢`→ni/nỉ, `么`→yêu, `着`→trước, `怎`→chẩm, `您`→nâm — 2 chữ sau cùng là bổ
sung mới của review này) **có** âm Hán Việt ghi trong `readings` nhưng bị `byPinyin` đặt rỗng riêng cho âm
tiết dùng làm trợ từ trong HSK1 (`ba5`, `ne5`, `me5`, `zhe5`) vì không mang nghĩa từ vựng độc lập trong ngữ
cảnh đó; `地` đọc `de5` và `儿` đọc `r5` (儿化) cũng theo quy tắc này.

---

**Lệch hợp đồng F6/F7 §5.4.3 bước 7 [chấp nhận theo review Opus 2026-09-17]:** bản gốc coi bất kỳ nghĩa
CVDICT nào chứa tham chiếu pinyin dạng `[xx1]` là rác và loại bỏ **cả câu**. Thực tế CVDICT hay chèn chú
thích tham chiếu pinyin/lượng từ/cách đọc vùng miền NGAY TRONG một câu có nghĩa chính hữu ích đứng trước
(vd `"(nối hai danh từ) và; cùng với; với (cách đọc ở Đài Loan [han4])"`, `"rau (LT:棵[ke1])"`), khiến áp
dụng đúng văn bản hợp đồng làm mất nghĩa chính của nhiều từ rất cơ bản — phát hiện khi review: 和 mất "và;
cùng với", 菜 mất "rau", 这/那/有/国/路/就/来/多/听/考/年/回/太... mất nghĩa chính dùng nhiều nhất. Từ
2026-09-17, `build-hsk.mjs` (`stripPinyinReferenceSegments`) chỉ **cắt đoạn** chứa tham chiếu (ngoặc bao
quanh, hoặc chuỗi "[xx1]"/"[xx1 yy2]" trần đứng ngay sau chữ Hán/`｜`) rồi giữ phần còn lại; chỉ loại cả
câu khi phần còn lại rỗng hoặc chính nó khớp một mẫu loại khác (họ người/biến thể/tục/lóng/dâm/lượng từ —
xem `WHOLE_REJECT_PATTERNS`). Kết quả: nghĩa nguồn CVDICT tăng từ 487/500 lên 484/500 (giảm nhẹ vì đồng
thời sửa thêm 2 lỗi khác — mẫu `biến thể\b` từng không khớp sau nguyên âm có dấu, và câu "Lượng từ: ..."
viết đầy đủ chữ thay vì viết tắt "LT:" từng lọt lưới — cả hai làm CHẶT hơn, không phải nới lỏng), 10/500
cần dịch máy (`sources/meaning-vi-machine.json`), 6/500 được `hsk1-overrides.json` ghi đè tay thêm (xem
hàng `hsk1-overrides` ở trên). `validate.mjs` có 2 từ neo (和/菜) + lưới an toàn cấm nghĩa chứa "tục"/
"lóng"/"dâm" (trừ các cụm tiếng Việt trung tính chứa các chuỗi con này, vd "tiếp tục", "phong tục", "lóng
lánh" — xem `BANNED_MEANING_TERMS` trong `validate.mjs`) để chặn tái diễn.

**Mã nguồn tham chiếu:** `py-fsrs` — https://github.com/open-spaced-repetition/py-fsrs — MIT
License, Copyright (c) 2022 Open Spaced Repetition — commit ghim
`9446cb06605c597a063aeee49f7d188d42e34dc2` (tag `v6.3.2`, tệp `fsrs/scheduler.py`,
`tests/test_basic.py`). F7.1 chép NGUYÊN thuật toán FSRS-6 (21 trọng số mặc định, công thức
S/D/khoảng ôn, máy trạng thái Learning/Review/Relearning) sang
`AntFarm.Chinese.Domain/Srs/FsrsScheduler.cs` (C#, không copy mã nguồn Python) và đối chiếu vector
vàng bằng cách chạy trực tiếp thư viện Python đúng commit này (`AntFarm.Chinese.UnitTests/Srs/FsrsGoldenTests.cs`).
Giấy phép đầy đủ: `LICENSES/MIT-py-fsrs.txt`.

---

## F9 — Bài học chủ đề HSK 1 + quiz (5 bài seed)

Toàn bộ nội dung 5 bài học (`data/lessons/01-chao-hoi.json` … `05-thoi-gian.json`: hội thoại, ngữ pháp,
mẹo, câu quiz và lời giải) do content-implement **tự soạn** (`sources: ["original"]`), dùng khoá
`original` đã khai ở bảng đầu file này. Không sao chép từ giáo trình nào. Mọi từ trong trường `words` của
từng bài được đối chiếu (khoá `simplified` + `pinyin`) với `data/vocabulary/hsk-words.json` (F6.1); mọi chữ
Hán xuất hiện trong hội thoại/ví dụ ngữ pháp/audio quiz đều thuộc từ đã "dạy" ở bài này hoặc các bài có
`orderIndex` nhỏ hơn (kiểm bằng `scripts/validate.mjs`, mục "MỞ RỘNG F9").

| Khoá | Tên | URL | Giấy phép | Ngày lấy | Phiên bản | Phần đã dùng | Nghĩa vụ | File |
|---|---|---|---|---|---|---|---|---|
| `original` | Nội dung tự soạn AntFarm (content-implement) | — | Thuộc dự án AntFarm | 2026-09-17 | — | 5 bài học: tiêu đề, mục tiêu, khối văn bản/hội thoại/ngữ pháp/mẹo, 35 câu quiz (7 câu/bài) và lời giải | Không (thuộc dự án) | `data/lessons/01-chao-hoi.json`, `02-ban-than.json`, `03-so-dem.json`, `04-gia-dinh.json`, `05-thoi-gian.json` |

File JSON học liệu **không có trường `reviewStatus`** — đây là cột của bảng `content.lessons` do
`LessonImporter` (backend F9) gán khi nạp: bài mới (chưa có `slug` trong DB) luôn được gán
`review_status='machine'` (R-LS14), học viên sẽ thấy nhãn "Nội dung chưa được duyệt" cho tới khi F10 duyệt
tay. Thứ tự bài: chào hỏi (1) → giới thiệu bản thân (2) → số đếm (3) → gia đình (4) → ngày giờ (5) — lý do
sư phạm xem `docs/agent-workflow/2026-09-17-antfarm-f8-f11-chi-tiet.md` §1.3.

**Từ vựng dùng ngoài phạm vi khai trong `words` của bài (chấp nhận được — chỉ là ghép ký tự đã dạy):** một
số cụm trong hội thoại/ngữ pháp là tổ hợp của các CHỮ đã học riêng lẻ chứ không phải từ mới cần thêm vào
SRS, ví dụ `你们`/`他们`/`她们` (bài 1, ghép 你/他/她 + 们 qua từ đã khai 我们, dùng trong khối ngữ pháp "们 —
số nhiều của đại từ"), `不是`/`哪国人` (bài 2, ghép từ các từ đã khai) — đúng theo quy tắc "phủ chữ" ở mức
từng CHỮ Hán (không phải từng TỪ) của hợp đồng F8–F11 §5.4.3. `scripts/validate.mjs` còn kiểm thêm: một từ
(cặp `simplified`+`pinyin`) chỉ được khai trong `words` của bài ĐẦU TIÊN dùng nó (FAIL nếu khai lại ở bài
sau), và cách đọc từng chữ trong dòng hội thoại/ví dụ ngữ pháp phải khớp cách đọc suy ra từ
`hsk-words.json` (WARN nếu lệch — bắt các trường hợp lỡ ghi biến điệu vào pinyin lưu trữ thay vì thanh gốc).

**Glossary (từ bổ sung, không vào SRS)** cũng do content-implement tự soạn: hai tên riêng dùng làm nhân vật
hội thoại bài 2 (`阮兰` Ruǎn Lán — phiên âm Hán Việt của "Nguyễn Lan"; `王明` Wáng Míng — tên nhân vật người
Trung Quốc), trợ từ toán học `加` (cộng) ở bài 3 cho ví dụ cộng trừ đơn giản, và `零` (số 0) ở bài 5 để đọc
năm theo từng chữ số (`二零二六年` = năm 2026).

**Chưa dùng chữ ngoài HSK 1:** tất cả 71 lượt khai `words` (mỗi từ chỉ khai đúng MỘT LẦN, ở bài đầu tiên
dùng nó — không có từ nào bị khai lặp ở nhiều bài, xem kiểm tra ở trên) đều nằm trong
`data/vocabulary/hsk-words.json` (`hsk3Level=1`) và mọi chữ Hán rời đều có trong
`data/characters/characters.json` — không có ngoại lệ cần ghi chú.
