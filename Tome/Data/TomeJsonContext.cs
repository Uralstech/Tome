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

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tome.Data;

[JsonSerializable(typeof(TomeMeta))]
[JsonSerializable(typeof(TomeManifest))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata,
                             Converters = [typeof(UInt128JsonConverter)])]
internal sealed partial class TomeJsonContext : JsonSerializerContext;

// Based on:
// https://github.com/dotnet/runtime/blob/4b73d784df79a696fd0487b2e66f9e51efc955f3/src/libraries/System.Text.Json/tests/System.Text.Json.Tests/Serialization/ConvertersForUnsupportedTypesFunctionalTests.cs

internal sealed class UInt128JsonConverter : JsonConverter<UInt128>
{
    public override UInt128 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string value = reader.HasValueSequence
            ? Encoding.UTF8.GetString(reader.ValueSequence)
            : Encoding.UTF8.GetString(reader.ValueSpan);

        return UInt128.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
    }

    public override UInt128 ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Read(ref reader, typeToConvert, options);
    
    public override void Write(Utf8JsonWriter writer, UInt128 value, JsonSerializerOptions options) =>
        writer.WriteRawValue(value.ToString(CultureInfo.InvariantCulture.NumberFormat));

    public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] UInt128 value, JsonSerializerOptions options) =>
        writer.WritePropertyName(value.ToString(CultureInfo.InvariantCulture.NumberFormat));
}