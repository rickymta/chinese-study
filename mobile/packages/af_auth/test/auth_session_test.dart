import 'dart:async';

import 'package:af_auth/af_auth.dart';
import 'package:af_core/af_core.dart';
import 'package:fake_async/fake_async.dart';
import 'package:flutter_test/flutter_test.dart';

import 'helpers/auth_fixtures.dart';

void main() {
  final now = DateTime.utc(2026, 9, 17, 8);

  /// Dựng session với lời gọi refresh giả [responses] (mỗi phần tử: response hoặc lỗi), [waits] ghi các khoảng chờ.
  ({
    AuthSession session,
    InMemoryTokenStore store,
    List<String> calls,
    List<Duration> waits,
    List<AuthSessionEvent> events,
  })
  build({required List<Object> responses, StoredSession? stored, Completer<void>? gate, DateTime Function()? nowFn}) {
    final store = InMemoryTokenStore(stored);
    final calls = <String>[];
    final waits = <Duration>[];
    var i = 0;
    final session = AuthSession(
      store: store,
      now: nowFn ?? () => now,
      wait: (d) async => waits.add(d),
      refreshCall: (token) async {
        calls.add(token);
        if (gate != null) await gate.future;
        final r = responses[i < responses.length ? i : responses.length - 1];
        i++;
        if (r is MobileRefreshResponse) return r;
        throw r;
      },
    );
    final events = <AuthSessionEvent>[];
    session.events.listen(events.add);
    return (session: session, store: store, calls: calls, waits: waits, events: events);
  }

  test('restore: đọc kho; setFromAuth ghi kho TRƯỚC rồi mới giữ access token (RM-S2)', () async {
    final t = build(responses: []);
    expect(await t.session.restore(), isNull);
    expect(t.session.accessToken, isNull);

    await t.session.setFromAuth(authResponse(access: 'at-1', refresh: 'rt-1'));
    expect(t.store.log, ['read', 'write:rt-1']);
    expect(t.session.accessToken, 'at-1');
    expect(t.session.refreshToken, 'rt-1');
    expect(t.session.account, testAccount);
    expect(t.events.single, isA<AuthTokenChanged>());
    t.session.dispose();
  });

  test('3 lời gọi refresh đồng thời ⇒ ĐÚNG 1 lời gọi mạng, cùng token', () async {
    final gate = Completer<void>();
    final t = build(
      responses: [refreshResponse(access: 'at-1', refresh: 'rt-1')],
      stored: storedSession(),
      gate: gate,
    );
    await t.session.restore();
    final f1 = t.session.refresh();
    final f2 = t.session.refresh();
    final f3 = t.session.refresh();
    gate.complete();
    expect(await Future.wait([f1, f2, f3]), ['at-1', 'at-1', 'at-1']);
    expect(t.calls, ['rt-0']);
    expect(t.session.refreshToken, 'rt-1');
    // Lời gọi sau khi xong ⇒ lời gọi mạng mới với token đã xoay.
    await t.session.refresh();
    expect(t.calls, ['rt-0', 'rt-1']);
    t.session.dispose();
  });

  test('refresh(force: true) khi đang có lượt bay ⇒ lượt MỚI xếp sau, 2 lời gọi mạng, token của lượt sau', () async {
    final gate = Completer<void>();
    final t = build(
      responses: [
        refreshResponse(access: 'at-1', refresh: 'rt-1'),
        refreshResponse(access: 'at-2', refresh: 'rt-2'),
      ],
      stored: storedSession(),
      gate: gate,
    );
    await t.session.restore();
    final f1 = t.session.refresh();
    final f2 = t.session.refresh(force: true);
    final f3 = t.session.refresh(); // dùng chung lượt force (đang là lượt hiện hành)
    expect(t.calls, ['rt-0']); // lượt force CHƯA gọi mạng — chờ lượt 1 xong (không xoay song song)
    gate.complete();
    expect(await Future.wait([f1, f2, f3]), ['at-1', 'at-2', 'at-2']);
    expect(t.calls, ['rt-0', 'rt-1']); // lượt 2 dùng token đã xoay của lượt 1
    expect(t.session.refreshToken, 'rt-2');
    t.session.dispose();
  });

  test('refresh: kho được ghi trước khi Future hoàn tất (thứ tự trong log)', () async {
    final t = build(
      responses: [refreshResponse(refresh: 'rt-1')],
      stored: storedSession(),
    );
    await t.session.restore();
    final order = <String>[];
    final f = t.session.refresh().then((_) => order.add('done:${t.store.log.last}'));
    await f;
    expect(order, ['done:write:rt-1']);
    expect(t.store.session?.refreshToken, 'rt-1');
    t.session.dispose();
  });

  test('lỗi mạng 2 lần rồi thành công ⇒ không mất phiên, chờ 1 s rồi 3 s', () async {
    final t = build(
      responses: [
        ApiError.network(),
        ApiError.network(),
        refreshResponse(access: 'at-ok', refresh: 'rt-ok'),
      ],
      stored: storedSession(),
    );
    await t.session.restore();
    expect(await t.session.refresh(), 'at-ok');
    expect(t.calls, ['rt-0', 'rt-0', 'rt-0']);
    expect(t.waits, [const Duration(seconds: 1), const Duration(seconds: 3)]);
    expect(t.events.whereType<AuthSessionLost>(), isEmpty);
    t.session.dispose();
  });

  test('lỗi mạng 3 lần ⇒ ném lỗi nhưng GIỮ phiên trong kho (RM-S3)', () async {
    final t = build(responses: [ApiError.network()], stored: storedSession());
    await t.session.restore();
    await expectLater(t.session.refresh(), throwsA(isA<ApiError>().having((e) => e.isNetwork, 'isNetwork', true)));
    expect(t.calls, hasLength(3));
    expect(t.store.session?.refreshToken, 'rt-0');
    expect(t.session.hasStoredSession, isTrue);
    expect(t.events.whereType<AuthSessionLost>(), isEmpty);
    t.session.dispose();
  });

  test('5xx cũng thử lại; 4xx khác (429) không thử lại và giữ phiên', () async {
    final t = build(
      responses: [ApiError('Dịch vụ chưa sẵn sàng', status: 503), refreshResponse()],
      stored: storedSession(),
    );
    await t.session.restore();
    await t.session.refresh();
    expect(t.calls, hasLength(2));

    final t2 = build(
      responses: [ApiError('Quá nhanh', status: 429, code: 'RATE_LIMITED')],
      stored: storedSession(),
    );
    await t2.session.restore();
    await expectLater(t2.session.refresh(), throwsA(isA<ApiError>()));
    expect(t2.calls, hasLength(1));
    expect(t2.store.session, isNotNull);
    t.session.dispose();
    t2.session.dispose();
  });

  test('refresh 401 ⇒ kho bị xoá + phát AuthSessionLost + ném lỗi; 403 tương tự', () async {
    for (final status in [401, 403]) {
      final t = build(
        responses: [ApiError('mất', status: status, code: 'REFRESH_INVALID')],
        stored: storedSession(),
      );
      await t.session.restore();
      await expectLater(t.session.refresh(), throwsA(isA<ApiError>().having((e) => e.status, 'status', status)));
      expect(t.store.session, isNull);
      expect(t.store.log.last, 'clear');
      expect(t.session.hasStoredSession, isFalse);
      expect(t.session.accessToken, isNull);
      expect(t.events.whereType<AuthSessionLost>(), hasLength(1));
      expect(t.calls, hasLength(1)); // không thử lại
      t.session.dispose();
    }
  });

  test('không có phiên trong kho ⇒ refresh ném 401 NO_SESSION, KHÔNG phát lost', () async {
    final t = build(responses: []);
    await t.session.restore();
    await expectLater(t.session.refresh(), throwsA(isA<ApiError>().having((e) => e.code, 'code', 'NO_SESSION')));
    expect(t.events, isEmpty);
    t.session.dispose();
  });

  test('handleAuthLost (từ Dio): xoá kho, phát lost một lần; gọi lại khi đã rỗng không phát thêm', () async {
    final t = build(responses: [], stored: storedSession());
    await t.session.restore();
    t.session.handleAuthLost();
    await Future<void>.delayed(Duration.zero);
    t.session.handleAuthLost();
    await Future<void>.delayed(Duration.zero);
    expect(t.events.whereType<AuthSessionLost>(), hasLength(1));
    expect(t.store.session, isNull);
    t.session.dispose();
  });

  test('refresh cập nhật tài khoản từ claim JWT mới (name, zoneinfo) — R4-4', () async {
    final jwt = fakeJwt(name: 'Tên Mới', zoneinfo: 'Asia/Bangkok');
    final t = build(
      responses: [refreshResponse(access: jwt)],
      stored: storedSession(),
    );
    await t.session.restore();
    await t.session.refresh();
    expect(t.session.account?.displayName, 'Tên Mới');
    expect(t.session.account?.timeZone, 'Asia/Bangkok');
    expect(t.store.session?.account.displayName, 'Tên Mới');
    t.session.dispose();
  });

  test('kho ghi lỗi ⇒ vẫn giữ access token trong bộ nhớ (không chặn người học)', () async {
    final t = build(responses: []);
    t.store.failNextWrite = StateError('keystore hỏng');
    await t.session.setFromAuth(authResponse(access: 'at-mem'));
    expect(t.session.accessToken, 'at-mem');
    expect(t.store.session, isNull);
    t.session.dispose();
  });

  test('hẹn giờ làm mới 60 s trước hạn; onAppResumed làm mới ngay khi còn ≤ 60 s', () {
    fakeAsync((async) {
      var clock = DateTime.utc(2026, 9, 17, 8);
      final t = build(
        responses: [refreshResponse(access: 'at-2', refresh: 'rt-2', expiresAt: DateTime.utc(2026, 9, 17, 8, 30))],
        nowFn: () => clock,
      );
      // Token hết hạn 08:15 ⇒ hẹn 08:14 (60 s trước hạn).
      t.session.setFromAuth(authResponse(access: 'at-1', refresh: 'rt-1'));
      async.flushMicrotasks();
      expect(t.session.accessToken, 'at-1');

      clock = clock.add(const Duration(minutes: 13, seconds: 59));
      async.elapse(const Duration(minutes: 13, seconds: 59));
      expect(t.calls, isEmpty);

      clock = clock.add(const Duration(seconds: 2));
      async.elapse(const Duration(seconds: 2));
      expect(t.calls, ['rt-1']);
      expect(t.session.accessToken, 'at-2');

      // Token mới hết hạn 08:30; giả lập app ở nền tới 08:29:30 rồi resumed ⇒ còn 30 s ⇒ làm mới ngay.
      clock = DateTime.utc(2026, 9, 17, 8, 29, 30);
      t.session.onAppResumed();
      async.flushMicrotasks();
      expect(t.calls, ['rt-1', 'rt-2']);
      t.session.dispose();
    });
  });

  test('hẹn giờ tối thiểu 5 s khi hạn đã quá gần/đã qua (đồng hồ lệch)', () {
    fakeAsync((async) {
      final t = build(responses: [refreshResponse()], nowFn: () => DateTime.utc(2026, 9, 17, 8, 14, 50));
      t.session.setFromAuth(authResponse()); // hạn 08:15 — chỉ còn 10 s < 60 s
      async.flushMicrotasks();
      async.elapse(const Duration(seconds: 4));
      expect(t.calls, isEmpty);
      async.elapse(const Duration(seconds: 2));
      expect(t.calls, hasLength(1));
      t.session.dispose();
    });
  });

  test('clear: xoá kho + token, huỷ hẹn giờ, KHÔNG phát lost', () async {
    final t = build(responses: [], stored: storedSession());
    await t.session.restore();
    await t.session.setFromAuth(authResponse());
    await t.session.clear();
    expect(t.session.accessToken, isNull);
    expect(t.session.hasStoredSession, isFalse);
    expect(t.store.session, isNull);
    expect(t.events.whereType<AuthSessionLost>(), isEmpty);
    expect(t.events.last, isA<AuthTokenChanged>().having((e) => e.accessToken, 'accessToken', isNull));
    t.session.dispose();
  });
  test('đăng xuất (clear) trong lúc refresh treo ⇒ kết quả bị bỏ, kho rỗng, không hẹn giờ, không lost', () async {
    final gate = Completer<void>();
    final t = build(
      responses: [refreshResponse(access: 'at-muon', refresh: 'rt-muon')],
      stored: storedSession(),
      gate: gate,
    );
    await t.session.restore();
    final pending = t.session.refresh();
    await t.session.clear(); // đăng xuất khi refresh đang bay
    gate.complete();
    await expectLater(pending, throwsA(isA<AuthSessionChanged>()));
    expect(t.store.session, isNull);
    expect(t.session.accessToken, isNull);
    expect(t.session.hasStoredSession, isFalse);
    expect(t.session.hasRefreshTimer, isFalse);
    expect(t.store.log.where((l) => l.startsWith('write:')), isEmpty);
    expect(t.events.whereType<AuthSessionLost>(), isEmpty);
    t.session.dispose();
  });

  test('đăng nhập B trong lúc refresh A treo ⇒ kho + access token là của B, refresh A bị bỏ', () async {
    final gate = Completer<void>();
    final t = build(
      responses: [refreshResponse(access: 'at-A', refresh: 'rt-A2')],
      stored: storedSession(),
      gate: gate,
    );
    await t.session.restore();
    final pendingA = t.session.refresh();
    const accountB = Account(id: 'u-B', email: 'b@vidu.com', displayName: 'B', timeZone: 'UTC');
    await t.session.setFromAuth(authResponse(access: 'at-B', refresh: 'rt-B', account: accountB));
    gate.complete();
    await expectLater(pendingA, throwsA(isA<AuthSessionChanged>()));
    expect(t.session.accessToken, 'at-B');
    expect(t.session.refreshToken, 'rt-B');
    expect(t.store.session?.account.id, 'u-B');
    expect(t.store.log.where((l) => l.startsWith('write:')), ['write:rt-B']);
    // Refresh mới sau đó dùng token của B (không dùng chung future cũ của A).
    t.session.dispose();
  });

  test('401 muộn của refresh A (sau khi B đăng nhập) KHÔNG xoá phiên B, không phát lost', () async {
    final gate = Completer<void>();
    final t = build(
      responses: [ApiError('mất', status: 401, code: 'REFRESH_INVALID')],
      stored: storedSession(),
      gate: gate,
    );
    await t.session.restore();
    final pendingA = t.session.refresh();
    const accountB = Account(id: 'u-B', email: 'b@vidu.com', displayName: 'B', timeZone: 'UTC');
    await t.session.setFromAuth(authResponse(access: 'at-B', refresh: 'rt-B', account: accountB));
    gate.complete();
    await expectLater(pendingA, throwsA(isA<AuthSessionChanged>()));
    expect(t.session.refreshToken, 'rt-B');
    expect(t.store.session?.refreshToken, 'rt-B');
    expect(t.events.whereType<AuthSessionLost>(), isEmpty);
    t.session.dispose();
  });

  test('waitForInflight chờ refresh đang bay xong (nuốt lỗi), không có gì thì về ngay', () async {
    final gate = Completer<void>();
    final t = build(responses: [ApiError.network()], stored: storedSession(), gate: gate);
    await t.session.restore();
    await t.session.waitForInflight();
    final pending = t.session.refresh();
    unawaited(pending.then<void>((_) {}, onError: (Object _) {}));
    final waited = t.session.waitForInflight();
    gate.complete();
    await waited;
    expect(t.calls, isNotEmpty);
    t.session.dispose();
  });
  test(
    'clear() chen giữa lúc write của setFromAuth treo ⇒ không sống lại: token null, không Timer, kho rỗng',
    () async {
      final gate = Completer<void>();
      final t = build(responses: []);
      t.store.writeGate = gate.future;
      final applying = t.session.setFromAuth(authResponse(access: 'at-1', refresh: 'rt-1'));
      final clearing = t.session.clear(); // xếp sau write đang treo
      t.store.writeGate = null;
      gate.complete();
      await Future.wait([applying, clearing]);
      expect(t.session.accessToken, isNull);
      expect(t.session.hasStoredSession, isFalse);
      expect(t.session.hasRefreshTimer, isFalse);
      expect(t.store.session, isNull);
      expect(t.store.log, ['write:rt-1', 'clear']); // đĩa sau cùng khớp thao tác gọi sau cùng
      expect(t.events.whereType<AuthTokenChanged>().where((e) => e.accessToken != null), isEmpty);
      t.session.dispose();
    },
  );

  test('clear() chen giữa lúc write của refresh treo ⇒ refresh ném AuthSessionChanged, kho rỗng', () async {
    final gate = Completer<void>();
    final t = build(
      responses: [refreshResponse(access: 'at-2', refresh: 'rt-2')],
      stored: storedSession(),
    );
    await t.session.restore();
    t.store.writeGate = gate.future;
    final pending = t.session.refresh();
    await Future<void>.delayed(Duration.zero); // refreshCall xong, đang treo ở write
    final clearing = t.session.clear();
    t.store.writeGate = null;
    gate.complete();
    await expectLater(pending, throwsA(isA<AuthSessionChanged>()));
    await clearing;
    expect(t.session.accessToken, isNull);
    expect(t.session.hasRefreshTimer, isFalse);
    expect(t.store.session, isNull);
    expect(t.store.log.last, 'clear');
    t.session.dispose();
  });

  test('setFromAuth(B) chen giữa lúc write của A treo ⇒ kho = B, bộ nhớ = B', () async {
    final gate = Completer<void>();
    final t = build(responses: []);
    t.store.writeGate = gate.future;
    final applyA = t.session.setFromAuth(authResponse(access: 'at-A', refresh: 'rt-A'));
    t.store.writeGate = null;
    const accountB = Account(id: 'u-B', email: 'b@vidu.com', displayName: 'B', timeZone: 'UTC');
    final applyB = t.session.setFromAuth(authResponse(access: 'at-B', refresh: 'rt-B', account: accountB));
    gate.complete();
    await Future.wait([applyA, applyB]);
    expect(t.store.log, ['write:rt-A', 'write:rt-B']);
    expect(t.store.session?.refreshToken, 'rt-B');
    expect(t.session.accessToken, 'at-B');
    expect(t.session.account?.id, 'u-B');
    t.session.dispose();
  });
}
