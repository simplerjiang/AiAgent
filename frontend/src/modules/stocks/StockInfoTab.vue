<script setup>
import { ref } from 'vue'

const symbol = ref('')
const loading = ref(false)
const error = ref('')
const quote = ref(null)

const fetchQuote = async () => {
  if (!symbol.value.trim()) {
    error.value = '请输入股票代码'
    return
  }

  loading.value = true
  error.value = ''
  quote.value = null

  try {
    const response = await fetch(`http://localhost:5000/api/stocks/quote?symbol=${encodeURIComponent(symbol.value)}`)
    if (!response.ok) {
      throw new Error('接口请求失败')
    }
    quote.value = await response.json()
  } catch (err) {
    error.value = err.message || '请求失败'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="panel">
    <h2>股票信息</h2>
    <div class="field">
      <input v-model="symbol" placeholder="输入股票代码，例如 000001" />
      <button @click="fetchQuote" :disabled="loading">查询</button>
    </div>

    <p v-if="error" class="muted">{{ error }}</p>
    <p v-else-if="loading" class="muted">查询中...</p>

    <div v-if="quote">
      <p><strong>{{ quote.name }}</strong>（{{ quote.symbol }}）</p>
      <p>当前价：{{ quote.price }}</p>
      <p>涨跌：{{ quote.change }}（{{ quote.changePercent }}%）</p>
      <p class="muted">新闻：{{ quote.news.length }} 条</p>
      <p class="muted">指标：{{ quote.indicators.length }} 项</p>
    </div>

    <p v-else class="muted">
      数据来源：腾讯 / 新浪 / 百度（后端爬虫占位）
    </p>
  </section>
</template>
