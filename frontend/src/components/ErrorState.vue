<script setup>
defineProps({
  icon: { type: String, default: '⚠️' },
  title: { type: String, default: '加载失败' },
  description: { type: String, default: '' },
  retryLabel: { type: String, default: '重新加载' },
  compact: { type: Boolean, default: false }
})

const emit = defineEmits(['retry'])
</script>

<template>
  <div class="error-state" :class="{ 'error-state-compact': compact }">
    <div class="error-state-icon">{{ icon }}</div>
    <div class="error-state-title">{{ title }}</div>
    <div v-if="description" class="error-state-desc">{{ description }}</div>
    <div class="error-state-action">
      <button class="btn btn-sm btn-secondary" @click="emit('retry')">{{ retryLabel }}</button>
    </div>
  </div>
</template>
