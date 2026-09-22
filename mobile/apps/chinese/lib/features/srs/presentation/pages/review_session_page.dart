import 'dart:async';

import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../api/clients.dart';
import '../../../../core/speech/chinese_speech.dart';
import '../../../../router/routes.dart';
import '../../../dictionary/presentation/widgets/word_detail_view.dart';
import '../../application/outbox_controller.dart';
import '../../application/providers.dart';
import '../../data/models.dart';
import '../../data/srs_api.dart';
import '../../domain/review_outbox.dart';
import '../../domain/session_deck.dart';
import '../widgets/flashcard.dart';
import '../widgets/pending_reviews_banner.dart';
import '../widgets/rating_bar.dart';
import '../widgets/session_header.dart';
import '../widgets/session_summary.dart';

/// Khoá nút chấm sau mỗi lượt để chống bấm đúp (hợp đồng: 300 ms).
const kRateLockMs = 300;

/// Key nút "Hiện đáp án" (test).
const kFlipButtonKey = ValueKey('flip-card');

enum _Phase { loading, ready, error, finished }

/// `/on-tap/phien` (hợp đồng M6, port `ReviewSessionPage.tsx`) — toàn màn hình trên root navigator (ẩn bottom nav),
/// bọc `AuthGate` ở router. Bộ bài: `GET queue?limit=20`; còn ≤ 5 thẻ chưa chấm ⇒ tải thêm theo `mergeIncoming`
/// (`skipNew` khi có lượt chấm chưa chắc tới server trong lúc gọi); hết ⇒ `SessionSummary`. Chấm là LẠC QUAN: sang
/// thẻ kế ngay, `clientReviewId = uuidV4()` sinh một lần, đưa vào outbox bền (gửi nền, giữ khi mất mạng/tắt app).
/// Tự đọc khi thẻ hiện nếu `autoPlayAudio` (khi chấm — trong thao tác chạm — đọc luôn thẻ kế: iOS chỉ cho phát ở đây).
/// Rời phiên khi còn lượt gửi hỏng ⇒ `PopScope` + `showAfConfirm` (RM-L7).
class ReviewSessionPage extends ConsumerStatefulWidget {
  const ReviewSessionPage({super.key});

  @override
  ConsumerState<ReviewSessionPage> createState() => _ReviewSessionPageState();
}

class _ReviewSessionPageState extends ConsumerState<ReviewSessionPage> {
  _Phase _phase = _Phase.loading;
  ApiError? _loadError;
  List<SrsQueueCard> _deck = const [];
  int _index = 0;
  bool _flipped = false;
  final List<SrsRating> _ratings = [];
  bool _locked = false;
  bool _exhausted = false;
  bool _loadingMore = false;
  ApiError? _moreError;

  final Set<String> _ratedIds = {};

  /// Đếm lượt chấm đã nộp vào outbox — phát hiện lượt chấm phát sinh TRONG LÚC đang gọi `/srs/queue`.
  int _submitSeq = 0;

  /// Thời gian thẻ hiện — `Stopwatch` (đồng hồ đơn điệu, không lệch khi giờ máy đổi).
  final Stopwatch _shown = Stopwatch();
  DateTime _sessionStart = DateTime.now();
  DateTime? _sessionEnd;
  String? _spokenCardId;
  Timer? _lockTimer;
  bool _detailOpen = false;
  SpeechController? _speech;

  SrsQueueCard? get _current => _index < _deck.length ? _deck[_index] : null;
  int get _remaining => _deck.length - _index;

  @override
  void initState() {
    super.initState();
    // Mở phiên ⇒ gửi ngay outbox còn dở (RM-L1) rồi tải hàng đợi.
    ref.read(reviewOutboxProvider.notifier).flushNow();
    unawaited(_loadInitial());
  }

  @override
  void dispose() {
    _lockTimer?.cancel();
    // Rời màn khi đang đọc ⇒ huỷ (hợp đồng §5.3.7); hoãn một tick vì Riverpod cấm đổi state trong dispose.
    final speech = _speech;
    if (speech != null) scheduleMicrotask(speech.cancel);
    super.dispose();
  }

  Set<String> get _pendingCardIds => ref.read(reviewOutboxProvider).pendingCardIds;

