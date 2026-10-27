# TexCore Business Flow

Verify flow in this order:

1. Business node Prefab under Assets/Resources/BusinessPrefabs.
2. Aa_Node.selfDatas[0].
3. Ordered Aa_Node.childData names.
4. Topic Prefab existence under Assets/Resources/TopicPrefabs.
5. Topic data arrays.
6. Scene step tree alignment.

Do not infer topic order from object names alone. Prefer generated config and serialized Prefab data.
