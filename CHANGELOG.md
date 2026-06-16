# Changelog

All notable changes to this package are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-06-16

### Added

- "Apply Play Mode Values" entry in every component's context (gear) menu.
- Capture component values during Play Mode and re-apply them on exit as a
  single undoable operation.
- Works on any `Component` (Transform, Rigidbody, custom scripts, ...).
