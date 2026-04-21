<script setup>
import { computed } from 'vue'
import {
  formatPlanAlertSummary,
  formatTradingPlanDateRange,
  formatTradingPlanScenario,
  getLatestPlanAlert,
  getPlanAlertClass,
  getTradingPlanExpiryText
} from './stockInfoTabTradingPlans'
import {
  formatTradingPlanStatus,
  getTradingPlanStatusClass,
  normalizeTradingPlanStatus
} from './tradingPlanReview'

const props = defineProps({
  plans: {
    type: Array,
    default: () => []
  },
  planAlerts: {
    type: Array,
    default: () => []
  },
  loading: {
    type: Boolean,
    default: false
  },
  error: {
    type: String,
    default: ''
  },
  selectedPlanId: {
    type: [String, Number],
    default: null
  },
  selectedPlanContext: {
    type: Object,
    default: null
  },
  searchValue: {
    type: String,
    default: ''
  },
  scope: {
    type: String,
    default: 'active'
  },
  refreshing: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits([
  'update:searchValue',
  'update:scope',
  'refresh',
  'select',
  'record-trade',
  'view-stock'
])

const ACTIVE_PLAN_STATUSES = ['Pending', 'Triggered', 'ReviewRequired']

const normalizedQuery = computed(() => props.searchValue.trim().toLowerCase())
const activePlanCount = computed(() => props.plans.filter(isActivePlan).length)
const visiblePlans = computed(() => {
  const list = props.scope === 'all'
    ? props.plans
    : props.plans.filter(isActivePlan)

  if (!normalizedQuery.value) {
    return list
  }

  return list.filter(item => {
    const haystack = [item.symbol, item.name, item.analysisSummary, item.expectedCatalyst]
      .filter(Boolean)
      .join(' ')
      .toLowerCase()
    return haystack.includes(normalizedQuery.value)
  })
})

function isActivePlan(item) {
  return ACTIVE_PLAN_STATUSES.includes(normalizeTradingPlanStatus(item?.status))
}

function isSelected(item) {
  return String(props.selectedPlanId) === String(item?.id)
}

function formatPlanPrice(value) {
  if (value == null || value === '') return '--'
  return Number(value).toFixed(2)
}

function getReferencePrice(item) {
  const selectedContext = isSelected(item) ? props.selectedPlanContext : null
  return selectedContext?.currentPositionSnapshot?.latestPrice
    ?? selectedContext?.scenarioStatus?.referencePrice
    ?? item?.currentPositionSnapshot?.latestPrice
    ?? item?.currentScenarioStatus?.referencePrice
    ?? null
}

function getReferencePriceLabel(item) {
  return getReferencePrice(item) == null ? '参考价待更新' : '参考价'
}

function getPlanAlertSignal(item) {
  const alert = getLatestPlanAlert({ planAlerts: props.planAlerts }, item.id)
  if (alert) {
    return {
      label: '告警',
      text: formatPlanAlertSummary(alert),
      className: getPlanAlertClass(alert.severity)
    }
  }

  return {
    label: '告警',
    text: '暂无告警',
    className: 'plan-signal-muted'
  }
}

function getPlanExecutionSignal(item) {
  if (item.executionSummary?.summary) {
    return {
      label: '执行摘要',
      text: item.executionSummary.summary,
      className: 'plan-signal-execution'
    }
  }

  const executionBits = []
  if (item.executionSummary?.executionCount) {
    executionBits.push(`已执行 ${item.executionSummary.executionCount} 次`)
  }
  if (item.executionSummary?.latestAction) {
    executionBits.push(`最近动作 ${item.executionSummary.latestAction}`)
  }

  if (executionBits.length) {
    return {
      label: '执行摘要',
      text: executionBits.join(' · '),
      className: 'plan-signal-execution'
    }
  }

  if (item.currentScenarioStatus?.summary || item.currentScenarioStatus?.reason) {
    return {
      label: '执行摘要',
      text: `尚无执行回写 · ${item.currentScenarioStatus.summary || item.currentScenarioStatus.reason}`,
      className: 'plan-signal-scenario'
    }
  }

  return {
    label: '执行摘要',
    text: '尚无执行回写',
    className: 'plan-signal-summary'
  }
}

function canRecordTrade(item) {
  return ['Pending', 'Triggered'].includes(normalizeTradingPlanStatus(item?.status))
}

function updateSearchValue(event) {
  emit('update:searchValue', event.target.value)
}
</script>

<template>
  <section class="trade-plan-workspace card" data-testid="trade-plan-workspace">
    <div class="trade-plan-workspace__header">
      <div>
        <h3>计划管理工作区</h3>
        <p class="text-secondary">用于交易后查看、筛选和联动复盘，不替代单票深度编辑。</p>
      </div>
      <div class="trade-plan-workspace__counts">
        <span class="plan-count-pill">活跃 {{ activePlanCount }}</span>
        <span class="plan-count-pill plan-count-pill-subtle">全部 {{ plans.length }}</span>
      </div>
    </div>

    <div class="trade-plan-workspace__toolbar">
      <label class="trade-plan-search">
        <span class="sr-only">搜索股票代码或名称</span>
        <input
          class="input input-sm"
          data-testid="trade-plan-search"
          type="search"
          :value="searchValue"
          placeholder="搜索股票代码 / 名称"
          @input="updateSearchValue"
        >
      </label>
      <div class="trade-plan-scope-switch" role="tablist" aria-label="计划范围切换">
        <button
          type="button"
          class="btn btn-sm"
          :class="scope === 'active' ? 'btn-primary' : 'btn-secondary'"
          @click="emit('update:scope', 'active')"
        >活跃计划</button>
        <button
          type="button"
          class="btn btn-sm"
          :class="scope === 'all' ? 'btn-primary' : 'btn-secondary'"
          @click="emit('update:scope', 'all')"
        >全部计划</button>
      </div>
      <button
        type="button"
        class="btn btn-sm btn-secondary trade-plan-refresh"
        :disabled="refreshing"
        @click="emit('refresh')"
      >{{ refreshing ? '刷新中...' : '手动刷新' }}</button>
    </div>

    <div v-if="error && !plans.length" class="trade-plan-feedback trade-plan-feedback-error">
      <strong>交易计划加载失败</strong>
      <p>{{ error }}</p>
      <button type="button" class="btn btn-sm btn-secondary" @click="emit('refresh')">重试</button>
    </div>

    <div v-else-if="loading && !plans.length" class="trade-plan-skeleton-list" aria-hidden="true">
      <div v-for="index in 5" :key="index" class="trade-plan-skeleton-item">
        <span class="trade-plan-skeleton-line trade-plan-skeleton-line-short"></span>
        <span class="trade-plan-skeleton-line"></span>
        <span class="trade-plan-skeleton-line trade-plan-skeleton-line-long"></span>
      </div>
    </div>

    <div v-else-if="!plans.length" class="trade-plan-feedback trade-plan-feedback-empty">
      <strong>暂无交易计划</strong>
      <p>先去股票信息页建立计划，后续会在这里统一跟踪。</p>
    </div>

    <div v-else-if="!visiblePlans.length" class="trade-plan-feedback trade-plan-feedback-empty">
      <strong>当前没有匹配的计划</strong>
      <p>试试清空关键词，或切换到“全部计划”看看。</p>
    </div>

    <div v-else class="trade-plan-list">
      <article
        v-for="item in visiblePlans"
        :key="item.id"
        :data-testid="`trade-plan-item-${item.id}`"
        class="trade-plan-item"
        :class="{ 'trade-plan-item-selected': isSelected(item) }"
        @click="emit('select', item)"
      >
        <div class="trade-plan-item__header">
          <div class="trade-plan-item__identity">
            <div class="trade-plan-item__title-row">
              <strong>{{ item.symbol }} {{ item.name }}</strong>
              <span class="plan-status-badge" :class="getTradingPlanStatusClass(item.status)">{{ formatTradingPlanStatus(item.status) }}</span>
            </div>
            <p class="trade-plan-item__summary">{{ item.analysisSummary || item.expectedCatalyst || '等待补充计划摘要' }}</p>
          </div>
          <div class="trade-plan-item__price-grid">
            <div class="trade-plan-price-box">
              <span class="trade-plan-price-label">{{ getReferencePriceLabel(item) }}</span>
              <strong class="trade-plan-price-value">{{ formatPlanPrice(getReferencePrice(item)) }}</strong>
            </div>
            <div class="trade-plan-price-box">
              <span class="trade-plan-price-label">触发价</span>
              <strong class="trade-plan-price-value">{{ formatPlanPrice(item.triggerPrice) }}</strong>
            </div>
          </div>
        </div>

        <div class="trade-plan-item__meta">
          <span v-if="item.activeScenario" class="plan-pill">{{ formatTradingPlanScenario(item.activeScenario) }}</span>
          <span v-if="formatTradingPlanDateRange(item.planStartDate, item.planEndDate)" class="plan-pill">{{ formatTradingPlanDateRange(item.planStartDate, item.planEndDate) }}</span>
          <span v-if="getTradingPlanExpiryText(item)" class="plan-pill">{{ getTradingPlanExpiryText(item) }}</span>
          <span v-if="item.currentScenarioStatus?.label" class="plan-pill plan-pill-scene">{{ item.currentScenarioStatus.label }}</span>
          <span v-if="item.currentMarketContext?.stageLabel" class="plan-pill">当前 {{ item.currentMarketContext.stageLabel }}</span>
          <span v-if="item.executionSummary?.executionCount" class="plan-pill">已执行 {{ item.executionSummary.executionCount }} 次</span>
        </div>

        <div class="trade-plan-signal-grid">
          <div
            class="trade-plan-signal"
            :class="getPlanAlertSignal(item).className"
            :data-testid="`trade-plan-alert-${item.id}`"
          >
            <strong>{{ getPlanAlertSignal(item).label }}</strong>
            <span>{{ getPlanAlertSignal(item).text }}</span>
          </div>
          <div
            class="trade-plan-signal"
            :class="getPlanExecutionSignal(item).className"
            :data-testid="`trade-plan-execution-${item.id}`"
          >
            <strong>{{ getPlanExecutionSignal(item).label }}</strong>
            <span>{{ getPlanExecutionSignal(item).text }}</span>
          </div>
        </div>

        <div class="trade-plan-item__actions">
          <div class="trade-plan-linkage-action">
            <button
              type="button"
              class="btn btn-sm"
              :class="isSelected(item) ? 'btn-primary' : 'btn-secondary'"
              @click.stop="emit('select', item)"
            >{{ isSelected(item) ? '已联动复盘' : '选中联动' }}</button>
            <p :data-testid="`trade-plan-linkage-hint-${item.id}`" class="trade-plan-linkage-hint">
              {{ isSelected(item)
                ? '已联动右侧复盘，并筛选下方关联交易。'
                : '会切右侧复盘，并筛选下方关联交易。' }}
            </p>
          </div>
          <div class="trade-plan-item__action-buttons">
            <button type="button" class="btn btn-sm btn-secondary" @click.stop="emit('view-stock', item)">查看股票</button>
            <button
              v-if="canRecordTrade(item)"
              type="button"
              class="btn btn-sm btn-primary"
              @click.stop="emit('record-trade', item)"
            >录入执行</button>
          </div>
        </div>
      </article>
    </div>

    <p v-if="error && plans.length" class="trade-plan-feedback-inline">{{ error }}</p>
  </section>
</template>

<style scoped>
.trade-plan-workspace {
  display: grid;
  gap: var(--space-3);
}

.trade-plan-workspace__header,
.trade-plan-workspace__toolbar,
.trade-plan-item__header,
.trade-plan-item__actions {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-3);
}

.trade-plan-workspace__header h3 {
  margin: 0;
  font-size: var(--text-lg);
}

.trade-plan-workspace__header p {
  margin: 4px 0 0;
}

.trade-plan-workspace__counts,
.trade-plan-scope-switch {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  flex-wrap: wrap;
}

.trade-plan-search {
  min-width: min(320px, 100%);
  flex: 1;
}

.trade-plan-search input {
  width: 100%;
}

.plan-count-pill,
.plan-pill {
  display: inline-flex;
  align-items: center;
  border-radius: 999px;
  padding: 0.18rem 0.6rem;
  font-size: 0.78rem;
}

.plan-count-pill {
  background: rgba(37, 99, 235, 0.08);
  color: #1d4ed8;
}

.plan-count-pill-subtle {
  background: rgba(148, 163, 184, 0.14);
  color: #475569;
}

.plan-pill {
  background: rgba(37, 99, 235, 0.08);
  color: #1d4ed8;
}

.plan-pill-scene {
  background: rgba(14, 165, 233, 0.1);
  color: #0369a1;
}

.trade-plan-list,
.trade-plan-skeleton-list {
  display: grid;
  gap: var(--space-3);
}

.trade-plan-item {
  display: grid;
  gap: var(--space-3);
  padding: 1rem;
  border-radius: 18px;
  border: 1px solid rgba(148, 163, 184, 0.18);
  background: rgba(248, 250, 252, 0.92);
  cursor: pointer;
  transition: border-color var(--transition-fast), box-shadow var(--transition-fast), transform var(--transition-fast);
}

.trade-plan-item:hover {
  border-color: rgba(59, 130, 246, 0.3);
  box-shadow: 0 10px 24px rgba(15, 23, 42, 0.06);
}

.trade-plan-item-selected {
  border-color: rgba(37, 99, 235, 0.36);
  box-shadow: 0 0 0 1px rgba(37, 99, 235, 0.08);
  background: linear-gradient(180deg, rgba(239, 246, 255, 0.95), rgba(248, 250, 252, 0.95));
}

.trade-plan-item__identity,
.trade-plan-item__price-grid,
.trade-plan-price-box {
  display: grid;
  gap: 6px;
}

.trade-plan-item__identity {
  flex: 1;
  min-width: 0;
}

.trade-plan-item__title-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.trade-plan-item__summary {
  margin: 0;
  color: var(--color-text-body);
  line-height: 1.5;
}

.trade-plan-item__price-grid {
  grid-template-columns: repeat(2, minmax(92px, auto));
}

.trade-plan-price-box {
  justify-items: end;
}

.trade-plan-price-label {
  font-size: var(--text-xs);
  color: var(--color-text-muted);
}

.trade-plan-price-value {
  font-size: var(--text-lg);
  font-family: var(--font-family-mono);
  color: var(--color-text-primary);
}

.trade-plan-item__meta {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  flex-wrap: wrap;
}

.trade-plan-signal-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--space-2);
}

