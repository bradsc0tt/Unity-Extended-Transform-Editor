# Changelog

## [2.0.0] - 2026-03-17

### Changed
- Complete rewrite from IMGUI to UIToolkit
- Restructured as a Unity package (`com.bs3d.extended-transform`)
- Minimum Unity version is now 2022.3 LTS
- All code namespaced under `BS3D.ExtendedTransform`
- Editor assembly defined via `.asmdef` (editor-only)
- Icons loaded via package-relative paths instead of `Resources.Load`
- Dropped GUISkin asset — all styling via USS
- Fixed potential divide-by-zero in uniform scale when X axis is zero
- Fixed rotation euler angle display drift by caching quaternion values

## [1.0.0] - 2017

### Added
- Custom Transform inspector with reset buttons
- Uniform scale lock mode
- Snap to ground utility
- Random rotation utility
- Thin inspector mode for narrow windows
- Multi-object editing support
