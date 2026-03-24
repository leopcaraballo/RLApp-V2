'use client';

import { useQueries } from '@tanstack/react-query';
import { StatusBadge } from '@/components/shared/status-badge';
import { rlappApi } from '@/services/rlapp-api';
import type { HealthStatusResponse } from '@/types/api';

function toneForHealth(status?: string): 'success' | 'warning' | 'danger' | 'neutral' {
  if (!status) {
    return 'neutral';
  }

  if (status.toLowerCase() === 'healthy') {
    return 'success';
  }

  if (status.toLowerCase() === 'degraded') {
    return 'warning';
  }

  return 'danger';
}

function HealthCard({
  title,
  description,
  data,
  isLoading,
}: {
  title: string;
  description: string;
  data?: HealthStatusResponse;
  isLoading: boolean;
}) {
  return (
    <article className="panel">
      <div className="panel__header">
        <div>
          <div className="panel__eyebrow">{title}</div>
          <h2>{description}</h2>
        </div>
        <StatusBadge tone={toneForHealth(data?.status)}>
          {isLoading ? 'Loading' : data?.status ?? 'Unknown'}
        </StatusBadge>
      </div>

      <div className="health-details">
        {data?.details?.map((detail) => (
          <div className="health-details__row" key={`${title}-${detail.key}`}>
            <strong>{detail.key}</strong>
            <span>{detail.status}</span>
            <small>{detail.description ?? 'No description provided by backend.'}</small>
          </div>
        ))}
      </div>
    </article>
  );
}

export function HealthPanels() {
  const [healthQuery, readyQuery, liveQuery] = useQueries({
    queries: [
      { queryKey: ['health'], queryFn: rlappApi.getHealth },
      { queryKey: ['health', 'ready'], queryFn: rlappApi.getReadiness },
      { queryKey: ['health', 'live'], queryFn: rlappApi.getLiveness },
    ],
  });

  return (
    <div className="grid grid--three">
      <HealthCard
        data={healthQuery.data}
        description="Aggregated health checks"
        isLoading={healthQuery.isLoading}
        title="/health"
      />
      <HealthCard
        data={readyQuery.data}
        description="Readiness dependencies"
        isLoading={readyQuery.isLoading}
        title="/health/ready"
      />
      <HealthCard
        data={liveQuery.data}
        description="Process liveness"
        isLoading={liveQuery.isLoading}
        title="/health/live"
      />
    </div>
  );
}
