/**
 * @vitest-environment jsdom
 * @vitest-environment-options {"url":"http://localhost/"}
 */
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import EmbeddingDegradedBanner from './EmbeddingDegradedBanner.vue'

describe('EmbeddingDegradedBanner', () => {
  beforeEach(() => {
    sessionStorage.clear()
  })

  it('renders ollama-unavailable cause with no backfill button', () => {
    const wrapper = mount(EmbeddingDegradedBanner, {
      props: {
        status: {
          available: false,
          model: 'bge-m3',
          dimension: null,
          embeddingCount: 0,
          chunkCount: 1456,
          coverage: 0
        }
      }
    })

    expect(wrapper.text()).toContain('向量模型未就绪')
    expect(wrapper.text()).toContain('Ollama 未运行或 bge-m3 模型未安装')
    expect(wrapper.text()).toContain('bge-m3')
    expect(wrapper.text()).toContain('0 / 1456 chunks (0.0%)')
    expect(wrapper.find('.embedding-degraded-banner__backfill').exists()).toBe(false)
  })

  it('renders zero-embedding cause with backfill button', () => {
    const wrapper = mount(EmbeddingDegradedBanner, {
      props: {
        status: {
          available: true,
          model: 'bge-m3',
          dimension: 1024,
          embeddingCount: 0,
          chunkCount: 100,
          coverage: 0
        }
      }
    })

    expect(wrapper.text()).toContain('尚无向量数据')
    expect(wrapper.text()).toContain('点击补建开始构建检索索引')
    const backfillBtn = wrapper.find('.embedding-degraded-banner__backfill')
    expect(backfillBtn.exists()).toBe(true)
    expect(backfillBtn.text()).toBe('开始补建')
  })

  it('renders low-coverage cause with backfill button', () => {
    const wrapper = mount(EmbeddingDegradedBanner, {
      props: {
        status: {
          available: true,
          model: 'bge-m3',
          dimension: 1024,
          embeddingCount: 30,
          chunkCount: 100,
          coverage: 0.3
        }
      }
    })

    expect(wrapper.text()).toContain('向量覆盖不足')
    expect(wrapper.text()).toContain('30.0%')
    const backfillBtn = wrapper.find('.embedding-degraded-banner__backfill')
    expect(backfillBtn.exists()).toBe(true)
    expect(backfillBtn.text()).toBe('补建向量')
  })

  it('does not render when embedding is available with full coverage', () => {
    const wrapper = mount(EmbeddingDegradedBanner, {
      props: {
        status: {
          available: true,
          model: 'bge-m3',
          dimension: 1024,
          embeddingCount: 1456,
          chunkCount: 1456,
          coverage: 1
        }
      }
    })

    expect(wrapper.html()).toBe('<!--v-if-->')
  })

  it('emits refresh when clicking refresh button', async () => {
    const wrapper = mount(EmbeddingDegradedBanner, {
      props: {
        status: {
          available: false,
          model: 'bge-m3',
          embeddingCount: 0,
          chunkCount: 10,
          coverage: 0
        }
      }
    })

    await wrapper.find('.embedding-degraded-banner__refresh').trigger('click')
    expect(wrapper.emitted('refresh')).toHaveLength(1)
  })

  it('calls backfill endpoint and shows success message', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true }))
    const wrapper = mount(EmbeddingDegradedBanner, {
      props: {
        status: {
          available: true,
          model: 'bge-m3',
          dimension: 1024,
          embeddingCount: 0,
          chunkCount: 100,
          coverage: 0
        }
      }
    })

    await wrapper.find('.embedding-degraded-banner__backfill').trigger('click')
    await vi.dynamicImportSettled()

    expect(fetch).toHaveBeenCalledWith('/api/embedding/backfill', { method: 'POST' })
    expect(wrapper.text()).toContain('补建任务已启动')
    vi.unstubAllGlobals()
  })

  it('shows error message on backfill failure', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false }))
    const wrapper = mount(EmbeddingDegradedBanner, {
      props: {
        status: {
          available: true,
          model: 'bge-m3',
          dimension: 1024,
          embeddingCount: 10,
          chunkCount: 100,
          coverage: 0.1
        }
      }
    })

    await wrapper.find('.embedding-degraded-banner__backfill').trigger('click')
    await vi.dynamicImportSettled()

    expect(wrapper.text()).toContain('启动失败')
    vi.unstubAllGlobals()
  })
})