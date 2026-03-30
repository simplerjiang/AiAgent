<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import StockInfoTab from './modules/stocks/StockInfoTab.vue'
import NewsArchiveTab from './modules/stocks/NewsArchiveTab.vue'
import StockRecommendTab from './modules/stocks/StockRecommendTab.vue'
import MarketSentimentTab from './modules/market/MarketSentimentTab.vue'
import AdminLlmSettings from './modules/admin/AdminLlmSettings.vue'
import SourceGovernanceDeveloperMode from './modules/admin/SourceGovernanceDeveloperMode.vue'

const tabs = [
  { key: 'stock-info', name: '股票信息', shortName: '股票', component: StockInfoTab },
  { key: 'market-sentiment', name: '情绪轮动', shortName: '情绪', component: MarketSentimentTab },
  { key: 'news-archive', name: '全量资讯库', shortName: '资讯', component: NewsArchiveTab },
  { key: 'stock-recommend', name: '股票推荐', shortName: '推荐', component: StockRecommendTab },
  { key: 'admin-llm', name: 'LLM 设置', shortName: 'LLM', component: AdminLlmSettings },
  { key: 'source-governance-dev', name: '治理开发者模式', shortName: '治理', component: SourceGovernanceDeveloperMode }
]

/* ── 时钟 ── */
const clockText = ref('')
let clockTimer = null
const updateClock = () => {
  const now = new Date()
  clockText.value = [now.getHours(), now.getMinutes(), now.getSeconds()]
    .map(n => String(n).padStart(2, '0')).join(':')
}

/* ── Tab 指示线 ── */
const tabNavRef = ref(null)
const indicatorStyle = ref({ left: '0px', width: '0px' })
const updateIndicator = () => {
  if (!tabNavRef.value) return
  const activeEl = tabNavRef.value.querySelector('.nav-tab.active')
  if (activeEl) {
    const navRect = tabNavRef.value.getBoundingClientRect()
    const tabRect = activeEl.getBoundingClientRect()
    indicatorStyle.value = {
      left: `${tabRect.left - navRect.left}px`,
      width: `${tabRect.width}px`
    }
  }
}

const defaultTabKey = tabs[0].key
const validTabKeys = new Set(tabs.map(tab => tab.key))
const initialSearchParams = new URLSearchParams(window.location.search)
const initialRequestedTab = initialSearchParams.get('tab')
const initialHasExplicitTab = validTabKeys.has(initialRequestedTab)
const initialForcedOnboarding = initialSearchParams.get('onboarding') === '1'

const onboardingStatus = ref({
  loading: true,
  requiresOnboarding: false,
  activeProviderKey: 'default',
  recommendedTabKey: 'admin-llm'
})
const appVersion = ref('')

const getTabFromLocation = () => {
  const tab = new URLSearchParams(window.location.search).get('tab')
  return validTabKeys.has(tab) ? tab : defaultTabKey
}

const activeTab = ref(getTabFromLocation())

const activeComponent = computed(() => tabs.find(tab => tab.key === activeTab.value)?.component)

const setActiveTab = tabKey => {
  if (validTabKeys.has(tabKey)) {
    activeTab.value = tabKey
  }
}

const syncLocation = () => {
  const nextUrl = new URL(window.location.href)
  nextUrl.searchParams.set('tab', activeTab.value)
  if (onboardingStatus.value.requiresOnboarding) {
    nextUrl.searchParams.set('onboarding', '1')
  } else {
    nextUrl.searchParams.delete('onboarding')
  }

  const nextLocation = `${nextUrl.pathname}${nextUrl.search}${nextUrl.hash}`
  window.history.replaceState({}, '', nextLocation)
}

watch(activeTab, syncLocation, { immediate: true })

watch(activeTab, () => nextTick(updateIndicator))

const openOnboardingTab = () => {
  setActiveTab(onboardingStatus.value.recommendedTabKey || 'admin-llm')
}

const loadOnboardingStatus = async ({ allowAutoRedirect = false } = {}) => {
  try {
    const response = await fetch('/api/llm/onboarding-status')
    if (!response.ok) {
      onboardingStatus.value.loading = false
      return
    }

    const data = await response.json()
    onboardingStatus.value = {
      loading: false,
      requiresOnboarding: Boolean(data.requiresOnboarding),
      activeProviderKey: data.activeProviderKey || 'default',
      recommendedTabKey: data.recommendedTabKey || 'admin-llm'
    }

    if (allowAutoRedirect && onboardingStatus.value.requiresOnboarding && (!initialHasExplicitTab || initialForcedOnboarding)) {
      openOnboardingTab()
      return
    }
  } catch {
    onboardingStatus.value.loading = false
    return
  }

  syncLocation()
}

onMounted(async () => {
  updateClock()
  clockTimer = setInterval(updateClock, 1000)

  try {
    const versionResponse = await fetch('/api/app/version')
    if (versionResponse.ok) {
      const versionData = await versionResponse.json()
      appVersion.value = versionData.version || ''
    }
  } catch {
    appVersion.value = ''
  }

  await loadOnboardingStatus({ allowAutoRedirect: true })
  nextTick(updateIndicator)
})

onBeforeUnmount(() => {
  if (clockTimer) clearInterval(clockTimer)
})
</script>

