#!/usr/bin/env node
// scripts/check-ui-conventions.mjs — CỔNG CHẶN quy ước giao diện (F1, hợp đồng
// `docs/agent-workflow/2026-09-16-antfarm-nen-tang-tieng-trung-mvp-hop-dong-thuc-thi.md` §5.3.0.6).
//
// Bê ý tưởng từ `mdt-re-construct/mdt-frontend/scripts/check-ui-conventions.mjs` nhưng viết gọn, TỰ CHỨA
// (không tách lib) và chỉ dùng Node chuẩn. Quét `apps/*/src/**/*.{ts,tsx}` (mặc định) hoặc (các) đường
// dẫn truyền vào.
//
//   FAIL (exit 1):
//     raw-dialog                     — import `Dialog`/`Drawer`/`SwipeableDrawer` từ '@mui/material' (kể cả
//                                      đường dẫn con '@mui/material/Dialog') trong `apps/`. Phải dùng
//                                      `AppDialog`/`AppDrawer` của '@af/ui' (có từ F5). `packages/ui` được miễn
//                                      vì chính nó là nơi bọc thẻ MUI.
//     uuid-import                    — `from 'uuid'` — dùng `crypto.randomUUID()`.
//     autocomplete-slotprops-override — trong `renderInput` có `{...params}` rồi `slotProps={{` mà khối không mở
//                                      đầu bằng `...params.slotProps` ⇒ mất ref của <input>, ô Autocomplete không
//                                      bao giờ hiện gợi ý (sự cố MedDental 29/08/2026, không một lỗi nào nổi lên).
//   WARN (không đổi exit code):
//     tabs-no-url                    — `<Tabs` trong file không có `useTabParam` và không nằm trong
//                                      `AppDialog`/`AppDrawer` — tab cấp trang phải lên URL; tab trong hộp thoại
//                                      hoặc control tái dùng thì hợp lệ, RÀ TAY.
//
// Dùng:  node scripts/check-ui-conventions.mjs [duongdan...] [--json]
// Exit: 0 sạch FAIL · 1 có FAIL · 2 tham số sai (đường dẫn không tồn tại).

import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const ROOT = path.resolve(__dirname, '..') // frontend/

const rawArgs = process.argv.slice(2)
const asJson = rawArgs.includes('--json')
const targets = rawArgs.filter((a) => a !== '--json')

const RAW_TAGS = ['Dialog', 'Drawer', 'SwipeableDrawer']
const DIALOG_WRAP_TAGS = ['AppDialog', 'AppDrawer']

// ─── Liệt kê file .ts/.tsx dưới một thư mục (đệ quy, bỏ node_modules/dist) ───
function listSourceFiles(dir) {
  if (!fs.existsSync(dir)) return []
  const out = []
  const walk = (d) => {
    for (const e of fs.readdirSync(d, { withFileTypes: true })) {
      if (e.name === 'node_modules' || e.name === 'dist') continue
      const p = path.join(d, e.name)
      if (e.isDirectory()) walk(p)
      else if (/\.(ts|tsx)$/.test(e.name) && !e.name.endsWith('.d.ts')) out.push(p)
    }
  }
  walk(dir)
  return out.sort()
}

// Đường dẫn tương đối trong tham số tính từ process.cwd() — vì `apps/<app>/package.json` gọi
// `node ../../scripts/check-ui-conventions.mjs src` với cwd = thư mục app. Không truyền gì ⇒ quét apps/*/src.
function resolveFiles() {
  if (targets.length === 0) {
    const appsDir = path.join(ROOT, 'apps')
    if (!fs.existsSync(appsDir)) return []
    return fs
      .readdirSync(appsDir, { withFileTypes: true })
      .filter((e) => e.isDirectory())
      .flatMap((e) => listSourceFiles(path.join(appsDir, e.name, 'src')))
  }
  const files = []
  for (const t of targets) {
    const abs = path.isAbsolute(t) ? t : path.resolve(process.cwd(), t)
    if (!fs.existsSync(abs)) {
      console.error(`Không tìm thấy đường dẫn: ${t}`)
      process.exit(2)
    }
    if (fs.statSync(abs).isDirectory()) files.push(...listSourceFiles(abs))
    else if (/\.(ts|tsx)$/.test(abs)) files.push(abs)
  }
  return [...new Set(files)].sort()
}

