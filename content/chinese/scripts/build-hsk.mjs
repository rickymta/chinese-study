#!/usr/bin/env node
// content/chinese/scripts/build-hsk.mjs
//
// Dựng content/chinese/data/vocabulary/hsk-words.json (500 mục HSK 3.0 cấp 1) và
// content/chinese/data/characters/characters.json từ các nguồn ghim ở sources/sources.lock.json
// (xem SOURCES.md) + tệp biên soạn sources/{hsk1-overrides,han-viet,meaning-vi-machine}.json.
// Thuật toán đúng như hợp đồng docs/agent-workflow/2026-09-17-antfarm-f6-f7-chi-tiet.md §5.4.3.
//
// Chạy: `yarn --cwd content build:chinese` (cần chạy `fetch:chinese` trước để có .raw/).
// Tất định: không nhúng thời điểm build; cùng nguồn ⇒ chạy lại ra file giống hệt byte (R6-12).

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { loadCedictEntries } from './lib/cedict.mjs';
import { loadUnihanProperties, parseCjkRadicals, parseFirstRsUnicode } from './lib/unihan.mjs';
import { toneMarkedSyllableToNumbered } from './lib/pinyin.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CHINESE_ROOT = path.resolve(__dirname, '..');
const RAW_DIR = path.join(CHINESE_ROOT, '.raw');
const SOURCES_DIR = path.join(CHINESE_ROOT, 'sources');
const DATA_DIR = path.join(CHINESE_ROOT, 'data');

const DATASET_VERSION = '2026-09-17'; // đổi tay khi đổi dữ liệu nguồn (R6-12 — không dùng ngày build thật)
const EXPECTED_HSK3_L1 = 500;

const errors = [];
const warnings = [];
function err(msg) {
  errors.push(msg);
}
function warn(msg) {
  warnings.push(msg);
}

// ---------------------------------------------------------------------------
// 1. Đọc lock + kiểm hash .raw
// ---------------------------------------------------------------------------

const lock = JSON.parse(fs.readFileSync(path.join(SOURCES_DIR, 'sources.lock.json'), 'utf8'));

function requireRaw(key) {
  const meta = lock.files[key];
  const p = path.join(RAW_DIR, meta.raw);
  if (!fs.existsSync(p)) {
    console.error(`✗ Thiếu ${path.relative(CHINESE_ROOT, p)} — chạy 'yarn --cwd content fetch:chinese' trước.`);
    process.exit(1);
  }
  const hash = createHash('sha256').update(fs.readFileSync(p)).digest('hex');
  if (hash !== meta.sha256) {
    console.error(
      `✗ ${path.relative(CHINESE_ROOT, p)} (nguồn "${key}") có sha256 ${hash}, KHÁC sources.lock.json (${meta.sha256}) — ` +
        `dữ liệu thô đã đổi mà lock chưa cập nhật. Xoá file rồi chạy lại fetch:chinese, hoặc fetch:chinese --update-lock nếu chủ động nâng phiên bản.`,
    );
    process.exit(1);
  }
  return p;
}

const hsk30WordlistPath = requireRaw('hsk30-official');
const completeJsonPath = requireRaw('complete-hsk-vocabulary');
const cvdictPath = requireRaw('cvdict');
const unihanDir = path.join(RAW_DIR, 'unihan');
const cjkRadicalsPath = requireRaw('cjk-radicals');

for (const f of ['Unihan_Readings.txt', 'Unihan_Variants.txt', 'Unihan_IRGSources.txt']) {
  if (!fs.existsSync(path.join(unihanDir, f))) {
    console.error(`✗ Thiếu .raw/unihan/${f} — chạy 'yarn --cwd content fetch:chinese' trước.`);
    process.exit(1);
  }
}

// ---------------------------------------------------------------------------
// 2. Danh sách chính thức cấp 1
// ---------------------------------------------------------------------------

const POS_HINT_CHARS = new Set('名动形副代量数介连助叹、'.split(''));

function parseOfficialLevel1(text) {
  const lines = text.split('\n');
  const startIdx = lines.findIndex((l) => l.trim() === '一级词汇表');
  if (startIdx === -1) throw new Error('Không tìm thấy tiêu đề "一级词汇表" trong hsk30-wordlist.txt');
  const items = [];
  for (let i = startIdx + 1; i < lines.length; i++) {
    const trimmed = lines[i].trim();
    if (trimmed === '') continue;
    if (/级词汇表$/.test(trimmed)) break;
    const m = trimmed.match(/^(\d+)\s+(.+)$/);
    if (!m) {
      err(`Dòng không đúng định dạng "<idx> <raw>" trong 一级词汇表: "${trimmed}"`);
      continue;
    }
    items.push({ idx: Number(m[1]), raw: m[2] });
  }
  return items;
}

