# WPF .NET 10 – Project Rules & Best Practices

This document defines rules and guidelines for any contributor or AI agent working on WPF projects targeting .NET 10 in this repository.

## 1. General Architecture

- **MVVM first**  
  - Prefer Model–View–ViewModel.  
  - Keep UI logic in Views and view-specific helpers; move business logic and state to ViewModels or services.
- **Separation of concerns**  
  - Views (XAML + code-behind) should only contain presentation logic, wiring, and simple event handlers.  
  - Avoid direct data access, networking, or complex computation in code-behind.
- **Dependency injection**  
  - When adding services, design them as interfaces and register them in the DI container (if present), or via a simple service locator as a fallback.
- **Async by default**  
  - For I/O-bound or potentially long-running work, prefer `async`/`await` and `Task`-based APIs.

## 2. XAML & Styling

- **Use semantic resources**  
  - Do not hardcode colors, brushes, or fonts in XAML.  
  - Use application-level `ResourceDictionary` entries with semantic names (e.g. `ApplicationBackgroundBrush`, `OverlayBackgroundBrush`, `TextFillColorPrimaryBrush`, `TitleBarButtonHoverBackgroundBrush`).
- **Theme-aware design**  
  - All brushes must be defined as `DynamicResource` in UI elements to support theme switching at runtime.  
  - Define Light/Dark variations in separate resource dictionaries that override the same keys.
- **Naming conventions**  
  - Use `x:Name` for elements that need to be accessed from code-behind or bindings (e.g. `StatusText`, `LoadItem`).  
  - Use clear style keys: `TitleBarButtonStyle`, `TitleBarCloseButtonStyle`, etc.
- **Reuse styles & templates**  
  - Avoid duplicating ControlTemplates and Styles. Extract common patterns into shared resources.

## 3. MVVM Best Practices

- **ViewModels own state and logic**  
  - Expose UI state via properties implementing `INotifyPropertyChanged`.  
  - Keep domain logic and long-running operations out of Views.
- **Commands instead of code-behind**  
  - Use `ICommand` (e.g. `RelayCommand`, `DelegateCommand`) for user actions.  
  - Bind buttons and menu items to commands on the ViewModel instead of handling `Click` in code-behind when possible.
- **One View ↔ one ViewModel**  
  - Each Window/Page/UserControl should have a clear primary ViewModel.  
  - Set it via `DataContext` in XAML or in a minimal code-behind constructor.
- **No direct service calls from XAML**  
  - Views should not directly reach into services.  
  - ViewModels should depend on abstractions (interfaces) injected via DI or factories.
- **Navigation & dialogs via services**  
  - Use navigation or dialog services that are called from ViewModels instead of directly instantiating Windows from ViewModels.

## 4. Data Binding & x:Bind

- **Use classic WPF `{Binding}`**  
  - This project is pure WPF; use `{Binding ...}` for data and command bindings.  
  - Do **not** use `x:Bind` (it is a WinUI/UWP feature and not supported in standard WPF).
- **Strong, explicit bindings**  
  - Prefer explicit `Path`, `Mode`, `UpdateSourceTrigger`, and `Converter` where needed.  
  - Use `ElementName` and `RelativeSource` bindings instead of code-behind glue.
- **Bind to ViewModel, not controls**  
  - Bind UI elements to ViewModel properties and collections (`ObservableCollection<T>`).  
  - Avoid reading or writing UI element properties from ViewModels.
- **Commands over event handlers**  
  - Use `{Binding SomeCommand}` on `Button.Command` etc.  
  - Reserve event handlers in XAML/code-behind for view-only concerns (dragging, window chrome, overlay toggles, etc.).
- **Validation & errors**  
  - Use binding validation (`IDataErrorInfo`, `INotifyDataErrorInfo`) or validation rules for user input.  
  - Surface errors via bound properties rather than MessageBox calls in code-behind.

## 5. Code-Behind Rules

- **Minimize logic in code-behind**  
  - Code-behind can contain:
    - Simple UI event handlers.  
    - Window-level operations (drag, minimize/maximize, close, overlay toggle).  
    - Dispatch calls to ViewModels or services.
