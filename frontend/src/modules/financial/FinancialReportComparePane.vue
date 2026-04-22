<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import FinancialPdfViewer from './FinancialPdfViewer.vue'
import FinancialPdfParsePreview from './FinancialPdfParsePreview.vue'
import FinancialPdfVotingPanel from './FinancialPdfVotingPanel.vue'
import {
  buildPdfFileContentUrl,
  fetchPdfFileDetail,
  reparsePdfFile
} from './financialApi.js'

/**
 * V041-S5: FinancialReportComparePane
 *
 * 左 PDF 原件 / 右（解析单元 | 投票信息）双栏对照面板。
 *
 * Props:
 *   pdfFileId        PDF 文件 ID（必填）
 *   pdfFileDetail    可选，外部已加载的 detail；缺省时内部调 fetchPdfFileDetail
 *   loading / error  外部状态透传（与外部 detail 配套使用）
 *
 * Emits:
 *   refresh(detail)  reparse 成功后通知父级
 *   close            关闭按钮（保留以便父级控制）
 *
 * 文案约定（避免与抽屉「重新采集报告」混淆）:
 *   本面板仅有「重新解析 PDF」按钮（位于 VotingPanel 内），不调用 recollectFinancialReport。
 */

const props = defineProps({
  pdfFileId: { type: [String, Number], required: true },
  pdfFileDetail: { type: Object, default: null },
  loading: { type: Boolean, default: false },
  error: { type: String, default: null }
})

const emit = defineEmits(['refresh', 'close'])

const internalDetail = ref(null)
const internalLoading = ref(false)
const internalError = ref(null)
const reparsing = ref(false)
const rightPane = ref('parse')
const viewerJumpPage = ref(null)

let fetchToken = 0

// 优先使用外部 detail；外部未提供时使用内部加载结果
const effectiveDetail = computed(() => props.pdfFileDetail || internalDetail.value)
const effectiveLoading = computed(() => props.loading || internalLoading.value)
const effectiveError = computed(() => props.error || internalError.value)

const parseUnits = computed(() => {
  const detail = effectiveDetail.value
  if (!detail) return []
  return Array.isArray(detail.parseUnits) ? detail.parseUnits : []
})

const headerTitle = computed(() => {
  const d = effectiveDetail.value
  if (!d) return 'PDF 报告对照'
  const parts = []
  if (d.fileName) parts.push(d.fileName)
  if (d.reportPeriod) parts.push(d.reportPeriod)
  return parts.length ? parts.join(' · ') : 'PDF 报告对照'
})

const pdfSrc = computed(() => {
  if (props.pdfFileId === null || props.pdfFileId === undefined || props.pdfFileId === '') return ''
  return buildPdfFileContentUrl(props.pdfFileId)
})

async function loadDetailIfNeeded() {
  // 外部已传 detail 则不重复请求
  if (props.pdfFileDetail) return
  if (props.pdfFileId === null || props.pdfFileId === undefined || props.pdfFileId === '') {
    internalDetail.value = null
    return
  }
  const token = ++fetchToken
  internalLoading.value = true
  internalError.value = null
  try {
    const data = await fetchPdfFileDetail(props.pdfFileId)
    if (token !== fetchToken) return
    internalDetail.value = data
  } catch (e) {
    if (token !== fetchToken) return
    internalError.value = e?.message || '加载 PDF 详情失败'
    internalDetail.value = null
  } finally {
    if (token === fetchToken) {
      internalLoading.value = false
    }
  }
}

async function handleReparse() {
  const id = props.pdfFileId
  if (!id || reparsing.value) return
  reparsing.value = true
  internalError.value = null
  try {
    const result = await reparsePdfFile(id)
    if (result && result.success === false) {
      internalError.value = result.error || '重新解析失败'
      return
    }
    if (result && result.detail) {
      // 整体替换，确保 parseUnits 三字段（pageStart/pageEnd/blockKind）同步刷新
      internalDetail.value = result.detail
      emit('refresh', result.detail)
    } else {
      // 后端返回 success 但无 detail 时，退化为重新拉详情
      await loadDetailIfNeeded()
      emit('refresh', internalDetail.value)
    }
  } catch (e) {
    internalError.value = e?.message || '重新解析失败'
  } finally {
    reparsing.value = false
  }
}

function onJumpToPage(page) {
  const n = Number(page)
  if (!Number.isFinite(n) || n <= 0) return
  viewerJumpPage.value = Math.floor(n)
}

function switchTab(name) {
  if (name === 'parse' || name === 'voting') {
    rightPane.value = name
  }
}

watch(
  () => props.pdfFileId,
  () => {
    viewerJumpPage.value = null
    internalDetail.value = null
    loadDetailIfNeeded()
  }
)

watch(
  () => props.pdfFileDetail,
  (next) => {
    // 外部传入新的 detail 时清掉内部缓存的旧值
    if (next) {
      internalDetail.value = null
      internalError.value = null
    }
  }
)

onMounted(() => {
  loadDetailIfNeeded()
})
</script>