function parseRawEntry(raw) {
  if (raw.includes('｜')) {
    const parts = raw.split('｜');
    return { head: parts[0], variants: parts.slice(1), usageNote: null };
  }
  const midMatch = raw.match(/^(.*?)（(.+?)）(.+)$/);
  if (midMatch) {
    const [, x, y, z] = midMatch;
    return { head: x + z, variants: [x + y + z], usageNote: null };
  }
  const endMatch = raw.match(/^(.*?)（(.+?)）$/);
  if (endMatch) {
    const [, x, y] = endMatch;
    const isPosHint = [...y].every((c) => POS_HINT_CHARS.has(c));
    return { head: x, variants: [], usageNote: isPosHint ? null : y };
  }
  return { head: raw, variants: [], usageNote: null };
}

const officialRaw = fs.readFileSync(hsk30WordlistPath, 'utf8');
const officialItems = parseOfficialLevel1(officialRaw).map((it) => ({ idx: it.idx, raw: it.raw, ...parseRawEntry(it.raw) }));

if (officialItems.length !== EXPECTED_HSK3_L1) {
  err(`Danh sách 一级词汇表 có ${officialItems.length} dòng, cần đúng ${EXPECTED_HSK3_L1}`);
}
{
  const idxs = officialItems.map((i) => i.idx);
  for (let i = 0; i < idxs.length; i++) {
    if (idxs[i] !== i + 1) {
      err(`Số thứ tự không liên tục 1..${EXPECTED_HSK3_L1}: gặp ${idxs[i]} ở vị trí ${i + 1}`);
      break;
    }
  }
}

exitIfErrors();

// ---------------------------------------------------------------------------
// 3. complete.json → Map theo simplified
// ---------------------------------------------------------------------------

const completeList = JSON.parse(fs.readFileSync(completeJsonPath, 'utf8'));
const completeMap = new Map(completeList.map((d) => [d.simplified, d]));

for (const item of officialItems) {
  if (!completeMap.has(item.head)) {
    err(`Không tìm thấy "${item.head}" (idx ${item.idx}) trong complete-hsk-vocabulary`);
  }
}
exitIfErrors();

// ---------------------------------------------------------------------------
// 4–5. Cách đọc + phồn thể
// ---------------------------------------------------------------------------

const overridesDoc = JSON.parse(fs.readFileSync(path.join(SOURCES_DIR, 'hsk1-overrides.json'), 'utf8'));
const overrides = overridesDoc.byOfficialIndex;

function normalizeReadingString(raw, item) {
  const toks = raw.split(/\s+/).map((tok) => {
    let t = tok.replace(/ü/g, 'v');
    if (!/[1-5]$/.test(t)) {
      warn(`Âm tiết thiếu số thanh trong complete-hsk-vocabulary cho "${item.head}" (idx ${item.idx}): "${tok}" — coi là thanh nhẹ (+5)`);
      t = `${t}5`;
    }
    return t;
  });
  return toks.join(' ');
}

function codePointsOf(traditionalVariantField) {
  if (!traditionalVariantField) return [];
  return traditionalVariantField
    .trim()
    .split(/\s+/)
    .map((v) => String.fromCodePoint(Number.parseInt(v.slice(2), 16)));
}

const unihanProps = loadUnihanProperties(
  ['Unihan_Readings.txt', 'Unihan_Variants.txt', 'Unihan_IRGSources.txt'].map((f) => path.join(unihanDir, f)),
);
const cjkRadicals = parseCjkRadicals(cjkRadicalsPath);

const resolved = []; // { item, entry, chosen, chosenLower, matchingForms, traditional }

