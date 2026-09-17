# Giấy phép đóng gói trong app mobile (`assets/licenses/`)

Mọi file ở đây là **bản sao nguyên văn** (từng byte) của `content/chinese/LICENSES/` trong kho AntFarm, chép tay
17/09/2026 (M4) để `LicenseRegistry` của app hiển thị trong "Giấy phép phần mềm" (`lib/features/licenses/licenses.dart`).
Đổi bản gốc thì chép lại và ghi ngày ở đây.

| File | Gốc | Áp dụng cho |
|---|---|---|
| `hanzi-writer.LICENSE.txt` | `content/chinese/LICENSES/hanzi-writer-MIT.txt` (= `frontend/node_modules/hanzi-writer/LICENSE`) | thuật toán chấm nét port sang Dart (M10.2) |
| `CC-BY-SA-4.0.txt` | `content/chinese/LICENSES/CC-BY-SA-4.0.txt` | dữ liệu từ điển dẫn xuất CC-CEDICT / CVDICT / Wiktionary / Hán Việt AntFarm |
| `Unicode-License-v3.txt` | `content/chinese/LICENSES/Unicode-License-v3.txt` | Unihan (số nét, bộ thủ, phồn thể, cách đọc) |
| `MIT-elkmovie-hsk30.txt` | `content/chinese/LICENSES/MIT-elkmovie-hsk30.txt` | danh sách HSK 3.0 chính thức |
| `MIT-complete-hsk-vocabulary.txt` | `content/chinese/LICENSES/MIT-complete-hsk-vocabulary.txt` | cách đọc/phồn thể/nghĩa Anh |
| `MIT-py-fsrs.txt` | `content/chinese/LICENSES/MIT-py-fsrs.txt` | thuật toán FSRS-6 (máy chủ) |

Arphic Public License của dữ liệu nét chữ nằm ở `assets/hanzi-data/ARPHICPL.TXT` (mirror từ bản web, M10.1).
