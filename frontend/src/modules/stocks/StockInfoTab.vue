<script setup>
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import StockCharts from './StockCharts.vue'
import StockAgentPanels from './StockAgentPanels.vue'
import ChatWindow from '../../components/ChatWindow.vue'

const symbol = ref('')
const loading = ref(false)
const error = ref('')
const detail = ref(null)
const interval = ref(localStorage.getItem('stock_interval') || 'day')
const refreshSeconds = ref(Number(localStorage.getItem('stock_refresh_seconds') || 30))
const autoRefresh = ref(localStorage.getItem('stock_auto_refresh') === 'true')
const sources = ref([])
const selectedSource = ref(localStorage.getItem('stock_source') || '')
let refreshTimer = null
const selectedSymbol = ref('')
const searchResults = ref([])
const searchOpen = ref(false)
const searchLoading = ref(false)
const searchError = ref('')
let searchTimer = null
let isSelecting = false
const historyList = ref([])
const historyLoading = ref(false)
const historyError = ref('')
const historyRefreshSeconds = ref(Number(localStorage.getItem('stock_history_refresh_seconds') || 30))
const historyAutoRefresh = ref(localStorage.getItem('stock_history_auto_refresh') === 'true')
let historyTimer = null
const contextMenu = ref({ visible: false, x: 0, y: 0, item: null })
const sortKey = ref('id')
const sortAsc = ref(true)
const monochromeMode = ref(localStorage.getItem('stock_monochrome_mode') === 'true')
const chatRef = ref(null)
const chatSessions = ref([])
const chatSessionsLoading = ref(false)
const chatSessionsError = ref('')
const selectedChatSession = ref('')
const agentResults = ref([])
const agentLoading = ref(false)
const agentError = ref('')
const agentUpdatedAt = ref('')
const agentHistoryList = ref([])
const agentHistoryLoading = ref(false)
const agentHistoryError = ref('')
const selectedAgentHistoryId = ref('')
const newsImpact = ref(null)
const newsImpactLoading = ref(false)
const newsImpactError = ref('')

const upsertAgentResult = result => {
  const agentId = result?.agentId ?? result?.AgentId ?? ''
  if (!agentId) return
  const list = [...agentResults.value]
  const index = list.findIndex(item => (item.agentId ?? item.AgentId) === agentId)
  if (index >= 0) {
    list[index] = result
  } else {
    list.push(result)
  }
  agentResults.value = list
}

const applyHistorySymbol = item => {
  selectedSymbol.value = item.symbol || item.Symbol || ''
  symbol.value = selectedSymbol.value
  if (symbol.value.trim()) {
    fetchQuote()
  }
}


const formatDate = value => {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  return date.toLocaleString()
}

const formatImpactScore = value => {
  if (value == null || Number.isNaN(Number(value))) return ''
  const num = Number(value)
  return num > 0 ? `+${num}` : `${num}`
}

const getImpactClass = category => {
  if (category === '利好') return 'impact-positive'
  if (category === '利空') return 'impact-negative'
  return 'impact-neutral'
}

const buildStockContext = currentDetail => {
  const quote = currentDetail?.quote
  if (!quote) return ''
  const name = quote.name ?? ''
  const symbol = quote.symbol ?? ''
  const price = quote.price ?? ''
  const change = quote.change ?? ''
  const changePercent = quote.changePercent ?? ''
  const high = quote.high ?? ''
  const low = quote.low ?? ''
  const timestamp = quote.timestamp ?? ''
  return `股票：${name}（${symbol})\n价格：${price}\n涨跌：${change}（${changePercent}%）\n高：${high} 低：${low}\n时间：${formatDate(timestamp)}`
}

const chatSymbolKey = computed(() => {
  const quote = detail.value?.quote
  const raw = quote?.symbol || selectedSymbol.value || symbol.value || ''
  return String(raw || '').trim().toLowerCase()
})

const chatSessionOptions = computed(() => {
  return Array.isArray(chatSessions.value) ? chatSessions.value : []
})

const chatHistoryKey = computed(() => {
  return selectedChatSession.value || ''
})

