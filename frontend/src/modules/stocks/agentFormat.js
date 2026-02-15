const percentKeys = ['percent', 'rate', 'ratio']
const labelMap = {
  agent: 'Agent',
  summary: '摘要',
  price: '价格',
  changePercent: '涨跌幅',
  turnoverRate: '换手率',
  innerVolume: '内盘',
  outerVolume: '外盘',
  sector: '行业板块',
  date: '日期',
  entryScore: '入场评分',
  valuationScore: '估值评分',
  confidence: '置信度',
  action: '建议动作',
  targetPrice: '目标价',
  takeProfitPrice: '止盈价',
  stopLossPrice: '止损价',
  timeHorizon: '持有周期',
  positionPercent: '建议仓位',
  rating: '评级',
  revenue: '营收',
  revenueYoY: '营收同比',
  netProfit: '净利润',
  netProfitYoY: '净利润同比',
  nonRecurringProfit: '扣非利润',
  institutionHoldingPercent: '机构持仓',
  institutionTargetPrice: '机构目标价',
  positive: '利好',
  neutral: '中性',
  negative: '利空',
  overall: '总体',
  title: '标题',
  category: '类别',
  publishedAt: '发布时间',
  source: '来源',
  impact: '影响',
  symbol: '代码',
  name: '名称',
  reason: '原因',
  label: '标签',
  timeframe: '周期',
  trend: '趋势',
  point: '证据要点',
  triggers: '触发条件',
  invalidations: '失效条件',
  riskLimits: '风险上限',
  signals: '信号',
  risks: '风险'
}

export const formatMetricValue = (value, key = '') => {
  if (value === null || value === undefined || value === '') return '-'
  const keyText = String(key).toLowerCase()
  const num = Number(value)
  if (Number.isFinite(num)) {
    if (keyText.includes('confidence')) {
      if (num >= 0 && num <= 1) {
        return `${(num * 100).toFixed(0)}%`
      }
      if (num > 1 && num <= 100) {
        return `${num.toFixed(0)}%`
      }
    }
    const isPercent = percentKeys.some(term => keyText.includes(term))
    return isPercent ? `${num}%` : num
  }
  return String(value)
}

export const buildMetricRows = (...groups) => {
  const rows = []
  groups.forEach(group => {
    if (!group || typeof group !== 'object') return
    Object.entries(group).forEach(([key, value]) => {
      rows.push({ key, value })
    })
  })
  return rows
}

export const formatMetricLabel = key => {
  if (!key) return ''
  return labelMap[key] ?? key
}
