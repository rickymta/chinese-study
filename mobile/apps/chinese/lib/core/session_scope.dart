import 'package:af_auth/af_auth.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Id người dùng hiện tại (null khi chưa đăng nhập). MỌI provider dữ liệu theo người dùng phải
/// `ref.watch(userScopeProvider)` để đăng xuất/đổi người là tự huỷ cache (hợp đồng mobile §5.3.8).
///
/// Ví dụ: `final srsSummaryProvider = FutureProvider.autoDispose((ref) { ref.watch(userScopeProvider); ... });`
final userScopeProvider = Provider<String?>((ref) => ref.watch(currentAccountProvider)?.id);
