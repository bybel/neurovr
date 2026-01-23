# NeuroReach VR (Meta Quest 3S, Pass-through MR)

NeuroReach VR is a **pass-through mixed-reality (MR)** prototype built in **Unity (C#)** for **Meta Quest 3S** with **Logitech MX Ink stylus** support. It provides two short, repeatable upper-limb exercises designed around *clinically meaningful primitives* (reaching/grasping and tracing) and prioritizes **simple, clinician-readable metrics** exported as **CSV** for spreadsheet review.

> Scope note: This project is a **proof-of-concept prototype**. It is intended to support structured practice and logging, not to make diagnostic or clinical-efficacy claims.

---

## Table of contents

- [System overview](#system-overview)
- [Developer setup & Quest build](./DEVELOPMENT_SETUP.md)
- [Tasks](#tasks)
  - [Task A: BalloonPop (reach-and-grasp)](#task-a-balloonpop-reach-and-grasp)
  - [Task B: PathTrace (path silhouette tracing)](#task-b-pathtrace-path-silhouette-tracing)
- [Difficulty modes (Easy vs Hard)](#difficulty-modes-easy-vs-hard)
- [Scoring](#scoring)
- [What gets logged](#what-gets-logged)
- [Metrics glossary](#metrics-glossary)
- [How to use the application](#how-to-use-the-application)
- [Data export (CSV)](#data-export-csv)
- [Implementation notes (high level)](#implementation-notes-high-level)
- [Known limitations](#known-limitations)

---

## System overview

**Hardware / runtime**
- **Headset:** Meta Quest 3S (pass-through MR)
- **Input devices:**
  - **Logitech MX Ink stylus** (6DoF) for PathTrace, including vibration (haptics)
- **Runtime:** Unity on Android (Quest), using **OpenXR** device access.

**Design goals**
- Keep users grounded in the real environment (pass-through MR), reduce disorientation and discomfort.
- Provide tasks that are **short, repeatable**, and easy to administer.
- Provide **immediate feedback** (visual urgency + haptics) and **auditable outputs** (CSV logs with interpretable metrics).

---

## Tasks

### Task A: BalloonPop (reach-and-grasp)

**Goal:** Train repeated reaching and grasping in peri-personal space under time pressure.

**Interaction**
- Balloons spawn in a forward “reach dome” in front of the user.
- A balloon is popped via a **stylus-tracked motion**, in which you are required to prick the balloon with the stylus.

**Urgency cue**
- Each balloon has a lifetime **T**.
- Balloon color shifts **blue → red** from spawn to expiry, so urgency is visible without needing text UI.

**What this task trains (practical interpretation)**
- Repetition count (number of successful reaches)
- Movement initiation under time pressure (misses / slower reaction times)
- Throughput over a block (pops per minute)

---

### Task B: PathTrace (path silhouette tracing)

**Goal:** Train fine motor control, steady stylus motion, and visuomotor error correction.

**Interaction**
- A target silhouette (e.g., line/polygon/circle/spiral) is shown on a stable drawing plane anchored in front of the user (aligned to a table surface when available).
- The user traces the target using the **MX Ink stylus**.

**Tolerance corridor + haptic error feedback**
- The target is represented as a polyline.
- A tolerance corridor of half-width **τ** (in mm) is defined around the target.
- At runtime we compute the shortest distance **d** from the stylus tip to the target polyline:
  - If **d > τ**, the stylus **vibrates in pulses** until the trace returns inside the corridor (**d ≤ τ**).
- This provides a **binary, easy-to-interpret error signal** without requiring reading text.

**What this task trains (practical interpretation)**
- Accuracy (fraction of trace inside corridor / overlap)
- Speed–accuracy trade-offs (time vs overlap)
- Error frequency and stability (out-of-bounds events and time)

---

## Difficulty modes (Easy vs Hard)

The prototype exposes **two clinician-selectable profiles per task**. Parameters are intentionally visible and adjustable rather than “opaque” adaptive difficulty.

### BalloonPop parameters (defaults)
- **Easy:** lifetime **L = 10 s**, spawn radius **r ∈ [0.35, 0.55] m**
- **Hard:** lifetime **L = 5 s**,  spawn radius **r ∈ [0.45, 0.75] m**

### PathTrace parameters (defaults)
- Primary knob: corridor half-width **τ**
  - **Easy:** **τ = 20 mm**
  - **Hard:** **τ = 8 mm**
- Secondary knobs:
  - optional **time limit T** for the trial (selectable before start)
  - **path complexity**:
    - Easy: simpler shapes (e.g., lines, polygons)
    - Hard: more complex curves (e.g., circles, spirals)

---

## Scoring

Scores are designed to be **simple, interpretable**, and aligned with the feedback mechanisms.

### BalloonPop score (per balloon)
Let:
- **T** = balloon lifetime (seconds)
- **t_p** = time since spawn when the balloon is popped (seconds)
- **S_max** = max score per balloon (default: **10**)

Normalized score:
```
s = max(0, 1 - t_p / T)
```

Points added for that balloon:
```
ΔScore = S_max * s
```

- **Fast pops** → higher score
- **Expired balloons** → add **0**

### PathTrace score (per path stage)
Let:
- **L** = target path length
- **l_trace** = traced length credited as “covered” (progress along the path)
- **S_max** = max score per stage (default: **10**)

Normalized score:
```
s = min(1, l_trace / L)
```

Points added:
```
ΔScore = S_max * s
```

- **Full completion / overlap** → higher score
- Traces outside the corridor reduce credited progress, and haptics highlight errors.

---

## What gets logged

NeuroReach VR is designed around **clinician-readable outputs**:
- **No specialized signal processing required**
- Metrics emphasize **counts**, **time**, and **accuracy/overlap**

### BalloonPop (per session block)
- Balloons spawned
- Balloons popped
- Balloons expired (missed)
- Mean reaction time (**t_p**)
- Pop rate (pops per minute)
- Total score

### PathTrace (per trial)
- Completion time (with flags for *completed* vs *timed out*)
- Overlap (% samples within corridor)
- Total out-of-corridor duration
- Out-of-bounds events (debounced count)
- Total score (progress/completion)

---

## Metrics glossary

The table below mirrors the intended “how a clinician can use it” interpretation.

<table>
  <thead>
    <tr>
      <th>Task</th>
      <th>Metric</th>
      <th>What it measures (computed)</th>
      <th>Practical interpretation</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td rowspan="4" style="text-align:center;">BalloonPop</td>
      <td>Balloons popped</td>
      <td>Count of successful pops in a block</td>
      <td>Repetition count (completed reaches)</td>
    </tr>
    <tr>
      <td>Balloons missed</td>
      <td>Count of expired balloons</td>
      <td>Time-pressure sensitivity / slower initiation</td>
    </tr>
    <tr>
      <td>Pop rate</td>
      <td>Pops per minute over block duration</td>
      <td>Throughput/efficiency over time</td>
    </tr>
    <tr>
      <td>Score</td>
      <td>&Sigma; S<sub>max</sub> * max(0, 1 - t<sub>p</sub> / T)</td>
      <td>Rewards faster responses under time pressure</td>
    </tr>
    <tr>
      <td rowspan="4" style="text-align:center;">PathTrace</td>
      <td>Overlap (%)</td>
      <td>Fraction of samples within corridor</td>
      <td>Accuracy / fine motor control summary</td>
    </tr>
    <tr>
      <td>Trace time</td>
      <td>Time to completion or timeout</td>
      <td>Speed/efficiency; compare against overlap</td>
    </tr>
    <tr>
      <td>Out-of-bounds events</td>
      <td>Debounced corridor-exit count</td>
      <td>Error frequency / subtle instability</td>
    </tr>
    <tr>
      <td>Score</td>
      <td>&Sigma; S<sub>max</sub> * min(1, l<sub>trace</sub> / L)</td>
      <td>Completion/progress incentive</td>
    </tr>
  </tbody>
</table>

---

## How to use the application

A typical session flow (therapist/clinician administered):

1. **Start NeuroReach VR** (pass-through MR).
2. In the **clinician menu**, select:
   - **Patient ID**
   - **Task order** (BalloonPop or PathTrace)
   - **Difficulty** (Easy / Hard)
   - **Duration / time limit** (block duration for BalloonPop; optional time limit for PathTrace)
3. Patient completes the selected task(s).
4. After each block/trial, the application provides:
   - **Score**
   - Core performance indicators (counts/time/overlap) in the saved logs
5. Repeat blocks as needed for practice.
6. **Export/retrieve CSV logs** for review.

---

## Data export (CSV)

The system exports **per-trial summary metrics** in **CSV** format for direct spreadsheet review.

Export contents:
- **One row per trial** including:
  - timestamp
  - Patient ID
  - task type
  - difficulty profile
  - duration / time limit
  - task-specific metrics (listed above)

Additionally, an **event-level log** may also be produced, capturing events such as:
- BalloonPop: spawn, pop, expiry
- PathTrace: trace start, trace end, timeout

> Retrieval: Logs are stored in the application’s persistent storage on the Quest device. Use **Quest Developer Hub** or **ADB** to pull the CSV files from the headset for analysis.

---

## Implementation notes (high level)

- **Anchoring / coordinate frames**
  - A local task frame is defined relative to the user’s head pose at trial start.
  - Balloon positions are sampled within this frame to keep reach distances consistent.
  - PathTrace spawns a stable drawing plane at a fixed offset in front of the user to reduce drift effects during a trial.

- **Haptics**
  - Stylus vibration is triggered on corridor exit (d > τ).
  - Pulses are debounced to avoid constant buzzing and to keep error feedback perceivable.

- **Parameterization**
  - Key parameters (lifetimes, spawn radii, corridor widths, time limits) are stored in a **JSON config** file loaded at startup.

---

## Known limitations

- The initial evaluation was a **non-patient feasibility pilot** (healthy participants); no clinical-efficacy claims are made.
- Participants reported occasional **spatial drift** of virtual elements (a priority for future iterations).
- Some users requested clearer onboarding and stronger completion feedback (sound/visual confirmations).