for (const item of officialItems) {
  const entry = completeMap.get(item.head);
  const forms = entry.forms;
  const readings = forms.map((f) => normalizeReadingString(f.transcriptions.numeric, item));
  const override = overrides[String(item.idx)];

  let chosen;
  if (override && override.pinyin) {
    const ok = readings.some((r) => r.toLowerCase() === override.pinyin.toLowerCase());
    if (!ok) {
      err(`Override pinyin "${override.pinyin}" (idx ${item.idx}, ${item.head}) không thuộc các cách đọc: ${[...new Set(readings)].join(', ')}`);
      continue;
    }
    chosen = override.pinyin;
  } else {
    const lowerOnly = readings.filter((r) => r === r.toLowerCase());
    const distinctLower = [...new Set(lowerOnly)];
    if (distinctLower.length === 1) {
      chosen = distinctLower[0];
    } else if (distinctLower.length === 0) {
      chosen = readings[0];
    } else {
      err(`Cần override cách đọc cho idx=${item.idx} (${item.head}) — các cách đọc: ${[...new Set(readings)].join(', ')}`);
      continue;
    }
  }

  const matchingForms = forms.filter((f, i) => readings[i].toLowerCase() === chosen.toLowerCase());

  // Phồn thể (R6-6)
  const headChars = [...item.head];
  const candTrads = [...new Set(matchingForms.map((f) => f.traditional))];
  function passesUnihanFilter(t) {
    const tChars = [...t];
    if (tChars.length !== headChars.length) return false;
    return tChars.every((tc, i) => {
      const hc = headChars[i];
      const props = unihanProps.get(hc);
      const variants = codePointsOf(props?.kTraditionalVariant);
      if (variants.length === 0) return tc === hc;
      return variants.includes(tc);
    });
  }
  let chosenTrad;
  if (override && override.traditional) {
    chosenTrad = override.traditional;
  } else {
    const filtered = candTrads.filter(passesUnihanFilter);
    if (filtered.length === 1) {
      chosenTrad = filtered[0];
    } else if (filtered.length > 1) {
      warn(`Nhiều ứng viên phồn thể cho idx=${item.idx} (${item.head}): ${filtered.join(', ')} — lấy "${filtered[0]}" (cân nhắc thêm override)`);
      chosenTrad = filtered[0];
    } else {
      const fallback = candTrads[0] ?? item.head;
      warn(`Không có ứng viên phồn thể hợp lệ theo Unihan cho idx=${item.idx} (${item.head}) trong [${candTrads.join(', ')}] — lấy "${fallback}"`);
      chosenTrad = fallback;
    }
  }
  const traditional = chosenTrad === item.head ? null : chosenTrad;

  resolved.push({ item, entry, chosen, matchingForms, traditional, headChars });
}

exitIfErrors();

// ---------------------------------------------------------------------------
// 6. Nghĩa tiếng Anh
// ---------------------------------------------------------------------------

for (const r of resolved) {
  const tradForCompare = r.traditional ?? r.item.head;
  const ordered = [
    ...r.matchingForms.filter((f) => f.traditional === tradForCompare),
    ...r.matchingForms.filter((f) => f.traditional !== tradForCompare),
  ];
  const meaningsEn = [];
  outer: for (const f of ordered) {
    for (const m of f.meanings) {
      if (!meaningsEn.includes(m)) meaningsEn.push(m);
      if (meaningsEn.length >= 8) break outer;
    }
  }
  r.meaningsEn = meaningsEn;
}

// ---------------------------------------------------------------------------
// 7. Nghĩa tiếng Việt — CVDICT, lọc, fallback dịch máy
// ---------------------------------------------------------------------------

// LỆCH §5.4.3 bước 7 [chấp nhận theo review 2026-09-17]: bản gốc coi bất kỳ nghĩa nào chứa "[xx1]" là
// rác và bỏ CẢ câu — nhưng CVDICT hay chèn chú thích tham chiếu pinyin ("(cách đọc ở Đài Loan [han4])",
// "(dùng giống như 把[ba3]: ...)", "LT:個|个[ge4]") NGAY TRONG một câu có nghĩa chính hữu ích đứng trước,
// khiến 和 mất nghĩa "và; cùng với", 菜 mất "rau", các từ 这/那/有/国/路/就/来/多/听/考... mất nghĩa chính dùng
// nhiều nhất. Nay CHỈ CẮT đoạn chứa tham chiếu (ngoặc hoặc "[xx1]" trơ) rồi giữ phần còn lại; chỉ bỏ CẢ
// câu khi phần còn lại rỗng hoặc chính nó khớp một mẫu loại khác (họ người/biến thể/tục/lóng...). Xem
// SOURCES.md mục "F6.1" và `.raw/build-report.md` (in danh sách nghĩa đã cắt đoạn để đối chiếu).
const WHOLE_REJECT_PATTERNS = [
  /^họ\s.*\[/iu, // "họ [Gan1]" — \b không khớp trước "ọ" (không phải ranh giới từ theo \w) nên đổi \s
  // (tục)/(lóng)/(khẩu ngữ)... + BẤT KỲ nội dung nào theo sau trong ngoặc (không neo cuối bằng ')' ngay
  // sau từ khoá — bản cũ chỉ khớp khi ngoặc KHÔNG có gì thêm nên lọt "(tiếng lóng Internet)", "(khẩu ngữ)
  // dâm đãng"). Đây là loại-bỏ-cả-câu có chủ đích: nghĩa tục/lóng không phù hợp học liệu.
  /\((?:tục|lóng|tiếng lóng|thông tục|khẩu ngữ)[^)]*\)/iu,
  // "biến thể của"/"biến thể CŨ của"/"biến thể ER HOÁ của"... CVDICT chèn thêm tính từ giữa hai từ khoá
  // (cũ, er hoá...) nên neo đúng "biến thể của" bỏ lọt rất nhiều dòng (vd 一块儿 "biến thể er hoá của
  // 一块[yi1 kuai4]" từng lọt qua nguyên câu vì không khớp cả whole-reject cũ lẫn bị cắt hết bởi
  // stripPinyinReferenceSegments). Neo lỏng hơn: bắt đầu bằng "biến thể" là đủ — không có nghĩa hợp lệ
  // nào của CVDICT bắt đầu bằng cụm này ngoài chú thích dị thể/biến âm.
  // LƯU Ý: không dùng \b sau nguyên âm có dấu tiếng Việt — \b của JS chỉ hiểu \w kiểu ASCII nên coi các
  // chữ có dấu (ể, ọ...) đã là "ranh giới từ" ngay sau phụ âm, khiến \b rơi sai chỗ và không khớp gì cả
  // (bug y hệt mục "họ\b" ở trên) — dùng khớp tiền tố trần, đủ đặc trưng để không dương tính giả.
  /^(biến thể|viết tắt của|dùng trong|cũng viết|cũng đọc|xem |tiếng Đài Loan đọc là|phiên âm)/iu,
  /^\((cổ|cổ điển|văn học|văn viết|phương ngữ)\)/iu,
  /^LT:/u,
  /^Lượng từ:/iu, // CVDICT có nơi viết chữ đầy đủ "Lượng từ:" thay vì viết tắt "LT:" — cùng loại chú
  // thích lượng từ đi kèm, không phải nghĩa của từ; sau khi cắt các "[xx1]" bên trong chỉ còn trơ
  // "Lượng từ:" hoặc "Lượng từ: ," — vẫn phải loại cả câu vì không còn nội dung dạy được.
  // "dâm" trơ không có ngoặc chú thích đi kèm — vd CVDICT 花: ".../(khẩu ngữ) dâm đãng/dâm ô/" — mục đầu
  // đã bị mẫu (khẩu ngữ) ở trên loại, nhưng "dâm ô" là MỘT MỤC RIÊNG không có ngoặc nào để bắt. Không có
  // từ tiếng Việt thông dụng/trung tính nào chứa "dâm" nên loại thẳng, không cần bọc trong dấu ngoặc.
  /dâm/iu,
];

