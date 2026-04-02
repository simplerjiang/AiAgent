import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import RecommendFeed from './RecommendFeed.vue'

describe('RecommendFeed', () => {
  it('groups feed items by turn and merges live role summaries into the active turn', () => {
    const wrapper = mount(RecommendFeed, {
      props: {
        session: {
          activeTurnId: 12,
          turns: [
            {
              id: 11,
              turnIndex: 0,
              userPrompt: '先看一下今天的市场主线',
              requestedAt: '2026-04-01T09:00:00Z',
              feedItems: [
                {
                  id: 1,
                  itemType: 'StageTransition',
                  eventType: 'StageStarted',
                  stageType: 'MarketScan',
                  summary: '阶段 MarketScan 开始',
                  timestamp: '2026-04-01T09:00:02Z'
                },
                {
                  id: 2,
                  itemType: 'RoleMessage',
                  eventType: 'RoleSummaryReady',
                  roleId: 'recommend_macro_analyst',
                  summary: '{"summary":"宏观偏强"}',
                  timestamp: '2026-04-01T09:00:05Z'
                }
              ]
            },
            {
              id: 12,
              turnIndex: 1,
              userPrompt: '再看看半导体还有没有机会',
              requestedAt: '2026-04-01T09:10:00Z',
              feedItems: []
            }
          ]
        },
        sseEvents: [
          {
            turnId: 12,
            eventType: 'RoleSummaryReady',
            roleId: 'recommend_sector_hunter',
            stageType: 'SectorDebate',
            summary: '{"summary":"半导体共振最强"}',
            timestamp: '2026-04-01T09:10:04Z'
          }
        ],
        isRunning: false
      }
    })

    expect(wrapper.text()).toContain('初始问题')
    expect(wrapper.text()).toContain('先看一下今天的市场主线')
    expect(wrapper.text()).toContain('追问 1')
    expect(wrapper.text()).toContain('再看看半导体还有没有机会')
    expect(wrapper.text()).toContain('市场扫描 开始')
    expect(wrapper.text()).toContain('宏观分析师')
    expect(wrapper.text()).toContain('宏观偏强')
    expect(wrapper.text()).toContain('板块猎手')
    expect(wrapper.text()).toContain('半导体共振最强')
  })

  it('expands tool details and shows typing indicator while running', async () => {
    const wrapper = mount(RecommendFeed, {
      props: {
        session: {
          activeTurnId: 21,
          turns: [
            {
              id: 21,
              turnIndex: 0,
              userPrompt: '找一下市场热点',
              requestedAt: '2026-04-01T10:00:00Z',
              feedItems: [
                {
                  id: 3,
                  itemType: 'ToolEvent',
                  eventType: 'ToolCompleted',
                  roleId: 'recommend_macro_analyst',
                  summary: '角色 recommend_macro_analyst 调用工具: web_search',
                  detailJson: JSON.stringify({
                    toolName: 'web_search',
                    status: 'Completed',
                    args: { query: 'A股 热点 板块' },
                    resultPreview: '{"summary":"政策催化明显"}'
                  }),
                  timestamp: '2026-04-01T10:00:05Z'
                }
              ]
            }
          ]
        },
        sseEvents: [],
        isRunning: true
      }
    })

    expect(wrapper.text()).toContain('团队分析中')
    expect(wrapper.find('.feed-tool-detail').exists()).toBe(false)

    await wrapper.find('.feed-tool').trigger('click')

    expect(wrapper.text()).toContain('网页搜索')
    expect(wrapper.text()).toContain('返回数据')
    expect(wrapper.text()).toContain('政策催化明显')
  })
})