'use client';

import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import {
  useForm,
  type DefaultValues,
  type FieldValues,
  type Path,
  type Resolver,
} from 'react-hook-form';
import type { ZodType } from 'zod';
import { ContractAlert } from '@/components/shared/contract-alert';
import { StatusBadge } from '@/components/shared/status-badge';
import { ApiError } from '@/services/http-client';

export interface FormFieldOption {
  label: string;
  value: string;
}

export interface FormFieldConfig<TForm extends FieldValues> {
  name: Path<TForm>;
  label: string;
  description?: string;
  placeholder?: string;
  kind?: 'text' | 'textarea' | 'number' | 'select';
  options?: FormFieldOption[];
  inputMode?: React.HTMLAttributes<HTMLInputElement>['inputMode'];
  step?: string;
  min?: number;
}

interface ActionFormCardProps<TForm extends FieldValues, TResult> {
  title: string;
  description?: string;
  schema: ZodType<TForm>;
  defaultValues: DefaultValues<TForm>;
  fields: FormFieldConfig<TForm>[];
  submitLabel: string;
  notes?: string[];
  contractWarnings?: string[];
  onSubmit: (values: TForm) => Promise<TResult>;
  onSettled?: (payload: {
    status: 'success' | 'error';
    title: string;
    message: string;
    correlationId?: string;
  }) => void;
  renderResult?: (result: TResult) => ReactNode;
}

function readError(error: unknown): { message: string; correlationId?: string } {
  if (error instanceof ApiError) {
    const correlationId =
      typeof error.payload === 'object' &&
      error.payload !== null &&
      'correlationId' in error.payload &&
      typeof error.payload.correlationId === 'string'
        ? error.payload.correlationId
        : undefined;

    const message =
      typeof error.payload === 'object' &&
      error.payload !== null &&
      'error' in error.payload &&
      typeof error.payload.error === 'string'
        ? error.payload.error
        : error.message;

    return { message, correlationId };
  }

  if (error instanceof Error) {
    return { message: error.message };
  }

  return { message: 'Se produjo un error inesperado al comunicarse con el backend.' };
}

function readSuccess(result: unknown): { message: string; correlationId?: string } {
  if (typeof result === 'object' && result !== null) {
    const message =
      'message' in result && typeof result.message === 'string'
        ? result.message
        : 'La accion se completo correctamente';
    const correlationId =
      'correlationId' in result && typeof result.correlationId === 'string'
        ? result.correlationId
        : undefined;

    return { message, correlationId };
  }

  return { message: 'La accion se completo correctamente' };
}

export function ActionFormCard<TForm extends FieldValues, TResult>({
  title,
  description,
  schema,
  defaultValues,
  fields,
  submitLabel,
  notes = [],
  contractWarnings = [],
  onSubmit,
  onSettled,
  renderResult,
}: ActionFormCardProps<TForm, TResult>) {
  const form = useForm<TForm>({
    resolver: zodResolver(schema as never) as Resolver<TForm>,
    defaultValues,
  });

  const mutation = useMutation({
    mutationFn: onSubmit,
    onSuccess(result) {
      const success = readSuccess(result);
      onSettled?.({
        status: 'success',
        title,
        message: success.message,
        correlationId: success.correlationId,
      });
    },
    onError(error) {
      const failure = readError(error);
      onSettled?.({
        status: 'error',
        title,
        message: failure.message,
        correlationId: failure.correlationId,
      });
    },
  });

  return (
    <section className="operation-card clinical-panel">
      <div className="operation-card__header">
        <div>
          <div className="panel__eyebrow">Accion operativa</div>
          <h2>{title}</h2>
          {description ? <p>{description}</p> : null}
        </div>
        <StatusBadge tone="neutral">Operacion</StatusBadge>
      </div>

      <ContractAlert title="Importante antes de continuar" items={contractWarnings} />

      <form
        className="form-grid"
        onSubmit={form.handleSubmit(async (values) => {
          await mutation.mutateAsync(values as TForm);
        })}
      >
        {fields.map((field) => {
          const fieldError = form.formState.errors[field.name];
          const baseProps = {
            id: String(field.name),
            placeholder: field.placeholder,
            'aria-invalid': Boolean(fieldError),
            ...form.register(field.name, field.kind === 'number' ? { valueAsNumber: true } : {}),
          };

          return (
            <label className="form-field" htmlFor={String(field.name)} key={String(field.name)}>
              <span>{field.label}</span>
              {field.kind === 'textarea' ? (
                <textarea {...baseProps} rows={4} />
              ) : field.kind === 'select' ? (
                <select {...baseProps}>
                  <option value="">Selecciona una opcion</option>
                  {field.options?.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              ) : (
                <input
                  {...baseProps}
                  inputMode={field.inputMode}
                  min={field.min}
                  step={field.step}
                  type={field.kind === 'number' ? 'number' : 'text'}
                />
              )}
              {field.description ? <small>{field.description}</small> : null}
              {fieldError ? (
                <strong className="form-field__error">
                  {String(fieldError.message ?? 'Valor no valido')}
                </strong>
              ) : null}
            </label>
          );
        })}

        {notes.length > 0 ? (
          <div className="form-notes">
            <div className="panel__eyebrow">Notas</div>
            <ul>
              {notes.map((note) => (
                <li key={note}>{note}</li>
              ))}
            </ul>
          </div>
        ) : null}

        <div className="form-actions">
          <button className="primary-button" disabled={mutation.isPending} type="submit">
            {mutation.isPending ? 'Enviando...' : submitLabel}
          </button>
        </div>
      </form>

      {mutation.isError ? (
        <div className="response-card response-card--error">
          <div className="response-card__title">Error</div>
          <p>{readError(mutation.error).message}</p>
        </div>
      ) : null}

      {mutation.isSuccess && mutation.data ? (
        <div className="response-card response-card--success">
          <div className="response-card__title">Resultado</div>
          {renderResult ? (
            renderResult(mutation.data)
          ) : (
            <details>
              <summary>Ver detalle tecnico</summary>
              <pre>{JSON.stringify(mutation.data, null, 2)}</pre>
            </details>
          )}
        </div>
      ) : null}
    </section>
  );
}
