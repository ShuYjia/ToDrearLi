# Purple Object Diagnosis

A Unity object may appear pink or purple when:

1. The Shader is missing.
2. Unity falls back to Hidden/InternalErrorShader.
3. The Shader is unsupported in the current render pipeline.
4. A URP/HDRP material is used in a Built-in pipeline project, or the reverse.
5. The material reference is missing/null and Unity preview renders the slot as error color.

Do not classify a material as pipeline-broken only because:

- the material color is magenta
- the object is hidden
- the Renderer is disabled
- the Mesh is missing and cannot draw
- static GUID search misses package assets under Library/PackageCache
