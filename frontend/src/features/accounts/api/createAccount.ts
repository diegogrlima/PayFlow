import { apiRequest } from '../../../lib/api'

export type AccountType = 'Individual' | 'Business'

export type CreateAccountRequest = {
  holderName: string
  accountType: AccountType
}

export type AccountResponse = {
  id: string
  userId: string
  holderName: string
  balance: number
  createdAtUtc: string
  accountType: AccountType
  transferKeyType: string | null
  transferKey: string | null
  hasTransferKey: boolean
}

export function createAccount(request: CreateAccountRequest, accessToken: string) {
  return apiRequest<AccountResponse>('/api/accounts', {
    method: 'POST',
    headers: { Authorization: `Bearer ${accessToken}` },
    body: JSON.stringify(request),
  })
}
