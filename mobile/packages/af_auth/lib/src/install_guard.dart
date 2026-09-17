import 'package:af_core/af_core.dart';

import 'token_store.dart';

/// Cờ trong `shared_preferences` đánh dấu app đã chạy lần đầu sau khi cài (hợp đồng mobile §5.1.2).
const kInstallFlagKey = 'af.install.v1';

/// RM-S4: cài lại app trên iOS thì Keychain vẫn còn phiên cũ ⇒ lần chạy đầu (thiếu cờ trong `shared_preferences`,
/// thứ bị xoá cùng app) phải xoá sạch khoá `af.auth.*` của secure storage rồi đặt cờ.
///
/// Mọi lỗi kho đều nuốt — không được chặn khởi động app.
class InstallGuard {
  InstallGuard({required this._prefs, required this._tokenStore});

  final KeyValueStore _prefs;
  final TokenStore _tokenStore;

  /// Khoá thăm dò ghi — phân biệt "kho prefs hỏng" với "lần cài đầu".
  static const kProbeKey = 'af.install.probe';

  /// Trả `true` khi đã xoá kho (lần chạy đầu sau cài).
  ///
  /// `KeyValueStore` nuốt lỗi và trả null khi đọc hỏng — giống hệt "chưa có cờ". Để không xoá oan phiên của người
  /// dùng khi prefs lỗi tạm thời, thử GHI một khoá thăm dò trước: ghi thất bại ⇒ coi như kho hỏng, giữ phiên, ghi log.
  Future<bool> ensure() async {
    final installed = await _prefs.getBool(kInstallFlagKey) ?? false;
    if (installed) return false;
    final writable = await _prefs.setBool(kProbeKey, true);
    if (!writable) {
      afLog('InstallGuard: không đọc/ghi được shared_preferences — giữ nguyên phiên, không đặt cờ');
      return false;
    }
    afLog('InstallGuard: lần chạy đầu sau khi cài — xoá phiên cũ trong secure storage');
    await _tokenStore.clearAll();
    await _prefs.setBool(kInstallFlagKey, true);
    return true;
  }
}
