# NOTICE — hanzi-writer-data (tập con)

## Tiếng Việt

Nguồn: `hanzi-writer-data` phiên bản 2.0.1 (npm, https://github.com/chanind/hanzi-writer-data), dữ liệu
lấy từ dự án Make Me a Hanzi (https://github.com/skishore/makemeahanzi), trích xuất từ phông chữ
Arphic PL KaitiM GB / UKai — Copyright 1999 Arphic Technology Co., Ltd.; Copyright 2016 Shaunak Kishore.
Phân phối lại theo Arphic Public License, toàn văn ở file `ARPHICPL.TXT` cạnh thư mục này.

**Thay đổi của AntFarm:** thư mục này CHỈ chứa tập con các chữ có trong
`content/chinese/data/characters/characters.json` (từ vựng HSK 3.0 cấp 1) và các file đã được ĐỔI TÊN từ
tên gốc là ký tự Hán (vd `爱.json`) sang mã điểm Unicode dạng thập lục phân chữ thường (vd `7231.json`).
Nội dung TỪNG FILE giữ nguyên từng byte, không sửa, không minify. Ngày sinh của thư mục này ghi tay khi
đổi phiên bản nguồn (không tự động sinh, để lần build lại cho ra kết quả giống hệt — không tạo diff).

## English

Source: `hanzi-writer-data` version 2.0.1 (npm, https://github.com/chanind/hanzi-writer-data), data
derived from the Make Me a Hanzi project (https://github.com/skishore/makemeahanzi), extracted from the
Arphic PL KaitiM GB / UKai font — Copyright 1999 Arphic Technology Co., Ltd.; Copyright 2016 Shaunak
Kishore. Redistributed under the Arphic Public License, full text in `ARPHICPL.TXT` next to this folder.

**AntFarm's changes:** this folder ONLY contains the subset of characters present in
`content/chinese/data/characters/characters.json` (HSK 3.0 level 1 vocabulary), and files have been
RENAMED from their original name (the Hanzi character itself, e.g. `爱.json`) to the lowercase hexadecimal
Unicode code point (e.g. `7231.json`). The content of each file is byte-for-byte identical to the
original, not modified or minified. The generation date of this folder is recorded by hand whenever the
source version changes (not auto-generated, so re-running the build produces an identical result — no
diff).
