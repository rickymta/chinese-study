import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../application/providers.dart';
import '../../data/models.dart';
import '../widgets/dictionary_error_view.dart';
import '../widgets/source_attribution.dart';
import '../widgets/word_detail_view.dart';
import 'dictionary_search_page.dart';

/// `/tu-dien/:id` (hợp đồng M8): [WordDetailView] (chữ lớn + nghe, pinyin dấu, Hán Việt, nghĩa + chip chưa duyệt, nghĩa
/// Anh, cấp HSK, từ loại, chữ cấu thành chạm ⇒ `/tu-dien/chu/:hanzi`, `AddToSrsButton`) + dòng nguồn. Trang mới luôn mở
/// từ đầu (mỗi lần `push` là một `ScrollView` mới). 404 ⇒ `/404` qua interceptor (giữ nút quay lại).
class WordDetailPage extends ConsumerWidget {
  const WordDetailPage({super.key, required this.id});

  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final value = ref.watch(wordPageProvider(id));
    return Scaffold(
      appBar: AppBar(leading: const DictionaryBackButton(), title: const Text('Chi tiết từ')),
      body: AfPageBody(
        child: AsyncValueView<WordDetail>(
          value: value,
          onRetry: () => ref.invalidate(wordPageProvider(id)),
          error: (e) => DictionaryErrorView(error: e, onRetry: () => ref.invalidate(wordPageProvider(id))),
          data: (word) => Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              WordDetailView(word: word),
              const SizedBox(height: 24),
              const SourceAttribution(),
            ],
          ),
        ),
      ),
    );
  }
}
