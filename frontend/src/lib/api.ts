const API_URL = (import.meta.env.VITE_API_URL ?? 'http://localhost:5048').replace(/\/$/, '')

export type FieldErrors = Record<string, string[]>

type ProblemDetails = {
  title?: string
  status?: number
  detail?: string
  errors?: FieldErrors
  erros?: FieldErrors
}

export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: FieldErrors

  constructor(status: number, message: string, fieldErrors: FieldErrors = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  let response: Response

  try {
    response = await fetch(`${API_URL}${path}`, {
      ...init,
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/json',
        ...init.headers,
      },
    })
  } catch {
    throw new ApiError(0, 'Não foi possível conectar ao PayFlow. Verifique sua conexão e tente novamente.')
  }

  if (response.ok) {
    return (response.status === 204 ? undefined : await response.json()) as T
  }

  const problem = (await response.json().catch(() => ({}))) as ProblemDetails
  // A API envia os erros de validação em "erros"; "errors" é o padrão do ASP.NET.
  const fieldErrors = problem.erros ?? problem.errors ?? {}
  const message =
    problem.detail ??
    problem.title ??
    'Algo deu errado do nosso lado. Tente novamente em instantes.'

  throw new ApiError(response.status, message, fieldErrors)
}
