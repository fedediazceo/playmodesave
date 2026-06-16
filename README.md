# Apply Play Mode Values

Tweak a component during **Play Mode**, then keep those values when you stop —
without copying anything by hand.

While playing, open any component's **⋮ (gear) context menu** and choose
**Apply Play Mode Values**. The component's current values are captured, and the
moment you exit Play Mode they are written back to the object (as a single,
undoable change). Works on any component: `Transform`, `Rigidbody`, colliders,
your own `MonoBehaviour` scripts, etc.

## Install (Git URL)

In Unity: **Window → Package Manager → + → Add package from git URL…** and paste:
(Use this version, it's the preview one, but it's ok)

**NOTE**: If you have a compile error, please, PLEASE read the note about the cool save button

```
https://github.com/fedediazceo/playmodesave.git#v0.1.1-preview.1
```

Or add it directly to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.fedediaz.applyplaymodevalues": "https://github.com/fedediazceo/playmodesave.git#v0.1.1-preview.1"
  }
}
```

## Usage

1. Save the scene (capture requires a saved scene).
2. Enter Play Mode.
3. Adjust the component you care about.
4. Right-click the component header (or use the ⋮ menu) → **Apply Play Mode Values**.
5. Exit Play Mode — the captured values are applied and can be undone with Ctrl+Z.

<img width="1272" height="937" alt="SavePlayMode" src="https://github.com/user-attachments/assets/7415006a-0c2e-44c1-8489-800517e5da5b" />

## Optional: the much cooler header button

<img width="305" height="74" alt="image" src="https://github.com/user-attachments/assets/405078f5-c730-41fb-b9e6-3a3fc1028922" />

In addition to the context-menu entry, the package adds a small **save icon to the
component header**, next to the preset/help buttons, that does the same capture.

This part relies on an **internal** Unity attribute (`EditorHeaderItem`) that is only
reachable from an assembly named `Unity.InternalAPIEditorBridge.NNN`. Those `NNN`
slots are a shared, finite pool (`001`–`025`) and other packages claim them too, so
a collision is possible.

- **If you hit a "duplicate assembly name" compile error**, open
  `Editor/HeaderItem/Unity.InternalAPIEditorBridge.024.asmdef` and change `024` to
  another free number. That's the only change needed.
- **If you don't want the header button at all**, delete the
  `Editor/HeaderItem/` folder. The context-menu entry keeps working without it.

## Notes & limitations

- The menu entry is disabled outside Play Mode and on unsaved scenes.
- Captured values are stored per editor session (`SessionState`); they apply on
  the next exit from Play Mode and are then cleared.
- Object references that only exist at runtime can't be resolved back in Edit
  Mode and will come through as null. Asset and persistent scene references are fine.

## Requirements

- Unity 2021.3 or newer (uses C# 9 pattern syntax).

## License

MIT — see [LICENSE.md](LICENSE.md).
