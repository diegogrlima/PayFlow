// Mesmas regras do CreateAccountRequestValidator do backend.
export const HOLDER_NAME_MIN_LENGTH = 3
export const HOLDER_NAME_MAX_LENGTH = 120

export function validateHolderName(value: string): string | undefined {
  const name = value.trim()
  if (!name) return 'Informe o nome do titular.'
  if (name.length < HOLDER_NAME_MIN_LENGTH) return `O nome deve ter pelo menos ${HOLDER_NAME_MIN_LENGTH} caracteres.`
  if (name.length > HOLDER_NAME_MAX_LENGTH) return `O nome deve ter no máximo ${HOLDER_NAME_MAX_LENGTH} caracteres.`
  return undefined
}
