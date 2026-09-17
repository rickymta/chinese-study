import 'dart:async';

import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../application/providers.dart';
import '../widgets/dictionary_error_view.dart';
import '../widgets/source_attribution.dart';
import '../widgets/word_list_tile.dart';

/// Gợi ý trong ô tìm (cùng chuỗi với web).
const kSearchPlaceholder = 'Chữ Hán, pinyin (ni3hao3 / nǐhǎo / nihao), nghĩa hoặc Hán Việt';

/// Độ trễ gõ ⇒ tìm (web 300 ms).
const kSearchDebounce = Duration(milliseconds: 300);

/// Còn ≤ 5 dòng cuối ⇒ tải trang kế (hợp đồng M8).
const kLoadMoreThreshold = 5;

/// Giới hạn `q` của server (400 `VALIDATION` khi dài hơn).
const kSearchMaxLength = 64;

/// Key ô tìm / chip HSK (test).
const kDictionarySearchFieldKey = ValueKey('dictionary-search');
const kHskChipKey = ValueKey('dictionary-hsk-chip');
const kLoadMoreRetryKey = ValueKey('dictionary-load-more-retry');

/// `/tu-dien?q=` (hợp đồng M8, cần `study.use`): ô tìm dính đầu (`TextInputAction.search`), debounce 300 ms, chip
/// "HSK 1", danh sách cuộn vô hạn (tải trang kế khi còn 5 dòng cuối — thay `Pagination` web), trạng thái tải/lỗi/rỗng/
/// 503. `q` đọc từ URL MỘT lần lúc mở (không ghi lại URL — quy ước app); state danh sách ở
/// [dictionarySearchProvider] (giữ trong nhánh), vị trí cuộn ở [ScrollController] của trang — trang chi tiết `push`
/// lên trên nên quay lại vẫn đúng kết quả + vị trí cuộn.
///
/// LƯU Ý cho M9+: trang chỉ đọc `?q=` MỘT lần lúc dựng; `go('/tu-dien?q=B')` khi trang đã nằm trong nhánh KHÔNG đổi từ
/// khoá — muốn mở với từ khoá mới hãy `push` (tạo trang mới) hoặc đổi cách đọc sang theo dõi `GoRouterState`.
class DictionarySearchPage extends ConsumerStatefulWidget {
  const DictionarySearchPage({super.key});

  @override
  ConsumerState<DictionarySearchPage> createState() => _DictionarySearchPageState();
}

