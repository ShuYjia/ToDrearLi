// Copyright (c) Mixed Reality Toolkit Contributors
// Licensed under the BSD 3-Clause

using System;
using System.Collections.Generic;
using Unity.XR.Management.AndroidManifest.Editor;
using UnityEngine.XR.OpenXR;

namespace MixedReality.Toolkit.Examples.Build
{
    /// <summary>
    /// BPF PICO 发布路径不再注入 Quest 专用 overlay keyboard 特性。
    /// </summary>
    internal class AndroidManifestPatcher : IAndroidManifestRequirementProvider
    {
        ManifestRequirement IAndroidManifestRequirementProvider.ProvideManifestRequirement() => new()
        {
            SupportedXRLoaders = new HashSet<Type>
            {
                typeof(OpenXRLoader)
            },
            NewElements = new List<ManifestElement>
            {
                new ManifestElement
                {
                    ElementPath = new List<string> { "manifest", "application", "meta-data" },
                    Attributes = new Dictionary<string, string>
                    {
                        { "name", "pvr.app.type" },
                        { "value", "vr" }
                    }
                },
                new ManifestElement
                {
                    ElementPath = new List<string> { "manifest", "application", "meta-data" },
                    Attributes = new Dictionary<string, string>
                    {
                        { "name", "enable_vst" },
                        { "value", "1" }
                    }
                },
                new ManifestElement
                {
                    ElementPath = new List<string> { "manifest", "uses-permission" },
                    Attributes = new Dictionary<string, string>
                    {
                        { "name", "android.permission.CAMERA" }
                    }
                }
            }
        };
    }
}
