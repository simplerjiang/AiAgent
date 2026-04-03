<template>
  <Teleport to="body">
    <div v-if="visible" class="confirm-overlay" @click.self="handleCancel">
      <div class="confirm-dialog">
        <div class="confirm-header">
          <span class="confirm-icon">{{ type === 'danger' ? '🚨' : '⚠️' }}</span>
          <h3>{{ title }}</h3>
        </div>
        <div class="confirm-body">{{ message }}</div>
        <div class="confirm-actions">
          <button class="btn btn-sm btn-cancel" @click="handleCancel">{{ cancelText }}</button>
          <button class="btn btn-sm" :class="type === 'danger' ? 'btn-danger' : 'btn-primary'" @click="handleConfirm">
            {{ confirmText }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { useConfirm } from '../composables/useConfirm.js'
const { visible, title, message, confirmText, cancelText, type, handleConfirm, handleCancel } = useConfirm()
</script>

<style scoped>
.confirm-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10001;
}
.confirm-dialog {
  background: var(--bg-primary, #1a1a2e);
  border: 1px solid var(--border-color, #333);
  border-radius: 12px;
  padding: 20px 24px;
  max-width: 420px;
  width: 90%;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
}
.confirm-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
}
.confirm-header h3 {
  margin: 0;
  font-size: 15px;
  color: var(--text-primary, #fff);
}
.confirm-icon { font-size: 20px; }
.confirm-body {
  font-size: 13px;
  color: var(--text-secondary, #aaa);
  line-height: 1.6;
  margin-bottom: 16px;
  white-space: pre-wrap;
}
.confirm-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
.btn-cancel {
  background: var(--bg-secondary, #2a2a3e);
  border: 1px solid var(--border-color, #444);
  color: var(--text-secondary, #aaa);
}
.btn-cancel:hover {
  background: var(--bg-tertiary, #3a3a4e);
}
.btn-danger {
  background: #dc2626 !important;
  border-color: #dc2626 !important;
  color: #fff !important;
}
</style>
