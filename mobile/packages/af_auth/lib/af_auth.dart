/// Phiên đăng nhập mobile dùng chung mọi app AntFarm (hợp đồng mobile §5.3.6): kho token an toàn, làm mới
/// single-flight (ghi kho trước khi dùng), `AuthController` (Riverpod), redirect cho go_router, màn đăng nhập/đăng ký.
library;

export 'src/auth_controller.dart';
export 'src/auth_errors.dart';
export 'src/auth_session.dart';
export 'src/auth_state.dart';
export 'src/identity_client.dart';
export 'src/install_guard.dart';
export 'src/jwt.dart';
export 'src/models.dart';
export 'src/pages/login_page.dart';
export 'src/pages/register_page.dart';
export 'src/router/auth_redirect.dart';
export 'src/time_zones.dart';
export 'src/token_store.dart';
export 'src/validators.dart';
export 'src/widgets/auth_gate.dart';
export 'src/widgets/auth_lifecycle_observer.dart';
export 'src/widgets/auth_scaffold.dart';
export 'src/widgets/password_field.dart';
export 'src/widgets/time_zone_field.dart';
