# UI Standard

Use this guide when designing or changing frontend pages, components, shells, or visual patterns. Treat it as the source of truth for Fabric's current UI language.

## Design principles

Fabric uses an operational workspace style derived from the Axxession mockup we aligned on.

Core rules:

1. Prefer structured workspaces over generic app chrome.
2. Prefer soft page backgrounds with white content surfaces.
3. Prefer dark shell chrome for navigation, white surfaces for work areas.
4. Prefer grouped views, tabs, and list rows over long stacks of unrelated cards.
5. Prefer clear hierarchy: title, support text, action, status.
6. Keep layouts calm. Avoid loud fills, dense borders, and many competing callouts.

### Signal Over Noise

Prefer decision-shaped UI over API-shaped UI.

Rules:

1. Show summary before detail.
2. Surface action-worthy states first.
3. Compress healthy, default, or fully aligned states into counts, chips, or collapsed groups.
4. Do not give equal visual weight to every attribute returned by the backend.
5. If two sections repeat the same context, merge them or lift that context into the header.
6. Use emphasis for what needs attention, not for everything on screen.
7. Users should be able to answer “what matters here?” in one scan.

Patterns that improve signal:

1. summary strips
2. KPI rows
3. contextual tabs
4. compact status chips
5. exception-first sections
6. collapsible detail rows

Patterns that usually add noise:

1. nested cards inside cards
2. repeating the same status in several places
3. rendering every neutral field as its own box
4. mixing workflow-critical actions with passive metadata
5. long always-open sections of audit detail

## Theme tokens

Primary theme tokens live in `src/frontend/src/styles.css`.

Current core palette:

1. `--fabric-background: #f4f5f6`
2. `--fabric-content: #ffffff`
3. `--fabric-primary: #1d4268`
4. `--fabric-text: #42464d`
5. `--fabric-text-muted: #787c84`
6. `--fabric-text-faint: #9aa0a8`
7. `--fabric-border: #e2e4e8`
8. `--fabric-hover-blue: #eef3f8`
9. `--fabric-active-blue: #e3ebf4`
10. `--fabric-hover-gray: #f7f8f9`

Shell palette:

1. `--fabric-sidebar-rail: #13283f`
2. `--fabric-sidebar-rail-hover: #1d3a5a`
3. `--fabric-sidebar-menu: #1d4268`
4. `--fabric-sidebar-menu-muted: rgba(255,255,255,0.62)`

Semantic meaning:

1. `background`: page background only.
2. `content`: white work surfaces, panels, popovers, cards, rows.
3. `primary`: active navigation, key titles when emphasis needed, primary actions, focus accents.
4. `muted`: secondary copy and metadata.
5. `faint`: placeholders and low-emphasis inline text.

Do not hardcode colors in pages when a token already exists.

## Typography

Typography defaults live in `src/frontend/src/styles.css`.

1. Default font family: `Montserrat, system-ui, Arial, sans-serif`
2. Body text should feel compact and readable, not airy.
3. Headings use slightly tighter tracking via global `letter-spacing: -0.02em`.

Preferred sizes:

1. Page titles: `text-[28px]` to `text-[30px]`, `font-semibold`
2. Section titles: `text-[15px]` to `text-[18px]`, `font-semibold`
3. Body text: `text-[14px]`
4. Supporting text: `text-[13px]` to `text-[14px]`
5. Tiny metadata / labels: `text-[11px]` to `text-[12px]`, often uppercase with tracking

Prefer `font-semibold` over `font-bold` for most UI text.

## Radius and spacing

Global radius tokens:

1. `--radius-interactive: 12px`
2. `--radius-structural: 14px`
3. `--radius-fixed-action: 18px`

Spacing rhythm:

1. Use `12`, `14`, `18`, `24`, `32` as the default scale.
2. Shell and nav surfaces should be compact.
3. Content panels can breathe more than navigation chrome.
4. Rows should have larger outer padding than inner chip spacing.

Use:

1. `rounded-interactive` for controls, tabs, buttons, compact chips.
2. `rounded-structural` for cards, panels, list rows, major surfaces.
3. `rounded-fixed-action` for FABs.

