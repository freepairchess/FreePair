using System.Collections.Generic;
using System.Text.Json.Serialization;
using FreePair.Core.Tournaments;

namespace FreePair.UscfEngine;

[JsonSerializable(typeof(IReadOnlyList<PairingAnnotation>))]
[JsonSerializable(typeof(List<PairingAnnotation>))]
[JsonSerializable(typeof(PairingAnnotation))]
internal partial class AnnotationsJsonContext : JsonSerializerContext;
