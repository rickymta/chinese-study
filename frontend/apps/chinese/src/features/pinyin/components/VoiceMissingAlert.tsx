import { useMemo, useState, type ReactNode } from 'react'
import { Alert, AlertTitle, Box, Button, Collapse, Typography } from '@mui/material'
import { LangText, type SpeechStatus } from '@af/ui'

type OsKey = 'windows' | 'macos' | 'ios' | 'android' | 'linux' | 'other'

const GUIDES: { key: OsKey; title: string; steps: ReactNode }[] = [
  {
    key: 'windows',
    title: 'Windows 10/11',
    steps: (
      <>
        Cài đặt → Thời gian và ngôn ngữ → Ngôn ngữ và vùng → Thêm ngôn ngữ → chọn “<LangText lang="zh-CN">中文(中华人民共和国)</LangText>”
        → tích “Chuyển văn bản thành giọng nói” → cài xong thì khởi động lại trình duyệt.
      </>
    ),
  },
  {
    key: 'macos',
    title: 'macOS',
    steps:
      'Cài đặt hệ thống → Trợ năng → Nội dung được đọc → Giọng hệ thống → Quản lý giọng → Tiếng Trung (Trung Quốc đại lục), ví dụ giọng Tingting → tải về rồi mở lại trình duyệt.',
  },
  {
    key: 'ios',
    title: 'iPhone / iPad',
    steps: 'Cài đặt → Trợ năng → Nội dung được đọc → Giọng nói → Tiếng Trung → tải một giọng (ví dụ Tingting).',
  },
  {
    key: 'android',
    title: 'Android',
    steps:
      'Cài đặt → Hệ thống → Ngôn ngữ → Đầu ra chuyển văn bản sang lời nói → Dịch vụ của Google → Cài đặt dữ liệu giọng nói → Tiếng Trung (Trung Quốc).',
  },
  {
    key: 'linux',
    title: 'Linux',
    steps: 'Firefox trên Linux thường không có giọng tiếng Trung — hãy dùng Chrome/Chromium hoặc Microsoft Edge.',
  },
]

function detectOs(ua: string): OsKey {
  if (/iPhone|iPad|iPod/i.test(ua)) return 'ios'
  if (/Android/i.test(ua)) return 'android'
  if (/Windows/i.test(ua)) return 'windows'
  if (/Mac OS X|Macintosh/i.test(ua)) return 'macos'
  if (/Linux/i.test(ua)) return 'linux'
  return 'other'
}

/**
 * Dải cảnh báo khi máy chưa có giọng tiếng Trung (`no-voice`) hoặc trình duyệt không hỗ trợ TTS (`unsupported`).
 * Hướng dẫn rút gọn theo hệ điều hành đoán từ `navigator.userAgent`; nút mở rộng cho hệ điều hành khác (§5.3.D).
 */
export function VoiceMissingAlert({ status }: { status: SpeechStatus }) {
  const [expanded, setExpanded] = useState(false)
  const os = useMemo(() => detectOs(typeof navigator === 'undefined' ? '' : navigator.userAgent), [])
  if (status !== 'unsupported' && status !== 'no-voice') return null

  if (status === 'unsupported') {
    return (
      <Alert severity="warning">
        <AlertTitle>Trình duyệt này không hỗ trợ đọc văn bản</AlertTitle>
        Hãy dùng Chrome, Edge hoặc Safari bản mới để nghe phát âm.
      </Alert>
    )
  }

  const primary = GUIDES.find((g) => g.key === os)
  const others = GUIDES.filter((g) => g.key !== os)

  return (
    <Alert
      severity="warning"
      action={
        <Button color="inherit" size="small" onClick={() => setExpanded((v) => !v)}>
          {expanded ? 'Thu gọn' : 'Hệ điều hành khác'}
        </Button>
      }
    >
      <AlertTitle>Máy chưa có giọng đọc tiếng Trung</AlertTitle>
      <Typography variant="body2">
        Các nút nghe sẽ bị vô hiệu cho tới khi bạn cài giọng “Tiếng Trung (Trung Quốc)” cho hệ điều hành rồi mở lại
        trình duyệt.
      </Typography>
      {primary && (
        <Typography variant="body2" sx={{ mt: 1 }}>
          <strong>{primary.title}:</strong> {primary.steps}
        </Typography>
      )}
      <Typography variant="body2" sx={{ mt: 1 }}>
        <strong>Mẹo:</strong> Microsoft Edge có sẵn giọng tiếng Trung trực tuyến chất lượng cao, không cần cài thêm.
      </Typography>
      <Collapse in={expanded}>
        <Box sx={{ mt: 1, display: 'flex', flexDirection: 'column', gap: 0.75 }}>
          {others.map((g) => (
            <Typography key={g.key} variant="body2">
              <strong>{g.title}:</strong> {g.steps}
            </Typography>
          ))}
        </Box>
      </Collapse>
    </Alert>
  )
}
