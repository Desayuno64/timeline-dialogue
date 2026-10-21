# Timeline Dialogue

Text strips on a Unity Timeline. Glyphs pop in, `/` codes mark bold, jitter, and wave, and a speaker profile mutters vowel clips as the line reveals.

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
3. The default speaker is `Runtime/Voice/DefaultSpeaker.asset`. Assign it on the controller, or override it per strip.
4. Add a clip and edit the line in the clip inspector. The strip label follows the line when the text changes.

Do not overlap dialogue clips. The mixer uses the highest-weight clip.

## Slash codes

Codes are not drawn and do not take reveal time.

| Code | Effect |
| --- | --- |
| `/b` | Bold, until the closer. |
| `/j` | Jitter. |
| `/w` | Wave. |
| `/` | Closes every open code. |
| `//` | A literal slash. |

`/b` and `/w` stack: `soy /b/w Plumón/, el dodo.`

An unclosed code runs to the end of the line. A slash followed by any other letter is shown as text.

## Reveal

**Clip curve** maps the strip time through the curve. Existing clips stay on this mode. **Characters per second** types at a fixed rate and holds if the strip is longer than the line. **Instant** shows the whole line at rest scale and does not mutter.

Pop, wave, and jitter use the playhead time, so a paused scrub stays on one frame.

## Mutter

Each new non-space glyph plays a vowel clip from the strip's speaker, or the controller default. A consonant uses the last vowel of its word. `u` uses O until the profile has a U clip. Variants of the same vowel are chosen per glyph so a line replays the same mutter.

Blips play while time moves forward, including an Editor Timeline scrub. A jump larger than half a second, or a backward scrub, does not replay them. Editor preview has no pitch jitter. Pitch jitter plays in Play Mode.

Sample clips live in `Runtime/Voice/` (`Vocal_A_01`, `Vocal_A_02`, `Vocal_E_01`, `Vocal_I_01`, `Vocal_O_01`).

## Localization

An optional **Localization Key** on the strip is ignored when empty. A `DialogueLineResolver` on the controller (or a parent) receives the authored line and the key. The package does not depend on Unity Localization.

## Samples

**GameObject → Timeline Dialogue → Create Panel** builds a canvas, text, audio source, and controller. See `Samples~/Basic`.