## Shell layout

Preferred desktop shell:

1. Far-left dark rail for top-level workspaces or sections.
2. Second dark contextual menu for local navigation.
3. Main content surface on soft page background.
4. Account avatar at rail bottom.
5. Branding inside the second menu, not a separate desktop top bar.

Preferred mobile shell:

1. Drawer for shell navigation.
2. Slim top row only when needed for drawer access and account actions.

Reference implementations:

1. `src/frontend/src/shared/layout/perspective-sidebar.tsx`
2. `src/frontend/src/features/reception-desk/layout/reception-desk-workstation-layout.tsx`

## Tabs

Tabs are global underline tabs, not pills.

Rules:

1. Use tabs for same-level workspace views.
2. Tabs should sit on a flat row with a bottom border.
3. Active tab uses bottom border + primary text.
4. Inactive tabs use muted text.
5. Counts belong inline after the label, not as separate badges.

Do not wrap tabs in extra card chrome unless the content needs its own panel.

### Filter pills

Use filter pills for same-dataset filtering inside a workspace view.

Rules:

1. Place filter pills between the section header and the table or list they control.
2. Keep pills inside the same white panel as the controlled content.
3. Use exact domain states when possible instead of vague groupings.
4. Keep counts inline after the label, low emphasis, and not as separate badges.
5. Pills should wrap on smaller screens without breaking the layout.

Preferred styling:

1. compact rounded shape
2. subtle border
3. white or soft background when inactive
4. soft blue background + primary text when active
5. no heavy shadows
6. active filter pills may use a small leading checkmark icon to confirm the filter is applied

Checkmark rule:

1. use checkmarks on filter pills and applied toggle pills
2. do not use checkmarks on tabs
3. do not use checkmarks on passive status badges
4. use them when the control means “this filter is currently applied”, not when it means “this is the current page/view”

Good examples:

1. `All`
2. `In Progress`
3. `Approved`
4. `Partially Approved`
5. `Rejected`
6. `Expired`

Reference implementation:

1. `src/frontend/src/shared/components/ui/tabs.tsx`

## Cards and panels

Preferred surface treatment:

1. White background
2. Soft border
3. Subtle shadow only when needed
4. Larger corner radius

Use cards for:

1. major grouped sections
2. settings panels
3. detail/edit forms

Do not use cards for every small chunk of content on overview pages. Prefer grouped panels and list rows.

## Lists and rows

The default row style should follow the employee overview pattern.

Preferred row anatomy:

1. white rounded row surface
2. title first
3. supporting description below
4. status chip or badge aligned to the right
5. metadata represented as compact chips below

When rows are clickable:

1. whole row should be the hit area
2. hover should be subtle
3. avoid tiny action buttons inside every row unless required

Signal rules for rows:

1. lead with the field a user will decide from
2. move secondary metadata into chips when possible
3. do not render every supporting fact as its own bordered block
4. if a row is healthy and uninteresting, summarize it instead of expanding it
5. if a row needs investigation, expose detail on demand rather than by default

Use rows instead of tables when the user is scanning work items, requests, events, visitors, or assignments.

Use tables only when many aligned columns matter.

## Progressive Disclosure

Use progressive disclosure to keep dense workflows readable.

Rules:

1. Use tabs to split peer concerns like `Approvals` and `Grants`.
2. Use expanders or `details` for audit-level detail.
3. Keep problematic, actionable, or blocked states visible first.
4. Collapse fully compliant, completed, or low-interest states by default.
5. Do not hide the primary summary; hide the explanatory or audit detail.

Good examples:

1. approval rows that open to requirement detail
2. compliant locations grouped behind a disclosure
3. workflow pages where grants and approvals live in separate tabs

## Tables

Use tables when users need to compare aligned attributes across many records.

Good fit:

1. request lists
2. access assignments
3. people or subject listings with repeated attributes
4. comparison-heavy operational views

Bad fit:

1. timeline feeds
2. activity streams
3. compact dashboard summaries
4. short lists where a row/card pattern scans faster than columns

Preferred table structure:

