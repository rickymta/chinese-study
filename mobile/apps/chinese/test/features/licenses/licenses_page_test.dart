import 'package:af_chinese/features/licenses/data/sources.dart';
import 'package:af_chinese/features/licenses/licenses.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/test_app.dart';

void main() {
  testWidgets('registerAntFarmLicenses: LicenseRegistry có Arphic (từ assets) và MIT hanzi-writer', (tester) async {
    registerAntFarmLicenses();
    final entries = await LicenseRegistry.licenses.toList();
    final arphic = entries.where((e) => e.packages.contains(kArphicLicensePackage)).toList();
    expect(arphic, isNotEmpty, reason: 'assets/hanzi-data/ARPHICPL.TXT phải được khai trong pubspec');
    final arphicText = arphic.first.paragraphs.map((p) => p.text).join('\n');
    expect(arphicText, contains('ARPHIC PUBLIC LICENSE'));
    expect(arphicText, contains('Arphic Technology'));

    final mit = entries.where((e) => e.packages.contains(kHanziWriterLicensePackage)).toList();
    expect(mit, isNotEmpty);
    expect(mit.first.paragraphs.map((p) => p.text).join('\n'), contains('David Chanin'));

    // Giấy phép dữ liệu học liệu (bản sao content/chinese/LICENSES/).
    String textOf(String package) =>
        entries.firstWhere((e) => e.packages.contains(package)).paragraphs.map((p) => p.text).join('\n');
    expect(textOf(kDataLicenses[0].$1), contains('Attribution-ShareAlike 4.0 International'));
    expect(textOf(kDataLicenses[1].$1), contains('Unicode'));
    expect(textOf(kDataLicenses[2].$1), contains('Pleco'));
    expect(textOf(kDataLicenses[4].$1), contains('Open Spaced Repetition'));
  });

  testWidgets('/giay-phep liệt kê nguồn (CC BY-SA, CVDICT, CC-CEDICT, Unihan, Arphic, py-fsrs); nút mở LicensePage', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(360, 740);
    tester.view.devicePixelRatio = 1.0;
    tester.platformDispatcher.textScaleFactorTestValue = 1.3;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
      tester.platformDispatcher.clearTextScaleFactorTestValue();
    });
    await tester.pumpWidget(
      buildTestApp(chineseAdapter: okSystemInfo('chinese-backend'), identityAdapter: okSystemInfo('identity-service')),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.text('Thêm'));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.text('Giấy phép & nguồn'));
    await tester.tap(find.text('Giấy phép & nguồn'));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);

    expect(find.text('Nguồn học liệu'), findsOneWidget);
    expect(find.textContaining('CVDICT'), findsWidgets);
    expect(find.textContaining('CC-CEDICT'), findsWidgets);
    expect(find.textContaining('Unihan'), findsWidgets);
    expect(find.text('Giấy phép: CC BY-SA 4.0'), findsWidgets);
    expect(find.textContaining('Dữ liệu đã được chỉnh sửa'), findsOneWidget);
    // Nghĩa vụ CC BY-SA 4.0 §3(a)(1)(C): mọi nguồn kèm URL giấy phép (chọn-sao-chép được).
    for (final s in [...kDictionarySources, ...kWritingSources, ...kAlgorithmSources]) {
      await tester.scrollUntilVisible(
        find.widgetWithText(SelectableText, s.licenseUrl).first,
        200,
        scrollable: find.byType(Scrollable).first,
      );
      expect(find.widgetWithText(SelectableText, s.licenseUrl), findsWidgets, reason: s.label);
    }
    expect(find.textContaining('https://creativecommons.org/licenses/by-sa/4.0/'), findsWidgets);
    expect(find.textContaining('https://www.unicode.org/license.txt'), findsOneWidget);
    expect(find.textContaining('trong kho dự án AntFarm'), findsOneWidget);

    await tester.scrollUntilVisible(find.text('Giấy phép phần mềm'), 200, scrollable: find.byType(Scrollable).first);
    expect(find.textContaining('Arphic'), findsWidgets);
    expect(find.textContaining('py-fsrs'), findsWidgets);
    expect(find.text('Giấy phép: Arphic Public License'), findsOneWidget);

    await tester.tap(find.text('Giấy phép phần mềm'));
    // LicensePage hiện vòng xoay tới khi nạp xong mọi giấy phép ⇒ không dùng pumpAndSettle. Danh sách gói chỉ dựng
    // phần nhìn thấy nên nội dung Arphic được kiểm ở test LicenseRegistry phía trên.
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));
    expect(find.byType(LicensePage), findsOneWidget);
    expect(find.text('AntFarm · Tiếng Trung'), findsWidgets);
  });
}
