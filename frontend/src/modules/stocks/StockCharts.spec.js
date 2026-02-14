import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import StockCharts from './StockCharts.vue'

const setOptionMock = vi.fn()

vi.mock('echarts', () => {
  return {
    init: vi.fn(() => ({
      setOption: setOptionMock,
      resize: vi.fn(),
      dispose: vi.fn(),
      convertFromPixel: vi.fn()
    }))
  }
})

describe('StockCharts', () => {
  beforeEach(() => {
    setOptionMock.mockClear()
  })

  it('parses minute lines and renders line series', async () => {
    Object.defineProperty(HTMLElement.prototype, 'getBoundingClientRect', {
      configurable: true,
      value: () => ({
        width: 600,
        height: 300,
        top: 0,
        left: 0,
        right: 600,
        bottom: 300
      })
    })

    const minuteLines = [
      { date: '2026-01-29', time: '09:31:00', price: 31.2 },
      { date: '2026-01-29', time: '09:30:00', price: 31.1 }
    ]

    mount(StockCharts, {
      props: {
        kLines: [],
        minuteLines,
        interval: 'day'
      }
    })

    await nextTick()

    expect(setOptionMock).toHaveBeenCalled()
    const calls = setOptionMock.mock.calls
    const minuteCall = calls.find(call => {
      const option = call[0]
      return option?.series?.[0]?.type === 'line'
    })
    expect(minuteCall).toBeTruthy()
    const option = minuteCall[0]
    expect(option.series[0].data.length).toBe(2)
    expect(option.series[0].data[0]).toBe(31.1)
    const tooltipText = option.tooltip.formatter([{ axisValue: '09:30', value: 31.1 }])
    expect(tooltipText).toContain('涨跌幅')
  })

  it('sorts kline data and includes percent change', async () => {
    Object.defineProperty(HTMLElement.prototype, 'getBoundingClientRect', {
      configurable: true,
      value: () => ({
        width: 600,
        height: 300,
        top: 0,
        left: 0,
        right: 600,
        bottom: 300
      })
    })

    const kLines = [
      { date: '2026-01-02', open: 10, close: 11, low: 9, high: 12 },
      { date: '2026-01-01', open: 8, close: 9, low: 7, high: 10 }
    ]

    mount(StockCharts, {
      props: {
        kLines,
        minuteLines: [],
        interval: 'day'
      }
    })

    await nextTick()

    const calls = setOptionMock.mock.calls
    const klineCall = calls.find(call => {
      const option = call[0]
      return option?.series?.[0]?.type === 'candlestick'
    })
    expect(klineCall).toBeTruthy()
    const option = klineCall[0]
    expect(option.xAxis.data[0]).toBe('2026-01-01')
    const tooltipText = option.tooltip.formatter({ name: '2026-01-01', value: [8, 9, 7, 10] })
    expect(tooltipText).toContain('涨跌幅')
  })
})
