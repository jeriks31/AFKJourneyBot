using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace AFKJourneyBot.UI;

public static class AutoScrollBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(AutoScrollBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty HandlerProperty = DependencyProperty.RegisterAttached(
        "Handler",
        typeof(NotifyCollectionChangedEventHandler),
        typeof(AutoScrollBehavior),
        new PropertyMetadata(null));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ItemsControl itemsControl)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            Attach(itemsControl);
        }
        else
        {
            Detach(itemsControl);
        }
    }

    private static void Attach(ItemsControl itemsControl)
    {
        var handler = new NotifyCollectionChangedEventHandler((_, _) => ScrollToEnd(itemsControl));
        itemsControl.SetValue(HandlerProperty, handler);
        itemsControl.Items.CurrentChanged += (_, _) => ScrollToEnd(itemsControl);

        if (itemsControl.Items is INotifyCollectionChanged notifyCollectionChanged)
        {
            notifyCollectionChanged.CollectionChanged += handler;
        }

        itemsControl.Loaded += ItemsControlOnLoaded;
        itemsControl.Unloaded += ItemsControlOnUnloaded;
        ScrollToEnd(itemsControl);
    }

    private static void Detach(ItemsControl itemsControl)
    {
        itemsControl.Loaded -= ItemsControlOnLoaded;
        itemsControl.Unloaded -= ItemsControlOnUnloaded;

        if (itemsControl.Items is INotifyCollectionChanged notifyCollectionChanged)
        {
            if (itemsControl.GetValue(HandlerProperty) is NotifyCollectionChangedEventHandler handler)
            {
                notifyCollectionChanged.CollectionChanged -= handler;
            }
        }

        itemsControl.ClearValue(HandlerProperty);
    }

    private static void ItemsControlOnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ItemsControl itemsControl)
        {
            ScrollToEnd(itemsControl);
        }
    }

    private static void ItemsControlOnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ItemsControl itemsControl)
        {
            Detach(itemsControl);
        }
    }

    private static void ScrollToEnd(ItemsControl itemsControl)
    {
        if (itemsControl.Items.Count == 0)
        {
            return;
        }

        var lastItem = itemsControl.Items[itemsControl.Items.Count - 1];
        itemsControl.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            () =>
            {
                if (itemsControl is ListBox listBox)
                {
                    listBox.ScrollIntoView(lastItem);
                    return;
                }

                if (itemsControl is ListView listView)
                {
                    listView.ScrollIntoView(lastItem);
                    return;
                }

                var scrollViewer = FindScrollViewer(itemsControl);
                scrollViewer?.ScrollToEnd();
            });
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer viewer)
        {
            return viewer;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var result = FindScrollViewer(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
