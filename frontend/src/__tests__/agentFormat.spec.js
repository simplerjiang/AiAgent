import { describe, it, expect } from 'vitest'
import { buildMetricRows, formatMetricLabel, formatMetricValue } from '../modules/stocks/agentFormat'

describe('agentFormat', () => {
  it('formats confidence percent', () => {
    expect(formatMetricValue(0.62, 'confidence')).toBe('62%')
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
    expect(formatMetricLabel('unknownKey')).toBe('unknownKey')
  })
})
