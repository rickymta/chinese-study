import { describe, expect, it } from 'vitest'
import type { AdminLessonBlock, AdminLessonWord, AdminQuizQuestion } from '../types'
import {
  blockProblems,
  blocksDirty,
  blocksFromLesson,
  blocksToRequest,
  checkPublishable,
  emptyParagraphErrors,
  isKnownBlockPath,
  isKnownQuestionPath,
  metaDirty,
  metaFromLesson,
  metaToRequest,
  moveItem,
  newBlockDraft,
  newQuestionDraft,
  parseObjectives,
  quizDirty,
  quizFromLesson,
  quizToPreview,
  quizToRequest,
  splitBlockPath,
  splitQuestionPath,
  wordsDirty,
  type BlockDraft,
  type QuizQuestionDraft,
} from './lessonDraft'

const dialogueDto: AdminLessonBlock = {
  id: 'b1',
  type: 'dialogue',
  payload: {
    title: null,
    lines: [
      { speaker: 'A', hanzi: '你好！', pinyin: 'ni3 hao3!', vi: 'Chào bạn!' },
      { speaker: 'B', hanzi: '你好。', pinyin: 'ni3 hao3.', vi: 'Chào.' },
    ],
  },
}

const word = (id: string): AdminLessonWord => ({ id, simplified: '你', pinyin: 'ni3', meaningsVi: ['bạn'], meaningViStatus: 'machine' })

const validQuestion = (type: QuizQuestionDraft['type'] = 'single_choice'): QuizQuestionDraft => ({
  ...newQuestionDraft(),
  type,
  prompt: '"Xin chào" tiếng Trung là gì?',
  audioText: type === 'listen_choice' ? '你好' : '',
  options: [
    { text: '你好', lang: 'zh' },
    { text: '谢谢', lang: 'zh' },
    { text: '再见', lang: 'zh' },
  ],
  correctIndex: 0,
})

describe('blocksFromLesson ⇄ blocksToRequest', () => {
  it('vòng tròn giữ nội dung, bỏ trường tuỳ chọn rỗng', () => {
    const drafts = blocksFromLesson([dialogueDto])
    expect(drafts[0]!.localId).toBe('b1')
    const req = blocksToRequest(drafts)
    expect(req).toEqual([
      {
        type: 'dialogue',
        payload: {
          lines: [
            { speaker: 'A', hanzi: '你好！', pinyin: 'ni3 hao3!', vi: 'Chào bạn!' },
            { speaker: 'B', hanzi: '你好。', pinyin: 'ni3 hao3.', vi: 'Chào.' },
          ],
        },
      },
    ])
  })
  it('text: trim nhưng GIỮ vị trí đoạn rỗng (lỗi paragraphs[i] gắn đúng ô); tip: variant rỗng bị bỏ', () => {
    const text: BlockDraft = { localId: 'x', type: 'text', paragraphs: ['  Đoạn 1 ', '', '   ', 'Đoạn 4'] }
    const tip: BlockDraft = { localId: 'y', type: 'tip', variant: '', text: ' Mẹo ' }
    expect(blocksToRequest([text, tip])).toEqual([
      { type: 'text', payload: { paragraphs: ['Đoạn 1', '', '', 'Đoạn 4'] } },
      { type: 'tip', payload: { text: 'Mẹo' } },
    ])
    expect(emptyParagraphErrors([tip, text])).toEqual({
      'blocks[1].payload.paragraphs[1]': ['Đoạn trống — điền nội dung hoặc xoá đoạn.'],
      'blocks[1].payload.paragraphs[2]': ['Đoạn trống — điền nội dung hoặc xoá đoạn.'],
    })
    expect(blockProblems(text)).toEqual(['Đoạn 2 trống — điền nội dung hoặc xoá đoạn.', 'Đoạn 3 trống — điền nội dung hoặc xoá đoạn.'])
  })
  it('khối mới có localId riêng', () => {
    const a = newBlockDraft('dialogue')
    const b = newBlockDraft('dialogue')
    expect(a.localId).not.toBe(b.localId)
    expect(a.type === 'dialogue' && a.lines).toHaveLength(2)
  })
})

