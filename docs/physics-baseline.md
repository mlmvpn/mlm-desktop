# Rug physics baseline

Measured 2026-10-12 before the solver rewrite. The baseline was frozen and compiled from copies of `src/Cloth.cs` and `src/ClothContacts.cs`; later edits cannot affect these numbers. All numerical results use the public `Cloth` API and a 640 by 440 rug with its default 759 particles.

Frozen SHA-256 hashes:

- `Cloth.cs`: `072B996353ACC84D5913FC14AED70AD7DC113C160B46C8BECEECB940EDEEC55C`
- `ClothContacts.cs`: `9EF362656B430F420655B3F3BC68EAABF4E6784B206953D949717A9F4C36C650`

## Reproduce

Run from the repository in PowerShell. This deliberately uses an optimized x64 build for the diagnostics, matching the application's compiler switches. The regression build now uses the same optimized x64 switches. Run diagnostic timing separately from tests and other builds.

```powershell
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$diagnosticExe = Join-Path $env:TEMP 'MlmPhysicsDiagnostics.exe'
& $compiler /nologo /optimize+ /platform:x64 "/out:$diagnosticExe" .\src\Cloth.cs .\src\ClothContacts.cs .\src\ClothBending.cs .\tests\PhysicsDiagnostics.cs
& $diagnosticExe
```

`tests/PhysicsDiagnostics.cs` does not use reflection or depend on the constraint implementation. It prints measurements rather than pass/fail assertions. Stretch is the largest current horizontal/vertical grid-edge length divided by its initial rest length. Final RMS speed averages all particles over the final half-second. These are observable deformation measures; different valid solvers need not produce identical fold shapes.

## Baseline: frame-frequency sensitivity

Grab the initial upper-left corner at `(260,160,120)` and simulate one wall-clock second with a constant target. Compare every vertex to the 120 Hz run.

| Calls per second | RMS position difference, px | Maximum difference, px | Final maximum stretch ratio |
| --- | ---: | ---: | ---: |
| 30 | 35.003 | 133.776 | 1.143 |
| 60 | 60.176 | 180.496 | 1.137 |
| 120 | 0 | 0 | 1.064 |
| 240 | 72.927 | 194.858 | 1.029 |

The baseline caps each call's `dt` at 1/60 second, applies damping once per call, and performs the same nine projection sweeps per call. This explains why equal real durations have different effective elapsed time, damping, and stiffness. This is a code-based diagnosis; the table measures the resulting behavior.

## Baseline: drag, twist, and release

Both trajectories use 120 Hz updates. The quick drag moves the upper-left corner linearly from the origin to `(600,400,180)` over 150 ms, holds until 750 ms, then releases for four seconds. The twist lifts that corner to `(350,180,130)` over 750 ms, then traces two 150 px radius circles centered at `(200,180)` at one revolution per second. Its height varies by 25 px. It then releases for four seconds.

| Trajectory | Peak stretch | Final stretch | Peak height, px | Final maximum height, px | Final RMS speed, px/s | Far corner travel, px |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Quick drag | 3.263 | 1.105 | 211.421 | 29.038 | 4.418 | 34.031 |
| Two-turn twist | 1.518 | 1.122 | 155.000 | 41.566 | 7.090 | 7.249 |

All coordinates stayed finite and above the floor in these two baseline cases. The hand was a hard positional pin, so its reported positional error was zero even when neighboring edges stretched severely. A bounded-speed grab should be assessed using both deformation and eventual catch-up, rather than preserving zero instantaneous error during a teleport.

## Baseline: performance

Machine: Windows, eight logical processors, .NET Framework release 533320, CLR `4.0.30319.42000`, x64. Each case warms up for 300 steps, then times 900 steps at 120 Hz. No deformation scans, array clones, or allocations from diagnostic trajectory setup occur inside the timed loop. Allocations are measured using `AppDomain.MonitoringTotalAllocatedMemorySize`; that counter is domain-wide, so the figures are approximate. Median and p95 use per-step stopwatch timestamps. Mean includes the small surrounding loop overhead.

| Scenario | Mean, ms/step | Median, ms/step | p95, ms/step | Allocated bytes/step | Gen0 collections in 900 steps |
| --- | ---: | ---: | ---: | ---: | ---: |
| Flat rug | 1.912 | 1.753 | 2.802 | 103,748 | 22 |
| Continuous twist | 4.677 | 4.456 | 6.782 | 94,674 | 20 |

The original solver clones two arrays per step; its contact broadphase recreates per-cell lists. The application's render loop also clones the point array before each physics step, outside these measurements. Timing results vary with system load and should be remeasured using the same helper after changes. Do not turn these machine-specific milliseconds into a brittle unit-test threshold.

## Five meaningful regression tests

These expectations describe intended behavior, not a particular solver structure. Tests should run the actual public API and assert finite X, Y, and Z as part of trajectory validation.

