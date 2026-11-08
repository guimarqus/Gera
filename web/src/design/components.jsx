import { useId } from 'react';
import { AlertTriangle, CheckCircle2, Info } from 'lucide-react';
import './components.css';

export function Button({ children, variant = 'primary', loading = false, className = '', type = 'button', ...props }) {
  return <button className={`ds-button ds-button--${variant} ${className}`} type={type} disabled={props.disabled || loading} aria-busy={loading || undefined} {...props}>
    {loading && <span className="ds-spinner" aria-hidden="true" />}{children}
  </button>;
}

export function Field({ label, error, hint, id, required, children, className = '' }) {
  const generatedId = useId(); const fieldId = id || generatedId;
  return <div className={`ds-field ${className}`}>
    {label && <label className="ds-field__label" htmlFor={fieldId}>{label}{required && <span aria-hidden="true"> *</span>}</label>}
    {children ? children({ id: fieldId, 'aria-invalid': !!error, 'aria-describedby': error ? `${fieldId}-error` : hint ? `${fieldId}-hint` : undefined }) : null}
    {error && <span className="ds-field__error" id={`${fieldId}-error`} role="alert">{error}</span>}
    {!error && hint && <span className="ds-field__hint" id={`${fieldId}-hint`}>{hint}</span>}
  </div>;
}

export function Input({ label, error, hint, id, required, className = '', ...props }) {
  const generatedId = useId(); const fieldId = id || generatedId;
  return <Field label={label} error={error} hint={hint} id={fieldId} required={required}>
    {attrs => <input id={fieldId} className={`ds-input ${error ? 'ds-input--error' : ''} ${className}`} required={required} {...attrs} {...props} />}
  </Field>;
}

export function Card({ children, className = '', ...props }) { return <section className={`ds-card ${className}`} {...props}>{children}</section>; }

export function Badge({ children, tone = 'neutral', className = '' }) { return <span className={`ds-badge ds-badge--${tone} ${className}`}>{children}</span>; }

export function Avatar({ name = 'Usuário', src, size = 'md', className = '' }) {
  const initials = name.trim().split(/\s+/).slice(0, 2).map(part => part[0]).join('').toLocaleUpperCase('pt-BR') || 'G';
  return <span className={`ds-avatar ds-avatar--${size} ${className}`} aria-label={name}>
    {src ? <img src={src} alt="" /> : initials}
  </span>;
}

export function Skeleton({ lines = 3, className = '' }) {
  return <div className={`ds-skeleton ${className}`} role="status" aria-label="Carregando conteúdo">
    {Array.from({ length: lines }, (_, index) => <span className="ds-skeleton__line" key={index} />)}
  </div>;
}

export function EmptyState({ icon: Icon = Info, title, description, action }) {
  return <div className="ds-state">
    <span className="ds-state__icon"><Icon size={24} aria-hidden="true" /></span>
    <h2>{title}</h2><p>{description}</p>{action}
  </div>;
}

export function ErrorState({ title = 'Não foi possível carregar', description = 'Tente novamente em instantes.', onRetry }) {
  return <div className="ds-state ds-state--error">
    <span className="ds-state__icon"><AlertTriangle size={24} aria-hidden="true" /></span>
    <h2>{title}</h2><p>{description}</p>{onRetry && <Button onClick={onRetry} variant="secondary">Tentar de novo</Button>}
  </div>;
}

export function PageHeader({ eyebrow, title, description, action }) {
  return <header className="page-heading">
    <div>{eyebrow && <p className="page-heading__eyebrow">{eyebrow}</p>}<h1>{title}</h1>{description && <p className="page-heading__description">{description}</p>}</div>
    {action && <div className="page-heading__action">{action}</div>}
  </header>;
}

export function Toast({ message, tone = 'success', onDismiss }) {
  if (!message) return null;
  const Icon = tone === 'error' ? AlertTriangle : tone === 'warning' ? AlertTriangle : CheckCircle2;
  return <div className={`ds-toast ds-toast--${tone}`} role={tone === 'error' ? 'alert' : 'status'} aria-live="polite">
    <Icon size={18} aria-hidden="true" /><span>{message}</span>{onDismiss && <Button variant="ghost" onClick={onDismiss} aria-label="Fechar aviso">×</Button>}
  </div>;
}
