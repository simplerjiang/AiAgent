<script setup>
import { onMounted, onUnmounted, ref, watch, nextTick } from 'vue'
import * as echarts from 'echarts'

const props = defineProps({
  chart: { type: Object, default: null },
  height: { type: String, default: '200px' }
})

const chartRef = ref(null)
const chartInstance = ref(null)

const buildOption = chart => {
  if (!chart || !chart.type) return null
  const title = chart.title || ''
  if (chart.type === 'gauge') {
    const value = Number(chart.value ?? 0)
    const min = Number(chart.min ?? 0)
    const max = Number(chart.max ?? 100)
    return {
      title: { text: title, left: 'center', top: 6, textStyle: { fontSize: 12 } },
      series: [
        {
          type: 'gauge',
          min,
          max,
          data: [{ value, name: title }],
          axisLine: { lineStyle: { width: 10 } },
          pointer: { width: 4 },
          detail: { formatter: '{value}', fontSize: 14 }
        }
      ]
    }
  }

  const labels = Array.isArray(chart.labels) ? chart.labels : []
  const values = Array.isArray(chart.values) ? chart.values : []

  if (chart.type === 'bar') {
    return {
      title: { text: title, left: 'center', top: 6, textStyle: { fontSize: 12 } },
      grid: { left: '10%', right: '6%', top: 36, bottom: 24 },
      xAxis: { type: 'category', data: labels, axisLabel: { color: '#64748b' } },
      yAxis: { type: 'value', axisLabel: { color: '#64748b' } },
      series: [{ type: 'bar', data: values, itemStyle: { color: '#2563eb' } }]
    }
  }

  if (chart.type === 'line') {
    return {
      title: { text: title, left: 'center', top: 6, textStyle: { fontSize: 12 } },
      grid: { left: '10%', right: '6%', top: 36, bottom: 24 },
      xAxis: { type: 'category', data: labels, axisLabel: { color: '#64748b' } },
      yAxis: { type: 'value', axisLabel: { color: '#64748b' } },
      series: [
        {
          type: 'line',
          data: values,
          smooth: true,
          showSymbol: false,
          lineStyle: { color: '#0ea5e9', width: 2 },
          areaStyle: { color: 'rgba(14, 165, 233, 0.12)' }
        }
      ]
    }
  }

  return null
}

const renderChart = () => {
  if (!chartRef.value || !props.chart) return
  if (!chartInstance.value) {
    chartInstance.value = echarts.init(chartRef.value)
  }
  const option = buildOption(props.chart)
  if (!option) {
    chartInstance.value?.clear()
    return
  }
  chartInstance.value.setOption(option, true)
}

onMounted(() => {
  renderChart()
  nextTick(() => {
    chartInstance.value?.resize()
  })
})

onUnmounted(() => {
  chartInstance.value?.dispose()
  chartInstance.value = null
})

watch(
  () => props.chart,
  async () => {
    renderChart()
    await nextTick()
    chartInstance.value?.resize()
  },
  { deep: true }
)
</script>

<template>
  <div class="agent-chart" :style="{ height }">
    <div ref="chartRef" class="agent-chart-canvas" />
    <p v-if="!chart" class="muted">暂无图表数据</p>
  </div>
</template>

<style scoped>
.agent-chart {
  width: 100%;
  position: relative;
}

.agent-chart-canvas {
  width: 100%;
  height: 100%;
}

.muted {
  color: #94a3b8;
  font-size: 0.85rem;
  position: absolute;
  inset: 0;
  display: grid;
  place-items: center;
}
</style>