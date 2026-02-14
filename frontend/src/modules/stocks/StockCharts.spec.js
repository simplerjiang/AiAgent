import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import StockCharts from './StockCharts.vue'

const chartMocks = vi.hoisted(() => ({
  klineSetDataMock: vi.fn(),
  volumeSetDataMock: vi.fn(),
  minuteSetDataMock: vi.fn(),
  minuteCreatePriceLineMock: vi.fn(),
  minuteRemovePriceLineMock: vi.fn(),
  subscribeCrosshairMoveMock: vi.fn(),
  applyOptionsMock: vi.fn(),
  fitContentMock: vi.fn(),
  removeMock: vi.fn()
}))

vi.mock('lightweight-charts', () => {
  let createCount = 0

  const klineChart = {
    addSeries: vi.fn(seriesType => {
      if (seriesType === 'CandlestickSeries') {
        return { setData: chartMocks.klineSetDataMock }
      }
      if (seriesType === 'HistogramSeries') {
        return { setData: chartMocks.volumeSetDataMock }
      }
      return { setData: vi.fn() }
    }),
    priceScale: vi.fn(() => ({ applyOptions: vi.fn() })),
    subscribeCrosshairMove: chartMocks.subscribeCrosshairMoveMock,
    applyOptions: chartMocks.applyOptionsMock,
    timeScale: vi.fn(() => ({ fitContent: chartMocks.fitContentMock })),
    remove: chartMocks.removeMock
  }

  const minuteChart = {
    addSeries: vi.fn(seriesType => {
      if (seriesType === 'AreaSeries') {
        return {
          setData: chartMocks.minuteSetDataMock,
          createPriceLine: chartMocks.minuteCreatePriceLineMock,
          removePriceLine: chartMocks.minuteRemovePriceLineMock
        }
      }
      return { setData: vi.fn() }
    }),
    priceScale: vi.fn(() => ({ applyOptions: vi.fn() })),
    subscribeCrosshairMove: chartMocks.subscribeCrosshairMoveMock,
    applyOptions: chartMocks.applyOptionsMock,
    timeScale: vi.fn(() => ({ fitContent: chartMocks.fitContentMock })),
    remove: chartMocks.removeMock
  }

  return {
    createChart: vi.fn(() => {
      createCount += 1
      return createCount % 2 === 1 ? klineChart : minuteChart
    }),
    ColorType: { Solid: 'solid' },
    CandlestickSeries: 'CandlestickSeries',
    HistogramSeries: 'HistogramSeries',
    AreaSeries: 'AreaSeries'
  }
})

describe('StockCharts', () => {
  beforeEach(() => {
    chartMocks.klineSetDataMock.mockClear()
    chartMocks.volumeSetDataMock.mockClear()
    chartMocks.minuteSetDataMock.mockClear()
    chartMocks.minuteCreatePriceLineMock.mockClear()
    chartMocks.minuteRemovePriceLineMock.mockClear()
    chartMocks.subscribeCrosshairMoveMock.mockClear()
    chartMocks.applyOptionsMock.mockClear()
    chartMocks.fitContentMock.mockClear()
    chartMocks.removeMock.mockClear()
    window.matchMedia = vi.fn().mockReturnValue({
      matches: false,
      media: '',
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn()
    })
  })

  it('parses minute lines and renders professional minute series', async () => {
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
    Object.defineProperty(HTMLElement.prototype, 'clientWidth', {
      configurable: true,
      get() {
        return 600
      }
    })
    Object.defineProperty(HTMLElement.prototype, 'clientHeight', {
      configurable: true,
      get() {
        return 300
      }
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

    expect(chartMocks.minuteSetDataMock).toHaveBeenCalled()
    const minuteSeriesData = chartMocks.minuteSetDataMock.mock.calls.at(-1)?.[0] ?? []
    expect(minuteSeriesData.length).toBe(2)
    expect(minuteSeriesData[0].value).toBe(31.1)
    expect(minuteSeriesData[0].time).toBeLessThan(minuteSeriesData[1].time)
    expect(chartMocks.minuteCreatePriceLineMock).toHaveBeenCalled()
  })

  it('sorts kline data and includes candlestick + volume data', async () => {
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
    Object.defineProperty(HTMLElement.prototype, 'clientWidth', {
      configurable: true,
      get() {
        return 600
      }
    })
    Object.defineProperty(HTMLElement.prototype, 'clientHeight', {
      configurable: true,
      get() {
        return 300
      }
    })

    const kLines = [
      { date: '2026-01-02', open: 10, close: 11, low: 9, high: 12, volume: 1200 },
      { date: '2026-01-01', open: 8, close: 9, low: 7, high: 10, volume: 800 }
    ]

    mount(StockCharts, {
      props: {
        kLines,
        minuteLines: [],
        interval: 'day'
      }
    })

    await nextTick()

    expect(chartMocks.klineSetDataMock).toHaveBeenCalled()
    expect(chartMocks.volumeSetDataMock).toHaveBeenCalled()

    const klineData = chartMocks.klineSetDataMock.mock.calls.at(-1)?.[0] ?? []
    expect(klineData[0].time).toEqual({ year: 2026, month: 1, day: 1 })
    expect(klineData[0].open).toBe(8)
    expect(klineData[1].close).toBe(11)

    const volumeData = chartMocks.volumeSetDataMock.mock.calls.at(-1)?.[0] ?? []
    expect(volumeData[0].value).toBe(800)
    expect(volumeData[1].value).toBe(1200)
  })
})
