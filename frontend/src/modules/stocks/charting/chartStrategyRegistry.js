import { registerIndicator } from 'klinecharts'
import { CHART_VIEW_OPTIONS, KLINE_VIEW_IDS } from './chartViews'
import { CANDLE_PANE_ID, KDJ_PANE_ID, MACD_PANE_ID, RSI_PANE_ID, VOLUME_PANE_ID } from './chartPanes'

const CATEGORY_LABELS = Object.freeze({
  core: '基础图层',
  trend: '趋势策略',
  oscillator: '动量指标',
  signal: '信号标记'
})

const PRICE_LABELS = Object.freeze({
  minute: '分时',
  day: '蜡烛',
  month: '蜡烛',
  year: '蜡烛'
})

const ORB_WINDOW_MS = 30 * 60 * 1000
let customIndicatorsRegistered = false

const roundNumber = (value, precision = 4) => {
  const number = Number(value)
  if (!Number.isFinite(number)) return null
  return Number(number.toFixed(precision))
}

const uniqueSortedNumbers = values => Array.from(new Set(values.filter(Number.isFinite))).sort((left, right) => left - right)

const createIndicatorSpec = ({ aggregateKey, name, paneId, paneOptions, series, calcParams = [], isStack = false, order = 0 }) => ({
  aggregateKey,
  name,
  paneId,
  paneOptions,
  series,
  calcParams,
  isStack,
  order
})

const createPriceLineOverlay = ({ groupId, value, color, textColor = color }) => ({
  type: 'priceLine',
  groupId,
  value,
  color,
  textColor
})

const createStrategyDefinition = definition => Object.freeze(definition)

const createHelp = (description, interpretation, usage) => ({
  description,
  interpretation,
  usage
})

const resolveLabel = (definition, viewId) => {
  if (typeof definition.label === 'function') {
    return definition.label(viewId)
  }
  if (definition.label && typeof definition.label === 'object') {
    return definition.label[viewId] ?? definition.label.default ?? definition.id
  }
  return definition.label ?? definition.id
}

const getOrbRange = records => {
  if (!Array.isArray(records) || records.length < 2) {
    return null
  }

  const startTimestamp = records[0]?.timestamp
  if (!Number.isFinite(startTimestamp)) {
    return null
  }

  const windowRecords = records.filter(item => Number.isFinite(item.timestamp) && item.timestamp - startTimestamp <= ORB_WINDOW_MS)
  if (windowRecords.length < 2) {
    return null
  }

  const high = Math.max(...windowRecords.map(item => Number(item.high ?? item.close)).filter(Number.isFinite))
  const low = Math.min(...windowRecords.map(item => Number(item.low ?? item.close)).filter(Number.isFinite))
  if (!Number.isFinite(high) || !Number.isFinite(low)) {
    return null
  }

  return { high, low }
}

