import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { ThemeProvider as MuiThemeProvider, type ThemeOptions } from '@mui/material/styles'
import CssBaseline from '@mui/material/CssBaseline'
import { buildTheme, type ThemeMode } from './buildTheme'

/** Khoá localStorage lưu chế độ sáng/tối — dùng chung mọi app AntFarm (hợp đồng §5.3.0.1). */
const STORAGE_KEY = 'af.themeMode'

interface ThemeContextValue {
  mode: ThemeMode
  toggleMode: () => void
  setMode: (mode: ThemeMode) => void
}

const ThemeContext = createContext<ThemeContextValue | null>(null)

export interface ThemeProviderProps {
  children: ReactNode
  /** Màu nhấn riêng của app ngôn ngữ (xem `buildTheme`). */
  accent?: string
  accentDark?: string
  /** Dùng khi localStorage chưa có lựa chọn. Mặc định theo `prefers-color-scheme` của hệ thống. */
  defaultMode?: ThemeMode
  themeOptions?: (mode: ThemeMode) => ThemeOptions
}

// localStorage có thể bị chặn (Safari chế độ riêng tư, chính sách doanh nghiệp) ⇒ mọi thao tác bọc try/catch,
// hỏng thì coi như không có lựa chọn đã lưu — không được làm chết app chỉ vì không ghi được theme.
function readStoredMode(): ThemeMode | null {
  try {
    const v = localStorage.getItem(STORAGE_KEY)
    return v === 'light' || v === 'dark' ? v : null
  } catch {
    return null
  }
}

function writeStoredMode(mode: ThemeMode): void {
  try {
    localStorage.setItem(STORAGE_KEY, mode)
  } catch {
    /* bỏ qua — xem ghi chú ở readStoredMode */
  }
}

function systemPrefersDark(): boolean {
  try {
    return typeof window !== 'undefined' && window.matchMedia('(prefers-color-scheme: dark)').matches
  } catch {
    return false
  }
}

export function ThemeProvider({ children, accent, accentDark, defaultMode, themeOptions }: ThemeProviderProps) {
  const [mode, setModeState] = useState<ThemeMode>(
    () => readStoredMode() ?? defaultMode ?? (systemPrefersDark() ? 'dark' : 'light'),
  )

  const setMode = useCallback((next: ThemeMode) => {
    writeStoredMode(next)
    setModeState(next)
  }, [])

  const toggleMode = useCallback(() => {
    setModeState((prev) => {
      const next = prev === 'dark' ? 'light' : 'dark'
      writeStoredMode(next)
      return next
    })
  }, [])

  const theme = useMemo(
    () => buildTheme({ mode, accent, accentDark, extra: themeOptions?.(mode) }),
    [mode, accent, accentDark, themeOptions],
  )

  const ctx = useMemo(() => ({ mode, toggleMode, setMode }), [mode, toggleMode, setMode])

  return (
    <ThemeContext.Provider value={ctx}>
      <MuiThemeProvider theme={theme}>
        {/* enableColorScheme: control NATIVE (select, ô ngày, thanh cuộn) cũng đổi theo chế độ tối */}
        <CssBaseline enableColorScheme />
        {children}
      </MuiThemeProvider>
    </ThemeContext.Provider>
  )
}

export function useThemeMode(): ThemeContextValue {
  const ctx = useContext(ThemeContext)
  if (!ctx) throw new Error('useThemeMode phải được dùng bên trong <ThemeProvider> của @af/ui')
  return ctx
}
