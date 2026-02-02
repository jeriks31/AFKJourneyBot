using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AFKJourneyBot.UI;

public static class RichTextBoxAutoScrollBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(RichTextBoxAutoScrollBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBox richTextBox)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            richTextBox.TextChanged += RichTextBoxOnTextChanged;
            richTextBox.Unloaded += RichTextBoxOnUnloaded;
            richTextBox.Loaded += RichTextBoxOnLoaded;
        }
        else
        {
            richTextBox.TextChanged -= RichTextBoxOnTextChanged;
            richTextBox.Unloaded -= RichTextBoxOnUnloaded;
            richTextBox.Loaded -= RichTextBoxOnLoaded;
        }
    }

    private static void RichTextBoxOnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is RichTextBox richTextBox)
        {
            richTextBox.ScrollToEnd();
        }
    }

    private static void RichTextBoxOnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is RichTextBox richTextBox)
        {
            richTextBox.TextChanged -= RichTextBoxOnTextChanged;
            richTextBox.Unloaded -= RichTextBoxOnUnloaded;
            richTextBox.Loaded -= RichTextBoxOnLoaded;
        }
    }

    private static void RichTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not RichTextBox richTextBox)
        {
            return;
        }

        if (richTextBox.IsKeyboardFocusWithin)
        {
            return;
        }

        richTextBox.Dispatcher.BeginInvoke(DispatcherPriority.Background, richTextBox.ScrollToEnd);
    }
}
