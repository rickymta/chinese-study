import 'package:af_chinese/features/lessons/domain/inline_zh.dart';
import 'package:flutter_test/flutter_test.dart';

// Chép đủ ca của `inlineZh.test.ts` web (10 parse + 1 strip).
void main() {
  group('parseInlineZh', () {
    test('chuỗi rỗng ⇒ mảng rỗng', () {
      expect(parseInlineZh(''), isEmpty);
    });

    test('không có token ⇒ một đoạn text nguyên văn', () {
      expect(parseInlineZh('Xin chào các bạn.'), [const InlineText('Xin chào các bạn.')]);
    });

    test('một token giữa câu', () {
      expect(parseInlineZh('Câu chào là [[你好|ni3 hao3]], dùng được với mọi người.'), [
        const InlineText('Câu chào là '),
        const InlineZh(hanzi: '你好', pinyin: 'ni3 hao3'),
        const InlineText(', dùng được với mọi người.'),
      ]);
    });

    test('hai token liền nhau không có text ở giữa', () {
      expect(parseInlineZh('[[我|wo3]][[们|men5]]'), [
        const InlineZh(hanzi: '我', pinyin: 'wo3'),
        const InlineZh(hanzi: '们', pinyin: 'men5'),
      ]);
    });

    test('token ở đầu và cuối chuỗi', () {
      expect(parseInlineZh('[[谢谢|xie4 xie5]] nghĩa là cảm ơn [[吗|ma5]]'), [
        const InlineZh(hanzi: '谢谢', pinyin: 'xie4 xie5'),
        const InlineText(' nghĩa là cảm ơn '),
        const InlineZh(hanzi: '吗', pinyin: 'ma5'),
      ]);
    });

    test('token thiếu dấu | ⇒ giữ nguyên văn bản', () {
      expect(parseInlineZh('Chữ [[你好]] không có pinyin'), [const InlineText('Chữ [[你好]] không có pinyin')]);
    });

    test('`[[` không đóng ⇒ giữ nguyên văn bản', () {
      expect(parseInlineZh('Mở [[你好|ni3 hao3 mà không đóng'), [const InlineText('Mở [[你好|ni3 hao3 mà không đóng')]);
    });

    test('token có phần rỗng ⇒ giữ nguyên văn, token hợp lệ sau đó vẫn tách', () {
      expect(parseInlineZh('[[|ni3]] và [[好|hao3]]'), [
        const InlineText('[[|ni3]] và '),
        const InlineZh(hanzi: '好', pinyin: 'hao3'),
      ]);
    });

    test('cắt khoảng trắng thừa trong token', () {
      expect(parseInlineZh('[[ 再见 | zai4 jian4 ]]'), [const InlineZh(hanzi: '再见', pinyin: 'zai4 jian4')]);
    });

    test('token có xuống dòng bên trong ⇒ không phải token', () {
      const text = '[[你\n好|ni3 hao3]]';
      expect(parseInlineZh(text), [const InlineText(text)]);
    });
  });

  group('stripInlineZh', () {
    test('chỉ giữ chữ Hán của token', () {
      expect(stripInlineZh('Nói [[你好|ni3 hao3]] rồi [[再见|zai4 jian4]].'), 'Nói 你好 rồi 再见.');
    });
  });
}
