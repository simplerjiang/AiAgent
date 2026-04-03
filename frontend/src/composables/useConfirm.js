import { ref } from 'vue'

const visible = ref(false)
const title = ref('')
const message = ref('')
const confirmText = ref('确认')
const cancelText = ref('取消')
const type = ref('warning')
let resolvePromise = null

export function useConfirm() {
  function confirm(options) {
    if (typeof options === 'string') {
      options = { message: options }
    }
    title.value = options.title || '确认操作'
    message.value = options.message || ''
    confirmText.value = options.confirmText || '确认'
    cancelText.value = options.cancelText || '取消'
    type.value = options.type || 'warning'
    visible.value = true

    return new Promise(resolve => {
      resolvePromise = resolve
    })
  }

  function handleConfirm() {
    visible.value = false
    resolvePromise?.(true)
    resolvePromise = null
  }

  function handleCancel() {
    visible.value = false
    resolvePromise?.(false)
    resolvePromise = null
  }

  return { visible, title, message, confirmText, cancelText, type, confirm, handleConfirm, handleCancel }
}
