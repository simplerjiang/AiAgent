<script setup>
import { computed, nextTick, onMounted, onUnmounted, reactive } from 'vue'
import { fetchBackendGet, fetchBackendPost, fetchBackendPut, fetchBackendDelete, parseResponseMessage } from './stockInfoTabRequestUtils'
import { markdownToSafeHtml } from '../../utils/jsonMarkdownService'

const state = reactive({
  // Filter
  period: 'day',
  customFrom: null,
  customTo: null,
  symbolFilter: '',
  typeFilter: '',

  // Portfolio
  snapshot: null,
  snapshotLoading: false,

  // Exposure
  exposure: null,
  exposureLoading: false,
  exposureError: '',

  // Trades
  trades: [],
  tradesLoading: false,
  tradesError: '',

  // Summary
  summary: null,
  summaryLoading: false,

  // Modal
  tradeModalOpen: false,
  editingTradeId: null,
  tradeForm: { planId: null, symbol: '', name: '', direction: 'Buy', tradeType: 'Normal', executedPrice: '', quantity: '', executedAt: '', commission: '', userNote: '' },
  tradeFormSaving: false,
  tradeFormError: '',

  // Settings
  settingsModalOpen: false,
  capitalInput: '',
  settingsSaving: false,

  // Extra errors
  snapshotError: '',
  summaryError: '',

  // Behavior
  behaviorStats: null,
  behaviorStatsLoading: false,

  // Review
  reviewMenuOpen: false,
  reviewGenerating: false,
  reviewError: '',
  reviewCurrent: null,
  reviewList: [],
  reviewListLoading: false,
  showReviewPanel: false,
})

