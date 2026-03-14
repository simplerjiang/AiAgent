export const CHART_VIEW_OPTIONS = [
  {
    id: 'minute',
    label: '分时图',
    legend: [
      { id: 'price', label: '分时' },
      { id: 'volume', label: '量能' },
      { id: 'baseLine', label: '昨收基线' },
      { id: 'aiLevels', label: 'AI 价位' }
    ]
  },
  {
    id: 'day',
    label: '日K图',
    legend: [
      { id: 'price', label: '蜡烛' },
      { id: 'volume', label: '量能' },
      { id: 'ma5', label: 'MA5' },
      { id: 'ma10', label: 'MA10' },
      { id: 'aiLevels', label: 'AI 价位' }
    ]
  },
  {
    id: 'month',
    label: '月K图',
    legend: [
      { id: 'price', label: '蜡烛' },
      { id: 'volume', label: '量能' },
      { id: 'ma5', label: 'MA5' },
      { id: 'ma10', label: 'MA10' },
      { id: 'aiLevels', label: 'AI 价位' }
    ]
  },
  {
    id: 'year',
    label: '年K图',
    legend: [
      { id: 'price', label: '蜡烛' },
      { id: 'volume', label: '量能' },
      { id: 'ma5', label: 'MA5' },
      { id: 'ma10', label: 'MA10' },
      { id: 'aiLevels', label: 'AI 价位' }
    ]
  }
]

export const KLINE_VIEW_IDS = ['day', 'month', 'year']

export const isKlineChartView = viewId => KLINE_VIEW_IDS.includes(viewId)

export const normalizeKlineInterval = interval => (KLINE_VIEW_IDS.includes(interval) ? interval : 'day')

export const resolveInitialChartView = interval => normalizeKlineInterval(interval)
