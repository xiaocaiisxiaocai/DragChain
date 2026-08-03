import { getErrorMessage } from './errorHandler';

if (getErrorMessage(new Error('服务异常')) !== '服务异常') {
  throw new Error('全局错误处理必须提取 Error.message');
}

if (getErrorMessage('网络异常') !== '网络异常') {
  throw new Error('全局错误处理必须保留字符串错误');
}

if (getErrorMessage({ code: 500 }, '操作失败') !== '操作失败') {
  throw new Error('未知错误类型必须使用调用方提供的兜底文案');
}
