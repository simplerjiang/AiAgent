import { describe, expect, it } from 'vitest'
import {
  formatTradingPlanStatus,
  getTradingPlanReviewText,
  getTradingPlanStatusClass,
  normalizeTradingPlanStatus,
  parseTradingPlanAlertMetadata
} from './tradingPlanReview'

describe('tradingPlanReview helpers', () => {
  it('normalizes legacy review statuses', () => {
    expect(normalizeTradingPlanStatus('Draft')).toBe('Pending')
    expect(normalizeTradingPlanStatus('NeedsReview')).toBe('ReviewRequired')
    expect(normalizeTradingPlanStatus('Archived')).toBe('Cancelled')
  })

  it('formats review required status for display', () => {
    expect(formatTradingPlanStatus('ReviewRequired')).toBe('待复核')
    expect(getTradingPlanStatusClass('ReviewRequired')).toBe('plan-status-review-required')
  })

  it('extracts review metadata from alert payload', () => {
    const metadata = parseTradingPlanAlertMetadata('{"localNewsId":18,"reason":"订单流失","confidence":88,"isPlanThreatened":true}')
    expect(metadata).toEqual({
      localNewsId: 18,
      newsTitle: '',
      reason: '订单流失',
      confidence: 88,
      isPlanThreatened: true
    })
    expect(getTradingPlanReviewText({ metadataJson: '{"reason":"订单流失","confidence":88}' })).toBe('订单流失 · 置信度 88')
  })
})