/** Cắt các cụm chứa tham chiếu pinyin kiểu "[gan1]" khỏi một nghĩa — GIỮ phần còn lại (không bỏ cả câu). */
// Tham chiếu pinyin trong ngoặc vuông của CVDICT: 1 âm tiết "[gan1]" HOẶC nhiều âm tiết cách nhau dấu
// phẩy/khoảng trắng của TỪ NHIỀU CHỮ "[yi1 kuai4]" (vd "一块[yi1 kuai4]" trong "biến thể er hoá của
// 一塊|一块[yi1 kuai4]" — thiếu nhánh khoảng trắng từng khiến cả cụm không bị cắt, lọt nguyên câu vô nghĩa).
// CVDICT viết nhiều âm tiết trong MỘT ngoặc theo 3 kiểu khác nhau: rời "[gan1]", cách nhau khoảng trắng
// "[yi1 kuai4]", HOẶC DÍNH LIỀN không dấu tách "[hui2lai5]" (vd trong ví dụ minh hoạ 回來|回来[hui2lai5],
// 出差[chu1chai1]) — thiếu nhánh dính liền từng khiến cả cụm ví dụ Hán tự không bị cắt, lọt nguyên câu.
const PINYIN_BRACKET_REF = /\[(?:[A-Za-z]+[1-5] ?)+\]/;

function stripPinyinReferenceSegments(text) {
  let t = text;
  // Cụm ngoặc có chứa tham chiếu pinyin ở bất kỳ đâu bên trong, vd "(cách đọc ở Đài Loan [han4])",
  // "(dùng giống như 把[ba3]: đánh dấu danh từ phía sau là tân ngữ)".
  t = t.replace(new RegExp(`\\s*\\((?:[^()]*${PINYIN_BRACKET_REF.source}[^()]*)\\)`, 'gu'), '');
  // Phần "[xx1]"/"[xx1 yy2]" còn sót lại không nằm trong ngoặc (có thể kèm chữ Hán/｜ đứng ngay trước,
  // kiểu "個|个[ge4],位[wei4]" hoặc "一塊|一块[yi1 kuai4]" sau "biến thể er hoá của").
  t = t.replace(new RegExp(`[\\p{Script=Han}｜|]*${PINYIN_BRACKET_REF.source}`, 'gu'), '');
  return t.replace(/\s{2,}/g, ' ').trim();
}

const cvdictIndex = loadCedictEntries(cvdictPath);
const machineDoc = fs.existsSync(path.join(SOURCES_DIR, 'meaning-vi-machine.json'))
  ? JSON.parse(fs.readFileSync(path.join(SOURCES_DIR, 'meaning-vi-machine.json'), 'utf8'))
  : { entries: [] };
const machineIndex = new Map(machineDoc.entries.map((e) => [`${e.simplified} ${e.pinyin}`, e]));

