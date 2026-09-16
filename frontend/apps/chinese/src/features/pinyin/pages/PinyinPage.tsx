import { useState } from 'react'
import { Box, IconButton, Tab, Tabs, Tooltip, useMediaQuery, useTheme } from '@mui/material'
import SettingsVoiceOutlinedIcon from '@mui/icons-material/SettingsVoiceOutlined'
import { PageContainer, useTabParam } from '@af/ui'
import { ChineseSpeechProvider, useChineseSpeech } from '@/components/speech/ChineseSpeech'
import { VoiceMissingAlert } from '../components/VoiceMissingAlert'
import { VoiceSettings } from '../components/VoiceSettings'
import { GuideView } from '../components/GuideView'
import { PinyinChart } from '../components/PinyinChart'
import { DrillTab } from '../components/drill/DrillTab'

const TABS = ['huong-dan', 'bang', 'luyen'] as const
type TabKey = (typeof TABS)[number]

function PinyinPageInner() {
  const theme = useTheme()
  const isXs = useMediaQuery(theme.breakpoints.down('sm'), { noSsr: true })
  // D33: mặc định Hướng dẫn — người số 0 cần đọc trước khi bấm bảng.
  const [tab, setTab] = useTabParam<TabKey>(TABS, 'huong-dan')
  const [settingsOpen, setSettingsOpen] = useState(false)
  const { status } = useChineseSpeech()

  return (
    <PageContainer
      title="Pinyin & thanh điệu"
      actions={
        <Tooltip title="Cài đặt giọng đọc">
          <IconButton aria-label="Cài đặt giọng đọc" onClick={() => setSettingsOpen(true)}>
            <SettingsVoiceOutlinedIcon />
          </IconButton>
        </Tooltip>
      }
    >
      <Box sx={{ mb: 2 }}>
        <VoiceMissingAlert status={status} />
      </Box>

      <Tabs
        value={tab}
        onChange={(_e, v: TabKey) => setTab(v)}
        variant={isXs ? 'fullWidth' : 'standard'}
        sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
        aria-label="Mục pinyin"
      >
        <Tab value="huong-dan" label="Hướng dẫn" />
        <Tab value="bang" label="Bảng" />
        <Tab value="luyen" label="Luyện" />
      </Tabs>

      {tab === 'huong-dan' && <GuideView onGoToChart={() => setTab('bang')} />}
      {tab === 'bang' && <PinyinChart />}
      {tab === 'luyen' && <DrillTab />}

      <VoiceSettings open={settingsOpen} onClose={() => setSettingsOpen(false)} />
    </PageContainer>
  )
}

/** `/pinyin?tab=huong-dan|bang|luyen` (F5, cần `study.use`) — một `useSpeech('zh')` dùng chung cho cả trang. */
export function PinyinPage() {
  return (
    <ChineseSpeechProvider>
      <PinyinPageInner />
    </ChineseSpeechProvider>
  )
}
