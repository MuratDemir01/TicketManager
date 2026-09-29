export type UserRole = 'Admin' | 'Employee'

export type TicketStatus =
  | 'New'
  | 'Assigned'
  | 'InProgress'
  | 'OnHold'
  | 'Resolved'
  | 'Closed'

export type TicketPriority = 'Low' | 'Normal' | 'High' | 'Critical'

export type TicketSortBy =
  | 'CreatedAt'
  | 'TicketNumber'
  | 'Title'
  | 'CustomerName'
  | 'TicketStatus'
  | 'Priority'
  | 'AssignedUserId'

export type SortDirection = 'Asc' | 'Desc'

export interface AuthUser {
  token: string
  userId: string
  userName: string
  email: string
  role: UserRole
}

export interface TicketSummary {
  total: number
  open: number
  critical: number
  resolved: number
  closed: number
}

export interface Ticket {
  id: number
  ticketNumber: string
  title: string
  description: string
  customerName: string
  customerEmail: string
  priority: TicketPriority | number
  ticketStatus: TicketStatus | number
  assignedUserId?: string | null
  createdByUserId?: string | null
  createdAt: string
  updatedAt?: string | null
}

export interface TicketNote {
  id: number
  ticketId: number
  noteText: string
  createdByUserId: string
  createdAt: string
}

export interface TicketHistory {
  id: number
  action: string
  fieldName: string
  oldValue?: string | null
  newValue?: string | null
  changedByUserId: string
  changedAt: string
}

export interface TicketDetail extends Ticket {
  notes: TicketNote[]
  histories: TicketHistory[]
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export interface UserItem {
  id: string
  userName: string
  email: string
  role: UserRole
}

/** API enum'ları sayı veya string dönebilir; UI için normalize eder. */
export const STATUS_LABELS: Record<number, TicketStatus> = {
  0: 'New',
  1: 'Assigned',
  2: 'InProgress',
  3: 'OnHold',
  4: 'Resolved',
  5: 'Closed',
}

export const PRIORITY_LABELS: Record<number, TicketPriority> = {
  0: 'Low',
  1: 'Normal',
  2: 'High',
  3: 'Critical',
}

export const STATUS_TR: Record<TicketStatus, string> = {
  New: 'Yeni',
  Assigned: 'Atandı',
  InProgress: 'İşlemde',
  OnHold: 'Beklemede',
  Resolved: 'Çözüldü',
  Closed: 'Kapatıldı',
}

export const PRIORITY_TR: Record<TicketPriority, string> = {
  Low: 'Düşük',
  Normal: 'Normal',
  High: 'Yüksek',
  Critical: 'Kritik',
}

export function toStatus(value: TicketStatus | number): TicketStatus {
  if (typeof value === 'number') return STATUS_LABELS[value] ?? 'New'
  return value
}

export function toPriority(value: TicketPriority | number): TicketPriority {
  if (typeof value === 'number') return PRIORITY_LABELS[value] ?? 'Normal'
  return value
}

export function statusLabel(value: TicketStatus | number): string {
  return STATUS_TR[toStatus(value)]
}

export function priorityLabel(value: TicketPriority | number): string {
  return PRIORITY_TR[toPriority(value)]
}

export const ALLOWED_TRANSITIONS: Record<TicketStatus, TicketStatus[]> = {
  New: ['Assigned'],
  Assigned: ['InProgress'],
  InProgress: ['OnHold', 'Resolved'],
  OnHold: ['InProgress', 'Resolved'],
  Resolved: ['Closed'],
  Closed: [],
}

export const STATUS_OPTIONS: TicketStatus[] = [
  'New',
  'Assigned',
  'InProgress',
  'OnHold',
  'Resolved',
  'Closed',
]

export const PRIORITY_OPTIONS: TicketPriority[] = [
  'Low',
  'Normal',
  'High',
  'Critical',
]

export function formatDate(value?: string | null): string {
  if (!value) return '—'
  return new Date(value).toLocaleString('tr-TR')
}
