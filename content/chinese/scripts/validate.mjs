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

const REQUIRED_SOURCE_KEYS = ['pinyin-table', 'cc-cedict', 'original'];
if (!fs.existsSync(SOURCES_PATH)) {
  fail(`Thiếu ${path.relative(CHINESE_ROOT, SOURCES_PATH)}`);
} else {
  const sourcesText = fs.readFileSync(SOURCES_PATH, 'utf8');
  for (const key of REQUIRED_SOURCE_KEYS) {
    if (!sourcesText.includes(key)) {
      fail(`SOURCES.md thiếu mục cho khoá nguồn "${key}"`);
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

console.log(`\n${failCount} lỗi, ${warnCount} cảnh báo.`);
if (failCount > 0) {
  console.error('KIỂM TRA THẤT BẠI.');
  process.exit(1);
}
console.log('Kiểm tra học liệu pinyin: OK.');