const rel = (p) => path.relative(ROOT, p).replace(/\\/g, '/')
const lineOf = (content, index) => content.slice(0, index).split('\n').length
const isUiPackageFile = (p) => rel(p).startsWith('packages/ui/')

// ─── File có import `name` TỪ `source` dạng named, không bí danh? Trả vị trí để báo dòng. ───
function findNamedImport(content, name, source) {
  const escaped = source.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
  // `[^}]*`: danh sách specifier không chứa `}` — chặn cứng ở `}` đầu tiên để không nuốt sang import khác.
  const re = new RegExp(`import\\s+(?:type\\s+)?\\{([^}]*)\\}\\s*from\\s*(['"])${escaped}\\2`, 'g')
  let m
  while ((m = re.exec(content))) {
    for (const spec of m[1].split(',').map((s) => s.trim()).filter(Boolean)) {
      const [base] = spec.split(/\s+as\s+/)
      if (base.trim() === name) return m.index
    }
  }
  return -1
}

// ─── Cặp thẻ <tag>…</tag> theo ngăn xếp; cấu trúc lệch ⇒ null (bên gọi bỏ qua, không đoán mò) ───
function findTagPairs(content, tag) {
  const re = new RegExp(`<${tag}(?![A-Za-z0-9_])|</${tag}>`, 'g')
  const stack = []
  const pairs = []
  let m
  while ((m = re.exec(content))) {
    if (m[0].startsWith('</')) {
      const open = stack.pop()
      if (!open) return null
      pairs.push([open.index, m.index + m[0].length])
    } else stack.push(m)
  }
  return stack.length ? null : pairs
}

// ─── Luật raw-dialog ───
function checkRawDialog(content, file) {
  if (isUiPackageFile(file)) return []
  const errors = []
  for (const tag of RAW_TAGS) {
    let at = findNamedImport(content, tag, '@mui/material')
    if (at < 0) {
      // Import mặc định theo đường dẫn con: import Dialog from '@mui/material/Dialog'
      const re = new RegExp(`import\\s+${tag}\\s+from\\s*['"]@mui/material/${tag}['"]`)
      const m = re.exec(content)
      at = m ? m.index : -1
    }
    if (at < 0) continue
    errors.push({
      rule: 'raw-dialog',
      file,
      line: lineOf(content, at),
      message: `<${tag}> import từ '@mui/material' — phải dùng App${tag === 'SwipeableDrawer' ? 'Drawer' : tag} của '@af/ui'.`,
    })
  }
  return errors
}

