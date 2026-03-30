<script setup>
const props = defineProps({
  sessions: { type: Array, default: () => [] },
  activeSessionId: { type: Number, default: null },
  replayTurnId: { type: Number, default: null },
  expandedSessionId: { type: Number, default: null },
  expandedTurns: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false }
})

defineEmits(['select-session', 'select-turn', 'back-to-live'])

const ratingEmoji = rating => {
  if (!rating) return '—'
  const r = rating.toLowerCase()
  if (r.includes('strongbuy') || r.includes('强烈推荐买入') || r.includes('强烈看涨')) return '🟢🟢'
  if (r.includes('buy') || r.includes('买入') || r.includes('看涨')) return '🟢'
  if (r.includes('neutral') || r.includes('hold') || r.includes('中性') || r.includes('持有')) return '🟡'
  if (r.includes('sell') || r.includes('卖出') || r.includes('看跌')) return '🔴'
  return '⚪'
}

const statusLabel = s => ({
  Running: '执行中', Completed: '已完成', Failed: '失败', Idle: '空闲',
  Degraded: '降级', Blocked: '阻塞', Closed: '已关闭', Queued: '排队中'
}[s] || s || '')

const fmtDate = ts => {
  if (!ts) return ''
  const d = new Date(ts)
  return d.toLocaleDateString('zh-CN', { month: 'numeric', day: 'numeric' }) + ' ' +
    d.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
}

const truncate = (s, n = 40) => s && s.length > n ? s.slice(0, n) + '…' : (s || '')
</script>

<template>
  <div class="wb-history">
    <!-- Replay banner -->
    <div v-if="replayTurnId" class="history-replay-bar">
      <span class="replay-icon">🔄</span>
      <span class="replay-text">正在查看历史记录</span>
      <button class="replay-back" @click="$emit('back-to-live')">返回最新 →</button>
    </div>

    <!-- Session list -->
    <div v-if="!sessions.length && !loading" class="history-empty">
      <div class="history-empty-icon">📂</div>
      <p>暂无历史记录</p>
    </div>

    <div v-for="sess in sessions" :key="sess.id"
         :class="['history-session', { expanded: expandedSessionId === sess.id, 'is-active': sess.id === activeSessionId }]">
      <div class="history-session-row" @click="$emit('select-session', sess.id)">
        <span class="sess-rating" :title="sess.latestRating || '无评级'">{{ ratingEmoji(sess.latestRating) }}</span>
        <div class="sess-info">
          <span class="sess-name">{{ truncate(sess.name, 50) || '研究会话' }}</span>
          <span class="sess-meta">
            <span class="sess-date">{{ fmtDate(sess.createdAt) }}</span>
            <span :class="['sess-status', `st-${(sess.status || '').toLowerCase()}`]">{{ statusLabel(sess.status) }}</span>
          </span>
          <span v-if="sess.latestDecisionHeadline" class="sess-headline">{{ truncate(sess.latestDecisionHeadline, 60) }}</span>
        </div>
        <span class="sess-arrow">{{ expandedSessionId === sess.id ? '▾' : '▸' }}</span>
      </div>

      <!-- Expanded turns -->
      <div v-if="expandedSessionId === sess.id && expandedTurns.length" class="history-turns">
        <div v-for="turn in expandedTurns" :key="turn.id"
             :class="['history-turn', { 'is-replay': turn.id === replayTurnId }]"
             @click.stop="$emit('select-turn', { sessionId: sess.id, turnId: turn.id })">
          <span class="turn-idx">T{{ turn.turnIndex }}</span>
          <span class="turn-prompt">{{ truncate(turn.userPrompt, 36) }}</span>
          <span :class="['turn-status', `st-${(turn.status || '').toLowerCase()}`]">
            {{ statusLabel(turn.status) }}
          </span>
        </div>
      </div>
      <div v-if="expandedSessionId === sess.id && !expandedTurns.length" class="history-turns-empty">
        加载中…
      </div>
    </div>

    <div v-if="loading" class="history-loading">加载中…</div>
  </div>
