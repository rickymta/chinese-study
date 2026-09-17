import { Link as MuiLink, Typography } from '@mui/material'
import { SOURCES } from '../lib/sources'

/** Thứ tự ghi công cố định theo hợp đồng §5.3.1 — không phụ thuộc `sources[]` của từng từ. */
const ATTRIBUTION_ORDER = ['hsk30-official', 'cc-cedict', 'cvdict', 'unihan', 'han-viet-curated'] as const

/**
 * Dòng chữ nhỏ cuối trang từ điển: ghi công nguồn + liên kết giấy phép (nghĩa vụ CC BY-SA 4.0: ghi công, ghi đã
 * chỉnh sửa, cùng giấy phép). Không dùng `sources[]` của từng từ vì dòng này áp dụng cho cả bộ dữ liệu.
 */
export function SourceAttribution() {
  return (
    <Typography variant="caption" color="text.secondary" component="p" sx={{ mt: 4, lineHeight: 1.6 }}>
      Nguồn:{' '}
      {ATTRIBUTION_ORDER.map((key, i) => {
        const s = SOURCES[key]!
        return (
          <span key={key}>
            {i > 0 && ' · '}
            <MuiLink href={s.url} target="_blank" rel="noopener noreferrer" color="inherit">
              {s.label}
            </MuiLink>{' '}
            (
            <MuiLink href={s.licenseUrl} target="_blank" rel="noopener noreferrer" color="inherit">
              {s.license}
            </MuiLink>
            )
          </span>
        )
      })}
      . Dữ liệu đã được chỉnh sửa.
    </Typography>
  )
}
