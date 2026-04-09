import { beforeEach, describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import MarketSentimentTab from './MarketSentimentTab.vue'

const makeResponse = ({ ok = true, status = 200, json }) => {
  const jsonFn = json || (async () => ({}))
  return {
    ok,
    status,
    json: jsonFn,
    text: async () => JSON.stringify(await jsonFn())
  }
}

const flushPromises = () => new Promise(resolve => setTimeout(resolve, 0))

const createDeferred = () => {
  let resolve
  let reject
  const promise = new Promise((res, rej) => {
    resolve = res
    reject = rej
  })

  return { promise, resolve, reject }
}

const createSummary = overrides => ({
  snapshotTime: '2026-03-15T06:35:00Z',
  sessionPhase: '盘中',
  stageLabel: '主升',
  stageLabelV2: '主升',
  stageScore: 78.6,
  stageConfidence: 84,
  maxLimitUpStreak: 5,
  limitUpCount: 62,
  limitDownCount: 4,
  brokenBoardCount: 10,
  brokenBoardRate: 16.1,
  advancers: 3680,
  decliners: 1120,
  flatCount: 202,
  top3SectorTurnoverShare: 26.4,
  top10SectorTurnoverShare: 58.8,
  diffusionScore: 72.5,
  continuationScore: 76.1,
  top3SectorTurnoverShare5dAvg: 24.1,
  top10SectorTurnoverShare5dAvg: 55.2,
  limitUpCount5dAvg: 48.6,
  brokenBoardRate5dAvg: 15.2,
  isDegraded: false,
  degradeReason: '',
  ...overrides
})

const createSectorPage = overrides => ({
  total: 1,
  snapshotTime: '2026-03-15T06:35:00Z',
  isDegraded: false,
  degradeReason: '',
  items: [{ boardType: 'concept', sectorCode: 'BK1', sectorName: '概念A', rankNo: 1, strengthScore: 80, changePercent: 4.2, mainNetInflow: 12, newsHotCount: 2, leaderName: '龙头A', advancerCount: 10 }],
  ...overrides
})

const createDetail = overrides => ({
  snapshot: { boardType: 'concept', sectorCode: 'BK1', sectorName: '概念A', changePercent: 4.2 },
  history: [],
  leaders: [],
  news: [],
  ...overrides
})

const createRealtimeOverview = overrides => ({
  snapshotTime: '2026-03-15T06:35:00Z',
  indices: [],
  breadth: { buckets: [] },
  ...overrides
})

const settle = async (times = 3) => {
  for (let index = 0; index < times; index += 1) {
    await flushPromises()
  }
}

beforeEach(() => {
  vi.restoreAllMocks()
  window.localStorage.clear()
})

describe('MarketSentimentTab', () => {
  it('shows neutral loading copy in hero before the first latest snapshot resolves', async () => {
    const latestDeferred = createDeferred()
    const fetchMock = vi.fn(async input => {
      const url = String(input)
      if (url === '/api/market/sentiment/latest') {
        return makeResponse({ json: async () => latestDeferred.promise })
      }
      if (url === '/api/market/sentiment/history?days=10') {
        return makeResponse({ json: async () => ([]) })
      }
      if (url === '/api/market/realtime/overview') {
        return makeResponse({
          json: async () => createRealtimeOverview({
            indices: [{ symbol: 'sh000001', name: '上证指数', price: 3300.12, changePercent: 0.1, turnoverAmount: 0 }]
          })
        })
      }
      if (url.includes('/api/market/sectors/realtime?boardType=concept')) {
        return makeResponse({ json: async () => ({ items: [] }) })
      }
      if (url.includes('/api/market/sectors?boardType=concept')) {
        return makeResponse({ json: async () => createSectorPage({ total: 0, items: [] }) })
      }
      throw new Error(`unexpected url: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(MarketSentimentTab)
    await flushPromises()

    const heroStage = wrapper.find('.hero-stage')
    expect(heroStage.classes()).toContain('hero-stage-loading')
    expect(heroStage.text()).toContain('加载中')
    expect(heroStage.text()).toContain('正在获取最新快照')
    expect(heroStage.text()).not.toContain('待同步')
    expect(heroStage.text()).not.toContain('暂无快照')

    const metricCards = wrapper.findAll('.metric-card')
    expect(metricCards).toHaveLength(4)
    metricCards.forEach(card => {
      expect(card.classes()).toContain('metric-card-placeholder')
      expect(card.text()).toContain('加载中')
    })
    expect(metricCards[0].text()).not.toContain('0 / 0')
    expect(metricCards[1].text()).not.toContain('0.00%')
    expect(metricCards[2].text()).not.toContain('0.0 / 0.0')
    expect(metricCards[3].text()).not.toContain('0.00%')

    const realtimeIndexCard = wrapper.find('.realtime-index-card').text()
    const realtimeFlowCard = wrapper.find('.realtime-flow-card').text()
    expect(realtimeIndexCard).toContain('成交额 实时补充中')
    expect(realtimeIndexCard).not.toContain('成交额 0')
    expect(realtimeFlowCard).toContain('待补齐')
    expect(realtimeFlowCard).not.toContain('+0.00 亿')
    expect(realtimeFlowCard).not.toContain('0 / 0')

    latestDeferred.resolve(createSummary())
    await settle(4)

    expect(wrapper.find('.hero-stage').text()).toContain('主升')
    expect(wrapper.find('.hero-stage').text()).not.toContain('正在获取最新快照')
  })

  it('loads market summary, sector list and first detail on mount', async () => {
    const fetchMock = vi.fn(async input => {
      const url = String(input)
      if (url === '/api/market/sentiment/latest') {
        return makeResponse({
          json: async () => ({
            snapshotTime: '2026-03-15T06:35:00Z',
            sessionPhase: '盘中',
            stageLabel: '主升',
            stageScore: 78.6,
            maxLimitUpStreak: 5,
            limitUpCount: 62,
            limitDownCount: 4,
            brokenBoardCount: 10,
            brokenBoardRate: 16.1,
            advancers: 3680,
            decliners: 1120,
            flatCount: 202,
            top3SectorTurnoverShare: 26.4,
            top10SectorTurnoverShare: 58.8,
            diffusionScore: 72.5,
            continuationScore: 76.1,
            stageLabelV2: '主升',
            stageConfidence: 84,
            top3SectorTurnoverShare5dAvg: 24.1,
            top10SectorTurnoverShare5dAvg: 55.2,
            limitUpCount5dAvg: 48.6,
            brokenBoardRate5dAvg: 15.2
          })
        })
      }
      if (url === '/api/market/sentiment/history?days=10') {
        return makeResponse({
          json: async () => ([
            { tradingDate: '2026-03-14T00:00:00Z', snapshotTime: '2026-03-14T07:00:00Z', stageLabel: '混沌', stageScore: 49.3 },
            { tradingDate: '2026-03-15T00:00:00Z', snapshotTime: '2026-03-15T06:35:00Z', stageLabel: '主升', stageScore: 78.6 }
          ])
        })
      }
      if (url === '/api/market/realtime/overview') {
        return makeResponse({
          json: async () => ({
            snapshotTime: '2026-03-15T06:35:00Z',
            indices: [
              { symbol: 'sh000001', name: '上证指数', price: 4000.12, changePercent: -0.35, turnoverAmount: 935264956106.7 },
              { symbol: 'sz399001', name: '深证成指', price: 13901.57, changePercent: -2.02, turnoverAmount: 1175704077687.75 },
              { symbol: 'sz399006', name: '创业板指', price: 2850.66, changePercent: -2.88, turnoverAmount: 320000000000 }
            ],
            mainCapitalFlow: { snapshotTime: '2026-03-15T06:35:00Z', mainNetInflow: -1079.04, superLargeOrderNetInflow: -615.43 },
            northboundFlow: { snapshotTime: '2026-03-15T06:35:00Z', totalNetInflow: 12.36, shanghaiNetInflow: 8.12, shenzhenNetInflow: 4.24 },
            breadth: {
              tradingDate: '2026-03-15T00:00:00Z',
              advancers: 473,
              decliners: 4815,
              flatCount: 18,
              limitUpCount: 40,
              limitDownCount: 20,
              buckets: [
                { label: '-5%', count: 552 },
                { label: '-4%', count: 1184 },
                { label: '涨停', count: 28 }
              ]
            }
          })
        })
      }
      if (url.includes('/api/market/sectors/realtime?boardType=concept')) {
        return makeResponse({
          json: async () => ({
            snapshotTime: '2026-03-15T06:36:00Z',
            items: [
              { boardType: 'concept', sectorCode: 'BK1101', sectorName: '机器人', changePercent: 4.82, mainNetInflow: 1260000000, rankNo: 1 },
              { boardType: 'concept', sectorCode: 'BK1102', sectorName: '算力租赁', changePercent: 5.15, mainNetInflow: 1520000000, rankNo: 2 }
            ]
          })
        })
      }
      if (url.includes('/api/market/sectors?')) {
        return makeResponse({
          json: async () => ({
            total: 2,
            snapshotTime: '2026-03-15T06:35:00Z',
            items: [
              {
                boardType: 'concept',
                sectorCode: 'BK1101',
                sectorName: '机器人',
                changePercent: 4.82,
                mainNetInflow: 1260000000,
                breadthScore: 86,
                continuityScore: 74,
                strengthScore: 81,
                newsSentiment: '利好',
                newsHotCount: 6,
                leaderName: '巨能股份',
                rankNo: 1,
                strengthAvg5d: 79,
                strengthAvg10d: 75,
                strengthAvg20d: 68,
                diffusionRate: 81,
                rankChange5d: 2,
                rankChange10d: 4,
                rankChange20d: 6,
                leaderStabilityScore: 70,
                mainlineScore: 78,
                isMainline: true,
                advancerCount: 22,
                declinerCount: 5,
                flatMemberCount: 1,
                limitUpMemberCount: 3
              },
              {
                boardType: 'concept',
                sectorCode: 'BK1102',
                sectorName: '算力租赁',
                changePercent: 3.15,
                mainNetInflow: 920000000,
                breadthScore: 75,
                continuityScore: 63,
                strengthScore: 70,
                newsSentiment: '中性',
                newsHotCount: 3,
                leaderName: '恒润股份',
                rankNo: 2,
                strengthAvg5d: 68,
                strengthAvg10d: 64,
                strengthAvg20d: 58,
                diffusionRate: 70,
                rankChange5d: 1,
                rankChange10d: 2,
                rankChange20d: 1,
                leaderStabilityScore: 55,
                mainlineScore: 62,
                isMainline: false,
                advancerCount: 15,
                declinerCount: 8,
                flatMemberCount: 3,
                limitUpMemberCount: 1
              }
            ]
          })
        })
      }
      if (url.includes('/api/market/sectors/BK1101?')) {
        return makeResponse({
          json: async () => ({
            snapshot: { boardType: 'concept', sectorCode: 'BK1101', sectorName: '机器人', changePercent: 4.82 },
            history: [{ tradingDate: '2026-03-15T00:00:00Z', changePercent: 4.82, strengthScore: 81, strengthAvg10d: 75, rankChange10d: 4 }],
            leaders: [{ rankInSector: 1, symbol: '832876', name: '巨能股份', changePercent: 11.2 }],
            news: [{ translatedTitle: '机器人链条获增量订单', source: '证券时报', sentiment: '利好', publishTime: '2026-03-15T05:00:00Z' }]
          })
        })
      }
      throw new Error(`unexpected url: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(MarketSentimentTab)
    await flushPromises()
    await flushPromises()
    await flushPromises()

    expect(wrapper.find('.hero-subtitle').text()).toBe('把涨停高度、涨跌家数、炸板率与板块扩散度压成同一屏，快速判断今天是主升、分歧、混沌还是退潮。')
    expect(wrapper.text()).toContain('情绪轮动')
    expect(wrapper.text()).toContain('主升')
    expect(wrapper.text()).toContain('机器人')
    expect(wrapper.text()).toContain('巨能股份')
    expect(wrapper.text()).toContain('机器人链条获增量订单')
    expect(wrapper.text()).toContain('主线')
    expect(wrapper.text()).toContain('比较窗口')
    expect(wrapper.text()).toContain('上证指数')
    expect(wrapper.text()).toContain('东财实时榜')
    expect(wrapper.text()).toContain('资金与广度')
    expect(wrapper.text()).toContain('涨跌分布桶')
  })

  it('reloads board list when board type changes', async () => {
    const fetchMock = vi.fn(async input => {
      const url = String(input)
      if (url === '/api/market/sentiment/latest') {
        return makeResponse({ json: async () => ({ stageLabel: '混沌', snapshotTime: '2026-03-15T06:35:00Z' }) })
      }
      if (url === '/api/market/sentiment/history?days=10') {
        return makeResponse({ json: async () => ([]) })
      }
      if (url === '/api/market/realtime/overview') {
        return makeResponse({ json: async () => ({ snapshotTime: '2026-03-15T06:35:00Z', indices: [], breadth: { buckets: [] } }) })
      }
      if (url.includes('/api/market/sectors/realtime?boardType=concept')) {
        return makeResponse({ json: async () => ({ items: [{ boardType: 'concept', sectorCode: 'BK1', sectorName: '概念A', rankNo: 1 }] }) })
      }
      if (url.includes('/api/market/sectors?boardType=concept')) {
        return makeResponse({ json: async () => ({ total: 1, items: [{ boardType: 'concept', sectorCode: 'BK1', sectorName: '概念A', rankNo: 1 }] }) })
      }
      if (url.includes('/api/market/sectors/BK1?')) {
        return makeResponse({ json: async () => ({ snapshot: { boardType: 'concept', sectorCode: 'BK1', sectorName: '概念A' }, history: [], leaders: [], news: [] }) })
      }
      if (url.includes('/api/market/sectors?boardType=industry')) {
        return makeResponse({ json: async () => ({ total: 1, items: [{ boardType: 'industry', sectorCode: 'BK2', sectorName: '行业A', rankNo: 1 }] }) })
      }
      if (url.includes('/api/market/sectors/realtime?boardType=industry')) {
        return makeResponse({ json: async () => ({ items: [{ boardType: 'industry', sectorCode: 'BK2', sectorName: '行业A', rankNo: 1 }] }) })
      }
      if (url.includes('/api/market/sectors/BK2?boardType=industry')) {
        return makeResponse({ json: async () => ({ snapshot: { boardType: 'industry', sectorCode: 'BK2', sectorName: '行业A' }, history: [], leaders: [], news: [] }) })
      }
      throw new Error(`unexpected url: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(MarketSentimentTab)
    await flushPromises()
    await flushPromises()

    await wrapper.find('select').setValue('industry')
    await flushPromises()
    await flushPromises()

    expect(fetchMock.mock.calls.some(([url]) => String(url).includes('/api/market/sectors?boardType=industry'))).toBe(true)
    expect(wrapper.text()).toContain('行业A')
  })

  it('keeps dashboard visible when realtime overview request fails', async () => {
    const fetchMock = vi.fn(async input => {
      const url = String(input)
      if (url === '/api/market/sentiment/latest') {
        return makeResponse({ json: async () => ({ stageLabel: '混沌', snapshotTime: '2026-03-15T06:35:00Z' }) })
      }
      if (url === '/api/market/sentiment/history?days=10') {
        return makeResponse({ json: async () => ([]) })
      }
      if (url === '/api/market/realtime/overview') {
        return makeResponse({ ok: false, status: 503, json: async () => ({ message: '实时总览暂不可用' }) })
      }
      if (url.includes('/api/market/sectors/realtime?boardType=concept')) {
        return makeResponse({ ok: false, status: 503, json: async () => ({ message: '实时板块榜暂不可用' }) })
      }
      if (url.includes('/api/market/sectors?boardType=concept')) {
        return makeResponse({ json: async () => ({ total: 1, items: [{ boardType: 'concept', sectorCode: 'BK1', sectorName: '概念A', rankNo: 1 }] }) })
      }
      if (url.includes('/api/market/sectors/BK1?')) {
        return makeResponse({ json: async () => ({ snapshot: { boardType: 'concept', sectorCode: 'BK1', sectorName: '概念A' }, history: [], leaders: [], news: [] }) })
      }
      throw new Error(`unexpected url: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(MarketSentimentTab)
    await flushPromises()
    await flushPromises()
    await flushPromises()

    expect(wrapper.text()).toContain('概念A')
    expect(wrapper.text()).toContain('实时总览暂不可用')
    expect(wrapper.text()).toContain('实时板块榜暂不可用')
  })

  it('keeps strength ordering stable when realtime board rank differs', async () => {
    const fetchMock = vi.fn(async input => {
      const url = String(input)
      if (url === '/api/market/sentiment/latest') {
        return makeResponse({ json: async () => ({ stageLabel: '混沌', snapshotTime: '2026-03-15T06:35:00Z' }) })
      }
      if (url === '/api/market/sentiment/history?days=10') {
        return makeResponse({ json: async () => ([]) })
      }
      if (url === '/api/market/realtime/overview') {
        return makeResponse({ json: async () => ({ snapshotTime: '2026-03-15T06:35:00Z', indices: [], breadth: { buckets: [] } }) })
      }
      if (url.includes('/api/market/sectors?boardType=concept')) {
        return makeResponse({
          json: async () => ({
            total: 2,
            items: [
              { boardType: 'concept', sectorCode: 'BK1', sectorName: '强度优先A', strengthScore: 90, rankNo: 9, changePercent: 3.2, mainNetInflow: 20, newsHotCount: 2, leaderName: '龙头A', advancerCount: 10 },
              { boardType: 'concept', sectorCode: 'BK2', sectorName: '强度优先B', strengthScore: 70, rankNo: 1, changePercent: 4.8, mainNetInflow: 50, newsHotCount: 2, leaderName: '龙头B', advancerCount: 10 }
            ]
          })
        })
      }
      if (url.includes('/api/market/sectors/realtime?boardType=concept')) {
        return makeResponse({
          json: async () => ({
            items: [
              { boardType: 'concept', sectorCode: 'BK1', sectorName: '强度优先A', rankNo: 9, changePercent: 3.2, mainNetInflow: 20 },
              { boardType: 'concept', sectorCode: 'BK2', sectorName: '强度优先B', rankNo: 1, changePercent: 4.8, mainNetInflow: 50 }
            ]
          })
        })
      }
      if (url.includes('/api/market/sectors/BK1?')) {
        return makeResponse({ json: async () => ({ snapshot: { boardType: 'concept', sectorCode: 'BK1', sectorName: '强度优先A' }, history: [], leaders: [], news: [] }) })
      }
      if (url.includes('/api/market/sectors/BK2?')) {
        return makeResponse({ json: async () => ({ snapshot: { boardType: 'concept', sectorCode: 'BK2', sectorName: '强度优先B' }, history: [], leaders: [], news: [] }) })
      }
      throw new Error(`unexpected url: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(MarketSentimentTab)
    await flushPromises()
    await flushPromises()
    await flushPromises()

    const cards = wrapper.findAll('.sector-card')
    expect(cards[0].text()).toContain('强度优先A')
    expect(cards[0].text()).toContain('东财#9')
    expect(cards[1].text()).toContain('强度优先B')
  })

  it('falls back to realtime breadth and marks sparse snapshots honestly', async () => {
    const fetchMock = vi.fn(async input => {
      const url = String(input)
      if (url === '/api/market/sentiment/latest') {
        return makeResponse({
          json: async () => ({
            snapshotTime: '2026-03-15T06:35:00Z',
            sessionPhase: '盘后',
            stageLabel: '混沌',
            stageScore: 43.8,
            stageConfidence: 57,
            limitUpCount: 0,
            limitDownCount: 0,
            advancers: 0,
            decliners: 0,
            flatCount: 0,
            top3SectorTurnoverShare: 0,
            top10SectorTurnoverShare: 0,
            diffusionScore: 50,
            continuationScore: 50,
            brokenBoardRate: 0,
            brokenBoardCount: 0,
            maxLimitUpStreak: 0,
            limitUpCount5dAvg: 0,
            brokenBoardRate5dAvg: 0,
            top3SectorTurnoverShare5dAvg: 0,
            top10SectorTurnoverShare5dAvg: 0
          })
        })
      }
      if (url === '/api/market/sentiment/history?days=10') {
        return makeResponse({ json: async () => ([]) })
      }
      if (url === '/api/market/realtime/overview') {
        return makeResponse({
          json: async () => ({
            snapshotTime: '2026-03-15T06:35:00Z',
            indices: [],
            mainCapitalFlow: { snapshotTime: '2026-03-15T06:35:00Z', mainNetInflow: 20, superLargeOrderNetInflow: 10 },
            northboundFlow: { snapshotTime: '2026-03-15T06:35:00Z', totalNetInflow: 3, shanghaiNetInflow: 2, shenzhenNetInflow: 1 },
            breadth: { tradingDate: '2026-03-15T00:00:00Z', advancers: 4992, decliners: 299, flatCount: 15, limitUpCount: 153, limitDownCount: 3, buckets: [{ label: '涨停', count: 83 }] }
          })
        })
      }
      if (url.includes('/api/market/sectors?boardType=concept')) {
        return makeResponse({ json: async () => ({ total: 1, items: [{ boardType: 'concept', sectorCode: 'BK1', sectorName: '快照有限板块', rankNo: 20, strengthScore: 57, changePercent: 4.42, mainNetInflow: -10, newsHotCount: 0, advancerCount: 0, declinerCount: 0, flatMemberCount: 0, limitUpMemberCount: 0, leaderName: '', leaderSymbol: '' }] }) })
      }
      if (url.includes('/api/market/sectors/realtime?boardType=concept')) {
        return makeResponse({ json: async () => ({ items: [{ boardType: 'concept', sectorCode: 'BK1', sectorName: '快照有限板块', rankNo: 20, changePercent: 4.42, mainNetInflow: -10 }] }) })
      }
      if (url.includes('/api/market/sectors/BK1?')) {
        return makeResponse({ json: async () => ({ snapshot: { boardType: 'concept', sectorCode: 'BK1', sectorName: '快照有限板块', changePercent: 4.42, advancerCount: 0, declinerCount: 0, flatMemberCount: 0, limitUpMemberCount: 0, leaderSymbol: '' }, history: [], leaders: [], news: [] }) })
      }
      throw new Error(`unexpected url: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(MarketSentimentTab)
    await flushPromises()
    await flushPromises()
    await flushPromises()

    expect(wrapper.text()).toContain('153 / 3')
    expect(wrapper.text()).toContain('4992 / 299 / 平盘 15')
    expect(wrapper.text()).toContain('待同步')
    expect(wrapper.text()).toContain('快照有限')
    expect(wrapper.text()).toContain('当前板块只有涨幅快照')
  })

  it('renders degraded hero, placeholder cards, empty board and realtime note clearly', async () => {
    const fetchMock = vi.fn(async input => {
      const url = String(input)
      if (url === '/api/market/sentiment/latest') {
        return makeResponse({
          json: async () => createSummary({
            stageLabel: '混沌',
            stageLabelV2: '同步不完整',
            stageScore: 0,
            stageConfidence: 0,
            maxLimitUpStreak: 0,
            limitUpCount: 0,
            limitDownCount: 0,
            brokenBoardCount: 0,
            brokenBoardRate: 0,
            advancers: 0,
            decliners: 0,
            flatCount: 0,
            top3SectorTurnoverShare: 0,
            top10SectorTurnoverShare: 0,
            diffusionScore: 0,
            continuationScore: 0,
            top3SectorTurnoverShare5dAvg: 0,
            top10SectorTurnoverShare5dAvg: 0,
            limitUpCount5dAvg: 0,
            brokenBoardRate5dAvg: 0,
            isDegraded: true,
            degradeReason: 'market_breadth_unavailable,sector_rankings_unavailable'
          })
        })
      }
      if (url === '/api/market/sentiment/history?days=10') {
        return makeResponse({
          json: async () => ([
            { tradingDate: '2026-03-15T00:00:00Z', snapshotTime: '2026-03-15T06:35:00Z', stageLabel: '混沌', stageScore: 0 }
          ])
        })
      }
      if (url === '/api/market/realtime/overview') {
        return makeResponse({
          json: async () => createRealtimeOverview({
            indices: [{ symbol: 'sh000001', name: '上证指数', price: 3300.12, changePercent: 0.1, turnoverAmount: 0 }]
          })
        })
      }
      if (url.includes('/api/market/sectors/realtime?boardType=concept')) {
        return makeResponse({ json: async () => ({ items: [] }) })
      }
      if (url.includes('/api/market/sectors?boardType=concept')) {
        return makeResponse({
          json: async () => createSectorPage({
            total: 0,
            items: [],
            isDegraded: true,
            degradeReason: 'sector_rankings_unavailable'
          })
        })
      }
      throw new Error(`unexpected url: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(MarketSentimentTab)
    await settle()

    const heroStage = wrapper.find('.hero-stage').text()
    const heroSubtitle = wrapper.find('.hero-subtitle').text()
    const historyChip = wrapper.find('.history-chip').text()
    const emptyState = wrapper.find('.sector-empty-state').text()
    const toolbarMeta = wrapper.find('.toolbar-meta').text()
    const metricCards = wrapper.findAll('.metric-card')
    const realtimeIndexCard = wrapper.find('.realtime-index-card').text()
    const realtimeFlowCard = wrapper.find('.realtime-flow-card').text()
    const realtimeBreadthCard = wrapper.find('.realtime-breadth-card').text()

    expect(heroSubtitle).toBe('当前仅同步到部分市场快照，下方实时数据仅供参考。')
    expect(heroStage).toContain('同步不完整')
    expect(heroStage).toContain('关键广度或榜单未同步完成，暂不输出阶段判断。')
    expect(heroStage).not.toContain('情绪分 0.00 / 置信 0')
    expect(heroStage).toContain('市场涨跌与涨跌停数据暂未同步完成')
    expect(historyChip).toContain('同步不完整')
    expect(historyChip).not.toContain('混沌')
    expect(toolbarMeta).toContain('榜单待补齐')
    expect(toolbarMeta).not.toContain('共 0 个板块')
    expect(emptyState).toContain('这次同步只拿到市场摘要，板块排行未同步完成。')
    expect(emptyState).toContain('为了避免把旧榜单误当最新结果，这里暂不展示历史榜单。')
    expect(metricCards[0].classes()).toContain('metric-card-placeholder')
    expect(metricCards[0].text()).toContain('待补齐')
    expect(metricCards[0].text()).not.toContain('0 / 0')
    expect(metricCards[1].text()).toContain('暂不展示')
    expect(metricCards[1].text()).not.toContain('0.00%')
    expect(metricCards[2].text()).toContain('以实时补充为准')
    expect(metricCards[2].text()).not.toContain('0.0 / 0.0')
    expect(metricCards[3].text()).toContain('待补齐')
    expect(metricCards[3].text()).not.toContain('0.00%')
    expect(wrapper.find('.realtime-note').text()).toContain('仅供参考')
    expect(realtimeIndexCard).toContain('成交额 实时补充中')
    expect(realtimeIndexCard).not.toContain('成交额 0')
    expect(realtimeFlowCard).toContain('待补齐')
    expect(realtimeFlowCard).toContain('涨停 暂不展示')
    expect(realtimeFlowCard).not.toContain('+0.00 亿')
    expect(realtimeFlowCard).not.toContain('0 / 0')
    expect(realtimeFlowCard).not.toContain('涨停 0 / 跌停 0 / 平盘 0')
    expect(realtimeBreadthCard).toContain('实时补充中')
  })

  it('shows partial sync feedback when refreshed data is still degraded', async () => {
    let summaryCalls = 0
    let sectorCalls = 0
    const summaryPayloads = [
      createSummary(),
      createSummary({
        stageLabel: '混沌',
        stageLabelV2: '同步不完整',
        stageScore: 0,
        stageConfidence: 0,
        isDegraded: true,
        degradeReason: 'market_breadth_unavailable,sector_rankings_unavailable'
      })
    ]
    const sectorPayloads = [
      createSectorPage(),
      createSectorPage({ total: 0, items: [], isDegraded: true, degradeReason: 'sector_rankings_unavailable' })
    ]
    const fetchMock = vi.fn(async (input, init) => {
      const url = String(input)
      if (url === '/api/market/sync' && init?.method === 'POST') {
        return makeResponse({ json: async () => ({ synced: true, timestamp: '2026-03-15T06:40:00Z' }) })
      }
      if (url === '/api/market/sentiment/latest') {
        return makeResponse({
          json: async () => summaryPayloads[Math.min(summaryCalls++, summaryPayloads.length - 1)]
        })
      }
      if (url === '/api/market/sentiment/history?days=10') {
        return makeResponse({ json: async () => ([]) })
      }
      if (url === '/api/market/realtime/overview') {
        return makeResponse({ json: async () => createRealtimeOverview() })
      }
      if (url.includes('/api/market/sectors/realtime?boardType=concept')) {
        return makeResponse({ json: async () => ({ items: [] }) })
      }
      if (url.includes('/api/market/sectors?boardType=concept')) {
        return makeResponse({
          json: async () => sectorPayloads[Math.min(sectorCalls++, sectorPayloads.length - 1)]
        })
      }
      if (url.includes('/api/market/sectors/BK1?')) {
        return makeResponse({ json: async () => createDetail() })
      }
      throw new Error(`unexpected url: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(MarketSentimentTab)
    await settle()

    await wrapper.find('.hero-actions .hero-button').trigger('click')
    await settle(4)

    expect(wrapper.text()).toContain('本次同步已完成，但仍有部分数据缺失')
    expect(wrapper.text()).toContain('市场涨跌与涨跌停数据暂未同步完成')
    expect(wrapper.text()).not.toContain('market_breadth_unavailable')
  })

  it('shows full success sync feedback when summary and boards refresh cleanly', async () => {
    const fetchMock = vi.fn(async (input, init) => {
      const url = String(input)
      if (url === '/api/market/sync' && init?.method === 'POST') {
        return makeResponse({ json: async () => ({ synced: true, timestamp: '2026-03-15T06:40:00Z' }) })
      }
      if (url === '/api/market/sentiment/latest') {
        return makeResponse({ json: async () => createSummary() })
      }
      if (url === '/api/market/sentiment/history?days=10') {
        return makeResponse({ json: async () => ([]) })
      }
      if (url === '/api/market/realtime/overview') {
        return makeResponse({ json: async () => createRealtimeOverview() })
      }
      if (url.includes('/api/market/sectors/realtime?boardType=concept')) {
        return makeResponse({ json: async () => ({ items: [] }) })
      }
      if (url.includes('/api/market/sectors?boardType=concept')) {
        return makeResponse({ json: async () => createSectorPage() })
      }
      if (url.includes('/api/market/sectors/BK1?')) {
        return makeResponse({ json: async () => createDetail() })
      }
      throw new Error(`unexpected url: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(MarketSentimentTab)
    await settle()

    await wrapper.find('.hero-actions .hero-button').trigger('click')
    await settle(4)

    expect(wrapper.text()).toContain('最新市场摘要与板块榜单已同步完成。')
  })

  it('shows sync failure feedback without hiding the current dashboard', async () => {
    const fetchMock = vi.fn(async (input, init) => {
      const url = String(input)
      if (url === '/api/market/sync' && init?.method === 'POST') {
        return makeResponse({ ok: false, status: 503, json: async () => ({ message: '同步通道暂不可用' }) })
      }
      if (url === '/api/market/sentiment/latest') {
        return makeResponse({ json: async () => createSummary() })
      }
      if (url === '/api/market/sentiment/history?days=10') {
        return makeResponse({ json: async () => ([]) })
      }
      if (url === '/api/market/realtime/overview') {
        return makeResponse({ json: async () => createRealtimeOverview() })
      }
      if (url.includes('/api/market/sectors/realtime?boardType=concept')) {
        return makeResponse({ json: async () => ({ items: [] }) })
      }
      if (url.includes('/api/market/sectors?boardType=concept')) {
        return makeResponse({ json: async () => createSectorPage() })
      }
      if (url.includes('/api/market/sectors/BK1?')) {
        return makeResponse({ json: async () => createDetail() })
      }
      throw new Error(`unexpected url: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(MarketSentimentTab)
    await settle()

    await wrapper.find('.hero-actions .hero-button').trigger('click')
    await settle(2)

    expect(wrapper.text()).toContain('同步通道暂不可用')
    expect(wrapper.text()).toContain('概念A')
  })
})
