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
