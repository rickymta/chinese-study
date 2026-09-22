// Nguồn học liệu + giấy phép hiển thị ở trang `/giay-phep` (hợp đồng mobile §5.4.1) — bám `content/chinese/SOURCES.md`
// và `SourceAttribution`/`sources.ts` của web. File này được phép chứa URL (luật `hardcoded-url` miễn `sources.dart`).
// KHÔNG import JSON học liệu — chỉ ghi công.

/// Một nguồn/giấy phép cần ghi công.
class SourceEntry {
  const SourceEntry({
    required this.label,
    required this.license,
    required this.url,
    required this.licenseUrl,
    this.note,
  });

  final String label;
  final String license;
  final String url;
  final String licenseUrl;

  /// Nghĩa vụ/ghi chú (vd "đã chỉnh sửa", "tập con 300 chữ").
  final String? note;
}

/// Nguồn nằm trong kho dự án (không có URL công khai riêng).
const kInRepoSource = 'trong kho dự án AntFarm (content/chinese/sources/)';

const _ccBySa = 'https://creativecommons.org/licenses/by-sa/4.0/';
const _mit = 'https://opensource.org/license/mit';

/// Nguồn dữ liệu từ điển/từ vựng (thứ tự ghi công cố định như `SourceAttribution` web).
const kDictionarySources = <SourceEntry>[
  SourceEntry(
    label: 'HSK 3.0 (danh sách chính thức — elkmovie/hsk30, Pleco)',
    license: 'MIT',
    url: 'https://github.com/elkmovie/hsk30',
    licenseUrl: _mit,
    note: 'Mục 一级词汇表 (500 từ cấp 1).',
  ),
  SourceEntry(
    label: 'complete-hsk-vocabulary (drkameleon)',
    license: 'MIT',
    url: 'https://github.com/drkameleon/complete-hsk-vocabulary',
    licenseUrl: _mit,
    note: 'Cách đọc, phồn thể, nghĩa Anh (nghĩa Anh gốc CC-CEDICT).',
  ),
  SourceEntry(
    label: 'CC-CEDICT (MDBG)',
    license: 'CC BY-SA 4.0',
    url: 'https://www.mdbg.net/chinese/dictionary?page=cc-cedict',
    licenseUrl: _ccBySa,
    note: 'Nghĩa Anh và gốc của nghĩa Việt dẫn xuất.',
  ),
  SourceEntry(
    label: 'CVDICT – Phong Phan',
    license: 'CC BY-SA 4.0',
    url: 'https://github.com/ph0ngp/CVDICT',
    licenseUrl: _ccBySa,
    note: 'Nghĩa tiếng Việt — đã chỉnh sửa (lọc, rút gọn).',
  ),
  SourceEntry(
    label: 'Unicode Unihan 18.0.0',
    license: 'Unicode License v3',
    url: 'https://www.unicode.org/charts/unihan.html',
    licenseUrl: 'https://www.unicode.org/license.txt',
    note: 'Số nét, bộ thủ, biến thể phồn thể, cách đọc chữ.',
  ),
  SourceEntry(
    label: 'English Wiktionary contributors (tư liệu đối chiếu Hán Việt)',
    license: 'CC BY-SA 4.0',
    url: 'https://en.wiktionary.org/',
    licenseUrl: _ccBySa,
  ),
  SourceEntry(
    label: 'Hán Việt do AntFarm biên soạn',
    license: 'CC BY-SA 4.0',
    url: kInRepoSource,
    licenseUrl: _ccBySa,
    note: 'Đối chiếu Unihan + Wiktionary, kiểm tay từng chữ.',
  ),
];

/// Dữ liệu nét chữ + thư viện luyện viết (M10.1 đã đóng gói assets; M10.2 port thuật toán chấm nét).
const kWritingSources = <SourceEntry>[
  SourceEntry(
    label: 'hanzi-writer-data 2.0.1 (Make Me a Hanzi, phông Arphic)',
    license: 'Arphic Public License',
    url: 'https://github.com/chanind/hanzi-writer-data',
    licenseUrl: 'https://github.com/chanind/hanzi-writer-data/blob/master/ARPHICPL.TXT',
    note:
        'Tập con 300 chữ HSK 3.0 cấp 1, nội dung từng file giữ nguyên, chỉ đổi tên file theo mã Unicode. '
        'Toàn văn giấy phép trong mục "Giấy phép phần mềm".',
  ),
  SourceEntry(
    label: 'hanzi-writer 3.7.3 — thuật toán chấm nét (port sang Dart, dùng ở luyện viết)',
    license: 'MIT',
    url: 'https://github.com/chanind/hanzi-writer',
    licenseUrl: _mit,
    note: 'Copyright (c) 2014 David Chanin. Toàn văn trong mục "Giấy phép phần mềm".',
  ),
];

/// Thuật toán ôn tập ngắt quãng (backend port sang C# — ghi công vì kết quả lịch ôn hiện trên app).
const kAlgorithmSources = <SourceEntry>[
  SourceEntry(
    label: 'py-fsrs v6.3.2 (Open Spaced Repetition) — FSRS-6',
    license: 'MIT',
    url: 'https://github.com/open-spaced-repetition/py-fsrs',
    licenseUrl: _mit,
    note: 'Copyright (c) 2022 Open Spaced Repetition. Thuật toán được viết lại bằng C# ở máy chủ.',
  ),
];

/// Nội dung tự soạn của dự án (pinyin, bài học, quiz) — không cần ghi công bên ngoài.
const kOriginalContentNote =
    'Bảng pinyin, hướng dẫn phát âm, 5 bài học chủ đề HSK 1 và câu hỏi quiz do AntFarm tự soạn. Dữ liệu từ điển dẫn '
    'xuất từ CC-CEDICT/CVDICT/Wiktionary được phân phối lại theo CC BY-SA 4.0 và ĐÃ ĐƯỢC CHỈNH SỬA (lọc nghĩa, chọn '
    'cách đọc, bổ sung Hán Việt). Nghĩa tiếng Việt chưa duyệt được đánh dấu "Chưa duyệt".';
