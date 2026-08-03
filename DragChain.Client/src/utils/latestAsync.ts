export function createLatestAsync<TArgs extends unknown[], TResult>(
  operation: (...args: TArgs) => Promise<TResult>,
  applyResult: (result: TResult, ...args: TArgs) => void,
  setLoading: (loading: boolean) => void = () => undefined
) {
  let generation = 0;

  async function run(...args: TArgs): Promise<TResult | undefined> {
    const requestGeneration = ++generation;
    setLoading(true);

    try {
      const result = await operation(...args);
      if (requestGeneration === generation) {
        applyResult(result, ...args);
      }
      return result;
    } catch (error) {
      if (requestGeneration === generation) throw error;
      return undefined;
    } finally {
      if (requestGeneration === generation) {
        setLoading(false);
      }
    }
  }

  function invalidate() {
    generation++;
    setLoading(false);
  }

  return { run, invalidate };
}