<template>
  <div class="app">
    <header class="app-header">
      <div class="brand">
        <svg class="brand-icon" width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M8 1L14.5 8L8 15L1.5 8L8 1Z" fill="currentColor"/>
        </svg>
        <span class="brand-text">SimplerJiang AI Agent</span>
        <span v-if="appVersion" class="version-badge">v{{ appVersion }}</span>
      </div>
      <nav ref="tabNavRef" class="nav-tabs">
        <button
          v-for="tab in tabs"
          :key="tab.key"
          class="nav-tab"
          :class="{ active: tab.key === activeTab }"
          @click="activeTab = tab.key"
        >
          <span class="nav-tab-full">{{ tab.name }}</span>
          <span class="nav-tab-short">{{ tab.shortName }}</span>
        </button>
        <div class="nav-indicator" :style="indicatorStyle" />
      </nav>
      <div class="header-status">
        <span class="header-clock">{{ clockText }}</span>
      </div>
    </header>

    <main class="app-content">
      <section v-if="onboardingStatus.requiresOnboarding" class="onboarding-banner">
        <div class="onboarding-body">
          <span class="onboarding-icon">⚠</span>
          <div>
            <strong>首次启动还未配置 LLM Key</strong>
            <p>先进入 LLM 设置页保存可用通道的 API Key。安装包不内置用户密钥。</p>
          </div>
        </div>
        <button class="btn btn-sm btn-warning btn-pill" @click="openOnboardingTab">去配置</button>
      </section>

      <component
        :is="activeComponent"
        @settings-saved="loadOnboardingStatus()"
      />
    </main>
  </div>
</template>

<style scoped>
/* ── 顶栏 ── */
.app-header {
  display: flex;
  align-items: center;
  height: 52px;
  padding: 0 var(--space-6);
  background: var(--color-bg-header);
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  position: sticky;
  top: 0;
  z-index: var(--z-sticky);
}

/* ── 品牌区 ── */
.brand {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  flex-shrink: 0;
  margin-right: var(--space-6);
}
.brand-icon {
  color: var(--color-accent);
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}
.brand-text {
  font-size: var(--text-lg);
  font-weight: 600;
  color: var(--color-text-on-dark);
  letter-spacing: 0.01em;
  white-space: nowrap;
}
.version-badge {
  display: inline-flex;
  align-items: center;
  padding: 2px 8px;
  border-radius: var(--radius-full);
  background: rgba(255, 255, 255, 0.08);
  color: var(--color-text-on-dark-muted);
  font-size: var(--text-xs);
  font-family: var(--font-family-mono);
  letter-spacing: 0.04em;
}

/* ── 导航 Tab ── */
.nav-tabs {
  display: flex;
  align-items: stretch;
  position: relative;
  overflow-x: auto;
  scrollbar-width: none;
  -webkit-overflow-scrolling: touch;
  gap: var(--space-0-5);
  flex: 1;
  min-width: 0;
}
.nav-tabs::-webkit-scrollbar { display: none; }

.nav-tab {
  display: inline-flex;
  align-items: center;
  height: 52px;
  padding: 0 var(--space-4);
  background: transparent;
  border: none;
  border-radius: 0;
  color: var(--color-text-on-dark-muted);
  font-size: var(--text-base);
  font-weight: 500;
  cursor: pointer;
  white-space: nowrap;
  transition: color var(--transition-fast), background var(--transition-fast);
  position: relative;
}
.nav-tab:hover {
  color: var(--color-text-on-dark);
  background: var(--color-bg-header-hover);
}
.nav-tab.active {
  color: #ffffff;
  font-weight: 600;
}
.nav-tab-short { display: none; }

/* 滑动指示线 */
.nav-indicator {
  position: absolute;
  bottom: 0;
  height: 2px;
  background: var(--color-accent);
  border-radius: 1px 1px 0 0;
  transition: left var(--transition-normal), width var(--transition-normal);
  pointer-events: none;
}

/* ── 右侧状态区 ── */
.header-status {
  flex-shrink: 0;
  margin-left: var(--space-4);
}
.header-clock {
  font-size: var(--text-sm);
  color: var(--color-text-on-dark-muted);
  font-family: var(--font-family-mono);
}

/* ── 内容区 ── */
.app-content {
  flex: 1;
  padding: var(--space-5);
  overflow-y: auto;
}

/* ── Onboarding Banner ── */
.onboarding-banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-4);
  margin-bottom: var(--space-4);
  padding: var(--space-4) var(--space-5);
  background: var(--color-warning-bg);
  border: 1px solid var(--color-warning-border);
  border-radius: var(--radius-lg);
}
.onboarding-body {
  display: flex;
  align-items: flex-start;
  gap: var(--space-3);
}
.onboarding-icon {
  color: var(--color-warning);
  font-size: var(--text-xl);
  flex-shrink: 0;
  margin-top: 1px;
}
.onboarding-banner strong {
  color: var(--color-text-primary);
}
.onboarding-banner p {
  margin: var(--space-1) 0 0;
  color: var(--color-text-secondary);
  font-size: var(--text-base);
}

/* ── 响应式 ── */
@media (max-width: 1200px) {
  .nav-tab-full { display: none; }
  .nav-tab-short { display: inline; }
}
@media (min-width: 1201px) {
  .nav-tab-short { display: none; }
  .nav-tab-full { display: inline; }
}
@media (max-width: 800px) {
  .brand-text { display: none; }
  .nav-tab { padding: 0 var(--space-3); }
}
</style>
