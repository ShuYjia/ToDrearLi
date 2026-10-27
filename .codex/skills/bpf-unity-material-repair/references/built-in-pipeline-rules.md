# Built-In Pipeline Rules

The BPF project is treated as Built-in Render Pipeline unless live Unity settings prove otherwise.

For Built-in pipeline:

- Standard shaders are expected.
- VolumetricLine BuiltInRenderPipeline shader includes must exist when VolumetricLine materials are used.
- Do not introduce URP/HDRP shader variants as a default repair.
- MRTK and Graphics Tools materials may come from packages; package ownership is not a missing reference by itself.
