// ========================================
// File: CnabProcessor.Domain/Extensions/EnumExtensions.cs
// Purpose: Extension methods for enums
// ========================================

using System.ComponentModel;
using System.Reflection;

namespace CnabProcessor.Domain.Extensions;

public static class EnumExtensions
{
    /// <summary>
    /// Gets the Description attribute value from an enum value.
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());

        if (field == null)
            return value.ToString();

        var attribute = field.GetCustomAttribute<DescriptionAttribute>();

        return attribute?.Description ?? value.ToString();
    }
}