  // ── Tải hàng đợi lần đầu / "Ôn tiếp" ──
  Future<void> _loadInitial() async {
    setState(() {
      _phase = _Phase.loading;
      _loadError = null;
      _deck = const [];
      _index = 0;
      _flipped = false;
      _ratings.clear();
      _exhausted = false;
      _moreError = null;
      _ratedIds.clear();
      _spokenCardId = null;
      _sessionStart = DateTime.now();
      _sessionEnd = null;
    });
    try {
      final res = await getSrsQueue(ref.read(chineseDioProvider), limit: kQueueLimitDefault);
      if (!mounted) return;
      _applySummary(res.summary);
      // Thẻ đang chờ gửi (mở lại phiên với outbox dở) ⇒ bỏ để không chấm hai lần.
      final cards = mergeIncoming(const [], const {}, _pendingCardIds, res.cards);
      setState(() {
        _deck = cards;
        if (cards.isEmpty) {
          _sessionEnd = DateTime.now();
          _phase = _Phase.finished;
        } else {
          _phase = _Phase.ready;
        }
      });
      if (cards.isNotEmpty) _onCardShown(userGesture: false);
      _afterDeckChange();
    } on Object catch (err) {
      if (!mounted) return;
      setState(() {
        _loadError = ApiError.from(err);
        _phase = _Phase.error;
      });
    }
  }

  void _applySummary(SrsSummary summary) {
    final userId = ref.read(reviewOutboxProvider).userId;
    if (userId != null) ref.read(srsSummaryByUserProvider(userId).notifier).apply(summary);
  }

  // ── Tải thêm khi còn ≤ 5 thẻ chưa chấm ──
  Future<void> _loadMore() async {
    if (_loadingMore) return;
    setState(() {
      _loadingMore = true;
      _moreError = null;
    });
    try {
      final seqAtStart = _submitSeq;
      final pendingAtStart = _pendingCardIds.isNotEmpty;
      final res = await getSrsQueue(ref.read(chineseDioProvider), limit: kQueueLimitDefault);
      if (!mounted) return;
      _applySummary(res.summary);
      // Có lượt chấm chưa chắc đã tới server trong suốt lời gọi ⇒ "từ mới hôm nay" của server đã cũ ⇒ KHÔNG nhận
      // thẻ mới, kẻo cấp dư rồi lượt chấm bị 422 NEW_CARD_LIMIT_REACHED.
      final pending = _pendingCardIds;
      final pendingNow = pending.isNotEmpty;
      final newUnsafe = pendingAtStart || pendingNow || _submitSeq != seqAtStart;
      // Chỉ so với phần bộ bài CHƯA chấm: bản quay lại của thẻ learning đã nằm sẵn thì không nối thêm.
      final unrated = _deck.sublist(_index);
      final candidates = mergeIncoming(unrated, _ratedIds, pending, res.cards);
      final skippedNew = newUnsafe && candidates.any((c) => c.state == SrsCardState.fresh);
      final added = newUnsafe ? candidates.where((c) => c.state != SrsCardState.fresh).toList() : candidates;
      var reload = false;
      setState(() {
        final wasEmpty = _current == null;
        if (added.isNotEmpty) {
          _deck = [..._deck, ...added];
          if (wasEmpty) _onCardShown(userGesture: false);
        } else if (!pendingNow && !skippedNew) {
          // Không có gì mới, không còn lượt chờ, không phải bỏ thẻ mới ⇒ server thật sự hết thẻ cho phiên này.
          _exhausted = true;
        }
        // Đã bỏ thẻ mới mà outbox đã trống (không còn gì kích hoạt lại) ⇒ tự tải lại.
        reload = skippedNew && !pendingNow;
      });
      _loadingMore = false;
      if (reload) {
        await Future<void>.delayed(const Duration(milliseconds: 300));
        if (!mounted) return;
      }
      // Lô này không thêm gì mà còn lượt chờ ⇒ KHÔNG gọi lại ngay (vòng lặp gọi `/srs/queue`); chờ outbox đổi hoặc
      // lượt chấm kế (`ref.listen` ở build) — tương đương effect theo deps của web.
      _afterDeckChange(allowLoadMore: added.isNotEmpty || reload);
    } on Object catch (err) {
      if (!mounted) return;
      setState(() => _moreError = ApiError.from(err));
      _loadingMore = false;
      // Lỗi tải thêm: không gọi lại ngay — thử lại sau lượt chấm kế / nút Thử lại.
      _afterDeckChange(allowLoadMore: false);
    }
  }

