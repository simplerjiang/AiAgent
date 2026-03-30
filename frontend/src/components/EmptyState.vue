<script setup>
defineProps({
  icon: { type: String, default: '📭' },
  title: { type: String, required: true },
  description: { type: String, default: '' },
  compact: { type: Boolean, default: false }
})
</script>

<template>
  <div class="empty-state" :class="{ 'empty-state-compact': compact }">
    <div class="empty-state-icon">{{ icon }}</div>
    <div class="empty-state-title">{{ title }}</div>
    <div v-if="description" class="empty-state-desc">{{ description }}</div>
    <div v-if="$slots.action" class="empty-state-action">
      <slot name="action" />
    </div>
  </div>
</template>
