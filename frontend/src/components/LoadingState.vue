<script setup>
defineProps({
  mode: { type: String, default: 'spinner', validator: v => ['spinner', 'skeleton'].includes(v) },
  message: { type: String, default: '' },
  lines: { type: Number, default: 3 },
  compact: { type: Boolean, default: false }
})
</script>

<template>
  <div v-if="mode === 'spinner'" class="loading-state" :class="{ 'loading-state-compact': compact }">
    <div class="loading-spinner" />
    <span v-if="message">{{ message }}</span>
  </div>
  <div v-else class="loading-skeleton-group" :class="{ 'loading-state-compact': compact }">
    <div class="skeleton skeleton-title" />
    <div v-for="i in lines" :key="i" class="skeleton skeleton-text" />
  </div>
</template>

<style scoped>
.loading-skeleton-group {
  padding: var(--space-5);
}
</style>