const CHART_STRATEGIES = Object.freeze([
  createStrategyDefinition({
    id: 'price',
    label: PRICE_LABELS,
    category: 'core',
    kind: 'core',
    accentColor: '#2563eb',
    help: createHelp(
      '展示当前主图的价格轨迹，分时视图显示分时线，K 线视图显示蜡烛。',
      '这是识别趋势方向和波动节奏的主图层，关闭后更适合单独观察副图指标。',
      '默认保持开启；只有在你想专注量能、MACD、RSI 等副图时再临时关闭。'
    ),
    supportedViews: CHART_VIEW_OPTIONS.map(view => view.id),
    defaultVisible: true,
    requires: ['price'],
    compute: () => null
  }),
  createStrategyDefinition({
    id: 'volume',
    label: '量能',
    category: 'core',
    kind: 'indicator',
    accentColor: '#64748b',
    help: createHelp(
      '显示每根 bar 对应的成交量柱，帮助判断放量、缩量和承接强弱。',
      '价格上涨但量能跟不上时，趋势延续性往往会变差；放量突破则更可信。',
      '建议与突破、回踩或 AI 价位线联动观察，不要只看价格不看量。'
    ),
    supportedViews: CHART_VIEW_OPTIONS.map(view => view.id),
    defaultVisible: true,
    requires: ['volume'],
    compute: () => ({
      indicators: [
        createIndicatorSpec({
          aggregateKey: 'VOL',
          name: 'VOL',
          paneId: VOLUME_PANE_ID,
          paneOptions: { id: VOLUME_PANE_ID, height: 96, minHeight: 72 },
          series: 'volume',
          calcParams: [5, 10, 20],
          isStack: true,
          order: 20
        })
      ]
    })
  }),
  createStrategyDefinition({
    id: 'baseLine',
    label: '昨收基线',
    category: 'core',
    kind: 'overlay',
    accentColor: '#64748b',
    help: createHelp(
      '以昨收价为基准画出水平参考线。',
      '分时价格站在昨收线上方，通常代表当日强于前一交易日；跌破则偏弱。',
      '适合和 VWAP、量能一起看盘中强弱，不建议孤立使用。'
    ),
    supportedViews: ['minute'],
    defaultVisible: true,
    requires: ['basePrice'],
    compute: ({ basePrice }) => Number.isFinite(basePrice)
      ? {
          overlays: [
            createPriceLineOverlay({
              groupId: 'minute-base-line',
              value: basePrice,
              color: '#64748b'
            })
          ]
        }
      : null
  }),
  createStrategyDefinition({
    id: 'aiLevels',
    label: 'AI 价位',
    category: 'core',
    kind: 'overlay',
    accentColor: '#f97316',
    accentSecondaryColor: '#10b981',
    help: createHelp(
      '显示 AI 推断的关键支撑位与压力位。',
      '橙色通常对应压力，绿色通常对应支撑；越接近这些价位，市场反应越值得观察。',
      '只能作为参考层，必须和真实量价、消息、趋势一起判断，不能单独当交易信号。'
    ),
    supportedViews: CHART_VIEW_OPTIONS.map(view => view.id),
    defaultVisible: true,
    requires: ['aiLevels'],
    compute: ({ aiLevels, viewId }) => {
      const overlays = []
      const resistance = roundNumber(aiLevels?.resistance)
      const support = roundNumber(aiLevels?.support)
      if (Number.isFinite(resistance)) {
        overlays.push(createPriceLineOverlay({ groupId: `${viewId}-ai-levels`, value: resistance, color: '#f97316' }))
      }
      if (Number.isFinite(support)) {
        overlays.push(createPriceLineOverlay({ groupId: `${viewId}-ai-levels`, value: support, color: '#10b981' }))
      }
      return overlays.length ? { overlays } : null
    }
  }),
  createStrategyDefinition({
    id: 'ma5',
    label: 'MA5',
    category: 'trend',
    kind: 'indicator',
    accentColor: '#f59e0b',
    help: createHelp(
      '5 日均线，代表最近 5 个交易日的平均成本。',
      '对短线节奏最敏感，拐头速度快，适合观察超短和短线趋势变化。',
      '建议和 MA10/MA20 对照使用；单独贴近价格时更容易被震荡反复打脸。'
    ),
    supportedViews: KLINE_VIEW_IDS,
    defaultVisible: true,
    requires: ['close'],
    compute: () => ({ indicators: [createIndicatorSpec({ aggregateKey: 'MA', name: 'MA', paneId: CANDLE_PANE_ID, paneOptions: { id: CANDLE_PANE_ID }, series: 'price', calcParams: [5], order: 40 })] })
  }),
  createStrategyDefinition({
    id: 'ma10',
    label: 'MA10',
    category: 'trend',
    kind: 'indicator',
    accentColor: '#8b5cf6',
    help: createHelp(
      '10 日均线，代表最近 10 个交易日的平均成本。',
      '相对 MA5 更稳，常用于确认短线趋势是否真正延续。',
      '适合和 MA5 做快慢线对照：MA5 上穿 MA10 常被视为短线转强参考。'
    ),
    supportedViews: KLINE_VIEW_IDS,
    defaultVisible: true,
    requires: ['close'],
    compute: () => ({ indicators: [createIndicatorSpec({ aggregateKey: 'MA', name: 'MA', paneId: CANDLE_PANE_ID, paneOptions: { id: CANDLE_PANE_ID }, series: 'price', calcParams: [10], order: 41 })] })
  }),
  createStrategyDefinition({
    id: 'ma20',
    label: 'MA20',
    category: 'trend',
    kind: 'indicator',
    accentColor: '#06b6d4',
    help: createHelp(
      '20 日均线，接近一个自然月的平均持仓成本。',
      '常作为趋势股的重要支撑或压力带，跌破后中短期结构会明显变弱。',
      '更适合波段视角；若只是看日内节奏，可临时关闭避免主图过密。'
    ),
    supportedViews: KLINE_VIEW_IDS,
    defaultVisible: false,
    requires: ['close'],
    compute: () => ({ indicators: [createIndicatorSpec({ aggregateKey: 'MA', name: 'MA', paneId: CANDLE_PANE_ID, paneOptions: { id: CANDLE_PANE_ID }, series: 'price', calcParams: [20], order: 42 })] })
  }),
  createStrategyDefinition({
    id: 'ma60',
    label: 'MA60',
    category: 'trend',
    kind: 'indicator',
    accentColor: '#ef4444',
    help: createHelp(
      '60 日均线，接近一个季度的平均成本。',
      '常用于判断中期强弱分界，能明显区分“回调中的强趋势”和“趋势已坏”。',
      '通常不需要一直开着；做中线趋势研判时打开价值更高。'
    ),
    supportedViews: KLINE_VIEW_IDS,
    defaultVisible: false,
    requires: ['close'],
    compute: () => ({ indicators: [createIndicatorSpec({ aggregateKey: 'MA', name: 'MA', paneId: CANDLE_PANE_ID, paneOptions: { id: CANDLE_PANE_ID }, series: 'price', calcParams: [60], order: 43 })] })
  }),
  createStrategyDefinition({
    id: 'vwap',
    label: 'VWAP',
    category: 'trend',
    kind: 'indicator',
    accentColor: '#0f766e',
    help: createHelp(
      'VWAP 是成交量加权平均价，强调真实成交重心。',
      '价格站稳 VWAP 往往说明盘中承接更强；跌回 VWAP 下方则代表强度减弱。',
      '主要用于分时图，适合和昨收线、量能、ORB 一起看盘中强弱。'
    ),
    supportedViews: ['minute'],
    defaultVisible: true,
    requires: ['close', 'volume'],
    compute: () => ({ indicators: [createIndicatorSpec({ aggregateKey: 'VWAP', name: 'VWAP', paneId: CANDLE_PANE_ID, paneOptions: { id: CANDLE_PANE_ID }, series: 'price', order: 45 })] })
  }),
  createStrategyDefinition({
    id: 'boll',
    label: 'BOLL',
    category: 'trend',
    kind: 'indicator',
    accentColor: '#14b8a6',
    help: createHelp(
      '布林带由中轨和上下轨组成，用于观察波动率扩张与收敛。',
      '开口扩大通常意味着趋势加速，通道收窄则常见于整理或变盘前。',
      '不要把触碰上轨简单当成卖点，趋势行情里价格可以沿轨运行很久。'
    ),
    supportedViews: KLINE_VIEW_IDS,
    defaultVisible: false,
    requires: ['high', 'low', 'close'],
    compute: () => ({ indicators: [createIndicatorSpec({ aggregateKey: 'BOLL', name: 'BOLL', paneId: CANDLE_PANE_ID, paneOptions: { id: CANDLE_PANE_ID }, series: 'price', calcParams: [20, 2], order: 46 })] })
  }),
  createStrategyDefinition({
    id: 'donchian',
    label: 'Donchian',
    category: 'trend',
    kind: 'indicator',
    accentColor: '#22c55e',
    help: createHelp(
      'Donchian 通道用最近一段时间的最高价和最低价构造突破区间。',
      '价格突破上轨更容易被视为趋势延续，跌破下轨则代表弱化。',
      '适合配合量能与突破信号一起看，单独使用容易被假突破欺骗。'
    ),
    supportedViews: KLINE_VIEW_IDS,
    defaultVisible: false,
    requires: ['high', 'low'],
    compute: () => ({ indicators: [createIndicatorSpec({ aggregateKey: 'DONCHIAN', name: 'DONCHIAN', paneId: CANDLE_PANE_ID, paneOptions: { id: CANDLE_PANE_ID }, series: 'price', calcParams: [20], order: 47 })] })
  }),
  createStrategyDefinition({
    id: 'macd',
    label: 'MACD',
    category: 'oscillator',
    kind: 'indicator',
    accentColor: '#ec4899',
    help: createHelp(
      'MACD 用快慢均线差和柱体变化衡量趋势动能。',
      'DIFF、DEA 与柱体同向扩张时，趋势延续性通常更高；背离则提示动能衰减。',
      '适合和主图趋势一起用，少在纯震荡区间内把每次金叉都当成买点。'
    ),
    supportedViews: KLINE_VIEW_IDS,
    defaultVisible: false,
    requires: ['close'],
    compute: () => ({ indicators: [createIndicatorSpec({ aggregateKey: 'MACD', name: 'MACD', paneId: MACD_PANE_ID, paneOptions: { id: MACD_PANE_ID, height: 88, minHeight: 64 }, calcParams: [12, 26, 9], isStack: true, order: 60 })] })
  }),
  createStrategyDefinition({
    id: 'rsi',
    label: 'RSI',
    category: 'oscillator',
    kind: 'indicator',
    accentColor: '#f97316',
    help: createHelp(
      'RSI 衡量一段时间内上涨与下跌力度的相对强弱。',
      '高位持续强势并不一定意味着马上见顶，真正要警惕的是高位背离。',
      '更适合看强弱变化和背离，不建议把 70/30 当成机械化买卖线。'
    ),
    supportedViews: KLINE_VIEW_IDS,
    defaultVisible: false,
    requires: ['close'],
    compute: () => ({ indicators: [createIndicatorSpec({ aggregateKey: 'RSI', name: 'RSI', paneId: RSI_PANE_ID, paneOptions: { id: RSI_PANE_ID, height: 88, minHeight: 64 }, calcParams: [6, 12, 24], isStack: true, order: 61 })] })
  }),
  createStrategyDefinition({
    id: 'kdj',
    label: 'KDJ',
    category: 'oscillator',
    kind: 'indicator',
    accentColor: '#6366f1',
    help: createHelp(
      'KDJ 通过随机指标观察短线超买超卖与拐点。',
      '对短线节奏反应很快，但噪音也更大，趋势强时容易连续钝化。',
      '建议只把它当成辅助节奏工具，最好配合主趋势和量能一起判断。'
    ),
    supportedViews: ['day'],
    defaultVisible: false,
    requires: ['high', 'low', 'close'],
    compute: () => ({ indicators: [createIndicatorSpec({ aggregateKey: 'KDJ', name: 'KDJ', paneId: KDJ_PANE_ID, paneOptions: { id: KDJ_PANE_ID, height: 88, minHeight: 64 }, calcParams: [9, 3, 3], isStack: true, order: 62 })] })
  }),
  createStrategyDefinition({
    id: 'orb',
    label: 'ORB',
    category: 'signal',
    kind: 'overlay',
    accentColor: '#0f766e',
    accentSecondaryColor: '#dc2626',
    help: createHelp(
      'ORB 是开盘区间突破，通常取开盘后一段时间的高低点作为关键边界。',
      '上破高点偏强，下破低点偏弱；如果很快回到区间内，往往是假突破预警。',
      '更适合分时图观察，必须配合量能与 VWAP，不能只看价格一瞬间刺穿。'
    ),
    supportedViews: ['minute'],
    defaultVisible: false,
    requires: ['high', 'low'],
    compute: ({ records }) => {
      const range = getOrbRange(records)
      if (!range) {
        return null
      }
      return {
        overlays: [
          createPriceLineOverlay({ groupId: 'minute-orb-range', value: range.high, color: '#0f766e' }),
          createPriceLineOverlay({ groupId: 'minute-orb-range', value: range.low, color: '#dc2626' })
        ],
        signals: [
          { id: 'orb-high', label: `ORB 高点 ${range.high.toFixed(2)}` },
          { id: 'orb-low', label: `ORB 低点 ${range.low.toFixed(2)}` }
        ]
      }
    }
  })
])

