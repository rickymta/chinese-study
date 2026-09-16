# Giấy phép nguồn ngoài — tiếng Trung

Đợt F5 (pinyin) **không phân phối lại** dữ liệu có giấy phép ngoài trong `data/`: CC-CEDICT (CC BY-SA 4.0)
chỉ được dùng cục bộ để kiểm tra/lựa chọn chữ đơn âm (xem `../SOURCES.md`), file gốc nằm ở `.raw/` và
**không commit**. Vì vậy thư mục này hiện chưa cần chứa toàn văn giấy phép nào.

Nếu ở đợt sau (F6 — từ vựng HSK) có dữ liệu **dẫn xuất trực tiếp** từ nguồn CC BY-SA/CC BY (vd danh sách
từ HSK kèm nghĩa lấy từ một nguồn cụ thể), bổ sung vào thư mục này:

- `CC-BY-SA-4.0.txt` — toàn văn https://creativecommons.org/licenses/by-sa/4.0/legalcode
- `Unicode-3.0.txt` — toàn văn giấy phép dữ liệu Unicode (nếu dùng CLDR/Unicode data)

và ghi rõ trong `../SOURCES.md` file nào áp dụng giấy phép nào.
