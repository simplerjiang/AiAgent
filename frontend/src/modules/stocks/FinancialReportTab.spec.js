import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import FinancialReportTab from './FinancialReportTab.vue'

const mockFetch = vi.fn()
global.fetch = mockFetch

const mockTrendData = {
  symbol: '600519',
  periodCount: 2,
  revenue: [
    { period: '2024-12-31', value: 173695000000, yoY: 16.25 },
    { period: '2023-12-31', value: 149451000000, yoY: 18.04 }
  ],
  netProfit: [
    { period: '2024-12-31', value: 86229000000, yoY: 15.38 },
    { period: '2023-12-31', value: 74734000000, yoY: 19.16 }
  ],
  totalAssets: [
    { period: '2024-12-31', value: 278543000000, yoY: 12.50 }
  ],
  recentDividends: [
    { plan: '2024年年报 10派275.83元', dividendPerShare: 27.583 }
  ]
}

const mockSummaryData = {
  symbol: '600519',
  periodCount: 2,
  periods: [
    {
      reportDate: '2024-12-31',
      reportType: 'Annual',
      sourceChannel: 'emweb',
      keyMetrics: { Revenue: 173695000000, NetProfit: 86229000000, TotalAssets: 278543000000, DebtToAssetRatio: 0.25 }
    },
    {
      reportDate: '2023-12-31',
      reportType: 'Annual',
      sourceChannel: 'emweb',
      keyMetrics: { Revenue: 149451000000, NetProfit: 74734000000, TotalAssets: 247594000000, DebtToAssetRatio: 0.23 }
    }
  ]
}

const emptyTrendData = {
  symbol: 'SZ000001',
  revenue: [],
  netProfit: [],
  totalAssets: [],
  recentDividends: []
}

const emptySummaryData = {
  symbol: 'SZ000001',
  periods: []
}

const sparsePdfSummaryData = {
  symbol: 'SZ000001',
  periods: [
    {
      reportDate: '2024-09-30',
      reportType: 'Quarterly',
      sourceChannel: 'pdf',
      keyMetrics: {}
    },
    {
      reportDate: '2024-06-30',
      reportType: 'Quarterly',
      sourceChannel: 'pdf',
      keyMetrics: null
    }
  ]
}

const camelCaseSuccessCollectResult = {
  success: true,
  channel: 'emweb',
  reportCount: 4,
  durationMs: 3500,
  isDegraded: true,
  degradeReason: 'emweb empty data'
}

const pascalCaseSuccessCollectResult = {
  Success: true,
  Channel: 'emweb',
  ReportCount: 4,
  DurationMs: 3500
}

function createJsonResponse(data, ok = true, status = ok ? 200 : 500) {
  return {
    ok,
    status,
    json: () => Promise.resolve(data)
  }
}

function setupFetchMock(options = {}) {
  const {
    trendData = mockTrendData,
    summaryData = mockSummaryData,
    trendOk = true,
    summaryOk = true
  } = options

  mockFetch.mockImplementation((url) => {
    if (url.includes('/trend/')) {
      return Promise.resolve(createJsonResponse(trendData, trendOk))
    }
    if (url.includes('/summary/')) {
      return Promise.resolve(createJsonResponse(summaryData, summaryOk))
    }
    return Promise.resolve({ ok: false })
  })
}