const INDICATOR_FILTERS_BY_VIEW = Object.freeze({
  minute: [
    { paneId: VOLUME_PANE_ID, name: 'VOL' },
    { paneId: CANDLE_PANE_ID, name: 'VWAP' }
  ],
  day: [
    { paneId: CANDLE_PANE_ID, name: 'MA' },
    { paneId: CANDLE_PANE_ID, name: 'BOLL' },
    { paneId: CANDLE_PANE_ID, name: 'DONCHIAN' },
    { paneId: VOLUME_PANE_ID, name: 'VOL' },
    { paneId: MACD_PANE_ID, name: 'MACD' },
    { paneId: RSI_PANE_ID, name: 'RSI' },
    { paneId: KDJ_PANE_ID, name: 'KDJ' }
  ],
  month: [
    { paneId: CANDLE_PANE_ID, name: 'MA' },
    { paneId: CANDLE_PANE_ID, name: 'BOLL' },
    { paneId: CANDLE_PANE_ID, name: 'DONCHIAN' },
    { paneId: VOLUME_PANE_ID, name: 'VOL' },
    { paneId: MACD_PANE_ID, name: 'MACD' },
    { paneId: RSI_PANE_ID, name: 'RSI' }
  ],
  year: [
    { paneId: CANDLE_PANE_ID, name: 'MA' },
    { paneId: CANDLE_PANE_ID, name: 'BOLL' },
    { paneId: CANDLE_PANE_ID, name: 'DONCHIAN' },
    { paneId: VOLUME_PANE_ID, name: 'VOL' },
    { paneId: MACD_PANE_ID, name: 'MACD' },
    { paneId: RSI_PANE_ID, name: 'RSI' }
  ]
})

