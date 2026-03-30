import { ref, watch } from 'vue'

/**
 * Unified localStorage persistence for layout preferences.
 * All keys are prefixed with 'sc_' to avoid conflicts.
 */
const PREFIX = 'sc_'

export function readLayoutValue(key, defaultValue) {
  try {
    const raw = localStorage.getItem(PREFIX + key)
    if (raw === null) return defaultValue
    const parsed = JSON.parse(raw)
    return typeof parsed === typeof defaultValue ? parsed : defaultValue
  } catch {
    return defaultValue
  }
}

export function writeLayoutValue(key, value) {
  try {
    localStorage.setItem(PREFIX + key, JSON.stringify(value))
  } catch { /* quota exceeded — silently ignore */ }
}

/**
 * Creates a reactive ref synced with localStorage.
 * @param {string} key - Storage key (auto-prefixed with 'sc_')
 * @param {*} defaultValue - Default value when nothing is stored
 * @returns {import('vue').Ref}
 */
export function usePersistedRef(key, defaultValue) {
  const data = ref(readLayoutValue(key, defaultValue))
  watch(data, (v) => writeLayoutValue(key, v))
  return data
}
