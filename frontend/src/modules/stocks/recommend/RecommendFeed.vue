<script setup>
import { computed, nextTick, ref, watch } from 'vue'
import { ensureMarkdown, markdownToSafeHtml, parseJsonIfPossible } from '../../../utils/jsonMarkdownService.js'

const props = defineProps({
  session: { type: Object, default: null },
  sseEvents: { type: Array, default: () => [] },
  isRunning: { type: Boolean, default: false }
})

const ROLE_META = {
  recommend_macro_analyst: { label: '宏观分析师', color: '#6366f1', icon: '🌐' },
  recommend_sector_hunter: { label: '板块猎手', color: '#8b5cf6', icon: '🔍' },
  recommend_smart_money: { label: '资金分析师', color: '#a855f7', icon: '💰' },
  recommend_sector_bull: { label: '板块多头', color: '#22c55e', icon: '🐂' },
  recommend_sector_bear: { label: '板块空头', color: '#ef4444', icon: '🐻' },
  recommend_sector_judge: { label: '板块裁决官', color: '#3b82f6', icon: '⚖️' },
  recommend_leader_picker: { label: '龙头猎手', color: '#f59e0b', icon: '👑' },
  recommend_growth_picker: { label: '潜力猎手', color: '#10b981', icon: '🌱' },
  recommend_chart_validator: { label: '技术验证师', color: '#6366f1', icon: '📊' },
  recommend_stock_bull: { label: '个股多头', color: '#22c55e', icon: '📈' },
  recommend_stock_bear: { label: '个股空头', color: '#ef4444', icon: '📉' },
  recommend_risk_reviewer: { label: '风控审查', color: '#f97316', icon: '🛡️' },
  recommend_director: { label: '推荐总监', color: '#0ea5e9', icon: '🎯' },
  recommend_router: { label: '路由器', color: '#64748b', icon: '🔀' },
  direct_answer: { label: '直接回答', color: '#0d9488', icon: '💬' }
}

const STAGE_LABELS = {
  MarketScan: '市场扫描',
  SectorDebate: '板块辩论',
  StockPicking: '选股精选',
  StockDebate: '个股辩论',
  FinalDecision: '推荐决策',
  0: '市场扫描',
  1: '板块辩论',
  2: '选股精选',
  3: '个股辩论',
  4: '推荐决策'
}

const MCP_TOOL_LABELS = {
  web_search: '网页搜索',
  sector_analysis: '板块分析',
  stock_search: '股票搜索',
  market_context: '市场背景',
  CompanyOverviewMcp: '公司概况工具',
  MarketContextMcp: '市场背景工具',
  TechnicalMcp: '技术分析工具',
  StockFundamentalsMcp: '基本面工具',
  FundamentalsMcp: '基本面工具',
  NewsMcp: '新闻工具',
  SocialMcp: '社交舆情工具',
  SocialSentimentMcp: '社交情绪工具',
  StockShareholderMcp: '股东分析工具',
  ShareholderMcp: '股东分析工具',
  StockAnnouncementMcp: '公告分析工具',
  AnnouncementMcp: '公告分析工具',
  StockProductMcp: '产品分析工具',
  StockKlineMcp: 'K线数据工具',
  StockMinuteMcp: '分时数据工具',
  StockNewsMcp: '个股新闻工具',
  StockSearchMcp: '股票搜索工具',
  StockDetailMcp: '股票详情工具',
  StockStrategyMcp: '策略分析工具',
  SectorRotationMcp: '板块轮动工具'
}

const FEED_TEXT_PATTERNS = [
  [/\bRetrying\b/gi, '重试'],
  [/\battempt\s+(\d+)/gi, '第$1次'],
  [/\bRole\s+/gi, ''],
  [/\bstarted\b/gi, '开始'],
  [/\bCompleted\b/gi, '完成'],
  [/\bDegraded\b/gi, '降级完成'],
  [/\bRunning\b/gi, '执行中'],
  [/\bfailed after retries\b/gi, '重试后失败'],
  [/\bFailed\b/gi, '失败'],
  [/\bLLM ready\b/gi, 'LLM就绪']
]

const ROLE_PREVIEW_FIELDS = [
  'summary',
  'recommendation',
  'verdict',
  'reason',
  'verdictReason',
  'buyLogic',
  'globalContext',
  'marketSentiment',
  'sentiment',
  'triggerCondition',
  'invalidCondition',
  'riskLevel',
  'volumeAssessment',
  'trendState',
  'mainRisk'
]

const ROLE_PREVIEW_LIST_FIELDS = [
  'selectedSectors',
  'candidateSectors',
  'sectorCards',
  'stockCards',
  'picks',
  'validations',
  'assessments',
  'riskNotes',
  'riskWarnings',
  'keyDrivers',
  'catalysts',
  'resonanceSectors',
  'strategySignals'
]

