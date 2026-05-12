using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace RemoSystemProfiler;

public sealed class AnimatedFlyoutPanel : StackPanel
{
    private const double EntranceOffset = 8;
    private const int ChildStaggerMilliseconds = 22;

    private CancellationTokenSource? _animation;

    public AnimatedFlyoutPanel()
    {
        Opacity = 0;
        RenderTransform = new TranslateTransform(0, EntranceOffset);
        AttachedToVisualTree += (_, _) => StartEntranceAnimation();
        DetachedFromVisualTree += (_, _) => StopAnimation();
    }

    private void StartEntranceAnimation()
    {
        StopAnimation();
        _animation = new CancellationTokenSource();
        PrepareChildren();
        _ = AnimateEntranceAsync(_animation.Token);
    }

    private void PrepareChildren()
    {
        foreach (Control child in Children.OfType<Control>())
        {
            child.Opacity = 0;
            child.RenderTransform = new TranslateTransform(0, EntranceOffset);
        }
    }

    private async Task AnimateEntranceAsync(CancellationToken token)
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        if (token.IsCancellationRequested)
        {
            return;
        }

        Task panelTask = AnimateControlAsync(this, 0, token);
        List<Task> childTasks = [];
        int index = 0;
        foreach (Control child in Children.OfType<Control>())
        {
            childTasks.Add(AnimateControlAsync(child, index * ChildStaggerMilliseconds, token));
            index++;
        }

        await Task.WhenAll(childTasks.Prepend(panelTask)).ConfigureAwait(true);
    }

    private static async Task AnimateControlAsync(Control control, int delayMilliseconds, CancellationToken token)
    {
        if (delayMilliseconds > 0)
        {
            await Task.Delay(delayMilliseconds, token).ConfigureAwait(true);
        }

        TranslateTransform? transform = control.RenderTransform as TranslateTransform;
        if (transform is null)
        {
            transform = new TranslateTransform(0, EntranceOffset);
            control.RenderTransform = transform;
        }

        for (int frame = 1; frame <= DashboardAnimation.Frames; frame++)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            double t = frame / (double)DashboardAnimation.Frames;
            double eased = DashboardAnimation.EaseOutCubic(t);
            control.Opacity = DashboardAnimation.Lerp(0, 1, eased);
            transform.Y = DashboardAnimation.Lerp(EntranceOffset, 0, eased);
            await Task.Delay(DashboardAnimation.DurationMilliseconds / DashboardAnimation.Frames, token).ConfigureAwait(true);
        }

        control.Opacity = 1;
        transform.Y = 0;
    }

    private void StopAnimation()
    {
        _animation?.Cancel();
        _animation?.Dispose();
        _animation = null;
    }
}
