<script setup>
import { ref, watch } from 'vue'

const props = defineProps({
  session: { type: Object, default: null },
  activeTurn: { type: Object, default: null },
  sessionStatus: { type: Object, default: () => ({ label: '空闲', cls: 'status-idle' }) },
  currentStage: { type: String, default: null },
  isRunning: { type: Boolean, default: false },
  error: { type: String, default: null },
  isFullscreen: { type: Boolean, default: false },
  symbol: { type: String, default: '' }
})
defineEmits(['refresh', 'toggle-fullscreen'])

// Position state
const posQuantity = ref(0)
const posCost = ref(0)
const posNotes = ref('')
const posEditing = ref(false)
const posSaving = ref(false)
const posLoaded = ref(false)

async function loadPosition() {
  if (!props.symbol) return
  try {
    const resp = await fetch(`/api/stocks/position?symbol=${encodeURIComponent(props.symbol)}`)
    if (resp.ok) {
      const data = await resp.json()
      posQuantity.value = data.quantityLots ?? 0
      posCost.value = data.averageCostPrice ?? 0
      posNotes.value = data.notes ?? ''
      posLoaded.value = true
    }
  } catch { /* silent */ }
}

async function savePosition() {
  if (!props.symbol) return
  posSaving.value = true
  try {
    const resp = await fetch('/api/stocks/position', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        symbol: props.symbol,
        quantityLots: posQuantity.value,
        averageCostPrice: posCost.value,
        notes: posNotes.value || null
      })
    })
    if (resp.ok) {
      posEditing.value = false
    }
  } catch { /* silent */ }
  finally { posSaving.value = false }
}

watch(() => props.symbol, () => {
  posEditing.value = false
  posLoaded.value = false
  loadPosition()
}, { immediate: true })
</script>

<template>
  <div class="wb-header">
    <div class="wb-header-top">
      <div class="wb-header-left">
        <span v-if="session" class="wb-session-badge" :title="`Session #${session.id}`">
          S{{ session.id }}
        </span>
        <span v-if="activeTurn" class="wb-turn-badge" :title="`Turn #${activeTurn.id}`">
          T{{ activeTurn.turnIndex ?? 0 }}
        </span>
        <span :class="['wb-status', sessionStatus.cls]">
          <span v-if="isRunning" class="pulse-dot" />
          {{ sessionStatus.label }}
        </span>
      </div>
      <button class="wb-refresh-btn" title="刷新" @click="$emit('refresh')">↻</button>
      <button class="wb-fullscreen-btn" :title="isFullscreen ? '退出全屏' : '全屏模式'" :aria-label="isFullscreen ? '退出全屏' : '全屏模式'" @click="$emit('toggle-fullscreen')">
        {{ isFullscreen ? '⛶' : '⛶' }}
      </button>
    </div>

    <!-- Position row -->
    <div v-if="symbol" class="wb-position-row">
      <template v-if="!posEditing">
        <span class="wb-pos-label" @click="posEditing = true" title="点击编辑持仓">
          <template v-if="posLoaded && posQuantity > 0">
            📦 持仓 {{ posQuantity }} 手 @ ¥{{ posCost.toFixed(2) }}
            <span v-if="posNotes" class="wb-pos-notes">· {{ posNotes }}</span>
          </template>
          <template v-else>📦 未设置持仓 (点击编辑)</template>
        </span>
      </template>
      <template v-else>
        <div class="wb-pos-form">
          <label class="wb-pos-field">手数<input v-model.number="posQuantity" type="number" min="0" step="1" /></label>
          <label class="wb-pos-field">均价<input v-model.number="posCost" type="number" min="0" step="0.01" /></label>
          <label class="wb-pos-field">备注<input v-model="posNotes" type="text" placeholder="可选" /></label>
          <button class="wb-pos-save" :disabled="posSaving" @click="savePosition">{{ posSaving ? '...' : '保存' }}</button>
          <button class="wb-pos-cancel" @click="posEditing = false">取消</button>
        </div>
      </template>
    </div>

    <div v-if="currentStage" class="wb-stage-indicator">
      <span class="wb-stage-label">当前阶段:</span>
      <span class="wb-stage-name">{{ currentStage }}</span>
    </div>

    <div v-if="error" class="wb-error">
      <span class="wb-error-icon">⚠️</span>
      <span class="wb-error-text">{{ error }}</span>
    </div>
  </div>