const ROLE_PREVIEW_ITEM_FIELDS = [
  'name',
  'sectorName',
  'symbol',
  'summary',
  'reason',
  'verdictReason',
  'buyLogic',
  'recommendation',
  'verdict',
  'riskLevel',
  'technicalScore',
  'changePercent',
  'triggerCondition',
  'invalidCondition'
]

const STRUCTURED_SUMMARY_PLACEHOLDER = '已生成结构化分析，展开查看完整结果'

const escapeRegExp = value => String(value).replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
const mcpPattern = new RegExp(Object.keys(MCP_TOOL_LABELS).join('|'), 'g')
const rolePattern = new RegExp(Object.keys(ROLE_META).map(escapeRegExp).join('|'), 'g')

const feedEnd = ref(null)
const expandedItems = ref(new Set())
const expandedBodies = ref(new Set())
const filterMode = ref('all')
const filterModes = [
  { key: 'all', label: '全部' },
  { key: 'conclusion', label: '仅结论' },
  { key: 'tool', label: '仅工具' }
]

const roleMeta = id => ROLE_META[id] || { label: id || '系统', color: '#94a3b8', icon: '🤖' }

const activeTurnId = computed(() => props.session?.activeTurnId ?? props.session?.ActiveTurnId ?? null)

const toggleExpand = key => {
  const next = new Set(expandedItems.value)
  next.has(key) ? next.delete(key) : next.add(key)
  expandedItems.value = next
}

const toggleBody = key => {
  const next = new Set(expandedBodies.value)
  next.has(key) ? next.delete(key) : next.add(key)
  expandedBodies.value = next
}

const translateMcpNames = text => {
  if (!text) return text
  let result = String(text)
    .replace(rolePattern, match => roleMeta(match).label)
    .replace(mcpPattern, match => MCP_TOOL_LABELS[match] || match)
  for (const [pattern, replacement] of FEED_TEXT_PATTERNS) {
    result = result.replace(pattern, replacement)
  }
  return result
}

const hasReadableValue = value => {
  if (value == null) return false
  if (Array.isArray(value)) return value.length > 0
  if (typeof value === 'object') return Object.keys(value).length > 0
  if (typeof value === 'string') return value.trim().length > 0
  return true
}

const summarizePreviewValue = value => {
  if (Array.isArray(value)) {
    return value.slice(0, 3).map(summarizePreviewValue)
  }

  if (!value || typeof value !== 'object') {
    return value
  }

  const preview = {}
  for (const key of ROLE_PREVIEW_ITEM_FIELDS) {
    if (hasReadableValue(value[key])) {
      preview[key] = value[key]
    }
  }

  if (Object.keys(preview).length > 0) {
    return preview
  }

  for (const [key, itemValue] of Object.entries(value)) {
    if (!hasReadableValue(itemValue)) continue
    preview[key] = Array.isArray(itemValue) ? itemValue.slice(0, 3).map(summarizePreviewValue) : itemValue
    if (Object.keys(preview).length >= 3) break
  }

  return preview
}

const buildRolePreviewData = value => {
  if (Array.isArray(value)) {
    return value.slice(0, 3).map(summarizePreviewValue)
  }

  if (!value || typeof value !== 'object') {
    return null
  }

  const preview = {}

  for (const key of ROLE_PREVIEW_FIELDS) {
    if (!hasReadableValue(value[key])) continue
    preview[key] = value[key]
    if (Object.keys(preview).length >= 3) break
  }

  for (const key of ROLE_PREVIEW_LIST_FIELDS) {
    if (!hasReadableValue(value[key])) continue
    preview[key] = Array.isArray(value[key])
      ? value[key].slice(0, 3).map(summarizePreviewValue)
      : summarizePreviewValue(value[key])
    if (Object.keys(preview).length >= 5) break
  }

  if (Object.keys(preview).length > 0) {
    return preview
  }

  for (const [key, itemValue] of Object.entries(value)) {
    if (!hasReadableValue(itemValue)) continue
    preview[key] = Array.isArray(itemValue)
      ? itemValue.slice(0, 3).map(summarizePreviewValue)
      : summarizePreviewValue(itemValue)
    if (Object.keys(preview).length >= 4) break
  }

  return Object.keys(preview).length > 0 ? preview : null
}

const shortenSummaryPrefix = (value, maxLength = 120) => {
  const compact = String(value || '').replace(/\s+/g, ' ').trim()
  if (!compact) return ''
  return compact.length > maxLength ? `${compact.slice(0, maxLength).trimEnd()}...` : compact
}

const findStructuredPayloadStart = value => {
  const text = String(value || '')
  const fenceStart = text.indexOf('```')
  if (fenceStart >= 0) return fenceStart

  for (let index = 0; index < text.length; index += 1) {
    const openingChar = text[index]
    if (openingChar !== '{' && openingChar !== '[') continue

    const remainder = text.slice(index).trim()
    if (remainder.length < 60) continue

    const newlineCount = (remainder.match(/\n/g) || []).length
    const commaCount = (remainder.match(/,/g) || []).length
    const hasKeyValuePairs = /(?:"[^"\n]+"|[A-Za-z0-9_$.-]+)\s*:/.test(remainder)

    if (openingChar === '{' && (hasKeyValuePairs || newlineCount >= 3)) return index
    if (openingChar === '[' && (hasKeyValuePairs || commaCount >= 2 || newlineCount >= 3)) return index
  }

  return -1
}

