import { useState, type ComponentProps, type KeyboardEvent } from 'react'
import { EyeIcon, EyeOffIcon } from '../../../components/icons'
import { FormField } from './FormField'

type PasswordFieldProps = Omit<ComponentProps<typeof FormField>, 'type' | 'trailing'>

export function PasswordField({ hint, onKeyUp, onBlur, ...props }: PasswordFieldProps) {
  const [visible, setVisible] = useState(false)
  const [capsLock, setCapsLock] = useState(false)

  function handleKeyUp(event: KeyboardEvent<HTMLInputElement>) {
    setCapsLock(event.getModifierState('CapsLock'))
    onKeyUp?.(event)
  }

  return (
    <FormField
      {...props}
      type={visible ? 'text' : 'password'}
      autoCapitalize="off"
      autoCorrect="off"
      spellCheck={false}
      onKeyUp={handleKeyUp}
      onBlur={(event) => {
        setCapsLock(false)
        onBlur?.(event)
      }}
      trailing={
        <button
          type="button"
          className="field__toggle"
          onClick={() => setVisible((current) => !current)}
          aria-pressed={visible}
          aria-controls={props.id}
          aria-label={visible ? 'Ocultar senha' : 'Mostrar senha'}
        >
          {visible ? <EyeOffIcon /> : <EyeIcon />}
        </button>
      }
      hint={
        <>
          {capsLock && (
            <p className="field__caps" role="status">
              Caps Lock está ativado.
            </p>
          )}
          {hint}
        </>
      }
    />
  )
}
