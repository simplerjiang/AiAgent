import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import StockInfoTab from './StockInfoTab.vue'

const makeResponse = ({ ok, status, json, text }) => ({
  ok,
  status,
  json: json || (async () => ([])),
  text: text || (async () => '')
})

const flushPromises = () => new Promise(resolve => setTimeout(resolve, 0))

const createChatFetchMock = (handlers = {}) => {
  const sessionsBySymbol = {}
  const messagesBySession = {}

  const ensureSessions = symbol => {
    if (!sessionsBySymbol[symbol]) {
      sessionsBySymbol[symbol] = [
        { sessionKey: `${symbol}-1`, title: '默认会话' }
      ]
    }
    return sessionsBySymbol[symbol]
  }

  const fetchMock = vi.fn(async (url, options = {}) => {
    if (url.startsWith('/api/stocks/chat/sessions?')) {
      const params = new URLSearchParams(url.split('?')[1])
      const symbol = params.get('symbol') || ''
      const list = ensureSessions(symbol)
      return makeResponse({ ok: true, status: 200, json: async () => list })
    }

    if (url === '/api/stocks/chat/sessions' && options.method === 'POST') {
      const body = JSON.parse(options.body)
      const symbol = body.symbol
      const title = body.title || '默认会话'
      const key = `${symbol}-${Date.now()}`
      const entry = { sessionKey: key, title }
      sessionsBySymbol[symbol] = [entry, ...(sessionsBySymbol[symbol] || [])]
      return makeResponse({ ok: true, status: 200, json: async () => entry })
    }

    if (url.includes('/api/stocks/chat/sessions/') && url.endsWith('/messages')) {
      const sessionKey = url.split('/api/stocks/chat/sessions/')[1].replace('/messages', '')
      if (options.method === 'PUT') {
        const body = JSON.parse(options.body)
        messagesBySession[sessionKey] = body.messages || []
        return makeResponse({ ok: true, status: 200, json: async () => ({ status: 'ok' }) })
      }
      return makeResponse({ ok: true, status: 200, json: async () => messagesBySession[sessionKey] || [] })
    }

    if (handlers.handle) {
      const handled = await handlers.handle(url, options)
      if (handled) return handled
    }

    if (url === '/api/stocks/sources') {
      return makeResponse({ ok: true, status: 200, json: async () => ([]) })
    }
    if (url === '/api/stocks/history') {
      return makeResponse({ ok: true, status: 200, json: async () => ([]) })
    }
    if (url.startsWith('/api/stocks/agents/history')) {
      return makeResponse({ ok: true, status: 200, json: async () => ([]) })
    }

    if (url.startsWith('/api/stocks/news/impact')) {
      return makeResponse({
        ok: true,
        status: 200,
        json: async () => ({
          summary: { positive: 0, neutral: 0, negative: 0, overall: '中性' },
          events: []
        })
      })
    }

    return makeResponse({ ok: false, status: 404 })
  })

  return { fetchMock, messagesBySession, sessionsBySymbol }
}

beforeEach(() => {
  vi.restoreAllMocks()
  localStorage.clear()
})