.trade-plan-signal {
  display: grid;
  gap: 6px;
  padding: 0.7rem 0.85rem;
  border-radius: 14px;
  border: 1px solid rgba(148, 163, 184, 0.18);
  background: rgba(255, 255, 255, 0.7);
  font-size: var(--text-sm);
}

.trade-plan-signal strong {
  font-size: 0.78rem;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.trade-plan-signal span {
  display: -webkit-box;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
  overflow: hidden;
  line-height: 1.45;
}

.trade-plan-signal.plan-alert-warning {
  background: rgba(245, 158, 11, 0.12);
  border-color: rgba(245, 158, 11, 0.2);
  color: #92400e;
}

.trade-plan-signal.plan-alert-critical {
  background: rgba(239, 68, 68, 0.12);
  border-color: rgba(239, 68, 68, 0.22);
  color: #991b1b;
}

.trade-plan-signal.plan-alert-info,
.trade-plan-signal.plan-signal-scenario {
  background: rgba(14, 165, 233, 0.1);
  border-color: rgba(14, 165, 233, 0.16);
  color: #0c4a6e;
}

.trade-plan-signal.plan-signal-execution {
  background: rgba(22, 163, 74, 0.08);
  border-color: rgba(22, 163, 74, 0.16);
  color: #166534;
}

.trade-plan-signal.plan-signal-summary {
  color: var(--color-text-secondary);
}

.trade-plan-signal.plan-signal-muted {
  color: var(--color-text-secondary);
  background: rgba(248, 250, 252, 0.9);
}

.trade-plan-linkage-action {
  display: grid;
  gap: 4px;
}

.trade-plan-linkage-hint {
  margin: 0;
  font-size: var(--text-xs);
  color: var(--color-text-secondary);
}

.trade-plan-item__action-buttons {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: var(--space-2);
  flex-wrap: wrap;
}

.trade-plan-feedback {
  display: grid;
  gap: 8px;
  padding: 1rem;
  border-radius: 16px;
  border: 1px dashed rgba(148, 163, 184, 0.4);
  background: rgba(248, 250, 252, 0.72);
}

.trade-plan-feedback p,
.trade-plan-feedback-inline {
  margin: 0;
  color: var(--color-text-secondary);
}

.trade-plan-feedback-error {
  border-color: rgba(239, 68, 68, 0.28);
  background: rgba(254, 242, 242, 0.88);
}

.trade-plan-feedback-inline {
  font-size: var(--text-sm);
}

.trade-plan-skeleton-item {
  display: grid;
  gap: 0.6rem;
  padding: 1rem;
  border-radius: 18px;
  border: 1px solid rgba(148, 163, 184, 0.14);
  background: rgba(248, 250, 252, 0.82);
}

.trade-plan-skeleton-line {
  display: block;
  height: 12px;
  border-radius: 999px;
  background: linear-gradient(90deg, rgba(226, 232, 240, 0.8), rgba(241, 245, 249, 0.98), rgba(226, 232, 240, 0.8));
  background-size: 200% 100%;
  animation: trade-plan-skeleton 1.3s ease-in-out infinite;
}

.trade-plan-skeleton-line-short {
  width: 28%;
}

.trade-plan-skeleton-line-long {
  width: 72%;
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

@keyframes trade-plan-skeleton {
  0% {
    background-position: 200% 0;
  }
  100% {
    background-position: -200% 0;
  }
}

@media (max-width: 900px) {
  .trade-plan-workspace__toolbar,
  .trade-plan-item__header,
  .trade-plan-item__actions {
    flex-direction: column;
  }

  .trade-plan-signal-grid {
    grid-template-columns: 1fr;
  }

  .trade-plan-item__price-grid {
    width: 100%;
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .trade-plan-price-box {
    justify-items: start;
  }

  .trade-plan-item__action-buttons {
    justify-content: flex-start;
  }
}
</style>
