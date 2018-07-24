using System;

namespace FFXIV.Framework.Common
{
    public class OverlayProgress
    {
        private IProgress<Action> currentProgress;

        public OverlayProgress()
        {
            this.CreateProgress();
        }

        private void CreateProgress()
        {
            this.currentProgress = new Progress<Action>(this.OnProgressChanged);
        }

        public void Report(Action action)
        {
            this.currentProgress?.Report(action);
        }

        private void OnProgressChanged(
            Action action)
        {
            action?.Invoke();
        }
    }
}