const OVERLAY_GROUPS_BY_VIEW = Object.freeze({
  minute: ['minute-ai-levels', 'minute-base-line', 'minute-orb-range', 'minute-markers'],
  day: ['day-ai-levels', 'day-markers'],
  month: ['month-ai-levels', 'month-markers'],
  year: ['year-ai-levels', 'year-markers']
})

function registerVwapIndicator() {
  registerIndicator({
    name: 'VWAP',
    shortName: 'VWAP',
    series: 'price',
    precision: 2,
    shouldOhlc: true,
    calcParams: [],
    figures: [{ key: 'vwap', title: 'VWAP: ', type: 'line' }],
    calc: dataList => {
      let cumulativeVolume = 0
      let cumulativeTurnover = 0
      return dataList.map(item => {
        const close = Number(item?.close)
        const volume = Math.max(0, Number(item?.volume ?? 0))
        if (!Number.isFinite(close)) {
          return {}
        }
        cumulativeVolume += volume
        cumulativeTurnover += close * volume
        return cumulativeVolume > 0 ? { vwap: cumulativeTurnover / cumulativeVolume } : {}
      })
    }
  })
}

function registerDonchianIndicator() {
  registerIndicator({
    name: 'DONCHIAN',
    shortName: 'DON',
    series: 'price',
    precision: 2,
    shouldOhlc: true,
    calcParams: [20],
    figures: [
      { key: 'upper', title: 'UP: ', type: 'line' },
      { key: 'middle', title: 'MID: ', type: 'line' },
      { key: 'lower', title: 'DN: ', type: 'line' }
    ],
    calc: (dataList, indicator) => {
      const period = Number(indicator?.calcParams?.[0] ?? 20)
      return dataList.map((item, index) => {
        if (index < period - 1) {
          return {}
        }
        const window = dataList.slice(index - period + 1, index + 1)
        const highs = window.map(point => Number(point?.high)).filter(Number.isFinite)
        const lows = window.map(point => Number(point?.low)).filter(Number.isFinite)
        if (!highs.length || !lows.length) {
          return {}
        }
        const upper = Math.max(...highs)
        const lower = Math.min(...lows)
        return {
          upper,
          middle: (upper + lower) / 2,
          lower
        }
      })
    }
  })
}

