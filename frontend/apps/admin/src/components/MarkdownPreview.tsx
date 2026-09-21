import { Box, type SxProps, type Theme } from '@mui/material'
import ReactMarkdown, { type Components } from 'react-markdown'
import remarkGfm from 'remark-gfm'

export interface MarkdownPreviewProps {
  /** Nguồn Markdown (GFM). */
  source: string
  sx?: SxProps<Theme>
}

const isExternal = (href: string | undefined): boolean => !!href && /^https?:\/\//i.test(href)

/**
 * Thành phần render dùng chung: link ngoài mở tab mới + `rel="noopener noreferrer"`; ảnh không tràn khung.
 * KHÔNG can thiệp `href` — `react-markdown` đã dùng `urlTransform` mặc định (`defaultUrlTransform`) loại
 * `javascript:`/`vbscript:`/`file:`/`data:` (trừ `data:image` không có — mặc định chỉ giữ http/https/mailto/irc/ircs/xmpp).
 */
const components: Components = {
  a: ({ node: _node, href, children, ...rest }) =>
    isExternal(href) ? (
      <a href={href} target="_blank" rel="noopener noreferrer" {...rest}>
        {children}
      </a>
    ) : (
      <a href={href} {...rest}>
        {children}
      </a>
    ),
  img: ({ node: _node, alt, ...rest }) => <img alt={alt ?? ''} loading="lazy" {...rest} />,
}

/**
 * Xem trước Markdown an toàn (hợp đồng W3b §5.3.3a): `react-markdown` + `remark-gfm` (bảng, gạch ngang, checklist),
 * `skipHtml` bỏ mọi HTML thô, **không** `rehype-raw` ⇒ `<script>`/`<iframe>` dán vào bị bỏ hẳn, không thành DOM.
 * Website (W7) render cùng cấu hình để admin xem trước đúng với thứ hiển thị.
 */
export function MarkdownPreview({ source, sx }: MarkdownPreviewProps) {
  return (
    <Box
      className="af-markdown"
      sx={[
        {
          wordBreak: 'break-word',
          '& img': { maxWidth: '100%', height: 'auto' },
          '& table': { borderCollapse: 'collapse', width: '100%', display: 'block', overflowX: 'auto' },
          '& th, & td': { border: 1, borderColor: 'divider', px: 1, py: 0.5, textAlign: 'left' },
          '& pre': { overflowX: 'auto', bgcolor: 'action.hover', p: 1.5, borderRadius: 1 },
          '& code': { fontFamily: 'monospace', fontSize: '0.9em' },
          '& blockquote': { borderLeft: 3, borderColor: 'divider', ml: 0, pl: 2, color: 'text.secondary' },
          '& > :first-of-type': { mt: 0 },
          '& > :last-child': { mb: 0 },
        },
        ...(Array.isArray(sx) ? sx : [sx]),
      ]}
    >
      <ReactMarkdown remarkPlugins={[remarkGfm]} skipHtml components={components}>
        {source}
      </ReactMarkdown>
    </Box>
  )
}