1. wrap the table in a white rounded panel
2. place actions and filter pills above the table inside the same panel
3. keep the header row light and quiet
4. use soft separators, not hard gridlines everywhere
5. give rows more vertical padding than a CRUD admin grid

Preferred table header styling:

1. `text-[11px]`
2. uppercase
3. tracking around `0.18em`
4. muted text color
5. very soft background tint or just a bottom divider

Preferred row styling:

1. subtle hover state only
2. clickable whole row when it opens a detail page
3. do not tint the whole row for normal statuses
4. rely on badges for status meaning

### Table Signal

When a table is justified, keep it scan-first:

1. make the first column strongest
2. keep metadata columns quieter
3. put filters above the table to reduce scan cost
4. use badges for state rather than colorizing the whole row
5. keep header treatment light and restrained

Column guidance:

1. first column should be strongest
2. first column may use title + support text
3. date and metadata columns should stay visually quieter
4. status columns should use badges, not raw text
5. trailing action/open column should be low emphasis

Responsive rule:

1. prefer desktop table + mobile stacked cards when the table does not translate cleanly to smaller screens
2. mobile and desktop should share the same filters and same data semantics

Reference implementation:

1. `src/frontend/src/features/perspectives/employee-request-access-page.tsx`

## Badges and chips

Badges live in `src/frontend/src/shared/components/ui/badge.tsx`.

Rules:

1. Status variants use soft tinted chips with a colored dot.
2. Neutral metadata variants use soft chips without a dot.
3. Badges are compact and should not dominate the row.

Use badge variants like this:

1. `success`: active, valid, confirmed, provisioned, compliant
2. `warning`: expiring, partial, pending warning states
3. `error`: failed, rejected, blocked, invalid
4. `secondary`: metadata labels, neutral tags, low-emphasis state
5. `outline`: plain neutral tags
6. `default`: primary-emphasis label when status-like

If a value is metadata rather than a state, prefer `secondary` or `outline` over strong state colors.

## Buttons and actions

Rules:

1. Keep headers light. Do not crowd every section header with multiple actions.
2. Use one primary action per workspace area.
3. Use outline actions for secondary navigation.
4. Use FABs for contextual actions that belong to the active tab or active workspace view.
5. Remove action chrome that does not improve the user’s next decision.
6. Actions should sit near the active concern, not repeat in every summary block.

FAB guidance:

1. place bottom-right
2. show only when relevant
3. use primary by default for the main action in that view

## Page composition

Preferred overview page structure:

1. page title + one support sentence
2. compact KPI strip if summary matters
3. tabs for peer views
4. structured row lists inside each view
5. contextual actions only for the active view

Preferred detail page structure:

1. title block
2. key state badges
3. compact fact summary
4. high-signal context like justification, validity, locations, or owner
5. grouped workflow sections after the summary
6. actions near the working area, not scattered everywhere

For workflow-heavy pages:

1. put package/request/identity context in the header
2. split peer concerns into tabs before stacking more sections
3. keep exception states expanded and healthy states summarized
4. prefer one strong workflow section over many medium-strength sections

## Do / Don't

Do:

1. use shell-consistent dark navigation
2. use white surfaces on the soft gray page background
3. use tabs for overview subviews
4. use chips/badges for compact state and metadata
5. use row surfaces for operational lists
6. summarize first, expand second
7. keep exception states visually ahead of healthy states

Don't:

1. reintroduce pill tabs
2. use saturated solid badges for standard statuses
3. stack many unrelated generic cards on overview pages
4. hardcode shell colors when tokens exist
5. crowd page headers with actions that belong inside a tab or subview
6. mirror backend object structure directly in the UI
7. show every neutral field as if it were equally important

## Source of truth

Current source files:

1. `src/frontend/src/styles.css`
2. `src/frontend/src/shared/components/ui/tabs.tsx`
3. `src/frontend/src/shared/components/ui/badge.tsx`
4. `src/frontend/src/shared/layout/perspective-sidebar.tsx`
5. `src/frontend/src/features/reception-desk/layout/reception-desk-workstation-layout.tsx`
6. `src/frontend/src/features/perspectives/perspective-home-page.tsx`

When changing the UI system, update both the implementation and this document.