const fetchChatSessions = async () => {
  const symbolKey = chatSymbolKey.value
  if (!symbolKey) {
    chatSessions.value = []
    selectedChatSession.value = ''
    return
  }

  chatSessionsLoading.value = true
  chatSessionsError.value = ''
  try {
    const params = new URLSearchParams({ symbol: symbolKey })
    const response = await fetch(`/api/stocks/chat/sessions?${params.toString()}`)
    if (!response.ok) {
      throw new Error('聊天历史加载失败')
    }
    const list = await response.json()
    chatSessions.value = Array.isArray(list) ? list.map(item => ({
      key: item.sessionKey ?? item.SessionKey,
      label: item.title ?? item.Title
    })) : []
    if (!chatSessions.value.length) {
      await createChatSession()
      return
    }
    if (!chatSessions.value.some(item => item.key === selectedChatSession.value)) {
      selectedChatSession.value = chatSessions.value[0]?.key || ''
    }
  } catch (err) {
    chatSessionsError.value = err.message || '聊天历史加载失败'
    chatSessions.value = []
  } finally {
    chatSessionsLoading.value = false
  }
}

const createChatSession = async () => {
  const symbolKey = chatSymbolKey.value
  if (!symbolKey) return
  const timestamp = new Date()
  const label = `${timestamp.getFullYear()}-${String(timestamp.getMonth() + 1).padStart(2, '0')}-${String(
    timestamp.getDate()
  ).padStart(2, '0')} ${String(timestamp.getHours()).padStart(2, '0')}:${String(timestamp.getMinutes()).padStart(2, '0')}`
  const response = await fetch('/api/stocks/chat/sessions', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ symbol: symbolKey, title: label })
  })
  if (!response.ok) {
    throw new Error('创建会话失败')
  }
  const session = await response.json()
  const entry = { key: session.sessionKey ?? session.SessionKey, label: session.title ?? session.Title }
  chatSessions.value = [entry, ...chatSessions.value]
  selectedChatSession.value = entry.key
}

const startNewChat = async () => {
  try {
    await createChatSession()
  } catch (err) {
    chatSessionsError.value = err.message || '创建会话失败'
    return
  }
  await nextTick()
  chatRef.value?.createNewChat()
}

const chatHistoryAdapter = {
  load: async key => {
    if (!key) return []
    const response = await fetch(`/api/stocks/chat/sessions/${encodeURIComponent(key)}/messages`)
    if (!response.ok) return []
    const list = await response.json()
    if (!Array.isArray(list)) return []
    return list.map(item => ({
      role: item.role ?? item.Role,
      content: item.content ?? item.Content,
      timestamp: item.timestamp ?? item.Timestamp
    }))
  },
  save: async (key, messages) => {
    if (!key) return
    await fetch(`/api/stocks/chat/sessions/${encodeURIComponent(key)}/messages`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ messages })
    })
  }
}

const fetchAgentHistory = async () => {
  const symbolKey = chatSymbolKey.value
  if (!symbolKey) {
    agentHistoryList.value = []
    selectedAgentHistoryId.value = ''
    return
  }
  agentHistoryLoading.value = true
  agentHistoryError.value = ''
  try {
    const params = new URLSearchParams({ symbol: symbolKey })
    const response = await fetch(`/api/stocks/agents/history?${params.toString()}`)
    if (!response.ok) {
      throw new Error('多Agent历史加载失败')
    }
    const list = await response.json()
    agentHistoryList.value = Array.isArray(list) ? list : []
  } catch (err) {
    agentHistoryError.value = err.message || '多Agent历史加载失败'
    agentHistoryList.value = []
  } finally {
    agentHistoryLoading.value = false
  }
}

const agentHistoryOptions = computed(() => {
  return agentHistoryList.value.map(item => {
    const id = item.id ?? item.Id
    const symbol = item.symbol ?? item.Symbol
    const createdAt = item.createdAt ?? item.CreatedAt
    const label = `${symbol} - ${formatDate(createdAt)}`
    return { value: id, label }
  })
})

const loadAgentHistoryDetail = async historyId => {
  if (!historyId) return
  agentHistoryLoading.value = true
  agentHistoryError.value = ''
  try {
    const response = await fetch(`/api/stocks/agents/history/${historyId}`)
    if (!response.ok) {
      throw new Error('历史详情加载失败')
    }
    const detail = await response.json()
    const result = detail.result ?? detail.Result
    agentResults.value = result?.agents ?? result?.Agents ?? []
    agentUpdatedAt.value = formatDate(detail.createdAt ?? detail.CreatedAt)
  } catch (err) {
    agentHistoryError.value = err.message || '历史详情加载失败'
  } finally {
    agentHistoryLoading.value = false
  }
}

