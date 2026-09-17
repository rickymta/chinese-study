// content/chinese/scripts/lib/cedict.mjs
//
// Đọc content/chinese/.raw/cedict_ts.u8 (CC-CEDICT, KHÔNG commit — xem SOURCES.md) để hỗ trợ validate.mjs
// kiểm tra chữ minh hoạ đơn âm (R5-3). Không phân phối lại nội dung CEDICT, chỉ dùng cục bộ lúc kiểm tra.
//
// Trả về Map<chữ Hán giản thể 1 ký tự, Set<'âm tiết'+'thanh'>> dựng từ các MỤC ĐƠN KÝ TỰ, ĐƠN ÂM TIẾT
// trong CEDICT — đây là "mọi mục của chữ đó" theo nghĩa R5-3: một chữ có thể có nhiều mục (nhiều cách đọc),
// ta gộp lại thành 1 tập hợp cách đọc cho chữ đó.

import fs from 'node:fs';

const LINE_RE = /^(\S+)\s+(\S+)\s+\[([^\]]+)\]\s*\/(.*)\/\s*$/;

/**
 * @param {string} rawPath đường dẫn tới file cedict_ts.u8
 * @returns {Map<string, Set<string>> | null} null nếu không tìm thấy file
 */
export function loadCedictSingleCharReadings(rawPath) {
  if (!fs.existsSync(rawPath)) return null;
  const content = fs.readFileSync(rawPath, 'utf8');
  const map = new Map();
  for (const line of content.split('\n')) {
    if (!line || line.startsWith('#')) continue;
    const m = line.match(LINE_RE);
    if (!m) continue;
    const [, , simplified] = m;
    const pinyinRaw = m[3];
    const chars = [...simplified];
    const toks = pinyinRaw.trim().split(/\s+/);
    if (chars.length !== 1 || toks.length !== 1) continue; // chỉ mục đơn ký tự, đơn âm tiết
    const reading = normalizeCedictToken(toks[0]);
    if (!reading) continue;
    const hz = chars[0];
    if (!map.has(hz)) map.set(hz, new Set());
    map.get(hz).add(reading);
  }
  return map;
}

/** 'Zhong1' | 'lu:4' | 'de5' → 'zhong1' | 'lv4' | 'de5'; token không hợp lệ → null */
export function normalizeCedictToken(token) {
  const t = token.toLowerCase().replace(/u:/g, 'v');
  const m = t.match(/^([a-z]+)([1-5])$/);
  if (!m) return null;
  return `${m[1]}${m[2]}`;
}

// ---------------------------------------------------------------------------
// MỞ RỘNG F6.1: đọc TOÀN BỘ mục (đa âm tiết) của một file định dạng CEDICT (CC-CEDICT hoặc CVDICT)
// để build-hsk.mjs tra nghĩa tiếng Việt (CVDICT) theo (giản thể, pinyin). Không đổi hành vi của
// loadCedictSingleCharReadings (F5) ở trên — hàm dưới đây là bổ sung, dùng LINE_RE chung.
// ---------------------------------------------------------------------------

/**
 * @typedef {Object} CedictEntry
 * @property {string} traditional
 * @property {string} simplified
 * @property {string} pinyinRaw   chuỗi pinyin gốc trong dấu ngoặc vuông, vd 'Bei3 jing1', 'nu:3'
 * @property {string} pinyinKey   pinyinRaw đã chuẩn hoá 'u:'/'ü' → 'v', GIỮ NGUYÊN hoa/thường, cách nhau 1 dấu cách
 * @property {string[]} definitions  tách theo '/', đã trim, bỏ phần tử rỗng
 */

/**
 * Đọc toàn bộ entry của một file CEDICT/CVDICT, nhóm theo `simplified`.
 * @param {string} rawPath
 * @returns {Map<string, CedictEntry[]> | null} null nếu không tìm thấy file
 */
export function loadCedictEntries(rawPath) {
  if (!fs.existsSync(rawPath)) return null;
  const content = fs.readFileSync(rawPath, 'utf8');
  const bySimplified = new Map();
  for (const line of content.split('\n')) {
    if (!line || line.startsWith('#') || line.startsWith('%')) continue;
    const m = line.match(LINE_RE);
    if (!m) continue;
    const traditional = m[1];
    const simplified = m[2];
    const pinyinRaw = m[3];
    const defsRaw = m[4];
    const pinyinKey = pinyinRaw
      .split(/\s+/)
      .map((tok) => tok.replace(/u:/g, 'v').replace(/ü/g, 'v'))
      .join(' ');
    const definitions = defsRaw
      .split('/')
      .map((d) => d.trim())
      .filter((d) => d.length > 0);
    const entry = { traditional, simplified, pinyinRaw, pinyinKey, definitions };
    if (!bySimplified.has(simplified)) bySimplified.set(simplified, []);
    bySimplified.get(simplified).push(entry);
  }
  return bySimplified;
}
