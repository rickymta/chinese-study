import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router-dom'
import { ThemeProvider } from '@af/ui'
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

// Thứ tự provider theo hợp đồng §5.3.0.1: ThemeProvider → QueryClientProvider → RouterProvider.
// F2 chèn AuthProvider (của @af/auth) giữa QueryClientProvider và RouterProvider.
export function App() {
  return (
    <ThemeProvider accent={CHINESE_ACCENT}>
      <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
      </QueryClientProvider>
    </ThemeProvider>
  )
}
