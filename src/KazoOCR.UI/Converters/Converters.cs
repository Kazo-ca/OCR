using System.Globalization;

namespace KazoOCR.UI.Converters;

/// <summary>
/// Converts an integer count to a boolean visibility value.
/// Returns true if count > 0, false otherwise.
/// Use ConverterParameter="inverse" to invert the logic.
/// </summary>
public sealed class IntToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var count = value as int? ?? 0;
        var result = count > 0;

        if (parameter is string paramString && paramString.Equals("inverse", StringComparison.OrdinalIgnoreCase))
        {
            result = !result;
        }

        return result;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a percentage value (0-100) to a progress value (0-1).
/// </summary>
public sealed class PercentToProgressConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            return percent / 100.0;
        }
        return 0.0;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double progress)
        {
            return progress * 100.0;
        }
        return 0.0;
    }
}