describe('blocksDirty', () => {
  it('chỉ khác khoảng trắng ⇒ không dirty; đổi nội dung ⇒ dirty', () => {
    const base = blocksFromLesson([dialogueDto])
    const same = blocksFromLesson([dialogueDto])
    expect(blocksDirty(same, base)).toBe(false)
    const spaced = [{ ...same[0]!, title: '  ' } as BlockDraft]
    expect(blocksDirty(spaced, base)).toBe(false)
    const changed = [{ ...same[0]!, title: 'Chào hỏi' } as BlockDraft]
    expect(blocksDirty(changed, base)).toBe(true)
    expect(blocksDirty([], base)).toBe(true)
  })
})

describe('quiz', () => {
  const dto: AdminQuizQuestion = {
    id: 'q1',
    key: 'q1',
    type: 'listen_choice',
    prompt: 'Nghe và chọn',
    promptLang: 'vi',
    audioText: '谢谢',
    options: [
      { id: 'a', text: 'cảm ơn', lang: 'vi' },
      { id: 'b', text: 'xin lỗi', lang: 'vi' },
    ],
    correctOptionId: 'b',
    explanation: null,
  }
  it('correctOptionId ⇒ correctIndex, request giữ id câu cũ', () => {
    const drafts = quizFromLesson([dto])
    expect(drafts[0]!.correctIndex).toBe(1)
    const req = quizToRequest(drafts)
    expect(req[0]).toEqual({
      id: 'q1',
      type: 'listen_choice',
      prompt: 'Nghe và chọn',
      promptLang: 'vi',
      audioText: '谢谢',
      options: [
        { text: 'cảm ơn', lang: 'vi' },
        { text: 'xin lỗi', lang: 'vi' },
      ],
      correctIndex: 1,
    })
    expect(quizDirty(drafts, quizFromLesson([dto]))).toBe(false)
  })
  it('promptPinyin chỉ gửi khi promptLang=zh; audioText chỉ khi listen', () => {
    const q = { ...validQuestion('single_choice'), promptPinyin: 'ni3 hao3', audioText: '你好' }
    const req = quizToRequest([q])[0]!
    expect(req.promptPinyin).toBeUndefined()
    expect(req.audioText).toBeUndefined()
  })
  it('xem trước gán id lựa chọn a..d', () => {
    const prev = quizToPreview([validQuestion()])[0]!
    expect(prev.options.map((o) => o.id)).toEqual(['a', 'b', 'c'])
    expect(prev.correctOptionId).toBe('a')
  })
})

describe('meta', () => {
  const lesson = {
    title: 'Chào hỏi',
    slug: 'chao-hoi',
    topic: 'giao-tiep',
    orderIndex: 1,
    summary: null,
    objectives: ['Chào', 'Tạm biệt'],
    estimatedMinutes: 15,
    glossary: [],
  }
  it('objectives mỗi dòng một mục; summary rỗng ⇒ null; glossary dòng trống bị bỏ', () => {
    const meta = metaFromLesson(lesson)
    expect(meta.objectivesText).toBe('Chào\nTạm biệt')
    expect(parseObjectives(' a \n\n b ')).toEqual(['a', 'b'])
    const req = metaToRequest({ ...meta, glossary: [{ hanzi: '', pinyin: '', vi: '' }, { hanzi: '安娜', pinyin: 'an1 na4', vi: 'Anna' }] }, 7)
    expect(req.version).toBe(7)
    expect(req.summary).toBe('')
    expect(req.objectives).toEqual(['Chào', 'Tạm biệt'])
    expect(req.glossary).toEqual([{ hanzi: '安娜', pinyin: 'an1 na4', vi: 'Anna' }])
  })
  it('metaDirty', () => {
    const base = metaFromLesson(lesson)
    expect(metaDirty({ ...base, summary: '  ' }, base)).toBe(false)
    expect(metaDirty({ ...base, title: 'Khác' }, base)).toBe(true)
  })
})

describe('wordsDirty', () => {
  it('so theo thứ tự id', () => {
    expect(wordsDirty([word('1'), word('2')], [word('1'), word('2')])).toBe(false)
    expect(wordsDirty([word('2'), word('1')], [word('1'), word('2')])).toBe(true)
  })
})

