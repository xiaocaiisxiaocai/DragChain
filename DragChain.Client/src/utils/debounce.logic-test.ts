import { debounce } from './debounce';

let nextTimerId = 1;
const timers = new Map<number, TimerHandler>();

Object.defineProperty(globalThis, 'window', {
  configurable: true,
  value: {
    setTimeout(handler: TimerHandler) {
      const timerId = nextTimerId++;
      timers.set(timerId, () => {
        timers.delete(timerId);
        if (typeof handler === 'function') handler();
      });
      return timerId;
    },
    clearTimeout(timerId: number) {
      timers.delete(timerId);
    }
  }
});

const calls: string[] = [];
const debounced = debounce((value: string) => {
  calls.push(value);
  return `handled:${value}`;
}, 500);

const returnValue = debounced('first');
debounced('second');

if (returnValue !== undefined) {
  throw new Error('防抖函数在延迟执行前不能伪装成返回原函数结果');
}
if (timers.size !== 1) {
  throw new Error('连续触发防抖函数时只能保留最后一个定时任务');
}

const latestHandler = [...timers.values()][0];
if (typeof latestHandler !== 'function') {
  throw new Error('防抖函数必须注册可执行的延迟回调');
}
latestHandler();

if (calls.join(',') !== 'second') {
  throw new Error('防抖函数必须只使用最后一次调用参数');
}

debounced('cancelled');
debounced.cancel();
if (Array.from(timers.keys()).length !== 0) {
  throw new Error('取消防抖后不能残留待执行任务');
}
