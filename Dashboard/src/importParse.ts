import * as XLSX from 'xlsx'
import type { TicketPriority } from './types'

export interface ImportTicketRow {
  title: string
  description: string
  customerName: string
  customerEmail: string
  priority: TicketPriority
  assignedUserId?: string | null
}

export interface ParseResult {
  rows: ImportTicketRow[]
  errors: string[]
}

/** Keys are output of normHeader (ASCII, no spaces). */
const HEADER_ALIASES: Record<string, string> = {
  title: 'title',
  baslik: 'title',
  description: 'description',
  aciklama: 'description',
  customername: 'customerName',
  musteri: 'customerName',
  musteriadi: 'customerName',
  customeremail: 'customerEmail',
  email: 'customerEmail',
  musterieposta: 'customerEmail',
  priority: 'priority',
  oncelik: 'priority',
  assigneduserid: 'assignedUserId',
  assigned: 'assignedUserId',
  atanan: 'assignedUserId',
  atananid: 'assignedUserId',
}

function normHeader(h: string): string {
  return h
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9]/g, '')
}

function mapHeader(raw: string): string | null {
  const key = normHeader(raw)
  return HEADER_ALIASES[key] ?? null
}

function parsePriority(raw: unknown): TicketPriority | null {
  if (raw == null || raw === '') return null
  const s = String(raw).trim()
  const lower = s.toLowerCase()
  const map: Record<string, TicketPriority> = {
    low: 'Low',
    normal: 'Normal',
    high: 'High',
    critical: 'Critical',
    '0': 'Low',
    '1': 'Normal',
    '2': 'High',
    '3': 'Critical',
    düşük: 'Low',
    dusuk: 'Low',
    yüksek: 'High',
    yuksek: 'High',
    kritik: 'Critical',
  }
  return map[lower] ?? map[s] ?? null
}

function cellStr(v: unknown): string {
  if (v == null) return ''
  return String(v).trim()
}

function rowsFromMatrix(matrix: unknown[][]): ParseResult {
  const errors: string[] = []
  if (matrix.length < 2) {
    return { rows: [], errors: ['Dosyada başlık satırı ve en az bir veri satırı olmalı.'] }
  }

  const headerCells = (matrix[0] ?? []).map((c) => cellStr(c))
  const colIndex: Partial<Record<string, number>> = {}
  headerCells.forEach((h, i) => {
    const mapped = mapHeader(h)
    if (mapped && colIndex[mapped] == null) colIndex[mapped] = i
  })

  const required = ['title', 'description', 'customerName', 'customerEmail', 'priority'] as const
  for (const r of required) {
    if (colIndex[r] == null) {
      errors.push(`Zorunlu kolon eksik: ${r}`)
    }
  }
  if (errors.length) return { rows: [], errors }

  const rows: ImportTicketRow[] = []
  for (let i = 1; i < matrix.length; i++) {
    const line = matrix[i] ?? []
    const isEmpty = line.every((c) => cellStr(c) === '')
    if (isEmpty) continue

    const title = cellStr(line[colIndex.title!])
    const description = cellStr(line[colIndex.description!])
    const customerName = cellStr(line[colIndex.customerName!])
    const customerEmail = cellStr(line[colIndex.customerEmail!])
    const priorityRaw = line[colIndex.priority!]
    const assigned =
      colIndex.assignedUserId != null
        ? cellStr(line[colIndex.assignedUserId]) || null
        : null

    const lineNo = i + 1
    if (!title || !description || !customerName || !customerEmail) {
      errors.push(`Satır ${lineNo}: Title, Description, CustomerName, CustomerEmail zorunlu.`)
      continue
    }
    const priority = parsePriority(priorityRaw)
    if (!priority) {
      errors.push(
        `Satır ${lineNo}: Geçersiz öncelik (Low/Normal/High/Critical veya Düşük/Normal/Yüksek/Kritik).`,
      )
      continue
    }

    rows.push({
      title,
      description,
      customerName,
      customerEmail,
      priority,
      assignedUserId: assigned,
    })
  }

  if (rows.length > 200) {
    return {
      rows: [],
      errors: ['Tek seferde en fazla 200 satır aktarılabilir.'],
    }
  }

  if (!rows.length && !errors.length) {
    errors.push('Aktarılacak geçerli satır bulunamadı.')
  }

  return { rows, errors }
}

function parseCsvText(text: string): unknown[][] {
  const rows: string[][] = []
  let row: string[] = []
  let cell = ''
  let inQuotes = false

  for (let i = 0; i < text.length; i++) {
    const ch = text[i]
    const next = text[i + 1]
    if (inQuotes) {
      if (ch === '"' && next === '"') {
        cell += '"'
        i++
      } else if (ch === '"') {
        inQuotes = false
      } else {
        cell += ch
      }
    } else if (ch === '"') {
      inQuotes = true
    } else if (ch === ',') {
      row.push(cell)
      cell = ''
    } else if (ch === '\n' || (ch === '\r' && next === '\n')) {
      row.push(cell)
      rows.push(row)
      row = []
      cell = ''
      if (ch === '\r') i++
    } else if (ch === '\r') {
      row.push(cell)
      rows.push(row)
      row = []
      cell = ''
    } else {
      cell += ch
    }
  }
  if (cell.length || row.length) {
    row.push(cell)
    rows.push(row)
  }
  // BOM
  if (rows[0]?.[0]?.charCodeAt(0) === 0xfeff) {
    rows[0][0] = rows[0][0].slice(1)
  }
  return rows
}

export async function parseImportFile(file: File): Promise<ParseResult> {
  const name = file.name.toLowerCase()
  if (name.endsWith('.csv') || name.endsWith('.txt')) {
    const text = await file.text()
    return rowsFromMatrix(parseCsvText(text))
  }
  if (name.endsWith('.xlsx') || name.endsWith('.xls')) {
    const buf = await file.arrayBuffer()
    const wb = XLSX.read(buf, { type: 'array' })
    const sheet = wb.Sheets[wb.SheetNames[0]]
    if (!sheet) return { rows: [], errors: ['Excel dosyasında sayfa yok.'] }
    const matrix = XLSX.utils.sheet_to_json<unknown[]>(sheet, {
      header: 1,
      defval: '',
      raw: false,
    }) as unknown[][]
    return rowsFromMatrix(matrix)
  }
  return {
    rows: [],
    errors: ['Desteklenen biçimler: .csv, .xlsx, .xls'],
  }
}
