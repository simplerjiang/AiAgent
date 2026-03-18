import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import StockAgentPanels from './StockAgentPanels.vue'

describe('StockAgentPanels', () => {
  it('marks stale evidence publishedAt with expired risk tag', () => {
    const staleTime = '2024-01-01 09:30'
    const wrapper = mount(StockAgentPanels, {
      props: {
        agents: [
          {
            agentId: 'stock_news',
            agentName: '个股资讯Agent',
            success: true,
            data: {
              summary: 'test',
              evidence: [
                {
                  point: '旧消息',
                  source: '测试源',
                  publishedAt: staleTime,
                  url: null
                }
              ],
              signals: [],
              risks: [],
              triggers: [],
              invalidations: [],
              riskLimits: []
            }
          }
        ]
      }
    })

    const expiredBadge = wrapper.find('.evidence-time.expired')
    expect(expiredBadge.exists()).toBe(true)
    expect(expiredBadge.text()).toContain('超72h')
  })

  it('renders traceable evidence link and read-state fields from new contract', () => {
    const wrapper = mount(StockAgentPanels, {
      props: {
        agents: [
          {
            agentId: 'stock_news',
            agentName: '个股资讯Agent',
            success: true,
            data: {
              summary: 'test',
              evidence: [
                {
                  title: '年报披露超预期',
                  source: '东方财富公告',
                  publishedAt: '2026-03-17T09:30:00Z',
                  url: 'https://example.com/report',
                  excerpt: '公司披露年报，利润增长超市场预期。',
                  readStatus: 'summary_only',
                  readMode: 'url_fetched',
                  localFactId: 11,
                  sourceRecordId: 'news-7'
                }
              ],
              signals: [],
              risks: [],
              triggers: [],
              invalidations: [],
              riskLimits: []
            }
          }
        ]
      }
    })

    const evidenceLink = wrapper.find('.evidence-link')
    expect(evidenceLink.exists()).toBe(true)
    expect(evidenceLink.attributes('href')).toBe('https://example.com/report')
    expect(wrapper.text()).toContain('摘要阅读')
    expect(wrapper.text()).toContain('链接抓取')
    expect(wrapper.text()).toContain('公司披露年报，利润增长超市场预期。')
    expect(wrapper.text()).toContain('事实#11')
    expect(wrapper.text()).toContain('源#news-7')
  })

  it('reads evidence fields from saved history casing', () => {
    const wrapper = mount(StockAgentPanels, {
      props: {
        agents: [
          {
            agentId: 'sector_news',
            agentName: '板块资讯Agent',
            success: true,
            data: {
              summary: 'test',
              Evidence: [
                {
                  Title: '政策催化加强',
                  Source: '行业快讯',
                  PublishedAt: '2026-03-17T10:00:00Z',
                  ReadStatus: 'full_text_read',
                  ReadMode: 'local_fact',
                  Point: '政策密集落地，板块预期升温。'
                }
              ],
              signals: [],
              risks: []
            }
          }
        ]
      }
    })

    expect(wrapper.text()).toContain('政策催化加强')
    expect(wrapper.text()).toContain('全文已读')
    expect(wrapper.text()).toContain('本地事实')
    expect(wrapper.text()).toContain('政策密集落地，板块预期升温。')
  })

  it('emits standard and pro run flags from action buttons', async () => {
    const wrapper = mount(StockAgentPanels)

    await wrapper.find('.run-standard-button').trigger('click')
    await wrapper.find('.run-pro-button').trigger('click')

    expect(wrapper.emitted('run')).toEqual([[false], [true]])
  })

  it('renders commander opinion schema fields', () => {
    const wrapper = mount(StockAgentPanels, {
      props: {
        agents: [
          {
            agentId: 'commander',
            agentName: '指挥Agent',
            success: true,
            data: {
              summary: '偏谨慎',
              analysis_opinion: '当前更适合观察，等待放量突破。',
              confidence_score: 72,
              trigger_conditions: '放量突破 12.60',
              invalid_conditions: '跌破 11.90',
              risk_warning: '单笔亏损控制在 2% 以内',
              evidence: [],
              signals: [],
              risks: []
            }
          }
        ]
      }
    })

    expect(wrapper.text()).toContain('分析结论')
    expect(wrapper.text()).toContain('放量突破 12.60')
    expect(wrapper.text()).toContain('跌破 11.90')
  })

  it('emits draft-plan from commander card', async () => {
    const wrapper = mount(StockAgentPanels, {
      props: {
        agents: [
          {
            agentId: 'commander',
            agentName: '指挥Agent',
            success: true,
            data: {
              summary: '偏多',
              analysis_opinion: '等待确认',
              triggers: [],
              invalidations: [],
              riskLimits: []
            }
          }
        ]
      }
    })

    await wrapper.find('.draft-plan-button').trigger('click')

    expect(wrapper.emitted('draft-plan')).toEqual([[]])
  })

  it('renders nested probability and scoring fields as metrics', () => {
    const wrapper = mount(StockAgentPanels, {
      props: {
        agents: [
          {
            agentId: 'sector_news',
            agentName: '板块资讯Agent',
            success: true,
            data: {
              summary: '偏多',
              probability_analysis: {
                up_probability: 65,
                down_probability: 35
              },
              entryScore: 85,
              valuationScore: 60,
              evidence: [
                {
                  point: '板块升温',
                  source: '测试源',
                  publishedAt: '2026-03-17 10:00'
                }
              ]
            }
          }
        ]
      }
    })

    expect(wrapper.text()).toContain('上涨概率')
    expect(wrapper.text()).toContain('65%')
    expect(wrapper.text()).toContain('入场评分')
    expect(wrapper.text()).toContain('85')
  })

  it('renders commander recommendation metrics from saved history shape', () => {
    const wrapper = mount(StockAgentPanels, {
      props: {
        agents: [
          {
            agentId: 'commander',
            agentName: '指挥Agent',
            success: true,
            data: {
              summary: '偏谨慎',
              metrics: {
                price: 31.02
              },
              recommendation: {
                entryScore: 65,
                valuationScore: 45,
                positionPercent: 0,
                targetPrice: 33.5,
                takeProfitPrice: 34.6,
                stopLossPrice: 29.4
              },
              evidence: []
            }
          }
        ]
      }
    })

    expect(wrapper.text()).toContain('入场评分')
    expect(wrapper.text()).toContain('65')
    expect(wrapper.text()).toContain('估值评分')
    expect(wrapper.text()).toContain('45')
  })
})