let cvdictCount = 0;
let machineCount = 0;
let manualCount = 0;

function filterDefs(defs) {
  const out = [];
  for (const raw of defs) {
    const m = raw.trim();
    if (m.length === 0) continue;
    if (WHOLE_REJECT_PATTERNS.some((re) => re.test(m))) continue;
    const stripped = stripPinyinReferenceSegments(m);
    if (stripped.length === 0) continue; // sau khi cắt tham chiếu, không còn nội dung hữu ích
    if (stripped.length > 120) continue;
    if (WHOLE_REJECT_PATTERNS.some((re) => re.test(stripped))) continue; // lộ mẫu xấu sau khi cắt
    if (stripped !== m) {
      warn(`Đã cắt tham chiếu pinyin trong nghĩa CVDICT: "${m}" → "${stripped}"`);
    }
    out.push(stripped);
    if (out.length >= 6) break;
  }
  return out;
}

for (const r of resolved) {
  const { item, chosen, traditional } = r;
  const entries = cvdictIndex?.get(item.head) ?? [];
  let candidates = entries.filter((e) => e.pinyinKey === chosen);
  if (candidates.length === 0) {
    candidates = entries.filter((e) => e.pinyinKey.toLowerCase() === chosen.toLowerCase());
  }
  if (candidates.length > 1) {
    // So theo traditional đã chọn — CẢ KHI null (nghĩa là phồn thể trùng giản thể): CVDICT có nhiều dòng
    // cùng (giản thể, pinyin) nhưng khác trường traditional (vd 考 考 kao3 vs 攷 考 kao3 — 攷 là dị thể cổ);
    // so với item.head khi traditional=null mới loại đúng dòng dị thể (nếu chỉ so khi truthy sẽ bỏ sót
    // trường hợp null và lấy nhầm dòng theo thứ tự xuất hiện trong file).
    const tradForCompare = traditional ?? item.head;
    const withTrad = candidates.filter((e) => e.traditional === tradForCompare);
    if (withTrad.length > 0) candidates = withTrad;
  }

  let filtered = [];
  if (candidates.length > 0) {
    // CVDICT thường có nhiều dòng trùng (giao lại/biến thể cũ) cho cùng (giản thể, pinyin) — vd 家/傢 家.
    // "Lấy mục đầu" theo nghĩa đen (§5.4.3 bước 7) có thể chọn đúng dòng "dùng trong X" (lọc ra rỗng) trong
    // khi dòng khác cùng khoá có nghĩa dùng được. Tinh chỉnh [content-implement]: lọc TỪNG ứng viên trước,
    // ưu tiên ứng viên còn nghĩa sau lọc; hoà (nhiều ứng viên đều còn nghĩa, hoặc đều rỗng) mới "lấy mục đầu".
    const withFiltered = candidates.map((c) => ({ c, filtered: filterDefs(c.definitions) }));
    const nonEmpty = withFiltered.filter((x) => x.filtered.length > 0);
    if (nonEmpty.length > 1) {
      warn(`CVDICT có nhiều mục còn nghĩa hợp lệ sau lọc cho "${item.head}" (${chosen}) — lấy mục đầu`);
    } else if (candidates.length > 1 && nonEmpty.length === 0) {
      warn(`CVDICT có nhiều mục cho "${item.head}" (${chosen}), mục đầu không còn nghĩa sau lọc — lấy mục đầu (rỗng, sẽ cần dịch máy)`);
    }
    filtered = nonEmpty.length > 0 ? nonEmpty[0].filtered : withFiltered[0].filtered;
  }

  if (filtered.length > 0) {
    r.meaningsVi = filtered;
    r.meaningViSource = 'cvdict';
    cvdictCount++;
  } else {
    const key = `${item.head} ${chosen}`;
    const machineEntry = machineIndex.get(key);
    if (!machineEntry) {
      err(
        `Cần dịch máy cho "${item.head}" (${chosen}, idx ${item.idx}) trong sources/meaning-vi-machine.json — nghĩa Anh: ${r.meaningsEn.join(' / ')}`,
      );
      continue;
    }
    r.meaningsVi = machineEntry.meaningsVi;
    r.meaningViSource = 'machine';
    machineCount++;
  }

  // Ghi đè nghĩa Việt theo officialIndex (hsk1-overrides.json.byOfficialIndex[idx].meaningsVi) — dùng để
  // content-implement sửa tay thứ tự/nội dung nghĩa rõ ràng sai hoặc lệch trọng tâm sư phạm sau khi đã
  // qua CVDICT/lọc (vd nghĩa chính "và; cùng với" của 和 bị xếp sau, "trở về" của 回 cần lên đầu, 星期
  // lẫn nghĩa "Chủ nhật" của 星期天/星期日). Không đổi meaningViStatus (vẫn "machine" theo D5 — nội dung
  // do content-implement biên tập tay vẫn cần người duyệt xác nhận ở F10).
  const meaningOverride = overrides[String(item.idx)]?.meaningsVi;
  if (meaningOverride) {
    if (r.meaningViSource === 'cvdict') cvdictCount--;
    else if (r.meaningViSource === 'machine') machineCount--;
    r.meaningsVi = meaningOverride;
    r.meaningViSource = 'manual';
    manualCount++;
  }

  r.meaningViStatus = 'machine'; // R6-11/D5: MỌI nghĩa Việt = machine tới khi được duyệt ở F10
}