<template>
  <section class="fc-compare-pane" data-testid="fc-compare-pane">
    <header class="fc-compare-header">
      <div class="fc-compare-title-wrap">
        <h3 class="fc-compare-title" data-testid="fc-compare-title">{{ headerTitle }}</h3>
        <p class="fc-compare-subtitle">左侧为 PDF 原件，右侧为结构化解析；「重新解析 PDF」按钮位于右侧投票信息面板内。</p>
      </div>
      <button
        v-if="$attrs.onClose !== undefined"
        type="button"
        class="fc-compare-close"
        data-testid="fc-compare-close"
        @click="emit('close')"
      >关闭</button>
    </header>

    <div v-if="effectiveError" class="fc-compare-banner-error" role="alert" data-testid="fc-compare-error">
      {{ effectiveError }}
    </div>

    <div class="fc-compare-grid">
      <div class="fc-compare-left" data-testid="fc-compare-left">
        <FinancialPdfViewer
          :src="pdfSrc"
          :page="viewerJumpPage"
          :title="headerTitle"
        />
      </div>

      <div class="fc-compare-right" data-testid="fc-compare-right">
        <div class="fc-compare-tabs" role="tablist">
          <button
            type="button"
            role="tab"
            class="fc-compare-tab"
            :class="{ 'fc-compare-tab--active': rightPane === 'parse' }"
            :aria-selected="rightPane === 'parse'"
            data-testid="fc-compare-tab-parse"
            @click="switchTab('parse')"
          >解析单元</button>
          <button
            type="button"
            role="tab"
            class="fc-compare-tab"
            :class="{ 'fc-compare-tab--active': rightPane === 'voting' }"
            :aria-selected="rightPane === 'voting'"
            data-testid="fc-compare-tab-voting"
            @click="switchTab('voting')"
          >投票信息</button>
        </div>

        <div class="fc-compare-pane-body">
          <div v-show="rightPane === 'parse'" data-testid="fc-compare-parse-wrap">
            <FinancialPdfParsePreview
              :parse-units="parseUnits"
              :loading="effectiveLoading"
              :error="effectiveError"
              @jump-to-page="onJumpToPage"
            />
          </div>

          <div v-show="rightPane === 'voting'" data-testid="fc-compare-voting-wrap">
            <FinancialPdfVotingPanel
              :detail="effectiveDetail"
              :reparsing="reparsing"
              @reparse="handleReparse"
            />
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.fc-compare-pane {
  display: flex;
  flex-direction: column;
  width: 100%;
  height: 100%;
  min-height: 480px;
  gap: 12px;
}

.fc-compare-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 12px;
}

.fc-compare-title-wrap {
  flex: 1;
  min-width: 0;
}

.fc-compare-title {
  margin: 0;
  font-size: 16px;
  font-weight: 700;
  color: var(--color-text-primary, #111827);
  word-break: break-all;
}

.fc-compare-subtitle {
  margin: 4px 0 0;
  font-size: 12px;
  color: var(--color-text-secondary, #6b7280);
}

.fc-compare-close {
  flex-shrink: 0;
  padding: 4px 12px;
  border: 1px solid var(--color-border, #e4e7eb);
  background: var(--color-bg-elevated, #fff);
  border-radius: 4px;
  cursor: pointer;
  font-size: 12px;
}

.fc-compare-banner-error {
  padding: 8px 12px;
  border-radius: 4px;
  background: #fef2f2;
  border: 1px solid #fecaca;
  color: #b91c1c;
  font-size: 13px;
}

.fc-compare-grid {
  flex: 1;
  display: grid;
  grid-template-columns: 60% 40%;
  gap: 12px;
  min-height: 0;
}

@media (max-width: 768px) {
  .fc-compare-grid {
    grid-template-columns: 1fr;
    grid-auto-rows: minmax(360px, auto);
  }
}

.fc-compare-left {
  min-height: 360px;
  display: flex;
  flex-direction: column;
}

.fc-compare-right {
  display: flex;
  flex-direction: column;
  border: 1px solid var(--color-border, #e4e7eb);
  border-radius: 6px;
  overflow: hidden;
  background: var(--color-bg-elevated, #fff);
  min-height: 360px;
}

.fc-compare-tabs {
  display: flex;
  border-bottom: 1px solid var(--color-border, #e4e7eb);
  background: var(--color-bg-surface-alt, #f9fafb);
}

.fc-compare-tab {
  flex: 1;
  padding: 10px 12px;
  border: 0;
  background: transparent;
  cursor: pointer;
  font-size: 13px;
  color: var(--color-text-secondary, #6b7280);
  border-bottom: 2px solid transparent;
}

.fc-compare-tab--active {
  color: var(--color-accent-text, #2563eb);
  border-bottom-color: var(--color-accent-text, #2563eb);
  background: var(--color-bg-elevated, #fff);
  font-weight: 600;
}

.fc-compare-pane-body {
  flex: 1;
  overflow: auto;
  padding: 12px;
}
</style>
