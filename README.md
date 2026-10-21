# Timeline Dialogue

Text strips on a Unity Timeline. Glyphs pop in, `/` codes mark bold, jitter, and wave, and a speaker profile mutters one voice clip as the line reveals.

**Package:** `com.desayuno64.timeline-dialogue`  
**Unity:** 6000.2 or newer  
**Dependencies:** Timeline, uGUI (TextMeshPro)

## Install

This repository is the package root. In another project, clone it into `Packages/`:

```text
Packages/com.desayuno64.timeline-dialogue
```

Unity loads any folder there that contains this `package.json`. No registry entry is required.

A private git URL also works once git can authenticate:

```json
"com.desayuno64.timeline-dialogue": "https://github.com/Desayuno64/timeline-dialogue.git"
```

## Setup

1. Add a track of type **Dialogue Track** and bind a `DialogueController`.
2. Create a panel with **GameObject → Timeline Dialogue → Create Panel**, or assign your own `CanvasGroup`, `TMP_Text`, and `AudioSource` on the controller.
3. The default speaker is `Runtime/Voice/DefaultSpeaker.asset`. Assign it on the controller. Each strip can override it with its own Speaker Profile.
4. Add a clip and edit the line in the clip inspector. The strip label follows the line when the text changes.

Do not overlap dialogue clips. The mixer uses the highest-weight clip.

## Slash codes

Codes are not drawn and do not take reveal time.

| Code | Effect |
| --- | --- |
| `/b` | Bold, until the closer. |
| `/j` | Jitter. |
| `/w` | Wave. |
| `/#rrggbb` | Color, until the closer. `#rgb` and `#rrggbbaa` also work. |
| `/` | Closes every open code, including color. |
| `//` | A literal slash. |

`/b` and `/w` stack: `soy /b/w Plumón/, el dodo.` Color stacks the same way: `the /#e23b3b/wqueen of hearts/`.

An unclosed code runs to the end of the line. A slash followed by any other letter is shown as text.

Pop is set on each strip. **Pop Out Duration** covers both ends of that strip: the panel fades in, and at the end the line pops and shrinks away. `0` keeps a hard cut. Wave and jitter are set on the `DialogueController`, under **Motion**. **Wave Period** is seconds per cycle (higher is slower). **Wave Spacing** is how far neighboring letters sit in that cycle (lower is a wider wave).

## Reveal

**Clip curve** maps the strip time through the curve. Existing clips stay on this mode. **Characters per second** types at a fixed rate and holds if the strip is longer than the line. **Instant** shows the whole line at rest scale and does not mutter.

Pop, wave, and jitter use the playhead time, so a paused scrub stays on one frame.

## Mutter

Each controller has one `AudioSource`. Each Speaker Profile has one voice clip. Each strip uses its own profile, or the controller default when the strip's profile is empty.

Blips play while time moves forward, including an Editor Timeline scrub. A jump larger than half a second, or a backward scrub, does not replay them. The strip's pitch offset applies in the Editor and in Play Mode. Pitch jitter plays in Play Mode only.

Outside Play Mode, turn on the Timeline window's Preview button and scrub. The panel updates in the Game view. Screen-space UI is not drawn in the Scene view.

Sample clips live in `Runtime/Voice/`. `DefaultSpeaker` uses `Vocal_A_01`.

## Localization

An optional **Localization Key** on the strip is ignored when empty. A `DialogueLineResolver` on the controller (or a parent) receives the authored line and the key. The package does not depend on Unity Localization.

## Samples

**GameObject → Timeline Dialogue → Create Panel** builds a canvas, text, audio source, and controller. See `Samples~/Basic`.
