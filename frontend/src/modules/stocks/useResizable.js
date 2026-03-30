import { onUnmounted, ref } from 'vue'
import { usePersistedRef } from './useLayoutPersistence'

/**
 * Generic drag-resize composable for splitter and chart height handles.
 *
 * @param {Object} opts
 * @param {'horizontal'|'vertical'} opts.direction - Drag axis
 * @param {number} opts.min - Minimum value (ratio 0–1 for horizontal, px for vertical)
 * @param {number} opts.max - Maximum value
 * @param {number} opts.defaultValue - Default size
 * @param {string} [opts.storageKey] - localStorage key for persistence
 * @param {HTMLElement|import('vue').Ref<HTMLElement>} [opts.containerRef] - Reference element for ratio calc
 */
export function useResizable({ direction, min, max, defaultValue, storageKey, containerRef }) {
  const size = storageKey
    ? usePersistedRef(storageKey, defaultValue)
    : ref(defaultValue)
  const isDragging = ref(false)

  let startPos = 0
  let startSize = 0

  function clamp(v) {
    return Math.min(max, Math.max(min, v))
  }

  function getContainerSize() {
    const el = containerRef?.value ?? containerRef
    if (!el) return 1
    return direction === 'horizontal' ? el.clientWidth : el.clientHeight
  }

  function onPointerMove(e) {
    if (!isDragging.value) return
    const currentPos = direction === 'horizontal' ? e.clientX : e.clientY
    const delta = currentPos - startPos
    const containerSize = getContainerSize()

    if (direction === 'horizontal') {
      // For horizontal splitter, size is a ratio (0–1)
      const deltaRatio = delta / containerSize
      size.value = clamp(startSize + deltaRatio)
    } else {
      // For vertical, size is pixels
      size.value = clamp(startSize + delta)
    }
  }

  function onPointerUp() {
    isDragging.value = false
    document.removeEventListener('pointermove', onPointerMove)
    document.removeEventListener('pointerup', onPointerUp)
    document.body.style.cursor = ''
    document.body.style.userSelect = ''
  }

  function startResize(e) {
    e.preventDefault()
    isDragging.value = true
    startPos = direction === 'horizontal' ? e.clientX : e.clientY
    startSize = size.value
    document.body.style.cursor = direction === 'horizontal' ? 'col-resize' : 'row-resize'
    document.body.style.userSelect = 'none'
    document.addEventListener('pointermove', onPointerMove)
    document.addEventListener('pointerup', onPointerUp)
  }

  function resetSize() {
    size.value = defaultValue
  }

  onUnmounted(() => {
    document.removeEventListener('pointermove', onPointerMove)
    document.removeEventListener('pointerup', onPointerUp)
  })

  return { size, isDragging, startResize, resetSize }
}
