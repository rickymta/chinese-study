import type { ReactElement } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { List, ListItemButton, ListItemIcon, ListItemText, Typography } from '@mui/material'
import ChecklistOutlinedIcon from '@mui/icons-material/ChecklistOutlined'
import StyleOutlinedIcon from '@mui/icons-material/StyleOutlined'
import AddCardOutlinedIcon from '@mui/icons-material/AddCardOutlined'
import AutoStoriesOutlinedIcon from '@mui/icons-material/AutoStoriesOutlined'
import DrawOutlinedIcon from '@mui/icons-material/DrawOutlined'
import GraphicEqOutlinedIcon from '@mui/icons-material/GraphicEqOutlined'
import RecordVoiceOverOutlinedIcon from '@mui/icons-material/RecordVoiceOverOutlined'
import ChevronRightIcon from '@mui/icons-material/ChevronRight'
import type { TodayTask, TodayTaskKind } from '../lib/todayTasks'
import { DashboardCard } from './DashboardCard'

const ICONS: Record<TodayTaskKind, ReactElement> = {
  review: <StyleOutlinedIcon color="primary" />,
  'new-cards': <AddCardOutlinedIcon color="primary" />,
  lesson: <AutoStoriesOutlinedIcon color="primary" />,
  writing: <DrawOutlinedIcon color="primary" />,
  tone: <GraphicEqOutlinedIcon color="primary" />,
  pinyin: <RecordVoiceOverOutlinedIcon color="primary" />,
}

interface Props {
  tasks: TodayTask[]
}

/** "Việc hôm nay" (R-PG9): danh sách bấm được, đúng thứ tự; rỗng ⇒ lời khen ngắn. Mỗi dòng cao ≥ 48px cho ngón tay. */
export function TodayTasks({ tasks }: Props) {
  return (
    <DashboardCard title="Việc hôm nay" icon={<ChecklistOutlinedIcon />}>
      {tasks.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          Không còn việc nào — bạn có thể tra từ điển hoặc luyện viết thêm.
        </Typography>
      ) : (
        <List disablePadding sx={{ mx: -1 }}>
          {tasks.map((t) => (
            <ListItemButton key={t.kind} component={RouterLink} to={t.to} sx={{ borderRadius: 2, minHeight: 48 }}>
              <ListItemIcon sx={{ minWidth: 40 }}>{ICONS[t.kind]}</ListItemIcon>
              <ListItemText
                primary={t.title}
                secondary={t.subtitle}
                slotProps={{ primary: { sx: { fontWeight: 600 } }, secondary: { noWrap: true } }}
              />
              <ChevronRightIcon color="action" />
            </ListItemButton>
          ))}
        </List>
      )}
    </DashboardCard>
  )
}
