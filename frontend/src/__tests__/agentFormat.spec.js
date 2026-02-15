import { describe, it, expect } from 'vitest'
import { buildMetricRows, formatMetricLabel, formatMetricValue } from '../modules/stocks/agentFormat'

describe('agentFormat', () => {
  it('formats confidence percent', () => {
    expect(formatMetricValue(0.62, 'confidence')).toBe('62%')
    expect(formatMetricValue(62, 'confidence')).toBe('62%')
  })

  it('builds metric rows from objects', () => {
    const rows = buildMetricRows({ price: 12 }, { changePercent: 1.2 })
    expect(rows).toEqual([
      { key: 'price', value: 12 },
      { key: 'changePercent', value: 1.2 }
    ])
  })

  it('formats metric labels', () => {
    expect(formatMetricLabel('changePercent')).toBe('涨跌幅')
    expect(formatMetricLabel('confidence')).toBe('置信度')
    expect(formatMetricLabel('action')).toBe('建议动作')
    expect(formatMetricLabel('triggers')).toBe('触发条件')
    expect(formatMetricLabel('invalidations')).toBe('失效条件')
    expect(formatMetricLabel('riskLimits')).toBe('风险上限')
    expect(formatMetricLabel('unknownKey')).toBe('unknownKey')
  })
})
