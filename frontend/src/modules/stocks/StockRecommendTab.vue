<script setup>
import { computed, nextTick, onMounted, ref } from 'vue'
import ChatWindow from '../../components/ChatWindow.vue'

const presets = [
  { label: '每日国内外新闻', prompt: '请汇总今日国内外重要财经新闻，分点列出。' },
  { label: '当日股票推荐', prompt: '请给出今日值得关注的股票方向或个股，并简述理由与风险。' },
  { label: '当日行情分析', prompt: '请对今日市场行情进行简要分析，包含指数、热点板块与风险提示。' }
]

const chatRef = ref(null)
const sessionStorageKey = 'stock_recommend_chat_sessions'
const historyStorageKey = 'stock_recommend_chat_history_map'
const sessions = ref([])
const selectedSession = ref('')

const sessionOptions = computed(() => sessions.value)

const persistSessions = () => {
  localStorage.setItem(sessionStorageKey, JSON.stringify(sessions.value))
}

const loadSessions = () => {
  try {
    const raw = localStorage.getItem(sessionStorageKey)
    sessions.value = raw ? JSON.parse(raw) : []
  } catch {
    sessions.value = []
  }
}

const createSession = () => {
  const timestamp = new Date()
  const key = `recommend-${timestamp.getTime()}`
  const label = `${timestamp.getFullYear()}-${String(timestamp.getMonth() + 1).padStart(2, '0')}-${String(
    timestamp.getDate()
  ).padStart(2, '0')} ${String(timestamp.getHours()).padStart(2, '0')}:${String(timestamp.getMinutes()).padStart(2, '0')}`
  const entry = { key, label }
  sessions.value = [entry, ...sessions.value]
  persistSessions()
  selectedSession.value = key
}

const handleNewChat = async () => {
  createSession()
  await nextTick()
  chatRef.value?.createNewChat()
}

onMounted(() => {
  loadSessions()
  if (!sessions.value.length) {
    createSession()
  } else if (!selectedSession.value) {
    selectedSession.value = sessions.value[0].key
  }
})
</script>

<template>
  <section class="panel">
    <div class="panel-header">
      <h2>股票推荐</h2>
      <p class="muted">使用 LLM 汇总新闻、推荐与行情分析。</p>
    </div>

    <ChatWindow
      ref="chatRef"
      title="推荐助手"
      :presets="presets"
      :history-key="selectedSession"
      :enable-history="true"
      :history-storage-key="historyStorageKey"
      placeholder="输入你的问题，例如：今天有哪些热点板块？"
      empty-text="点击上方按钮或输入问题开始对话。"
      max-height="520px"
    >
      <template #header-extra>
        <div class="session-selector">
          <select v-model="selectedSession">
            <option v-for="item in sessionOptions" :key="item.key" :value="item.key">
              {{ item.label }}
            </option>
          </select>
          <button class="session-new" @click="handleNewChat">新建对话</button>
        </div>
      </template>
    </ChatWindow>
  </section>
</template>

<style scoped>
.panel {
  background: linear-gradient(135deg, rgba(255, 255, 255, 0.75), rgba(248, 250, 252, 0.85));
  backdrop-filter: blur(10px);
  border: 1px solid rgba(148, 163, 184, 0.2);
  box-shadow: 0 10px 30px rgba(15, 23, 42, 0.08);
  border-radius: 16px;
  padding: 1.5rem;
}

.panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1rem;
}

.session-selector {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.session-selector select {
  border-radius: 8px;
  border: 1px solid rgba(148, 163, 184, 0.4);
  padding: 0.35rem 0.6rem;
  background: #ffffff;
}

.session-new {
  border-radius: 999px;
  border: none;
  padding: 0.35rem 0.8rem;
  background: #e2e8f0;
  color: #0f172a;
  cursor: pointer;
}

</style>
