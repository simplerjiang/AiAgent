import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import AdminLlmSettings from './AdminLlmSettings.vue'

const makeResponse = ({ ok, status, json, text }) => ({
  ok,
  status,
  json: json || (async () => ({})),
  text: text || (async () => '')
})

describe('AdminLlmSettings', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.restoreAllMocks()
  })

  it('logs out when save returns unauthorized', async () => {
    localStorage.setItem('admin_token', 'token')

    const fetchMock = vi.fn(async (url, options) => {
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
      if (options?.method === 'PUT') {
        return makeResponse({ ok: true, status: 200, json: async () => ({ apiKeyMasked: '****', hasApiKey: true }) })
      }
      return makeResponse({ ok: false, status: 404 })
    })

    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mount(AdminLlmSettings)
    await flushPromises()

    const textarea = wrapper.find('textarea')
    await textarea.setValue('你是股票助手')

    const saveButton = wrapper.findAll('button').find(button => button.text().includes('保存设置'))
    await saveButton.trigger('click')
    await flushPromises()

    const call = fetchMock.mock.calls.find(args => args[0].includes('/api/admin/llm/settings') && args[1]?.method === 'PUT')
    expect(call).toBeTruthy()
    const body = JSON.parse(call[1].body)
    expect(body.systemPrompt).toBe('你是股票助手')
  })

  it('includes forceChinese when saving', async () => {
    localStorage.setItem('admin_token', 'token')

    const fetchMock = vi.fn(async (url, options) => {
      if (options?.method === 'PUT') {
        return makeResponse({ ok: true, status: 200, json: async () => ({ apiKeyMasked: '****', hasApiKey: true }) })
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
    const body = JSON.parse(call[1].body)
    expect(body.forceChinese).toBe(true)
  })
})
