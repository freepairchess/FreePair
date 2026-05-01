using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FreePair.App.ViewModels;

/// <summary>
/// XAML helper: maps a <see cref="bool"/> to one of two brushes.
/// Used by <c>RenumberBoardsDialog</c> to colour the conflict
/// summary text orange when there are overlapping board ranges,
/// gray otherwise. Brush names use the standard Avalonia /
/// System.Drawing colour palette.
/// </summary>
public sealed class BoolToBrushConverter : IValueConverter
{
    /// <summary>Brush returned when the source value is <c>true</c>.</summary>
    public string TrueBrush { get; set; } = "DarkOrange";

    /// <summary>Brush returned when the source value is <c>false</c>.</summary>
    public string FalseBrush { get; set; } = "Gray";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var b = value is bool actual && actual;
        var name = b ? TrueBrush : FalseBrush;
        return Brush.Parse(name);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