exitIfErrors();

// ---------------------------------------------------------------------------
// 8. Cấp độ + tần suất + từ loại
// ---------------------------------------------------------------------------

for (const r of resolved) {
  const levels = r.entry.level ?? [];
  const oldLevels = levels.filter((l) => l.startsWith('old-')).map((l) => Number(l.split('-')[1]));
  const newestLevels = levels.filter((l) => l.startsWith('newest-')).map((l) => Number(l.split('-')[1]));
  r.hsk2Level = oldLevels.length ? Math.min(...oldLevels) : null;
  r.hskExam2026Level = newestLevels.length ? Math.min(...newestLevels) : null;
  r.frequencyRank = typeof r.entry.frequency === 'number' ? r.entry.frequency : null;
  r.pos = r.entry.pos ?? [];
  r.hasOld1 = levels.includes('old-1');
  r.hasNewest1 = levels.includes('newest-1');
}

// ---------------------------------------------------------------------------
// 9. Chữ Hán — tập hợp từ mọi head + đọc Hán Việt biên soạn
// ---------------------------------------------------------------------------

const hanVietDoc = JSON.parse(fs.readFileSync(path.join(SOURCES_DIR, 'han-viet.json'), 'utf8'));
const hanVietByChar = new Map(hanVietDoc.characters.map((c) => [c.hanzi, c]));

const charSet = new Set();
for (const r of resolved) {
  for (const c of r.headChars) charSet.add(c);
}
const sortedChars = [...charSet].sort((a, b) => a.codePointAt(0) - b.codePointAt(0));

// Gom cách đọc thực tế xuất hiện trong các từ (theo thứ tự officialIndex) — dùng để bổ sung
// pinyinReadings ngoài kMandarin, và để kiểm han-viet.json byPinyin có phủ đủ.
const readingsSeenByChar = new Map(); // char -> string[] (thứ tự gặp lần đầu, không trùng)
for (const r of resolved.slice().sort((a, b) => a.item.idx - b.item.idx)) {
  const tokens = r.chosen.split(' ');
  if (tokens.length !== r.headChars.length) {
    warn(`Số âm tiết (${tokens.length}) khác số chữ (${r.headChars.length}) trong "${r.item.head}" (idx ${r.item.idx}) — bỏ ánh xạ chữ↔âm cho mục này`);
    continue;
  }
  r.charSyllables = tokens.map((t) => t.toLowerCase());
  r.headChars.forEach((c, i) => {
    const syl = tokens[i].toLowerCase();
    if (!readingsSeenByChar.has(c)) readingsSeenByChar.set(c, []);
    const arr = readingsSeenByChar.get(c);
    if (!arr.includes(syl)) arr.push(syl);
  });
}

const characters = [];
const missingHanViet = [];
const hanVietReportRows = [];

for (const hanzi of sortedChars) {
  const props = unihanProps.get(hanzi) ?? {};

  // pinyinReadings: kMandarin trước, rồi các âm tiết gặp trong từ (thứ tự officialIndex)
  const kMandarinTokens = props.kMandarin
    ? props.kMandarin
        .trim()
        .split(/\s+/)
        .map((s) => toneMarkedSyllableToNumbered(s))
    : [];
  const pinyinReadings = [...kMandarinTokens];
  for (const s of readingsSeenByChar.get(hanzi) ?? []) {
    if (!pinyinReadings.includes(s)) pinyinReadings.push(s);
  }
  if (pinyinReadings.length === 0) {
    warn(`Chữ "${hanzi}" không có kMandarin trong Unihan và không gặp trong từ nào — bỏ trống pinyinReadings (sẽ FAIL schema, cần kiểm)`);
  }

  const traditionalVariants = codePointsOf(props.kTraditionalVariant).filter((t) => t !== hanzi);

  let strokeCount = null;
  if (props.kTotalStrokes) {
    const n = Number.parseInt(props.kTotalStrokes.trim().split(/\s+/)[0], 10);
    if (Number.isFinite(n)) strokeCount = n;
  }
  let radical = null;
  let radicalNumber = null;
  if (props.kRSUnicode) {
    const parsed = parseFirstRsUnicode(props.kRSUnicode);
    if (parsed) {
      const rad = cjkRadicals.get(parsed.radicalKey);
      if (rad) {
        radical = rad.cjkChar;
        radicalNumber = Number.parseInt(parsed.radicalKey, 10);
      }
    }
  }

  const hv = hanVietByChar.get(hanzi);
  hanVietReportRows.push({
    hanzi,
    kVietnamese: props.kVietnamese ?? '',
    pinyinReadings: pinyinReadings.join(' '),
    used: hv ? hv.readings.join(', ') : '(THIẾU)',
    note: hv?.note ?? '',
  });
  if (!hv) {
    missingHanViet.push(hanzi);
    continue;
  }
  if (pinyinReadings.length > 1) {
    const missingPinyin = pinyinReadings.filter((p) => !(hv.byPinyin && Object.prototype.hasOwnProperty.call(hv.byPinyin, p)));
    if (missingPinyin.length > 0) {
      err(`sources/han-viet.json thiếu byPinyin cho chữ "${hanzi}" các âm: ${missingPinyin.join(', ')} (cách đọc: ${pinyinReadings.join(', ')})`);
      continue;
    }
  }

  characters.push({
    hanzi,
    traditionalVariants,
    pinyinReadings,
    hanViet: hv.readings,
    hanVietByPinyin: hv.byPinyin ?? null,
    hanVietStatus: 'derived',
    strokeCount,
    radical,
    radicalNumber,
    sources: ['unihan', 'han-viet-curated'],
  });
}