</template>

<style scoped>
.wb-history { padding: 4px 0; }

/* ── Replay bar ────────────────────────────────── */
.history-replay-bar {
  display: flex; align-items: center; gap: 6px;
  padding: 6px 12px; margin: 4px 8px 8px;
  background: color-mix(in srgb, var(--color-accent) 10%, transparent);
  border: 1px solid var(--color-accent-border);
  border-radius: 6px; font-size: 13px;
  color: var(--color-accent);
}
.replay-icon { font-size: 15px; }
.replay-text { flex: 1; }
.replay-back {
  background: transparent; border: 1px solid var(--color-accent);
  color: var(--color-accent); border-radius: 4px;
  padding: 2px 8px; font-size: 12px; cursor: pointer;
  transition: background 0.15s;
}
.replay-back:hover { background: color-mix(in srgb, var(--color-accent) 15%, transparent); }

/* ── Empty ─────────────────────────────────────── */
.history-empty {
  text-align: center; padding: 24px 12px;
  color: var(--color-text-secondary); font-size: 14px;
}
.history-empty-icon { font-size: 28px; margin-bottom: 6px; }

/* ── Session ───────────────────────────────────── */
.history-session {
  margin: 0 8px 2px; border-radius: 6px;
  border: 1px solid var(--color-border-light);
  overflow: hidden; transition: border-color 0.15s;
}
.history-session.expanded { border-color: var(--color-accent); }
.history-session.is-active { border-left: 3px solid var(--color-accent); }

.history-session-row {
  display: flex; align-items: flex-start; gap: 8px;
  padding: 8px 10px; cursor: pointer;
  transition: background 0.15s;
}
.history-session-row:hover { background: var(--color-bg-surface-alt); }

.sess-rating { font-size: 16px; flex-shrink: 0; margin-top: 1px; }
.sess-info { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 2px; }
.sess-name {
  font-size: 14px; font-weight: 500;
  color: var(--color-text-body);
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.sess-meta { display: flex; align-items: center; gap: 6px; font-size: 12px; }
.sess-date { color: var(--color-text-secondary); }
.sess-status { font-weight: 600; }
.st-running { color: var(--color-info); }
.st-completed { color: var(--color-success); }
.st-failed { color: var(--color-danger); }
.st-idle { color: var(--color-text-secondary); }
.st-closed { color: var(--color-text-secondary); }
.st-queued { color: var(--color-warning); }
.st-degraded { color: var(--color-warning); }
.st-blocked { color: var(--color-danger); }

.sess-headline {
  font-size: 12px; color: var(--color-text-secondary);
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.sess-arrow {
  font-size: 12px; color: var(--color-text-secondary);
  flex-shrink: 0; margin-top: 2px;
}

/* ── Turns ─────────────────────────────────────── */
.history-turns {
  padding: 2px 10px 6px 32px;
  border-top: 1px solid var(--color-border-light);
}
.history-turn {
  display: flex; align-items: center; gap: 6px;
  padding: 4px 8px; border-radius: 4px;
  cursor: pointer; transition: background 0.15s;
  font-size: 13px;
}
.history-turn:hover { background: var(--color-bg-surface-alt); }
.history-turn.is-replay {
  background: color-mix(in srgb, var(--color-accent) 10%, transparent);
  border-left: 2px solid var(--color-accent);
}
.turn-idx {
  font-size: 11px; font-weight: 700; font-family: 'Consolas', monospace;
  color: var(--color-accent); flex-shrink: 0; min-width: 20px;
}
.turn-prompt {
  flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis;
  white-space: nowrap; color: var(--color-text-body);
}
.turn-status { font-size: 11px; font-weight: 600; flex-shrink: 0; }

.history-turns-empty {
  padding: 8px 10px 8px 32px; font-size: 12px;
  color: var(--color-text-secondary);
}

.history-loading {
  text-align: center; padding: 12px;
  font-size: 13px; color: var(--color-text-secondary);
}
</style>
