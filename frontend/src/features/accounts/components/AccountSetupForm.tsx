import { useRef, useState, type FormEvent, type ReactNode } from 'react'
import { AlertIcon, BuildingIcon, CheckIcon, UserIcon } from '../../../components/icons'
import { ApiError } from '../../../lib/api'
import { FormField } from '../../auth/components/FormField'
import { createAccount, type AccountResponse, type AccountType } from '../api/createAccount'
import { HOLDER_NAME_MAX_LENGTH, validateHolderName } from '../validation'

type AccountSetupFormProps = {
  accessToken: string
  onCreated: (account: AccountResponse) => void
}

const accountTypes: { value: AccountType; title: string; description: string; icon: ReactNode }[] = [
  {
    value: 'Individual',
    title: 'Pessoa física',
    description: 'Para uso pessoal, no seu nome.',
    icon: <UserIcon />,
  },
  {
    value: 'Business',
    title: 'Pessoa jurídica',
    description: 'Para a sua empresa ou negócio.',
    icon: <BuildingIcon />,
  },
]

export function AccountSetupForm({ accessToken, onCreated }: AccountSetupFormProps) {
  const [holderName, setHolderName] = useState('')
  const [accountType, setAccountType] = useState<AccountType>()
  const [touched, setTouched] = useState(false)
  const [submitted, setSubmitted] = useState(false)
  const [serverErrors, setServerErrors] = useState<{ holderName?: string; accountType?: string }>({})
  const [formError, setFormError] = useState<string>()
  const [submitting, setSubmitting] = useState(false)

  const holderNameRef = useRef<HTMLInputElement>(null)
  const accountTypeRef = useRef<HTMLFieldSetElement>(null)

  const holderNameError =
    serverErrors.holderName ?? (touched || submitted ? validateHolderName(holderName) : undefined)
  const accountTypeError =
    serverErrors.accountType ?? (submitted && !accountType ? 'Escolha o tipo da conta.' : undefined)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (submitting) return

    setSubmitted(true)
    setFormError(undefined)

    if (validateHolderName(holderName)) {
      holderNameRef.current?.focus()
      return
    }
    if (!accountType) {
      accountTypeRef.current?.querySelector('input')?.focus()
      return
    }

    setSubmitting(true)
    try {
      onCreated(await createAccount({ holderName: holderName.trim(), accountType }, accessToken))
    } catch (error) {
      if (error instanceof ApiError && error.status === 400) {
        const errors = {
          holderName: error.fieldErrors.HolderName?.[0],
          accountType: error.fieldErrors.AccountType?.[0],
        }
        setServerErrors(errors)
        if (errors.holderName) holderNameRef.current?.focus()
        else if (!errors.accountType) setFormError(error.message)
      } else if (error instanceof ApiError && error.status === 401) {
        setFormError('Sua sessão expirou. Entre novamente com seu e-mail e senha para abrir sua conta.')
      } else {
        setFormError(
          error instanceof ApiError && error.status === 0
            ? error.message
            : 'Não foi possível abrir sua conta agora. Tente novamente em instantes.',
        )
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <>
      <header className="card__header">
        <span className="step">Etapa 2 de 2 · Conta</span>
        <h1 tabIndex={-1} ref={(node) => node?.focus()}>
          Configure sua conta
        </h1>
        <p>Seu acesso foi criado. Agora diga em nome de quem a conta será aberta e como você vai usá-la.</p>
      </header>

      {formError && (
        <div className="alert" role="alert">
          <AlertIcon />
          <p>{formError}</p>
        </div>
      )}

      <form className="form" onSubmit={handleSubmit} noValidate aria-busy={submitting}>
        <FormField
          id="holderName"
          label="Nome do titular"
          autoComplete="name"
          placeholder="Como aparece no seu documento"
          maxLength={HOLDER_NAME_MAX_LENGTH}
          value={holderName}
          onChange={(event) => {
            setHolderName(event.target.value)
            setServerErrors((current) => ({ ...current, holderName: undefined }))
            setFormError(undefined)
          }}
          onBlur={() => holderName && setTouched(true)}
          error={holderNameError}
          inputRef={holderNameRef}
          disabled={submitting}
          required
        />

        <fieldset
          ref={accountTypeRef}
          className={`choice${accountTypeError ? ' choice--invalid' : ''}`}
          aria-describedby={accountTypeError ? 'accountType-error' : undefined}
          disabled={submitting}
        >
          <legend className="field__label">Tipo de conta</legend>
          <div className="choice__options">
            {accountTypes.map((option) => (
              <label key={option.value} className="choice__option">
                <input
                  type="radio"
                  name="accountType"
                  value={option.value}
                  checked={accountType === option.value}
                  onChange={() => {
                    setAccountType(option.value)
                    setServerErrors((current) => ({ ...current, accountType: undefined }))
                    setFormError(undefined)
                  }}
                />
                <span className="choice__icon">{option.icon}</span>
                <span className="choice__text">
                  <strong>{option.title}</strong>
                  <span>{option.description}</span>
                </span>
                <span className="choice__check">
                  <CheckIcon width={14} height={14} />
                </span>
              </label>
            ))}
          </div>
          {accountTypeError && (
            <p className="field__error" id="accountType-error">
              <AlertIcon width={16} height={16} />
              {accountTypeError}
            </p>
          )}
        </fieldset>

        <button type="submit" className="button button--primary" disabled={submitting}>
          {submitting ? (
            <>
              <span className="spinner" aria-hidden="true" />
              Abrindo sua conta…
            </>
          ) : (
            'Abrir conta'
          )}
        </button>
      </form>
    </>
  )
}
