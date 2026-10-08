import { useRef, useState, type FormEvent } from 'react'
import { AlertIcon, CheckIcon, ClockIcon, KeyIcon, LockIcon, Logo, ShieldIcon } from '../components/icons'
import { AccountSetupForm } from '../features/accounts/components/AccountSetupForm'
import type { AccountResponse } from '../features/accounts/api/createAccount'
import { createUser, type UserResponse } from '../features/auth/api/createUser'
import { login } from '../features/auth/api/login'
import { FormField } from '../features/auth/components/FormField'
import { PasswordField } from '../features/auth/components/PasswordField'
import { PasswordRequirements } from '../features/auth/components/PasswordRequirements'
import {
  EMAIL_MAX_LENGTH,
  validateEmail,
  validatePassword,
  validatePasswordConfirmation,
} from '../features/auth/validation'
import { ApiError } from '../lib/api'
import './SignUpPage.css'

type Field = 'email' | 'password' | 'confirmPassword'
type Values = Record<Field, string>
type Errors = Partial<Record<Field, string>>

const initialValues: Values = { email: '', password: '', confirmPassword: '' }
const fieldOrder: Field[] = ['email', 'password', 'confirmPassword']

function validate(values: Values): Errors {
  return {
    email: validateEmail(values.email),
    password: validatePassword(values.password),
    confirmPassword: validatePasswordConfirmation(values.password, values.confirmPassword),
  }
}

// Converte as chaves de erro da API (ex.: "Email") para os campos do formulário.
function mapServerErrors(fieldErrors: Record<string, string[]>): Errors {
  const errors: Errors = {}
  for (const [key, messages] of Object.entries(fieldErrors)) {
    const field = key.toLowerCase() === 'email' ? 'email' : key.toLowerCase() === 'password' ? 'password' : undefined
    if (field && messages.length > 0) errors[field] = messages[0]
  }
  return errors
}