describe('StockInfoTab', () => {
  it('renders search input and button', () => {
    const wrapper = mount(StockInfoTab)
    const input = wrapper.find('input')
    const button = wrapper.find('button')

    expect(input.exists()).toBe(true)
    expect(button.exists()).toBe(true)
  })

  it('sends chat prompt with selected stock context', async () => {
    const { fetchMock } = createChatFetchMock({
      handle: async (url) => {
        if (url === '/api/llm/chat/stream/openai') {
          return makeResponse({ ok: true, status: 200, json: async () => ({ content: 'ok' }) })
        }
        return null
      }
    })

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(StockInfoTab)
    wrapper.vm.detail = {
      quote: {
        name: '深科技',
        symbol: 'sz000021',
        price: 31.1,
        change: -0.72,
        changePercent: -2.26,
        high: 32,
        low: 30,
        timestamp: '2026-01-29T00:00:00Z'
      },
      kLines: [],
      minuteLines: [],
      messages: []
    }
    await wrapper.vm.$nextTick()
    await flushPromises()
    await flushPromises()
    const chatWindow = wrapper.findComponent({ name: 'ChatWindow' })
    chatWindow.vm.chatInput = '今天走势如何？'

    await chatWindow.vm.sendChat()
    await wrapper.vm.$nextTick()

    const call = fetchMock.mock.calls.find(args => args[0] === '/api/llm/chat/stream/openai')
    expect(call).toBeTruthy()
    const body = JSON.parse(call[1].body)
    expect(body.prompt).toContain('sz000021')
    expect(body.prompt).toContain('今天走势如何？')
    expect(body.useInternet).toBe(true)
  })

  it('shows loading indicator while chatting', async () => {
    const { fetchMock } = createChatFetchMock({
      handle: async (url) => {
        if (url === '/api/llm/chat/stream/openai') {
          return makeResponse({ ok: true, status: 200, json: async () => ({ content: 'ok' }) })
        }
        return null
      }
    })

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(StockInfoTab)
    wrapper.vm.detail = {
      quote: { name: '深科技', symbol: 'sz000021', price: 31.1, change: 0, changePercent: 0 },
      kLines: [],
      minuteLines: [],
      messages: []
    }
    await wrapper.vm.$nextTick()
    await flushPromises()
    const chatWindow = wrapper.findComponent({ name: 'ChatWindow' })

    chatWindow.vm.chatInput = '测试'
    const pending = chatWindow.vm.sendChat()

    expect(chatWindow.vm.chatLoading).toBe(true)
    await pending
  })

  it('includes time-check question in chat prompt', async () => {
    const { fetchMock } = createChatFetchMock({
      handle: async (url) => {
        if (url === '/api/llm/chat/stream/openai') {
          return makeResponse({ ok: true, status: 200, json: async () => ({ content: 'ok' }) })
        }
        return null
      }
    })

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(StockInfoTab)
    wrapper.vm.detail = {
      quote: { name: '深科技', symbol: 'sz000021', price: 31.1, change: 0, changePercent: 0 },
      kLines: [],
      minuteLines: [],
      messages: []
    }
    await wrapper.vm.$nextTick()
    await flushPromises()

    const question = '询问今日时间是不是2026年1月29号'
    const chatWindow = wrapper.findComponent({ name: 'ChatWindow' })
    chatWindow.vm.chatInput = question
    await chatWindow.vm.sendChat()

    const call = fetchMock.mock.calls.find(args => args[0] === '/api/llm/chat/stream/openai')
    const body = JSON.parse(call[1].body)
    expect(body.prompt).toContain(question)
    expect(body.useInternet).toBe(true)
  })

  it('streams assistant response chunks and persists history per stock', async () => {
    const encoder = new TextEncoder()
    const stream = new ReadableStream({
      start(controller) {
        controller.enqueue(encoder.encode('data: 你好\n\n'))
        controller.enqueue(encoder.encode('data: 世界\n\n'))
        controller.enqueue(encoder.encode('data: [DONE]\n\n'))
        controller.close()
      }
    })

    const { fetchMock, messagesBySession } = createChatFetchMock({
      handle: async (url) => {
        if (url === '/api/llm/chat/stream/openai') {
          return {
            ok: true,
            status: 200,
            body: stream,
            text: async () => '',
            json: async () => ({})
          }
        }
        return null
      }
    })

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(StockInfoTab)
    wrapper.vm.detail = {
      quote: { name: '深科技', symbol: 'sz000021', price: 31.1, change: 0, changePercent: 0 },
      kLines: [],
      minuteLines: [],
      messages: []
    }
    await wrapper.vm.$nextTick()
    await flushPromises()
    const chatWindow = wrapper.findComponent({ name: 'ChatWindow' })

    chatWindow.vm.chatInput = '测试流式'
    await chatWindow.vm.sendChat()
    await flushPromises()

    const assistant = chatWindow.vm.chatMessages.find(item => item.role === 'assistant')
    expect(assistant.content).toBe('你好世界')

    const sessionKey = wrapper.vm.selectedChatSession
    expect(messagesBySession[sessionKey]?.some(item => item.content === '你好世界')).toBe(true)

    wrapper.vm.detail = {
      quote: { name: '平安银行', symbol: 'sz000001', price: 12, change: 0, changePercent: 0 },
      kLines: [],
      minuteLines: [],
      messages: []
    }
    await wrapper.vm.$nextTick()
    await flushPromises()
    await flushPromises()

    wrapper.vm.detail = {
      quote: { name: '深科技', symbol: 'sz000021', price: 31.1, change: 0, changePercent: 0 },
      kLines: [],
      minuteLines: [],
      messages: []
    }
    await wrapper.vm.$nextTick()
    await flushPromises()
    const restored = chatWindow.vm.chatMessages.find(item => item.role === 'assistant')
    expect(restored?.content).toBe('你好世界')
  })

  it('keeps chat history per stock and switches on symbol change', async () => {
    const { fetchMock, messagesBySession } = createChatFetchMock()

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(StockInfoTab)
    wrapper.vm.detail = {
      quote: { name: '深科技', symbol: 'sz000021', price: 31.1, change: 0, changePercent: 0 },
      kLines: [],
      minuteLines: [],
      messages: []
    }
    await wrapper.vm.$nextTick()
    const chatWindow = wrapper.findComponent({ name: 'ChatWindow' })
    chatWindow.vm.chatMessages = [{ role: 'assistant', content: 'A', timestamp: '2026-01-29T00:00:00Z' }]
    await wrapper.vm.$nextTick()
    messagesBySession[wrapper.vm.selectedChatSession] = chatWindow.vm.chatMessages

    wrapper.vm.detail = {
      quote: { name: '平安银行', symbol: 'sz000001', price: 12, change: 0, changePercent: 0 },
      kLines: [],
      minuteLines: [],
      messages: []
    }
    await wrapper.vm.$nextTick()
    await flushPromises()
    await flushPromises()
    wrapper.vm.selectedChatSession = 'sz000001-1'
    await wrapper.vm.$nextTick()
    await flushPromises()

    chatWindow.vm.chatMessages = [{ role: 'assistant', content: 'B', timestamp: '2026-01-29T00:00:00Z' }]
    await wrapper.vm.$nextTick()
    messagesBySession[wrapper.vm.selectedChatSession] = chatWindow.vm.chatMessages

    wrapper.vm.detail = {
      quote: { name: '深科技', symbol: 'sz000021', price: 31.1, change: 0, changePercent: 0 },
      kLines: [],
      minuteLines: [],
      messages: []
    }
    await wrapper.vm.$nextTick()
    await flushPromises()
    expect(chatWindow.vm.chatMessages[0].content).toBe('A')
  })

  it('allows creating a new chat for current stock', async () => {
    const { fetchMock } = createChatFetchMock()

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(StockInfoTab)
    wrapper.vm.detail = {
      quote: { name: '深科技', symbol: 'sz000021', price: 31.1, change: 0, changePercent: 0 },
      kLines: [],
      minuteLines: [],
      messages: []
    }
    await wrapper.vm.$nextTick()
    const chatWindow = wrapper.findComponent({ name: 'ChatWindow' })
    chatWindow.vm.chatMessages = [{ role: 'assistant', content: '旧记录', timestamp: '2026-01-29T00:00:00Z' }]
    await wrapper.vm.$nextTick()

    await wrapper.find('.chat-session-new').trigger('click')
    await wrapper.vm.$nextTick()

    expect(chatWindow.vm.chatMessages.length).toBe(0)
  })

  it('renders markdown in chat content', async () => {
    const { fetchMock } = createChatFetchMock()

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(StockInfoTab)
    wrapper.vm.detail = {
      quote: { name: '深科技', symbol: 'sz000021', price: 31.1, change: 0, changePercent: 0 },
      kLines: [],
      minuteLines: [],
      messages: []
    }
    await wrapper.vm.$nextTick()
    const chatWindow = wrapper.findComponent({ name: 'ChatWindow' })
    chatWindow.vm.chatMessages = [
      {
        role: 'assistant',
        content: '**加粗**\n\n- 列表项',
        timestamp: '2026-01-29T00:00:00Z'
      }
    ]

    await wrapper.vm.$nextTick()

    const html = wrapper.find('.chat-content').html()
    expect(html).toContain('<strong>加粗</strong>')
    expect(html).toContain('<li>列表项</li>')
  })

  it('allows switching between chat sessions for same stock', async () => {
    const { fetchMock, messagesBySession } = createChatFetchMock()

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(StockInfoTab)
    wrapper.vm.detail = {
      quote: { name: '深科技', symbol: 'sz000021', price: 31.1, change: 0, changePercent: 0 },
      kLines: [],
      minuteLines: [],
      messages: []
    }
    await wrapper.vm.$nextTick()

    const chatWindow = wrapper.findComponent({ name: 'ChatWindow' })
    chatWindow.vm.chatMessages = [{ role: 'assistant', content: '历史A', timestamp: '2026-01-29T00:00:00Z' }]
    await wrapper.vm.$nextTick()
    await wrapper.vm.$nextTick()
    messagesBySession[wrapper.vm.selectedChatSession] = chatWindow.vm.chatMessages

    const oldSessionKey = wrapper.vm.selectedChatSession

    await wrapper.find('.chat-session-new').trigger('click')
    await wrapper.vm.$nextTick()

    chatWindow.vm.chatMessages = [{ role: 'assistant', content: '历史B', timestamp: '2026-01-29T00:00:00Z' }]
    await wrapper.vm.$nextTick()
    await wrapper.vm.$nextTick()
    messagesBySession[wrapper.vm.selectedChatSession] = chatWindow.vm.chatMessages

    const selector = wrapper.find('.chat-session select')
    const options = selector.findAll('option')
    expect(options.length).toBeGreaterThan(1)

    await selector.setValue(oldSessionKey)
    await wrapper.vm.$nextTick()
    await wrapper.vm.$nextTick()

    const restored = chatWindow.vm.chatMessages.find(item => item.role === 'assistant')
    expect(restored?.content).toBe('历史A')
  })

  it('renders news impact summary when data is available', async () => {
    const { fetchMock } = createChatFetchMock({
      handle: async (url) => {
        if (url.startsWith('/api/stocks/news/impact')) {
          return makeResponse({
            ok: true,
            status: 200,
            json: async () => ({
              summary: { positive: 2, neutral: 1, negative: 0, overall: '利好偏多' },
              events: [{ title: '公司宣布回购', category: '利好', impactScore: 60 }]
            })
          })
        }
        return null
      }
    })

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(StockInfoTab)
    wrapper.vm.detail = {
      quote: { name: '深科技', symbol: 'sz000021', price: 31.1, change: 0, changePercent: 0 },
      kLines: [],
      minuteLines: [],
      messages: []
    }
    await wrapper.vm.$nextTick()
    await flushPromises()
    await flushPromises()

    expect(wrapper.text()).toContain('资讯影响')
    expect(wrapper.text()).toContain('利好偏多')
  })
})