describe('FinancialReportTab', () => {
  beforeEach(() => {
    mockFetch.mockReset()
  })

  it('shows empty state when no symbol', async () => {
    const wrapper = mount(FinancialReportTab, { props: { symbol: '', active: true } })
    await flushPromises()
    expect(wrapper.text()).toContain('请先选择一只股票')
    expect(mockFetch).not.toHaveBeenCalled()
  })

  it('does not fetch when inactive', async () => {
    setupFetchMock()
    mount(FinancialReportTab, { props: { symbol: '600519', active: false } })
    await flushPromises()
    expect(mockFetch).not.toHaveBeenCalled()
  })

  it('fetches data when active with symbol', async () => {
    setupFetchMock()
    mount(FinancialReportTab, { props: { symbol: '600519', active: true } })
    await flushPromises()
    expect(mockFetch).toHaveBeenCalledTimes(2)
    expect(mockFetch).toHaveBeenCalledWith(expect.stringContaining('/api/stocks/financial/trend/600519'))
    expect(mockFetch).toHaveBeenCalledWith(expect.stringContaining('/api/stocks/financial/summary/600519'))
  })

  it('renders metric cards with data', async () => {
    setupFetchMock()
    const wrapper = mount(FinancialReportTab, { props: { symbol: '600519', active: true } })
    await flushPromises()
    const cards = wrapper.findAll('.metric-card')
    expect(cards.length).toBe(3)
    expect(wrapper.text()).toContain('营业收入')
    expect(wrapper.text()).toContain('净利润')
    expect(wrapper.text()).toContain('总资产')
  })

  it('renders trend table rows', async () => {
    setupFetchMock()
    const wrapper = mount(FinancialReportTab, { props: { symbol: '600519', active: true } })
    await flushPromises()
    const rows = wrapper.findAll('.trend-table tbody tr')
    expect(rows.length).toBeGreaterThanOrEqual(1)
    expect(wrapper.text()).toContain('2024-12-31')
  })

  it('switches between statement types', async () => {
    setupFetchMock()
    const wrapper = mount(FinancialReportTab, { props: { symbol: '600519', active: true } })
    await flushPromises()

    expect(wrapper.find('.statement-tabs button.active').text()).toBe('利润表')

    const buttons = wrapper.findAll('.statement-tabs button')
    const balanceBtn = buttons.find(b => b.text() === '资产负债表')
    await balanceBtn.trigger('click')
    expect(wrapper.find('.statement-tabs button.active').text()).toBe('资产负债表')
  })

  it('renders dividend records', async () => {
    setupFetchMock()
    const wrapper = mount(FinancialReportTab, { props: { symbol: '600519', active: true } })
    await flushPromises()
    expect(wrapper.text()).toContain('近期分红')
    expect(wrapper.text()).toContain('2024年年报')
  })

  it('shows error state on fetch failure', async () => {
    mockFetch.mockRejectedValue(new Error('Network error'))
    const wrapper = mount(FinancialReportTab, { props: { symbol: '600519', active: true } })
    await flushPromises()
    expect(wrapper.text()).toContain('加载失败')
  })

  it('shows collect button when endpoints return empty payloads', async () => {
    setupFetchMock({ trendData: emptyTrendData, summaryData: emptySummaryData })
    const wrapper = mount(FinancialReportTab, { props: { symbol: 'SZ000001', active: true } })
    await flushPromises()
    expect(wrapper.find('.collect-btn').exists()).toBe(true)
    expect(wrapper.find('.collect-btn').text()).toContain('获取财务数据')
    expect(wrapper.find('.refresh-btn').exists()).toBe(false)
  })

  it('refreshes data from a camelCase collect success response and keeps the success banner', async () => {
    mockFetch
      .mockResolvedValueOnce(createJsonResponse(emptyTrendData))
      .mockResolvedValueOnce(createJsonResponse(emptySummaryData))
      .mockResolvedValueOnce(createJsonResponse(camelCaseSuccessCollectResult))
      .mockResolvedValueOnce(createJsonResponse(mockTrendData))
      .mockResolvedValueOnce(createJsonResponse(mockSummaryData))

    const wrapper = mount(FinancialReportTab, { props: { symbol: 'SZ000001', active: true } })
    await flushPromises()

    expect(wrapper.find('.collect-btn').exists()).toBe(true)

    await wrapper.find('.collect-btn').trigger('click')
    await flushPromises()

    const postCall = mockFetch.mock.calls.find(c => c[1]?.method === 'POST')
    expect(postCall).toBeTruthy()
    expect(postCall[0]).toContain('/api/stocks/financial/collect/SZ000001')
    expect(wrapper.find('.refresh-btn').exists()).toBe(true)
    expect(wrapper.find('.collect-btn').exists()).toBe(false)
    expect(wrapper.text()).toContain('营业收入')
    expect(wrapper.find('.collect-info').exists()).toBe(true)
    expect(wrapper.find('.collect-info').text()).toContain('已通过 emweb 获取 4 期报表')
    expect(wrapper.find('.collect-info').text()).toContain('提示：采集渠道未返回有效数据。')
  })

  it('localizes a live-style English collect error message for the empty state', async () => {
    mockFetch
      .mockResolvedValueOnce(createJsonResponse(emptyTrendData))
      .mockResolvedValueOnce(createJsonResponse(emptySummaryData))
      .mockResolvedValueOnce(createJsonResponse({ success: false, errorMessage: 'All channels (API + PDF) failed or returned empty data', isDegraded: true, degradeReason: 'emweb empty data' }))

    const wrapper = mount(FinancialReportTab, { props: { symbol: 'SZ000001', active: true } })
    await flushPromises()

    await wrapper.find('.collect-btn').trigger('click')
    await flushPromises()

    expect(wrapper.find('.error-msg').exists()).toBe(true)
    expect(wrapper.find('.error-msg').classes()).toContain('error-msg-prominent')
    expect(wrapper.find('.error-msg').text()).toContain('所有采集渠道都未返回有效财务数据，请稍后重试或更换股票。')
  })

  it('falls back to the camelCase degradeReason when collect errorMessage is missing', async () => {
    mockFetch
      .mockResolvedValueOnce(createJsonResponse(emptyTrendData))
      .mockResolvedValueOnce(createJsonResponse(emptySummaryData))
      .mockResolvedValueOnce(createJsonResponse({ success: false, isDegraded: true, degradeReason: 'emweb empty data' }))

    const wrapper = mount(FinancialReportTab, { props: { symbol: 'SZ000001', active: true } })
    await flushPromises()

    await wrapper.find('.collect-btn').trigger('click')
    await flushPromises()

    expect(wrapper.find('.error-msg').exists()).toBe(true)
    expect(wrapper.find('.error-msg').text()).toContain('采集渠道未返回有效数据。')
  })

  it('accepts a PascalCase collect success response for compatibility', async () => {
    mockFetch
      .mockResolvedValueOnce(createJsonResponse(emptyTrendData))
      .mockResolvedValueOnce(createJsonResponse(emptySummaryData))
      .mockResolvedValueOnce(createJsonResponse(pascalCaseSuccessCollectResult))
      .mockResolvedValueOnce(createJsonResponse(mockTrendData))
      .mockResolvedValueOnce(createJsonResponse(mockSummaryData))

    const wrapper = mount(FinancialReportTab, { props: { symbol: 'SZ000001', active: true } })
    await flushPromises()

    await wrapper.find('.collect-btn').trigger('click')
    await flushPromises()

    expect(wrapper.find('.collect-info').exists()).toBe(true)
    expect(wrapper.find('.collect-info').text()).toContain('已通过 emweb 获取 4 期报表')
    expect(wrapper.text()).toContain('营业收入')
  })

  it('shows an explicit partial-data message when collect succeeds with only sparse report periods', async () => {
    mockFetch
      .mockResolvedValueOnce(createJsonResponse(emptyTrendData))
      .mockResolvedValueOnce(createJsonResponse(emptySummaryData))
      .mockResolvedValueOnce(createJsonResponse({ success: true, channel: 'pdf', reportCount: 2, durationMs: 1800 }))
      .mockResolvedValueOnce(createJsonResponse(emptyTrendData))
      .mockResolvedValueOnce(createJsonResponse(sparsePdfSummaryData))

    const wrapper = mount(FinancialReportTab, { props: { symbol: 'SZ000001', active: true } })
    await flushPromises()

    await wrapper.find('.collect-btn').trigger('click')
    await flushPromises()

    expect(wrapper.find('.refresh-btn').exists()).toBe(true)
    expect(wrapper.find('.collect-info').exists()).toBe(false)
    expect(wrapper.find('.partial-data-message').exists()).toBe(true)
    expect(wrapper.find('.partial-data-message').text()).toContain('已通过 pdf 获取 2 期报表')
    expect(wrapper.find('.partial-data-message').text()).toContain('暂无可展示的结构化财务指标')
    expect(wrapper.find('.partial-data-message').text()).toContain('2024-09-30')
    expect(wrapper.find('.partial-data-message').text()).toContain('来源：pdf')
    expect(wrapper.find('.summary-table').exists()).toBe(false)
    expect(wrapper.find('.trend-table').exists()).toBe(false)
    expect(wrapper.text()).not.toContain('营业收入')
  })

  it('clears previous symbol data when the next symbol returns empty payloads', async () => {
    mockFetch.mockImplementation((url) => {
      if (url.includes('/trend/600519')) {
        return Promise.resolve(createJsonResponse(mockTrendData))
      }
      if (url.includes('/summary/600519')) {
        return Promise.resolve(createJsonResponse(mockSummaryData))
      }
      if (url.includes('/trend/SZ000001')) {
        return Promise.resolve(createJsonResponse(emptyTrendData))
      }
      if (url.includes('/summary/SZ000001')) {
        return Promise.resolve(createJsonResponse(emptySummaryData))
      }
      return Promise.resolve({ ok: false })
    })

    const wrapper = mount(FinancialReportTab, { props: { symbol: '600519', active: true } })
    await flushPromises()

    expect(wrapper.find('.refresh-btn').exists()).toBe(true)
    expect(wrapper.text()).toContain('营业收入')

    await wrapper.setProps({ symbol: 'SZ000001' })
    await flushPromises()

    expect(wrapper.find('.collect-btn').exists()).toBe(true)
    expect(wrapper.find('.refresh-btn').exists()).toBe(false)
    expect(wrapper.text()).toContain('暂无财务数据')
    expect(wrapper.text()).not.toContain('营业收入')
  })

  it('shows refresh button when data exists', async () => {
    setupFetchMock()
    const wrapper = mount(FinancialReportTab, { props: { symbol: '600519', active: true } })
    await flushPromises()
    expect(wrapper.find('.refresh-btn').exists()).toBe(true)
    expect(wrapper.find('.refresh-btn').text()).toContain('刷新数据')
  })

  it('shows error when collect fails', async () => {
    mockFetch
      .mockResolvedValueOnce(createJsonResponse(emptyTrendData))
      .mockResolvedValueOnce(createJsonResponse(emptySummaryData))
      .mockResolvedValueOnce(createJsonResponse({ error: '采集失败 (503)' }, false, 503))

    const wrapper = mount(FinancialReportTab, { props: { symbol: 'SZ000001', active: true } })
    await flushPromises()

    await wrapper.find('.collect-btn').trigger('click')
    await flushPromises()

    expect(wrapper.find('.error-msg').exists()).toBe(true)
    expect(wrapper.find('.error-msg').text()).toContain('采集失败')
  })
})
