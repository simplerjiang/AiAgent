import { beforeEach, describe, expect, it, vi } from 'vitest'

import { stockInfoTabTradingPlanCases } from './StockInfoTab.trading-plan.cases'
import { installStockInfoTabCaseSuite } from './stockInfoTabTestUtils'

describe('StockInfoTab trading plan regressions', () => {
	installStockInfoTabCaseSuite({ beforeEach, describe, expect, it, vi }, stockInfoTabTradingPlanCases)
})