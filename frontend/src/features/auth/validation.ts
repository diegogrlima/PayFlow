// Mesmas regras do CreateUserRequestValidator do backend.
export const EMAIL_MAX_LENGTH = 255

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export const passwordRules = [
  { id: 'length', label: 'Mínimo de 8 caracteres', test: (value: string) => value.length >= 8 },
  { id: 'upper', label: 'Uma letra maiúscula', test: (value: string) => /[A-Z]/.test(value) },
  { id: 'lower', label: 'Uma letra minúscula', test: (value: string) => /[a-z]/.test(value) },
  { id: 'number', label: 'Um número', test: (value: string) => /[0-9]/.test(value) },
  { id: 'special', label: 'Um caractere especial (ex.: ! @ # $)', test: (value: string) => /[^a-zA-Z0-9]/.test(value) },
] as const

export function validateEmail(value: string): string | undefined {
  const email = value.trim()
  if (!email) return 'Informe seu e-mail.'
  if (email.length > EMAIL_MAX_LENGTH) return `O e-mail deve ter no máximo ${EMAIL_MAX_LENGTH} caracteres.`
  if (!EMAIL_PATTERN.test(email)) return 'Informe um e-mail válido, como nome@exemplo.com.'
  return undefined
}

export function validatePassword(value: string): string | undefined {
  if (!value) return 'Crie uma senha.'
  if (passwordRules.some((rule) => !rule.test(value))) return 'A senha ainda não atende a todos os requisitos.'
  return undefined
}

export function validatePasswordConfirmation(password: string, confirmation: string): string | undefined {
  if (!confirmation) return 'Confirme sua senha.'
  if (password !== confirmation) return 'As senhas não coincidem.'
  return undefined
}
