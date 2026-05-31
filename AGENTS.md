# AGENTS.md

OpenCode instructions for this Unity VR Interaction Toolkit (XRI) starter kit.

## Environment

- **Unity 6000.4.0f1** — no CLI build/test/lint. All work is in the Unity Editor.
- **XRI v3.4.0**, URP 17.4.0, Input System 1.19.0
- Open project via Unity Hub or Editor; there are no npm/make scripts.
- Local LM via LM Studio at `http://localhost:1234/v1` (model: `qwen/qwen3.6-35b-a3b`). Config in `.opencode/opencode.json`.

## Code location

All custom code: `Assets/XRI Starter Kit/Assets/`

| Directory | What it is |
|---|---|
| `Features/Hand Poser/` | PoseScriptableObject assets, `HandPoser.cs`, `HandAnimator.cs`, `XRHandPoser.cs` |
| `Features/Distance Grabbing/` | `DistanceGrabber.cs` (RayCast+SphereCast Alyx-style grab) |
| `Features/Inventory/` | `InventoryManager.cs`, `InventorySlot.cs`, cross-system static events |
| `Features/Climbing/` | `PlayerClimbingXR.cs`, `ClimbingStamina.cs`, `ZiplineController.cs` |
| `Interactables/Guns/` | `ProjectileWeapon.cs` base class, magazines, recoil, ammo |
| `Interactables/Door/`, `Archery/`, `FireHand/`, `Grenades/`, `FlashLight/`, `JoystickLever/` | Specific interactables |
| `Scripts/Reusable/` | ~36 utility MonoBehaviours (`ObjectSpawner`, `DestroyAfterTime`, `FloatSO`, etc.) |
| `Scripts/Player/` | Player mechanics (`PlayerCrouch.cs`, `TeleportRayEnabler.cs`) |
| `MiniGames/` | Target shooting, bowling, zombie game, forklift |
| `Official Unity Assets/XRI/` | Official XRI scripts (copy-pasted: `XRSlider`, `XRLever`, `XRKnob`, `KeyLockSystem/`) |
| `WIP/` | Work-in-progress, not integrated (`GrapplingGun.cs`, `HookCollision.cs`) |

**Assembly definition:** `Assets/XRI Starter Kit/MikeNspiredXRIStarterKitr.Runtime.asmdef`
**Primary namespace:** `MikeNspired.XRIStarterKit`

## Architecture notes

- **Hand Poser** — pose transitions driven by `PoseScriptableObject` assets. No Animator controllers or string-based animation names. Left/right hands tracked independently.
- **Inventory** — `InventoryManager` polls hand proximity every 0.1s. Cross-system communication via static events (`OnLeftSlotHoverBegan/Ended`). Each slot is a standalone `InventorySlot` component.
- **Distance Grabber** — attaches to XRI's `NearFarInteractor`. Line visual via `DistanceGrabberLineBender`.
- **Weapons** — `ProjectileWeapon.cs` fires `UnityEvent`s (`BulletFiredEvent`, `OutOfAmmoEvent`) for decoupled audio/VFX. Magazine attachment via `MagazineAttachPoint.cs`.
- **Shared interfaces:** `IDamageable`, `IImpactType`, `IKeychain`

## Code style (from `.cursorrules.txt`)

```csharp
// Naming
private int m_VariableName;        // private fields (m_ prefix)
private const int c_MaxItems = 10; // constants (c_ prefix)
private static int s_Count;        // statics (s_ prefix)
public int PropertyName { get; }   // properties (PascalCase)
public void MethodName() {}        // methods (PascalCase)
public void Method(int _arg) {}    // arguments (_ prefix)
int temporaryVariable;             // local vars (camelCase)

// Structure — always use #regions
#region Constants
#region Private Fields
#region Public Properties
#region Unity Lifecycle
#region Private Methods
#region Public Methods

// Inspector
[SerializeField] private float m_Speed;
[SerializeField, Range(0f, 1f)] private float m_Chance;

// Editor-only
#if UNITY_EDITOR
[ContextMenu("Debug")]
private void DebugInfo() { ... }
#endif
```

Key patterns:
- `TryGetComponent` over `GetComponent` where null is possible
- `ScriptableObject` for shared/configurable data
- `InputActionReference` for all input bindings
- `TextMeshPro` for all text
- Object pooling for projectiles/effects
- `Coroutine` for time-based sequences

## Gotchas

- `Official Unity Assets/XRI/` contains copied XRI scripts — do not edit these; they should be updated via package updates instead.
- `WIP/` scripts are not wired into the scene.
- Sirenix Odin Inspector is in `Assets/Plugins/` — a paid package. Code should not depend on Odin-specific features.
- The project has no automated tests — verification is manual in-editor play mode.
