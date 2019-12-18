using System;
using System.Threading.Tasks;

namespace FFXIV.Framework.Common
{
    public class DelayableTask
    {
        public Task Task { get; private set; }

        public bool IsCancel { get; set; }

        public static DelayableTask Run(
            Action action,
            TimeSpan delay)
        {
            var task = new DelayableTask(action, delay);
            task.Task.Start();
            return task;
        }

        public DelayableTask()
        {
        }

        public DelayableTask(
            Action action,
            TimeSpan delay)
            => this.CreateTask(action, delay);

        public void CreateTask(
            Action action,
            TimeSpan delay)
        {
            this.Task = new Task(async () =>
            {
                if (delay.TotalMilliseconds <= 0)
                {
                    action?.Invoke();
                    return;
                }

                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay((int)delay.TotalMilliseconds / 10);

                    if (this.IsCancel)
                    {
                        return;
                    }
                }

                action?.Invoke();
            });
        }
    }
}
