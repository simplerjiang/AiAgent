export const TRADING_PLAN_STATUS_OPTIONS = [
  { value: 'Draft', label: '草稿' },
  { value: 'Pending', label: '观察中' },
  { value: 'Triggered', label: '已触发' },
  { value: 'Invalid', label: '已失效' },
  { value: 'ReviewRequired', label: '待复核' },
  { value: 'Cancelled', label: '已取消' }
]

export const normalizeTradingPlanStatus = value => {
  if (value === 'Archived') {
    return 'Cancelled'
  }
  if (value === 'NeedsReview' || value === 'Review') {
    return 'ReviewRequired'
  }
  return value || 'Pending'
}

export const formatTradingPlanStatus = status => {
  switch (normalizeTradingPlanStatus(status)) {
    case 'Draft':
      return '草稿'
    case 'Pending':
      return '观察中'
    case 'Triggered':
      return '已触发'
    case 'Invalid':
      return '已失效'
    case 'Cancelled':
      return '已取消'
    case 'ReviewRequired':
      return '待复核'
    default:
      return normalizeTradingPlanStatus(status)
  }
}

export const getTradingPlanStatusClass = status => {
  switch (normalizeTradingPlanStatus(status)) {
    case 'Draft':
      return 'plan-status-draft'
    case 'Triggered':
      return 'plan-status-triggered'
    case 'Invalid':
    case 'Cancelled':
      return 'plan-status-invalid'
    case 'ReviewRequired':
      return 'plan-status-review-required'
    default:
      return 'plan-status-pending'
  }
}

export const parseTradingPlanAlertMetadata = metadataJson => {
  if (!metadataJson || typeof metadataJson !== 'string') {
    return null
  }

  try {
    const parsed = JSON.parse(metadataJson)
    return {
      localNewsId: Number.isFinite(Number(parsed?.localNewsId)) ? Number(parsed.localNewsId) : null,
      newsTitle: parsed?.newsTitle || '',
      reason: parsed?.reason || '',
      confidence: Number.isFinite(Number(parsed?.confidence)) ? Number(parsed.confidence) : null,
      isPlanThreatened: parsed?.isPlanThreatened === true
    }
  } catch {
    return null
  }
}

export const getTradingPlanReviewText = alert => {
  const metadata = parseTradingPlanAlertMetadata(alert?.metadataJson)
  if (!metadata?.reason) {
    return ''
  }

  const confidenceText = metadata.confidence == null ? '' : ` · 置信度 ${metadata.confidence}`
  return `${metadata.reason}${confidenceText}`
}