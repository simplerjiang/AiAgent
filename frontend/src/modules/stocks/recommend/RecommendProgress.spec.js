import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import RecommendProgress from './RecommendProgress.vue'

const STAGE_TYPES = ['MarketScan', 'SectorDebate', 'StockPicking', 'StockDebate', 'FinalDecision']
const ALL_PENDING = new Array(STAGE_TYPES.length).fill('待执行')

const createTurn = ({
  id,
  turnIndex = 0,
  status = 'Completed',
  stageSnapshots = [],
  feedItems = []
} = {}) => ({
  id,
  turnIndex,
  status,
  userPrompt: `turn-${id}`,
  requestedAt: '2026-04-01T10:00:00Z',
  startedAt: '2026-04-01T10:00:05Z',
  stageSnapshots,
  feedItems
})

const completedStageSnapshots = STAGE_TYPES.map(stageType => ({
  stageType,
  status: 'Completed',
  roleStates: []
}))

describe('RecommendProgress', () => {
  it('keeps a fresh startup turn pending instead of borrowing older snapshots', () => {
    const wrapper = mount(RecommendProgress, {
      props: {
        isRunning: true,
        session: {
          status: 'Running',
          turns: [
            createTurn({ id: 801, turnIndex: 0, status: 'Completed', stageSnapshots: completedStageSnapshots }),
            createTurn({ id: 802, turnIndex: 1, status: null, stageSnapshots: [] })
          ],
          feedItems: []
        }
      }
    })

    expect(wrapper.findAll('.stage-status').map(node => node.text())).toEqual(ALL_PENDING)
  })

  it('ignores unscoped session feed items while a fresh live turn is waiting for snapshots', () => {
    const wrapper = mount(RecommendProgress, {
      props: {
        isRunning: true,
        session: {
          status: 'Running',
          activeTurnId: 902,
          turns: [
            createTurn({ id: 901, turnIndex: 0, status: 'Completed', stageSnapshots: completedStageSnapshots }),
            createTurn({ id: 902, turnIndex: 1, status: 'Queued', stageSnapshots: [] })
          ],
          feedItems: STAGE_TYPES.map((stageType, index) => ({
            id: index + 1,
            eventType: 'StageCompleted',
            stageType
          }))
        }
      }
    })

    expect(wrapper.findAll('.stage-status').map(node => node.text())).toEqual(ALL_PENDING)
  })
})