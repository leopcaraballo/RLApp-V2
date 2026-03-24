'use client';

import { useEffect, useEffectEvent, useState } from 'react';

export interface OperationJournalEntry {
  title: string;
  status: 'success' | 'error';
  message: string;
  timestamp: string;
  correlationId?: string;
}

export function useOperationJournal(scope: string) {
  const storageKey = `rlapp-journal:${scope}`;
  const [entries, setEntries] = useState<OperationJournalEntry[]>([]);

  useEffect(() => {
    if (typeof window === 'undefined') {
      return;
    }

    const rawValue = window.sessionStorage.getItem(storageKey);
    if (!rawValue) {
      return;
    }

    try {
      const parsed = JSON.parse(rawValue) as OperationJournalEntry[];
      setEntries(parsed);
    } catch {
      window.sessionStorage.removeItem(storageKey);
    }
  }, [storageKey]);

  const persistEntries = useEffectEvent((nextEntries: OperationJournalEntry[]) => {
    window.sessionStorage.setItem(storageKey, JSON.stringify(nextEntries));
  });

  useEffect(() => {
    if (typeof window === 'undefined') {
      return;
    }

    persistEntries(entries);
  }, [entries, persistEntries]);

  function pushEntry(entry: OperationJournalEntry) {
    setEntries((currentEntries) => [entry, ...currentEntries].slice(0, 10));
  }

  function clearEntries() {
    setEntries([]);
  }

  return {
    entries,
    pushEntry,
    clearEntries,
  };
}
