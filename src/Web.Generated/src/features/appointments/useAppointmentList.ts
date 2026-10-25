import { useCallback, useEffect, useState } from 'react'
import {
  appointmentPeriodRange,
  formatApiError,
  type AppointmentPeriod,
} from '../../lib/format'
import type { AppointmentDto } from '../../types/appointment'

const statusAll = 'all'

type AppointmentListQuery = { status?: string; from?: string; to?: string }

export function useAppointmentList(
  fetchItems: (query: AppointmentListQuery) => Promise<AppointmentDto[]>,
) {
  const [items, setItems] = useState<AppointmentDto[]>([])
  const [status, setStatus] = useState(statusAll)
  const [period, setPeriod] = useState<AppointmentPeriod>('all')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const fetchCurrent = useCallback(
    () =>
      fetchItems({
        status: status === statusAll ? undefined : status,
        ...appointmentPeriodRange(period),
      }),
    [fetchItems, status, period],
  )

  useEffect(() => {
    let cancelled = false
    void fetchCurrent()
      .then((next) => {
        if (cancelled) return
        setItems(next)
        setError(null)
      })
      .catch((e: unknown) => {
        if (!cancelled) setError(formatApiError(e))
      })
    return () => {
      cancelled = true
    }
  }, [fetchCurrent])

  function selectStatus(value: string) {
    setError(null)
    setStatus(value)
  }

  function selectPeriod(value: AppointmentPeriod) {
    setError(null)
    setPeriod(value)
  }

  async function run(action: () => Promise<void>) {
    setBusy(true)
    setError(null)
    try {
      await action()
      setItems(await fetchCurrent())
    } catch (e) {
      setError(formatApiError(e))
    } finally {
      setBusy(false)
    }
  }

  return {
    items,
    status,
    selectStatus,
    period,
    selectPeriod,
    error,
    busy,
    run,
  }
}
