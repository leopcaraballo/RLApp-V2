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
  identifier: z.string().min(1, 'Username is required.'),
  password: z.string().min(1, 'Password is required.'),
});

type LoginValues = z.infer<typeof loginSchema>;

export function LoginForm() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const form = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      identifier: 'superadmin',
      password: 'SuperAdmin@2026Dev!',
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
          <div className="section-intro__eyebrow">RLApp access</div>
          <h1>Authenticate against the backend contract that exists today.</h1>
          <p>
            This frontend uses a server-side session proxy so the browser never talks to the backend
            with a raw JWT directly.
          </p>
        </div>

        <div className="auth-grid">
          <StatusBadge tone="warning">Backend caveat</StatusBadge>
          <p>
            The login payload says <code>identifier</code>, but the backend currently resolves it as
            <code>username</code> only.
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
            <span>Username</span>
            <input id="identifier" placeholder="admin" {...form.register('identifier')} />
            {form.formState.errors.identifier ? (
              <strong className="form-field__error">
                {form.formState.errors.identifier.message}
              </strong>
            ) : null}
          </label>

          <label className="form-field" htmlFor="password">
            <span>Password</span>
            <input
              id="password"
              placeholder="Password123!"
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
              {loginMutation.isPending ? 'Signing in...' : 'Login'}
            </button>
          </div>
        </form>

        {loginMutation.isError ? (
          <div className="response-card response-card--error">
            <div className="response-card__title">Authentication failed</div>
            <p>{loginMutation.error.message}</p>
          </div>
        ) : null}
      </div>
    </section>
  );
}
