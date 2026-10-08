import type { InputHTMLAttributes, ReactNode, Ref } from 'react'
import { AlertIcon } from '../../../components/icons'

type FormFieldProps = InputHTMLAttributes<HTMLInputElement> & {
  id: string
  label: string
  error?: string
  hint?: ReactNode
  trailing?: ReactNode
  inputRef?: Ref<HTMLInputElement>
}

export function FormField({ id, label, error, hint, trailing, inputRef, ...inputProps }: FormFieldProps) {
  const hintId = hint ? `${id}-hint` : undefined
  const errorId = error ? `${id}-error` : undefined
  const describedBy = [errorId, hintId].filter(Boolean).join(' ') || undefined

  return (
    <div className={`field${error ? ' field--invalid' : ''}`}>
      <label className="field__label" htmlFor={id}>
        {label}
      </label>
      <div className="field__control">
        <input
          ref={inputRef}
          id={id}
          className="field__input"
          aria-invalid={error ? true : undefined}
          aria-describedby={describedBy}
          {...inputProps}
        />
        {trailing}
      </div>
      {error && (
        <p className="field__error" id={errorId}>
          <AlertIcon width={16} height={16} />
          {error}
        </p>
      )}
      {hint && (
        <div className="field__hint" id={hintId}>
          {hint}
        </div>
      )}
    </div>
  )
}
