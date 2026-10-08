import { CheckIcon, DotIcon } from '../../../components/icons'
import { passwordRules } from '../validation'

// Indexado pela quantidade de requisitos atendidos; "Forte" só quando todos forem cumpridos.
const strengthLabels = ['Muito fraca', 'Muito fraca', 'Fraca', 'Razoável', 'Quase lá', 'Forte']

export function PasswordRequirements({ password }: { password: string }) {
  const results = passwordRules.map((rule) => ({ ...rule, met: rule.test(password) }))
  const metCount = results.filter((rule) => rule.met).length
  const level = password ? metCount : 0

  return (
    <div className="requirements">
      <div className="strength" data-level={level}>
        <div className="strength__bar" aria-hidden="true">
          {passwordRules.map((rule, index) => (
            <span key={rule.id} className={index < level ? 'is-filled' : undefined} />
          ))}
        </div>
        <span className="strength__label">
          {password ? `Força da senha: ${strengthLabels[metCount]}` : 'Sua senha precisa ter:'}
        </span>
      </div>
      <ul className="requirements__list">
        {results.map((rule) => (
          <li key={rule.id} className={rule.met ? 'is-met' : undefined}>
            {rule.met ? <CheckIcon width={16} height={16} /> : <DotIcon width={16} height={16} />}
            <span>
              {rule.label}
              <span className="visually-hidden">{rule.met ? ' — atendido' : ' — pendente'}</span>
            </span>
          </li>
        ))}
      </ul>
    </div>
  )
}
