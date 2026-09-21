import type { ReactElement } from 'react'
import { Alert, AlertTitle } from '@mui/material'
import RecordVoiceOverOutlinedIcon from '@mui/icons-material/RecordVoiceOverOutlined'
import PublicOutlinedIcon from '@mui/icons-material/PublicOutlined'
import PsychologyOutlinedIcon from '@mui/icons-material/PsychologyOutlined'
import MenuBookOutlinedIcon from '@mui/icons-material/MenuBookOutlined'
import LightbulbOutlinedIcon from '@mui/icons-material/LightbulbOutlined'
import type { TipBlockPayload, TipVariant } from '../types'
import { InlineZh } from '../InlineZh'

const VARIANT_META: Record<TipVariant, { title: string; icon: ReactElement }> = {
  pronunciation: { title: 'Mẹo phát âm', icon: <RecordVoiceOverOutlinedIcon fontSize="inherit" /> },
  culture: { title: 'Văn hoá', icon: <PublicOutlinedIcon fontSize="inherit" /> },
  memory: { title: 'Mẹo ghi nhớ', icon: <PsychologyOutlinedIcon fontSize="inherit" /> },
  grammar: { title: 'Lưu ý ngữ pháp', icon: <MenuBookOutlinedIcon fontSize="inherit" /> },
}

/** Khối mẹo: `Alert` info với tiêu đề/biểu tượng theo `variant` (không có ⇒ "Mẹo"). */
export function TipBlock({ payload }: { payload: TipBlockPayload }) {
  const meta = payload.variant ? VARIANT_META[payload.variant] : undefined
  return (
    <Alert severity="info" icon={meta?.icon ?? <LightbulbOutlinedIcon fontSize="inherit" />} sx={{ '& .MuiAlert-message': { minWidth: 0 } }}>
      <AlertTitle sx={{ fontWeight: 700 }}>{meta?.title ?? 'Mẹo'}</AlertTitle>
      <InlineZh text={payload.text} variant="body2" component="div" />
    </Alert>
  )
}
