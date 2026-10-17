# Solar Deflect contributor guide

This repository contains **Solar Deflect**, a Windows desktop application built with .NET 8 and WPF. It provides color-target detection, configurable global hotkeys, click automation, speed testing, and a one-shot camera curve macro.

## Quick path

1. Work on a feature branch; do not develop directly on `master`.
2. Make the smallest coherent source and test changes.
3. Run the tests and build:

   ```bash
   dotnet test TriggerBot.slnx --no-restore
   dotnet build TriggerBot.slnx --no-restore
   ```

4. Publish and replace the distributable executable:

   ```bash
   dotnet publish TriggerBot.App/TriggerBot.App.csproj \
     -c Release -r win-x64 --self-contained true \
     -p:PublishSingleFile=true \
     -p:IncludeNativeLibrariesForSelfExtract=true
   cp TriggerBot.App/bin/Release/net8.0-windows/win-x64/publish/TriggerBot.exe \
      dist/TriggerBot-win-x64/TriggerBot.exe
   ```

5. Inspect the result, commit the source/docs/artifact changes that belong in Git, then push the branch to GitHub.

## Repository map

| Path | Responsibility |
| --- | --- |
| `TriggerBot.App/` | WPF shell, `MainWindow`, navigation, settings screens, and application lifecycle. |
| `TriggerBot.App/ViewModels/` | UI-facing state and coordination with the detector. |
| `TriggerBot.Core/Detection/` | Detector contracts, settings validation, colors, points, and platform-neutral logic. |
| `TriggerBot.Windows/Input/` | Windows global hotkeys, `SendInput`, mouse movement, clicks, and runtime input state. |
| `TriggerBot.Windows/ScreenCapture/` | Windows pixel capture. |
| `TriggerBot.Configuration/` | JSON persistence under the user's local application-data directory. |
| `TriggerBot.Core/SpeedTesting/` | Speed-test domain logic. |
| `TriggerBot.Tests/` | xUnit tests for core behavior and validation. |
| `dist/TriggerBot-win-x64/` | User-facing Windows distribution directory. |
| `odd/tasks/` | ODD feature specifications, task evidence, and user-requested change logs. |

## Application behavior

### Navigation

`MainWindow.xaml` contains the dashboard, Ajustes, Curva macro, speed test, and help views. `MainWindow.xaml.cs` maps each navigation button's `Tag` to a view, title, subtitle, and active-state style.

The **Curva macro** sidebar view is intentionally limited to the macro's own configuration:

- Macro hotkey, default `XButton1`.
- Macro sensitivity.
- The horizontal movement is calculated as `1080 / sensitivity`, followed by the inverse movement.
- It runs once per hotkey press.

The **Ajustes** view contains detector timing, color tolerance, spam timing, and the configurable action buttons for color selection and spam. Color detection and the camera macro are independent features; detecting a color does not automatically run the macro.

### Settings and persistence

`DetectionSettings` is an immutable record. Changes must be validated before persistence. `DetectionConfigurationStore` serializes settings to the existing local JSON configuration path. When adding a setting:

1. Add it to `DetectionSettings` with a safe default.
2. Validate it in `DetectionSettings.Validate()`.
3. Expose it through `MainViewModel` when the UI needs it.
4. Update the relevant UI initialization and apply handlers.
5. Add tests for defaults and invalid values.

### Hotkeys and input

`GlobalHotkeyService` registers the fixed detector hotkey and the configurable color, spam, and camera-macro hotkeys. Supported names are defined by `HotkeyVirtualKey`. Windows mouse movement and clicks are sent by `WindowsDetectionRuntime` through `SendInput`.

When changing hotkeys:

- Keep registration IDs stable.
- Validate names before applying them.
- Re-register only after the settings are valid.
- Preserve the stopped-detector requirement for settings changes.
- Update user-facing help or labels when a shortcut changes.

## Development flow

### 1. Authorize and understand

Confirm that the request authorizes source changes. Read the relevant existing code before editing. If a product behavior is ambiguous, ask one focused question before implementation.

### 2. Track meaningful work

For multi-step work, create or update an ODD task file under `odd/tasks/`. Record the user's exact requirement in the `## Log` section and keep task-to-commit evidence current. Keep the visible Pi todo list synchronized with that file.

### 3. Implement narrowly

Keep code, tests, UI, and documentation for one behavior together. Avoid unrelated formatting changes. Use English for source identifiers and repository-facing technical documentation unless the project explicitly requires another convention.

### 4. Verify

Run the focused tests first when practical, then the full suite and build:

```bash
dotnet test TriggerBot.slnx --no-restore
dotnet build TriggerBot.slnx --no-restore
```

For UI changes, also inspect the generated layout at the supported minimum window size. Pay special attention to horizontal overflow, text wrapping, and whether every new named control is initialized during startup.

### 5. Commit

Use a Conventional Commit message, for example:

```text
git add TriggerBot.App/MainWindow.xaml TriggerBot.App/MainWindow.xaml.cs TriggerBot.Tests AGENTS.md odd/tasks/<feature>.md
git commit -m "feat: describe the behavior changed"
```

Do not stage unrelated user files such as local documentation or credentials. Check the staged file list before committing.

## GitHub upload workflow

The configured remote is `origin` and points to the project's GitHub repository. Push the current feature branch; do not force-push shared history.

```bash
git status --short --branch
git diff --check
git diff --cached --stat
git remote -v
git push -u origin "$(git branch --show-current)"
```

If the branch already tracks a remote branch, use:

```bash
git push
```

After pushing, verify the result:

```bash
git status --short --branch
git log --oneline -3
```

A successful local commit is not the same as a GitHub upload. Report both the commit hash and the push result. Pushes, pull requests, merges, and releases are separate actions; only perform them when explicitly requested.

## Creating the Windows executable

The supported distributable is a self-contained, single-file Windows x64 executable. Always publish from the application project, not from a class-library project:

```bash
dotnet publish TriggerBot.App/TriggerBot.App.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

The publish output is:

```text
TriggerBot.App/bin/Release/net8.0-windows/win-x64/publish/TriggerBot.exe
```

Replace the distribution executable only after publish succeeds:

```bash
cp TriggerBot.App/bin/Release/net8.0-windows/win-x64/publish/TriggerBot.exe \
   dist/TriggerBot-win-x64/TriggerBot.exe
sha256sum \
  TriggerBot.App/bin/Release/net8.0-windows/win-x64/publish/TriggerBot.exe \
  dist/TriggerBot-win-x64/TriggerBot.exe
```

The two hashes must match. The `dist` executable is a generated delivery artifact; source and documentation changes are what normally belong in Git. If the repository's release policy requires tracking the binary, verify that policy before staging it because generated binaries can be large.

## Completion checklist

- [ ] The requested behavior is implemented without unrelated changes.
- [ ] Settings have safe defaults and validation.
- [ ] Existing and new behavior have tests where applicable.
- [ ] `dotnet test` passes.
- [ ] `dotnet build` passes with no warnings/errors.
- [ ] The Release executable was published for `win-x64`.
- [ ] `dist/TriggerBot-win-x64/TriggerBot.exe` was replaced and hashes match.
- [ ] The source/docs commit has a Conventional Commit message.
- [ ] The feature branch was pushed to GitHub when requested.
- [ ] The final report names verification results, commit hash, push result, and executable path.

## Safety notes

- Do not commit secrets, local configuration, credentials, or unrelated untracked files.
- Do not use `git reset --hard`, force-push, or delete files without explicit authorization.
- Do not claim that a change is on GitHub until `git push` succeeds.
- Do not claim that an executable is refreshed until the destination hash matches the publish output.
