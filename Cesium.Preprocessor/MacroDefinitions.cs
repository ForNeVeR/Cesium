using System.Collections.Immutable;

namespace Cesium.Preprocessor;

public record ObjectMacroDefinition(string Name) : MacroDefinition(Name);
public record FunctionMacroDefinition(string Name, ImmutableArray<string> Parameters, bool hasEllipsis = false) : MacroDefinition(Name);
public record MacroDefinition(string Name);
public record ParameterTypeList(ImmutableArray<string> Parameters, bool HasEllipsis = false);
