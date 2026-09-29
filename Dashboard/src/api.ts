import type {
  AuthUser,
  PagedResult,
  Ticket,
  TicketDetail,
  TicketNote,
  TicketPriority,
  TicketStatus,
  TicketSummary,
  TicketSortBy,
  SortDirection,
  UserItem,
  UserRole,
} from './types'

const API_BASE =
  import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ||
  'http://localhost:5280'

export class ApiError extends Error {
  status: number
  details?: unknown

  constructor(message: string, status: number, details?: unknown) {
    super(message)
    this.status = status
    this.details = details
  }
}

function getToken(): string | null {
  return localStorage.getItem('tm_token')
}

async function request<T>(
  path: string,
  options: RequestInit = {},
  auth = true,
): Promise<T> {
  const headers = new Headers(options.headers)
  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json')
  }
  if (auth) {
    const token = getToken()
    if (token) headers.set('Authorization', `Bearer ${token}`)
  }

  const res = await fetch(`${API_BASE}${path}`, { ...options, headers })
  if (res.status === 204) return undefined as T

  const text = await res.text()
  let data: unknown = null
  if (text) {
    try {
      data = JSON.parse(text)
    } catch {
      data = text
    }
  }

  if (!res.ok) {
    const message =
      (data as { title?: string; message?: string; detail?: string })?.title ||
      (data as { message?: string })?.message ||
      (data as { detail?: string })?.detail ||
      `İstek başarısız (${res.status})`
    throw new ApiError(message, res.status, data)
  }

  return data as T
}

export const api = {
  login(userNameOrEmail: string, password: string) {
    return request<AuthUser>(
      '/api/auth/login',
      {
        method: 'POST',
        body: JSON.stringify({ userNameOrEmail, password }),
      },
      false,
    ).then((r) => ({
      token: r.token,
      userId: r.userId,
      userName: r.userName,
      email: r.email,
      role: r.role,
    }))
  },

  summary() {
    return request<TicketSummary>('/api/tickets/summary')
  },

  tickets(params: {
    search?: string
    status?: TicketStatus | ''
    priority?: TicketPriority | ''
    assignedUserId?: string
    page?: number
    pageSize?: number
    sort?: TicketSortBy
    direction?: SortDirection
  }) {
    const q = new URLSearchParams()
    if (params.search) q.set('Search', params.search)
    if (params.status) q.set('TicketStatus', params.status)
    if (params.priority) q.set('Priority', params.priority)
    if (params.assignedUserId) q.set('AssignedUserId', params.assignedUserId)
    q.set('Page', String(params.page ?? 1))
    q.set('PageSize', String(params.pageSize ?? 20))
    if (params.sort) q.set('Sort', params.sort)
    if (params.direction) q.set('Direction', params.direction)
    return request<PagedResult<Ticket>>(`/api/tickets?${q}`)
  },

  ticket(id: number) {
    return request<TicketDetail>(`/api/tickets/${id}`)
  },

  async exportCsv(params: {
    search?: string
    status?: TicketStatus | ''
    priority?: TicketPriority | ''
    assignedUserId?: string
    sort?: TicketSortBy
    direction?: SortDirection
  }) {
    const q = new URLSearchParams()
    if (params.search) q.set('Search', params.search)
    if (params.status) q.set('TicketStatus', params.status)
    if (params.priority) q.set('Priority', params.priority)
    if (params.assignedUserId) q.set('AssignedUserId', params.assignedUserId)
    if (params.sort) q.set('Sort', params.sort)
    if (params.direction) q.set('Direction', params.direction)

    const token = getToken()
    const res = await fetch(`${API_BASE}/api/tickets/export?${q}`, {
      headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    })
    if (!res.ok) {
      const text = await res.text()
      throw new ApiError(text || `Dışa aktarma başarısız (${res.status})`, res.status)
    }
    const blob = await res.blob()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download =
      res.headers.get('Content-Disposition')?.match(/filename="?([^"]+)"?/)?.[1] ||
      'tickets.csv'
    a.click()
    URL.revokeObjectURL(url)
  },

  createTicket(body: {
    title: string
    description: string
    customerName: string
    customerEmail: string
    priority: TicketPriority
    assignedUserId?: string | null
  }) {
    return request<Ticket>('/api/tickets', {
      method: 'POST',
      body: JSON.stringify(body),
    })
  },

  importTickets(
    rows: {
      title: string
      description: string
      customerName: string
      customerEmail: string
      priority: TicketPriority
      assignedUserId?: string | null
    }[],
  ) {
    return request<{ createdCount: number; errors: string[] }>(
      '/api/tickets/import',
      {
        method: 'POST',
        body: JSON.stringify({ rows }),
      },
    )
  },

  updateTicket(
    id: number,
    body: {
      title: string
      description: string
      customerName: string
      customerEmail: string
    },
  ) {
    return request<Ticket>(`/api/tickets/${id}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    })
  },

  deleteTicket(id: number) {
    return request<void>(`/api/tickets/${id}`, { method: 'DELETE' })
  },

  changeStatus(id: number, ticketStatus: TicketStatus) {
    return request<Ticket>(`/api/tickets/${id}/status`, {
      method: 'POST',
      body: JSON.stringify({ ticketStatus }),
    })
  },

  changePriority(id: number, priority: TicketPriority) {
    return request<Ticket>(`/api/tickets/${id}/priority`, {
      method: 'POST',
      body: JSON.stringify({ priority }),
    })
  },

  assign(id: number, assignedUserId: string | null) {
    return request<Ticket>(`/api/tickets/${id}/assign`, {
      method: 'POST',
      body: JSON.stringify({ assignedUserId }),
    })
  },

  addNote(id: number, noteText: string) {
    return request<TicketNote>(`/api/tickets/${id}/notes`, {
      method: 'POST',
      body: JSON.stringify({ noteText }),
    })
  },

  users() {
    return request<UserItem[]>('/api/users')
  },

  createUser(body: {
    userName: string
    email: string
    password: string
    role: UserRole
  }) {
    return request<UserItem>('/api/users', {
      method: 'POST',
      body: JSON.stringify(body),
    })
  },
}
