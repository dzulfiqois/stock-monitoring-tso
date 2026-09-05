import { useSyncExternalStore } from 'react'

const emptySubscribe = () => () => {}

/// Server snapshot false, client snapshot true — tanpa hydration mismatch.
export function useIsClient(): boolean {
  return useSyncExternalStore(
    emptySubscribe,
    () => true,
    () => false,
  )
}
