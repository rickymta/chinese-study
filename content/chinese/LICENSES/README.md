# Giấy phép nguồn ngoài — tiếng Trung

Đợt F5 (pinyin) **không phân phối lại** dữ liệu có giấy phép ngoài trong `data/`: CC-CEDICT (CC BY-SA 4.0)
chỉ được dùng cục bộ để kiểm tra/lựa chọn chữ đơn âm (xem `../SOURCES.md`), file gốc nằm ở `.raw/` và
**không commit**.

Từ đợt F6.1 (từ vựng HSK 3.0 cấp 1), `data/vocabulary/hsk-words.json` và `data/characters/characters.json`
chứa dữ liệu **dẫn xuất trực tiếp** từ các nguồn CC BY-SA/MIT/Unicode License bên dưới (chi tiết + phần đã
dùng ở `../SOURCES.md` mục "F6.1"):

- `CC-BY-SA-4.0.txt` — toàn văn https://creativecommons.org/licenses/by-sa/4.0/legalcode — áp dụng cho dữ
  liệu dẫn xuất CC-CEDICT/CVDICT/Hán Việt biên soạn (`meaningsVi`, `meaningsEn` gián tiếp, `hanViet`).
- `MIT-elkmovie-hsk30.txt` — toàn văn giấy phép `github.com/elkmovie/hsk30` (danh sách chính thức HSK 3.0
  cấp 1, © 2021 Pleco Inc.).
- `MIT-complete-hsk-vocabulary.txt` — toàn văn giấy phép `github.com/drkameleon/complete-hsk-vocabulary`
  (cách đọc, phồn thể, cấp độ, tần suất, © 2026 Yanis Zafirópulos).
- `Unicode-License-v3.txt` — toàn văn giấy phép dữ liệu Unicode Unihan/CJKRadicals (số nét, bộ thủ, phồn
  thể ứng viên, tư liệu đối chiếu Hán Việt).
- `MIT-py-fsrs.txt` — toàn văn giấy phép `github.com/open-spaced-repetition/py-fsrs` (© 2022 Open Spaced
  Repetition). Dùng ở F7.1 (`FsrsScheduler` chép thuật toán từ mã nguồn này).
- `ARPHICPL.TXT` — toàn văn Arphic Public License, chép nguyên văn từ gói npm `hanzi-writer-data@2.0.1`
  bằng `scripts/build-hanzi-data.mjs` (F8). Áp dụng cho dữ liệu nét chữ (tập con) ở
  `frontend/apps/chinese/public/hanzi-data/` — xem `NOTICE.md` cạnh thư mục đó và mục "F8" trong
  `../SOURCES.md`.
- `hanzi-writer-MIT.txt` — toàn văn giấy phép MIT của `hanzi-writer@3.7.3` (© 2014 David Chanin), thư viện
  hoạt hình/chấm nét dùng ở `frontend/apps/chinese` (F8). Bản sao thứ hai ở
  `frontend/apps/chinese/public/licenses/hanzi-writer.LICENSE.txt` để hiển thị trực tiếp cho người dùng.

Ghi rõ trong `../SOURCES.md` file dữ liệu nào áp dụng giấy phép nào.