// ─── Luật uuid-import ───
function checkUuidImport(content, file) {
  const m = /from\s*['"]uuid['"]/.exec(content)
  if (!m) return []
  return [
    {
      rule: 'uuid-import',
      file,
      line: lineOf(content, m.index),
      message: `Không dùng package 'uuid' — dùng crypto.randomUUID() có sẵn.`,
    },
  ]
}

// ─── Luật autocomplete-slotprops-override ───
// Với mỗi `renderInput={ ... }`: lấy nguyên khối theo độ sâu ngoặc, đọc tên tham số (mặc định `params`),
// nếu khối có `{...params}` VÀ `slotProps={{` mà ngay sau `slotProps={{` không phải `...params.slotProps` ⇒ FAIL.
function checkAutocompleteSlotProps(content, file) {
  const errors = []
  const re = /renderInput\s*=\s*\{/g
  let m
  while ((m = re.exec(content))) {
    const start = m.index + m[0].length - 1 // vị trí dấu `{`
    let depth = 0
    let end = -1
    for (let i = start; i < content.length; i++) {
      const ch = content[i]
      if (ch === '{') depth++
      else if (ch === '}') {
        depth--
        if (depth === 0) {
          end = i
          break
        }
      }
    }
    if (end < 0) continue
    const block = content.slice(start, end + 1)
    const paramMatch = /^\{\s*(?:async\s*)?\(?\s*([A-Za-z_$][\w$]*)\s*(?::[^)=]*)?\)?\s*=>/.exec(block)
    const param = paramMatch ? paramMatch[1] : 'params'
    const spreadRe = new RegExp(`\\{\\s*\\.\\.\\.${param}\\s*\\}`)
    if (!spreadRe.test(block)) continue
    const slotRe = /slotProps\s*=\s*\{\s*\{/g
    let s
    while ((s = slotRe.exec(block))) {
      const after = block.slice(s.index + s[0].length).replace(/^\s+/, '')
      if (after.startsWith(`...${param}.slotProps`)) continue
      errors.push({
        rule: 'autocomplete-slotprops-override',
        file,
        line: lineOf(content, start + s.index),
        message:
          `renderInput trải {...${param}} rồi ghi đè slotProps mà không mở đầu bằng ...${param}.slotProps — ` +
          `mất ref của <input>, Autocomplete không bao giờ hiện gợi ý. Viết ` +
          `slotProps={{ ...${param}.slotProps, inputLabel: { ...${param}.slotProps?.inputLabel, ... } }}.`,
      })
    }
  }
  return errors
}

// ─── Luật tabs-no-url (WARN) ───
function checkTabsNoUrl(content, file) {
  if (!/<Tabs(?![A-Za-z0-9_])/.test(content)) return []
  if (/\buseTabParam\b/.test(content)) return []
  const wrapRanges = DIALOG_WRAP_TAGS.flatMap((t) => findTagPairs(content, t) ?? [])
  const warnings = []
  const re = /<Tabs(?![A-Za-z0-9_])/g
  let m
  while ((m = re.exec(content))) {
    if (wrapRanges.some(([a, b]) => m.index >= a && m.index < b)) continue
    warnings.push({
      rule: 'tabs-no-url',
      file,
      line: lineOf(content, m.index),
      message:
        `<Tabs> không thấy useTabParam và không nằm trong AppDialog/AppDrawer — tab cấp trang phải lên URL ` +
        `(useTabParam của '@af/ui'); control tái dùng/tab trong hộp thoại thì hợp lệ, RÀ TAY.`,
    })
  }
  return warnings
}

function main() {
  const files = resolveFiles()
  const errors = []
  const warnings = []
  for (const file of files) {
    const content = fs.readFileSync(file, 'utf8')
    errors.push(...checkRawDialog(content, file), ...checkUuidImport(content, file), ...checkAutocompleteSlotProps(content, file))
    warnings.push(...checkTabsNoUrl(content, file))
  }

  if (asJson) {
    console.log(
      JSON.stringify(
        {
          filesScanned: files.length,
          errorCount: errors.length,
          warningCount: warnings.length,
          errors: errors.map((e) => ({ ...e, file: rel(e.file) })),
          warnings: warnings.map((w) => ({ ...w, file: rel(w.file) })),
        },
        null,
        2,
      ),
    )
  } else {
    console.log(`check-ui-conventions — quét ${files.length} file .ts/.tsx`)
    if (errors.length) {
      console.log('\nLỖI (chặn build):')
      for (const e of errors) console.log(`  [${e.rule}] ${rel(e.file)}:${e.line} — ${e.message}`)
    }
    if (warnings.length) {
      console.log('\nCẢNH BÁO (không chặn build, cần rà tay):')
      for (const w of warnings) console.log(`  [${w.rule}] ${rel(w.file)}:${w.line} — ${w.message}`)
    }
    console.log(`\nTổng kết: ${errors.length} lỗi, ${warnings.length} cảnh báo trên ${files.length} file.`)
    if (!errors.length) console.log('✔ Sạch các luật FAIL (raw-dialog, uuid-import, autocomplete-slotprops-override).')
  }
  process.exit(errors.length ? 1 : 0)
}

main()
