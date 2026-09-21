import { describe, expect, it } from 'vitest'
import { renderToStaticMarkup } from 'react-dom/server'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'

// Kiểm cấu hình cốt lõi (skipHtml + urlTransform mặc định + remark-gfm) — render tĩnh trong môi trường node, không
// cần jsdom. `MarkdownPreview` bọc MUI `Box` (cần theme/emotion) nên test dùng đúng cấu hình ReactMarkdown mà nó
// truyền; components tuỳ biến (a/img) không thay đổi `href` nên không ảnh hưởng kết luận bảo mật.
const render = (source: string) => renderToStaticMarkup(
  <ReactMarkdown remarkPlugins={[remarkGfm]} skipHtml>
    {source}
  </ReactMarkdown>,
)

describe('MarkdownPreview — an toàn', () => {
  it('bỏ hẳn <script> dán vào (skipHtml, không rehype-raw)', () => {
    const html = render('Xin chào <script>alert(1)</script> bạn\n\n<iframe src="https://x"></iframe>')
    // Thẻ bị bỏ; phần chữ "alert(1)" còn lại chỉ là văn bản thuần (đã escape), không phải phần tử script.
    expect(html).not.toMatch(/<script/i)
    expect(html).not.toMatch(/<iframe/i)
    expect(html).toContain('Xin chào')
  })

  it('không cho link javascript: có href', () => {
    const html = render('[bấm](javascript:alert(1))')
    expect(html).not.toMatch(/href="javascript:/i)
    expect(html).not.toContain('alert(1)')
    expect(html).toContain('bấm')
  })

  it('giữ link https bình thường', () => {
    const html = render('[AntFarm](https://antfarms.xyz)')
    expect(html).toContain('href="https://antfarms.xyz"')
  })

  it('render bảng GFM', () => {
    const html = render('| A | B |\n| --- | --- |\n| 1 | 2 |')
    expect(html).toContain('<table')
    expect(html).toContain('<td>1</td>')
  })
})
