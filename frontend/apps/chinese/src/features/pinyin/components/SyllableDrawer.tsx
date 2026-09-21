import { useEffect, useRef, useState } from 'react'
import { Alert, Box, Button, Stack, Typography } from '@mui/material'
import PlaylistPlayIcon from '@mui/icons-material/PlaylistPlay'
import StopIcon from '@mui/icons-material/Stop'
import { AppDrawer } from '@af/ui'
import { Hanzi, Pinyin, useChineseSpeech, SpeakButton, displaySyllableKey } from '@af/chinese-kit'
import { TONE_KEYS, type PinyinFinal, type PinyinInitial, type PinyinSyllable } from '../types'

const TONE_LABEL: Record<string, string> = { '1': 'ˉ', '2': 'ˊ', '3': 'ˇ', '4': 'ˋ' }
const SEQUENCE_GAP_MS = 600

export interface SyllableDrawerProps {
  syllable: PinyinSyllable | null
  initial?: PinyinInitial
  final?: PinyinFinal
  onClose: () => void
}

/** Ngăn kéo chi tiết một âm tiết: thanh mẫu/vận mẫu + 4 dòng thanh (chữ minh hoạ, nghĩa, nghe) + "Nghe lần lượt". */
export function SyllableDrawer({ syllable, initial, final, onClose }: SyllableDrawerProps) {
  const { canSpeak, speakZh, cancel } = useChineseSpeech()
  const [playingAll, setPlayingAll] = useState(false)
  const runId = useRef(0)

  // Dừng "Nghe lần lượt" khi đóng drawer, đổi âm tiết, và khi UNMOUNT (rời trang giữa chừng): tăng runId trong
  // cleanup ⇒ vòng lặp `playAll` thấy id lệch và thoát, không gọi speak/setState sau unmount.
  useEffect(() => {
    runId.current++
    setPlayingAll(false)
    cancel()
    return () => {
      runId.current++
      cancel()
    }
  }, [syllable, cancel])

  const stopAll = () => {
    runId.current++
    setPlayingAll(false)
    cancel()
  }

  // Phát các thanh có chữ cách nhau 600 ms. Bắt đầu TRONG onClick (iOS); các lượt sau nối tiếp promise —
  // iOS đã "mở khoá" audio sau lượt đầu trong cùng chuỗi tương tác nên vẫn phát được trên đa số thiết bị.
  const playAll = async () => {
    if (!syllable) return
    const id = ++runId.current
    setPlayingAll(true)
    try {
      for (const k of TONE_KEYS) {
        const ex = syllable.tones[k]
        if (!ex) continue
        if (runId.current !== id) return
        await speakZh(ex.hanzi).catch(() => undefined)
        if (runId.current !== id) return
        await new Promise((r) => setTimeout(r, SEQUENCE_GAP_MS))
      }
    } finally {
      if (runId.current === id) setPlayingAll(false)
    }
  }

  const open = !!syllable
  const hasAny = !!syllable && Object.keys(syllable.tones).length > 0

  return (
    <AppDrawer
      open={open}
      onClose={onClose}
      title={syllable ? displaySyllableKey(syllable.syllable) : ''}
      anchor="responsive"
      // Hộp CHỈ ĐỌC (nghe/xem), không có dữ liệu nhập ⇒ cho bấm ra ngoài/ESC để đóng nhanh khi lướt bảng.
      closeOnBackdrop
      actions={
        hasAny ? (
          playingAll ? (
            <Button variant="outlined" color="inherit" startIcon={<StopIcon />} onClick={stopAll}>
              Dừng
            </Button>
          ) : (
            <Button variant="contained" startIcon={<PlaylistPlayIcon />} onClick={() => void playAll()} disabled={!canSpeak}>
              Nghe lần lượt
            </Button>
          )
        ) : undefined
      }
    >
      {syllable && (
        <Stack spacing={2}>
          <Box>
            <Typography variant="body2">
              <strong>Thanh mẫu:</strong> {initial && initial.code !== '' ? initial.display : 'không có (Ø)'}
              {initial?.ipa ? ` · IPA /${initial.ipa}/` : ''}
              {initial?.aspirated ? ' · bật hơi' : ''}
            </Typography>
            {initial?.noteVi && (
              <Typography variant="body2" color="text.secondary">
                {initial.noteVi}
              </Typography>
            )}
          </Box>
          <Box>
            <Typography variant="body2">
              <strong>Vận mẫu:</strong> {final?.display ?? syllable.final}
              {final?.standaloneSpelling && final.standaloneSpelling !== final.code ? ` · đứng một mình viết "${final.standaloneSpelling}"` : ''}
            </Typography>
            {final?.noteVi && (
              <Typography variant="body2" color="text.secondary">
                {final.noteVi}
              </Typography>
            )}
          </Box>

          {!hasAny && <Alert severity="info">Chưa có chữ minh hoạ đọc đúng cho âm tiết này.</Alert>}

          <Stack divider={<Box sx={{ borderBottom: 1, borderColor: 'divider' }} />}>
            {TONE_KEYS.map((k) => {
              const ex = syllable.tones[k]
              return (
                <Box key={k} sx={{ display: 'flex', alignItems: 'center', gap: 1.5, py: 1, opacity: ex ? 1 : 0.5 }}>
                  <Typography sx={{ width: 40, fontWeight: 700 }} aria-label={`Thanh ${k}`}>
                    {k} {TONE_LABEL[k]}
                  </Typography>
                  <Pinyin value={`${syllable.syllable}${k}`} sx={{ width: 72, fontWeight: 600 }} />
                  {ex ? (
                    <>
                      <Hanzi size="lg" sx={{ minWidth: 44 }}>
                        {ex.hanzi}
                      </Hanzi>
                      <Typography variant="body2" color="text.secondary" sx={{ flex: 1, minWidth: 0 }}>
                        {ex.meaningVi}
                      </Typography>
                      <SpeakButton text={ex.hanzi} />
                    </>
                  ) : (
                    <Typography variant="body2" color="text.disabled" sx={{ flex: 1 }}>
                      không có chữ minh hoạ
                    </Typography>
                  )}
                </Box>
              )
            })}
          </Stack>
        </Stack>
      )}
    </AppDrawer>
  )
}