class _DictionarySearchPageState extends ConsumerState<DictionarySearchPage> {
  final _input = TextEditingController();
  final _scroll = ScrollController();
  Timer? _debounce;
  bool _initialized = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    // Đọc `?q=` một lần lúc mở (`GoRouterState.of` phụ thuộc InheritedWidget nên không gọi được trong initState).
    if (_initialized) return;
    _initialized = true;
    // Deep link có thể mang `q` dài hơn giới hạn server (64 ký tự ⇒ 400) — cắt như `maxLength` của ô.
    final urlQ = (GoRouterState.of(context).uri.queryParameters['q'] ?? '')
        .trim()
        .characters
        .take(kSearchMaxLength)
        .toString();
    final current = ref.read(dictionarySearchProvider);
    // Mở lại trang không có `q` ⇒ hiện lại kết quả lần trước (state trong nhánh); có `q` ⇒ tìm theo URL.
    _input.text = urlQ.isNotEmpty ? urlQ : current.q;
    _input.selection = TextSelection.collapsed(offset: _input.text.length);
    // Không được sửa provider trong lúc dựng cây ⇒ hoãn sang sau khung hình đầu.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      final notifier = ref.read(dictionarySearchProvider.notifier);
      if (urlQ.isNotEmpty) {
        unawaited(notifier.search(q: urlQ));
      } else if (!current.loaded && !current.loading) {
        unawaited(notifier.search(q: current.q));
      }
    });
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _input.dispose();
    _scroll.dispose();
    super.dispose();
  }

  void _onChanged(String text) {
    setState(() {}); // nút xoá hiện/ẩn
    _debounce?.cancel();
    _debounce = Timer(kSearchDebounce, () => _applyQuery(text));
  }

  void _applyQuery(String text) {
    _debounce?.cancel();
    if (!mounted) return;
    unawaited(ref.read(dictionarySearchProvider.notifier).search(q: text));
    // Từ khoá mới ⇒ danh sách mới ⇒ về đầu (web `window.scrollTo(0)` khi đổi trang/từ khoá).
    if (_scroll.hasClients && _scroll.offset > 0) _scroll.jumpTo(0);
  }

  void _clear() {
    _input.clear();
    _applyQuery('');
  }

  void _maybeLoadMore(int index, DictionarySearchState st) {
    if (!st.hasMore || st.loadingMore || st.loadMoreError != null) return;
    if (index < st.items.length - kLoadMoreThreshold) return;
    // Đang trong `build` của dòng ⇒ hoãn.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) unawaited(ref.read(dictionarySearchProvider.notifier).loadMore());
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final st = ref.watch(dictionarySearchProvider);
    final notifier = ref.read(dictionarySearchProvider.notifier);

    return Scaffold(
      appBar: AppBar(title: const Text('Từ điển')),
      body: Align(
        alignment: Alignment.topCenter,
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: afMaxContentWidth),
          child: Column(
            children: [
              // ── Ô tìm dính đầu + chip lọc ──
              Material(
                color: theme.colorScheme.surface,
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      TextField(
                        key: kDictionarySearchFieldKey,
                        controller: _input,
                        onChanged: _onChanged,
                        onSubmitted: _applyQuery,
                        textInputAction: TextInputAction.search,
                        keyboardType: TextInputType.text,
                        autocorrect: false,
                        enableSuggestions: false,
                        maxLength: kSearchMaxLength,
                        decoration: InputDecoration(
                          hintText: kSearchPlaceholder,
                          prefixIcon: const Icon(Icons.search),
                          counterText: '',
                          suffixIcon: _input.text.isEmpty
                              ? null
                              : IconButton(tooltip: 'Xoá', onPressed: _clear, icon: const Icon(Icons.close)),
                          isDense: true,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Wrap(
                        spacing: 8,
                        children: [
                          FilterChip(
                            key: kHskChipKey,
                            label: const Text('HSK $kHskFilterLevel'),
                            selected: st.hsk == kHskFilterLevel,
                            onSelected: (_) => unawaited(notifier.toggleHsk()),
                            visualDensity: VisualDensity.compact,
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
              const Divider(height: 1),
              Expanded(child: _buildList(context, st)),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildList(BuildContext context, DictionarySearchState st) {
    final theme = Theme.of(context);
    final notifier = ref.read(dictionarySearchProvider.notifier);
    final String title;
    if (st.isBrowsing) {
      title = st.loaded ? 'Lộ trình HSK 1 (${st.totalCount} từ)' : 'Lộ trình HSK 1';
    } else {
      title = st.loaded ? '${st.totalCount} kết quả cho “${st.q}”' : 'Đang tìm “${st.q}”…';
    }
    final titleWidget = Padding(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
      child: Text(title, style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600)),
    );

    final Widget body;
    if (st.error != null) {
      body = ListView(
        controller: _scroll,
        // Nội dung ngắn hơn màn vẫn kéo-để-làm-mới được (503 ⇒ học liệu nạp xong ⇒ kéo).
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          titleWidget,
          DictionaryErrorView(error: st.error!, onRetry: () => unawaited(notifier.reload())),
          const Padding(padding: EdgeInsets.symmetric(horizontal: 16), child: SourceAttribution()),
        ],
      );
    } else if (!st.loaded && st.items.isEmpty) {
      body = ListView(
        controller: _scroll,
        children: [
          titleWidget,
          for (var i = 0; i < 6; i++)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
              child: Container(
                height: 64,
                decoration: BoxDecoration(
                  color: theme.colorScheme.surfaceContainerHighest,
                  borderRadius: BorderRadius.circular(10),
                ),
              ),
            ),
        ],
      );
    } else if (st.items.isEmpty) {
      body = ListView(
        controller: _scroll,
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          titleWidget,
          _EmptyResult(q: st.q),
          const Padding(padding: EdgeInsets.symmetric(horizontal: 16), child: SourceAttribution()),
        ],
      );
    } else {
      // Đang tải từ khoá mới nhưng còn dữ liệu cũ ⇒ làm mờ thay vì nháy khung xương (`keepPreviousData` web).
      body = AnimatedOpacity(
        opacity: st.loading ? 0.6 : 1,
        duration: const Duration(milliseconds: 150),
        child: ListView.builder(
          controller: _scroll,
          physics: const AlwaysScrollableScrollPhysics(),
          itemCount: st.items.length + 2,
          itemBuilder: (context, index) {
            if (index == 0) return titleWidget;
            final i = index - 1;
            if (i < st.items.length) {
              _maybeLoadMore(i, st);
              return WordListTile(word: st.items[i]);
            }
            return _ListFooter(state: st, onRetry: () => unawaited(notifier.loadMore()));
          },
        ),
      );
    }
    return RefreshIndicator(onRefresh: notifier.reload, child: body);
  }
}

/// Cuối danh sách: vòng xoay khi tải trang kế · lỗi trang kế + Thử lại · hết ⇒ dòng ghi công nguồn.
class _ListFooter extends StatelessWidget {
  const _ListFooter({required this.state, required this.onRetry});

  final DictionarySearchState state;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (state.loadingMore) {
      return const Padding(
        padding: EdgeInsets.all(16),
        child: Center(child: SizedBox.square(dimension: 24, child: CircularProgressIndicator(strokeWidth: 2))),
      );
    }
    final err = state.loadMoreError;
    if (err != null) {
      return Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            Text(
              'Không tải thêm được: ${err.message}',
              style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.error),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 8),
            OutlinedButton.icon(
              key: kLoadMoreRetryKey,
              onPressed: onRetry,
              icon: const Icon(Icons.refresh),
              label: const Text('Thử lại'),
            ),
          ],
        ),
      );
    }
    if (state.hasMore) {
      // Chưa tới ngưỡng tải (danh sách ngắn hơn màn) — hiếm; vẫn cho bấm tải thêm.
      return Padding(
        padding: const EdgeInsets.all(16),
        child: Center(
          child: TextButton(onPressed: onRetry, child: Text('Tải thêm (${state.items.length}/${state.totalCount})')),
        ),
      );
    }
    return const Padding(padding: EdgeInsets.fromLTRB(16, 8, 16, 24), child: SourceAttribution());
  }
}

