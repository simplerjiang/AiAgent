import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import AdminLlmSettings from './AdminLlmSettings.vue'

const makeResponse = ({ ok, status, json, text }) => ({
  ok,
  status,
  json: json || (async () => ({})),
  text: text || (async () => '')
})

const activeProviderResponse = () => makeResponse({
  ok: true,
  status: 200,
  json: async () => ({
    activeProviderKey: 'default',
    providerKeys: ['default', 'gemini_official']
  })
})

const savedSettingsResponse = () => ({
  apiKeyMasked: '****',
  hasApiKey: true,
  tavilyApiKeyMasked: 'tv****',
  hasTavilyApiKey: true
})

describe('AdminLlmSettings', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.restoreAllMocks()
  })

  it('logs out when save returns unauthorized', async () => {
    localStorage.setItem('admin_token', 'token')

    const fetchMock = vi.fn(async (url, options) => {
      if (url.includes('/api/admin/llm/settings/active')) {
        return activeProviderResponse()
      }
      if (options?.method === 'PUT') {
        return makeResponse({ ok: false, status: 401 })
      }
      return makeResponse({ ok: false, status: 404 })
    })

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(AdminLlmSettings)
    await flushPromises()

    const saveButton = wrapper.findAll('button').find(button => button.text().includes('保存设置'))
    expect(saveButton).toBeTruthy()
    await saveButton.trigger('click')
    await flushPromises()

    expect(localStorage.getItem('admin_token')).toBeNull()
    expect(wrapper.text()).toContain('管理员登录')
    expect(wrapper.text()).toContain('登录已过期')
  })

  it('includes system prompt when saving', async () => {
    localStorage.setItem('admin_token', 'token')

    const fetchMock = vi.fn(async (url, options) => {
      if (url.includes('/api/admin/llm/settings/active')) {
        return activeProviderResponse()
      }
      if (options?.method === 'PUT') {
        return makeResponse({ ok: true, status: 200, json: async () => savedSettingsResponse() })
      }
      return makeResponse({ ok: false, status: 404 })
    })

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(AdminLlmSettings)
    await flushPromises()

    await wrapper.find('textarea').setValue('你是股票助手')

    const saveButton = wrapper.findAll('button').find(button => button.text().includes('保存设置'))
    await saveButton.trigger('click')
    await flushPromises()

    const call = fetchMock.mock.calls.find(args => args[0].includes('/api/admin/llm/settings') && args[1]?.method === 'PUT')
    expect(call).toBeTruthy()
    expect(JSON.parse(call[1].body).systemPrompt).toBe('你是股票助手')
  })

  it('includes forceChinese when saving', async () => {
    localStorage.setItem('admin_token', 'token')

    const fetchMock = vi.fn(async (url, options) => {
      if (url.includes('/api/admin/llm/settings/active')) {
        return activeProviderResponse()
      }
      if (options?.method === 'PUT') {
        return makeResponse({ ok: true, status: 200, json: async () => savedSettingsResponse() })
      }
      return makeResponse({ ok: false, status: 404 })
    })

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(AdminLlmSettings)
    await flushPromises()

    const checkbox = wrapper.find('input[type="checkbox"]')
    await checkbox.setValue(true)

    const saveButton = wrapper.findAll('button').find(button => button.text().includes('保存设置'))
    await saveButton.trigger('click')
    await flushPromises()

    const call = fetchMock.mock.calls.find(args => args[0].includes('/api/admin/llm/settings') && args[1]?.method === 'PUT')
    expect(JSON.parse(call[1].body).forceChinese).toBe(true)
  })

  it('emits settings-saved after save succeeds', async () => {
    localStorage.setItem('admin_token', 'token')

    const fetchMock = vi.fn(async (url, options) => {
      if (url.includes('/api/admin/llm/settings/active')) {
        return activeProviderResponse()
      }
      if (options?.method === 'PUT') {
        return makeResponse({ ok: true, status: 200, json: async () => savedSettingsResponse() })
      }
      return makeResponse({ ok: false, status: 404 })
    })

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(AdminLlmSettings)
    await flushPromises()

    const saveButton = wrapper.findAll('button').find(button => button.text().includes('保存设置'))
    await saveButton.trigger('click')
    await flushPromises()

    expect(wrapper.emitted('settings-saved')).toBeTruthy()
    expect(wrapper.emitted('settings-saved')[0][0]).toEqual(savedSettingsResponse())
  })

  it('includes Tavily API key when saving', async () => {
    localStorage.setItem('admin_token', 'token')

    const fetchMock = vi.fn(async (url, options) => {
      if (url.includes('/api/admin/llm/settings/active')) {
        return activeProviderResponse()
      }
      if (options?.method === 'PUT') {
        return makeResponse({ ok: true, status: 200, json: async () => savedSettingsResponse() })
      }
      return makeResponse({ ok: false, status: 404 })
    })

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(AdminLlmSettings)
    await flushPromises()

    const tavilyInput = wrapper.findAll('input').find(input => input.attributes('placeholder')?.includes('Tavily Key'))
    expect(tavilyInput).toBeTruthy()
    await tavilyInput.setValue('tv-new-key')

    const saveButton = wrapper.findAll('button').find(button => button.text().includes('保存设置'))
    await saveButton.trigger('click')
    await flushPromises()

    const call = fetchMock.mock.calls.find(args => args[0].includes('/api/admin/llm/settings') && args[1]?.method === 'PUT')
    expect(JSON.parse(call[1].body).tavilyApiKey).toBe('tv-new-key')
    expect(wrapper.text()).toContain('当前已保存 Tavily Key：tv****')
  })

  it('switches active provider through admin endpoint', async () => {
    localStorage.setItem('admin_token', 'token')

    const fetchMock = vi.fn(async (url, options) => {
      if (url.includes('/api/admin/llm/settings/active') && options?.method === 'PUT') {
        return makeResponse({ ok: true, status: 200, json: async () => ({ activeProviderKey: 'gemini_official', providerKeys: ['default', 'gemini_official'] }) })
      }
      if (url.includes('/api/admin/llm/settings/active')) {
        return activeProviderResponse()
      }
      return makeResponse({ ok: false, status: 404 })
    })

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(AdminLlmSettings)
    await flushPromises()

    const selects = wrapper.findAll('select')
    await selects[0].setValue('gemini_official')

    const switchButton = wrapper.findAll('button').find(button => button.text().includes('切换激活通道'))
    await switchButton.trigger('click')
    await flushPromises()

    const call = fetchMock.mock.calls.find(args => args[0].includes('/api/admin/llm/settings/active') && args[1]?.method === 'PUT')
    expect(call).toBeTruthy()
    expect(JSON.parse(call[1].body)).toEqual({ activeProviderKey: 'gemini_official' })
    expect(wrapper.text()).toContain('激活通道已切换')
  })

  it('loads Tavily API key metadata and renders masked state', async () => {
    localStorage.setItem('admin_token', 'token')

    const fetchMock = vi.fn(async (url, options) => {
      if (url.includes('/api/admin/llm/settings/active')) {
        return activeProviderResponse()
      }
      if (!options?.method && url.includes('/api/admin/llm/settings/default')) {
        return makeResponse({
          ok: true,
          status: 200,
          json: async () => ({
            baseUrl: 'https://api.bltcy.ai',
            model: 'gemini-3.1-flash-lite-preview-thinking-high',
            enabled: true,
            apiKeyMasked: '****',
            hasApiKey: true,
            tavilyApiKeyMasked: 'tv****',
            hasTavilyApiKey: true
          })
        })
      }
      return makeResponse({ ok: false, status: 404 })
    })

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(AdminLlmSettings)
    await flushPromises()

    expect(wrapper.text()).toContain('Tavily API Key（外部搜索）')
    expect(wrapper.text()).toContain('当前已保存 Tavily Key：tv****')
  })
})
