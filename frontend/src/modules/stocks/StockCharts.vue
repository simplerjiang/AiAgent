<script setup>
import { onMounted, onUnmounted, ref, watch, nextTick } from 'vue'
import * as echarts from 'echarts'

const props = defineProps({
  kLines: {
    type: Array,
    default: () => []
  },
  minuteLines: {
    type: Array,
    default: () => []
  },
  basePrice: {
    type: Number,
    default: null
  },
  interval: {
    type: String,
    default: 'day'
  }
})

const emit = defineEmits(['update:interval'])

const klineRef = ref(null)
const minuteRef = ref(null)
const klineWrapRef = ref(null)
const minuteWrapRef = ref(null)
const klineChart = ref(null)
const minuteChart = ref(null)
let resizeObserver = null
let resizeHandler = null
const klineAxis = ref([])
const klineValues = ref([])
const minuteAxis = ref([])
const minuteValues = ref([])
const minuteBasePrice = ref(null)
const klineHover = ref({ visible: false, x: 0, y: 0, lines: [] })
const minuteHover = ref({ visible: false, x: 0, y: 0, lines: [] })

const ensureCharts = () => {
  if (klineRef.value && !klineChart.value) {
    klineChart.value = echarts.init(klineRef.value)
  }
}

const formatMinuteTime = time => {
  if (!time) return ''
  if (typeof time === 'number') {
    const date = new Date(time * 1000)
    const pad = value => String(value).padStart(2, '0')
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`
  }
  if (typeof time === 'string') {
    return time
  }
  if (typeof time === 'object' && time.year && time.month && time.day) {
    const pad = value => String(value).padStart(2, '0')
    return `${time.year}-${pad(time.month)}-${pad(time.day)}`
  }
  return ''
}

const resizeMinuteChart = () => {
  if (!minuteRef.value || !minuteChart.value) return
  const rect = minuteRef.value.getBoundingClientRect()
  minuteChart.value.resize({
    width: Math.max(1, Math.floor(rect.width)),
    height: Math.max(1, Math.floor(rect.height))
  })
}

const ensureMinuteChart = () => {
  if (!minuteRef.value || minuteChart.value) return
  minuteChart.value = echarts.init(minuteRef.value)
}

const handleKlineMove = event => {
  if (!klineWrapRef.value || !klineChart.value) return
  const rect = klineWrapRef.value.getBoundingClientRect()
  const x = event.clientX - rect.left
  const axisValue = klineChart.value.convertFromPixel({ xAxisIndex: 0 }, x)
  let index = -1
  if (typeof axisValue === 'string') {
    index = klineAxis.value.indexOf(axisValue)
  } else if (typeof axisValue === 'number') {
    index = Math.round(axisValue)
  }
  if (index < 0 || index >= klineValues.value.length) {
    klineHover.value.visible = false
    return
  }
  const item = klineValues.value[index]
  if (!item) {
    klineHover.value.visible = false
    return
  }
  const open = Number(item[0])
  const close = Number(item[1])
  const prevClose = index > 0 ? Number(klineValues.value[index - 1]?.[1]) : NaN
  const changePercent = Number.isFinite(prevClose) && prevClose !== 0
    ? (((close - prevClose) / prevClose) * 100).toFixed(2)
    : '-'
  klineHover.value = {
    visible: true,
    x: x + 12,
    y: event.clientY - rect.top + 12,
    lines: [
      `${klineAxis.value[index]}`,
      `开: ${item[0]}`,
      `收: ${item[1]}`,
      `高: ${item[3]}`,
      `低: ${item[2]}`,
      `涨跌幅: ${changePercent}%`
    ]
  }
}

const handleKlineLeave = () => {
  klineHover.value.visible = false
}

const handleMinuteMove = event => {
  if (!minuteWrapRef.value || minuteAxis.value.length === 0) {
    minuteHover.value.visible = false
    return
  }
  const rect = minuteWrapRef.value.getBoundingClientRect()
  const x = event.clientX - rect.left
  const y = event.clientY - rect.top
  if (rect.width <= 0) {
    minuteHover.value.visible = false
    return
  }
  const ratio = Math.min(Math.max(x / rect.width, 0), 1)
  const index = Math.round(ratio * (minuteAxis.value.length - 1))
  const value = minuteValues.value[index]
  if (value == null) {
    minuteHover.value.visible = false
    return
  }
  const base = minuteBasePrice.value
  const changePercent = base && base !== 0
    ? (((value - base) / base) * 100).toFixed(2)
    : '-'
  minuteHover.value = {
    visible: true,
    x: x + 12,
    y: y + 12,
    lines: [formatMinuteTime(minuteAxis.value[index]), `${value}`, `涨跌幅: ${changePercent}%`]
  }
}

const handleMinuteLeave = () => {
  minuteHover.value.visible = false
}

const renderKLine = () => {
  if (!klineRef.value || !props.kLines.length) return
  ensureCharts()
  const normalized = props.kLines
    .map(item => {
      const rawDate = item?.date ?? item?.Date
      const dateText = typeof rawDate === 'string'
        ? rawDate.slice(0, 10)
        : rawDate instanceof Date
          ? rawDate.toISOString().slice(0, 10)
          : ''
      const timestamp = dateText ? Date.parse(dateText) : NaN
      const open = Number(item?.open ?? item?.Open)
      const close = Number(item?.close ?? item?.Close)
      const low = Number(item?.low ?? item?.Low)
      const high = Number(item?.high ?? item?.High)
      return {
        dateText,
        timestamp,
        values: [open, close, low, high]
      }
    })
    .filter(item => item.dateText && Number.isFinite(item.timestamp) && item.values.every(value => Number.isFinite(value)))
    .sort((a, b) => a.timestamp - b.timestamp)

  if (!normalized.length) {
    klineAxis.value = []
    klineValues.value = []
    klineChart.value?.clear()
    return
  }

  const dates = normalized.map(item => item.dateText)
  const values = normalized.map(item => item.values)
  klineAxis.value = dates
  klineValues.value = values

  klineChart.value?.setOption({
    silent: false,
    tooltip: {
      show: true,
      trigger: 'item',
      triggerOn: 'mousemove',
      appendToBody: true,
      confine: false,
      enterable: false,
      alwaysShowContent: false,
      formatter: params => {
        const value = Array.isArray(params.value) ? params.value : []
        const open = value[0] ?? '-'
        const close = value[1] ?? '-'
        const low = value[2] ?? '-'
        const high = value[3] ?? '-'
        const closeNum = Number(close)
        const index = Number.isFinite(params?.dataIndex)
          ? params.dataIndex
          : klineAxis.value.indexOf(params?.name)
        const prevClose = index > 0 ? Number(klineValues.value[index - 1]?.[1]) : NaN
        const changePercent = Number.isFinite(prevClose) && prevClose !== 0 && Number.isFinite(closeNum)
          ? (((closeNum - prevClose) / prevClose) * 100).toFixed(2)
          : '-'
        return `${params.name}<br/>开:${open} 收:${close}<br/>高:${high} 低:${low}<br/>涨跌幅:${changePercent}%`
      }
    },
    axisPointer: { link: [{ xAxisIndex: 'all' }] },
    grid: { left: '6%', right: '6%', top: 24, bottom: 36, containLabel: true },
    xAxis: { type: 'category', data: dates, axisPointer: { type: 'line' } },
    yAxis: { scale: true, axisPointer: { type: 'line' } },
    series: [{ type: 'candlestick', data: values, emphasis: { focus: 'series' } }]
  }, true)
  requestAnimationFrame(() => klineChart.value?.resize())
}

const renderMinute = () => {
  if (!minuteRef.value) return
  ensureMinuteChart()
  const parseDate = raw => {
    if (!raw) return null
    if (raw instanceof Date && !Number.isNaN(raw.getTime())) {
      return { year: raw.getFullYear(), month: raw.getMonth() + 1, day: raw.getDate() }
    }
    if (typeof raw !== 'string') return null
    const trimmed = raw.trim()
    if (!trimmed) return null
    if (/^\d{8}$/.test(trimmed)) {
      return {
        year: Number(trimmed.slice(0, 4)),
        month: Number(trimmed.slice(4, 6)),
        day: Number(trimmed.slice(6, 8))
      }
    }
    const parts = trimmed.split('T')[0].split('-').map(Number)
    if (parts.length === 3 && parts.every(Number.isFinite)) {
      return { year: parts[0], month: parts[1], day: parts[2] }
    }
    return null
  }

  const parseTime = raw => {
    if (raw == null) return null
    if (typeof raw === 'number' && Number.isFinite(raw)) {
      const hour = Math.floor(raw / 100)
      const minute = raw % 100
      return { hour, minute, second: 0 }
    }
    if (raw instanceof Date && !Number.isNaN(raw.getTime())) {
      return { hour: raw.getHours(), minute: raw.getMinutes(), second: raw.getSeconds() }
    }
    if (typeof raw !== 'string') return null
    const trimmed = raw.trim()
    if (!trimmed) return null
    const parts = trimmed.split(':').map(Number)
    if (parts.length >= 2 && parts.every(Number.isFinite)) {
      return { hour: parts[0], minute: parts[1], second: parts[2] ?? 0 }
    }
    if (/^\d{3,4}$/.test(trimmed) && Number.isFinite(Number(trimmed))) {
      const hhmm = Number(trimmed)
      return { hour: Math.floor(hhmm / 100), minute: hhmm % 100, second: 0 }
    }
    return null
  }

  const data = props.minuteLines.map(item => {
    const datePart = parseDate(item?.date ?? item?.Date)
    const timePart = parseTime(item?.time ?? item?.Time)
    const price = Number(item?.price ?? item?.Price)
    if (!datePart || !timePart || !Number.isFinite(price)) return null
    const { year, month, day } = datePart
    const { hour, minute, second } = timePart
    if (![year, month, day, hour, minute, second].every(Number.isFinite)) return null
    const timestamp = Math.floor(new Date(year, month - 1, day, hour, minute, second).getTime() / 1000)
    return Number.isFinite(timestamp) ? { time: timestamp, value: price } : null
  })
    .filter(Boolean)
    .sort((a, b) => a.time - b.time)
  minuteAxis.value = data.map(item => item.time)
  minuteValues.value = data.map(item => item.value)
  const basePrice = Number.isFinite(props.basePrice) ? props.basePrice : null
  minuteBasePrice.value = basePrice ?? (data.length ? data[0].value : null)
  if (!minuteChart.value || data.length === 0) return
  const base = minuteBasePrice.value
  const timeLabels = data.map(item => formatMinuteTime(item.time))
  const values = data.map(item => item.value)
  minuteChart.value.setOption({
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'line' },
      formatter: params => {
        const item = Array.isArray(params) ? params[0] : params
        if (!item) return ''
        const label = item.axisValue ?? ''
        const value = Number(item.value)
        const changePercent = base && Number.isFinite(value) && base !== 0
          ? (((value - base) / base) * 100).toFixed(2)
          : '-'
        return `${label}<br/>${item.value}<br/>涨跌幅:${changePercent}%`
      }
    },
    grid: { left: '6%', right: '6%', top: 24, bottom: 36, containLabel: true },
    xAxis: {
      type: 'category',
      data: timeLabels,
      boundaryGap: false,
      axisLabel: { color: '#64748b' }
    },
    yAxis: { scale: true, axisLabel: { color: '#64748b' } },
    series: [
      {
        type: 'line',
        data: values,
        showSymbol: false,
        smooth: true,
        lineStyle: { color: '#2563eb', width: 2 },
        areaStyle: { color: 'rgba(37, 99, 235, 0.12)' }
      }
    ]
  }, true)
  resizeMinuteChart()
}

const renderAll = () => {
  renderKLine()
  renderMinute()
}

onMounted(() => {
  ensureCharts()
  renderAll()
  if (typeof ResizeObserver !== 'undefined') {
    resizeObserver = new ResizeObserver(() => {
      klineChart.value?.resize()
      resizeMinuteChart()
    })
    if (klineRef.value) {
      resizeObserver.observe(klineRef.value)
    }
    if (minuteRef.value) {
      resizeObserver.observe(minuteRef.value)
    }
  }
  resizeHandler = () => {
    klineChart.value?.resize()
    resizeMinuteChart()
  }
  window.addEventListener('resize', resizeHandler)
  nextTick(() => {
    klineChart.value?.resize()
    resizeMinuteChart()
  })
})

onUnmounted(() => {
  if (resizeHandler) {
    window.removeEventListener('resize', resizeHandler)
    resizeHandler = null
  }
  if (resizeObserver) {
    resizeObserver.disconnect()
    resizeObserver = null
  }
  if (klineChart.value) {
    klineChart.value.dispose()
    klineChart.value = null
  }
  if (minuteChart.value) {
    minuteChart.value.remove()
    minuteChart.value = null
  }
})

watch(() => [props.kLines, props.minuteLines], async () => {
  renderAll()
  await nextTick()
  klineChart.value?.resize()
  resizeMinuteChart()
})
</script>

<template>
  <div class="charts">
    <div class="chart-wrapper" ref="klineWrapRef" @mousemove="handleKlineMove" @mouseleave="handleKlineLeave">
      <div class="chart-header">
        <h3>K 线图</h3>
        <div class="chart-tabs">
          <button
            v-for="item in ['day', 'week', 'month', 'year']"
            :key="item"
            class="tab"
            :class="{ active: interval === item }"
            @click="emit('update:interval', item)"
          >
            {{ item === 'day' ? '日线' : item === 'week' ? '周线' : item === 'month' ? '月线' : '年线' }}
          </button>
        </div>
      </div>
      <div ref="klineRef" class="chart" v-show="kLines.length" />
      <p class="placeholder" v-show="!kLines.length">暂无 K 线数据</p>
      <div
        v-if="klineHover.visible"
        class="hover-tip"
        :style="{ left: `${klineHover.x}px`, top: `${klineHover.y}px` }"
      >
        <div v-for="(line, idx) in klineHover.lines" :key="idx">{{ line }}</div>
      </div>
    </div>
    <div class="chart-wrapper" ref="minuteWrapRef" @mousemove="handleMinuteMove" @mouseleave="handleMinuteLeave">
      <h3>分时图</h3>
      <div ref="minuteRef" class="chart" v-show="minuteLines.length" />
      <p class="placeholder" v-show="!minuteLines.length">暂无分时数据</p>
      <div
        v-if="minuteHover.visible"
        class="hover-tip"
        :style="{ left: `${minuteHover.x}px`, top: `${minuteHover.y}px` }"
      >
        <div v-for="(line, idx) in minuteHover.lines" :key="idx">{{ line }}</div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.charts {
  display: grid;
  grid-template-columns: 1fr;
  gap: 1rem;
  width: 100%;
}

.chart-wrapper {
  width: 100%;
  min-width: 0;
  overflow: visible;
  position: relative;
}

.chart-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.chart-tabs {
  display: flex;
  gap: 0.5rem;
}

.chart {
  width: 100%;
  height: 280px;
  display: block;
  pointer-events: auto;
}

.chart :deep(canvas),
.chart :deep(div) {
  width: 100% !important;
}

:global(.echarts-tooltip) {
  z-index: 9999 !important;
}

.placeholder {
  color: #9ca3af;
  margin: 0.5rem 0 0;
}

.hover-tip {
  position: absolute;
  z-index: 20;
  background: rgba(15, 23, 42, 0.9);
  color: #f8fafc;
  padding: 0.4rem 0.6rem;
  border-radius: 6px;
  font-size: 0.8rem;
  pointer-events: none;
  white-space: nowrap;
}
</style>
