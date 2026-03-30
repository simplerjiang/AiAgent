<script setup>
defineProps({
  isDragging: { type: Boolean, default: false },
  direction:  { type: String, default: 'horizontal' }
})
const emit = defineEmits(['pointerdown', 'dblclick'])
</script>

<template>
  <div
    class="sc-splitter"
    :class="{
      'sc-splitter--active': isDragging,
      'sc-splitter--vertical': direction === 'vertical'
    }"
    role="separator"
    tabindex="0"
    @pointerdown="emit('pointerdown', $event)"
    @dblclick="emit('dblclick')"
  >
    <div class="sc-splitter__grip">
      <span /><span /><span />
    </div>
  </div>
</template>

<style scoped>
.sc-splitter {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  cursor: col-resize;
  background: transparent;
  transition: background var(--transition-fast);
  position: relative;
  z-index: 10;
  touch-action: none;
}

.sc-splitter:hover,
.sc-splitter--active {
  background: var(--color-accent-subtle);
}

.sc-splitter--active {
  width: 8px;
}

.sc-splitter__grip {
  display: flex;
  flex-direction: column;
  gap: 3px;
  opacity: 0.4;
  transition: opacity var(--transition-fast);
}

.sc-splitter:hover .sc-splitter__grip,
.sc-splitter--active .sc-splitter__grip {
  opacity: 1;
}

.sc-splitter__grip span {
  display: block;
  width: 3px;
  height: 3px;
  border-radius: var(--radius-full);
  background: var(--color-border-strong);
}

/* Vertical variant (for chart height resizer) */
.sc-splitter--vertical {
  width: auto;
  height: 8px;
  cursor: row-resize;
}

.sc-splitter--vertical .sc-splitter__grip {
  flex-direction: row;
  gap: 3px;
}

@media (max-width: 1179px) {
  .sc-splitter:not(.sc-splitter--vertical) {
    display: none;
  }
}
</style>
