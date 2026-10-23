# Changelog

All notable changes to this package are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the versioning follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

A version is dated once it is tagged. Until then it carries no date and its entries may still
change.

## [0.1.0] - 2026-10-24

Initial release.

### Added

- `MoohaLoader` as the single entry point: one source registered per request type, placeholder and
  default resources registered per resource type.
- `AsyncResourceProvider`, which turns one request into a stream of placeholder, then resource or
  default. The placeholder is skipped when the resource resolves without suspending.
- Addressables sources for `SpriteAtlas`, standalone `Sprite`, `Texture2D`, and for a sprite packed
  into an atlas.
- `ResourceCacheStore`, a lease-based LRU that demotes a released resource instead of freeing it, so
  a screen the user walks back into does not reload. One budget is shared by every resource type.
- Appliers for `VisualElement` background, UI Toolkit `Image`, and `SpriteRenderer`.
- `ResourceSizeTable`, baked in the editor, because a `SpriteAtlas` cannot report its page textures
  at runtime. The bake runs on build, on play mode entry, on build target switches, and on demand;
  a build fails rather than shipping a stale table.
- `IResourceSizeCalculator` for measuring project-specific asset types, discovered by type scan.