const saveAgentHistory = async () => {
  if (!detail.value?.quote?.symbol) return
  const payload = {
    symbol: detail.value.quote.symbol,
    name: detail.value.quote.name,
    interval: interval.value,
    source: selectedSource.value || null,
    provider: 'openai',
    model: null,
    useInternet: true,
    result: {
      symbol: detail.value.quote.symbol,
      name: detail.value.quote.name,
      timestamp: new Date().toISOString(),
      agents: agentResults.value
    }
  }
  const response = await fetch('/api/stocks/agents/history', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  })
  if (!response.ok) {
    throw new Error('保存多Agent历史失败')
  }
  const saved = await response.json()
  selectedAgentHistoryId.value = saved.id ?? saved.Id ?? ''
}

const selectAgentHistory = async value => {
  selectedAgentHistoryId.value = value || ''
  if (!selectedAgentHistoryId.value) {
    return
  }
  await loadAgentHistoryDetail(selectedAgentHistoryId.value)
}

const buildChatPrompt = content => {
  const context = buildStockContext(detail.value)
  const responseRule = '回答要求：只输出自然语言或Markdown，不要JSON，不要代码块。'
  return context
    ? `你是股票助手，请基于以下股票信息回答用户问题。\n${responseRule}\n\n${context}\n\n用户问题：${content}`
    : `${responseRule}\n${content}`
}

const currentStockLabel = computed(() => {
  const quote = detail.value?.quote
  if (!quote) return '未选择股票'
  return `${quote.name ?? ''}（${quote.symbol ?? ''}）`
})

const getChangeClass = value => {
  const number = Number(value)
  if (Number.isNaN(number)) return ''
  if (number > 0) return 'text-rise'
  if (number < 0) return 'text-fall'
  return ''
}

const getPriceClass = item => {
  const change = item.changePercent ?? item.ChangePercent ?? 0
  return getChangeClass(change)
}

const getHighClass = item => {
  const price = Number(item.price ?? item.Price)
  const high = Number(item.high ?? item.High)
  if (Number.isNaN(price) || Number.isNaN(high)) return ''
  return high >= price ? 'text-rise' : 'text-fall'
}

const getLowClass = item => {
  const price = Number(item.price ?? item.Price)
  const low = Number(item.low ?? item.Low)
  if (Number.isNaN(price) || Number.isNaN(low)) return ''
  return low <= price ? 'text-fall' : 'text-rise'
}

const formatPercent = value => {
  if (value === null || value === undefined || value === '') return ''
  return `${value}%`
}

const getSortValue = (item, key) => {
  switch (key) {
    case 'id':
      return Number(item.id ?? item.Id ?? 0)
    case 'symbol':
      return String(item.symbol ?? item.Symbol ?? '')
    case 'name':
      return String(item.name ?? item.Name ?? '')
    case 'price':
      return Number(item.price ?? item.Price ?? 0)
    case 'changePercent':
      return Number(item.changePercent ?? item.ChangePercent ?? 0)
    case 'turnoverRate':
      return Number(item.turnoverRate ?? item.TurnoverRate ?? 0)
    case 'peRatio':
      return Number(item.peRatio ?? item.PeRatio ?? 0)
    case 'speed':
      return Number(item.speed ?? item.Speed ?? 0)
    case 'high':
      return Number(item.high ?? item.High ?? 0)
    case 'low':
      return Number(item.low ?? item.Low ?? 0)
    case 'updatedAt':
      return new Date(item.updatedAt ?? item.UpdatedAt ?? 0).getTime()
    default:
      return 0
  }
}

const sortedHistoryList = computed(() => {
  const list = [...historyList.value]
  const key = sortKey.value
  const direction = sortAsc.value ? 1 : -1
  return list.sort((a, b) => {
    const va = getSortValue(a, key)
    const vb = getSortValue(b, key)
    if (va === vb) return 0
    return va > vb ? direction : -direction
  })
})

const toggleSort = key => {
  if (sortKey.value === key) {
    sortAsc.value = !sortAsc.value
  } else {
    sortKey.value = key
    sortAsc.value = true
  }
}

const openContextMenu = (event, item) => {
  event.preventDefault()
  contextMenu.value = {
    visible: true,
    x: event.clientX,
    y: event.clientY,
    item
  }
}

const closeContextMenu = () => {
  contextMenu.value = { visible: false, x: 0, y: 0, item: null }
}

const deleteHistoryItem = async () => {
  const target = contextMenu.value.item
  const id = target?.id ?? target?.Id
  if (!id) {
    closeContextMenu()
    return
  }

  try {
    const response = await fetch(`/api/stocks/history/${id}`, { method: 'DELETE' })
    if (response.ok || response.status === 204) {
      historyList.value = historyList.value.filter(item => (item.id ?? item.Id) !== id)
    }
  } finally {
    closeContextMenu()
  }
}

