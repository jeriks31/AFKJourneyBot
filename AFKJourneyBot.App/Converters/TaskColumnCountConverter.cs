using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AFKJourneyBot.App.Converters;

public sealed class TaskColumnCountConverter : IValueConverter
{
    private const double MinCardWidth = 190;
    private const int MaxColumns = 3;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double width || double.IsNaN(width) || width <= 0)
        {
            return 1;
        }

        return Math.Clamp((int)(width / MinCardWidth), 1, MaxColumns);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
