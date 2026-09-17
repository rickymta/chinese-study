// @af/ui — thành phần giao diện dùng chung cho MỌI app ngôn ngữ của AntFarm.
// Import thẳng TS source (workspace symlink, không build/dist) — sửa ở đây là vá cho mọi app.

// ─── Theme ───
export { buildTheme, AF_FONT_CJK } from './theme/buildTheme'
export type { BuildThemeOptions, ThemeMode } from './theme/buildTheme'
export { ThemeProvider, useThemeMode } from './theme/ThemeProvider'
export type { ThemeProviderProps } from './theme/ThemeProvider'

// ─── Layout ───
export { AppLayout } from './components/layout/AppLayout'
export type { AppLayoutProps, NavItem } from './components/layout/AppLayout'
export { PageContainer } from './components/layout/PageContainer'
export type { PageContainerProps } from './components/layout/PageContainer'
export { StickyActionBar } from './components/layout/StickyActionBar'
export type { StickyActionBarProps } from './components/layout/StickyActionBar'

// ─── Phản hồi nhanh (F4 — thay window.confirm/alert) ───
export { ConfirmProvider, useConfirm } from './feedback/ConfirmProvider'
export type { ConfirmOptions } from './feedback/ConfirmProvider'
export { ToastProvider, useToast } from './feedback/ToastProvider'
export type { ToastApi } from './feedback/ToastProvider'

// ─── Ô nhập (F4) — AppAutocomplete là mẫu chuẩn cho mọi Autocomplete (trải params.slotProps trước) ───
export { AppAutocomplete } from './inputs/AppAutocomplete'
export type { AppAutocompleteProps } from './inputs/AppAutocomplete'
export { TimeZoneAutocomplete } from './inputs/TimeZoneAutocomplete'
export type { TimeZoneAutocompleteProps } from './inputs/TimeZoneAutocomplete'

// ─── Trang lỗi ───
export { ErrorPage } from './components/errors/ErrorPage'
export type { ErrorPageProps } from './components/errors/ErrorPage'
export { NotFoundPage } from './components/errors/NotFoundPage'

// ─── Văn bản theo ngôn ngữ ───
export { LangText } from './components/text/LangText'
export type { LangTextProps } from './components/text/LangText'

// ─── Hộp thoại / ngăn kéo (F5 — chặn đóng ngoài ý muốn; lint `raw-dialog` bắt buộc dùng thay Dialog/Drawer trần) ───
export { AppDialog } from './components/dialog/AppDialog'
export type { AppDialogProps } from './components/dialog/AppDialog'
export { AppDrawer } from './components/dialog/AppDrawer'
export type { AppDrawerProps } from './components/dialog/AppDrawer'

// ─── Hook ───
export { useTabParam } from './hooks/useTabParam'
// F6: quay lại trang cha có chủ đích + giữ vị trí cuộn danh sách khi mở chi tiết rồi back.
export { useBackTo, linkState, isInternalPath } from './hooks/useBackTo'
export type { BackToState } from './hooks/useBackTo'
export { useScrollRestore } from './hooks/useScrollRestore'

// ─── Đọc văn bản (Web Speech API) — dùng chung mọi ngôn ngữ ───
export { isSpeechSupported, listVoices, pickVoice, speak, cancelSpeech } from './speech/speech'
export type { SpeakOptions } from './speech/speech'
export { useSpeech } from './speech/useSpeech'
export type { SpeechStatus, UseSpeechOptions, UseSpeechResult } from './speech/useSpeech'
