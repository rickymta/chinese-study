#!/usr/bin/env node
// content/chinese/scripts/validate.mjs
//
// Kiểm tra học liệu pinyin (F5) trước khi bàn giao. Chạy: `yarn --cwd content validate:chinese`
// (tương đương `node chinese/scripts/validate.mjs` chạy từ thư mục content/).
//
// Quy ước: FAIL ⇒ in lỗi rõ ràng (đường dẫn JSON) và thoát mã 1. WARN ⇒ in cảnh báo, KHÔNG thoát lỗi (exit 0).
// Khung kiểm tra viết chung cho mọi "dataset" pinyin-*; F6 (từ vựng HSK) sẽ thêm dataset mới + schema mới
// và có thể tái dùng loadJson/loadSchema/report bên dưới — xem ghi chú "MỞ RỘNG F6".

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import Ajv2020 from 'ajv/dist/2020.js';
import addFormats from 'ajv-formats';
import { loadCedictSingleCharReadings } from './lib/cedict.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CHINESE_ROOT = path.resolve(__dirname, '..'); // content/chinese
const DATA_DIR = path.join(CHINESE_ROOT, 'data', 'pinyin');
const SCHEMA_DIR = path.join(CHINESE_ROOT, 'schemas');
const SOURCES_PATH = path.join(CHINESE_ROOT, 'SOURCES.md');
const CEDICT_PATH = path.join(CHINESE_ROOT, '.raw', 'cedict_ts.u8');

let failCount = 0;
let warnCount = 0;

function fail(message) {
  failCount++;
  console.error(`✗ FAIL  ${message}`);
}
function warn(message) {
  warnCount++;
  console.warn(`! WARN  ${message}`);
}
function info(message) {
  console.log(`  ${message}`);
}

function loadJson(fileName) {
  const p = path.join(DATA_DIR, fileName);
  if (!fs.existsSync(p)) {
    fail(`Không tìm thấy file ${path.relative(CHINESE_ROOT, p)}`);
    return null;
  }
  try {
    return JSON.parse(fs.readFileSync(p, 'utf8'));
  } catch (err) {
    fail(`${fileName} không phải JSON hợp lệ: ${err.message}`);
    return null;
  }
}

function loadSchema(fileName) {
  const p = path.join(SCHEMA_DIR, fileName);
  return JSON.parse(fs.readFileSync(p, 'utf8'));
}

// ---------------------------------------------------------------------------
// 1. Đọc 4 file + kiểm schema (ajv, allErrors)
// ---------------------------------------------------------------------------

const FILES = {
  initials: { data: 'initials.json', schema: 'pinyin-initials.schema.json' },
  finals: { data: 'finals.json', schema: 'pinyin-finals.schema.json' },
  syllables: { data: 'syllables.json', schema: 'pinyin-syllables.schema.json' },
  guide: { data: 'guide.json', schema: 'pinyin-guide.schema.json' },
};

const ajv = new Ajv2020({ allErrors: true, strict: true });
addFormats(ajv);