const extractFirstJsonValue = text => {
  const source = String(text || '')

  for (let start = 0; start < source.length; start += 1) {
    const openingChar = source[start]
    if (openingChar !== '{' && openingChar !== '[') continue

    let depth = 0
    let inString = false
    let isEscaped = false

    for (let index = start; index < source.length; index += 1) {
      const char = source[index]

      if (inString) {
        if (isEscaped) {
          isEscaped = false
        } else if (char === '\\') {
          isEscaped = true
        } else if (char === '"') {
          inString = false
        }
        continue
      }

      if (char === '"') {
        inString = true
        continue
      }

      if (char === '{' || char === '[') {
        depth += 1
        continue
      }

      if (char !== '}' && char !== ']') {
        continue
      }

      depth -= 1
      if (depth < 0) break
      if (depth !== 0) continue

      const candidate = source.slice(start, index + 1).trim()
      const parsed = parseJsonIfPossible(candidate)
      if (parsed && typeof parsed === 'object') {
        return { parsed, start }
      }
      break
    }
  }

  return null
}

const parseStructuredCandidate = value => {
  const parsed = parseJsonIfPossible(value)
  return parsed && typeof parsed === 'object' ? parsed : null
}

const extractStructuredSummary = value => {
  if (typeof value !== 'string') {
    const structured = parseStructuredCandidate(value)
    return { structured, prefixText: '', hasStructuredPayload: !!structured }
  }

  const text = value.trim()
  if (!text) {
    return { structured: null, prefixText: '', hasStructuredPayload: false }
  }

  const direct = parseStructuredCandidate(text)
  if (direct) {
    return { structured: direct, prefixText: '', hasStructuredPayload: true }
  }

  const fencePattern = /```(?:[a-zA-Z0-9_-]+)?\s*([\s\S]*?)```/g
  let match = fencePattern.exec(value)
  while (match) {
    const fencedBody = String(match[1] || '').trim()
    const extractedFromFence = parseStructuredCandidate(fencedBody) || extractFirstJsonValue(fencedBody)?.parsed || null
    if (extractedFromFence) {
      return {
        structured: extractedFromFence,
        prefixText: value.slice(0, match.index).trim(),
        hasStructuredPayload: true
      }
    }
    match = fencePattern.exec(value)
  }

  const extracted = extractFirstJsonValue(value)
  if (extracted) {
    return {
      structured: extracted.parsed,
      prefixText: value.slice(0, extracted.start).trim(),
      hasStructuredPayload: true
    }
  }

  const payloadStart = findStructuredPayloadStart(value)
  if (payloadStart >= 0) {
    return {
      structured: null,
      prefixText: value.slice(0, payloadStart).trim(),
      hasStructuredPayload: true
    }
  }

  return { structured: null, prefixText: '', hasStructuredPayload: false }
}

const parseStructuredContent = value => {
  return extractStructuredSummary(value).structured
}

const inferEventType = (itemType, summary) => {
  const normalizedType = String(itemType || '').toLowerCase()
  const text = String(summary || '')

  if (normalizedType === 'userfollowup') return 'TurnStarted'
  if (normalizedType === 'toolevent') return /返回完成|已完成|completed/i.test(text) ? 'ToolCompleted' : 'ToolDispatched'
  if (normalizedType === 'stagetransition') {
    if (/失败/.test(text)) return 'StageFailed'
    if (/开始/.test(text)) return 'StageStarted'
    return 'StageCompleted'
  }
  if (normalizedType === 'degradednotice') return 'DegradedNotice'
  if (normalizedType === 'errornotice') return 'TurnFailed'
  if (normalizedType === 'systemnotice') return 'SystemNotice'
  if (normalizedType === 'rolemessage') {
    if (/开始执行/.test(text)) return 'RoleStarted'
    if (/执行失败/.test(text)) return 'RoleFailed'
    if (/执行完成/.test(text)) return 'RoleCompleted'
    return 'RoleSummaryReady'
  }
  return itemType || null
}

