import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router-dom'
import { AuthProvider } from '@af/auth'
import { ThemeProvider } from '@af/ui'
import { authSession, identity } from './api/clients'
import { loadMe } from './features/auth/loadMe'
import { router } from './router'

/** Màu nhấn riêng của app tiếng Trung: đỏ son (§5.3.0.5). Màu nền tảng (xanh lục) nằm trong `buildTheme`. */
const CHINESE_ACCENT = '#C62828'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
      staleTime: 30_000,
    },
  },
})

// Thứ tự provider theo hợp đồng §5.3.0.1/§5.3.1: ThemeProvider → QueryClientProvider → AuthProvider → RouterProvider.
// AuthProvider đứng NGOÀI router nên không điều hướng; RequireAuth (trong router) lo chuyển về /dang-nhap.
export function App() {
  return (
    <ThemeProvider accent={CHINESE_ACCENT}>
      <QueryClientProvider client={queryClient}>
        <AuthProvider session={authSession} identity={identity} loadMe={loadMe}>
          <RouterProvider router={router} />
        </AuthProvider>
      </QueryClientProvider>
    </ThemeProvider>
  )
}