const docs = {};
for (const [key, { data, schema }] of Object.entries(FILES)) {
  const doc = loadJson(data);
  docs[key] = doc;
  if (doc === null) continue;
  const validateFn = ajv.compile(loadSchema(schema));
  const ok = validateFn(doc);
  if (!ok) {
    for (const err of validateFn.errors ?? []) {
      const at = err.instancePath ? err.instancePath.replace(/\//g, ' › ') : '(gốc)';
      fail(`${data} › ${at} — ${err.message} (${JSON.stringify(err.params)})`);
    }
  }
}

if (failCount > 0) {
  console.error(`\nDừng kiểm tra sớm: ${failCount} lỗi schema — sửa xong hãy chạy lại.`);
  process.exit(1);
}

const initials = docs.initials.items;
const finals = docs.finals.items;
const syllables = docs.syllables.items;
const guideTopics = docs.guide.items;

info(`Đọc OK: ${initials.length} thanh mẫu, ${finals.length} vận mẫu, ${syllables.length} âm tiết, ${guideTopics.length} chủ đề hướng dẫn.`);

// ---------------------------------------------------------------------------
// 2. initials.code duy nhất, đủ 22 mục (gồm ""); finals.code duy nhất
// ---------------------------------------------------------------------------

{
  const codes = initials.map((i) => i.code);
  const dup = codes.filter((c, idx) => codes.indexOf(c) !== idx);
  if (dup.length) fail(`initials.json › trùng code: ${[...new Set(dup)].join(', ')}`);
  if (initials.length !== 22) fail(`initials.json phải có đúng 22 mục (21 thanh mẫu + ""), hiện có ${initials.length}`);
  if (!codes.includes('')) fail(`initials.json thiếu mục "không thanh mẫu" (code: "")`);
}
{
  const codes = finals.map((f) => f.code);
  const dup = codes.filter((c, idx) => codes.indexOf(c) !== idx);
  if (dup.length) fail(`finals.json › trùng code: ${[...new Set(dup)].join(', ')}`);
}

const initialCodes = new Set(initials.map((i) => i.code));
const finalCodes = new Set(finals.map((f) => f.code));
const finalsByCode = new Map(finals.map((f) => [f.code, f]));

// ---------------------------------------------------------------------------
// 3. syllable duy nhất; khớp R5-1; initial/final tồn tại; tổng số ≥ 380
// ---------------------------------------------------------------------------

const V_ALLOWED_SYLLABLES = new Set(['nv', 'lv', 'nve', 'lve']);

{
  const keys = syllables.map((s) => s.syllable);
  const dup = keys.filter((k, idx) => keys.indexOf(k) !== idx);
  if (dup.length) fail(`syllables.json › trùng syllable: ${[...new Set(dup)].join(', ')}`);

  syllables.forEach((s, idx) => {
    const loc = `syllables.json › items[${idx}] (${s.syllable})`;
    if (/v/.test(s.syllable) && !V_ALLOWED_SYLLABLES.has(s.syllable)) {
      fail(`${loc} — 'v' chỉ được phép ở nv/lv/nve/lve theo R5-1, syllable "${s.syllable}" sai`);
    }
    if (!initialCodes.has(s.initial)) fail(`${loc} — initial "${s.initial}" không có trong initials.json`);
    if (!finalCodes.has(s.final)) fail(`${loc} — final "${s.final}" không có trong finals.json`);
  });

  if (syllables.length < 380) {
    fail(`syllables.json phải có ≥ 380 âm tiết theo R5-2, hiện có ${syllables.length}`);
  }
}

const syllableSet = new Set(syllables.map((s) => s.syllable));
const syllableByKey = new Map(syllables.map((s) => [s.syllable, s]));

// ---------------------------------------------------------------------------
// 4. Ghép initial + final theo quy tắc chính tả ra đúng syllable
// ---------------------------------------------------------------------------

const PALATAL = new Set(['j', 'q', 'x']);

/** Ghép thanh mẫu + vận mẫu thành âm tiết viết ra theo chính tả chuẩn. Trả về null nếu không ghép được. */
function spell(initial, finalCode) {
  if (initial === '') {
    const final = finalsByCode.get(finalCode);
    return final ? final.standaloneSpelling : null; // null hợp lệ = vận mẫu này không thể đứng một mình
  }
  let f = finalCode;
  if (PALATAL.has(initial)) {
    if (f === 'v') f = 'u';
    else if (f === 've') f = 'ue';
    else if (f === 'van') f = 'uan';
    else if (f === 'vn') f = 'un';
  }
  if (f === '-i') f = 'i';
  return initial + f;
}

syllables.forEach((s, idx) => {
  const spelled = spell(s.initial, s.final);
  if (spelled !== s.syllable) {
    fail(`syllables.json › items[${idx}] — ghép "${s.initial}" + "${s.final}" ra "${spelled ?? '(không hợp lệ)'}", khác syllable đã khai "${s.syllable}"`);
  }
});

// ---------------------------------------------------------------------------
// 5. Chữ minh hoạ: 1 ký tự CJK; không thuộc danh sách cấm; không dùng lại cho 2 cặp khác nhau
// ---------------------------------------------------------------------------

const FORBIDDEN_HANZI = new Set('一 不 了 的 着 地 得 和 行 长 重 还 为 都 要 好 啊 吧 呢 吗'.split(' '));
const CJK_SINGLE_RE = /^[一-鿿㐀-䶿]$/u;

const hanziOwner = new Map(); // hanzi -> "syllable+tone" đầu tiên đã dùng

syllables.forEach((s, idx) => {
  for (const tone of Object.keys(s.tones)) {
    const ex = s.tones[tone];
    const loc = `syllables.json › items[${idx}] › tones.${tone} (${s.syllable}${tone})`;
    if (!CJK_SINGLE_RE.test(ex.hanzi)) {
      fail(`${loc} — hanzi "${ex.hanzi}" phải là đúng 1 ký tự CJK`);
      continue;
    }
    if (FORBIDDEN_HANZI.has(ex.hanzi)) {
      fail(`${loc} — hanzi "${ex.hanzi}" thuộc danh sách cấm (có biến điệu/không đơn âm điển hình)`);
    }
    if (hanziOwner.has(ex.hanzi)) {
      fail(`${loc} — hanzi "${ex.hanzi}" đã dùng làm minh hoạ cho ${hanziOwner.get(ex.hanzi)}, không được dùng lại`);
    } else {
      hanziOwner.set(ex.hanzi, `${s.syllable}${tone}`);
    }
  }
});

// ---------------------------------------------------------------------------
// 6 & 6b. Đối chiếu CC-CEDICT (nếu có .raw/)
// ---------------------------------------------------------------------------

const cedict = loadCedictSingleCharReadings(CEDICT_PATH);

if (cedict === null) {
  warn(`Bỏ qua kiểm tra đơn âm — tải CC-CEDICT vào content/chinese/.raw/ (xem SOURCES.md)`);
} else {
  syllables.forEach((s, idx) => {
    for (const tone of Object.keys(s.tones)) {
      const ex = s.tones[tone];
      const loc = `syllables.json › items[${idx}] › tones.${tone} (${s.syllable}${tone})`;
      const expected = `${s.syllable}${tone}`;
      const readings = cedict.get(ex.hanzi);
      if (!readings) {
        fail(`${loc} — chữ "${ex.hanzi}" không có trong CC-CEDICT (mục đơn ký tự)`);
      } else if (readings.size !== 1) {
        fail(`${loc} — chữ "${ex.hanzi}" có ${readings.size} cách đọc trong CC-CEDICT (${[...readings].join(', ')}), không phải đơn âm`);
      } else if (!readings.has(expected)) {
        fail(`${loc} — chữ "${ex.hanzi}" đọc là "${[...readings][0]}" theo CC-CEDICT, khác "${expected}" đã khai`);
      }
    }
  });
}

// ---------------------------------------------------------------------------
// 7. Ví dụ trong initials.examples và guide (examples, compare)
// ---------------------------------------------------------------------------

const PINYIN_TOKEN_RE = /^[A-Za-z]+[1-5]$/;

/** Kiểm 1 cặp {pinyin, hanzi}: trả mảng thông điệp lỗi (rỗng nếu hợp lệ). */
function checkExampleItem(loc, item) {
  const errors = [];
  const tokens = item.pinyin.trim().split(/\s+/);
  if (!tokens.every((t) => PINYIN_TOKEN_RE.test(t))) {
    errors.push(`${loc} — pinyin "${item.pinyin}" không đúng cú pháp (mỗi âm tiết là chữ cái + số thanh 1-5)`);
    return errors;
  }
  const hanziChars = [...item.hanzi];
  if (hanziChars.length !== tokens.length) {
    errors.push(`${loc} — số âm tiết (${tokens.length}) khác số chữ Hán (${hanziChars.length}) trong "${item.hanzi}"`);
    return errors;
  }
  tokens.forEach((tok, i) => {
    const m = tok.toLowerCase().match(/^([a-z]+)([1-5])$/);
    const base = m[1];
    const tone = m[2];
    if (!syllableSet.has(base)) {
      errors.push(`${loc} — âm tiết "${base}" (trong "${tok}") không có trong syllables.json`);
    }
    if (cedict) {
      const hz = hanziChars[i];
      const readings = cedict.get(hz);
      const expected = `${base}${tone}`;
      if (!readings) {
        errors.push(`${loc} — chữ "${hz}" không có trong CC-CEDICT (mục đơn ký tự)`);
      } else if (!readings.has(expected)) {
        errors.push(`${loc} — chữ "${hz}" không có cách đọc "${expected}" trong CC-CEDICT (có: ${[...readings].join(', ')})`);
      } else if (hanziChars.length === 1 && readings.size !== 1) {
        // Ví dụ MỘT chữ phải đơn âm như check #6
        errors.push(`${loc} — chữ "${hz}" có ${readings.size} cách đọc, ví dụ 1 chữ phải đơn âm (đúng 1 cách đọc)`);
      }
    }
  });
  return errors;
}

initials.forEach((ini, idx) => {
  ini.examples.forEach((ex, exIdx) => {
    const loc = `initials.json › items[${idx}] › examples[${exIdx}]`;
    for (const msg of checkExampleItem(loc, ex)) fail(msg);
  });
});

guideTopics.forEach((topic, tIdx) => {
  topic.blocks.forEach((block, bIdx) => {
    const loc = `guide.json › items[${tIdx}] (${topic.id}) › blocks[${bIdx}]`;
    if (block.type === 'examples') {
      block.items.forEach((ex, exIdx) => {
        for (const msg of checkExampleItem(`${loc} › items[${exIdx}]`, ex)) fail(msg);
      });
    } else if (block.type === 'compare') {
      block.pairs.forEach((pair, pIdx) => {
        for (const msg of checkExampleItem(`${loc} › pairs[${pIdx}] › left`, pair.left)) fail(msg);
        for (const msg of checkExampleItem(`${loc} › pairs[${pIdx}] › right`, pair.right)) fail(msg);
      });
    }
  });
});

// ---------------------------------------------------------------------------
// 8. guide có đủ 8 id bắt buộc, order duy nhất
// ---------------------------------------------------------------------------

const REQUIRED_GUIDE_IDS = [
  'bon-thanh', 'dat-dau', 'thanh-mau', 'van-mau',
  'u-hai-cham', 'cap-de-nham', 'bien-dieu', 'cach-luyen',
];

{
  const ids = guideTopics.map((t) => t.id);
  for (const req of REQUIRED_GUIDE_IDS) {
    if (!ids.includes(req)) fail(`guide.json thiếu chủ đề bắt buộc "${req}"`);
  }
  const orders = guideTopics.map((t) => t.order);
  const dupOrders = orders.filter((o, idx) => orders.indexOf(o) !== idx);
  if (dupOrders.length) fail(`guide.json › trùng order: ${[...new Set(dupOrders)].join(', ')}`);
}

// ---------------------------------------------------------------------------
// 9. Độ phủ (WARN, không chặn)
// ---------------------------------------------------------------------------

let filledCells = 0;
const perTone = { 1: 0, 2: 0, 3: 0, 4: 0 };
let syllablesWithAtLeast3 = 0;
for (const s of syllables) {
  let c = 0;
  for (const tone of [1, 2, 3, 4]) {
    if (s.tones[String(tone)]) {
      filledCells++;
      perTone[tone]++;
      c++;
    }
  }
  if (c >= 3) syllablesWithAtLeast3++;
}

if (filledCells < 300) {
  warn(`Độ phủ thấp: chỉ ${filledCells} cặp (âm tiết,thanh) có chữ minh hoạ (khuyến nghị ≥ 300)`);
}
for (const tone of [1, 2, 3, 4]) {
  if (perTone[tone] < 60) {
    warn(`Độ phủ thanh ${tone} thấp: ${perTone[tone]} âm tiết có chữ (khuyến nghị ≥ 60)`);
  }
}
if (syllablesWithAtLeast3 < 100) {
  warn(`Ít âm tiết có ≥ 3 thanh với chữ minh hoạ: ${syllablesWithAtLeast3} (khuyến nghị ≥ 100)`);
}

// ---------------------------------------------------------------------------
// 10. SOURCES.md có nhắc các khoá nguồn đã dùng + in thống kê
// ---------------------------------------------------------------------------

const REQUIRED_SOURCE_KEYS = [
  'pinyin-table', 'cc-cedict', 'original',
  // F6.1 — §5.4.1 hợp đồng F6/F7 (9 khoá)
  'hsk30-official', 'complete-hsk-vocabulary', 'cvdict', 'unihan', 'wiktionary',
  'han-viet-curated', 'machine', 'hsk1-overrides',
];
let sourcesText = '';
if (!fs.existsSync(SOURCES_PATH)) {
  fail(`Thiếu ${path.relative(CHINESE_ROOT, SOURCES_PATH)}`);
} else {
  sourcesText = fs.readFileSync(SOURCES_PATH, 'utf8');
  for (const key of REQUIRED_SOURCE_KEYS) {
    if (!sourcesText.includes(key)) {
      fail(`SOURCES.md thiếu mục cho khoá nguồn "${key}"`);
    }
  }
}

// ---------------------------------------------------------------------------
// 11. MỞ RỘNG F6.1 — hsk-words.json, characters.json, sources/{hsk1-overrides,han-viet,meaning-vi-machine}.json
// ---------------------------------------------------------------------------

const EXPECTED_HSK3_L1 = 500;
const SOURCES_DIR = path.join(CHINESE_ROOT, 'sources');
const VOCAB_DIR = path.join(CHINESE_ROOT, 'data', 'vocabulary');
const CHAR_DIR = path.join(CHINESE_ROOT, 'data', 'characters');
const RAW_DIR_F6 = path.join(CHINESE_ROOT, '.raw');

function loadJsonAt(dir, fileName, requiredLabel) {
  const p = path.join(dir, fileName);
  if (!fs.existsSync(p)) {
    fail(`Không tìm thấy file ${path.relative(CHINESE_ROOT, p)} (${requiredLabel})`);
    return null;
  }
  try {
    return JSON.parse(fs.readFileSync(p, 'utf8'));
  } catch (err) {
    fail(`${path.relative(CHINESE_ROOT, p)} không phải JSON hợp lệ: ${err.message}`);
    return null;
  }
}

function validateAgainstSchema(doc, schemaFileName, label) {
  if (doc === null) return false;
  const validateFn = ajv.compile(loadSchema(schemaFileName));
  const ok = validateFn(doc);
  if (!ok) {
    for (const err of validateFn.errors ?? []) {
      const at = err.instancePath ? err.instancePath.replace(/\//g, ' › ') : '(gốc)';
      fail(`${label} › ${at} — ${err.message} (${JSON.stringify(err.params)})`);
    }
    return false;
  }
  return true;
}

const hskWords = loadJsonAt(VOCAB_DIR, 'hsk-words.json', 'F6.1');
const characters = loadJsonAt(CHAR_DIR, 'characters.json', 'F6.1');
const hanVietSrc = loadJsonAt(SOURCES_DIR, 'han-viet.json', 'F6.1');
const overridesSrc = loadJsonAt(SOURCES_DIR, 'hsk1-overrides.json', 'F6.1');
const machineSrc = loadJsonAt(SOURCES_DIR, 'meaning-vi-machine.json', 'F6.1');
const lockSrc = loadJsonAt(SOURCES_DIR, 'sources.lock.json', 'F6.1');

const hskWordsOk = validateAgainstSchema(hskWords, 'hsk-words.schema.json', 'hsk-words.json');
const charactersOk = validateAgainstSchema(characters, 'characters.schema.json', 'characters.json');
validateAgainstSchema(hanVietSrc, 'han-viet.schema.json', 'sources/han-viet.json');
validateAgainstSchema(overridesSrc, 'hsk1-overrides.schema.json', 'sources/hsk1-overrides.json');
validateAgainstSchema(machineSrc, 'meaning-vi-machine.schema.json', 'sources/meaning-vi-machine.json');

// Cú pháp pinyin số thanh: phần chữ (bỏ số) phải thuộc bảng âm tiết F5 (syllables.json) hoặc 'r' (儿化).
const KNOWN_SYLLABLES = new Set([...syllableSet, 'r']);
function checkPinyinSyntax(loc, pinyinStr) {
  const toks = pinyinStr.split(' ');
  for (const tok of toks) {
    const m = tok.match(/^([A-Za-z]+)([1-5])$/);
    if (!m) {
      fail(`${loc} — âm tiết "${tok}" trong pinyin "${pinyinStr}" không đúng cú pháp (chữ cái + số thanh 1-5)`);
      continue;
    }
    if (!KNOWN_SYLLABLES.has(m[1].toLowerCase())) {
      fail(`${loc} — âm tiết "${m[1]}" (trong "${tok}") không có trong bảng âm tiết F5 (syllables.json) và khác 'r'`);
    }
  }
}

let cvdictSourceCount = 0;
let machineSourceCount = 0;
let manualSourceCount = 0;
let hanVietPresentCount = 0;

// Từ neo (anchor) — kiểm nghĩa chính không bị lọc mất bởi FILTER_PATTERNS của build-hsk.mjs (review
// 2026-09-17, mục 1) + lưới an toàn cấm nghĩa tục/lóng lọt qua (mục 3).
const ANCHOR_MEANING_CHECKS = [
  { simplified: '和', pinyin: 'he2', mustContain: 'và' },
  { simplified: '菜', pinyin: 'cai4', mustContainAny: ['rau', 'món'] },
];
// "tục"/"lóng" là chuỗi con hợp lệ trong nhiều từ tiếng Việt trung tính (tiếp tục, phong tục, tục ngữ,
// lóng ngóng, lóng lánh...) — loại các cụm an toàn này trước khi kết luận vi phạm, tránh dương tính giả
// (vd 呢 có nghĩa hợp lệ "...chỉ sự tiếp tục của trạng thái..."). "dâm" không có trường hợp an toàn nào.
const BANNED_MEANING_TERMS = [
  { term: 'tục', safeCompounds: ['tiếp tục', 'phong tục', 'tục ngữ', 'thế tục', 'tục lệ', 'hủ tục', 'mỹ tục', 'tập tục', 'cổ tục', 'thuần phong mỹ tục'] },
  { term: 'lóng', safeCompounds: ['lóng ngóng', 'lóng lánh', 'lóng cóng', 'vươn lóng'] },
  { term: 'dâm', safeCompounds: [] },
];
function findBannedTerm(meaning) {
  const lower = meaning.toLowerCase();
  for (const { term, safeCompounds } of BANNED_MEANING_TERMS) {
    if (!lower.includes(term)) continue;
    let stripped = lower;
    for (const safe of safeCompounds) stripped = stripped.split(safe).join('');
    if (stripped.includes(term)) return term;
  }
  return null;
}
const anchorFound = new Set();

if (hskWordsOk && hskWords) {
  const words = hskWords.words;

  if (words.length !== EXPECTED_HSK3_L1) {
    fail(`hsk-words.json phải có đúng ${EXPECTED_HSK3_L1} mục hsk3Level=1 (hằng EXPECTED_HSK3_L1), hiện có ${words.length}`);
  }
  if (hskWords.counts?.words !== words.length) {
    fail(`hsk-words.json › counts.words (${hskWords.counts?.words}) khác số mục thật (${words.length})`);
  }

  const keySet = new Set();
  const officialIndexByLevel = new Map(); // level -> Set<idx>
  const pathOrders = [];

  words.forEach((w, idx) => {
    const loc = `hsk-words.json › words[${idx}] (${w.simplified} ${w.pinyin})`;

    const key = `${w.simplified} ${w.pinyin}`;
    if (keySet.has(key)) fail(`${loc} — trùng khoá (simplified, pinyin) với mục khác`);
    keySet.add(key);

    if (!/^\p{Script=Han}+$/u.test(w.simplified)) {
      fail(`${loc} — simplified "${w.simplified}" có ký tự không phải chữ Hán`);
    }
    if (w.traditional !== null) {
      if ([...w.traditional].length !== [...w.simplified].length) {
        fail(`${loc} — traditional "${w.traditional}" khác số ký tự với simplified "${w.simplified}"`);
      }
      if (w.traditional === w.simplified) {
        fail(`${loc} — traditional trùng simplified, phải để null`);
      }
    }
    for (const v of w.variants) {
      if (v === w.simplified) fail(`${loc} — variants chứa chính simplified`);
    }

    checkPinyinSyntax(loc, w.pinyin);

    if (w.officialIndex !== null && w.hsk3Level !== null) {
      const set = officialIndexByLevel.get(w.hsk3Level) ?? new Set();
      if (set.has(w.officialIndex)) fail(`${loc} — officialIndex ${w.officialIndex} trùng trong cùng hsk3Level=${w.hsk3Level}`);
      set.add(w.officialIndex);
      officialIndexByLevel.set(w.hsk3Level, set);
    }

    if (w.hsk3Level === 1 && w.pathOrder !== null) pathOrders.push(w.pathOrder);

    if (w.meaningViStatus !== 'machine') {
      warn(`${loc} — meaningViStatus="${w.meaningViStatus}" khác "machine" (D5: mọi nghĩa Việt ở F6.1 phải "machine" tới khi F10 duyệt)`);
    }
    if (w.meaningViSource === 'cvdict') cvdictSourceCount++;
    if (w.meaningViSource === 'machine') machineSourceCount++;
    if (w.meaningViSource === 'manual') manualSourceCount++;
    if (w.hanViet !== null) hanVietPresentCount++;

    for (const meaning of w.meaningsVi) {
      const bad = findBannedTerm(meaning);
      if (bad) {
        fail(`${loc} — nghĩa "${meaning}" chứa từ cấm "${bad}" (không phù hợp học liệu — xem build-hsk.mjs WHOLE_REJECT_PATTERNS)`);
      }
    }

    for (const anchor of ANCHOR_MEANING_CHECKS) {
      if (w.simplified !== anchor.simplified || w.pinyin !== anchor.pinyin) continue;
      anchorFound.add(anchor.simplified);
      const joined = w.meaningsVi.join(' | ');
      const needles = anchor.mustContainAny ?? [anchor.mustContain];
      if (!needles.some((n) => joined.includes(n))) {
        fail(`${loc} — nghĩa neo: kỳ vọng chứa ${needles.map((n) => `"${n}"`).join(' hoặc ')} nhưng meaningsVi = [${joined}]`);
      }
    }

    for (const src of w.sources) {
      if (sourcesText && !sourcesText.includes(src)) {
        fail(`${loc} — nguồn "${src}" không thấy nhắc trong SOURCES.md`);
      }
    }

    // Mọi chữ Hán trong simplified phải có mặt trong characters.json
    if (charactersOk && characters) {
      const charSetInFile = new Set(characters.characters.map((c) => c.hanzi));
      for (const ch of [...w.simplified]) {
        if (!charSetInFile.has(ch)) {
          fail(`${loc} — chữ "${ch}" không có trong characters.json`);
        }
      }
    }
  });

  for (const anchor of ANCHOR_MEANING_CHECKS) {
    if (!anchorFound.has(anchor.simplified)) {
      fail(`Không tìm thấy từ neo (${anchor.simplified} ${anchor.pinyin}) trong hsk-words.json để kiểm nghĩa chính`);
    }
  }

  const sortedPathOrders = [...pathOrders].sort((a, b) => a - b);
  const expectedLen = words.filter((w) => w.hsk3Level === 1).length;
  if (sortedPathOrders.length !== expectedLen) {
    fail(`hsk-words.json — có ${expectedLen} mục hsk3Level=1 nhưng chỉ ${sortedPathOrders.length} mục có pathOrder`);
  }
  const dupPathOrders = sortedPathOrders.filter((p, i) => sortedPathOrders[i - 1] === p);
  if (dupPathOrders.length) fail(`hsk-words.json — pathOrder trùng: ${[...new Set(dupPathOrders)].join(', ')}`);
  for (let i = 0; i < sortedPathOrders.length; i++) {
    if (sortedPathOrders[i] !== i + 1) {
      fail(`hsk-words.json — pathOrder không liên tục 1..${expectedLen} (lệch tại vị trí ${i + 1}, giá trị ${sortedPathOrders[i]})`);
      break;
    }
  }
}

if (charactersOk && characters) {
  const seenHanzi = new Set();
  for (const [idx, c] of characters.characters.entries()) {
    const loc = `characters.json › characters[${idx}] (${c.hanzi})`;
    if ([...c.hanzi].length !== 1) fail(`${loc} — hanzi phải đúng 1 code point, hiện có ${[...c.hanzi].length}`);
    if (seenHanzi.has(c.hanzi)) fail(`${loc} — hanzi trùng với mục khác`);
    seenHanzi.add(c.hanzi);
    for (const r of c.pinyinReadings) {
      if (!KNOWN_SYLLABLES.has(r.replace(/[1-5]$/, ''))) {
        fail(`${loc} — pinyinReadings "${r}" không thuộc bảng âm tiết F5 và khác 'r'`);
      }
    }
    if (c.hanViet.length === 0) {
      warn(`${loc} — hanViet rỗng (chưa có âm Hán Việt) — cần duyệt ở F10`);
    }
    for (const src of c.sources) {
      if (sourcesText && !sourcesText.includes(src)) {
        fail(`${loc} — nguồn "${src}" không thấy nhắc trong SOURCES.md`);
      }
    }
  }
}

if (!fs.existsSync(RAW_DIR_F6) || !fs.existsSync(path.join(RAW_DIR_F6, 'unihan'))) {
  warn(`Bỏ qua đối chiếu nguồn thô F6.1 (.raw/) — chạy 'yarn --cwd content fetch:chinese' rồi 'yarn --cwd content build:chinese' trước khi bàn giao để có đối chiếu đầy đủ`);
} else if (lockSrc) {
  for (const [key, meta] of Object.entries(lockSrc.files ?? {})) {
    const p = path.join(RAW_DIR_F6, meta.raw);
    if (!fs.existsSync(p)) {
      warn(`Thiếu .raw/${meta.raw} (nguồn "${key}") — không đối chiếu được, chạy fetch:chinese`);
      continue;
    }
    const buf = fs.readFileSync(p);
    const hash = createHash('sha256').update(buf).digest('hex');
    if (hash !== meta.sha256) {
      fail(`.raw/${meta.raw} (nguồn "${key}") có sha256 khác sources.lock.json — dữ liệu thô đã đổi mà chưa build lại / chưa --update-lock`);
    }
  }
}

console.log('\n--- Thống kê học liệu pinyin ---');
console.log(`Thanh mẫu:            ${initials.length}`);
console.log(`Vận mẫu:               ${finals.length}`);
console.log(`Âm tiết:                ${syllables.length}`);
console.log(`Cặp (âm tiết,thanh) có chữ: ${filledCells}`);
console.log(`  Theo thanh:  1=${perTone[1]}  2=${perTone[2]}  3=${perTone[3]}  4=${perTone[4]}`);
console.log(`Âm tiết có ≥3 thanh có chữ: ${syllablesWithAtLeast3}`);
console.log(`Chủ đề hướng dẫn:       ${guideTopics.length}`);
console.log(`Đối chiếu CC-CEDICT:    ${cedict ? `có (${cedict.size} chữ đơn ký tự)` : 'KHÔNG (thiếu .raw/) — xem cảnh báo ở trên'}`);

console.log('\n--- Thống kê học liệu HSK 3.0 cấp 1 (F6.1) ---');
console.log(`Từ (hsk-words.json):    ${hskWords?.words?.length ?? '(lỗi)'}`);
console.log(`Chữ (characters.json):  ${characters?.characters?.length ?? '(lỗi)'}`);
console.log(`Nghĩa Việt: cvdict=${cvdictSourceCount}, machine=${machineSourceCount}, manual=${manualSourceCount}`);
console.log(`Có Hán Việt cấp từ: ${hanVietPresentCount}/${hskWords?.words?.length ?? 0}`);

console.log(`\n${failCount} lỗi, ${warnCount} cảnh báo.`);
if (failCount > 0) {
  console.error('KIỂM TRA THẤT BẠI.');
  process.exit(1);
}
console.log('Kiểm tra học liệu pinyin: OK.');