const normalizeFeedItem = (rawItem, turn) => {
  const itemType = rawItem?.itemType ?? rawItem?.ItemType ?? rawItem?.type ?? rawItem?.Type ?? null
  const summary = rawItem?.summary ?? rawItem?.Summary ?? rawItem?.content ?? rawItem?.Content ?? ''
  return {
    id: rawItem?.id ?? rawItem?.Id ?? null,
    turnId: rawItem?.turnId ?? rawItem?.TurnId ?? turn?.id ?? null,
    turnIndex: rawItem?.turnIndex ?? rawItem?.TurnIndex ?? turn?.turnIndex ?? 0,
    itemType,
    eventType: rawItem?.eventType ?? rawItem?.EventType ?? inferEventType(itemType, summary),
    roleId: rawItem?.roleId ?? rawItem?.RoleId ?? null,
    stageType: rawItem?.stageType ?? rawItem?.StageType ?? null,
    summary,
    detailJson: rawItem?.detailJson ?? rawItem?.DetailJson ?? rawItem?.metadataJson ?? rawItem?.MetadataJson ?? null,
    traceId: rawItem?.traceId ?? rawItem?.TraceId ?? null,
    timestamp: rawItem?.timestamp ?? rawItem?.Timestamp ?? rawItem?.createdAt ?? rawItem?.CreatedAt ?? null
  }
}

const turnGroups = computed(() => {
  const rawTurns = Array.isArray(props.session?.turns) ? props.session.turns : []
  const groups = rawTurns
    .map(turn => ({
      id: turn?.id ?? turn?.Id,
      turnIndex: turn?.turnIndex ?? turn?.TurnIndex ?? 0,
      userPrompt: turn?.userPrompt ?? turn?.UserPrompt ?? '',
      requestedAt: turn?.requestedAt ?? turn?.RequestedAt ?? null,
      items: Array.isArray(turn?.feedItems ?? turn?.FeedItems)
        ? (turn.feedItems ?? turn.FeedItems).map(item => normalizeFeedItem(item, turn))
        : []
    }))
    .sort((left, right) => left.turnIndex - right.turnIndex)

  const byTurnId = new Map(groups.map(group => [group.id, {
    ...group,
    items: [...group.items].sort((left, right) => new Date(left.timestamp || 0) - new Date(right.timestamp || 0))
  }]))

  const fallbackTurn = groups[groups.length - 1] || null

  for (const rawEvent of props.sseEvents) {
    const normalized = normalizeFeedItem(rawEvent, {
      id: rawEvent?.turnId ?? rawEvent?.TurnId ?? activeTurnId.value ?? fallbackTurn?.id ?? null,
      turnIndex: fallbackTurn?.turnIndex ?? 0
    })

    if (!normalized.turnId || normalized.eventType === 'TurnSwitched') {
      continue
    }

    if (!byTurnId.has(normalized.turnId)) {
      byTurnId.set(normalized.turnId, {
        id: normalized.turnId,
        turnIndex: normalized.turnIndex,
        userPrompt: '',
        requestedAt: normalized.timestamp,
        items: []
      })
    }

    const group = byTurnId.get(normalized.turnId)
    const exists = group.items.some(item => createFeedKey(item) === createFeedKey(normalized))
    if (!exists) {
      group.items.push(normalized)
      group.items.sort((left, right) => new Date(left.timestamp || 0) - new Date(right.timestamp || 0))
    }
  }

  return [...byTurnId.values()].sort((left, right) => left.turnIndex - right.turnIndex)
})

const displayItems = computed(() => {
  const rows = []

  for (const turn of turnGroups.value) {
    if (turn.userPrompt) {
      rows.push({
        kind: 'user',
        key: `turn-${turn.id}`,
        turn
      })
    }

    turn.items.forEach((item, index) => {
      const kind = itemKind(item)
      if (kind === 'hidden') {
        return
      }

      if (filterMode.value === 'conclusion') {
        if (kind === 'tool' || kind === 'lifecycle') return
      } else if (filterMode.value === 'tool') {
        if (kind !== 'tool' && kind !== 'divider') return
      }

      rows.push({
        kind,
        key: itemKey(item, index),
        item,
        turn
      })
    })
  }

  return rows
})

const itemKind = item => {
  const eventType = String(item.eventType || '').toLowerCase()
  const itemType = String(item.itemType || '').toLowerCase()
  const summary = String(item.summary || '')

  if (eventType === 'turnstarted' || itemType === 'userfollowup' || eventType === 'turnswitched') return 'hidden'
  if (eventType === 'stagestarted' || eventType === 'stagecompleted' || eventType === 'stagefailed' || itemType === 'stagetransition') return 'divider'
  if (eventType === 'tooldispatched' || eventType === 'toolcompleted' || itemType === 'toolevent') return 'tool'
  if (eventType === 'rolesummaryready') return 'role'
  if (eventType === 'rolestarted' || eventType === 'rolecompleted') return 'lifecycle'
  if (eventType === 'rolefailed') return 'role'
  if (itemType === 'rolemessage') {
    if (/开始执行|执行完成/.test(summary)) return 'lifecycle'
    return 'role'
  }
  if (eventType === 'turncompleted' || eventType === 'turnfailed' || eventType === 'degradednotice' || eventType === 'systemnotice' || itemType === 'degradednotice' || itemType === 'systemnotice' || itemType === 'errornotice') {
    return 'system'
  }
  return 'system'
}

