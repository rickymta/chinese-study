import 'dart:async';
import 'dart:convert';

import 'package:af_auth/af_auth.dart';
import 'package:af_chinese/api/clients.dart';
import 'package:af_chinese/config/app_config_provider.dart';
import 'package:af_chinese/core/session_scope.dart';
import 'package:af_chinese/features/auth/application/auth_providers.dart';
import 'package:af_chinese/features/srs/application/outbox_controller.dart';
import 'package:af_chinese/features/srs/application/providers.dart';
import 'package:af_chinese/features/srs/data/models.dart';
import 'package:af_chinese/features/srs/domain/review_outbox.dart';
import 'package:af_core/af_core.dart';
import 'package:af_ui/af_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/test_app.dart';

// Tiêu chí M6 #1: OutboxController với InMemoryKeyValueStore + API giả.

OutboxItem item(String id, {String? cardId}) =>
    OutboxItem(clientReviewId: id, cardId: cardId ?? 'card-$id', rating: SrsRating.good, durationMs: 900);

String summaryBody({int dueNow = 3, int newAvailable = 2}) => jsonEncode({
  'localDate': '2026-09-17',
  'timeZone': 'Asia/Ho_Chi_Minh',
  'dueToday': dueNow,
  'dueNow': dueNow,
  'reviewedToday': 1,
  'reviewsDoneToday': 1,
  'reviewLimitRemaining': 199,
  'dailyReviewLimit': 200,
  'newIntroducedToday': 0,
  'newAvailableToday': newAvailable,
  'dailyNewCards': 10,
  'totalCards': 5,
  'matureCards': 0,
});

ReviewResponse okResponse(OutboxItem it, {int dueNow = 3}) => ReviewResponse.fromJson({
  'reviewId': 'r-${it.clientReviewId}',
  'duplicate': false,
  'card': {'cardId': it.cardId, 'state': 'review', 'isSuspended': false},
  'summary': jsonDecode(summaryBody(dueNow: dueNow)),
});

/// Máy chủ giả: [failuresLeft] lần đầu ném lỗi mạng, sau đó nhận và ghi [received] (id ⇒ số lần nhận).
class FakeServer {
  FakeServer({this.failuresLeft = 0});

  int failuresLeft;
  final List<String> calls = [];
  final Map<String, int> received = {};
  Object? Function(OutboxItem it)? errorFor;

  Future<ReviewResponse> send(OutboxItem it) async {
    calls.add(it.clientReviewId);
    final custom = errorFor?.call(it);
    if (custom != null) throw custom;
    if (failuresLeft > 0) {
      failuresLeft--;
      throw ApiError.network();
    }
    received[it.clientReviewId] = (received[it.clientReviewId] ?? 0) + 1;
    return okResponse(it);
  }
}

/// Người dùng hiện tại đổi được trong test (mô phỏng đăng xuất A ⇒ đăng nhập B).
class ScopeCtl extends Notifier<String?> {
  ScopeCtl(this.initial);

  final String? initial;

  @override
  String? build() => initial;

  void set(String? v) => state = v;
}

/// Kho đọc chậm: `getString` chờ [gate] — mô phỏng shared_preferences trả về sau khi người học đã chấm.
class SlowStore extends InMemoryKeyValueStore {
  SlowStore(super.initial);

  final gate = Completer<void>();

  @override
  Future<String?> getString(String key) async {
    final v = await super.getString(key);
    await gate.future;
    return v;
  }
}

