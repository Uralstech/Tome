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

public sealed class CreditsGroupsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IReadOnlyList<CreditsGroup> { Count: > 0 } creditsGroups)
            return "No credits to list.";

        StringBuilder sb = new();
        foreach (CreditsGroup group in creditsGroups)
            AddGroup(sb, group);

        return sb.Length > 0
            ? sb.ToString().TrimEnd()
            : "No credits to list.";
    }

    private static void AddGroup(StringBuilder builder, CreditsGroup group)
    {
        if (group.Authors is not { Count: > 0 })
            return;

        builder.Append(group.GroupName)
               .AppendLine(":");

        foreach (CreditedAuthor author in group.Authors)
        {
            builder.Append(' ', 4)
                   .Append("* ")
                   .Append(author.Name);

            if (!string.IsNullOrEmpty(author.Role))
                builder.Append(": ").Append(author.Role);

            builder.AppendLine();

            if (!string.IsNullOrEmpty(author.ProfileUrl))
                builder.Append(' ', 8)
                       .Append("> ")
                       .AppendLine(author.ProfileUrl);

            if (!string.IsNullOrEmpty(author.OtherUrl))
                builder.Append(' ', 8)
                       .Append("> ")
                       .AppendLine(author.OtherUrl);
        }

        builder.AppendLine();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
