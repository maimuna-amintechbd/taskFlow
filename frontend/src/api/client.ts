// The only place in the frontend that performs HTTP. Pages call the small
// functions in src/api/*.ts, which all go through request<T>() below.

const UNREACHABLE_MESSAGE = 'Cannot reach the server. Is the API running?'

/** Field errors keyed by camelCase request property, e.g. { status: ['...'] }. */
export type FieldErrors = Record<string, string[]>

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

function unreachable(): ApiError {
  return new ApiError(0, UNREACHABLE_MESSAGE)
}

/** "$.Status" -> "status", "Name" -> "name". Done once here so pages can match keys to inputs. */
function normalizeKey(key: string): string {
  const withoutPrefix = key.startsWith('$.') ? key.slice(2) : key
  return withoutPrefix.charAt(0).toLowerCase() + withoutPrefix.slice(1)
}

function normalize(errors: unknown): FieldErrors {
  const result: FieldErrors = {}
  if (typeof errors !== 'object' || errors === null) return result
  for (const [key, messages] of Object.entries(errors)) {
    const normalizedKey = normalizeKey(key)
    const list = Array.isArray(messages) ? messages.map(String) : [String(messages)]
    result[normalizedKey] = [...(result[normalizedKey] ?? []), ...list]
  }
  return result
}

interface ProblemBody {
  title?: unknown
  errors?: unknown
}

export async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const res = await fetch(url, init).catch(() => {
    throw unreachable()
  })

  // Parse JSON only when the server says it is JSON; everything else is text.
  const isJson = res.headers.get('content-type')?.includes('json') ?? false
  const body: unknown = isJson ? await res.json() : await res.text()

  if (res.ok) return body as T

  // The Vite proxy answers a plain-text 5xx when the API is not running.
  if (!isJson && res.status >= 500) throw unreachable()

  const problem: ProblemBody = isJson && typeof body === 'object' && body !== null ? body : {}
  const message =
    typeof problem.title === 'string' && problem.title !== ''
      ? problem.title
      : res.statusText || `Request failed with status ${res.status}.`
  throw new ApiError(res.status, message, normalize(problem.errors))
}
