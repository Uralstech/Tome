// Copyright 2026 URAV ADVANCED LEARNING SYSTEMS PRIVATE LIMITED
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GamebananaAPI.Data.Submission;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Tome.Converters;

public sealed class LicensePermissionsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not LicenseChecklist checklist)
            return "No license permissions provided.";

        StringBuilder sb = new();
        AddSection(sb, nameof(checklist.Yes), checklist.Yes);
        AddSection(sb, nameof(checklist.Ask), checklist.Ask);
        AddSection(sb, nameof(checklist.No), checklist.No);

        return sb.Length > 0
            ? sb.ToString().TrimEnd()
            : "No license permissions provided.";
    }

    private static void AddSection(StringBuilder builder, string sectionName, string[]? values)
    {
        builder.Append(sectionName)
               .AppendLine(":");

        if (values is null or { Length: 0 })
        {
            builder.Append(' ', 4)
                   .AppendLine("-")
                   .AppendLine();
            return;
        }

        foreach (string val in values)
        {
            builder.Append(' ', 4)
                   .Append("* ")
                   .AppendLine(val);
        }

        builder.AppendLine();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
