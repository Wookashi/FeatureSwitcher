type Task<T> = () => Promise<T>;

interface QueuedTask<T> {
  task: Task<T>;
  resolve: (value: T) => void;
  reject: (error: unknown) => void;
}

export class ConcurrencyLimiter {
  private maxConcurrent: number;
  private running = 0;
  private queue: QueuedTask<unknown>[] = [];

  constructor(maxConcurrent: number = 6) {
    this.maxConcurrent = maxConcurrent;
  }

  async run<T>(task: Task<T>): Promise<T> {
    return new Promise<T>((resolve, reject) => {
      this.queue.push({ task, resolve, reject } as QueuedTask<unknown>);
      this.processQueue();
    });
  }

  private processQueue(): void {
    while (this.running < this.maxConcurrent && this.queue.length > 0) {
      const item = this.queue.shift();
      if (!item) break;

      this.running++;
      item
        .task()
        .then((result) => {
          item.resolve(result);
        })
        .catch((error) => {
          item.reject(error);
        })
        .finally(() => {
          this.running--;
          this.processQueue();
        });
    }
  }

  clear(): void {
    this.queue = [];
  }
}

export function createLimiter(maxConcurrent: number = 6): ConcurrencyLimiter {
  return new ConcurrencyLimiter(maxConcurrent);
}