const fetchQuote = async () => {
  const query = symbol.value.trim()
  if (!query) {
    error.value = '请输入股票代码'
    return
  }

  if (!/^(\d{6}|(sh|sz)\d{6})$/i.test(query)) {
    searchStocks(query)
    return
  }

  const targetSymbol = selectedSymbol.value || query

  loading.value = true
  error.value = ''
  // 保留已有数据，避免页面闪烁

  try {
    const params = new URLSearchParams({
      symbol: targetSymbol,
      interval: interval.value
    })
    if (selectedSource.value) {
      params.set('source', selectedSource.value)
    }

    const response = await fetch(`/api/stocks/detail?${params.toString()}`)
    if (!response.ok) {
      throw new Error('接口请求失败')
    }
    detail.value = await response.json()
  } catch (err) {
    error.value = err.message || '请求失败'
  } finally {
    loading.value = false
  }
}

const fetchNewsImpact = async () => {
  const symbolValue = detail.value?.quote?.symbol
  if (!symbolValue) {
    newsImpact.value = null
    newsImpactError.value = ''
    return
  }

  newsImpactLoading.value = true
  newsImpactError.value = ''
  try {
    const params = new URLSearchParams({ symbol: symbolValue })
    if (selectedSource.value) {
      params.set('source', selectedSource.value)
    }
    const response = await fetch(`/api/stocks/news/impact?${params.toString()}`)
    if (!response.ok) {
      throw new Error('资讯影响加载失败')
    }
    newsImpact.value = await response.json()
  } catch (err) {
    newsImpactError.value = err.message || '资讯影响加载失败'
    newsImpact.value = null
  } finally {
    newsImpactLoading.value = false
  }
}

const runAgents = async () => {
  if (!detail.value?.quote?.symbol) {
    agentError.value = '请先选择股票'
    return
  }

  agentLoading.value = true
  agentError.value = ''
  try {
    agentResults.value = []
    selectedAgentHistoryId.value = ''
    const order = ['stock_news', 'sector_news', 'financial_analysis', 'trend_analysis', 'commander']
    for (const agentId of order) {
      const payload = {
        symbol: detail.value.quote.symbol,
        agentId,
        interval: interval.value,
        count: detail.value.kLines?.length || 60,
        source: selectedSource.value || null,
        provider: 'openai',
        useInternet: true,
        dependencyResults: agentId === 'commander' ? agentResults.value : []
      }

      try {
        const response = await fetch('/api/stocks/agents/single', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload)
        })
        if (!response.ok) {
          const message = await response.text()
          throw new Error(message || `${agentId} 请求失败`)
        }
        const result = await response.json()
        upsertAgentResult(result)
      } catch (err) {
        upsertAgentResult({
          agentId,
          agentName: agentId,
          success: false,
          error: err.message || `${agentId} 请求失败`,
          data: null,
          rawContent: null
        })
      }

      agentUpdatedAt.value = new Date().toLocaleString()
    }

    try {
      await saveAgentHistory()
      await fetchAgentHistory()
    } catch (err) {
      agentHistoryError.value = err.message || '保存多Agent历史失败'
    }
  } catch (err) {
    agentError.value = err.message || '多Agent请求失败'
  } finally {
    agentLoading.value = false
  }
}

const searchStocks = async query => {
  searchLoading.value = true
  searchError.value = ''
  try {
    const params = new URLSearchParams({ q: query })
    const response = await fetch(`/api/stocks/search?${params.toString()}`)
    if (!response.ok) {
      throw new Error('搜索失败')
    }
    searchResults.value = await response.json()
    searchOpen.value = true
  } catch (err) {
    searchError.value = err.message || '搜索失败'
    searchResults.value = []
    searchOpen.value = true
  } finally {
    searchLoading.value = false
  }
}

const onSymbolInput = () => {
  if (isSelecting) {
    return
  }

  if (searchTimer) {
    clearTimeout(searchTimer)
  }

  searchResults.value = []
  searchOpen.value = false
  searchError.value = ''
  selectedSymbol.value = ''
}

const selectSearchResult = item => {
  isSelecting = true
  symbol.value = item.code || item.Code || item.symbol || item.Symbol || ''
  selectedSymbol.value = item.symbol || item.Symbol || ''
  searchOpen.value = false
  searchResults.value = []
  setTimeout(() => {
    isSelecting = false
    if (symbol.value.trim()) {
      fetchQuote()
    }
  }, 0)
}

const closeSearch = () => {
  searchOpen.value = false
}

