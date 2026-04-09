'use client';

import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import { startTransition } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { StatusBadge } from '@/components/shared/status-badge';
import { rlappApi } from '@/services/rlapp-api';

const loginSchema = z.object({
  identifier: z.string().min(1, 'El usuario es obligatorio.'),
  password: z.string().min(1, 'La contrasena es obligatoria.'),
});

type LoginValues = z.infer<typeof loginSchema>;

export function LoginForm() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const form = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      identifier: 'superadmin',
      password: '',
    },
  });

  const loginMutation = useMutation({
    mutationFn: rlappApi.login,
    onSuccess(result) {
      queryClient.setQueryData(['session'], result.session);
      startTransition(() => {
        router.replace('/');
        router.refresh();
      });
    },
  });

  return (
    <section className="auth-card">
      <div className="auth-layout">
        <div>
          <div className="section-intro__eyebrow">Acceso de staff</div>
          <h1>RLApp Clinical Orchestrator</h1>
          <p>
            La sesion se protege desde el servidor para que el navegador no exponga credenciales
            internas del backend.
          </p>
        </div>

        <div className="auth-grid">
          <StatusBadge tone="warning">Importante</StatusBadge>
          <p>
            En el entorno actual, el campo <code>identifier</code> se resuelve como nombre de
            usuario.
          </p>
        </div>

        <div className="auth-grid">
          <StatusBadge tone="info">Usuarios seeded</StatusBadge>
          <ul>
            <li>
              Supervisor: <code>superadmin</code>
            </li>
            <li>
              Soporte: <code>support</code>
            </li>
          </ul>
          <p>
            Las passwords seeded se configuran en el backend por entorno y no se exponen en la
            interfaz.
          </p>
        </div>

        <div className="auth-grid">
          <StatusBadge tone="info">Docker local users</StatusBadge>
          <ul>
            <li>
              Supervisor: <code>superadmin</code> / <code>SuperAdmin@2026Dev!</code>
            </li>
            <li>
              Support: <code>support</code> / <code>Support@2026Dev!</code>
            </li>
          </ul>
        </div>

        <form
          className="form-grid"
          onSubmit={form.handleSubmit(async (values) => {
            await loginMutation.mutateAsync(values);
          })}
        >
          <label className="form-field" htmlFor="identifier">
            <span>Usuario</span>
            <input id="identifier" placeholder="superadmin" {...form.register('identifier')} />
            {form.formState.errors.identifier ? (
              <strong className="form-field__error">
                {form.formState.errors.identifier.message}
              </strong>
            ) : null}
          </label>

          <label className="form-field" htmlFor="password">
            <span>Contrasena</span>
            <input
              id="password"
              placeholder="Ingresa tu contrasena"
              type="password"
              {...form.register('password')}
            />
            {form.formState.errors.password ? (
              <strong className="form-field__error">
                {form.formState.errors.password.message}
              </strong>
            ) : null}
          </label>

          <div className="form-actions">
            <button className="primary-button" disabled={loginMutation.isPending} type="submit">
              {loginMutation.isPending ? 'Ingresando...' : 'Ingresar'}
            </button>
          </div>
        </form>

        {loginMutation.isError ? (
          <div className="response-card response-card--error">
            <div className="response-card__title">No fue posible iniciar sesion</div>
            <p>{loginMutation.error.message}</p>
          </div>
        ) : null}
      </div>
    </section>
  );
}
