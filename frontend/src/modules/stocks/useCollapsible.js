import { usePersistedRef } from './useLayoutPersistence'

/**
 * Composable for collapse/expand with localStorage persistence.
 * @param {string} storageKey - Unique key (e.g. 'collapse_market_overview')
 * @param {boolean} [defaultOpen=true] - Whether the section starts open
 */
export function useCollapsible(storageKey, defaultOpen = true) {
  const isOpen = usePersistedRef(storageKey, defaultOpen)

  function toggle() { isOpen.value = !isOpen.value }
  function open()   { isOpen.value = true }
  function close()  { isOpen.value = false }

  return { isOpen, toggle, open, close }
}
