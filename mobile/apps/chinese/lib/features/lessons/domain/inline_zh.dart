/// Cú pháp chữ Hán nội dòng trong học liệu bài học (hợp đồng §5.4.3): `[[<chữ Hán>|<pinyin số thanh>]]` xuất hiện
/// trong `text.paragraphs`, `grammar.explanation/pattern`, `tip.text`, `explanation` của câu hỏi. Backend đã validate
/// khi nạp; parser này VẪN phải khoan dung: token hỏng (thiếu `|`, `[[` không đóng, phần rỗng) giữ nguyên văn bản
/// để không mất chữ khi admin (F10) gõ dở. Port 1-1 từ `features/lessons/lib/inlineZh.ts` web.
library;

import 'package:flutter/foundation.dart';

/// Một đoạn sau khi tách: văn bản thường hoặc token chữ Hán.
@immutable
sealed class InlineZhSegment {
  const InlineZhSegment();
}

class InlineText extends InlineZhSegment {
  const InlineText(this.text);

  final String text;

  @override
  bool operator ==(Object other) => other is InlineText && other.text == text;

  @override
  int get hashCode => text.hashCode;

  @override
  String toString() => 'InlineText($text)';
}

class InlineZh extends InlineZhSegment {
  const InlineZh({required this.hanzi, required this.pinyin});

  final String hanzi;

  /// Pinyin dạng SỐ THANH như trong học liệu — hiển thị dạng dấu qua `numberedToMarked`.
  final String pinyin;

  @override
  bool operator ==(Object other) => other is InlineZh && other.hanzi == hanzi && other.pinyin == pinyin;

  @override
  int get hashCode => Object.hash(hanzi, pinyin);

  @override
  String toString() => 'InlineZh($hanzi|$pinyin)';
}

// `[[` … `|` … `]]` — hai phần không chứa `[`, `]`, `|`, không xuống dòng (cùng biểu thức với web).
final RegExp _tokenRe = RegExp(r'\[\[([^\[\]|\n]+)\|([^\[\]|\n]+)\]\]');

List<InlineZhSegment> parseInlineZh(String text) {
  final out = <InlineZhSegment>[];
  if (text.isEmpty) return out;
  var last = 0;
  for (final m in _tokenRe.allMatches(text)) {
    final hanzi = m.group(1)!.trim();
    final pinyin = m.group(2)!.trim();
    // Phần rỗng sau khi bỏ khoảng trắng ⇒ coi là token hỏng, giữ nguyên văn (không tách).
    if (hanzi.isEmpty || pinyin.isEmpty) continue;
    if (m.start > last) out.add(InlineText(text.substring(last, m.start)));
    out.add(InlineZh(hanzi: hanzi, pinyin: pinyin));
    last = m.end;
  }
  if (last < text.length) out.add(InlineText(text.substring(last)));
  return out;
}

/// Bỏ mã hoá token, chỉ giữ chữ Hán (`[[你好|ni3 hao3]]` ⇒ `你好`) — dùng cho nhãn trợ năng/tiêu đề thuần chữ.
String stripInlineZh(String text) => parseInlineZh(text)
    .map(
      (s) => switch (s) {
        InlineZh() => s.hanzi,
        InlineText() => s.text,
      },
    )
    .join();
