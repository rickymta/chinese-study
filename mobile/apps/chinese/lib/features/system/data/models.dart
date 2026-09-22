import 'package:af_core/af_core.dart';

/// Phản hồi `GET /api/system/info` của MỌI service AntFarm (hợp đồng §6.1 HĐG).
class SystemInfo {
  const SystemInfo({required this.service, required this.version, required this.environment, this.serverTimeUtc});

  /// Model viết tay (DB-M3): thiếu trường ⇒ chuỗi rỗng/null, không ném.
  factory SystemInfo.fromJson(JsonMap json) => SystemInfo(
    service: readStringOr(json, 'service'),
    version: readStringOr(json, 'version'),
    environment: readStringOr(json, 'environment'),
    serverTimeUtc: readDateTime(json, 'serverTimeUtc'),
  );

  final String service;
  final String version;
  final String environment;

  /// ISO-8601 UTC, vd `2026-09-16T08:00:00Z`.
  final DateTime? serverTimeUtc;

  Map<String, Object?> toJson() => {
    'service': service,
    'version': version,
    'environment': environment,
    'serverTimeUtc': serverTimeUtc?.toIso8601String(),
  };
}

/// Hai service mà app tiếng Trung đi qua gateway tới.
enum SystemService {
  chinese('Tiếng Trung', 'chinese-backend'),
  identity('Tài khoản', 'identity-service');

  const SystemService(this.label, this.serviceName);

  /// Nhãn hiện trước dấu hai chấm trên chip.
  final String label;

  /// Tên service trong phản hồi (dùng khi báo "Không tới được: ...").
  final String serviceName;
}