if (missingHanViet.length > 0) {
  err(`sources/han-viet.json thiếu ${missingHanViet.length} chữ: ${missingHanViet.join(' ')}`);
}

// Ghi báo cáo đối chiếu Hán Việt NGAY (không chờ tới cuối) — cần để biên soạn han-viet.json kể cả khi
// build còn lỗi (thiếu chữ, thiếu byPinyin). Không commit (.raw/).
{
  const reportLines = [];
  reportLines.push('# Báo cáo build-hsk.mjs — đối chiếu Hán Việt\n');
  reportLines.push('| Chữ | Cách đọc (pinyin dùng trong từ) | kVietnamese (Unihan, chỉ để đối chiếu — CÓ THỂ lẫn âm Nôm) | Đang dùng (han-viet.json) | Ghi chú |');
  reportLines.push('|---|---|---|---|---|');
  for (const row of hanVietReportRows) {
    reportLines.push(`| ${row.hanzi} | ${row.pinyinReadings} | ${row.kVietnamese} | ${row.used} | ${row.note} |`);
  }
  reportLines.push('');
  reportLines.push(`Tổng số chữ: ${sortedChars.length}. Thiếu han-viet.json: ${missingHanViet.length} (${missingHanViet.join(' ')})`);
  fs.mkdirSync(RAW_DIR, { recursive: true });
  fs.writeFileSync(path.join(RAW_DIR, 'build-report.md'), reportLines.join('\n'));
}

// ---------------------------------------------------------------------------
// 10. Hán Việt cấp từ (R6-8)
// ---------------------------------------------------------------------------

for (const r of resolved) {
  if (!r.charSyllables) {
    r.hanViet = null;
    r.hanVietStatus = null;
    continue;
  }
  const parts = [];
  let ok = true;
  for (let i = 0; i < r.headChars.length; i++) {
    const c = r.headChars[i];
    const syl = r.charSyllables[i];
    const hv = hanVietByChar.get(c);
    if (!hv) {
      ok = false;
      break;
    }
    let piece;
    if (hv.byPinyin && Object.prototype.hasOwnProperty.call(hv.byPinyin, syl)) {
      piece = hv.byPinyin[syl];
    } else if (hv.readings.length === 1) {
      piece = hv.readings[0];
    } else {
      err(`Không xác định được Hán Việt của chữ "${c}" (âm "${syl}") trong từ "${r.item.head}" (idx ${r.item.idx})`);
      ok = false;
      break;
    }
    if (piece) parts.push(piece);
  }
  if (!ok) continue;
  r.hanViet = parts.join(' ') || null;
  r.hanVietStatus = r.hanViet ? 'derived' : null;
}

exitIfErrors();

// ---------------------------------------------------------------------------
// 11. pathOrder (R6-9)
// ---------------------------------------------------------------------------

function tierOf(r) {
  if (r.hasOld1) return 1;
  if (r.hasNewest1) return 2;
  return 3;
}
const byPathOrder = resolved.slice().sort((a, b) => {
  const t = tierOf(a) - tierOf(b);
  if (t !== 0) return t;
  const fa = a.frequencyRank ?? Number.MAX_SAFE_INTEGER;
  const fb = b.frequencyRank ?? Number.MAX_SAFE_INTEGER;
  if (fa !== fb) return fa - fb;
  return a.item.idx - b.item.idx;
});
byPathOrder.forEach((r, i) => {
  r.pathOrder = i + 1;
});