- **Thread safety for UI updates**  
  - Never update WPF UI elements from background threads directly.  
  - Use `Dispatcher.CheckAccess()` and `Dispatcher.BeginInvoke` when updating UI (e.g. `StatusText.Text`).
- **Safe event handlers**  
  - Event handlers that call `DragMove()` or other stateful window operations must be wrapped in `try/catch` for `InvalidOperationException` (and optionally a general `Exception` catch) to avoid crashing the UI thread.
  - Exception handlers in UI events should log and return gracefully; they must not rethrow.
- **File-scoped namespaces**  
  - Prefer file-scoped namespaces (`namespace MyApp;`) instead of block-scoped.

## 6. Error Handling & Logging

- **Do not crash the UI thread**  
  - Any operation triggered by user interaction should handle foreseeable exceptions locally.  
  - Use conservative `try/catch` blocks around interactions with window state, overlays, and drag operations.
- **Logging**  
  - Prefer a central logging abstraction if available (e.g., `ILogger`).  
  - If no logger exists, use `System.Diagnostics.Debug.WriteLine` for non-critical diagnostics.  
  - Do not spam logs; log only meaningful errors and state transitions.

## 7. Theming & Resources

- **Centralized resources**  
  - All shared colors, brushes, and fonts must be defined under `Application.Resources` or in merged `ResourceDictionary` files.  
  - UI elements should reference these via `{DynamicResource ...}`.
- **Light/Dark mode**  
  - When adding new UI, provide semantic brushes that can be overridden by theme dictionaries.  
  - Do not assume a specific background (e.g., always dark) in code or layout.

## 8. Performance & Responsiveness

- **No blocking on UI thread**  
  - Avoid `.Result`, `.Wait()`, or long-running loops on the UI thread.  
  - Use `async void` only for event handlers; elsewhere prefer `async Task`.
- **Virtualization and bindings**  
  - For lists or grids, enable UI virtualization where appropriate.  
  - Use bindings instead of manual UI updates when the data model is reactive (INotifyPropertyChanged / ObservableCollection).

## 9. Overlay & Window Management

- **Overlay windows**  
  - Overlay windows should be topmost, borderless, and use semantic overlay brushes.  
  - Drag operations in overlays must be protected with `try/catch` around `DragMove()`.
- **Main window chrome**  
  - Custom title bars must:
    - Support drag to move the window.  
    - Handle minimize/maximize/close via explicit event handlers.  
    - Avoid crashes by wrapping window state changes in safe logic.
- **Toggle behavior**  
  - UI elements that open/close overlays (e.g. a button) should behave as toggles and keep the logical state in sync (button content, internal references, and overlay lifetime).

## 10. API & Library Usage

- **Stick to WPF APIs**  
  - Do not mix WPF with WinUI 3 or UWP-specific APIs (`Microsoft.UI.Xaml`, `DispatcherQueue`, etc.) in this project.  
  - Use `System.Windows`, `System.Windows.Controls`, `System.Windows.Input`, and `Dispatcher` APIs.
- **Versioning**  
  - Target .NET 10 and keep NuGet packages reasonably up to date, avoiding breaking changes without prior review.

## 11. Testing & Validation

- **Unit tests**  
  - Business logic and ViewModels should be unit-testable and covered by tests when feasible.  
  - Avoid direct dependencies on UI types in testable code.
- **Manual UI testing**  
  - When changing window chrome, overlays, or threading behavior, manually verify:
    - Dragging windows does not crash.  
    - Overlays open/close correctly and clean up references.  
    - No cross-thread exceptions occur when updating the UI from background operations.

## 12. Git & Commits

- **Small, focused commits**  
  - Group related changes: UI layout, behavior, theming, refactors.  
  - Avoid mixing large refactors with feature work in the same commit.
- **Conventional commit style (recommended)**  
  - Use prefixes like `feat:`, `fix:`, `refactor:`, `chore:`, `test:`.

---

Any future WPF/.NET 10 changes in this repository should follow these rules unless there is a clear, documented reason to deviate.