ProviderContainer makeContainer({
  required String? userId,
  required KeyValueStore store,
  required FakeServer server,
  NotifierProvider<ScopeCtl, String?>? scope,
  SendReview? send,
}) {
  final container = ProviderContainer(
    overrides: [
      appConfigProvider.overrideWithValue(testConfig),
      keyValueStoreProvider.overrideWithValue(store),
      tokenStoreProvider.overrideWithValue(InMemoryTokenStore()),
      chineseAdapterProvider.overrideWithValue(FakeAdapter((_) async => (200, summaryBody()))),
      if (scope != null)
        userScopeProvider.overrideWith((ref) => ref.watch(scope))
      else
        userScopeProvider.overrideWithValue(userId),
      outboxSenderProvider.overrideWithValue(send ?? server.send),
    ],
    retry: afNoRetry,
  );
  // Giữ provider sống như `OutboxNotices` trong app (không có listener thì Notifier chỉ tồn tại lúc read).
  final sub = container.listen(reviewOutboxProvider, (_, _) {});
  addTearDown(() {
    sub.close();
    container.dispose();
  });
  return container;
}

void main() {
  test('mất mạng 3 lần rồi thành công ⇒ CÙNG clientReviewId, thứ tự giữ nguyên, kho được ghi/xoá theo', () async {
    final store = InMemoryKeyValueStore();
    final server = FakeServer(failuresLeft: 3);
    final container = makeContainer(userId: 'A', store: store, server: server);
    final notifier = container.read(reviewOutboxProvider.notifier);
    await pumpEventQueue();
    expect(container.read(reviewOutboxProvider).restored, isTrue);

    notifier
      ..submit(item('a'))
      ..submit(item('b'))
      ..submit(item('c'));
    await pumpEventQueue();
    // Lần 1 hỏng ⇒ a.attempts = 1 ⇒ unsentCount = cả hàng đợi; kho có đủ 3 phần tử.
    var state = container.read(reviewOutboxProvider);
    expect(state.pendingCount, 3);
    expect(state.unsentCount, 3);
    expect(decodeOutbox(await store.getString(outboxStorageKey('A'))).map((x) => x.clientReviewId), ['a', 'b', 'c']);

    // Thay vì chờ bậc thang (1 s, 2 s…) — ép gửi lại như app resumed.
    notifier.flushNow();
    await pumpEventQueue();
    notifier.flushNow();
    await pumpEventQueue();
    expect(container.read(reviewOutboxProvider).items.first.attempts, 3);
    notifier.flushNow();
    await pumpEventQueue();

    state = container.read(reviewOutboxProvider);
    expect(state.items, isEmpty);
    expect(server.calls, ['a', 'a', 'a', 'a', 'b', 'c']);
    expect(server.received, {'a': 1, 'b': 1, 'c': 1});
    expect(await store.containsKey(outboxStorageKey('A')), isFalse);
    // Gửi xong ⇒ summary của server được ghi vào provider tóm tắt (huy hiệu).
    expect(container.read(srsSummaryByUserProvider('A')).value?.dueNow, 3);
  });

  test('controller mới đọc lại phần tử đã lưu (mô phỏng tắt app) và gửi với cùng id', () async {
    final store = InMemoryKeyValueStore({
      outboxStorageKey('A'): encodeOutbox([item('x'), item('y')]),
    });
    final server = FakeServer();
    final container = makeContainer(userId: 'A', store: store, server: server);
    container.read(reviewOutboxProvider);
    await pumpEventQueue();
    expect(server.calls, ['x', 'y']);
    expect(container.read(reviewOutboxProvider).items, isEmpty);
    expect(await store.containsKey(outboxStorageKey('A')), isFalse);
  });

  test('outbox của user A KHÔNG gửi khi đăng nhập user B; kho của A giữ nguyên', () async {
    final raw = encodeOutbox([item('x')]);
    final store = InMemoryKeyValueStore({outboxStorageKey('A'): raw});
    final server = FakeServer();
    final container = makeContainer(userId: 'B', store: store, server: server);
    container.read(reviewOutboxProvider);
    await pumpEventQueue();
    expect(server.calls, isEmpty);
    expect(container.read(reviewOutboxProvider).items, isEmpty);
    expect(await store.getString(outboxStorageKey('A')), raw);
    expect(await container.read(pendingOutboxCountProvider.future), 0);
  });

  test('chưa đăng nhập ⇒ submit bị bỏ qua, không ghi kho', () async {
    final store = InMemoryKeyValueStore();
    final server = FakeServer();
    final container = makeContainer(userId: null, store: store, server: server);
    container.read(reviewOutboxProvider.notifier).submit(item('a'));
    await pumpEventQueue();
    expect(server.calls, isEmpty);
    expect(store.snapshot, isEmpty);
  });

  test('4xx thật (422) ⇒ bỏ phần tử + lastDrop mang thông điệp server; phần tử sau vẫn gửi', () async {
    final store = InMemoryKeyValueStore();
    final server = FakeServer()
      ..errorFor = (it) => it.clientReviewId == 'a'
          ? ApiError('Đã đủ từ mới hôm nay', status: 422, code: 'NEW_CARD_LIMIT_REACHED')
          : null;
    final container = makeContainer(userId: 'A', store: store, server: server);
    final notifier = container.read(reviewOutboxProvider.notifier);
    await pumpEventQueue();
    notifier
      ..submit(item('a'))
      ..submit(item('b'));
    await pumpEventQueue();
    final state = container.read(reviewOutboxProvider);
    expect(state.items, isEmpty);
    expect(state.lastDrop?.item.clientReviewId, 'a');
    expect(state.lastDrop?.error.code, 'NEW_CARD_LIMIT_REACHED');
    expect(state.lastDrop?.error.message, 'Đã đủ từ mới hôm nay');
    expect(server.received.keys, ['b']);
  });

  test('unsentCount = 0 khi mọi phần tử đang gửi lần đầu; trùng id không thêm lần hai', () async {
    final store = InMemoryKeyValueStore();
    final server = FakeServer(failuresLeft: 1);
    final container = makeContainer(userId: 'A', store: store, server: server);
    final notifier = container.read(reviewOutboxProvider.notifier);
    await pumpEventQueue();
    notifier
      ..submit(item('a'))
      ..submit(item('a'));
    expect(container.read(reviewOutboxProvider).pendingCount, 1);
    expect(container.read(reviewOutboxProvider).unsentCount, 0);
    await pumpEventQueue();
    expect(container.read(reviewOutboxProvider).unsentCount, 1);
    expect(await container.read(pendingOutboxCountProvider.future), 1);
  });

  test('đổi A→B giữa lúc gửi dở: phần còn lại của A KHÔNG đi bằng phiên B, kho A giữ nguyên (review M6 C1)', () async {
    final store = InMemoryKeyValueStore({
      outboxStorageKey('A'): encodeOutbox([item('a1'), item('a2'), item('a3')]),
    });
    final scope = NotifierProvider<ScopeCtl, String?>(() => ScopeCtl('A'));
    late ProviderContainer container;
    final sentUnder = <String>[];
    final gate = Completer<void>();
    Future<ReviewResponse> send(OutboxItem it) async {
      if (it.clientReviewId == 'a1') await gate.future;
      sentUnder.add('${it.clientReviewId}@${container.read(userScopeProvider)}');
      return okResponse(it);
    }

    container = makeContainer(userId: 'A', store: store, server: FakeServer(), scope: scope, send: send);
    await pumpEventQueue();
    // a1 đang bay ⇒ đổi sang B.
    container.read(scope.notifier).set('B');
    await pumpEventQueue();
    expect(container.read(reviewOutboxProvider).userId, 'B');
    expect(container.read(reviewOutboxProvider).items, isEmpty);
    gate.complete();
    await pumpEventQueue();
    expect(sentUnder, ['a1@B']); // lời gọi a1 đã đi trước khi đổi (token A lúc gửi); a2/a3 KHÔNG được gửi
    expect(decodeOutbox(await store.getString(outboxStorageKey('A'))).map((x) => x.clientReviewId), ['a1', 'a2', 'a3']);
    expect(await store.containsKey(outboxStorageKey('B')), isFalse);
    // A đăng nhập lại ⇒ gửi tiếp (a1 trùng id ⇒ server trả duplicate, không tạo log).
    container.read(scope.notifier).set('A');
    await pumpEventQueue();
    expect(sentUnder, ['a1@B', 'a1@A', 'a2@A', 'a3@A']);
    expect(await store.containsKey(outboxStorageKey('A')), isFalse);
  });

  test('A→null→A khi a1 đang bay: chỉ MỘT vòng gửi, mỗi id đúng 1 lần (review R3-1)', () async {
    final store = InMemoryKeyValueStore({
      outboxStorageKey('A'): encodeOutbox([item('a1'), item('a2'), item('a3')]),
    });
    final scope = NotifierProvider<ScopeCtl, String?>(() => ScopeCtl('A'));
    final sent = <String>[];
    var inflight = 0;
    var maxInflight = 0;
    var firstA1 = true;
    final gate = Completer<void>();
    Future<ReviewResponse> send(OutboxItem it) async {
      inflight++;
      if (inflight > maxInflight) maxInflight = inflight;
      if (it.clientReviewId == 'a1' && firstA1) {
        firstA1 = false;
        await gate.future;
      } else {
        await Future<void>.delayed(const Duration(milliseconds: 5));
      }
      inflight--;
      sent.add(it.clientReviewId);
      return okResponse(it);
    }

    final container = makeContainer(userId: 'A', store: store, server: FakeServer(), scope: scope, send: send);
    await pumpEventQueue();
    container.read(scope.notifier).set(null);
    await pumpEventQueue();
    container.read(scope.notifier).set('A');
    await Future<void>.delayed(const Duration(milliseconds: 5));
    await pumpEventQueue();
    gate.complete();
    await Future<void>.delayed(const Duration(milliseconds: 100));
    await pumpEventQueue();
    expect(maxInflight, 1);
    // Lượt a1 của vòng cũ hoàn tất khi vẫn cùng người ⇒ được gỡ khỏi hàng đợi đời mới; mỗi id gửi đúng 1 lần.
    expect(sent, ['a1', 'a2', 'a3']);
    expect(container.read(reviewOutboxProvider).items, isEmpty);
    expect(await store.containsKey(outboxStorageKey('A')), isFalse);
  });

  test('chấm TRƯỚC khi đọc xong kho: gộp kho trước + lượt mới sau, không mất, không gửi lặp (review M6 C2)', () async {
    final store = SlowStore({
      outboxStorageKey('A'): encodeOutbox([item('s1'), item('s2')]),
    });
    final server = FakeServer();
    final container = makeContainer(userId: 'A', store: store, server: server);
    container.read(reviewOutboxProvider.notifier).submit(item('n1'));
    await pumpEventQueue();
    // Chưa đọc xong kho ⇒ chỉ giữ bộ nhớ: chưa gửi, chưa ghi đè kho.
    expect(server.calls, isEmpty);
    expect(container.read(reviewOutboxProvider).restored, isFalse);
    expect(decodeOutbox(store.snapshot[outboxStorageKey('A')] as String?).length, 2); // getString bị cổng chặn
    store.gate.complete();
    await pumpEventQueue();
    expect(server.calls, ['s1', 's2', 'n1']);
    expect(server.received, {'s1': 1, 's2': 1, 'n1': 1});
    expect(container.read(reviewOutboxProvider).items, isEmpty);
    expect(await store.containsKey(outboxStorageKey('A')), isFalse);
  });

  test('401 khi gửi mà phiên CHƯA mất ⇒ giữ nguyên (attempts 0, không bỏ) và hẹn lại sau 30 s', () async {
    final store = InMemoryKeyValueStore();
    var fail401 = true;
    final server = FakeServer()..errorFor = (_) => fail401 ? ApiError('Cần đăng nhập để tiếp tục.', status: 401) : null;
    final container = makeContainer(userId: 'A', store: store, server: server);
    final notifier = container.read(reviewOutboxProvider.notifier);
    await pumpEventQueue();
    notifier
      ..submit(item('a'))
      ..submit(item('b'));
    await pumpEventQueue();
    final state = container.read(reviewOutboxProvider);
    expect(state.items, [item('a'), item('b')]); // attempts vẫn 0
    expect(state.lastDrop, isNull);
    expect(server.calls, ['a']);
    expect(decodeOutbox(await store.getString(outboxStorageKey('A'))).length, 2);
    // Phiên vẫn còn ⇒ hẹn lại 30 s (mô phỏng bằng flushNow như app resumed) ⇒ gửi được.
    fail401 = false;
    notifier.flushNow();
    await pumpEventQueue();
    expect(server.received.keys, ['a', 'b']);
  });

  test('pendingOutboxCountProvider chờ đọc xong kho rồi mới đếm (app vừa mở, kho có 2 lượt)', () async {
    final store = SlowStore({
      outboxStorageKey('A'): encodeOutbox([item('s1'), item('s2')]),
    });
    final container = makeContainer(userId: 'A', store: store, server: FakeServer(failuresLeft: 9));
    container.read(reviewOutboxProvider);
    final counting = container.read(pendingOutboxCountProvider.future);
    await pumpEventQueue();
    expect(container.read(reviewOutboxProvider).restored, isFalse);
    store.gate.complete();
    expect(await counting, 2);
  });

  test('_restore cũng cắt theo giới hạn kho; lastDrop mang lượt CŨ bị bỏ', () async {
    final store = InMemoryKeyValueStore({
      outboxStorageKey('A'): encodeOutbox([for (var i = 0; i < kOutboxMaxItems + 1; i++) item('i$i')]),
    });
    final container = makeContainer(userId: 'A', store: store, server: FakeServer(failuresLeft: 9999));
    container.read(reviewOutboxProvider);
    await pumpEventQueue();
    final state = container.read(reviewOutboxProvider);
    expect(state.items.length, kOutboxMaxItems);
    expect(state.items.first.clientReviewId, 'i1');
    expect(state.lastDrop?.item.clientReviewId, 'i0');
  });

  test('kho đầy (500) ⇒ bỏ lượt cũ nhất + lastDrop báo', () async {
    final store = InMemoryKeyValueStore();
    final server = FakeServer(failuresLeft: 1000);
    final container = makeContainer(userId: 'A', store: store, server: server);
    final notifier = container.read(reviewOutboxProvider.notifier);
    await pumpEventQueue();
    for (var i = 0; i < kOutboxMaxItems + 2; i++) {
      notifier.submit(item('i$i'));
    }
    await pumpEventQueue();
    final state = container.read(reviewOutboxProvider);
    expect(state.items.length, kOutboxMaxItems);
    expect(state.items.first.clientReviewId, 'i2');
    // Lượt CŨ bị bỏ (không phải lượt vừa chấm): i0 gửi hỏng quay lại đầu hàng rồi bị cắt lần nữa.
    expect(state.lastDrop?.item.clientReviewId, 'i0');
    expect(state.lastDrop?.error.message, contains('đã đầy'));
  });

  test('clearForUser (đăng xuất) xoá kho + hàng đợi trong bộ nhớ, không gửi nữa', () async {
    final store = InMemoryKeyValueStore();
    final server = FakeServer(failuresLeft: 5);
    final container = makeContainer(userId: 'A', store: store, server: server);
    final notifier = container.read(reviewOutboxProvider.notifier);
    await pumpEventQueue();
    notifier.submit(item('a'));
    await pumpEventQueue();
    expect(await store.containsKey(outboxStorageKey('A')), isTrue);
    await notifier.clearForUser('A');
    await pumpEventQueue();
    expect(container.read(reviewOutboxProvider).items, isEmpty);
    expect(await store.containsKey(outboxStorageKey('A')), isFalse);
    notifier.flushNow();
    await pumpEventQueue();
    expect(server.calls, ['a']);
  });
}
