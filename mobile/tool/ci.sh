#!/usr/bin/env bash
# Cổng kiểm bắt buộc cho mọi feature đụng mobile/ (hợp đồng mobile §9.1). Chạy: `cd mobile && ./tool/ci.sh`.
# Thứ tự: pub get → format → analyze (fatal-infos) → luật dự án → test từng package/app → build web (kiểm biên dịch).
set -euo pipefail
cd "$(dirname "$0")/.."

echo "▶ flutter pub get (workspace)"
flutter pub get

echo "▶ dart format --set-exit-if-changed"
dart format --output=none --set-exit-if-changed .

echo "▶ flutter analyze --fatal-infos"
flutter analyze --fatal-infos

echo "▶ dart run tool/check_conventions.dart"
dart run tool/check_conventions.dart

# Package chưa tồn tại (vd af_auth trước M2) thì vòng lặp bỏ qua.
for p in packages/af_core packages/af_auth packages/af_ui apps/chinese; do
  if [ -d "$p/test" ]; then
    echo "▶ flutter test ($p)"
    (cd "$p" && flutter test)
  fi
done

echo "▶ flutter build web (apps/chinese, chỉ để kiểm biên dịch — bản web KHÔNG triển khai)"
(cd apps/chinese && flutter build web --release --dart-define-from-file=config/dev-web.json)

echo "✔ ci.sh: mọi cổng đều qua"