/// Không tìm thấy — gợi ý các cách gõ (cùng nội dung với web).
class _EmptyResult extends StatelessWidget {
  const _EmptyResult({required this.q});

  final String q;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final muted = theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant);
    final italic = muted?.copyWith(fontStyle: FontStyle.italic);
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 32, 16, 16),
      child: Column(
        children: [
          Text('Không tìm thấy “$q”.', style: theme.textTheme.titleSmall, textAlign: TextAlign.center),
          const SizedBox(height: 8),
          Text.rich(
            TextSpan(
              style: muted,
              children: [
                const TextSpan(text: 'Thử gõ theo cách khác: chữ Hán ('),
                WidgetSpan(
                  alignment: PlaceholderAlignment.middle,
                  child: HanziText('爱', style: muted?.copyWith(fontSize: 18)),
                ),
                const TextSpan(text: '), pinyin có thanh ('),
                TextSpan(text: 'ai4', style: italic),
                const TextSpan(text: ' hoặc '),
                TextSpan(text: 'ài', style: italic),
                const TextSpan(text: ') hay không thanh ('),
                TextSpan(text: 'ai', style: italic),
                const TextSpan(text: '), nghĩa tiếng Việt ('),
                TextSpan(text: 'yêu', style: italic),
                const TextSpan(text: ') hoặc âm Hán Việt ('),
                TextSpan(text: 'ái', style: italic),
                const TextSpan(text: ').'),
              ],
            ),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}

/// Nút quay lại của các trang con từ điển: có gì để pop thì pop, mở thẳng bằng deep link ⇒ về `/tu-dien` (port
/// `useBackTo('/tu-dien')` web).
class DictionaryBackButton extends StatelessWidget {
  const DictionaryBackButton({super.key});

  @override
  Widget build(BuildContext context) {
    return BackButton(
      onPressed: () {
        if (context.canPop()) {
          context.pop();
        } else {
          context.go('/tu-dien');
        }
      },
    );
  }
}
