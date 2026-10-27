# TexCore Step Tree

Treat TexCore_StepObj.interacterObjs as a strict three-level tree:

1. NodeObjs
2. TopicObjs
3. StepObjs

For each step object, verify:

- component derives from or behaves as TexCore_InteracterBase
- stepIndex is present and continuous within its topic
- collider exists when click or physics input is required
- target transform exists when target snapping or guidance is enabled
- animator and animation name exist when animation flags are enabled
