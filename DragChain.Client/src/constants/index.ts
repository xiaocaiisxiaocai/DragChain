/**
 * 防抖延迟时间配置（毫秒）
 */
export const DEBOUNCE_DELAY = {
  /** 保存工作区延迟 */
  SAVE_WORKSPACE: 600,
  /** 计算延迟 */
  CALCULATE: 300,
  /** 搜索延迟 */
  SEARCH: 500
} as const;

/**
 * 管线类型分类
 */
export const PIPE_CATEGORIES = {
  /** 线槽允许的管线类型 */
  TRUNKING: ['weak_cable', 'strong_cable', 'encoder'] as const,
  /** 拖链允许的管线类型 */
  CHAIN: ['tube', 'weak_cable', 'strong_cable', 'cable', 'encoder', 'other'] as const
} as const;

/**
 * 文件上传限制
 */
export const UPLOAD_LIMITS = {
  /** 图片最大尺寸（字节） */
  IMAGE_MAX_SIZE: 5 * 1024 * 1024, // 5MB
  /** 图片允许的 MIME 类型 */
  IMAGE_MIME_TYPES: ['image/png', 'image/jpeg', 'image/webp', 'image/jpg'] as const
} as const;
