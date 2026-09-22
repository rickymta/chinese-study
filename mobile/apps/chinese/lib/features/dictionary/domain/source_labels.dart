// Nhãn nguồn dữ liệu cho dòng ghi công ở chi tiết từ (port `sourceInfo`/`meaningViSourceLabel` của `sources.ts` web).
// URL đầy đủ nằm ở `features/licenses/data/sources.dart` (trang `/giay-phep`) — ở đây chỉ nhãn + giấy phép.

class SourceLabel {
  const SourceLabel(this.label, this.license);

  final String label;
  final String license;
}

const _ccBySa = 'CC BY-SA 4.0';
const _mit = 'MIT';

const Map<String, SourceLabel> kSourceLabels = {
  'hsk30-official': SourceLabel('HSK 3.0 (danh sách chính thức)', _mit),
  'complete-hsk-vocabulary': SourceLabel('complete-hsk-vocabulary', _mit),
  'cc-cedict': SourceLabel('CC-CEDICT (MDBG)', _ccBySa),
  'cvdict': SourceLabel('CVDICT – Phong Phan', _ccBySa),
  'unihan': SourceLabel('Unicode Unihan', 'Unicode License v3'),
  'wiktionary': SourceLabel('Wiktionary contributors', _ccBySa),
  'han-viet-curated': SourceLabel('Hán Việt do AntFarm biên soạn', _ccBySa),
  'machine': SourceLabel('Dịch máy (AntFarm)', _ccBySa),
  'hsk1-overrides': SourceLabel('Chọn cách đọc (AntFarm)', _ccBySa),
};

/// Thông tin nguồn theo khoá; khoá lạ (nguồn thêm sau) ⇒ `null` để nơi gọi bỏ qua.
SourceLabel? sourceLabel(String key) => kSourceLabels[key];

/// Nhãn nguồn nghĩa Việt (`meaningViSource`) cho chú thích ở chi tiết từ.
String? meaningViSourceLabel(String? source) => switch (source) {
  'cvdict' => 'Dịch từ CVDICT',
  'machine' => 'Dịch máy',
  'manual' => 'Biên soạn tay',
  _ => null,
};