const fetchSources = async () => {
  try {
    const response = await fetch('/api/stocks/sources')
    if (response.ok) {
      sources.value = await response.json()
    }
  } catch {
    // 忽略来源加载失败
  }
}

const fetchHistory = async () => {
  historyLoading.value = true
  historyError.value = ''
  try {
    const response = await fetch('/api/stocks/history')
    if (!response.ok) {
      throw new Error('历史记录请求失败')
    }
    historyList.value = await response.json()
  } catch (err) {
    historyError.value = err.message || '历史记录请求失败'
  } finally {
    historyLoading.value = false
  }
}

const refreshHistory = async () => {
  historyLoading.value = true
  historyError.value = ''
  try {
    const params = new URLSearchParams()
    if (selectedSource.value) {
      params.set('source', selectedSource.value)
    }
    const url = params.toString() ? `/api/stocks/history/refresh?${params.toString()}` : '/api/stocks/history/refresh'
    const response = await fetch(url, { method: 'POST' })
    if (!response.ok) {
      throw new Error('历史记录刷新失败')
    }
    historyList.value = await response.json()
  } catch (err) {
    historyError.value = err.message || '历史记录刷新失败'
  } finally {
    historyLoading.value = false
  }
}

const setupHistoryRefresh = () => {
  if (historyTimer) {
    clearInterval(historyTimer)
    historyTimer = null
  }

  if (historyAutoRefresh.value && historyRefreshSeconds.value > 0) {
    historyTimer = setInterval(() => {
      if (!historyLoading.value) {
        refreshHistory()
      }
    }, historyRefreshSeconds.value * 1000)
  }
}

const setupRefresh = () => {
  if (refreshTimer) {
    clearInterval(refreshTimer)
    refreshTimer = null
  }

  if (autoRefresh.value && refreshSeconds.value > 0) {
    refreshTimer = setInterval(() => {
      if (!loading.value && symbol.value.trim()) {
        fetchQuote()
      }
    }, refreshSeconds.value * 1000)
  }
}

watch(interval, value => {
  localStorage.setItem('stock_interval', value)
  if (symbol.value.trim()) {
    fetchQuote()
  }
})

watch(refreshSeconds, value => {
  localStorage.setItem('stock_refresh_seconds', String(value))
  setupRefresh()
})

watch(autoRefresh, value => {
  localStorage.setItem('stock_auto_refresh', String(value))
  setupRefresh()
})

watch(selectedSource, value => {
  localStorage.setItem('stock_source', value)
  if (symbol.value.trim()) {
    fetchQuote()
  }
  if (historyList.value.length) {
    refreshHistory()
  }
  if (detail.value?.quote?.symbol) {
    fetchNewsImpact()
  }
})

watch(chatSymbolKey, value => {
  if (!value) {
    chatSessions.value = []
    selectedChatSession.value = ''
    agentHistoryList.value = []
    selectedAgentHistoryId.value = ''
    return
  }
  fetchChatSessions()
  fetchAgentHistory()
})

watch(historyRefreshSeconds, value => {
  localStorage.setItem('stock_history_refresh_seconds', String(value))
  setupHistoryRefresh()
})

watch(historyAutoRefresh, value => {
  localStorage.setItem('stock_history_auto_refresh', String(value))
  setupHistoryRefresh()
})

onMounted(() => {
  fetchSources()
  setupRefresh()
  fetchHistory()
  setupHistoryRefresh()
  window.addEventListener('click', closeContextMenu)
})

onUnmounted(() => {
  if (refreshTimer) {
    clearInterval(refreshTimer)
  }
  if (historyTimer) {
    clearInterval(historyTimer)
  }
  window.removeEventListener('click', closeContextMenu)
})

watch(monochromeMode, value => {
  localStorage.setItem('stock_monochrome_mode', String(value))
})

watch(
  () => detail.value?.quote?.symbol,
  () => {
    agentResults.value = []
    agentError.value = ''
    agentUpdatedAt.value = ''
    fetchNewsImpact()
  }
)
</script>

