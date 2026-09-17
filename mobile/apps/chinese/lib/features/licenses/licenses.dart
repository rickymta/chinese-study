import 'package:af_core/af_core.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';

/// Tên gói hiện trong `showLicensePage` cho dữ liệu nét chữ (Arphic Public License).
const kArphicLicensePackage = 'hanzi-writer-data (Make Me a Hanzi, Arphic)';

/// Tên gói cho thuật toán chấm nét port từ hanzi-writer (MIT).
const kHanziWriterLicensePackage = 'hanzi-writer — thuật toán chấm nét';

/// Đường dẫn assets giấy phép (khai trong `pubspec.yaml`).
const kArphicLicenseAsset = 'assets/hanzi-data/ARPHICPL.TXT';
const kHanziWriterLicenseAsset = 'assets/licenses/hanzi-writer.LICENSE.txt';

/// Giấy phép dữ liệu học liệu — bản sao nguyên văn của `content/chinese/LICENSES/` (xem `assets/licenses/NOTICE.md`):
/// (tên gói hiện trong LicensePage, asset).
const kDataLicenses = <(String, String)>[
  (
    'Dữ liệu từ điển dẫn xuất CC-CEDICT / CVDICT / Wiktionary / Hán Việt AntFarm (CC BY-SA 4.0)',
    'assets/licenses/CC-BY-SA-4.0.txt',
  ),
  ('Unicode Unihan (Unicode License v3)', 'assets/licenses/Unicode-License-v3.txt'),
  ('HSK 3.0 — elkmovie/hsk30 (MIT)', 'assets/licenses/MIT-elkmovie-hsk30.txt'),
  ('complete-hsk-vocabulary (MIT)', 'assets/licenses/MIT-complete-hsk-vocabulary.txt'),
  ('py-fsrs — thuật toán FSRS-6 (MIT)', 'assets/licenses/MIT-py-fsrs.txt'),
];

/// Đăng ký giấy phép của dữ liệu/thư viện ngoài pub vào [LicenseRegistry] để `showLicensePage` liệt kê (hợp đồng
/// mobile §5.4.1): Arphic (nét chữ), MIT hanzi-writer, CC BY-SA 4.0 / Unicode / MIT của dữ liệu học liệu. Gọi một
/// lần ở `main.dart` trước `runApp`. Asset thiếu ⇒ log, bỏ qua (không làm hỏng trang).
void registerAntFarmLicenses({AssetBundle? bundle}) {
  final b = bundle ?? rootBundle;
  LicenseRegistry.addLicense(() async* {
    final arphic = await _load(b, kArphicLicenseAsset);
    if (arphic != null) yield LicenseEntryWithLineBreaks(const [kArphicLicensePackage], arphic);
    final mit = await _load(b, kHanziWriterLicenseAsset);
    if (mit != null) yield LicenseEntryWithLineBreaks(const [kHanziWriterLicensePackage], mit);
    for (final (package, asset) in kDataLicenses) {
      final text = await _load(b, asset);
      if (text != null) yield LicenseEntryWithLineBreaks([package], text);
    }
  });
}

Future<String?> _load(AssetBundle bundle, String asset) async {
  try {
    return await bundle.loadString(asset);
  } on Object catch (e) {
    afLog('Không đọc được giấy phép $asset (${e.runtimeType}) — bỏ qua');
    return null;
  }
}
