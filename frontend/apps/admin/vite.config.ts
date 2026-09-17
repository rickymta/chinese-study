/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'

// Trình duyệt chỉ nói chuyện với gateway (R-N4). Dev không có subdomain id.* nên identity cũng đi cùng origin qua
// /identity; production identity ở https://id.antfarms.xyz (VITE_IDENTITY_API_URL), còn /cms và /chinese vẫn cùng
// origin trên admin.antfarms.xyz (nginx biên tách /cms/ và /chinese/ → gateway — hợp đồng W2 §5.3.1, R-W7).
const GATEWAY = 'http://localhost:5280'

// ⚠️ Route SPA của admin KHÔNG được bắt đầu bằng /identity, /cms, /chinese hoặc tiền tố ngôn ngữ tương lai
// (/english, /japanese, /vietnamese...) — sẽ bị proxy nuốt. Quy ước: mọi route admin là tiếng Việt không dấu
// (/dang-nhap, /nguoi-dung-cms...) và module ngôn ngữ dùng tiền tố /ngon-ngu/<code>/... (vd /ngon-ngu/chinese/bai-hoc).
// Thêm ngôn ngữ mới = thêm một dòng proxy ở đây + một mục trong src/modules/registry.ts.
export default defineConfig({
  plugins: [react()],
  resolve: { alias: { '@': path.resolve(import.meta.dirname, 'src') } },
  server: {
    port: 3290,
    strictPort: true,
    proxy: {
      '/identity': { target: GATEWAY, changeOrigin: false },
      '/cms': { target: GATEWAY, changeOrigin: false },
      '/chinese': { target: GATEWAY, changeOrigin: false },
    },
  },
  // vitest: chỉ test hàm thuần (gộp quyền nhiều service) — môi trường node, không jsdom/testing-library.
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
})
