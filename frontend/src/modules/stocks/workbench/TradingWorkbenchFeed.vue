<script setup>
import { ref, computed, nextTick, watch } from 'vue'
import DOMPurify from 'dompurify'
import { marked } from 'marked'

const props = defineProps({
  items: { type: Array, default: () => [] },
  activeTurn: { type: Object, default: null },
  isRunning: { type: Boolean, default: false },
  currentStage: { type: String, default: null }
})

const feedEnd = ref(null)
const collapsedItems = ref(new Set())

const toggleCollapse = idx => {
  const s = new Set(collapsedItems.value)
  s.has(idx) ? s.delete(idx) : s.add(idx)
  collapsedItems.value = s
}

const safeHtml = md => {
  if (!md) return ''
  return DOMPurify.sanitize(marked.parse(md, { breaks: true }))
}

/** Role visual config: avatar icon, bubble color, alignment. */
const roleConfig = roleId => {
  const id = (roleId || '').toLowerCase()
  if (id.includes('market')) return { avatar: '📈', color: '#2b4a7a', name: '市场分析师' }
  if (id.includes('social') || id.includes('sentiment')) return { avatar: '💬', color: '#2b4a6a', name: '情绪分析师' }
  if (id.includes('news')) return { avatar: '📰', color: '#2b4a5a', name: '新闻分析师' }
  if (id.includes('fundamental')) return { avatar: '📊', color: '#2b3a6a', name: '基本面分析师' }
  if (id.includes('shareholder')) return { avatar: '👥', color: '#2b3a5a', name: '股东分析师' }
  if (id.includes('product')) return { avatar: '🏭', color: '#2b4a4a', name: '产品分析师' }
  if (id.includes('companyoverview')) return { avatar: '🏢', color: '#2b3a4a', name: '公司概览' }
  if (id.includes('bull')) return { avatar: '🐂', color: '#1a4a2a', name: '多方研究员' }
  if (id.includes('bear')) return { avatar: '🐻', color: '#5a1a1a', name: '空方研究员' }
  if (id.includes('researchmanager')) return { avatar: '👔', color: '#4a3a1a', name: '研究经理' }
  if (id.includes('trader')) return { avatar: '💹', color: '#3a1a4a', name: '交易员' }
  if (id.includes('aggressive')) return { avatar: '🔥', color: '#4a2a1a', name: '激进风控' }
  if (id.includes('neutral')) return { avatar: '⚖️', color: '#2a3a4a', name: '中性风控' }
  if (id.includes('conservative')) return { avatar: '🛡️', color: '#1a2a4a', name: '保守风控' }
  if (id.includes('portfolio')) return { avatar: '🎯', color: '#4a3a0a', name: '组合经理' }
  return { avatar: '🤖', color: '#2a2d35', name: roleId || '系统' }
}

/** Classify item for rendering. */
const itemKind = item => {
  const t = (item.type || item.feedType || item.itemType || '').toLowerCase()
  if (t.includes('stagetransition') || t.includes('stagestarted') || t.includes('stagecompleted') || t.includes('stagefailed')) return 'divider'
  if (t.includes('tooldispatched') || t.includes('toolcompleted') || t.includes('toolprogress') || t.includes('toolevent')) return 'tool'
  if (t.includes('userfollowup') || t.includes('turnstarted')) return 'user'
  if (t.includes('system') || t.includes('degraded') || t.includes('retryattempt')) return 'system'

  // Demote lifecycle status messages (Started/Completed) to compact style
  const content = getContent(item)
  if (/^Role \S+ (started|Completed|Degraded|Running|LLM ready)$/i.test(content)) return 'lifecycle'

  return 'role'
}

