// Kiểm luật dự án cho mobile/ (tương đương `scripts/check-ui-conventions.mjs` của frontend) — hợp đồng mobile §5.3.4.
// Chỉ dùng dart:io. Chạy: `dart run tool/check_conventions.dart` (từ thư mục mobile/). Exit 1 khi có FAIL.
//
// | Luật | Mức | Phát hiện |
// |---|---|---|
// | raw-dialog | FAIL | showDialog( / showModalBottomSheet( / showCupertinoDialog( ở mọi nơi trừ packages/af_ui |
// | uuid-import | FAIL | package:uuid/ |
// | token-in-prefs | FAIL | file vừa chứa SharedPreferences/KeyValueStore/keyValueStoreProvider vừa chứa refreshToken/accessToken |
// | hardcoded-url | FAIL | http:// hoặc https:// trong lib/ ngoài lib/**/config/** và sources.dart/licenses.dart |
// | print-call | FAIL | print( / debugPrint( ngoài af_core/lib/src/log/ |
// | cjk-without-hanzitext | WARN | chuỗi literal có ký tự CJK trong file không import HanziText/LangText |
import 'dart:io';

class Finding {
  Finding(this.rule, this.level, this.path, this.line, this.detail);

  final String rule;
  final String level; // FAIL | WARN
  final String path;
  final int line;
  final String detail;

  @override
  String toString() => '$level $rule  $path:$line  $detail';
}

// Cho phép tham số kiểu: `showDialog<void>(`.
final _rawDialog = RegExp(r'\b(showDialog|showModalBottomSheet|showCupertinoDialog)\s*(<[^>]*>)?\s*\(');
final _uuidImport = RegExp(r'''package:uuid/''');
final _sharedPrefs = RegExp(r'SharedPreferences|KeyValueStore|keyValueStoreProvider');
final _tokenWord = RegExp(r'\b(refreshToken|accessToken)\b');
final _url = RegExp(r'''https?://''');
final _printCall = RegExp(r'\b(print|debugPrint)\s*\(');
final _cjk = RegExp(r'[一-鿿]');
final _hanziImport = RegExp(r'\b(HanziText|LangText)\b');
// Dòng chỉ có bình luận — bỏ qua cho luật URL/CJK/print (bình luận được phép nhắc URL, ví dụ chữ Hán).
final _commentLine = RegExp(r'^\s*//');

void main(List<String> args) {
  final root = Directory.current;
  final files = <File>[
    ..._dartFilesUnder(Directory('${root.path}/packages'), 'lib'),
    ..._dartFilesUnder(Directory('${root.path}/apps'), 'lib'),
  ];
  final findings = <Finding>[];
  for (final f in files) {
    findings.addAll(_check(f, root.path));
  }

  final fails = findings.where((f) => f.level == 'FAIL').toList();
  final warns = findings.where((f) => f.level == 'WARN').toList();
  for (final f in findings) {
    stdout.writeln(f);
  }
  stdout.writeln('check_conventions: ${files.length} file, ${fails.length} FAIL, ${warns.length} WARN');
  if (fails.isNotEmpty) exitCode = 1;
}

/// Mọi file .dart trong `<dir>/<pkg>/<sub>/**` (bỏ build/, .dart_tool/).
Iterable<File> _dartFilesUnder(Directory dir, String sub) sync* {
  if (!dir.existsSync()) return;
  for (final pkg in dir.listSync().whereType<Directory>()) {
    final libDir = Directory('${pkg.path}/$sub');
    if (!libDir.existsSync()) continue;
    for (final entity in libDir.listSync(recursive: true, followLinks: false)) {
      if (entity is! File || !entity.path.endsWith('.dart')) continue;
      final p = entity.path;
      if (p.contains('/build/') || p.contains('/.dart_tool/')) continue;
      yield entity;
    }
  }
}

List<Finding> _check(File file, String root) {
  final rel = file.path.startsWith(root) ? file.path.substring(root.length + 1) : file.path;
  final unix = rel.replaceAll('\\', '/');
  final content = file.readAsStringSync();
  final lines = content.split('\n');
  final out = <Finding>[];

  final isAfUi = unix.startsWith('packages/af_ui/');
  final isLogModule = unix.startsWith('packages/af_core/lib/src/log/');
  final isConfig = RegExp(r'(^|/)lib/(.*/)?config/').hasMatch(unix);
  final isSourcesOrLicenses = unix.endsWith('/sources.dart') || unix.endsWith('/licenses.dart');
  final importsHanzi = _hanziImport.hasMatch(content);
  final hasPrefs = _sharedPrefs.hasMatch(content);

  for (var i = 0; i < lines.length; i++) {
    final line = lines[i];
    final n = i + 1;
    final isComment = _commentLine.hasMatch(line);

    if (!isAfUi && _rawDialog.hasMatch(line) && !isComment) {
      out.add(Finding('raw-dialog', 'FAIL', unix, n, 'dùng showAfDialog/showAfBottomSheet của af_ui'));
    }
    if (_uuidImport.hasMatch(line)) {
      out.add(Finding('uuid-import', 'FAIL', unix, n, 'dùng uuidV4() của af_core'));
    }
    if (hasPrefs && _tokenWord.hasMatch(line)) {
      out.add(Finding('token-in-prefs', 'FAIL', unix, n, 'token chỉ được ở secure storage / bộ nhớ (RM-S1)'));
    }
    if (!isConfig && !isSourcesOrLicenses && !isComment && _url.hasMatch(line)) {
      out.add(
        Finding('hardcoded-url', 'FAIL', unix, n, 'URL chỉ được ở lib/**/config/** hoặc sources.dart/licenses.dart'),
      );
    }
    if (!isLogModule && !isComment && _printCall.hasMatch(line)) {
      out.add(Finding('print-call', 'FAIL', unix, n, 'dùng afLog() của af_core'));
    }
    if (!importsHanzi && !isComment && _cjk.hasMatch(line)) {
      out.add(Finding('cjk-without-hanzitext', 'WARN', unix, n, 'chữ Hán phải hiển thị qua HanziText (locale zh-CN)'));
    }
  }
  return out;
}
