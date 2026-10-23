# MoohaDev

Development project for **Mooha**, an async resource loading library for Unity. What it does and
how to use it: [package README](Packages/dev.headwind.mooha/README.md).

## Requirements

| | How it resolves |
|---|---|
| **Addressables** 2.9.1+ | Declared as a dependency; UPM installs it from the Unity registry. |
| **UniTask** 2.5.x | **Not declared** — it lives on OpenUPM, which UPM cannot reach. The consuming project must install it. |

UniTask is in the public surface (`UniTask<ResourceHandle<T>>`), so it is mandatory. Any install
path works as long as the `UniTask`, `UniTask.Linq` and `UniTask.Addressables` assemblies exist.
Via OpenUPM, add to the consuming project's `Packages/manifest.json`:

```json
"scopedRegistries": [
  { "name": "OpenUPM", "url": "https://package.openupm.com", "scopes": ["com.cysharp.unitask"] }
]
```

## Install

The package sits in a subfolder, so the git URL names it. Pin a tag to stay off the moving branch.

```json
"dev.headwind.mooha": "https://github.com/95D/MoohaDev.git?path=/Packages/dev.headwind.mooha#v0.1.0"
```

## License

MIT. See [LICENSE.md](Packages/dev.headwind.mooha/LICENSE.md).