function formatPnL(v) { return v == null ? '-' : (v >= 0 ? '+' : '') + v.toFixed(2) }
function formatMoney(v) { return v == null ? '-' : v.toFixed(2) }
function formatPercent(v) { return v == null ? '-' : (v * 100).toFixed(1) + '%' }
function pnlClass(v) { return v > 0 ? 'text-rise' : v < 0 ? 'text-fall' : '' }
function directionBadgeClass(d) { return d === 'Buy' ? 'badge-danger' : 'badge-success' }
function complianceBadgeClass(tag) {
  if (tag === 'FollowedPlan') return 'badge-success'
  if (tag === 'DeviatedFromPlan') return 'badge-warning'
  return 'badge-info'
}
function complianceLabel(tag) {
  if (tag === 'FollowedPlan') return '遵守计划'
  if (tag === 'DeviatedFromPlan') return '偏离计划'
  return '无计划'
}
function formatDateTime(v) {
  if (!v) return '-'
  const d = new Date(v)
  return `${d.getMonth() + 1}/${d.getDate()} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}
function formatDateTimeLocal(v) {
  if (!v) return ''
  const d = new Date(v)
  const pad = n => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}
function getPeriodDates(period) {
  const now = new Date()
  const to = now.toISOString()
  let from
  if (period === 'day') {
    from = new Date(now.getFullYear(), now.getMonth(), now.getDate()).toISOString()
  } else if (period === 'week') {
    const d = new Date(now)
    d.setDate(d.getDate() - d.getDay() + 1)
    d.setHours(0, 0, 0, 0)
    from = d.toISOString()
  } else if (period === 'month') {
    from = new Date(now.getFullYear(), now.getMonth(), 1).toISOString()
  }
  return { from, to }
}

const positionRatioClass = computed(() => {
  const r = state.snapshot?.totalPositionRatio ?? 0
  if (r > 0.8) return 'badge-danger'
  if (r > 0.5) return 'badge-warning'
  return 'badge-success'
})

const positionBarWidth = computed(() => {
  const r = state.snapshot?.totalPositionRatio ?? 0
  return Math.min(100, Math.max(0, r * 100)) + '%'
})

const positionBarClass = computed(() => {
  const r = state.snapshot?.totalPositionRatio ?? 0
  if (r > 0.8) return 'bar-danger'
  if (r > 0.5) return 'bar-warning'
  return 'bar-safe'
})

const winRateClass = computed(() => {
  const w = state.summary?.winRate ?? 0
  if (w >= 0.6) return 'text-rise'
  if (w < 0.4) return 'text-fall'
  return ''
})

const exposureBarWidth = computed(() => {
  const e = state.exposure?.combinedExposure ?? 0
  return Math.min(100, Math.max(0, e * 100)) + '%'
})

const exposureBarClass = computed(() => {
  const e = state.exposure?.combinedExposure ?? 0
  if (e > 0.8) return 'bar-danger'
  if (e > 0.5) return 'bar-warning'
  return 'bar-safe'
})

const exposureBadgeClass = computed(() => {
  const e = state.exposure?.combinedExposure ?? 0
  if (e > 0.8) return 'badge-danger'
  if (e > 0.5) return 'badge-warning'
  return 'badge-success'
})

async function loadPortfolioSnapshot() {
  state.snapshotLoading = true
  state.snapshotError = ''
  try {
    const res = await fetchBackendGet('/api/portfolio/snapshot')
    if (res.ok) {
      state.snapshot = await res.json()
    }
  } catch {
    state.snapshotError = '加载持仓信息失败'
  }
  state.snapshotLoading = false
}

async function loadExposure() {
  state.exposureLoading = true
  state.exposureError = ''
  try {
    const res = await fetchBackendGet('/api/portfolio/exposure')
    if (res.ok) {
      state.exposure = await res.json()
    }
  } catch {
    state.exposureError = '加载暴露数据失败'
  }
  state.exposureLoading = false
}

async function loadBehaviorStats() {
  state.behaviorStatsLoading = true
  try {
    const res = await fetchBackendGet('/api/trades/behavior-stats')
    if (res.ok) {
      state.behaviorStats = await res.json()
    }
  } catch { /* silent */ }
  state.behaviorStatsLoading = false
}

const disciplineScoreClass = computed(() => {
  const s = state.behaviorStats?.disciplineScore ?? 100
  if (s >= 80) return 'score-good'
  if (s >= 60) return 'score-warn'
  return 'score-danger'
})

const planRateClass = computed(() => {
  const r = state.behaviorStats?.planExecutionRate ?? 1
  if (r >= 0.8) return 'text-rise'
  if (r >= 0.5) return ''
  return 'text-fall'
})

const lossStreakClass = computed(() => {
  const s = state.behaviorStats?.currentLossStreak ?? 0
  if (s >= 3) return 'text-fall'
  if (s >= 1) return 'text-warning'
  return ''
})

async function loadTrades() {
  state.tradesLoading = true
  state.tradesError = ''
  try {
    const params = new URLSearchParams()
    if (state.symbolFilter) params.set('symbol', state.symbolFilter)
    if (state.typeFilter) params.set('type', state.typeFilter)
    if (state.period === 'custom') {
      if (state.customFrom) params.set('from', new Date(state.customFrom).toISOString())
      if (state.customTo) params.set('to', new Date(state.customTo).toISOString())
    } else {
      const { from, to } = getPeriodDates(state.period)
      if (from) params.set('from', from)
      if (to) params.set('to', to)
    }
    const res = await fetchBackendGet(`/api/trades?${params}`)
    if (res.ok) {
      state.trades = await res.json()
    } else {
      state.tradesError = await parseResponseMessage(res, '加载失败')
    }
  } catch {
    state.tradesError = '网络错误'
  }
  state.tradesLoading = false
}

async function loadSummary() {
  state.summaryLoading = true
  state.summaryError = ''
  try {
    const params = new URLSearchParams()
    if (state.period !== 'custom') params.set('period', state.period)
    if (state.period === 'custom') {
      if (state.customFrom) params.set('from', new Date(state.customFrom).toISOString())
      if (state.customTo) params.set('to', new Date(state.customTo).toISOString())
    } else {
      const { from, to } = getPeriodDates(state.period)
      if (from) params.set('from', from)
      if (to) params.set('to', to)
    }
    const res = await fetchBackendGet(`/api/trades/summary?${params}`)
    if (res.ok) {
      state.summary = await res.json()
    }
  } catch {
    state.summaryError = '加载汇总数据失败'
  }
  state.summaryLoading = false
}

async function saveTrade() {
  state.tradeFormSaving = true
  state.tradeFormError = ''
  const price = Number(state.tradeForm.executedPrice)
  const qty = Number(state.tradeForm.quantity)
  if (!state.tradeForm.symbol?.trim()) { state.tradeFormError = '请输入股票代码'; state.tradeFormSaving = false; return }
  if (!price || price <= 0) { state.tradeFormError = '请输入有效的成交价'; state.tradeFormSaving = false; return }
  if (!qty || qty <= 0) { state.tradeFormError = '请输入有效的数量'; state.tradeFormSaving = false; return }
  if (!state.tradeForm.executedAt) { state.tradeFormError = '请选择成交时间'; state.tradeFormSaving = false; return }
  try {
    const f = state.tradeForm
    const isEdit = !!state.editingTradeId
    const dto = isEdit
      ? {
          executedPrice: Number(f.executedPrice),
          quantity: Number(f.quantity),
          executedAt: f.executedAt ? new Date(f.executedAt).toISOString() : new Date().toISOString(),
          commission: f.commission ? Number(f.commission) : 0,
          userNote: f.userNote || undefined,
        }
      : {
          planId: f.planId || undefined,
          symbol: f.symbol,
          name: f.name,
          direction: f.direction,
          tradeType: f.tradeType,
          executedPrice: Number(f.executedPrice),
          quantity: Number(f.quantity),
          executedAt: f.executedAt ? new Date(f.executedAt).toISOString() : new Date().toISOString(),
          commission: f.commission ? Number(f.commission) : 0,
          userNote: f.userNote || undefined,
        }
    const res = isEdit
      ? await fetchBackendPut(`/api/trades/${state.editingTradeId}`, dto)
      : await fetchBackendPost('/api/trades', dto)
    if (res.ok) {
      state.tradeModalOpen = false
      state.editingTradeId = null
      resetTradeForm()
      loadTrades()
      loadSummary()
      loadPortfolioSnapshot()
    } else {
      state.tradeFormError = await parseResponseMessage(res, '保存失败')
    }
  } catch {
    state.tradeFormError = '网络错误'
  }
  state.tradeFormSaving = false
}

async function deleteTrade(id) {
  if (!confirm('确定删除此交易记录？')) return
  try {
    const res = await fetchBackendDelete(`/api/trades/${id}`)
    if (res.ok) {
      loadTrades()
      loadSummary()
      loadPortfolioSnapshot()
    }
  } catch (err) {
    state.tradesError = '删除交易记录失败'
  }
}

function editTrade(trade) {
  state.editingTradeId = trade.id
  state.tradeForm = {
    planId: trade.planId,
    symbol: trade.symbol,
    name: trade.name,
    direction: trade.direction,
    tradeType: trade.tradeType,
    executedPrice: trade.executedPrice,
    quantity: trade.quantity,
    executedAt: formatDateTimeLocal(trade.executedAt),
    commission: trade.commission || '',
    userNote: trade.userNote || ''
  }
  state.tradeFormError = ''
  state.tradeModalOpen = true
}

async function saveSettings() {
  state.settingsSaving = true
  try {
    const res = await fetchBackendPut('/api/portfolio/settings', { totalCapital: Number(state.capitalInput) })
    if (res.ok) {
      state.settingsModalOpen = false
      loadPortfolioSnapshot()
    }
  } catch (err) {
    state.tradeFormError = '保存本金设置失败：' + (err?.message || '')
  }
  state.settingsSaving = false
}

function resetTradeForm() {
  Object.assign(state.tradeForm, {
    planId: null, symbol: '', name: '', direction: 'Buy', tradeType: 'Normal',
    executedPrice: '', quantity: '', executedAt: '', commission: '', userNote: ''
  })
}

function openQuickEntry() {
  state.editingTradeId = null
  resetTradeForm()
  state.tradeModalOpen = true
}

function openSettings() {
  state.capitalInput = state.snapshot?.totalCapital ?? ''
  state.settingsModalOpen = true
}

function onPeriodChange() {
  loadTrades()
  loadSummary()
}

function handleNavigateTradeLog(e) {
  const plan = e?.detail?.plan
  if (plan) {
    resetTradeForm()
    state.tradeForm.planId = plan.id
    state.tradeForm.symbol = plan.symbol || ''
    state.tradeForm.name = plan.name || ''
    state.tradeForm.direction = plan.direction === 'Short' ? 'Sell' : 'Buy'
    state.tradeModalOpen = true
  }
}

function handleGlobalClick() { state.reviewMenuOpen = false }

// ── Review functions ──
const reviewContentHtml = computed(() => {
  return state.reviewCurrent?.reviewContent ? markdownToSafeHtml(state.reviewCurrent.reviewContent) : ''
})

async function generateReview(type) {
  state.reviewMenuOpen = false
  state.reviewGenerating = true
  state.reviewError = ''
  state.reviewCurrent = null
  state.showReviewPanel = true
  try {
    const body = { type }
    if (type === 'custom' && state.period === 'custom') {
      if (state.customFrom) body.from = new Date(state.customFrom).toISOString()
      if (state.customTo) body.to = new Date(state.customTo).toISOString()
    }
    const res = await fetchBackendPost('/api/trades/reviews/generate', body)
    if (res.ok) {
      state.reviewCurrent = await res.json()
      loadReviewList()
      await nextTick()
      document.querySelector('.review-panel')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
    } else {
      state.reviewError = await parseResponseMessage(res, '生成复盘失败')
    }
  } catch {
    state.reviewError = '网络错误'
  }
  state.reviewGenerating = false
}

async function loadReviewList() {
  state.reviewListLoading = true
  try {
    const res = await fetchBackendGet('/api/trades/reviews')
    if (res.ok) {
      state.reviewList = await res.json()
    }
  } catch { /* silent */ }
  state.reviewListLoading = false
}

async function viewReview(id) {
  state.showReviewPanel = true
  state.reviewGenerating = true
  state.reviewError = ''
  try {
    const res = await fetchBackendGet(`/api/trades/reviews/${id}`)
    if (res.ok) {
      state.reviewCurrent = await res.json()
    } else {
      state.reviewError = '加载复盘详情失败'
    }
  } catch {
    state.reviewError = '网络错误'
  }
  state.reviewGenerating = false
}

function reviewTypeLabel(type) {
  const map = { Daily: '日复盘', Weekly: '周复盘', Monthly: '月复盘', Custom: '自定义' }
  return map[type] || type
}

onMounted(() => {
  loadPortfolioSnapshot()
  loadExposure()
  loadTrades()
  loadSummary()
  loadReviewList()
  loadBehaviorStats()
  window.addEventListener('navigate-trade-log', handleNavigateTradeLog)
  document.addEventListener('click', handleGlobalClick)
})

onUnmounted(() => {
  window.removeEventListener('navigate-trade-log', handleNavigateTradeLog)
  document.removeEventListener('click', handleGlobalClick)
})
</script>

<template>
  <div class="trade-log-tab">
    <!-- 持仓总览 -->
    <div class="portfolio-overview card" v-if="state.snapshot">
      <div class="portfolio-header">
        <h3>持仓总览</h3>
        <span class="badge" :class="positionRatioClass">
          仓位 {{ formatPercent(state.snapshot.totalPositionRatio) }}
        </span>
      </div>
      <div class="portfolio-metrics">
        <div class="metric">
          <span class="metric-label">总本金</span>
          <span class="metric-value">{{ formatMoney(state.snapshot.totalCapital) }}</span>
        </div>
        <div class="metric">
          <span class="metric-label">总市值</span>
          <span class="metric-value">{{ formatMoney(state.snapshot.totalMarketValue) }}</span>
        </div>
        <div class="metric">
          <span class="metric-label">总浮盈</span>
          <span class="metric-value" :class="pnlClass(state.snapshot.totalUnrealizedPnL)">
            {{ formatPnL(state.snapshot.totalUnrealizedPnL) }}
          </span>
        </div>
        <div class="metric">
          <span class="metric-label">可用资金</span>
          <span class="metric-value">{{ formatMoney(state.snapshot.availableCash) }}</span>
        </div>
      </div>
      <div class="position-bar">
        <div class="position-bar-fill" :style="{ width: positionBarWidth }" :class="positionBarClass"></div>
      </div>
      <div class="position-list" v-if="state.snapshot.positions?.length">
        <div class="position-item" v-for="p in state.snapshot.positions" :key="p.symbol">
          <span class="position-symbol">{{ p.symbol }} {{ p.name }}</span>
          <span>{{ p.quantity }}股</span>
          <span>成本 {{ p.averageCost?.toFixed(2) }}</span>
          <span :class="pnlClass(p.unrealizedPnL)">{{ formatPnL(p.unrealizedPnL) }}</span>
          <span class="badge badge-pill">{{ formatPercent(p.positionRatio) }}</span>
        </div>
      </div>
    </div>
    <div v-else-if="state.snapshotLoading" class="card loading-state">加载持仓中...</div>
    <div v-else-if="state.snapshotError" class="card text-danger" style="padding:1rem">{{ state.snapshotError }}</div>

    <!-- 仓位暴露条 -->
    <div class="exposure-bar-card card" v-if="state.exposure">
      <div class="exposure-header">
        <span class="exposure-title">风险敞口</span>
        <span class="badge" :class="exposureBadgeClass">{{ formatPercent(state.exposure.combinedExposure) }}</span>
        <span v-if="state.exposure.currentMode" class="execution-mode-tag" :class="'mode-' + state.exposure.currentMode.confirmationLevel">
          {{ state.exposure.currentMode.executionMode }}
        </span>
      </div>
      <div class="exposure-detail">
        <span class="exposure-item">
          <span class="exposure-label">真实暴露</span>
          <span class="exposure-value">{{ formatPercent(state.exposure.totalExposure) }}</span>
        </span>
        <span class="exposure-plus">+</span>
        <span class="exposure-item">
          <span class="exposure-label">待执行计划</span>
          <span class="exposure-value">{{ formatPercent(state.exposure.pendingExposure) }}</span>
        </span>
        <span class="exposure-eq">=</span>
        <span class="exposure-item">
          <span class="exposure-label">总风险敞口</span>
          <span class="exposure-value" :class="state.exposure.combinedExposure > 0.8 ? 'text-fall' : ''">{{ formatPercent(state.exposure.combinedExposure) }}</span>
        </span>
      </div>
      <div class="position-bar">
        <div class="position-bar-fill" :style="{ width: exposureBarWidth }" :class="exposureBarClass"></div>
      </div>
      <div v-if="state.exposure.combinedExposure > 0.8" class="exposure-warning">
        ⚠ 总风险敞口超过 80%，新建仓或加仓请谨慎
      </div>
      <div v-if="state.exposure.symbolExposures?.length" class="exposure-symbols">
        <span v-for="s in state.exposure.symbolExposures" :key="s.symbol" class="exposure-symbol-item">
          {{ s.name || s.symbol }} {{ formatPercent(s.exposure) }}
        </span>
      </div>
    </div>
    <div v-else-if="state.exposureLoading" class="card loading-state" style="padding:0.5rem">加载暴露数据中...</div>

    <!-- 交易健康度 -->
    <div class="behavior-dashboard card" v-if="state.behaviorStats">
      <div class="behavior-header">
        <h4>🧘 交易健康度</h4>
        <span class="discipline-score" :class="disciplineScoreClass">
          {{ state.behaviorStats.disciplineScore }} 分
        </span>
      </div>
      <div class="behavior-metrics">
        <div class="behavior-metric">
          <span class="metric-label">7日交易</span>
          <span class="metric-value">{{ state.behaviorStats.trades7Days }}笔</span>
        </div>
        <div class="behavior-metric">
          <span class="metric-label">计划执行率</span>
          <span class="metric-value" :class="planRateClass">
            {{ formatPercent(state.behaviorStats.planExecutionRate) }}
          </span>
        </div>
        <div class="behavior-metric">
          <span class="metric-label">当前连亏</span>
          <span class="metric-value" :class="lossStreakClass">
            {{ state.behaviorStats.currentLossStreak }}笔
          </span>
        </div>
        <div class="behavior-metric">
          <span class="metric-label">过度交易</span>
          <span class="metric-value" :class="{ 'text-danger': state.behaviorStats.isOverTrading }">
            {{ state.behaviorStats.isOverTrading ? '⚠️ 是' : '✅ 否' }}
          </span>
        </div>
      </div>
      <div class="behavior-alerts" v-if="state.behaviorStats.activeAlerts?.length">
        <div v-for="alert in state.behaviorStats.activeAlerts" :key="alert.alertType"
             class="behavior-alert" :class="'alert-' + alert.severity">
          {{ alert.message }}
        </div>
      </div>
    </div>
    <div v-else-if="state.behaviorStatsLoading" class="card loading-state" style="padding:0.5rem">加载健康度数据中...</div>

    <!-- 工具栏 -->
    <div class="toolbar">
      <div class="toolbar-filters">
        <button
          v-for="p in [{ key: 'day', label: '今日' }, { key: 'week', label: '本周' }, { key: 'month', label: '本月' }, { key: 'custom', label: '自定义' }]"
          :key="p.key"
          class="btn btn-sm"
          :class="state.period === p.key ? 'btn-primary' : 'btn-secondary'"
          @click="state.period = p.key; onPeriodChange()"
        >{{ p.label }}</button>
        <template v-if="state.period === 'custom'">
          <input class="input input-sm date-input" type="date" v-model="state.customFrom" @change="onPeriodChange" />
          <span class="text-secondary">至</span>
          <input class="input input-sm date-input" type="date" v-model="state.customTo" @change="onPeriodChange" />
        </template>
      </div>
      <div class="toolbar-actions">
        <div class="review-btn-group">
          <button class="btn btn-sm btn-accent" @click.stop="state.reviewMenuOpen = !state.reviewMenuOpen">
            📝 生成复盘总结
          </button>
          <div v-if="state.reviewMenuOpen" class="review-menu" @click.stop>
            <button class="review-menu-item" @click="generateReview('daily')">今日复盘</button>
            <button class="review-menu-item" @click="generateReview('weekly')">本周复盘</button>
            <button class="review-menu-item" @click="generateReview('monthly')">本月复盘</button>
            <button class="review-menu-item" @click="generateReview('custom')">自定义时段</button>
          </div>
        </div>
        <button class="btn btn-sm btn-primary" @click="openQuickEntry">快速录入</button>
        <button class="btn btn-sm btn-secondary" @click="openSettings">设置本金</button>
      </div>
    </div>

    <!-- 盈亏汇总 -->
    <div v-if="state.summaryLoading" class="loading-state" style="padding:1rem">汇总加载中...</div>
    <div v-else-if="state.summaryError" class="text-danger" style="padding:1rem">{{ state.summaryError }}</div>
    <div class="trade-summary card" v-else-if="state.summary">
      <div class="summary-grid">
        <div class="summary-item">
          <span class="summary-label">总盈亏</span>
          <span class="summary-value" :class="pnlClass(state.summary.totalPnL)">
            {{ formatPnL(state.summary.totalPnL) }}
          </span>
        </div>
        <div class="summary-item">
          <span class="summary-label">胜率</span>
          <span class="summary-value" :class="winRateClass">{{ formatPercent(state.summary.winRate) }}</span>
        </div>
        <div class="summary-item">
          <span class="summary-label">盈亏比</span>
          <span class="summary-value">{{ state.summary.profitLossRatio?.toFixed(2) ?? '-' }}</span>
        </div>
        <div class="summary-item">
          <span class="summary-label">做T盈亏</span>
          <span class="summary-value" :class="pnlClass(state.summary.dayTradePnL)">
            {{ formatPnL(state.summary.dayTradePnL) }}
          </span>
        </div>
        <div class="summary-item">
          <span class="summary-label">计划执行率</span>
          <span class="summary-value">{{ formatPercent(state.summary.plannedTradeCount / Math.max(1, state.summary.totalTrades)) }}</span>
        </div>
        <div class="summary-item">
          <span class="summary-label">Agent遵守率</span>
          <span class="summary-value">{{ formatPercent(state.summary.complianceRate) }}</span>
        </div>
        <div class="summary-item">
          <span class="summary-label">最大单笔亏损</span>
          <span class="summary-value text-fall">{{ formatPnL(state.summary.maxSingleLoss) }}</span>
        </div>
      </div>
    </div>

    <!-- 交易记录列表 -->
    <div class="trade-list">
      <div class="trade-item card card-compact" v-for="t in state.trades" :key="t.id">
        <div class="trade-item-header">
          <span class="trade-symbol">{{ t.symbol }} {{ t.name }}</span>
          <span class="badge" :class="directionBadgeClass(t.direction)">{{ t.direction === 'Buy' ? '买入' : '卖出' }}</span>
          <span class="badge badge-pill" v-if="t.tradeType === 'DayTrade'">做T</span>
          <span class="badge" :class="complianceBadgeClass(t.complianceTag)">
            {{ complianceLabel(t.complianceTag) }}
          </span>
          <span class="trade-time">{{ formatDateTime(t.executedAt) }}</span>
        </div>
        <div class="trade-item-body">
          <span>{{ t.executedPrice?.toFixed(2) }} × {{ t.quantity }}股</span>
          <span v-if="t.realizedPnL != null" :class="pnlClass(t.realizedPnL)">
            盈亏 {{ formatPnL(t.realizedPnL) }} ({{ formatPercent(t.returnRate) }})
          </span>
          <span v-if="t.planTitle" class="text-secondary">计划: {{ t.planTitle }}</span>
          <span v-if="t.agentDirection" class="text-secondary">Agent: {{ t.agentDirection }} ({{ formatPercent(t.agentConfidence) }})</span>
        </div>
        <div class="trade-item-actions">
          <button class="btn btn-ghost btn-sm" @click="editTrade(t)">编辑</button>
          <button class="btn btn-ghost btn-sm" @click="deleteTrade(t.id)">删除</button>
        </div>
      </div>
      <div v-if="!state.tradesLoading && !state.trades.length" class="empty-state">暂无交易记录</div>
      <div v-if="state.tradesLoading" class="loading-state">加载中...</div>
      <div v-if="state.tradesError" class="text-danger" style="padding:0.5rem">{{ state.tradesError }}</div>
    </div>

    <!-- 复盘面板 -->
    <div v-if="state.showReviewPanel" class="review-panel card">
      <div class="review-panel-header">
        <h3>📋 交易复盘</h3>
        <button class="btn btn-ghost btn-sm" @click="state.showReviewPanel = false; state.reviewCurrent = null">✕</button>
      </div>
      <div v-if="state.reviewGenerating" class="loading-state">
        <span class="review-spinner"></span> AI 教练正在分析你的交易记录...
      </div>
      <div v-else-if="state.reviewError" class="text-danger" style="padding:1rem">{{ state.reviewError }}</div>
      <div v-else-if="state.reviewCurrent" class="review-content">
        <div class="review-meta">
          <span class="badge badge-accent">{{ reviewTypeLabel(state.reviewCurrent.reviewType) }}</span>
          <span class="text-secondary">{{ formatDateTime(state.reviewCurrent.periodStart) }} - {{ formatDateTime(state.reviewCurrent.periodEnd) }}</span>
          <span>{{ state.reviewCurrent.tradeCount }} 笔卖出</span>
          <span :class="pnlClass(state.reviewCurrent.totalPnL)">盈亏 {{ formatPnL(state.reviewCurrent.totalPnL) }}</span>
          <span>胜率 {{ formatPercent(state.reviewCurrent.winRate) }}</span>
        </div>
        <div class="review-body markdown-body" v-html="reviewContentHtml"></div>
      </div>
    </div>

    <!-- 复盘历史 -->
    <div v-if="state.reviewList.length" class="review-history card">
      <h3>📆 复盘历史</h3>
      <div class="review-history-list">
        <div class="review-history-item" v-for="r in state.reviewList" :key="r.id" @click="viewReview(r.id)">
          <span class="badge" :class="r.reviewType === 'Daily' ? 'badge-info' : r.reviewType === 'Weekly' ? 'badge-success' : 'badge-warning'">
            {{ reviewTypeLabel(r.reviewType) }}
          </span>
          <span class="text-secondary">{{ formatDateTime(r.periodStart) }}</span>
          <span :class="pnlClass(r.totalPnL)">{{ formatPnL(r.totalPnL) }}</span>
          <span>胜率 {{ formatPercent(r.winRate) }}</span>
        </div>
      </div>
    </div>

    <!-- 交易录入弹窗 -->
    <div v-if="state.tradeModalOpen" class="trade-modal-backdrop" @click="state.tradeModalOpen = false">
      <div class="trade-modal card card-elevated" @click.stop>
        <div class="trade-modal-header">
          <h3>{{ state.editingTradeId ? '编辑交易' : (state.tradeForm.planId ? '录入执行' : '快速录入') }}</h3>
          <button class="btn btn-ghost btn-sm" @click="state.tradeModalOpen = false">✕</button>
        </div>
        <form @submit.prevent="saveTrade" class="trade-form">
          <div class="form-grid">
            <div class="form-group">
              <label>股票代码</label>
              <input class="input input-sm" v-model="state.tradeForm.symbol" required :disabled="!!state.tradeForm.planId || !!state.editingTradeId" />
            </div>
            <div class="form-group">
              <label>股票名称</label>
              <input class="input input-sm" v-model="state.tradeForm.name" required :disabled="!!state.editingTradeId" />
            </div>
            <div class="form-group">
              <label>方向</label>
              <select class="input input-sm" v-model="state.tradeForm.direction" :disabled="!!state.editingTradeId">
                <option value="Buy">买入</option>
                <option value="Sell">卖出</option>
              </select>
            </div>
            <div class="form-group">
              <label>类型</label>
              <select class="input input-sm" v-model="state.tradeForm.tradeType" :disabled="!!state.editingTradeId">
                <option value="Normal">普通</option>
                <option value="DayTrade">做T</option>
              </select>
            </div>
            <div class="form-group">
              <label>成交价</label>
              <input class="input input-sm" type="number" step="0.01" v-model="state.tradeForm.executedPrice" required />
            </div>
            <div class="form-group">
              <label>数量（股）</label>
              <input class="input input-sm" type="number" step="100" v-model="state.tradeForm.quantity" required />
            </div>
            <div class="form-group">
              <label>成交时间</label>
              <input class="input input-sm" type="datetime-local" v-model="state.tradeForm.executedAt" required />
            </div>
            <div class="form-group">
              <label>手续费</label>
              <input class="input input-sm" type="number" step="0.01" v-model="state.tradeForm.commission" />
            </div>
            <div class="form-group form-group-full">
              <label>备注</label>
              <textarea class="input input-sm" v-model="state.tradeForm.userNote" rows="2"></textarea>
            </div>
          </div>
          <div class="trade-modal-actions">
            <button type="button" class="btn btn-secondary btn-sm" @click="state.tradeModalOpen = false">取消</button>
            <button type="submit" class="btn btn-primary btn-sm" :disabled="state.tradeFormSaving">
              {{ state.tradeFormSaving ? '保存中...' : '保存' }}
            </button>
          </div>
          <div v-if="state.tradeFormError" class="text-danger" style="margin-top:8px">{{ state.tradeFormError }}</div>
        </form>
      </div>
    </div>

    <!-- 本金设置弹窗 -->
    <div v-if="state.settingsModalOpen" class="trade-modal-backdrop" @click="state.settingsModalOpen = false">
      <div class="trade-modal card card-elevated" style="max-width: 400px" @click.stop>
        <div class="trade-modal-header">
          <h3>设置本金</h3>
          <button class="btn btn-ghost btn-sm" @click="state.settingsModalOpen = false">✕</button>
        </div>
        <form @submit.prevent="saveSettings">
          <div class="form-group">
            <label>总本金（元）</label>
            <input class="input" type="number" step="0.01" v-model="state.capitalInput" required />
          </div>
          <div class="trade-modal-actions" style="margin-top:12px">
            <button type="button" class="btn btn-secondary btn-sm" @click="state.settingsModalOpen = false">取消</button>
            <button type="submit" class="btn btn-primary btn-sm" :disabled="state.settingsSaving">保存</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.trade-log-tab {
  display: grid;
  gap: var(--space-4);
  padding: var(--space-5);
  max-width: 960px;
  margin: 0 auto;
}

/* ── 持仓总览 ── */
.portfolio-overview {
  display: grid;
  gap: var(--space-3);
}
.portfolio-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.portfolio-header h3 {
  margin: 0;
  font-size: var(--text-lg);
  font-weight: 700;
  color: var(--color-text-primary);
}
.portfolio-metrics {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--space-3);
}
.metric {
  display: flex;
  flex-direction: column;
  gap: var(--space-0-5);
}
.metric-label {
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
}
.metric-value {
  font-size: var(--text-lg);
  font-weight: 700;
  color: var(--color-text-primary);
  font-family: var(--font-family-mono);
}
.position-bar {
  height: 6px;
  border-radius: var(--radius-full);
  background: var(--color-bg-inset);
  overflow: hidden;
}
.position-bar-fill {
  height: 100%;
  border-radius: var(--radius-full);
  transition: width var(--transition-normal);
}
.bar-safe { background: var(--color-success); }
.bar-warning { background: var(--color-warning); }
.bar-danger { background: var(--color-danger); }

.position-list {
  display: grid;
  gap: var(--space-2);
  margin-top: var(--space-1);
}
.position-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  font-size: var(--text-sm);
  color: var(--color-text-body);
  padding: var(--space-1-5) 0;
  border-bottom: 1px solid var(--color-border-light);
}
.position-item:last-child { border-bottom: none; }
.position-symbol {
  font-weight: 600;
  min-width: 120px;
}

/* ── 仓位暴露条 ── */
.exposure-bar-card {
  display: grid;
  gap: var(--space-2);
}
.exposure-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.exposure-title {
  font-size: var(--text-base);
  font-weight: 700;
  color: var(--color-text-primary);
}
.exposure-detail {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  font-size: var(--text-sm);
  flex-wrap: wrap;
}
.exposure-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
  text-align: center;
}
.exposure-label {
  font-size: var(--text-xs);
  color: var(--color-text-muted);
}
.exposure-value {
  font-weight: 700;
  font-family: var(--font-family-mono);
  color: var(--color-text-primary);
}
.exposure-plus, .exposure-eq {
  font-weight: 700;
  color: var(--color-text-muted);
}
.exposure-warning {
  font-size: var(--text-sm);
  color: var(--color-danger);
  font-weight: 600;
  padding: var(--space-1) var(--space-2);
  background: var(--color-danger-subtle);
  border-radius: var(--radius-md);
}
.exposure-symbols {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}
.exposure-symbol-item {
  font-size: var(--text-xs);
  padding: 2px 6px;
  background: var(--color-bg-inset);
  border-radius: var(--radius-sm);
  color: var(--color-text-body);
}

/* ── 工具栏 ── */
.toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  flex-wrap: wrap;
}
.toolbar-filters {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  flex-wrap: wrap;
}
.toolbar-actions {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}
.date-input {
  width: 140px;
}

/* ── 盈亏汇总 ── */
.trade-summary {
  display: grid;
  gap: var(--space-3);
}
.summary-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--space-3);
}
.summary-item {
  display: flex;
  flex-direction: column;
  gap: var(--space-0-5);
}
.summary-label {
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
}
.summary-value {
  font-size: var(--text-lg);
  font-weight: 700;
  color: var(--color-text-primary);
  font-family: var(--font-family-mono);
}

/* ── 交易列表 ── */
.trade-list {
  display: grid;
  gap: var(--space-2);
}
.trade-item {
  display: grid;
  gap: var(--space-2);
}
.trade-item-header {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  flex-wrap: wrap;
}
.trade-symbol {
  font-weight: 700;
  color: var(--color-text-primary);
}
.trade-time {
  margin-left: auto;
  font-size: var(--text-sm);
  color: var(--color-text-muted);
}
.trade-item-body {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  font-size: var(--text-sm);
  color: var(--color-text-body);
  flex-wrap: wrap;
}
.trade-item-actions {
  display: flex;
  justify-content: flex-end;
}

/* ── 弹窗 ── */
.trade-modal-backdrop {
  position: fixed;
  inset: 0;
  background: var(--color-bg-overlay);
  backdrop-filter: blur(4px);
  z-index: var(--z-modal);
  display: flex;
  align-items: center;
  justify-content: center;
}
.trade-modal {
  width: 90%;
  max-width: 560px;
  max-height: 90vh;
  overflow-y: auto;
}
.trade-modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-4);
}
.trade-modal-header h3 {
  margin: 0;
  font-size: var(--text-lg);
  font-weight: 700;
  color: var(--color-text-primary);
}
.trade-modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-2);
  margin-top: var(--space-4);
}

/* ── 表单 ── */
.form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-3);
}
.form-group {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}
.form-group label {
  font-size: var(--text-sm);
  font-weight: 600;
  color: var(--color-text-secondary);
}
.form-group-full {
  grid-column: 1 / -1;
}

/* ── 状态 ── */
.loading-state {
  text-align: center;
  padding: var(--space-6);
  color: var(--color-text-muted);
  font-size: var(--text-sm);
}
.text-rise { color: var(--color-market-rise); }
.text-fall { color: var(--color-market-fall); }
.text-secondary { color: var(--color-text-secondary); }
.text-danger { color: var(--color-danger); }

@media (max-width: 640px) {
  .portfolio-metrics,
  .summary-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .form-grid {
    grid-template-columns: 1fr;
  }
  .form-group-full {
    grid-column: auto;
  }
}

.execution-mode-tag {
  display: inline-flex;
  align-items: center;
  border-radius: 999px;
  padding: 0.15rem 0.55rem;
  font-size: 0.75rem;
  font-weight: 600;
  white-space: nowrap;
}

.execution-mode-tag.mode-normal { background: rgba(22, 163, 74, 0.12); color: #15803d; }
.execution-mode-tag.mode-confirm { background: rgba(234, 179, 8, 0.15); color: #a16207; }
.execution-mode-tag.mode-strong-confirm { background: rgba(234, 179, 8, 0.25); color: #92400e; }
.execution-mode-tag.mode-discouraged { background: rgba(239, 68, 68, 0.15); color: #b91c1c; }

/* ── 复盘按钮组 ── */
.review-btn-group {
  position: relative;
}
.btn-accent {
  background: var(--color-accent, #6366f1);
  color: #fff;
  border: none;
  cursor: pointer;
  border-radius: var(--radius-md);
  padding: 0.25rem 0.75rem;
  font-size: var(--text-sm);
  font-weight: 600;
}
.btn-accent:hover {
  opacity: 0.9;
}
.review-menu {
  position: absolute;
  top: 100%;
  right: 0;
  margin-top: 4px;
  background: var(--color-bg-elevated, #fff);
  border: 1px solid var(--color-border-light);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-lg, 0 4px 12px rgba(0,0,0,0.1));
  z-index: 10;
  min-width: 140px;
  overflow: hidden;
}
.review-menu-item {
  display: block;
  width: 100%;
  padding: 0.5rem 0.75rem;
  border: none;
  background: none;
  text-align: left;
  font-size: var(--text-sm);
  color: var(--color-text-body);
  cursor: pointer;
}
.review-menu-item:hover {
  background: var(--color-bg-hover, #f3f4f6);
}

/* ── 复盘面板 ── */
.review-panel {
  display: grid;
  gap: var(--space-3);
}
.review-panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.review-panel-header h3 {
  margin: 0;
  font-size: var(--text-lg);
  font-weight: 700;
  color: var(--color-text-primary);
}
.review-meta {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  flex-wrap: wrap;
  font-size: var(--text-sm);
  color: var(--color-text-body);
  margin-bottom: var(--space-3);
}
.badge-accent {
  background: rgba(99, 102, 241, 0.12);
  color: #6366f1;
}
.review-body {
  font-size: var(--text-sm);
  line-height: 1.7;
  color: var(--color-text-body);
}
.review-body :deep(h3) {
  font-size: var(--text-base);
  font-weight: 700;
  color: var(--color-text-primary);
  margin: 1rem 0 0.5rem;
}
.review-body :deep(ul),
.review-body :deep(ol) {
  padding-left: 1.2em;
}
.review-body :deep(li) {
  margin: 0.25rem 0;
}
.review-spinner {
  display: inline-block;
  width: 16px;
  height: 16px;
  border: 2px solid var(--color-border-light);
  border-top-color: var(--color-accent, #6366f1);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

/* ── 复盘历史 ── */
.review-history {
  display: grid;
  gap: var(--space-2);
}
.review-history h3 {
  margin: 0;
  font-size: var(--text-base);
  font-weight: 700;
  color: var(--color-text-primary);
}
.review-history-list {
  display: grid;
  gap: var(--space-1);
}
.review-history-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-2) var(--space-1);
  border-bottom: 1px solid var(--color-border-light);
  cursor: pointer;
  font-size: var(--text-sm);
  transition: background var(--transition-fast);
}
.review-history-item:hover {
  background: var(--color-bg-hover, #f3f4f6);
}
.review-history-item:last-child {
  border-bottom: none;
}

/* ── 交易健康度 ── */
.behavior-dashboard {
  display: grid;
  gap: var(--space-3);
}
.behavior-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.behavior-header h4 {
  margin: 0;
  font-size: var(--text-base);
  font-weight: 700;
  color: var(--color-text-primary);
}
.discipline-score {
  font-size: var(--text-xl, 1.25rem);
  font-weight: 800;
  font-family: var(--font-family-mono);
  padding: 0.15rem 0.6rem;
  border-radius: var(--radius-md);
}
.score-good { color: #15803d; background: rgba(22, 163, 74, 0.12); }
.score-warn { color: #a16207; background: rgba(234, 179, 8, 0.15); }
.score-danger { color: #b91c1c; background: rgba(239, 68, 68, 0.15); }
.behavior-metrics {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--space-3);
}
.behavior-metric {
  display: flex;
  flex-direction: column;
  gap: var(--space-0-5);
}
.text-warning { color: var(--color-warning, #a16207); }
.behavior-alerts {
  display: grid;
  gap: var(--space-1-5);
}
.behavior-alert {
  font-size: var(--text-sm);
  padding: var(--space-1-5) var(--space-2);
  border-radius: var(--radius-md);
  font-weight: 500;
}
.alert-info { background: rgba(59, 130, 246, 0.1); color: #1d4ed8; }
.alert-warning { background: rgba(234, 179, 8, 0.15); color: #92400e; }
.alert-danger { background: rgba(239, 68, 68, 0.12); color: #b91c1c; }

@media (max-width: 640px) {
  .behavior-metrics {
    grid-template-columns: repeat(2, 1fr);
  }
}
</style>
