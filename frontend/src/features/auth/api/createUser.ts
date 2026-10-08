import { apiRequest } from '../../../lib/api'

export type CreateUserRequest = {
  email: string
  password: string
}

export type UserResponse = {
  id: string
  email: string
}

export function createUser(request: CreateUserRequest) {
  return apiRequest<UserResponse>('/api/users', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}
