import { showToast } from '../utils/toast.js'

export function useToast() {
  return {
    success(msg, duration = 3000) { showToast({ message: msg, type: 'success', duration }) },
    error(msg, duration = 5000) { showToast({ message: msg, type: 'error', duration }) },
    info(msg, duration = 3000) { showToast({ message: msg, type: 'info', duration }) },
    warning(msg, duration = 4000) { showToast({ message: msg, type: 'warning', duration }) }
  }
}