  /// Sau mọi thay đổi bộ bài/outbox: tải thêm nếu cần ([allowLoadMore]); hết thẻ + server hết ⇒ tổng kết.
  void _afterDeckChange({bool allowLoadMore = true}) {
    if (!mounted || _phase != _Phase.ready) return;
    if (allowLoadMore && shouldLoadMore(_remaining, loading: _loadingMore, exhausted: _exhausted)) {
      unawaited(_loadMore());
      return;
    }
    if (_remaining == 0 && _exhausted && !_loadingMore) {
      setState(() {
        _sessionEnd = DateTime.now();
        _phase = _Phase.finished;
      });
    }
  }

  /// Thẻ mới hiện: mốc thời gian + tự đọc (một lần theo id thẻ; đổi giọng giữa chừng không đọc lại).
  void _onCardShown({required bool userGesture}) {
    final card = _current;
    if (card == null) return;
    _shown
      ..reset()
      ..start();
    if (!ref.read(autoPlayAudioProvider) || !ref.read(speechControllerProvider).canSpeak) return;
    if (_spokenCardId == card.cardId) return;
    _spokenCardId = card.cardId;
    unawaited(speakZh(ref, card.word.simplified));
  }

  void _flip() {
    if (_current == null || _flipped) return;
    setState(() => _flipped = true);
  }

  void _rate(SrsRating rating) {
    final current = _current;
    if (current == null || !_flipped || _locked) return;
    final durationMs = clampDurationMs(_shown.elapsedMilliseconds);
    unawaited(HapticFeedback.selectionClick());
    // Lạc quan: ghi nhận + chuyển thẻ kế ngay; gửi ở nền qua outbox (clientReviewId sinh MỘT lần, giữ khi gửi lại).
    _submitSeq++;
    ref
        .read(reviewOutboxProvider.notifier)
        .submit(OutboxItem(clientReviewId: uuidV4(), cardId: current.cardId, rating: rating, durationMs: durationMs));
    _ratedIds.add(current.cardId);
    setState(() {
      _ratings.add(rating);
      _flipped = false;
      _index += 1;
      _locked = true;
    });
    _lockTimer?.cancel();
    _lockTimer = Timer(const Duration(milliseconds: kRateLockMs), () {
      if (mounted) setState(() => _locked = false);
    });
    // Đang trong thao tác chạm ⇒ đọc thẻ kế ngay tại đây (iOS/web chỉ cho phát trong thao tác người dùng).
    _onCardShown(userGesture: true);
    _afterDeckChange();
  }

  Future<void> _openDetail(String wordId) async {
    setState(() => _detailOpen = true);
    try {
      await showWordDetailSheet(context, wordId);
    } finally {
      if (mounted) setState(() => _detailOpen = false);
    }
  }

  /// Rời phiên: chỉ hỏi khi có lượt đã gửi HỎNG (lượt đang gửi lần đầu vẫn hoàn tất ở nền; outbox bền nên an toàn).
  Future<void> _handleClose() async {
    final unsent = ref.read(reviewOutboxProvider).unsentCount;
    if (unsent > 0) {
      final ok = await showAfConfirm(
        context: context,
        title: 'Rời phiên ôn?',
        message: 'Còn $unsent đánh giá chưa gửi. Rời đi vẫn giữ để gửi lại sau?',
        confirmLabel: 'Rời đi',
        cancelLabel: 'Ở lại',
      );
      if (!ok || !mounted) return;
    }
    _leave();
  }

  void _leave() {
    if (context.canPop()) {
      context.pop();
    } else {
      context.go(AppRoutes.review);
    }
  }