const createFeedKey = item => [
  item.turnId,
  item.itemType,
  item.eventType,
  item.roleId,
  item.stageType,
  item.traceId,
  item.timestamp,
  item.summary
].join('|')

const itemKey = (item, index) => item.id || `${item.turnId || 'turn'}-${item.eventType || item.itemType || 'item'}-${index}-${item.timestamp || 'ts'}`
const detailKey = (item, index) => `detail-${itemKey(item, index)}`
const bodyKey = (item, index) => `body-${itemKey(item, index)}`

const parseDetailJson = item => {
  return parseStructuredContent(item?.detailJson)
}

const formatToolDetail = item => {
  const data = parseDetailJson(item)
  if (!data || Array.isArray(data)) return null

  const sections = []
  if (data.toolName) sections.push({ label: '工具', value: MCP_TOOL_LABELS[data.toolName] || data.toolName })
  if (data.status) sections.push({ label: '状态', value: data.status === 'Completed' ? '已完成' : data.status === 'Running' ? '执行中' : data.status === 'Failed' ? '失败' : data.status })
  if (data.symbol) sections.push({ label: '标的', value: data.symbol })
  if (data.summary) sections.push({ label: '摘要', value: data.summary })
  if (data.args) sections.push({ label: '参数', value: ensureMarkdown(data.args), isLarge: true, isHtml: true })
  if (data.resultPreview) sections.push({ label: '返回数据', value: ensureMarkdown(data.resultPreview), isLarge: true, isHtml: true })

  if (sections.length === 0) {
    for (const [key, value] of Object.entries(data)) {
      sections.push({
        label: key,
        value: typeof value === 'object' ? ensureMarkdown(value) : String(value),
        isHtml: typeof value === 'object'
      })
    }
  }

  return sections
}

const formatTime = timestamp => {
  if (!timestamp) return ''
  return new Date(timestamp).toLocaleTimeString('zh-CN', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  })
}

const stageLabel = item => {
  if (item.stageType != null && STAGE_LABELS[item.stageType]) {
    return STAGE_LABELS[item.stageType]
  }

  const content = translateMcpNames(item.summary || '')
  for (const [key, label] of Object.entries(STAGE_LABELS)) {
    if (content.includes(key)) return label
  }
  return content || item.eventType || item.itemType || '阶段'
}

const stageActionLabel = item => {
  const eventType = String(item.eventType || '').toLowerCase()
  if (eventType === 'stagestarted' || /开始/.test(item.summary || '')) return '开始'
  if (eventType === 'stagefailed' || /失败/.test(item.summary || '')) return '失败'
  return '完成'
}

const turnLabel = turn => turn.turnIndex === 0 ? '初始问题' : `追问 ${turn.turnIndex}`

const getRoleContent = item => {
  const { structured, prefixText, hasStructuredPayload } = extractStructuredSummary(item.summary)
  if (!structured) {
    if (hasStructuredPayload) {
      const prefixPreview = shortenSummaryPrefix(prefixText)
      return translateMcpNames(ensureMarkdown(prefixPreview || STRUCTURED_SUMMARY_PLACEHOLDER))
    }
    return translateMcpNames(ensureMarkdown(item.summary || ''))
  }

  const preview = buildRolePreviewData(structured) || structured
  const prefixPreview = shortenSummaryPrefix(prefixText)
  const content = prefixPreview
    ? `${prefixPreview}\n\n${ensureMarkdown(preview)}`
    : ensureMarkdown(preview)

  return translateMcpNames(content)
}

const getRoleDetailContent = item => {
  const fullContent = extractStructuredSummary(item?.summary).hasStructuredPayload
    ? item.summary
    : item?.detailJson ?? item?.summary ?? ''
  return translateMcpNames(ensureMarkdown(fullContent))
}

const getSystemContent = item => translateMcpNames(item.summary || '')
const renderHtml = value => markdownToSafeHtml(value || '')
const isLongContent = value => String(value || '').length > 900
const isBodyExpanded = (item, index) => expandedBodies.value.has(bodyKey(item, index))
const hasDetail = item => item?.detailJson != null || extractStructuredSummary(item?.summary).hasStructuredPayload

const lifecycleText = item => {
  const eventType = item.eventType
  if (eventType === 'RoleStarted') return `${roleMeta(item.roleId).label} 开始分析`
  if (eventType === 'RoleCompleted') return `${roleMeta(item.roleId).label} 分析完成`
  return translateMcpNames(item.summary || '')
}

const systemIcon = item => {
  const eventType = String(item.eventType || '').toLowerCase()
  if (eventType === 'turnfailed') return '⚠️'
  if (eventType === 'degradednotice') return '🔄'
  return 'ℹ️'
}

watch(() => displayItems.value.length, () => {
  nextTick(() => {
    feedEnd.value?.scrollIntoView({ behavior: 'smooth', block: 'end' })
  })
})
</script>