export function ensureChartStrategiesRegistered() {
  if (customIndicatorsRegistered || typeof registerIndicator !== 'function') {
    return
  }
  registerVwapIndicator()
  registerDonchianIndicator()
  customIndicatorsRegistered = true
}

export function getChartStrategiesForView(viewId) {
  return CHART_STRATEGIES
    .filter(item => item.supportedViews.includes(viewId))
    .map(item => ({ ...item, resolvedLabel: resolveLabel(item, viewId) }))
}

export function createStrategyVisibilityState() {
  return Object.fromEntries(
    CHART_VIEW_OPTIONS.map(view => [
      view.id,
      Object.fromEntries(
        getChartStrategiesForView(view.id).map(item => [item.id, item.defaultVisible !== false])
      )
    ])
  )
}

export function getStrategyGroupsForView(viewId, visibilityState = {}) {
  const strategies = getChartStrategiesForView(viewId)
  return Object.entries(CATEGORY_LABELS)
    .map(([categoryId, categoryLabel]) => ({
      id: categoryId,
      label: categoryLabel,
      items: strategies
        .filter(item => item.category === categoryId)
        .map(item => ({
          id: item.id,
          label: item.resolvedLabel,
          active: visibilityState[item.id] !== false,
          kind: item.kind,
          accentColor: item.accentColor ?? '#2563eb',
          accentSecondaryColor: item.accentSecondaryColor ?? null,
          description: item.help?.description ?? '',
          interpretation: item.help?.interpretation ?? '',
          usage: item.help?.usage ?? ''
        }))
    }))
    .filter(group => group.items.length > 0)
}

