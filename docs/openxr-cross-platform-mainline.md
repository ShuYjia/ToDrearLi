# BPF OpenXR Cross-Platform Baseline Archive

> Status: superseded historical baseline
> Original baseline date: 2026-06-05
> Superseded by: [current-project-baseline.md](./current-project-baseline.md)

This document previously defined a pure OpenXR, cross-platform-first route for
the BPF business mainline. It no longer describes the current Android project
configuration.

## Why It Was Superseded

Current repository evidence shows:

- Android XR Plug-in Management lists ARCore Loader and PXR Loader.
- Standalone and WSA list OpenXR Loader.
- PICO XR 3.4.0 is embedded under `Packages/PICO`.
- PICO support, PICO OpenXR extensions and PICO controller profiles are present
  in `Assets/XR/Settings/OpenXR Package Settings.asset`.
- the former validation scripts under `Assets/App/Editor` no longer exist.

Consequently, the earlier statements “Android is pure OpenXR,” “the route is not
based on PXR,” and “no PICO-specific loader is required” must not be used for
current implementation or build decisions.

## Historical Design Intent

The superseded baseline aimed to keep platform-neutral MRTK/XRI behavior by:

- using project-owned MRTK profiles;
- avoiding Windows speech in Android profiles;
- enabling portable hand and controller profiles;
- disabling Quest-specific Android feature switches;
- avoiding developer-local signing state in the shared repository.

These goals remain useful review criteria, but they do not determine which XR
loader is active.

## Current Interpretation

- Android/PICO work starts from PXR loader behavior.
- Standalone/WSA OpenXR settings remain relevant to those build targets.
- Feature assets must be checked by both list membership and `m_enabled`; their
  mere presence is not proof that a feature is active.
- Current scene and build facts live in
  [current-project-baseline.md](./current-project-baseline.md).
