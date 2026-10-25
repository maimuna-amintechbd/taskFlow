// Hand-written copies of the API's DTOs. Nullable fields are typed `T | null`
// because the API sends null (never omits the field).

export interface ProjectDto {
  id: number
  name: string
  description: string | null
  taskCount: number
}

export interface SaveProjectRequest {
  name: string | null
  description: string | null
}

/** The task statuses, in workflow order. Selects render their options from this array. */
export const TASK_STATUSES = ['Todo', 'InProgress', 'Done'] as const

/** The task priorities, lowest first. Selects render their options from this array. */
export const TASK_PRIORITIES = ['Low', 'Medium', 'High'] as const

/** 'Todo' | 'InProgress' | 'Done', derived from the array so the two never drift apart. */
export type TaskItemStatus = (typeof TASK_STATUSES)[number]

/** 'Low' | 'Medium' | 'High'. */
export type TaskPriority = (typeof TASK_PRIORITIES)[number]

export interface TaskDto {
  id: number
  title: string
  description: string | null
  status: TaskItemStatus
  priority: TaskPriority
  /** A plain 'yyyy-MM-dd' date string; never converted to a JS Date. */
  dueDate: string | null
  projectId: number
  projectName: string
  /** UTC timestamp ending in 'Z'. */
  createdAt: string
}

export interface SaveTaskRequest {
  title: string | null
  description: string | null
  projectId: number | null
  status: TaskItemStatus | null
  priority: TaskPriority | null
  /** 'yyyy-MM-dd' or null. */
  dueDate: string | null
}

/** GET /api/dashboard. total = completed (Done) + pending (Todo + InProgress), across all projects. */
export interface DashboardDto {
  total: number
  completed: number
  pending: number
}