<template>
  <div class="rec-feed-chat">
    <div class="feed-filter-bar">
      <button
        v-for="f in filterModes" :key="f.key"
        :class="['feed-filter-btn', { active: filterMode === f.key }]"
        @click="filterMode = f.key">
        {{ f.label }}
      </button>
    </div>
    <template v-if="displayItems.length > 0">
      <template v-for="(entry, index) in displayItems" :key="entry.key">
        <div v-if="entry.kind === 'user'" class="feed-msg feed-msg-user">
          <div class="feed-bubble-wrap feed-bubble-wrap-user">
            <div class="feed-bubble-name feed-bubble-name-user">
              {{ turnLabel(entry.turn) }}
              <span class="feed-bubble-time-inline">{{ formatTime(entry.turn.requestedAt) }}</span>
            </div>
            <div class="feed-bubble feed-bubble-user">
              <div class="feed-bubble-content" v-html="renderHtml(ensureMarkdown(entry.turn.userPrompt))" />
            </div>
          </div>
        </div>

        <div v-else-if="entry.kind === 'divider'" class="feed-divider">
          <span class="feed-divider-line" />
          <span class="feed-divider-text">{{ stageLabel(entry.item) }} {{ stageActionLabel(entry.item) }}</span>
          <span class="feed-divider-line" />
        </div>

        <div v-else-if="entry.kind === 'tool'" class="feed-tool-wrap">
          <div class="feed-tool" :class="{ 'feed-tool-expandable': !!formatToolDetail(entry.item) }" @click="formatToolDetail(entry.item) && toggleExpand(itemKey(entry.item, index))">
            <span class="feed-tool-icon">🔧</span>
            <span class="feed-tool-text">{{ getSystemContent(entry.item) }}</span>
            <span v-if="formatToolDetail(entry.item)" class="feed-tool-chevron">{{ expandedItems.has(itemKey(entry.item, index)) ? '▾' : '▸' }}</span>
            <span class="feed-tool-time">{{ formatTime(entry.item.timestamp) }}</span>
          </div>
          <div v-if="expandedItems.has(itemKey(entry.item, index)) && formatToolDetail(entry.item)" class="feed-tool-detail">
            <template v-for="(section, sectionIndex) in formatToolDetail(entry.item)" :key="sectionIndex">
              <div v-if="!section.isLarge" class="feed-tool-detail-row">
                <span class="feed-tool-detail-key">{{ section.label }}:</span>
                <span v-if="section.isHtml" class="feed-tool-detail-val" v-html="markdownToSafeHtml(section.value)" />
                <span v-else class="feed-tool-detail-val">{{ section.value }}</span>
              </div>
              <div v-else class="feed-tool-detail-large">
                <div class="feed-tool-detail-key">{{ section.label }}:</div>
                <div v-if="section.isHtml" class="feed-tool-detail-rendered" v-html="markdownToSafeHtml(section.value)" />
                <pre v-else class="feed-tool-detail-pre">{{ section.value }}</pre>
              </div>
            </template>
          </div>
        </div>

        <div v-else-if="entry.kind === 'system'" class="feed-system">
          <span class="feed-system-icon">{{ systemIcon(entry.item) }}</span>
          <span class="feed-system-text">{{ getSystemContent(entry.item) }}</span>
          <span class="feed-system-time">{{ formatTime(entry.item.timestamp) }}</span>
        </div>

        <div v-else-if="entry.kind === 'lifecycle'" class="feed-lifecycle">
          <span class="feed-lifecycle-dot">•</span>
          <span class="feed-lifecycle-text">{{ lifecycleText(entry.item) }}</span>
          <span class="feed-lifecycle-time">{{ formatTime(entry.item.timestamp) }}</span>
        </div>

        <div v-else class="feed-msg feed-msg-role">
          <div class="feed-avatar" :style="{ background: roleMeta(entry.item.roleId).color + '22' }">
            {{ roleMeta(entry.item.roleId).icon }}
          </div>
          <div class="feed-bubble-wrap">
            <div class="feed-bubble-name">
              {{ roleMeta(entry.item.roleId).label }}
              <span class="feed-bubble-time-inline">{{ formatTime(entry.item.timestamp) }}</span>
              <span v-if="entry.item.eventType === 'RoleFailed'" class="feed-badge-fail">失败</span>
            </div>
            <div class="feed-bubble feed-bubble-role" :class="{ 'feed-bubble-fail': entry.item.eventType === 'RoleFailed' }">
              <div
                :class="['feed-bubble-content', { collapsed: isLongContent(getRoleContent(entry.item)) && !isBodyExpanded(entry.item, index) }]"
                v-html="renderHtml(getRoleContent(entry.item))"
              />
              <button
                v-if="isLongContent(getRoleContent(entry.item))"
                class="feed-collapse-btn"
                @click="toggleBody(bodyKey(entry.item, index))"
              >
                {{ isBodyExpanded(entry.item, index) ? '收起' : '展开全部' }}
              </button>
              <div v-if="hasDetail(entry.item)" class="feed-detail-expand">
                <button class="feed-detail-toggle" @click="toggleExpand(detailKey(entry.item, index))">
                  {{ expandedItems.has(detailKey(entry.item, index)) ? '收起详情 ▾' : '查看详情 ▸' }}
                </button>
                <div
                  v-if="expandedItems.has(detailKey(entry.item, index))"
                  class="feed-detail-content"
                  v-html="renderHtml(getRoleDetailContent(entry.item))"
                />
              </div>
            </div>
          </div>
        </div>
      </template>
    </template>

    <div v-else class="feed-empty">
      <p>辩论过程暂无记录。启动推荐后将实时显示各角色发言。</p>
    </div>

    <div v-if="isRunning" class="feed-typing">
      <span class="feed-typing-label">团队分析中</span>
      <span class="feed-typing-dots">
        <span class="dot"></span>
        <span class="dot"></span>
        <span class="dot"></span>
      </span>
    </div>

    <div ref="feedEnd" />
  </div>
