import { defineConfig } from 'vitest/config'

// Test hàm thuần của kit (pinyin, inlineZh) — môi trường node, không jsdom/testing-library (cùng khuôn apps/chinese).
export default defineConfig({
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
})
