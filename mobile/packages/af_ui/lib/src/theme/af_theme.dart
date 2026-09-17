import 'package:flutter/material.dart';

/// Màu nền tảng dùng chung mọi ngôn ngữ (bám `frontend/packages/ui/src/theme/buildTheme.ts`).
const afPrimaryLight = Color(0xFF2E7D32);

/// Chế độ tối cần tông SÁNG hơn để chữ nút text/outlined còn đọc được trên nền tối.
const afPrimaryDark = Color(0xFF66BB6A);
const afOnPrimaryLight = Color(0xFFFFFFFF);
const afOnPrimaryDark = Color(0xFF0B1410);
const afBackgroundLight = Color(0xFFFAFAF7);
const afSurfaceLight = Color(0xFFFFFFFF);
const afBackgroundDark = Color(0xFF101412);
const afSurfaceDark = Color(0xFF1A201C);

/// Màu nhấn mặc định của app tiếng Trung (đỏ son) — app khác truyền `accent` riêng.
const afAccentChinese = Color(0xFFC62828);

/// Bo góc chung (web: `shape.borderRadius = 10`).
const afRadius = 10.0;

/// Chiều cao tối thiểu nút/vùng chạm (mobile-first).
const afMinTapHeight = 48.0;

/// Làm sáng một màu ~35% để dùng ở chế độ tối (port `lighten` của web).
Color lightenForDark(Color c) {
  int mix(int v) => (v + (255 - v) * 0.35).round();
  return Color.fromARGB(0xFF, mix((c.r * 255).round()), mix((c.g * 255).round()), mix((c.b * 255).round()));
}

/// Dựng theme Material 3 dùng chung cho mọi app AntFarm. Mỗi app truyền [accent] riêng của ngôn ngữ mình
/// (tiếng Trung `#C62828`), [accentDark] mặc định làm sáng từ [accent].
ThemeData buildAfTheme({required Brightness brightness, Color accent = afAccentChinese, Color? accentDark}) {
  final isDark = brightness == Brightness.dark;
  final primary = isDark ? afPrimaryDark : afPrimaryLight;
  final onPrimary = isDark ? afOnPrimaryDark : afOnPrimaryLight;
  final secondary = isDark ? (accentDark ?? lightenForDark(accent)) : accent;
  final onSecondary = isDark ? const Color(0xFF1A0A0A) : const Color(0xFFFFFFFF);
  final surface = isDark ? afSurfaceDark : afSurfaceLight;
  final background = isDark ? afBackgroundDark : afBackgroundLight;

  // fromSeed cho bộ màu tông phụ hài hoà, rồi ép đúng các màu chốt trong hợp đồng (§5.3.7).
  final scheme = ColorScheme.fromSeed(seedColor: primary, brightness: brightness).copyWith(
    primary: primary,
    onPrimary: onPrimary,
    secondary: secondary,
    onSecondary: onSecondary,
    surface: surface,
    // surfaceContainerLowest dùng cho nền Card/Dialog để tách nhẹ khỏi nền scaffold.
    surfaceContainerLowest: surface,
  );

  final radius = BorderRadius.circular(afRadius);
  final outline = scheme.outlineVariant;

  return ThemeData(
    useMaterial3: true,
    brightness: brightness,
    colorScheme: scheme,
    scaffoldBackgroundColor: background,
    visualDensity: VisualDensity.standard,
    // Không đóng gói phông riêng — phông hệ thống + fallback CJK ở HanziText (DB-M19).
    appBarTheme: AppBarTheme(
      backgroundColor: background,
      foregroundColor: scheme.onSurface,
      elevation: 0,
      scrolledUnderElevation: 0.5,
      centerTitle: false,
    ),
    cardTheme: CardThemeData(
      color: surface,
      elevation: 0,
      margin: EdgeInsets.zero,
      shape: RoundedRectangleBorder(
        borderRadius: radius,
        side: BorderSide(color: outline),
      ),
    ),
    dialogTheme: DialogThemeData(
      backgroundColor: surface,
      shape: RoundedRectangleBorder(borderRadius: radius),
    ),
    bottomSheetTheme: BottomSheetThemeData(
      backgroundColor: surface,
      showDragHandle: true,
      shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(16))),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        minimumSize: const Size(afMinTapHeight, afMinTapHeight),
        shape: RoundedRectangleBorder(borderRadius: radius),
      ),
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        elevation: 0,
        minimumSize: const Size(afMinTapHeight, afMinTapHeight),
        shape: RoundedRectangleBorder(borderRadius: radius),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        minimumSize: const Size(afMinTapHeight, afMinTapHeight),
        shape: RoundedRectangleBorder(borderRadius: radius),
      ),
    ),
    textButtonTheme: TextButtonThemeData(
      style: TextButton.styleFrom(
        minimumSize: const Size(afMinTapHeight, afMinTapHeight),
        shape: RoundedRectangleBorder(borderRadius: radius),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      border: OutlineInputBorder(borderRadius: radius),
      filled: true,
      fillColor: surface,
    ),
    chipTheme: ChipThemeData(shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8))),
    navigationBarTheme: NavigationBarThemeData(
      backgroundColor: surface,
      indicatorColor: scheme.primaryContainer,
      labelTextStyle: WidgetStateProperty.all(const TextStyle(fontSize: 12, fontWeight: FontWeight.w600)),
    ),
    snackBarTheme: SnackBarThemeData(
      behavior: SnackBarBehavior.floating,
      shape: RoundedRectangleBorder(borderRadius: radius),
    ),
    dividerTheme: DividerThemeData(color: outline, space: 1, thickness: 1),
  );
}