</template>

<style scoped>
.rec-feed-chat {
  padding: 10px 12px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

/* ── Stage divider ────────────────────────────── */
.feed-divider {
  display: flex; align-items: center; gap: 8px;
  margin: 8px 0 4px; font-size: 13px; color: var(--color-text-secondary);
}
.feed-divider-line { flex: 1; height: 1px; background: var(--color-border-light); }
.feed-divider-text { white-space: nowrap; font-weight: 500; }

/* ── Tool event (compact) ─────────────────────── */
.feed-tool-wrap { padding: 0; }
.feed-tool, .feed-system {
  display: flex; align-items: center; gap: 4px;
  padding: 2px 12px; font-size: 13px;
  color: var(--color-text-secondary);
}
.feed-tool-expandable { cursor: pointer; }
.feed-tool-expandable:hover { color: var(--color-text-body); }
.feed-tool-icon, .feed-system-icon { font-size: 12px; flex-shrink: 0; }
.feed-tool-text, .feed-system-text { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.feed-tool-chevron { font-size: 10px; opacity: 0.6; flex-shrink: 0; }
.feed-tool-time, .feed-system-time { font-size: 12px; flex-shrink: 0; opacity: 0.6; }
.feed-tool-detail {
  margin: 2px 24px 4px;
  padding: 4px 8px;
  background: var(--color-bg-surface-alt);
  border-left: 2px solid var(--color-accent);
  border-radius: 0 4px 4px 0;
  font-size: 12px;
  color: var(--color-text-secondary);
}
.feed-tool-detail-row { display: flex; gap: 6px; line-height: 1.5; }
.feed-tool-detail-key { color: var(--color-accent); font-weight: 500; white-space: nowrap; }
.feed-tool-detail-val { word-break: break-all; }
.feed-tool-detail-large { margin-top: 4px; }
.feed-tool-detail-pre {
  margin: 4px 0 0;
  padding: 6px 8px;
  background: var(--color-bg-surface-alt);
  border-radius: 4px;
  font-size: 11px;
  color: var(--color-text-body);
  white-space: pre-wrap;
  word-break: break-all;
  max-height: 300px;
  overflow-y: auto;
  font-family: 'Consolas', 'Monaco', monospace;
}
.feed-tool-detail-rendered {
  font-size: 12px;
  color: var(--color-text-body);
  padding: 6px 8px;
  background: var(--color-bg-surface-alt);
  border-radius: 4px;
  max-height: 300px;
  overflow-y: auto;
  line-height: 1.5;
}
.feed-tool-detail-rendered :deep(ul) { padding-left: 16px; margin: 4px 0; }
.feed-tool-detail-rendered :deep(li) { margin: 2px 0; }
.feed-tool-detail-rendered :deep(strong) { color: var(--color-accent); }

/* ── Lifecycle (dimmed compact) ───────────────── */
.feed-lifecycle {
  display: flex; align-items: center; gap: 4px;
  padding: 1px 12px; font-size: 12px;
  color: var(--color-text-secondary);
  opacity: 0.5;
}
.feed-lifecycle-dot { font-size: 10px; flex-shrink: 0; }
.feed-lifecycle-text { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.feed-lifecycle-time { font-size: 11px; flex-shrink: 0; opacity: 0.5; }

/* ── Message row ──────────────────────────────── */
.feed-msg { display: flex; gap: 8px; margin: 3px 0; align-items: flex-start; }
.feed-msg-role { flex-direction: row; }
.feed-msg-user { justify-content: flex-end; margin: 10px 0 8px; }

.feed-bubble-wrap-user {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  max-width: 78%;
}

.feed-bubble-name-user {
  color: var(--color-text-secondary);
}

/* ── Avatar ───────────────────────────────────── */
.feed-avatar {
  width: 30px; height: 30px; border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-size: 15px; flex-shrink: 0;
  background: var(--color-bg-surface-alt);
}

/* ── Bubble ───────────────────────────────────── */
.feed-bubble-wrap { flex: 1; min-width: 0; max-width: 85%; }
.feed-bubble-name {
  font-size: 13px; font-weight: 600; margin-bottom: 2px;
  color: var(--color-text-secondary);
}
.feed-bubble-time-inline { font-weight: 400; font-size: 12px; opacity: 0.6; margin-left: 6px; }

.feed-badge-fail {
  font-size: 11px; font-weight: 500; color: #fff; background: #ef4444;
  padding: 1px 6px; border-radius: 4px; margin-left: 6px;
}

.feed-bubble {
  padding: 8px 12px; border-radius: 12px;
  font-size: 15px; line-height: 1.6; word-break: break-word;
}
.feed-bubble-user {
  background: color-mix(in srgb, var(--color-accent) 16%, var(--color-bg-surface) 84%);
  color: var(--color-text-body);
  border-top-right-radius: 4px;
}
.feed-bubble-role {
  background: var(--color-bg-surface-alt);
  color: var(--color-text-body);
  border-top-left-radius: 4px;
}
.feed-bubble-fail {
  background: var(--color-market-fall-bg, #fde8e8);
  border-left: 3px solid #ef4444;
}

.feed-bubble-content { overflow: hidden; }
.feed-bubble-content.collapsed { max-height: 200px; -webkit-mask-image: linear-gradient(to bottom, #000 60%, transparent 100%); mask-image: linear-gradient(to bottom, #000 60%, transparent 100%); }
.feed-bubble-content :deep(p) { margin: 0 0 4px; }
.feed-bubble-content :deep(ul), .feed-bubble-content :deep(ol) { margin: 4px 0; padding-left: 16px; }
.feed-bubble-content :deep(li) { margin: 2px 0; }
.feed-bubble-content :deep(strong) { color: var(--color-accent); }
.feed-bubble-content :deep(code) { background: var(--color-bg-surface-alt); padding: 1px 4px; border-radius: 3px; font-size: 14px; }

.feed-collapse-btn {
  background: none; border: none; color: var(--color-accent);
  font-size: 13px; cursor: pointer; padding: 2px 0; margin-top: 4px;
}
.feed-collapse-btn:hover { text-decoration: underline; }

.feed-detail-expand { margin-top: 6px; border-top: 1px solid var(--color-border-light); padding-top: 4px; }
.feed-detail-toggle {
  background: transparent; border: none; color: var(--color-accent);
  font-size: 12px; cursor: pointer; padding: 2px 0;
  transition: opacity 0.15s;
}
.feed-detail-toggle:hover { opacity: 0.8; }
.feed-detail-content {
  margin-top: 4px; padding: 8px;
  background: var(--color-bg-surface-alt);
  border-radius: 6px;
  font-size: 12px;
  line-height: 1.6;
  max-height: 400px;
  overflow-y: auto;
  color: var(--color-text-body);
}
.feed-detail-content :deep(pre) {
  background: var(--color-bg-surface-alt);
  padding: 8px;
  border-radius: 4px;
  overflow-x: auto;
  font-size: 11px;
}

/* ── Filter bar ───────────────────────────────── */
.feed-filter-bar {
  display: flex;
  gap: 6px;
  padding: 8px 12px;
  border-bottom: 1px solid var(--color-border, #e0e0e0);
  background: var(--color-bg-elevated, #f9f9f9);
}
.feed-filter-btn {
  padding: 4px 12px;
  border: 1px solid var(--color-border, #ddd);
  border-radius: 12px;
  background: transparent;
  cursor: pointer;
  font-size: 12px;
  color: var(--color-text-secondary, #666);
  transition: all 0.2s;
}
.feed-filter-btn.active {
  background: var(--color-primary, #1677ff);
  color: #fff;
  border-color: var(--color-primary, #1677ff);
}

/* ── Typing indicator ─────────────────────────── */
.feed-typing {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 16px;
  color: var(--color-text-secondary, #888);
  font-size: 13px;
}
.feed-typing-label {
  opacity: 0.8;
}
.feed-typing-dots {
  display: flex;
  gap: 3px;
}
.feed-typing-dots .dot {
  width: 6px;
  height: 6px;
  background: var(--color-text-secondary, #aaa);
  border-radius: 50%;
  animation: feed-typing-bounce 1.4s ease-in-out infinite;
}
.feed-typing-dots .dot:nth-child(2) { animation-delay: 0.2s; }
.feed-typing-dots .dot:nth-child(3) { animation-delay: 0.4s; }

@keyframes feed-typing-bounce {
  0%, 60%, 100% { transform: translateY(0); opacity: 0.4; }
  30% { transform: translateY(-4px); opacity: 1; }
}

/* ── Empty state ──────────────────────────────── */
.feed-empty {
  text-align: center; padding: 24px 12px;
  color: var(--color-text-secondary); font-size: 14px;
}
</style>
