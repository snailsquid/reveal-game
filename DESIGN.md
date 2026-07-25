# Reveal Game Design System

## 1. Atmosphere & Identity

Reveal Game is a bright, playful game-show surface: sky-blue screens frame large, direct puzzle content, and a dark navy result panel creates a clear moment of reveal. The signature interaction is the answer-to-reward transition: a short success acknowledgement gives way to the roulette reel.

## 2. Color

| Role | Token | Value | Usage |
|---|---|---|---|
| Screen | `screen-sky` | `#3399FF` | Hint screen background |
| Surface | `surface-reveal` | `#0D1A2E` | Result popup |
| Text | `text-primary` | `#FFFFFF` | Hint, popup, and status text |
| Text muted | `text-muted` | `#808080` | Input placeholder (legacy) |
| Action | `action-ink` | `#14291F` | Action-button label |
| Status ready | `status-ready` | `#38D996` | Connected/listening status |
| Status waiting | `status-waiting` | `#FFFFFF` | Neutral connection status |
| Status unavailable | `status-unavailable` | `#FF8A8A` | Backend unavailable status |

Status color is semantic: it must not be the only indicator of state; every status includes clear text.

## 3. Typography

- Primary: Unity built-in legacy font, inherited from the existing screen controls.
- Hint title: 28 px landscape / 22 px portrait.
- Result text: 28 px landscape / 22 px portrait.
- Server status: 16 px landscape / 14 px portrait.
- Button labels: inherit their existing button's label font and size.

## 4. Spacing & Layout

- Base unit: 4 px.
- Hint screen uses a centered content stack and full-screen blue canvas.
- Server status occupies the upper safe area beneath the existing Back button, avoiding overlap with the hint title.
- Result popup remains the existing centered overlay; no new panel pattern is introduced.

## 5. Components

### Hint screen
- **Structure**: title, media carousel, server status, navigation, Back action, result overlay.
- **States**: server unavailable, connected but idle, starting listener, listening, matched, reveal spinning.
- **Accessibility**: state text names the backend condition; Back stays available until a correct Discord answer is matched.
- **Motion**: no added decorative motion; the established roulette sequence communicates the state transition.

### Result popup
- **Structure**: result message and an existing action button used only after the reveal.
- **States**: matched Discord answer, spinning, reveal complete, opening failure.
- **Accessibility**: the matching Discord username and their exact guess are rendered as text before the automatic reveal.

## 6. Motion & Interaction

- The success acknowledgement stays visible briefly, then proceeds automatically to the roulette spin.
- Existing roulette animation remains the only emphasis motion; no layout properties are animated by the new status UI.

## 7. Depth & Surface

The existing UI uses a mixed strategy: flat blue screen, dark opaque result overlay, and button color-state feedback. New status UI stays text-only to preserve that system.

## 8. Accessibility Constraints & Accepted Debt

- Target: WCAG 2.2 AA where UGUI supports it; visible textual status and keyboard-accessible Back button are required.
- Accepted debt: the project currently uses Unity legacy `Text` and lacks a dedicated UGUI primitive showcase. This change preserves those existing components rather than converting the whole UI system.
