'use client';

import { useQueries } from '@tanstack/react-query';
import { StatusBadge } from '@/components/shared/status-badge';
import { getHealthStatusDisplayName } from '@/lib/display-text';
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
  heading,
  data,
  isLoading,
}: {
  title: string;
  heading: string;
  data?: HealthStatusResponse;
  isLoading: boolean;
}) {
  return (
    <article className="panel clinical-panel clinical-panel--soft">
      <div className="panel__header">
        <div>
          <div className="panel__eyebrow">{title}</div>
          <h2>{heading}</h2>
        </div>
        <StatusBadge tone={toneForHealth(data?.status)}>
          {isLoading ? 'Cargando' : getHealthStatusDisplayName(data?.status)}
        </StatusBadge>
      </div>

      <div className="health-details">
        {data?.details?.map((detail) => (
          <div className="health-details__row" key={`${title}-${detail.key}`}>
            <strong>{detail.key}</strong>
            <span>{getHealthStatusDisplayName(detail.status)}</span>
            <small>{detail.description ?? 'El backend no entrego una descripcion.'}</small>
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
        heading="General"
        isLoading={healthQuery.isLoading}
        title="/health"
      />
      <HealthCard
        data={readyQuery.data}
        heading="Ready"
        isLoading={readyQuery.isLoading}
        title="/health/ready"
      />
      <HealthCard
        data={liveQuery.data}
        heading="Live"
        isLoading={liveQuery.isLoading}
        title="/health/live"
      />
    </div>
  );
}
