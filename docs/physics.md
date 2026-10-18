# Rug physics

The desktop uses a fixed 240 Hz physical clock, independent of the WPF render clock. The render loop bounds catch-up work instead of running an unbounded loop. Zero or invalid elapsed time does not alter the cloth.

## Material and input

The desktop uses a 13 × 9 particle grid; dense regression fixtures retain 33 × 23. Stiff warp/weft distances and one compliant, mesh-aligned shear diagonal per quad preserve fabric length while signed dihedral hinges allow folds. A second crossing diagonal was removed because it braced each quad against folding. XPBD multipliers reset each physical tick. A separate compression and tension limiter prevents accumulation of extreme stretch under sustained input. Stretch, bending, floor constraints and cached contacts are relaxed together, retaining XPBD multipliers throughout each physical tick.

The central 50% rectangle moves the whole rug with plain drag; the surrounding area, including decorative corners, folds locally. The mouse drives a bounded-speed hand target. Travel per tick is limited relative to the mesh spacing, preventing a single mouse event from teleporting a corner across the rug. A soft local grip distributes the pull across nearby particles. Wheel input while holding an edge rotates that grip in 15-degree increments; the solver eases into the requested angle. Grabbing preserves the offset to the picked fabric point.

The floor has no bounce and dissipates tangential motion. Layer contacts dissipate inward velocity. The app stops updating the mesh after the released cloth settles and wakes it on interaction.

## Contacts

The collision stage uses three-dimensional vertex/triangle and edge/edge tests, including swept coplanarity tests for motion between ticks. Speculative candidates cover nearby surfaces that later material projections can bring together. True swept crossings are repaired before one bounded proximity rebuild. Coplanar sliding uses deterministic material ordering rather than manufacturing a crossing. A reused spatial hash rejects distant vertex/triangle pairs. A coherent sweep along the X axis rejects distant edge pairs without filling spatial buckets with edges. Material neighbors are excluded. Contact normals follow the fabric orientation, so near-vertical folds are handled as well as horizontal stacks.

This is an interactive discrete cloth model, not a certified intersection-free simulator. Coplanar sliding, degeneracy and arbitrary pre-existing tangles remain difficult cases. Collision regression fixtures and whole-cloth stress trajectories both belong in the acceptance suite.

## Performance

The solver reuses particle snapshots, multipliers, spatial buckets and contact arrays. It does not clone the particle grid every tick. WPF rendering receives frozen geometry collections only while the cloth is awake.

The render loop performs individual 240 Hz ticks with a 12 ms soft wall-time budget and an eight-tick hard limit per callback. A single tick can overrun the soft budget. Excess catch-up debt is discarded to keep input responsive; on slower CPUs or complicated folds this means simulation runs slower than wall time. The coarser desktop mesh reduces contact cost, with less fine-scale fold detail. This does not promise 60 FPS. Repeated circular pulls can retain slow contact creep after release; the desktop fixture does not satisfy the dense solver settling threshold.

See [baseline measurements](physics-baseline.md). Benchmarks should run sequentially on the same machine; they report solver time, not end-to-end rendered FPS.

## Algorithm references

- [XPBD: Position-Based Simulation of Compliant Constrained Dynamics](https://matthias-research.github.io/pages/publications/XPBD.pdf), Macklin, Müller and Chentanez, 2016.
- [Small Steps in Physics Simulation](https://matthias-research.github.io/pages/publications/smallsteps.pdf), Macklin et al., 2019.
- [PositionBasedDynamics dihedral formulation](https://github.com/InteractiveComputerGraphics/PositionBasedDynamics/blob/master/PositionBasedDynamics/PositionBasedDynamics.cpp), using opposite vertices and the shared edge. The signed angle uses atan2 and matching signed gradients; tests check crease resistance, mirror symmetry, and conservation of center of mass.
