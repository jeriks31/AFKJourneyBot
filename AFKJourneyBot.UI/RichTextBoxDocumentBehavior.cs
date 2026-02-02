using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace AFKJourneyBot.UI;

public static class RichTextBoxDocumentBehavior
{
    public static readonly DependencyProperty DocumentProperty = DependencyProperty.RegisterAttached(
        "Document",
        typeof(FlowDocument),
        typeof(RichTextBoxDocumentBehavior),
        new PropertyMetadata(null, OnDocumentChanged));

    public static FlowDocument? GetDocument(DependencyObject obj) => (FlowDocument?)obj.GetValue(DocumentProperty);
    public static void SetDocument(DependencyObject obj, FlowDocument? value) => obj.SetValue(DocumentProperty, value);

    private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBox richTextBox)
        {
            return;
        }

        if (e.NewValue is FlowDocument document)
        {
            richTextBox.Document = document;
        }
        else
        {
            richTextBox.Document = new FlowDocument();
        }
    }
}
