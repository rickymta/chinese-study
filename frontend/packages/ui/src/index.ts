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

// ─── Trang lỗi ───
export { ErrorPage } from './components/errors/ErrorPage'
export type { ErrorPageProps } from './components/errors/ErrorPage'
export { NotFoundPage } from './components/errors/NotFoundPage'

// ─── Văn bản theo ngôn ngữ ───
export { LangText } from './components/text/LangText'
export type { LangTextProps } from './components/text/LangText'