<template>
  <section class="panel" :class="{ monochrome: monochromeMode }">
    <div class="panel-header">
      <h2>股票信息</h2>
      <button class="mode-toggle" @click="monochromeMode = !monochromeMode">
        {{ monochromeMode ? '彩色模式' : '黑白模式' }}
      </button>
    </div>
    <div class="history">
      <h3>历史查询</h3>
      <div class="field">
        <label class="muted">更新频率</label>
        <select v-model.number="historyRefreshSeconds">
          <option :value="10">10 秒</option>
          <option :value="15">15 秒</option>
          <option :value="30">30 秒</option>
          <option :value="60">60 秒</option>
        </select>
        <label class="muted">
          <input type="checkbox" v-model="historyAutoRefresh" /> 自动更新
        </label>
        <button @click="refreshHistory" :disabled="historyLoading">刷新</button>
      </div>

      <p class="muted">
        历史更新：{{ historyAutoRefresh ? `每 ${historyRefreshSeconds} 秒` : '手动刷新' }}
      </p>

      <p v-if="historyError" class="muted">{{ historyError }}</p>
      <p v-if="historyLoading && !historyList.length" class="muted">历史数据刷新中...</p>

      <table v-if="historyList.length" class="history-table">
        <thead>
          <tr>
            <th @click="toggleSort('symbol')">代码</th>
            <th @click="toggleSort('name')">名称</th>
            <th @click="toggleSort('price')">价格</th>
            <th @click="toggleSort('changePercent')">涨幅%</th>
            <th @click="toggleSort('turnoverRate')">换手率%</th>
            <th @click="toggleSort('peRatio')">市盈率</th>
            <th @click="toggleSort('speed')">涨速</th>
            <th @click="toggleSort('high')">高</th>
            <th @click="toggleSort('low')">低</th>
            <th @click="toggleSort('updatedAt')">更新时间</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="item in sortedHistoryList"
            :key="item.id || item.Id"
            @click="applyHistorySymbol(item)"
            @contextmenu="openContextMenu($event, item)"
          >
            <td>{{ item.symbol ?? item.Symbol }}</td>
            <td>{{ item.name ?? item.Name }}</td>
            <td :class="getPriceClass(item)">{{ item.price ?? item.Price }}</td>
            <td :class="getChangeClass(item.changePercent ?? item.ChangePercent)">{{ formatPercent(item.changePercent ?? item.ChangePercent) }}</td>
            <td>{{ formatPercent(item.turnoverRate ?? item.TurnoverRate) }}</td>
            <td>{{ item.peRatio ?? item.PeRatio }}</td>
            <td>{{ item.speed ?? item.Speed }}</td>
            <td :class="getHighClass(item)">{{ item.high ?? item.High }}</td>
            <td :class="getLowClass(item)">{{ item.low ?? item.Low }}</td>
            <td>{{ formatDate(item.updatedAt ?? item.UpdatedAt) }}</td>
          </tr>
        </tbody>
      </table>

      <div
        v-if="contextMenu.visible"
        class="context-menu"
        :style="{ left: `${contextMenu.x}px`, top: `${contextMenu.y}px` }"
      >
        <button @click="deleteHistoryItem">删除</button>
      </div>

      <p v-if="!historyLoading && !historyError && !historyList.length" class="muted">暂无历史数据。</p>
    </div>

    <div class="field search-field">
      <input
        v-model="symbol"
        placeholder="输入股票代码/名称/拼音缩写"
        @input="onSymbolInput"
      />
      <button @click="fetchQuote" :disabled="loading">查询</button>
    </div>

    <div v-if="searchOpen" class="search-modal" @click.self="closeSearch">
      <div class="search-modal-content">
        <div class="search-modal-header">
          <span>搜索结果</span>
          <button class="close-btn" @click="closeSearch">关闭</button>
        </div>
        <p v-if="searchError" class="muted">{{ searchError }}</p>
        <p v-else-if="searchLoading" class="muted">搜索中...</p>
        <ul v-else class="search-list">
          <li
            v-for="item in searchResults"
            :key="item.symbol || item.Symbol"
            @click="selectSearchResult(item)"
          >
            <div class="result-name">{{ item.name ?? item.Name }}</div>
            <div class="result-code">{{ item.symbol ?? item.Symbol }}</div>
          </li>
        </ul>
        <p v-if="!searchLoading && !searchError && !searchResults.length" class="muted">暂无匹配结果</p>
      </div>
    </div>

    <div class="field">
      <label class="muted">数据来源</label>
      <select v-model="selectedSource">
        <option value="">自动</option>
        <option v-for="item in sources" :key="item" :value="item">{{ item }}</option>
      </select>
    </div>

    <div class="field">
      <label class="muted">刷新(秒)</label>
      <input type="number" min="5" v-model.number="refreshSeconds" />
      <label class="muted">
        <input type="checkbox" v-model="autoRefresh" /> 自动刷新
      </label>
    </div>

    <p class="muted">
      数据刷新：{{ autoRefresh ? `每 ${refreshSeconds} 秒` : '手动刷新' }}
    </p>

    <p v-if="error" class="muted">{{ error }}</p>
    <p v-else-if="loading && !detail" class="muted">查询中...</p>

    <div v-if="detail">
      <p><strong>{{ detail.quote.name }}</strong>（{{ detail.quote.symbol }}）</p>
      <p>当前价：{{ detail.quote.price }}</p>
      <p>涨跌：{{ detail.quote.change }}（{{ detail.quote.changePercent }}%）</p>
      <p class="muted">盘中消息：{{ detail.messages.length }} 条</p>

      <ul v-if="detail.messages.length" class="messages">
        <li v-for="item in detail.messages.slice(0, 5)" :key="item.title">
          {{ item.title }} - {{ item.source }}
        </li>
      </ul>

      <section class="news-impact">
        <div class="news-impact-header">
          <div>
            <h3>资讯影响</h3>
            <p class="muted">对近期资讯做情绪与影响评估。</p>
          </div>
          <button @click="fetchNewsImpact" :disabled="newsImpactLoading">刷新</button>
        </div>

        <p v-if="newsImpactError" class="muted error">{{ newsImpactError }}</p>
        <p v-else-if="newsImpactLoading" class="muted">分析中...</p>

        <div v-else-if="newsImpact" class="news-impact-content">
          <div class="news-impact-summary">
            <span>利好 {{ newsImpact.summary.positive }}</span>
            <span>中性 {{ newsImpact.summary.neutral }}</span>
            <span>利空 {{ newsImpact.summary.negative }}</span>
            <span class="overall">总体：{{ newsImpact.summary.overall }}</span>
          </div>

          <ul v-if="newsImpact.events?.length" class="news-impact-list">
            <li v-for="item in newsImpact.events.slice(0, 6)" :key="item.title">
              <span class="impact-tag" :class="getImpactClass(item.category)">{{ item.category }}</span>
              <span class="impact-title">{{ item.title }}</span>
              <span class="impact-score">{{ formatImpactScore(item.impactScore) }}</span>
            </li>
          </ul>
          <p v-else class="muted">暂无资讯影响数据。</p>
        </div>

        <p v-else class="muted">暂无资讯影响数据。</p>
      </section>

      <StockCharts
        :k-lines="detail.kLines"
        :minute-lines="detail.minuteLines"
        :base-price="Number(detail.quote.price) - Number(detail.quote.change)"
        :interval="interval"
        @update:interval="interval = $event"
      />

      <StockAgentPanels
        :agents="agentResults"
        :loading="agentLoading"
        :error="agentError"
        :last-updated="agentUpdatedAt"
        :history-options="agentHistoryOptions"
        :selected-history-id="selectedAgentHistoryId"
        :history-loading="agentHistoryLoading"
        :history-error="agentHistoryError"
        @select-history="selectAgentHistory"
        @run="runAgents"
      />

      <ChatWindow
        ref="chatRef"
        title="股票助手"
        :build-prompt="buildChatPrompt"
        :history-key="chatHistoryKey"
        :enable-history="true"
        :history-adapter="chatHistoryAdapter"
        expandable
        expanded-storage-key="stock_chat_expanded"
        placeholder="请输入关于该股票的问题"
        empty-text="可以询问该股票的走势、风险或盘面解读。"
        max-height="320px"
        expanded-height="600px"
      >
        <template #header-extra>
          <div class="chat-session">
            <p class="muted">当前：{{ currentStockLabel }}</p>
            <select v-model="selectedChatSession" :disabled="!chatSessionOptions.length">
              <option v-for="item in chatSessionOptions" :key="item.key" :value="item.key">
                {{ item.label }}
              </option>
            </select>
            <button class="chat-session-new" @click="startNewChat" :disabled="!chatSymbolKey">
              新建对话
            </button>
          </div>
        </template>
      </ChatWindow>
    </div>

    <p v-if="!detail" class="muted">
      数据来源：腾讯 / 新浪 / 百度（后端爬虫占位）
    </p>
  </section>