export function SignUpPage() {
  const [values, setValues] = useState<Values>(initialValues)
  const [touched, setTouched] = useState<Partial<Record<Field, boolean>>>({})
  const [submitted, setSubmitted] = useState(false)
  const [serverErrors, setServerErrors] = useState<Errors>({})
  const [formError, setFormError] = useState<string>()
  const [submitting, setSubmitting] = useState(false)
  const [createdUser, setCreatedUser] = useState<UserResponse>()
  // Token mantido só em memória, usado para abrir a conta logo após o cadastro.
  const [accessToken, setAccessToken] = useState<string>()
  const [createdAccount, setCreatedAccount] = useState<AccountResponse>()

  const emailRef = useRef<HTMLInputElement>(null)
  const passwordRef = useRef<HTMLInputElement>(null)
  const confirmPasswordRef = useRef<HTMLInputElement>(null)

  const clientErrors = validate(values)

  function errorFor(field: Field) {
    if (serverErrors[field]) return serverErrors[field]
    return touched[field] || submitted ? clientErrors[field] : undefined
  }

  function update(field: Field, value: string) {
    setValues((current) => ({ ...current, [field]: value }))
    setServerErrors((current) => ({ ...current, [field]: undefined }))
    setFormError(undefined)
  }

  function touch(field: Field) {
    // Só valida no blur se o usuário digitou algo, para não acusar erro ao apenas navegar.
    if (values[field]) setTouched((current) => ({ ...current, [field]: true }))
  }

  function focusFirstInvalid(errors: Errors) {
    const refs = { email: emailRef, password: passwordRef, confirmPassword: confirmPasswordRef }
    const field = fieldOrder.find((name) => errors[name])
    if (field) refs[field].current?.focus()
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (submitting) return

    setSubmitted(true)
    setFormError(undefined)

    if (fieldOrder.some((field) => clientErrors[field])) {
      focusFirstInvalid(clientErrors)
      return
    }

    setSubmitting(true)
    try {
      const credentials = { email: values.email.trim(), password: values.password }
      const user = await createUser(credentials)
      try {
        // O endpoint de contas exige autenticação: entra automaticamente para seguir à etapa 2.
        const session = await login(credentials)
        setAccessToken(session.accessToken)
      } catch {
        // Sem sessão, o usuário cai na tela de sucesso e abre a conta após entrar.
      }
      setValues(initialValues)
      setCreatedUser(user)
    } catch (error) {
      if (error instanceof ApiError && error.status === 400) {
        const mapped = mapServerErrors(error.fieldErrors)
        setServerErrors(mapped)
        focusFirstInvalid(mapped)
        if (Object.keys(mapped).length === 0) setFormError(error.message)
      } else if (error instanceof ApiError && error.status === 422) {
        // Única regra de negócio do cadastro: e-mail já utilizado.
        setServerErrors({ email: error.message })
        emailRef.current?.focus()
      } else {
        setFormError(
          error instanceof ApiError && error.status === 0
            ? error.message
            : 'Não foi possível concluir seu cadastro agora. Tente novamente em instantes.',
        )
      }
    } finally {
      setSubmitting(false)
    }
  }

  function startOver() {
    setValues(initialValues)
    setTouched({})
    setSubmitted(false)
    setServerErrors({})
    setCreatedUser(undefined)
    setAccessToken(undefined)
    setCreatedAccount(undefined)
  }

  return (
    <div className="signup">
      <aside className="signup__brand">
        <div className="brand">
          <Logo />
          <span className="brand__name">PayFlow</span>
        </div>

        <div className="signup__pitch">
          <h2>Seu dinheiro em movimento, com segurança em cada etapa.</h2>
          <p>Abra sua conta digital em poucos minutos e faça transferências usando chaves.</p>
        </div>

        <ul className="trust">
          <li>
            <span className="trust__icon">
              <ShieldIcon />
            </span>
            <div>
              <strong>Senha protegida com Argon2</strong>
              <span>Sua senha nunca é armazenada em texto puro.</span>
            </div>
          </li>
          <li>
            <span className="trust__icon">
              <ClockIcon />
            </span>
            <div>
              <strong>Sessões de curta duração</strong>
              <span>O acesso expira automaticamente após 15 minutos.</span>
            </div>
          </li>
          <li>
            <span className="trust__icon">
              <KeyIcon />
            </span>
            <div>
              <strong>Transferências por chave</strong>
              <span>Envie por e-mail, CPF, CNPJ ou telefone.</span>
            </div>
          </li>
        </ul>
      </aside>

      <main className="signup__main">
        <div className="card">
          {createdAccount ? (
            <section className="success" aria-labelledby="success-title">
              <span className="success__icon">
                <CheckIcon width={28} height={28} />
              </span>
              <h1 id="success-title" tabIndex={-1} ref={(node) => node?.focus()}>
                Conta aberta
              </h1>
              <p>
                A conta {createdAccount.accountType === 'Business' ? 'pessoa jurídica' : 'pessoa física'} de{' '}
                <strong>{createdAccount.holderName}</strong> está pronta para receber depósitos e transferências.
              </p>
              <button type="button" className="button button--secondary" onClick={startOver}>
                Fazer outro cadastro
              </button>
            </section>
          ) : createdUser && accessToken ? (
            <AccountSetupForm accessToken={accessToken} onCreated={setCreatedAccount} />
          ) : createdUser ? (
            <section className="success" aria-labelledby="success-title">
              <span className="success__icon">
                <CheckIcon width={28} height={28} />
              </span>
              <h1 id="success-title" tabIndex={-1} ref={(node) => node?.focus()}>
                Cadastro concluído
              </h1>
              <p>
                Seu acesso foi criado para <strong>{createdUser.email}</strong>. Agora é só entrar com seu e-mail
                e senha para abrir sua conta.
              </p>
              <button type="button" className="button button--secondary" onClick={startOver}>
                Cadastrar outro e-mail
              </button>
            </section>
          ) : (
            <>
              <header className="card__header">
                <span className="step">Etapa 1 de 2 · Acesso</span>
                <h1>Crie seu acesso</h1>
                <p>Use um e-mail que só você acessa. Ele será usado para entrar e para recuperar sua conta.</p>
              </header>

              {formError && (
                <div className="alert" role="alert">
                  <AlertIcon />
                  <p>{formError}</p>
                </div>
              )}

              <form className="form" onSubmit={handleSubmit} noValidate aria-busy={submitting}>
                <FormField
                  id="email"
                  label="E-mail"
                  type="email"
                  inputMode="email"
                  autoComplete="email"
                  autoCapitalize="off"
                  spellCheck={false}
                  placeholder="nome@exemplo.com"
                  maxLength={EMAIL_MAX_LENGTH}
                  value={values.email}
                  onChange={(event) => update('email', event.target.value)}
                  onBlur={() => touch('email')}
                  error={errorFor('email')}
                  inputRef={emailRef}
                  disabled={submitting}
                  required
                />

                <PasswordField
                  id="password"
                  label="Senha"
                  autoComplete="new-password"
                  value={values.password}
                  onChange={(event) => update('password', event.target.value)}
                  onBlur={() => touch('password')}
                  error={errorFor('password')}
                  inputRef={passwordRef}
                  disabled={submitting}
                  hint={<PasswordRequirements password={values.password} />}
                  required
                />

                <PasswordField
                  id="confirmPassword"
                  label="Confirme a senha"
                  autoComplete="new-password"
                  value={values.confirmPassword}
                  onChange={(event) => update('confirmPassword', event.target.value)}
                  onBlur={() => touch('confirmPassword')}
                  error={errorFor('confirmPassword')}
                  inputRef={confirmPasswordRef}
                  disabled={submitting}
                  required
                />

                <button type="submit" className="button button--primary" disabled={submitting}>
                  {submitting ? (
                    <>
                      <span className="spinner" aria-hidden="true" />
                      Criando seu acesso…
                    </>
                  ) : (
                    'Criar acesso'
                  )}
                </button>
              </form>

              <p className="card__footnote">
                <LockIcon width={16} height={16} />
                Nunca pediremos sua senha por telefone, e-mail ou mensagem.
              </p>
            </>
          )}
        </div>
      </main>
    </div>
  )
}
