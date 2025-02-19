// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.Solution.Metadata;

/// <summary>This attribute is only used by the Cesium test infrastructure.</summary>
public class SolutionMetadataAttribute : Attribute
{
    public string SourceRoot { get; }
    public string ArtifactsRoot { get; }
    public string VersionPrefix { get; }

    public SolutionMetadataAttribute(string sourceRoot, string artifactsRoot, string versionPrefix)
    {
        SourceRoot = sourceRoot;
        ArtifactsRoot = artifactsRoot;
        VersionPrefix = versionPrefix;
    }
}