</template>

<style scoped>
.panel {
  background: linear-gradient(135deg, rgba(255, 255, 255, 0.75), rgba(248, 250, 252, 0.85));
  backdrop-filter: blur(10px);
  border: 1px solid rgba(148, 163, 184, 0.2);
  box-shadow: 0 10px 30px rgba(15, 23, 42, 0.08);
  border-radius: 16px;
  padding: 1.5rem;
}

.panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1rem;
}

.panel h2 {
  margin-bottom: 0;
  color: #0f172a;
}

.mode-toggle {
  border-radius: 999px;
  border: none;
  padding: 0.45rem 0.9rem;
  background: #e2e8f0;
  color: #0f172a;
  cursor: pointer;
}

.field {
  gap: 0.75rem;
  flex-wrap: wrap;
}

.field input,
.field select,
.field button {
  border-radius: 10px;
  border: 1px solid rgba(148, 163, 184, 0.4);
  padding: 0.55rem 0.75rem;
  background: rgba(255, 255, 255, 0.9);
}

.field button {
  background: linear-gradient(135deg, #2563eb, #38bdf8);
  color: #ffffff;
  border: none;
  box-shadow: 0 6px 16px rgba(37, 99, 235, 0.25);
}

.field button:disabled {
  background: #94a3b8;
  box-shadow: none;
}

.search-field {
  position: relative;
}

.search-modal {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.35);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 50;
  padding: 1rem;
}

