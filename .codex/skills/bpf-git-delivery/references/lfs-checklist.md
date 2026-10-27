# LFS Checklist

Run git lfs status when touching:

- .unity
- .prefab
- .fbx
- textures
- videos
- binary docs
- other LFS-tracked assets

Before commit, run git lfs fsck when LFS assets are staged.

If LFS fsck fails, do not commit until the failure is explained or fixed.
