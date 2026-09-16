import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'

// Trình duyệt chỉ nói chuyện với gateway (R-N4). Dev không có subdomain id.* nên identity cũng đi cùng origin
// qua /identity (R-N10); production identity nằm ở https://id.antfarms.xyz (VITE_IDENTITY_API_URL), còn
// /chinese vẫn cùng origin (nginx biên tách /chinese/ → gateway).
const GATEWAY = 'http://localhost:5280'

// ⚠️ Route SPA của app KHÔNG được bắt đầu bằng /identity hoặc /chinese — sẽ bị proxy nuốt. Quy ước route tiếng
// Việt không dấu (§5.0.3: /dang-nhap, /tu-dien, /on-tap...) đã tránh được.
export default defineConfig({
  plugins: [react()],
  // import.meta.dirname (Node ≥ 20.11) thay cho __dirname: Vite 8 cảnh báo __dirname không chạy với configLoader
  // 'native' (sẽ là mặc định ở bản lớn kế tiếp).
  resolve: { alias: { '@': path.resolve(import.meta.dirname, 'src') } },
  server: {
    port: 3280,
    strictPort: true,
    proxy: {
      '/identity': { target: GATEWAY, changeOrigin: false },
      '/chinese': { target: GATEWAY, changeOrigin: false },
    },
  },
})