  @override
  Widget build(BuildContext context) {
    _speech = ref.read(speechControllerProvider.notifier);
    // Outbox đổi (lượt gửi xong / bị bỏ) ⇒ thử tải thêm lại (thẻ vừa chấm có thể quay lại; 422 ⇒ tải lại khi trống).
    ref.listen<int>(reviewOutboxProvider.select((s) => s.pendingCount), (prev, next) {
      if (prev != next) _afterDeckChange();
    });
    final unsent = ref.watch(reviewOutboxProvider.select((s) => s.unsentCount));
    final theme = Theme.of(context);

    return PopScope(
      canPop: unsent == 0,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop && !_detailOpen) unawaited(_handleClose());
      },
      child: switch (_phase) {
        _Phase.loading => Scaffold(
          appBar: AppBar(
            leading: CloseButton(onPressed: _leave),
            title: const Text('Phiên ôn'),
          ),
          body: const Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [CircularProgressIndicator(), SizedBox(height: 16), Text('Đang lấy thẻ đến hạn…')],
            ),
          ),
        ),
        _Phase.error => Scaffold(
          appBar: AppBar(
            leading: CloseButton(onPressed: _leave),
            title: const Text('Phiên ôn'),
          ),
          body: AfPageBody(
            child: _QueueError(
              error: _loadError!,
              onRetry: () => unawaited(_loadInitial()),
              actions: [OutlinedButton(onPressed: _leave, child: const Text('Về trang ôn tập'))],
            ),
          ),
        ),
        _Phase.finished => Scaffold(
          appBar: AppBar(
            leading: CloseButton(onPressed: () => unawaited(_handleClose())),
            title: const Text('Phiên ôn'),
          ),
          body: AfPageBody(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (unsent > 0) ...[PendingReviewsBanner(count: unsent), const SizedBox(height: 16)],
                SessionSummary(
                  counts: countRatings(_ratings),
                  durationMs: (_sessionEnd ?? DateTime.now()).difference(_sessionStart).inMilliseconds,
                  canContinue: (ref.watch(ref.watch(srsSummaryProvider)).value?.toStart ?? 0) > 0,
                  onContinue: () => unawaited(_loadInitial()),
                  onHome: () => unawaited(_handleClose()),
                ),
              ],
            ),
          ),
        ),
        _Phase.ready => Scaffold(
          body: SafeArea(
            bottom: false,
            child: Column(
              children: [
                SessionHeader(done: _ratings.length, total: _deck.length, onClose: () => unawaited(_handleClose())),
                if (unsent > 0)
                  Padding(
                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 4),
                    child: PendingReviewsBanner(count: unsent),
                  ),
                Expanded(
                  child: Center(
                    child: SingleChildScrollView(
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
                      child: ConstrainedBox(
                        constraints: const BoxConstraints(maxWidth: afMaxContentWidth),
                        child: _current != null
                            ? Flashcard(
                                card: _current!,
                                flipped: _flipped,
                                onFlip: _flip,
                                onOpenDetail: (id) => unawaited(_openDetail(id)),
                              )
                            : _moreError != null
                            ? _QueueError(
                                error: _moreError!,
                                onRetry: () => unawaited(_loadMore()),
                                actions: [
                                  OutlinedButton(
                                    onPressed: () => setState(() {
                                      _sessionEnd = DateTime.now();
                                      _phase = _Phase.finished;
                                    }),
                                    child: const Text('Kết thúc phiên'),
                                  ),
                                ],
                              )
                            : const Column(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  CircularProgressIndicator(),
                                  SizedBox(height: 12),
                                  Text('Đang lấy thêm thẻ…'),
                                ],
                              ),
                      ),
                    ),
                  ),
                ),
                if (_moreError != null && _remaining > 0)
                  Padding(
                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
                    child: Text(
                      'Chưa lấy thêm được thẻ — sẽ thử lại sau lượt chấm kế.',
                      style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.error),
                      textAlign: TextAlign.center,
                    ),
                  ),
              ],
            ),
          ),
          bottomNavigationBar: _current == null
              ? null
              : StickyActionBar(
                  children: [
                    if (_flipped)
                      RatingBar(intervals: _current!.intervals, onRate: _rate, disabled: _locked)
                    else
                      OutlinedButton(
                        key: kFlipButtonKey,
                        onPressed: _flip,
                        style: OutlinedButton.styleFrom(
                          minimumSize: const Size(0, 56),
                          textStyle: const TextStyle(fontSize: 18),
                        ),
                        child: const Text('Hiện đáp án'),
                      ),
                  ],
                ),
        ),
      },
    );
  }
}

/// Lỗi tải hàng đợi: 503 ⇒ "Học liệu chưa sẵn sàng"; còn lại theo `ErrorView` + Thử lại + hành động thêm.
class _QueueError extends StatelessWidget {
  const _QueueError({required this.error, required this.onRetry, this.actions = const []});

  final ApiError error;
  final VoidCallback onRetry;
  final List<Widget> actions;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ErrorView(
        kind: error.isNetwork
            ? ErrorViewKind.network
            : error.isForbidden
            ? ErrorViewKind.forbidden
            : ErrorViewKind.unknown,
        title: error.status == 503 ? 'Học liệu chưa sẵn sàng' : 'Không tải được thẻ',
        message: error.message,
        onRetry: onRetry,
        actions: actions,
        compact: true,
      ),
    );
  }
}