.search-modal-content {
  width: min(560px, 100%);
  background: rgba(255, 255, 255, 0.95);
  border-radius: 16px;
  padding: 1rem 1.25rem;
  box-shadow: 0 20px 40px rgba(15, 23, 42, 0.2);
  backdrop-filter: blur(12px);
  border: 1px solid rgba(148, 163, 184, 0.2);
}

.search-modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.75rem;
  color: #0f172a;
  font-weight: 600;
}

.close-btn {
  background: transparent;
  border: none;
  color: #64748b;
  cursor: pointer;
}

.search-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: grid;
  gap: 0.5rem;
  max-height: 320px;
  overflow: auto;
}

.search-list li {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.6rem 0.75rem;
  border-radius: 10px;
  background: rgba(248, 250, 252, 0.9);
  border: 1px solid rgba(226, 232, 240, 0.9);
  cursor: pointer;
}

.search-list li:hover {
  background: rgba(226, 232, 240, 0.8);
}

.result-name {
  color: #0f172a;
  font-weight: 500;
}

.result-code {
  color: #475569;
  font-size: 0.85rem;
}

.messages {
  list-style: none;
  padding: 0;
  margin: 0 0 1rem;
}

.messages li {
  padding: 0.25rem 0;
  color: #4b5563;
  font-size: 0.9rem;
}

.chat-session {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.chat-session select {
  border-radius: 8px;
  border: 1px solid rgba(148, 163, 184, 0.4);
  padding: 0.35rem 0.6rem;
  background: #ffffff;
}

.chat-session-new {
  border-radius: 999px;
  border: none;
  padding: 0.3rem 0.75rem;
  background: #e2e8f0;
  color: #1f2937;
  cursor: pointer;
}

.history {
  margin-bottom: 2rem;
  padding: 1rem;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.7);
  border: 1px solid rgba(148, 163, 184, 0.2);
}

.history-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9rem;
  margin-top: 0.5rem;
  background: rgba(255, 255, 255, 0.9);
  border-radius: 12px;
  overflow: hidden;
}

.history-table th,
.history-table td {
  border-bottom: 1px solid #e5e7eb;
  padding: 0.5rem;
  text-align: left;
}

.history-table th {
  background: rgba(37, 99, 235, 0.08);
  color: #1e293b;
  font-weight: 600;
  cursor: pointer;
}

.history-table tbody tr {
  cursor: pointer;
}

.history-table tbody tr:hover {
  background: #f8fafc;
}

.text-rise {
  color: #ef4444;
  font-weight: 600;
}

.text-fall {
  color: #22c55e;
  font-weight: 600;
}

.panel.monochrome {
  background: #ffffff;
  color: #000000;
  box-shadow: none;
}

.panel.monochrome .mode-toggle,
.panel.monochrome .field button,
.panel.monochrome .context-menu button {
  background: #6b7280;
  color: #ffffff;
}

.panel.monochrome .field input,
.panel.monochrome .field select,
.panel.monochrome .history,
.panel.monochrome .history-table,
.panel.monochrome .search-modal-content {
  background: #ffffff;
  color: #000000;
}

.panel.monochrome .text-rise,
.panel.monochrome .text-fall {
  color: #000000;
  font-weight: 600;
}

.context-menu {
  position: fixed;
  z-index: 60;
  background: rgba(255, 255, 255, 0.98);
  border: 1px solid rgba(148, 163, 184, 0.3);
  border-radius: 10px;
  box-shadow: 0 12px 24px rgba(15, 23, 42, 0.2);
  padding: 0.25rem;
}

.context-menu button {
  background: transparent;
  border: none;
  color: #ef4444;
  padding: 0.4rem 0.8rem;
  cursor: pointer;
}
</style>