</template>

<style scoped>
.wb-header {
  padding: 10px 12px 8px;
  background: var(--color-bg-surface-alt);
  border-bottom: 1px solid var(--color-border-light);
}
.wb-header-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 6px;
}
.wb-header-left {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}

.wb-session-badge, .wb-turn-badge {
  font-size: 12px;
  font-weight: 600;
  padding: 2px 6px;
  border-radius: 4px;
  font-family: 'Consolas', monospace;
}
.wb-session-badge {
  background: var(--color-accent-subtle);
  color: var(--color-accent);
}
.wb-turn-badge {
  background: #ede9fe;
  color: #6d28d9;
}

.wb-status {
  font-size: 13px;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 4px;
}
.status-idle { color: var(--color-text-secondary); }
.status-queued { color: var(--color-warning); }
.status-running { color: var(--color-info); }
.status-completed { color: var(--color-success); }
.status-failed { color: var(--color-danger); }
.status-cancelled { color: var(--color-text-tertiary); }

.pulse-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: currentColor;
  animation: pulse 1.4s infinite ease-in-out;
}
@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.3; }
}

.wb-stage-indicator {
  margin-top: 6px;
  font-size: 13px;
  color: var(--color-text-secondary);
}
.wb-stage-label { margin-right: 4px; }
.wb-stage-name {
  color: var(--color-accent);
  font-weight: 500;
}

.wb-refresh-btn {
  background: transparent;
  border: 1px solid var(--color-border-light);
  color: var(--color-text-secondary);
  border-radius: 4px;
  padding: 2px 8px;
  cursor: pointer;
  font-size: 16px;
}
.wb-refresh-btn:hover { color: var(--color-text-body); }

.wb-fullscreen-btn {
  background: transparent;
  border: 1px solid var(--color-border-light);
  color: var(--color-text-secondary);
  border-radius: 4px;
  padding: 2px 8px;
  cursor: pointer;
  font-size: 16px;
}
.wb-fullscreen-btn:hover { color: var(--color-text-body); background: var(--color-bg-surface-alt); }

.wb-error {
  margin-top: 6px;
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 13px;
  color: var(--color-danger);
  background: rgba(239, 83, 80, 0.1);
  padding: 4px 8px;
  border-radius: 4px;
}
.wb-error-icon { font-size: 14px; }
.wb-error-text { line-height: 1.3; }

/* Position row */
.wb-position-row {
  margin-top: 8px;
  font-size: 14px;
  color: var(--color-text-secondary);
}
.wb-pos-label {
  cursor: pointer;
  transition: color 0.15s;
}
.wb-pos-label:hover { color: var(--color-text-body); }
.wb-pos-notes { opacity: 0.7; }
.wb-pos-form {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  margin-top: 4px;
}
.wb-pos-field {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 14px;
  color: var(--color-text-secondary);
}
.wb-pos-field input {
  width: 100px;
  padding: 5px 8px;
  font-size: 14px;
  background: var(--color-bg-surface-alt);
  border: 1px solid var(--color-border-light);
  color: var(--color-text-body);
  border-radius: 4px;
}
.wb-pos-field input[type=text] { width: 120px; }
.wb-pos-save, .wb-pos-cancel {
  padding: 5px 14px;
  font-size: 14px;
  border-radius: 4px;
  cursor: pointer;
  border: 1px solid var(--color-border-light);
}
.wb-pos-save {
  background: var(--color-accent);
  color: #fff;
  border-color: transparent;
}
.wb-pos-save:disabled { opacity: 0.5; cursor: default; }
.wb-pos-cancel {
  background: transparent;
  color: var(--color-text-secondary);
}
</style>