describe('checkPublishable (R-CA4)', () => {
  const goodBlocks = blocksFromLesson([dialogueDto])
  it('bài đủ điều kiện ⇒ không problem; cảnh báo < 5 câu, < 30% nghe', () => {
    const r = checkPublishable({ title: 'Bài', blocks: goodBlocks, words: [word('1')], quiz: [validQuestion(), validQuestion(), validQuestion()] })
    expect(r.problems).toEqual([])
    expect(r.warnings).toContain('Quiz nên có ≥ 5 câu.')
    expect(r.warnings.some((w) => w.includes('30%'))).toBe(true)
  })
  it('thiếu tiêu đề/khối/từ/câu ⇒ problems', () => {
    const r = checkPublishable({ title: ' ', blocks: [], words: [], quiz: [] })
    expect(r.problems).toHaveLength(4)
    expect(r.warnings).toContain('Bài chưa có khối hội thoại.')
  })
  it('câu nghe thiếu audioText, đáp án ngoài khoảng ⇒ problem', () => {
    const bad = { ...validQuestion('listen_choice'), audioText: '', correctIndex: 5 }
    const r = checkPublishable({ title: 'Bài', blocks: goodBlocks, words: [word('1')], quiz: [bad, validQuestion(), validQuestion()] })
    expect(r.problems.some((p) => p.includes('câu nghe'))).toBe(true)
    expect(r.problems.some((p) => p.includes('đáp án đúng'))).toBe(true)
  })
  it('> 15 từ ⇒ cảnh báo', () => {
    const words = Array.from({ length: 16 }, (_, i) => word(String(i)))
    const r = checkPublishable({ title: 'Bài', blocks: goodBlocks, words, quiz: [validQuestion(), validQuestion(), validQuestion()] })
    expect(r.warnings.some((w) => w.includes('16 từ'))).toBe(true)
  })
})

describe('blockProblems', () => {
  it('pinyin sai số âm tiết', () => {
    const d = blocksFromLesson([dialogueDto])[0] as Extract<BlockDraft, { type: 'dialogue' }>
    const bad: BlockDraft = { ...d, lines: [{ ...d.lines[0]!, pinyin: 'ni3' }, d.lines[1]!] }
    expect(blockProblems(bad)).toEqual(['Dòng 1: Số âm tiết (1) khác số chữ Hán (2).'])
  })
})

describe('đường dẫn lỗi 400', () => {
  it('splitBlockPath / isKnownBlockPath', () => {
    expect(splitBlockPath('blocks[2].payload.lines[0].pinyin')).toEqual({ index: 2, rel: 'lines[0].pinyin' })
    expect(splitBlockPath('blocks[2].type')).toBeNull()
    const d = blocksFromLesson([dialogueDto])[0]!
    expect(isKnownBlockPath(d, 'lines[1].pinyin')).toBe(true)
    expect(isKnownBlockPath(d, 'lines[5].pinyin')).toBe(false)
    expect(isKnownBlockPath(d, 'lines[0].foo')).toBe(false)
    expect(isKnownBlockPath(d, 'title')).toBe(true)
    const t = newBlockDraft('text')
    expect(isKnownBlockPath(t, 'paragraphs[0]')).toBe(true)
    expect(isKnownBlockPath(t, 'paragraphs[3]')).toBe(false)
  })
  it('splitQuestionPath / isKnownQuestionPath', () => {
    expect(splitQuestionPath('questions[1].options[2].text')).toEqual({ index: 1, rel: 'options[2].text' })
    const q = validQuestion()
    expect(isKnownQuestionPath(q, 'options[2].text')).toBe(true)
    expect(isKnownQuestionPath(q, 'options[3].text')).toBe(false)
    expect(isKnownQuestionPath(q, 'prompt')).toBe(true)
    expect(isKnownQuestionPath(q, 'foo')).toBe(false)
  })
})

describe('moveItem', () => {
  it('di chuyển không đổi mảng gốc, chỉ số ngoài khoảng ⇒ bản sao', () => {
    const src = [1, 2, 3]
    expect(moveItem(src, 0, 2)).toEqual([2, 3, 1])
    expect(moveItem(src, 2, 0)).toEqual([3, 1, 2])
    expect(moveItem(src, 0, -1)).toEqual([1, 2, 3])
    expect(src).toEqual([1, 2, 3])
  })
})