export function getActiveStrategyBadgesForView(viewId, visibilityState = {}) {
  return getChartStrategiesForView(viewId)
    .filter(item => visibilityState[item.id] !== false)
    .map(item => ({
      id: item.id,
      label: item.resolvedLabel,
      accentColor: item.accentColor ?? '#2563eb',
      accentSecondaryColor: item.accentSecondaryColor ?? null,
      description: item.help?.description ?? '',
      interpretation: item.help?.interpretation ?? '',
      usage: item.help?.usage ?? ''
    }))
}

export function getIndicatorFiltersForView(viewId) {
  return INDICATOR_FILTERS_BY_VIEW[viewId] ?? []
}

export function getOverlayGroupIdsForView(viewId) {
  return OVERLAY_GROUPS_BY_VIEW[viewId] ?? []
}

export function buildStrategyRenderPlan({ viewId, records, visibility = {}, aiLevels, basePrice }) {
  const renderPlan = {
    indicators: [],
    overlays: [],
    markers: [],
    signals: []
  }

  getChartStrategiesForView(viewId)
    .filter(item => visibility[item.id] !== false)
    .forEach(item => {
      const result = item.compute?.({ viewId, records, aiLevels, basePrice, visibility })
      if (!result) {
        return
      }
      if (Array.isArray(result.indicators)) {
        renderPlan.indicators.push(...result.indicators)
      }
      if (Array.isArray(result.overlays)) {
        renderPlan.overlays.push(...result.overlays)
      }
      if (Array.isArray(result.markers)) {
        renderPlan.markers.push(...result.markers)
      }
      if (Array.isArray(result.signals)) {
        renderPlan.signals.push(...result.signals)
      }
    })

  const aggregatedIndicators = new Map()
  renderPlan.indicators.forEach(item => {
    const key = item.aggregateKey ?? `${item.name}:${item.paneId}`
    if (!aggregatedIndicators.has(key)) {
      aggregatedIndicators.set(key, { ...item, calcParams: [...(item.calcParams ?? [])] })
      return
    }
    const current = aggregatedIndicators.get(key)
    current.calcParams = uniqueSortedNumbers([...(current.calcParams ?? []), ...(item.calcParams ?? [])])
  })

  renderPlan.indicators = Array.from(aggregatedIndicators.values())
    .map(item => ({ ...item, calcParams: uniqueSortedNumbers(item.calcParams ?? []) }))
    .sort((left, right) => (left.order ?? 0) - (right.order ?? 0))

  return renderPlan
}
