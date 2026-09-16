// content/chinese/scripts/lib/pinyin.mjs
//
// Tiện ích chuẩn hoá pinyin dùng chung cho build-hsk.mjs (F6.1). Nguồn sự thật của dự án là pinyin
// SỐ THANH (vd 'ni3 hao3'), 'ü' viết 'v' — xem CLAUDE.md mục "Quy ước tiếng Trung".

/**
 * Chuẩn hoá MỘT âm tiết đã có số thanh (từ complete.json `numeric` hoặc CVDICT `[pinyin]`) về dạng
 * số thanh thống nhất: 'ü' hoặc 'u:' → 'v', giữ nguyên hoa/thường (proper noun), thiếu số thanh ⇒ null
 * (gọi nơi khác quyết định WARN + gán '5').
 * @param {string} token vd 'ai4', 'nü3', 'nu:3', 'Bei3', 'ma'
 * @returns {{ letters: string, tone: string | null } | null} null nếu không phải dạng chữ cái [+ số thanh]
 */
export function splitToneToken(token) {
  const t = token.replace(/u:/gi, (m) => (m[0] === 'U' ? 'V' : 'v')).replace(/[üÜ]/g, (m) => (m === 'Ü' ? 'V' : 'v'));
  const m = t.match(/^([A-Za-z]+)([1-5])?$/);
  if (!m) return null;
  return { letters: m[1], tone: m[2] ?? null };
}

/**
 * Chuẩn hoá một âm tiết SỐ THANH đầy đủ (bắt buộc phải có số thanh). Thiếu số thanh ⇒ null.
 * @param {string} token
 * @returns {string | null}
 */
export function normalizeNumberedToken(token) {
  const s = splitToneToken(token);
  if (!s || !s.tone) return null;
  return `${s.letters}${s.tone}`;
}

const TONE_MARK_MAP = {
  ā: ['a', '1'], á: ['a', '2'], ǎ: ['a', '3'], à: ['a', '4'],
  ē: ['e', '1'], é: ['e', '2'], ě: ['e', '3'], è: ['e', '4'],
  ī: ['i', '1'], í: ['i', '2'], ǐ: ['i', '3'], ì: ['i', '4'],
  ō: ['o', '1'], ó: ['o', '2'], ǒ: ['o', '3'], ò: ['o', '4'],
  ū: ['u', '1'], ú: ['u', '2'], ǔ: ['u', '3'], ù: ['u', '4'],
  ǖ: ['v', '1'], ǘ: ['v', '2'], ǚ: ['v', '3'], ǜ: ['v', '4'],
};

/**
 * Chuyển MỘT âm tiết pinyin dạng dấu (vd 'hǎo', 'dì', 'lǜ', 'de' không dấu = thanh nhẹ) sang số thanh,
 * chữ thường, 'ü' → 'v'. Dùng cho Unihan `kMandarin`. Không có dấu ⇒ thanh nhẹ '5'.
 * @param {string} syllable
 * @returns {string}
 */
export function toneMarkedSyllableToNumbered(syllable) {
  let base = '';
  let tone = null;
  for (const ch of syllable.toLowerCase()) {
    const hit = TONE_MARK_MAP[ch];
    if (hit) {
      base += hit[0];
      tone = hit[1];
    } else if (ch === 'ü') {
      base += 'v';
    } else {
      base += ch;
    }
  }
  return `${base}${tone ?? '5'}`;
}

/**
 * Ghép danh sách âm tiết số thanh (mỗi phần tử đã có số) thành chuỗi pinyin cách nhau 1 dấu cách —
 * dạng lưu ở `pinyin` của hsk-words.json (giữ nguyên hoa/thường).
 * @param {string[]} tokens
 */
export function joinNumbered(tokens) {
  return tokens.join(' ');
}
