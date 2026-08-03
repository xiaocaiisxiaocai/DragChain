import { createLatestAsync } from './latestAsync';

type Deferred = {
  promise: Promise<string>;
  resolve: (value: string) => void;
};

function createDeferred(): Deferred {
  let resolve!: (value: string) => void;
  const promise = new Promise<string>(resolver => {
    resolve = resolver;
  });
  return { promise, resolve };
}

const deferredByKeyword = new Map<string, Deferred>();
const applied: string[] = [];
const loadingStates: boolean[] = [];
const latestSearch = createLatestAsync(
  (keyword: string) => {
    const deferred = createDeferred();
    deferredByKeyword.set(keyword, deferred);
    return deferred.promise;
  },
  (result: string) => applied.push(result),
  (loading: boolean) => loadingStates.push(loading)
);

const oldRequest = latestSearch.run('old');
const newRequest = latestSearch.run('new');
deferredByKeyword.get('new')?.resolve('new-result');
await newRequest;
deferredByKeyword.get('old')?.resolve('old-result');
await oldRequest;

if (applied.join(',') !== 'new-result') {
  throw new Error('较慢的旧请求不能覆盖最新搜索结果');
}
if (loadingStates[loadingStates.length - 1] !== false) {
  throw new Error('最新请求结束后必须关闭加载状态');
}

const invalidatedRequest = latestSearch.run('invalidated');
latestSearch.invalidate();
deferredByKeyword.get('invalidated')?.resolve('invalidated-result');
await invalidatedRequest;

if (applied.includes('invalidated-result')) {
  throw new Error('输入清空或组件卸载后，已失效请求不能再写入结果');
}
