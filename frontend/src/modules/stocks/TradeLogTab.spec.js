import { beforeEach, describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import TradeLogTab from './TradeLogTab.vue'

const makeResponse = ({ ok = true, json, text } = {}) => ({
  ok,
  json: json || (async () => ({})),
  text: text || (async () => '{}')
})

const flushPromises = () => new Promise(resolve => setTimeout(resolve, 0))

const defaultSnapshot = {
  totalCapital: 100000,
  totalMarketValue: 80000,
  totalUnrealizedPnL: 2000,
  availableCash: 20000,
  totalPositionRatio: 0.8,
  positions: []
}

const defaultSummary = {
  totalPnL: 1500,
  winRate: 0.6,
  profitLossRatio: 2.1,
  dayTradePnL: 300,
  plannedTradeCount: 3,
  totalTrades: 5,
  complianceRate: 0.8,
  maxSingleLoss: -500
}

const defaultTrades = [
  {
    id: 1,
    symbol: '000001',
    name: '平安银行',
    direction: 'Buy',
    tradeType: 'Normal',
    executedPrice: 10.5,
    quantity: 1000,
    executedAt: '2026-04-03T09:30:00Z',
    complianceTag: 'FollowedPlan',
    realizedPnL: 200,
    returnRate: 0.02,
    planTitle: '银行板块计划',
    agentDirection: 'Buy',
    agentConfidence: 0.85
  }
]

function setupFetchMock(overrides = {}) {
  const responses = {
    '/api/portfolio/snapshot': makeResponse({ json: async () => overrides.snapshot ?? defaultSnapshot }),
    '/api/trades/summary': makeResponse({ json: async () => overrides.summary ?? defaultSummary }),
    '/api/trades': makeResponse({ json: async () => overrides.trades ?? defaultTrades }),
    ...overrides.extra
  }

  return vi.fn(async (url, opts) => {
    const key = Object.keys(responses).find(k => url.startsWith(k))
    if (key) return responses[key]
    return makeResponse()
  })
}

beforeEach(() => {
  vi.restoreAllMocks()
  vi.stubGlobal('confirm', vi.fn(() => true))
})

describe('TradeLogTab', () => {
  // ── Rendering ──

  it('renders portfolio snapshot and summary on mount', async () => {
    vi.stubGlobal('fetch', setupFetchMock())
    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    expect(wrapper.text()).toContain('持仓总览')
    expect(wrapper.text()).toContain('仓位')
    expect(wrapper.text()).toContain('总盈亏')
    expect(wrapper.text()).toContain('胜率')
  })

  it('renders trade list items', async () => {
    vi.stubGlobal('fetch', setupFetchMock())
    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    expect(wrapper.text()).toContain('000001')
    expect(wrapper.text()).toContain('平安银行')
    expect(wrapper.text()).toContain('遵守计划')
  })

  it('shows loading states', async () => {
    vi.stubGlobal('fetch', vi.fn(() => new Promise(() => {})))
    const wrapper = mount(TradeLogTab)
    await flushPromises()

    expect(wrapper.text()).toContain('加载持仓中...')
    expect(wrapper.text()).toContain('加载中...')
    expect(wrapper.text()).toContain('汇总加载中...')
  })

  // ── Period switching ──

  it('reloads data on period change', async () => {
    const fetchMock = setupFetchMock()
    vi.stubGlobal('fetch', fetchMock)
    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    const initialCount = fetchMock.mock.calls.length

    const weekBtn = wrapper.findAll('.toolbar .btn').find(b => b.text() === '本周')
    expect(weekBtn).toBeTruthy()
    await weekBtn.trigger('click')
    await flushPromises()

    expect(fetchMock.mock.calls.length).toBeGreaterThan(initialCount)
  })

  it('shows date inputs for custom period', async () => {
    vi.stubGlobal('fetch', setupFetchMock())
    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    const customBtn = wrapper.findAll('.toolbar .btn').find(b => b.text() === '自定义')
    await customBtn.trigger('click')
    await flushPromises()

    expect(wrapper.findAll('input[type="date"]').length).toBe(2)
  })

  // ── Custom period summary fix (Fix 4) ──

  it('does not send period=day for custom mode summary', async () => {
    const fetchMock = setupFetchMock()
    vi.stubGlobal('fetch', fetchMock)
    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    const customBtn = wrapper.findAll('.toolbar .btn').find(b => b.text() === '自定义')
    await customBtn.trigger('click')
    await flushPromises()
    await flushPromises()

    const summaryCalls = fetchMock.mock.calls.filter(c => String(c[0]).includes('/api/trades/summary'))
    const lastSummaryUrl = String(summaryCalls.at(-1)?.[0])
    expect(lastSummaryUrl).not.toContain('period=day')
  })

  // ── Trade modal open/close ──

  it('opens quick entry modal', async () => {
    vi.stubGlobal('fetch', setupFetchMock())
    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    await wrapper.find('.toolbar-actions .btn-primary').trigger('click')
    await flushPromises()

    expect(wrapper.find('.trade-modal').exists()).toBe(true)
    expect(wrapper.text()).toContain('快速录入')
  })

  it('closes modal on cancel', async () => {
    vi.stubGlobal('fetch', setupFetchMock())
    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    await wrapper.find('.toolbar-actions .btn-primary').trigger('click')
    await flushPromises()

    const cancelBtn = wrapper.find('.trade-modal-actions .btn-secondary')
    await cancelBtn.trigger('click')
    await flushPromises()

    expect(wrapper.find('.trade-modal').exists()).toBe(false)
  })

  // ── Form validation (Fix 2) ──

  it('validates empty symbol', async () => {
    vi.stubGlobal('fetch', setupFetchMock())
    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    await wrapper.find('.toolbar-actions .btn-primary').trigger('click')
    await flushPromises()

    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('请输入股票代码')
  })

  it('validates invalid price', async () => {
    vi.stubGlobal('fetch', setupFetchMock())
    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    await wrapper.find('.toolbar-actions .btn-primary').trigger('click')
    await flushPromises()

    const inputs = wrapper.findAll('.trade-modal input')
    await inputs[0].setValue('000001')
    await inputs[1].setValue('测试')

    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('请输入有效的成交价')
  })

  it('validates missing time', async () => {
    vi.stubGlobal('fetch', setupFetchMock())
    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    await wrapper.find('.toolbar-actions .btn-primary').trigger('click')
    await flushPromises()

    const inputs = wrapper.findAll('.trade-modal input')
    await inputs[0].setValue('000001')
    await inputs[1].setValue('测试')
    await inputs[2].setValue('10.50')
    await inputs[3].setValue('1000')

    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('请选择成交时间')
  })

  // ── Delete confirm ──

  it('calls delete API on confirm', async () => {
    const fetchMock = setupFetchMock()
    vi.stubGlobal('fetch', fetchMock)
    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    const deleteBtns = wrapper.findAll('.trade-item-actions .btn')
    const deleteBtn = deleteBtns.find(b => b.text() === '删除')
    expect(deleteBtn.exists()).toBe(true)
    await deleteBtn.trigger('click')
    await flushPromises()

    expect(window.confirm).toHaveBeenCalledWith('确定删除此交易记录？')
    const deleteCalls = fetchMock.mock.calls.filter(c => c[1]?.method === 'DELETE')
    expect(deleteCalls.length).toBe(1)
    expect(String(deleteCalls[0][0])).toContain('/api/trades/1')
  })

  // ── Settings modal ──

  it('opens settings modal', async () => {
    vi.stubGlobal('fetch', setupFetchMock())
    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    await wrapper.find('.toolbar-actions .btn-secondary').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('设置本金')
    expect(wrapper.text()).toContain('总本金（元）')
  })

  // ── Error display (Fix 5) ──

  it('shows error when delete fails', async () => {
    const fetchMock = vi.fn(async (url, opts) => {
      if (opts?.method === 'DELETE') throw new Error('Network error')
      const key = ['/api/portfolio/snapshot', '/api/trades/summary', '/api/trades'].find(k => url.startsWith(k))
      if (key === '/api/portfolio/snapshot') return makeResponse({ json: async () => defaultSnapshot })
      if (key === '/api/trades/summary') return makeResponse({ json: async () => defaultSummary })
      if (key === '/api/trades') return makeResponse({ json: async () => defaultTrades })
      return makeResponse()
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    const deleteBtns = wrapper.findAll('.trade-item-actions .btn')
    await deleteBtns.find(b => b.text() === '删除').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('删除交易记录失败')
  })

  it('shows error when snapshot fails', async () => {
    const fetchMock = vi.fn(async (url) => {
      if (url.startsWith('/api/portfolio/snapshot')) throw new Error('fail')
      if (url.startsWith('/api/trades/summary')) return makeResponse({ json: async () => defaultSummary })
      if (url.startsWith('/api/trades')) return makeResponse({ json: async () => defaultTrades })
      return makeResponse()
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    expect(wrapper.text()).toContain('加载持仓信息失败')
  })

  it('shows error when summary fails', async () => {
    const fetchMock = vi.fn(async (url) => {
      if (url.startsWith('/api/portfolio/snapshot')) return makeResponse({ json: async () => defaultSnapshot })
      if (url.startsWith('/api/trades/summary')) throw new Error('fail')
      if (url.startsWith('/api/trades')) return makeResponse({ json: async () => defaultTrades })
      return makeResponse()
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    expect(wrapper.text()).toContain('加载汇总数据失败')
  })

  // ── Unified fetch (Fix 1) ──

  it('uses POST with json body for saveTrade', async () => {
    const fetchMock = vi.fn(async (url, opts) => {
      if (url.startsWith('/api/portfolio/snapshot')) return makeResponse({ json: async () => defaultSnapshot })
      if (url.startsWith('/api/trades/summary')) return makeResponse({ json: async () => defaultSummary })
      if (url === '/api/trades' && opts?.method === 'POST') return makeResponse()
      if (url.startsWith('/api/trades')) return makeResponse({ json: async () => defaultTrades })
      return makeResponse()
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    await wrapper.find('.toolbar-actions .btn-primary').trigger('click')
    await flushPromises()

    const inputs = wrapper.findAll('.trade-modal input')
    await inputs[0].setValue('000001')
    await inputs[1].setValue('测试')
    await inputs[2].setValue('10.50')
    await inputs[3].setValue('1000')
    await inputs[4].setValue('2026-04-03T09:30')

    await wrapper.find('form').trigger('submit')
    await flushPromises()

    const postCalls = fetchMock.mock.calls.filter(c => c[1]?.method === 'POST')
    expect(postCalls.length).toBe(1)
    expect(postCalls[0][1].headers['Content-Type']).toBe('application/json')
    const body = JSON.parse(postCalls[0][1].body)
    expect(body.symbol).toBe('000001')
    expect(body.executedPrice).toBe(10.5)
    expect(body.quantity).toBe(1000)
  })

  it('uses PUT with json body for saveSettings', async () => {
    const fetchMock = vi.fn(async (url, opts) => {
      if (url.startsWith('/api/portfolio/snapshot')) return makeResponse({ json: async () => defaultSnapshot })
      if (url.startsWith('/api/trades/summary')) return makeResponse({ json: async () => defaultSummary })
      if (url.startsWith('/api/portfolio/settings')) return makeResponse()
      if (url.startsWith('/api/trades')) return makeResponse({ json: async () => defaultTrades })
      return makeResponse()
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    await wrapper.find('.toolbar-actions .btn-secondary').trigger('click')
    await flushPromises()

    const settingsInput = wrapper.findAll('.trade-modal input').at(-1)
    await settingsInput.setValue('200000')

    const forms = wrapper.findAll('form')
    await forms.at(-1).trigger('submit')
    await flushPromises()

    const putCalls = fetchMock.mock.calls.filter(c => c[1]?.method === 'PUT')
    expect(putCalls.length).toBe(1)
    expect(putCalls[0][1].headers['Content-Type']).toBe('application/json')
    const body = JSON.parse(putCalls[0][1].body)
    expect(body.totalCapital).toBe(200000)
  })

  // ── Review (复盘) feature ──

  it('renders review dropdown menu', async () => {
    vi.stubGlobal('fetch', setupFetchMock())
    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    const reviewBtn = wrapper.findAll('.btn').find(b => b.text().includes('生成复盘总结'))
    expect(reviewBtn).toBeTruthy()

    await reviewBtn.trigger('click')
    await flushPromises()

    expect(wrapper.find('.review-menu').exists()).toBe(true)
    expect(wrapper.text()).toContain('今日复盘')
    expect(wrapper.text()).toContain('本周复盘')
    expect(wrapper.text()).toContain('本月复盘')
    expect(wrapper.text()).toContain('自定义时段')
  })

  it('triggers POST /api/trades/reviews/generate on 今日复盘', async () => {
    const reviewResult = {
      id: 1,
      reviewType: 'Daily',
      periodStart: '2026-04-03T00:00:00',
      periodEnd: '2026-04-03T15:00:00',
      tradeCount: 3,
      totalPnL: 500,
      winRate: 0.67,
      complianceRate: 0.8,
      reviewContent: '### 复盘内容\n\n测试内容',
      createdAt: '2026-04-03T16:00:00'
    }
    const fetchMock = vi.fn(async (url, opts) => {
      if (url.startsWith('/api/trades/reviews/generate') && opts?.method === 'POST')
        return makeResponse({ json: async () => reviewResult })
      if (url.startsWith('/api/trades/reviews'))
        return makeResponse({ json: async () => [reviewResult] })
      if (url.startsWith('/api/portfolio/snapshot'))
        return makeResponse({ json: async () => defaultSnapshot })
      if (url.startsWith('/api/trades/summary'))
        return makeResponse({ json: async () => defaultSummary })
      if (url.startsWith('/api/trades'))
        return makeResponse({ json: async () => defaultTrades })
      return makeResponse()
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    // Open menu
    const reviewBtn = wrapper.findAll('.btn').find(b => b.text().includes('生成复盘总结'))
    await reviewBtn.trigger('click')
    await flushPromises()

    // Click 今日复盘
    const dailyBtn = wrapper.findAll('.review-menu-item').find(b => b.text() === '今日复盘')
    await dailyBtn.trigger('click')
    await flushPromises()
    await flushPromises()

    const generateCalls = fetchMock.mock.calls.filter(c =>
      String(c[0]).includes('/api/trades/reviews/generate') && c[1]?.method === 'POST'
    )
    expect(generateCalls.length).toBe(1)
    const body = JSON.parse(generateCalls[0][1].body)
    expect(body.type).toBe('daily')
  })

  it('shows spinner while generating review', async () => {
    const fetchMock = vi.fn(async (url, opts) => {
      if (url.startsWith('/api/trades/reviews/generate'))
        return new Promise(() => {}) // never resolves
      if (url.startsWith('/api/portfolio/snapshot'))
        return makeResponse({ json: async () => defaultSnapshot })
      if (url.startsWith('/api/trades/summary'))
        return makeResponse({ json: async () => defaultSummary })
      if (url.startsWith('/api/trades'))
        return makeResponse({ json: async () => defaultTrades })
      return makeResponse()
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    const reviewBtn = wrapper.findAll('.btn').find(b => b.text().includes('生成复盘总结'))
    await reviewBtn.trigger('click')
    await flushPromises()

    const dailyBtn = wrapper.findAll('.review-menu-item').find(b => b.text() === '今日复盘')
    await dailyBtn.trigger('click')
    await flushPromises()

    expect(wrapper.find('.review-spinner').exists()).toBe(true)
    expect(wrapper.text()).toContain('AI 教练正在分析你的交易记录')
  })

  it('renders review content with markdown', async () => {
    const reviewResult = {
      id: 1,
      reviewType: 'Daily',
      periodStart: '2026-04-03T00:00:00',
      periodEnd: '2026-04-03T15:00:00',
      tradeCount: 2,
      totalPnL: 300,
      winRate: 0.5,
      complianceRate: 1.0,
      reviewContent: '### 市场环境\n\n今天市场**震荡**，主线板块为新能源。\n\n- 要点一\n- 要点二',
      createdAt: '2026-04-03T16:00:00'
    }
    const fetchMock = vi.fn(async (url, opts) => {
      if (url.startsWith('/api/trades/reviews/generate') && opts?.method === 'POST')
        return makeResponse({ json: async () => reviewResult })
      if (url.startsWith('/api/trades/reviews'))
        return makeResponse({ json: async () => [reviewResult] })
      if (url.startsWith('/api/portfolio/snapshot'))
        return makeResponse({ json: async () => defaultSnapshot })
      if (url.startsWith('/api/trades/summary'))
        return makeResponse({ json: async () => defaultSummary })
      if (url.startsWith('/api/trades'))
        return makeResponse({ json: async () => defaultTrades })
      return makeResponse()
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    const reviewBtn = wrapper.findAll('.btn').find(b => b.text().includes('生成复盘总结'))
    await reviewBtn.trigger('click')
    await flushPromises()
    const dailyBtn = wrapper.findAll('.review-menu-item').find(b => b.text() === '今日复盘')
    await dailyBtn.trigger('click')
    await flushPromises()
    await flushPromises()

    expect(wrapper.find('.review-panel').exists()).toBe(true)
    expect(wrapper.find('.review-body').exists()).toBe(true)
    // The markdown content should be rendered into the review body
    const reviewBody = wrapper.find('.review-body')
    expect(reviewBody.text()).toContain('市场环境')
    expect(reviewBody.text()).toContain('震荡')
    expect(reviewBody.text()).toContain('要点一')
  })

  it('loads review history list', async () => {
    const reviewItems = [
      { id: 1, reviewType: 'Daily', periodStart: '2026-04-03T00:00:00', totalPnL: 500, winRate: 0.67 },
      { id: 2, reviewType: 'Weekly', periodStart: '2026-03-31T00:00:00', totalPnL: -200, winRate: 0.4 }
    ]
    const fetchMock = vi.fn(async (url, opts) => {
      if (url.startsWith('/api/trades/reviews/generate') && opts?.method === 'POST')
        return makeResponse({ json: async () => ({ ...reviewItems[0], reviewContent: '测试', tradeCount: 1, complianceRate: 1, createdAt: '2026-04-03T16:00:00' }) })
      if (url.startsWith('/api/trades/reviews'))
        return makeResponse({ json: async () => reviewItems })
      if (url.startsWith('/api/portfolio/snapshot'))
        return makeResponse({ json: async () => defaultSnapshot })
      if (url.startsWith('/api/trades/summary'))
        return makeResponse({ json: async () => defaultSummary })
      if (url.startsWith('/api/trades'))
        return makeResponse({ json: async () => defaultTrades })
      return makeResponse()
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    // Trigger generate to also load review list
    const reviewBtn = wrapper.findAll('.btn').find(b => b.text().includes('生成复盘总结'))
    await reviewBtn.trigger('click')
    await flushPromises()
    const dailyBtn = wrapper.findAll('.review-menu-item').find(b => b.text() === '今日复盘')
    await dailyBtn.trigger('click')
    await flushPromises()
    await flushPromises()
    await flushPromises()

    expect(wrapper.find('.review-history').exists()).toBe(true)
    expect(wrapper.text()).toContain('复盘历史')
  })

  it('shows error when review generation fails', async () => {
    const fetchMock = vi.fn(async (url, opts) => {
      if (url.startsWith('/api/trades/reviews/generate') && opts?.method === 'POST')
        return makeResponse({ ok: false, text: async () => '生成复盘失败' })
      if (url.startsWith('/api/portfolio/snapshot'))
        return makeResponse({ json: async () => defaultSnapshot })
      if (url.startsWith('/api/trades/summary'))
        return makeResponse({ json: async () => defaultSummary })
      if (url.startsWith('/api/trades'))
        return makeResponse({ json: async () => defaultTrades })
      return makeResponse()
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    const reviewBtn = wrapper.findAll('.btn').find(b => b.text().includes('生成复盘总结'))
    await reviewBtn.trigger('click')
    await flushPromises()
    const dailyBtn = wrapper.findAll('.review-menu-item').find(b => b.text() === '今日复盘')
    await dailyBtn.trigger('click')
    await flushPromises()
    await flushPromises()

    expect(wrapper.find('.review-panel').exists()).toBe(true)
    expect(wrapper.text()).toContain('生成复盘失败')
  })

  // ── XSS sanitization via markdownToSafeHtml (rendered through review panel) ──

  it('strips <script> tags from review content', async () => {
    const xssReview = {
      id: 1, reviewType: 'Daily', periodStart: '2026-04-03T00:00:00', periodEnd: '2026-04-03T15:00:00',
      tradeCount: 1, totalPnL: 0, winRate: 0, complianceRate: 0,
      reviewContent: '正常内容<script>alert("xss")</script>结尾',
      createdAt: '2026-04-03T16:00:00'
    }
    const fetchMock = vi.fn(async (url, opts) => {
      if (url.startsWith('/api/trades/reviews/generate') && opts?.method === 'POST')
        return makeResponse({ json: async () => xssReview })
      if (url.startsWith('/api/trades/reviews'))
        return makeResponse({ json: async () => [] })
      if (url.startsWith('/api/portfolio/snapshot'))
        return makeResponse({ json: async () => defaultSnapshot })
      if (url.startsWith('/api/trades/summary'))
        return makeResponse({ json: async () => defaultSummary })
      if (url.startsWith('/api/trades'))
        return makeResponse({ json: async () => defaultTrades })
      return makeResponse()
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    const reviewBtn = wrapper.findAll('.btn').find(b => b.text().includes('生成复盘总结'))
    await reviewBtn.trigger('click')
    await flushPromises()
    const dailyBtn = wrapper.findAll('.review-menu-item').find(b => b.text() === '今日复盘')
    await dailyBtn.trigger('click')
    await flushPromises()
    await flushPromises()

    const reviewBody = wrapper.find('.review-body')
    expect(reviewBody.html()).not.toContain('<script>')
    expect(reviewBody.text()).toContain('正常内容')
    expect(reviewBody.text()).toContain('结尾')
  })

  it('strips onerror handlers from review content', async () => {
    const xssReview = {
      id: 1, reviewType: 'Daily', periodStart: '2026-04-03T00:00:00', periodEnd: '2026-04-03T15:00:00',
      tradeCount: 1, totalPnL: 0, winRate: 0, complianceRate: 0,
      reviewContent: '前面<img src=x onerror="alert(\'xss\')">后面',
      createdAt: '2026-04-03T16:00:00'
    }
    const fetchMock = vi.fn(async (url, opts) => {
      if (url.startsWith('/api/trades/reviews/generate') && opts?.method === 'POST')
        return makeResponse({ json: async () => xssReview })
      if (url.startsWith('/api/trades/reviews'))
        return makeResponse({ json: async () => [] })
      if (url.startsWith('/api/portfolio/snapshot'))
        return makeResponse({ json: async () => defaultSnapshot })
      if (url.startsWith('/api/trades/summary'))
        return makeResponse({ json: async () => defaultSummary })
      if (url.startsWith('/api/trades'))
        return makeResponse({ json: async () => defaultTrades })
      return makeResponse()
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    const reviewBtn = wrapper.findAll('.btn').find(b => b.text().includes('生成复盘总结'))
    await reviewBtn.trigger('click')
    await flushPromises()
    const dailyBtn = wrapper.findAll('.review-menu-item').find(b => b.text() === '今日复盘')
    await dailyBtn.trigger('click')
    await flushPromises()
    await flushPromises()

    const reviewBody = wrapper.find('.review-body')
    expect(reviewBody.html()).not.toContain('onerror')
    expect(reviewBody.text()).toContain('前面')
    expect(reviewBody.text()).toContain('后面')
  })

  it('renders normal markdown correctly in review content', async () => {
    const mdReview = {
      id: 1, reviewType: 'Daily', periodStart: '2026-04-03T00:00:00', periodEnd: '2026-04-03T15:00:00',
      tradeCount: 1, totalPnL: 0, winRate: 0, complianceRate: 0,
      reviewContent: '# 标题\n\n- 列表项一\n- 列表项二\n\n**粗体文本**',
      createdAt: '2026-04-03T16:00:00'
    }
    const fetchMock = vi.fn(async (url, opts) => {
      if (url.startsWith('/api/trades/reviews/generate') && opts?.method === 'POST')
        return makeResponse({ json: async () => mdReview })
      if (url.startsWith('/api/trades/reviews'))
        return makeResponse({ json: async () => [] })
      if (url.startsWith('/api/portfolio/snapshot'))
        return makeResponse({ json: async () => defaultSnapshot })
      if (url.startsWith('/api/trades/summary'))
        return makeResponse({ json: async () => defaultSummary })
      if (url.startsWith('/api/trades'))
        return makeResponse({ json: async () => defaultTrades })
      return makeResponse()
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(TradeLogTab)
    await flushPromises()
    await flushPromises()

    const reviewBtn = wrapper.findAll('.btn').find(b => b.text().includes('生成复盘总结'))
    await reviewBtn.trigger('click')
    await flushPromises()
    const dailyBtn = wrapper.findAll('.review-menu-item').find(b => b.text() === '今日复盘')
    await dailyBtn.trigger('click')
    await flushPromises()
    await flushPromises()

    const reviewBody = wrapper.find('.review-body')
    expect(reviewBody.html()).toContain('<h1>')
    expect(reviewBody.html()).toContain('<li>')
    expect(reviewBody.html()).toContain('<strong>')
    expect(reviewBody.text()).toContain('标题')
    expect(reviewBody.text()).toContain('列表项一')
    expect(reviewBody.text()).toContain('粗体文本')
  })
})