const tierCounts = { 1: 0, 2: 0, 3: 0 };
for (const r of resolved) tierCounts[tierOf(r)]++;

// ---------------------------------------------------------------------------
// 12–13. Ghi JSON (tất định — không thời điểm build)
// ---------------------------------------------------------------------------

const wordsSorted = resolved.slice().sort((a, b) => a.pathOrder - b.pathOrder);

const words = wordsSorted.map((r) => ({
  officialIndex: r.item.idx,
  simplified: r.item.head,
  traditional: r.traditional,
  variants: r.item.variants,
  pinyin: r.chosen,
  hsk3Level: 1,
  hsk2Level: r.hsk2Level,
  hskExam2026Level: r.hskExam2026Level,
  pathOrder: r.pathOrder,
  frequencyRank: r.frequencyRank,
  pos: r.pos,
  usageNote: r.item.usageNote,
  meaningsEn: r.meaningsEn,
  meaningsVi: r.meaningsVi,
  meaningViStatus: r.meaningViStatus,
  meaningViSource: r.meaningViSource,
  hanViet: r.hanViet,
  hanVietStatus: r.hanVietStatus,
  sources: ['hsk30-official', 'complete-hsk-vocabulary', 'cc-cedict', r.meaningViSource === 'cvdict' ? 'cvdict' : 'machine', 'han-viet-curated'],
}));

const meaningViSourceCounts =
  manualCount > 0
    ? { cvdict: cvdictCount, machine: machineCount, manual: manualCount }
    : { cvdict: cvdictCount, machine: machineCount };

const hskWordsOut = {
  dataset: 'hsk-words',
  version: DATASET_VERSION,
  standard: 'HSK 3.0 (GF0025-2021)',
  license: 'CC-BY-SA-4.0',
  counts: {
    words: words.length,
    byHsk3Level: { 1: words.length },
    meaningViSource: meaningViSourceCounts,
  },
  words,
};

const charactersOut = {
  dataset: 'characters',
  version: DATASET_VERSION,
  license: 'CC-BY-SA-4.0 (Hán Việt); Unicode-3.0 (Unihan)',
  characters,
};

fs.mkdirSync(path.join(DATA_DIR, 'vocabulary'), { recursive: true });
fs.mkdirSync(path.join(DATA_DIR, 'characters'), { recursive: true });
const hskWordsPath = path.join(DATA_DIR, 'vocabulary', 'hsk-words.json');
const charactersPath = path.join(DATA_DIR, 'characters', 'characters.json');
fs.writeFileSync(hskWordsPath, `${JSON.stringify(hskWordsOut, null, 2)}\n`);
fs.writeFileSync(charactersPath, `${JSON.stringify(charactersOut, null, 2)}\n`);

// ---------------------------------------------------------------------------
// 14. Tóm tắt
// ---------------------------------------------------------------------------

console.log(`\nĐã ghi ${path.relative(CHINESE_ROOT, hskWordsPath)}: ${words.length} từ (tầng 1=${tierCounts[1]}, 2=${tierCounts[2]}, 3=${tierCounts[3]})`);
console.log(`Đã ghi ${path.relative(CHINESE_ROOT, charactersPath)}: ${characters.length} chữ`);
console.log(`Nghĩa Việt: cvdict=${cvdictCount}, machine=${machineCount}, manual=${manualCount}`);
printSummaryAndExit();


/** Gọi ở các mốc kiểm tra giữa chừng: có lỗi tích luỹ ⇒ in hết rồi thoát mã 1. Không lỗi ⇒ chạy tiếp
 *  (KHÔNG in warnings ở đây để tránh in trùng nhiều lần — warnings in một lần duy nhất ở printSummaryAndExit). */
function exitIfErrors() {
  if (errors.length === 0) return;
  for (const e of errors) console.error(`✗ LỖI   ${e}`);
  console.error(`\n${errors.length} lỗi. Build THẤT BẠI — sửa xong chạy lại 'yarn --cwd content build:chinese'.`);
  process.exit(1);
}

/** Gọi một lần ở cuối script khi mọi bước đã xong: in toàn bộ warnings tích luỹ, tự chạy validate.mjs
 *  (§7 F6.1 — "build tự gọi validate ở cuối"), rồi thoát theo kết quả validate. */
function printSummaryAndExit() {
  for (const w of warnings) console.warn(`! WARN  ${w}`);
  console.log(`\n${warnings.length} cảnh báo. Build THÀNH CÔNG — chạy validate.mjs để kiểm tra cuối...\n`);
  try {
    execFileSync('node', [path.join(__dirname, 'validate.mjs')], { stdio: 'inherit', cwd: CHINESE_ROOT });
  } catch (err) {
    console.error('\n✗ validate.mjs THẤT BẠI sau khi build — xem lỗi ở trên.');
    process.exit(err.status ?? 1);
  }
}
