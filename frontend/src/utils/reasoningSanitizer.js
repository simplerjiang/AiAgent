const THINK_BLOCK_PATTERN = /<think>[\s\S]*?<\/think>/gi
const REASONING_SECTION_PATTERN = /(^|\n)#{0,6}\s*(思考过程|推理过程|reasoning|analysis|chain of thought|chain-of-thought)[^\n]*(\n[\s\S]*)?$/i

const REASONING_SCAFFOLD_PHRASES = [
  'initiating market analysis',
  'refining search strategies',
  'adapting query approach',
  'analyzing current context',
  'reviewing market trends',
  'considering the request',
  'analyzing the request',
  'analyzing the scenario',
  'analyzing the data',
  'refining the strategy',
  'refining the approach',
  'simulating the search',
  'simulating information retrieval',
  'defining the scope',
  'interpreting the data',
  'formulating the response',
  'assessing risk elements',
  'synthesizing risk insights',
  'my thought process',
  'thought process',
  "let's break this down before answering",
  "let's break this down",
  'before answering',
  'i need to understand',
  "i'm zeroing in on"
]

const LEADING_REASONING_NARRATIVE_MARKERS = [
  "i'm currently dissecting",
  'i am currently dissecting',
  "i'm now focusing on",
  'i am now focusing on',
  "here's how i'm approaching this",
  "here is how i'm approaching this",
  'the role is clear',
  'the task is clear',
  'the task is straightforward',
  'the core objective is',
  "i'll need to consider",
  'i will need to consider',
  'my analysis centers on',
  'focusing on the core task',
  'focusing on the core objective'
]

const LEADING_REASONING_NARRATIVE_ACTOR_MARKERS = [
  "i'm",
  'i am',
  "i've",
  'i have',
  "i'll",
  'i will',
  'my ',
  "here's",
  'here is',
  "let's",
  'the user',
  'the prompt',
  'the task',
  'the role',
  'the goal'
]

const LEADING_REASONING_NARRATIVE_META_MARKERS = [
  'json array',
  'structured json',
  'prompt',
  'task',
  'objective',
  'constraint',
  'approach',
  'analysis',
  'analyzing',
  'dissecting',
  'focus',
  'focusing',
  'consider',
  'generate',
  'delivering',
  'adhere'
]

const reasoningScaffoldPhrasePattern = REASONING_SCAFFOLD_PHRASES
  .map(phrase => phrase.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'))
  .join('|')

const REASONING_SCAFFOLD_LINE_PATTERN = new RegExp(
  `(\\*{0,2}\\s*)?(${reasoningScaffoldPhrasePattern})(\\*{0,2}\\s*)?[:：-]?\\s*`,
  'gi'
)

const ENGLISH_TITLE_WORD_PATTERN = "(?:[A-Z][A-Za-z'&/-]*|the|and|of|to|for|in|on|with|from|a|an)"
const LEADING_REASONING_TITLE_BLOCK_PATTERN = new RegExp(
  `^(?:\\s|[*#>` + '"' + `'_\\-])*(?:(?:\\*{0,2}${ENGLISH_TITLE_WORD_PATTERN}(?:\\s+${ENGLISH_TITLE_WORD_PATTERN}){0,7}\\*{0,2})(?:[:：-]?\\s*)){2,}`,
  'g'
)
const EMPHASIZED_TITLE_SEGMENT_PATTERN = "\\*{1,2}[A-Z][A-Za-z'&/-]*(?:\\s+[A-Z][A-Za-z'&/-]*){1,7}\\*{1,2}"
const LEADING_EMPHASIZED_TITLE_SEQUENCE_PATTERN = new RegExp(
  `^(?:\\s|[*#>` + '"' + `'_\\-])*(?:${EMPHASIZED_TITLE_SEGMENT_PATTERN}(?:\\s+|[:：-]?\\s*)){2,}`,
  'g'
)

const CJK_OR_JSON_START_PATTERN = /[\u3400-\u9fff[{]/

const looksLikeLeadingEnglishReasoningNarrative = value => {
  const normalized = String(value || '')
    .replace(/\s+/g, ' ')
    .trim()

  if (!normalized || normalized.length < 24 || /[\u3400-\u9fff]/.test(normalized)) {
    return false
  }

  const lowered = normalized.toLowerCase()
  if (LEADING_REASONING_NARRATIVE_MARKERS.some(marker => lowered.includes(marker))) {
    return true
  }

  return LEADING_REASONING_NARRATIVE_ACTOR_MARKERS.some(marker => lowered.includes(marker)) &&
    LEADING_REASONING_NARRATIVE_META_MARKERS.some(marker => lowered.includes(marker))
}

const stripLeadingEnglishReasoningNarrative = content => {
  const value = String(content || '')
  const match = value.match(CJK_OR_JSON_START_PATTERN)
  const cutoff = match ? match.index : value.length
  const prefix = value.slice(0, cutoff)

  if (!looksLikeLeadingEnglishReasoningNarrative(prefix)) {
    return value
  }

  return value.slice(cutoff).trimStart()
}

export const stripReasoningScaffolds = content => {
  let sanitized = String(content || '')
    .replace(THINK_BLOCK_PATTERN, '')
    .replace(REASONING_SECTION_PATTERN, '')
    .replace(REASONING_SCAFFOLD_LINE_PATTERN, '')

  while (true) {
    const nextValue = sanitized
      .replace(LEADING_EMPHASIZED_TITLE_SEQUENCE_PATTERN, '')
      .replace(LEADING_REASONING_TITLE_BLOCK_PATTERN, '')
      .trimStart()

    const nextNarrativeValue = stripLeadingEnglishReasoningNarrative(nextValue)
    if (nextNarrativeValue === sanitized) {
      break
    }

    sanitized = nextNarrativeValue
  }

  return sanitized
}

export const sanitizeAssistantContent = content => stripReasoningScaffolds(content)
  .replace(/\n{3,}/g, '\n\n')
  .trim()

export const sanitizeStreamingAssistantContent = content => stripReasoningScaffolds(content).trimStart()

export const summarizeReasoningSafeText = (value, emptyFallback = '返回内容包含中间推理，已脱敏。') => {
  const normalized = stripReasoningScaffolds(value)
    .replace(/\s+/g, ' ')
    .trim()

  if (!normalized) {
    return emptyFallback
  }

  return normalized.length > 180 ? `${normalized.slice(0, 180)}...` : normalized
}