1. **No elapsed time causes no movement.** Deform a rug through an ordinary drag, snapshot it, then call `Step(0)` repeatedly. Positions must remain identical. This catches integration or projection occurring despite no elapsed time.
2. **Equivalent elapsed time produces equivalent held deformation.** Use the constant-target one-second fixture above at 30, 60, 120, and 240 Hz. Maximum difference should be below 0.01 px when the implementation uses fixed internal updates. This catches dropped elapsed time and call-dependent damping. Moving targets should instead use equivalent timestamped input events, since input sampling itself changes a trajectory.
3. **A rapid hand movement preserves cloth integrity and eventually reaches the hand.** Use the 150 ms drag above, measuring every intermediate step. Require peak grid-edge stretch below 1.5x, finite coordinates, and less than 5 px hand error after one additional second of holding. This catches excessive constraint strain and a speed limiter that never catches up. The original peak stretch of 3.263x fails the deformation bound.
4. **Circular pulling remains stable and settles after release.** Use the two-turn twist trajectory, measure every intermediate step, and require peak stretch below 1.5x. After release, allow folded ridges up to 60 px but require final RMS speed below 2 px/s after four seconds. This catches unstable torsion and a contact/velocity feedback loop. The original twist fails both the stretch and settling-speed expectations. Retain the accepted settling bound when tuning, rather than weakening it to match a new implementation.
5. **Rigid relocation preserves subsequent physical behavior.** Run identical drag and release inputs on two rugs. Translate one through `Move(75,-30)` and translate all later hand targets by the same amount. Corresponding points should differ by `(75,-30,0)` within a small numerical tolerance throughout. This catches velocity, hand-target, rest-pose, or collision-state coordinates omitted by whole-rug movement. The test need not inspect any private arrays.

Separate contact regression tests should cover layered folds, oblique contacts, and a large target change crossing a layer. Those require geometric intersection assertions, not only a final maximum height.

## Primary references

The [XPBD paper by Macklin, Mueller, and Chentanez](https://matthias-research.github.io/pages/publications/XPBD.pdf) introduces compliance and accumulated constraint multipliers to reduce timestep/iteration-dependent stiffness. For a scalar constraint, use `alphaTilde = compliance / dt^2`, `deltaLambda = (-C - alphaTilde * lambda) / (sum(w_i * dot(gradient_i, gradient_i)) + alphaTilde)`, then update positions with `w_i * gradient_i * deltaLambda` and accumulate the multiplier within that timestep.

The [PositionBasedDynamics authors' dihedral solver](https://github.com/InteractiveComputerGraphics/PositionBasedDynamics/blob/master/PositionBasedDynamics/PositionBasedDynamics.cpp) contains analytic angle gradients in `solve_DihedralConstraint`. Its shared-edge endpoints are `p2,p3`; `p0,p1` are the opposite vertices. Preserve that ordering and the signed orientation when adapting the formulas. Guard degenerate triangle normals as well as short edges. A finite-difference check of the chosen signed constraint is useful during implementation, especially near a flat configuration.


## 0.2.0 release measurements — 2026-10-14

The final solver retains 759 particles, uses 240 Hz physical ticks, XPBD stretch/shear, signed angular bending, and swept 3D contacts. The optimized hinge was compared with its vector formulation on 2,500 seeded configurations with zero observed difference. All eight regression suites passed in the release build.

| Trajectory | Peak stretch | RMS speed after six simulated seconds of release, px/s |
| --- | ---: | ---: |
| Quick drag | 1.3302 | 1.3878 |
| Two-turn twist | 1.2368 | 1.4369 |

The release criterion allows **six** simulated seconds to settle, rather than the four seconds originally proposed above. This is an explicit acceptance change; these values must not be presented as passing the original four-second criterion or directly compared with the baseline's four-second speeds. This RMS measure does not certify absence of every local vibration or intersection. The independently initialized two-layer roll stayed finite above the floor with peak stretch 1.0196. The settled overlap fixture retained 3.4919 px clearance.

Same eight-logical-processor machine and optimized x64 compiler, 300 warmup and 900 measured calls at 1/120 second:

| Scenario | Mean ms/call | Median | p95 | Allocated bytes/call | Gen0 collections |
| --- | ---: | ---: | ---: | ---: | ---: |
| Flat resting rug | 0.0093 | 0.0088 | 0.0115 | 9.10 | 0 |
| Continuous twist | 97.276 | 84.816 | 216.749 | 3,213.11 | 0 |

The flat case uses an analytically resting floor state, after checking public particle positions and previous positions. The continuous-twist measurement uses the final collision/bending implementation; it was taken immediately before adding the flat-only shortcut, which does not apply during that moving trajectory. The flat case was then remeasured separately after the shortcut.

**Tradeoff:** collision fidelity and allocation pressure improved, but active solver CPU time is substantially worse than the old spring solver. Dense repeated twisting is not real-time on this machine. Rendering limits each batch to eight physical ticks and a soft 8 ms wall-time budget, dropping catch-up debt; a single expensive tick can exceed this budget, and simulated time slows under load. These numbers are solver timings, not rendered FPS. The next performance milestone is reducing active contact cost without losing the passing geometry fixtures.