const formatTime = ts => {
  if (!ts) return ''
  const d = new Date(ts)
  return d.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

const stageLabel = summary => {
  if (!summary) return ''
  const map = {
    CompanyOverviewPreflight: '🏢 公司概览',
    AnalystTeam: '📊 分析师团队',
    ResearchDebate: '⚔️ 研究辩论',
    TraderProposal: '💹 交易方案',
    RiskDebate: '🛡️ 风险评估',
    PortfolioDecision: '🎯 投资决策'
  }
  for (const [k, v] of Object.entries(map)) {
    if (summary.includes(k)) return v
  }
  return summary
}

const getContent = item => item.summary || item.message || item.content || ''

const isLongContent = content => content.length > 800

/** Group feed items by turnId. */
const groupByTurn = computed(() => {
  const groups = []
  let currentTurn = null
  let currentGroup = null
  for (const item of props.items) {
    const tid = item.turnId ?? item.turn_id ?? 0
    if (tid !== currentTurn) {
      currentTurn = tid
      currentGroup = { turnId: tid, items: [] }
      groups.push(currentGroup)
    }
    currentGroup.items.push(item)
  }
  return groups
})

// Auto-scroll to bottom when new items arrive
watch(() => props.items.length, () => {
  nextTick(() => { feedEnd.value?.scrollIntoView({ behavior: 'smooth', block: 'end' }) })
})
</script>

<template>
  <div class="wb-feed-chat">
    <template v-if="items.length > 0">
      <div
        v-for="group in groupByTurn"
        :key="group.turnId"
        class="feed-turn-group"
      >
        <!-- Turn header card -->
        <div class="feed-turn-header">
          <span class="turn-badge">Turn {{ group.turnId }}</span>
        </div>

        <template v-for="(item, idx) in group.items" :key="idx">
          <!-- Stage divider -->
          <div v-if="itemKind(item) === 'divider'" class="feed-divider">
            <span class="feed-divider-line" />
            <span class="feed-divider-text">{{ stageLabel(getContent(item)) || getContent(item) }}</span>
            <span class="feed-divider-line" />
          </div>

          <!-- Tool event (compact system-style) -->
          <div v-else-if="itemKind(item) === 'tool'" class="feed-tool">
            <span class="feed-tool-icon">🔧</span>
            <span class="feed-tool-text">{{ getContent(item) }}</span>
            <span class="feed-tool-time">{{ formatTime(item.timestamp || item.createdAt) }}</span>
          </div>

          <!-- System / retry / degraded notice -->
          <div v-else-if="itemKind(item) === 'system'" class="feed-system">
            <span class="feed-system-icon">{{ (item.type || item.feedType || '').toLowerCase().includes('retry') ? '🔄' : 'ℹ️' }}</span>
            <span class="feed-system-text">{{ getContent(item) }}</span>
            <span class="feed-system-time">{{ formatTime(item.timestamp || item.createdAt) }}</span>
          </div>

          <!-- Lifecycle status (compact, dimmed) -->
          <div v-else-if="itemKind(item) === 'lifecycle'" class="feed-lifecycle">
            <span class="feed-lifecycle-dot">•</span>
            <span class="feed-lifecycle-text">{{ roleConfig(item.roleId || item.role_id).name }} {{ getContent(item).includes('started') ? '开始分析' : getContent(item).includes('Completed') ? '分析完成' : getContent(item).includes('Degraded') ? '降级完成' : '' }}</span>
            <span class="feed-lifecycle-time">{{ formatTime(item.timestamp || item.createdAt) }}</span>
          </div>

          <!-- User follow-up (right aligned) -->
          <div v-else-if="itemKind(item) === 'user'" class="feed-msg feed-msg-user">
            <div class="feed-bubble feed-bubble-user">
              <div class="feed-bubble-content" v-html="safeHtml(getContent(item))" />
              <div class="feed-bubble-time">{{ formatTime(item.timestamp || item.createdAt) }}</div>
            </div>
            <div class="feed-avatar feed-avatar-user">👤</div>
          </div>

          <!-- Role message (left aligned chat bubble) -->
          <div v-else class="feed-msg feed-msg-role">
            <div class="feed-avatar" :style="{ background: roleConfig(item.roleId || item.role_id).color }">
              {{ roleConfig(item.roleId || item.role_id).avatar }}
            </div>
            <div class="feed-bubble-wrap">
              <div class="feed-bubble-name">
                {{ roleConfig(item.roleId || item.role_id).name }}
                <span class="feed-bubble-time-inline">{{ formatTime(item.timestamp || item.createdAt) }}</span>
              </div>
              <div class="feed-bubble feed-bubble-role" :style="{ '--bubble-bg': roleConfig(item.roleId || item.role_id).color }">
                <div
                  :class="['feed-bubble-content', { collapsed: isLongContent(getContent(item)) && !collapsedItems.has(`${group.turnId}-${idx}`) }]"
                  v-html="safeHtml(getContent(item))"
                />
                <button
                  v-if="isLongContent(getContent(item))"
                  class="feed-collapse-btn"
                  @click="toggleCollapse(`${group.turnId}-${idx}`)"
                >
                  {{ collapsedItems.has(`${group.turnId}-${idx}`) ? '收起' : '展开全部' }}
                </button>
              </div>
            </div>
          </div>
        </template>
      </div>
    </template>

    <div v-else class="feed-empty">
      <p>暂无讨论动态</p>
    </div>

    <!-- Typing indicator -->
    <div v-if="isRunning" class="feed-typing">
      <span class="feed-typing-dots"><span /><span /><span /></span>
      <span class="feed-typing-text">{{ currentStage ? `${currentStage} 分析中...` : '研究进行中...' }}</span>
    </div>

    <div ref="feedEnd" />
  </div>
</template>

<style scoped>
.wb-feed-chat {
  padding: 10px 12px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

/* ── Turn header ──────────────────────────────── */
.feed-turn-group { display: flex; flex-direction: column; gap: 4px; }
.feed-turn-header { text-align: center; margin: 8px 0 4px; }
.turn-badge {
  font-size: 10px; font-weight: 600; color: #b09cf6;
  background: rgba(176,156,246,0.1); padding: 2px 10px;
  border-radius: 10px; font-family: 'Consolas', monospace;
}

/* ── Stage divider ────────────────────────────── */
.feed-divider {
  display: flex; align-items: center; gap: 8px;
  margin: 8px 0 4px; font-size: 11px; color: var(--wb-text-muted, #8b8fa3);
}
.feed-divider-line { flex: 1; height: 1px; background: var(--wb-border, #2a2d35); }
.feed-divider-text { white-space: nowrap; font-weight: 500; }

/* ── Tool event (compact) ─────────────────────── */
.feed-tool, .feed-system {
  display: flex; align-items: center; gap: 4px;
  padding: 2px 12px; font-size: 11px;
  color: var(--wb-text-muted, #8b8fa3);
}
.feed-tool-icon, .feed-system-icon { font-size: 10px; flex-shrink: 0; }
.feed-tool-text, .feed-system-text { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.feed-tool-time, .feed-system-time { font-size: 10px; flex-shrink: 0; opacity: 0.6; }

/* ── Lifecycle (dimmed compact) ───────────────── */
.feed-lifecycle {
  display: flex; align-items: center; gap: 4px;
  padding: 1px 12px; font-size: 10px;
  color: var(--wb-text-muted, #8b8fa3);
  opacity: 0.5;
}
.feed-lifecycle-dot { font-size: 8px; flex-shrink: 0; }
.feed-lifecycle-text { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.feed-lifecycle-time { font-size: 9px; flex-shrink: 0; opacity: 0.5; }

/* ── Message row ──────────────────────────────── */
.feed-msg { display: flex; gap: 8px; margin: 3px 0; align-items: flex-start; }
.feed-msg-user { flex-direction: row-reverse; }
.feed-msg-role { flex-direction: row; }

/* ── Avatar ───────────────────────────────────── */
.feed-avatar {
  width: 30px; height: 30px; border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-size: 15px; flex-shrink: 0;
  background: var(--wb-card-bg, #2a2d35);
}
.feed-avatar-user { background: #3a4a5a; }

/* ── Bubble ───────────────────────────────────── */
.feed-bubble-wrap { flex: 1; min-width: 0; max-width: 85%; }
.feed-bubble-name {
  font-size: 11px; font-weight: 600; margin-bottom: 2px;
  color: var(--wb-text-muted, #8b8fa3);
}
.feed-bubble-time-inline { font-weight: 400; font-size: 10px; opacity: 0.6; margin-left: 6px; }

.feed-bubble {
  padding: 8px 12px; border-radius: 12px;
  font-size: 13px; line-height: 1.6; word-break: break-word;
}
.feed-bubble-role {
  background: var(--bubble-bg, #2a2d35);
  color: var(--wb-text, #e1e4ea);
  border-top-left-radius: 4px;
}
.feed-bubble-user {
  background: #2a4a6a;
  color: #fff;
  border-top-right-radius: 4px;
  margin-left: auto; max-width: 85%;
}

.feed-bubble-content { overflow: hidden; }
.feed-bubble-content.collapsed { max-height: 200px; -webkit-mask-image: linear-gradient(to bottom, #000 60%, transparent 100%); mask-image: linear-gradient(to bottom, #000 60%, transparent 100%); }
.feed-bubble-content :deep(p) { margin: 0 0 4px; }
.feed-bubble-content :deep(ul), .feed-bubble-content :deep(ol) { margin: 4px 0; padding-left: 16px; }
.feed-bubble-content :deep(li) { margin: 2px 0; }
.feed-bubble-content :deep(strong) { color: #fff; }
.feed-bubble-content :deep(code) { background: rgba(0,0,0,0.3); padding: 1px 4px; border-radius: 3px; font-size: 12px; }

.feed-bubble-time { font-size: 10px; color: rgba(255,255,255,0.4); margin-top: 4px; text-align: right; }

.feed-collapse-btn {
  background: none; border: none; color: var(--wb-accent, #5b9cf6);
  font-size: 11px; cursor: pointer; padding: 2px 0; margin-top: 4px;
}
.feed-collapse-btn:hover { text-decoration: underline; }

/* ── Typing indicator ─────────────────────────── */
.feed-typing {
  display: flex; align-items: center; gap: 8px;
  padding: 6px 12px; font-size: 11px;
  color: var(--wb-text-muted, #8b8fa3);
}
.feed-typing-dots { display: flex; gap: 3px; }
.feed-typing-dots span {
  width: 5px; height: 5px; border-radius: 50%;
  background: var(--wb-accent, #5b9cf6);
  animation: typing-bounce 1.4s infinite ease-in-out;
}
.feed-typing-dots span:nth-child(2) { animation-delay: 0.2s; }
.feed-typing-dots span:nth-child(3) { animation-delay: 0.4s; }
@keyframes typing-bounce {
  0%, 80%, 100% { opacity: 0.3; transform: scale(0.8); }
  40% { opacity: 1; transform: scale(1); }
}

/* ── Empty state ──────────────────────────────── */
.feed-empty {
  text-align: center; padding: 24px 12px;
  color: var(--wb-text-muted, #8b8fa3); font-size: 12px;
}
</style>
