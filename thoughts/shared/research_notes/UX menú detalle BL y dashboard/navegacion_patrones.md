# Navigation patterns for complex B2B web apps (~40 customer + ~30 admin destinations)

Evidence label key: **[R]** = empirical research (usability tests or measured data); **[G]** = design-system or standards guidance (expert opinion or convention); **[S]** = normative standard (WCAG/ARIA).

## How many top-level items to show, and whether to use mega menus, dropdowns or sidebars

### Takeaway
No major source sets a hard limit for desktop top-level items. The firm rules are these: keep the main navigation visible (don't hide it), use a left sidebar for apps with many destinations, allow at most one or two levels in the sidebar, avoid cascading flyouts, and use mega menus only for large, broad navigation, opened by click. Navigation is not a sitemap. Show the most important sections and reach the rest by other routes (landing pages, search, local navigation).

### Cited Findings
- [R] Hidden navigation (hamburger) on desktop was used in only 27% of cases, against 48–50% for visible or combo navigation. Tasks were 39% slower, and discoverability fell by more than 20% ("discoverability is cut almost in half"). Study from 2016. — [NN/g, Hamburger Menus and Hidden Navigation](https://www.nngroup.com/articles/hamburger-menus/)
- [G] NN/g Menu Design Checklist (2024), 17 guidelines. The relevant ones: show navigation on large screens (no hamburger on desktop). Put primary navigation in the header for websites or on the left for apps, with utility navigation at the top. Show the current location. Provide local navigation. Use clear, familiar words with no jargon. Put the key word first. Use submenus opened by click, not hover-only. Avoid multilevel cascading menus and use mega menus or landing pages instead. Place frequent commands close by (Fitts's law). Avoid novel patterns. — [NN/g, Menu-Design Checklist](https://www.nngroup.com/articles/menu-design/)
- [G/R] Mega menus work for sites with extensive navigation because everything is visible at once, with no scrolling, and users recognize options instead of recalling them. Group by the user's mental model with medium granularity. Labels should start with the word that carries the meaning. Put the most important group top-left. Show each option only once. Don't embed widgets or search boxes. Make the top-level item clickable and lead to an accessible landing page. If hover is used: 0.5 s delay before opening, show within 0.1 s, hide after 0.5 s, and handle the "diagonal problem". Article from 2017. — [NN/g, Mega Menus Work Well](https://www.nngroup.com/articles/mega-menus-work-well/)
- [R] Baymard (e-commerce, not B2B): hover dropdowns or mega menus are the main navigation on 88% of top US e-commerce sites, but tests show they need very careful interaction details to work well. 54% of sites suffer from "over-categorization". — [Baymard, Homepage & Category Navigation research](https://baymard.com/research/homepage-and-category-usability); [Baymard, Navigation best practices 2025](https://baymard.com/research-articles/ecommerce-navigation-best-practice)
- [G] Long dropdowns that need scrolling stop users seeing all options. Limit items, or switch to lists of links or a mega menu. Article from 2017. — [NN/g, Dropdowns: Design Guidelines](https://www.nngroup.com/articles/drop-down-menus/)
- [G] Material: the navigation rail holds 3–7 destinations. Use the navigation drawer for 5 or more primary destinations or more than one level of hierarchy. In M3 Expressive the drawer is no longer recommended and the expanded rail replaces it. — [M3 Navigation rail](https://m3.material.io/components/navigation-rail/guidelines); [M3 Navigation drawer](https://m3.material.io/components/navigation-drawer/guidelines) (numbers taken from search snippets, because the M3 page did not render in the fetch)
- [G] Fluent 2 Nav supports **one level of nesting** (two levels at most), and Tree is for anything deeper. Group links into categories, put the most important categories first, and keep the same order across platforms. Labels are short and scannable, in sentence case. Use chevrons to show sub-items. Secondary actions must always be in the DOM, not hover-only. Default width is 260 px, and it becomes an overlay at ≤640 px. — [Fluent 2 Nav usage](https://fluent2.microsoft.design/components/web/react/core/nav/usage)
- [G] IBM Carbon UI Shell: a persistent full-width header plus an optional left panel for product navigation. On small screens the header links collapse into the left panel. Order runs left to right: product items on the left, global actions on the right. Carbon sets no maximum for icons or items. — [Carbon UI shell header](https://carbondesignsystem.com/components/UI-shell-header/usage/)
- [R/G] Atlassian (Dec 2024) moved its navigation from the top bar to a **full-height sidebar**. Research: a qualitative study with 16 Jira users, then a Chrome plugin with 160 users. Findings: finding work was a recurring pain point, and users wanted to customize and hide sidebar items. Decision: the sidebar gives "vertical space and information density", and the top bar is kept for universal actions (search, create). They reduced 23 inconsistencies to 3 components: menu button, flyout menu and expandable menu. — [Atlassian, Designing Atlassian's new navigation](https://www.atlassian.com/blog/design/designing-atlassians-new-navigation); full-height sidebar rollout Dec 2025 — [Atlassian Community](https://community.atlassian.com/forums/Navigation-Refresh-articles/A-full-height-sidebar-for-better-navigation/ba-p/3144347)
- [G] GOV.UK: navigation is for services that are used repeatedly, have several tasks and have no linear order. Simplify the journey first. Links should go to "the most important top-level sections"; "navigation is not a site map and does not need to list every part of your service". For sequential flows, use a Task list instead of navigation. — [GOV.UK, Help users navigate a service](https://design-system.service.gov.uk/patterns/navigate-a-service/); [GOV.UK Service navigation](https://design-system.service.gov.uk/components/service-navigation/) (sets no numeric maximum; examples have 2–3 links)

### Inferences
- With ~40 destinations, the convergent pattern (Atlassian, Fluent, Carbon, M3) is a **collapsible left sidebar** with about 5–9 labeled groups and at most one nesting level. Keep the top bar for search, create, notifications, help and account. A top-bar mega menu fits less well in a "use the tool daily" app and better in "browse and explore" sites (inferred from NN/g "left side for apps").
- No source backs a specific "7±2" limit. It is a common folk number. Material's 3–7 applies only to the rail or bar. Treat any number as a heuristic, not evidence.
- Not all 40 destinations have to be in the navigation (GOV.UK): rare destinations can live on landing pages for their group, in local navigation, or in search.

### Gaps
- No public B2B study comparing the performance of a sidebar against a mega menu was found.
- Baymard's evidence is from B2C e-commerce, and there is no equivalent study of B2B portals.
- Shopify Polaris (navigation docs moved to shopify.dev with a redirect) and SLDS (the global navigation page is a CSS blueprint only, with no guidelines) could not be fetched usefully. I have no cited details on their item counts or "More" overflow.

## Task-based (jobs-to-be-done) vs. feature-based labels, and navigation for multiple roles

### Takeaway
Labels should use the user's language and be front-loaded with the key word. Audience or role-based navigation adds cognitive load and should be secondary. For roles with very different content (customer vs. internal admin), the convention is a separate workspace with an app switcher, not a mixed menu.

### Cited Findings
- [G] "Use clear, specific, familiar wording" with no jargon or made-up terms. Labels front-loaded and left-aligned. — [NN/g Menu-Design Checklist](https://www.nngroup.com/articles/menu-design/)
- [G/R] Audience-based navigation (2015): (1) users don't know which group they belong to, (2) labels are ambiguous ("Faculty": about them or for them?), (3) more cognitive load, (4) anxiety about missing content, (5) overlapping content and pogo-sticking. It works when content is truly unique to the group, the categories are mutually exclusive, labels use "For …", it is secondary, and switching is easy. — [NN/g, Audience-Based Navigation](https://www.nngroup.com/articles/audience-based-navigation/)
- [G] Carbon: the **switcher** (app switcher) always sits at the far right of the header so icons don't shift between products. Global actions (search, notifications, help, account) are right-aligned, with search furthest left so it can expand and account second from the right. — [Carbon UI shell header](https://carbondesignsystem.com/components/UI-shell-header/usage/)
- [G] Atlassian's new navigation includes switching between apps and sites and an "explore apps" area, separate from product navigation. — [Atlassian blog](https://www.atlassian.com/blog/design/designing-atlassians-new-navigation); [Atlassian Home navigation](https://support.atlassian.com/platform-experiences/docs/what-is-the-new-navigation-in-atlassian-home/)
- [G] GOV.UK: for services with sequential steps, use a Task list (task-based) instead of section navigation. — [GOV.UK navigate a service](https://design-system.service.gov.uk/patterns/navigate-a-service/)

### Inferences
- Internal admin (~30 destinations) should live in a **separate workspace or area** (its own route, own sidebar), reached from the switcher or account menu, and not mixed into the customer menu. This applies NN/g's "content unique to the audience" condition plus the Carbon/Atlassian pattern.
- Labels: prefer the user's domain nouns or task phrases ("Track shipment", "Release BL") over internal module names. Keep them consistent with the page title.

### Gaps
- No controlled study comparing task labels and feature labels in B2B was found. The recommendation rests on NN/g guidelines and card-sorting and tree-testing practice (not fetched).

## Hiding vs. disabling features (roles, licensing, feature flags), "more" menus, frequency-based ordering, personalization

### Takeaway
Within **menus of actions** (dropdowns, context menus), NN/g recommends disabling and explaining, not removing. In **global navigation**, the common practice is to hide what a role or licence will never use, but a deliberate upsell or "not enabled" page is better for features the user could get. Prefer user-controlled personalization (starred, recent, hide) over automatic frequency-based reordering, which breaks spatial consistency.

### Cited Findings
- [G] "Gray out any unavailable options instead of removing them". Removing them breaks spatial consistency and makes the interface harder to learn. Show a tooltip after about 1 s explaining why the option is unavailable and how to enable it. — [NN/g, Dropdowns](https://www.nngroup.com/articles/drop-down-menus/) (2017)
- [G] Staged and progressive disclosure for rare or advanced functions in complex apps. Accelerators for experts. — [NN/g, 10 Usability Heuristics Applied to Complex Applications](https://www.nngroup.com/articles/usability-heuristics-complex-applications/) (2021)
- [R] Atlassian users "wanted to personalize project navigation and have easier ways to hide sidebar items". The solution was starred items, recent items and hide/show controls, and users "loved the customization options so much that they wanted more of it". — [Atlassian blog](https://www.atlassian.com/blog/design/designing-atlassians-new-navigation)
- [G] Atlassian manages navigation changes as a gradual rollout with admin controls, an example of progressive rollout of a new navigation. — [Atlassian, Manage the navigation rollout](https://support.atlassian.com/navigation/docs/manage-the-navigation-rollout/)
- [G] Fluent: keep the same order across platforms, with the important items first. — [Fluent 2 Nav](https://fluent2.microsoft.design/components/web/react/core/nav/usage)
- [S] WCAG 3.2.3 Consistent Navigation (AA): navigation repeated across pages appears in the same relative order unless the user changes it. Automatic reordering by frequency puts this at risk; user-initiated changes are allowed. — [W3C Understanding 3.2.3](https://www.w3.org/WAI/WCAG22/Understanding/consistent-navigation.html)

### Inferences
- Rule of thumb from these sources: **role or permission that will never apply → hide** (avoids noise; there is no "how do I enable it?"). **Licence or plan the customer could buy, or a temporary state → show disabled or with a "Not included in your plan / Contact your representative" page.** **Feature flag in rollout → hide until enabled** and announce with an in-product banner or "New" badge when it turns on.
- "More" overflow: hides items (same penalty as the hamburger, per NN/g 2016). Use it only for low-frequency items, and with a stable order.
- Frequency-based ordering: prefer fixed pinned or favourite sections and "Recent" over automatic reordering (WCAG 3.2.3 and spatial consistency).

### Gaps
- No public quantitative study was found on hiding vs. disabling in **global navigation** specifically, or on licence-gated upsell in B2B. The NN/g guidance refers to menus and dropdowns.
- No found guidance from Polaris or SLDS on permissions or "More" (pages not reachable).

## Utility navigation, breadcrumbs, global search and command palette (Cmd/Ctrl+K)

### Takeaway
Utility navigation goes top-right in a stable order (search, help, notifications, account, switcher). Breadcrumbs show the hierarchy (not history) and supplement the navigation rather than replacing it. A Cmd/Ctrl+K palette is an accelerator for frequent users. It doesn't replace visible navigation, and it should be built on the ARIA combobox pattern inside a modal dialog. There is no peer-reviewed research evidence on command palettes; it is an industry pattern.

### Cited Findings
- [G] Utility navigation at the top, primary on the left in apps. — [NN/g Menu-Design Checklist](https://www.nngroup.com/articles/menu-design/)
- [G] Carbon: search, notifications, help, account, then switcher, right-aligned with no gaps. — [Carbon UI shell header](https://carbondesignsystem.com/components/UI-shell-header/usage/)
- [S] WCAG 3.2.6 Consistent Help (A, new in 2.2): if help mechanisms (contact details, chat or form, FAQ, chatbot) repeat across pages, they must appear in the same relative order. It does not require help to exist. — [W3C Understanding 3.2.6](https://www.w3.org/WAI/WCAG22/Understanding/consistent-help.html)
- [G] Breadcrumbs (2018, reviewed 2026): supplement, don't replace. Show the hierarchy, not the history. One canonical path. Include the current page, not as a link. Each node links to an ancestor. Not needed for flat 1–2 level hierarchies. Start at home. On mobile, no wrapping, adequate targets, and show only the last levels if needed. — [NN/g, Breadcrumbs: 11 Design Guidelines](https://www.nngroup.com/articles/breadcrumbs/)
- [G] GOV.UK: put breadcrumbs just before `<main>` so "Skip to main content" also skips them. — [GOV.UK navigate a service](https://design-system.service.gov.uk/patterns/navigate-a-service/)
- [G] Accelerators that are invisible to novices help experts break through the efficiency plateau. — [NN/g complex apps heuristics](https://www.nngroup.com/articles/usability-heuristics-complex-applications/)
- [G] GitHub uses Ctrl/Cmd+K for a command palette (navigation plus commands) in an enterprise product. — [GitHub Docs, Command palette](https://docs.github.com/en/enterprise-cloud@latest/get-started/using-github/github-command-palette)
- [G, secondary] Accessible palette implementation: modal `role="dialog" aria-modal="true"`, an input with `role="combobox"`, `aria-expanded`, `aria-autocomplete="list"` and `aria-activedescendant`. Focus stays on the input and arrow keys move the active descendant. Escape closes it. Announce the result count with `aria-live`. — [uxpatterns.dev, Command Palette Pattern](https://uxpatterns.dev/patterns/advanced/command-palette) (secondary source; the normative base is the [W3C APG Combobox pattern](https://www.w3.org/WAI/ARIA/apg/patterns/combobox/))

### Inferences
- In the portal, global search should handle both **entities** (BL, booking, container: the frequent B2B case) and **destinations** (pages and actions). Show a visible search button in the header as well as Ctrl+K, so the feature can be discovered. The shortcut should not collide with the browser's (Ctrl+K in Chrome/Firefox focuses the address bar or search when the page doesn't capture it). Document it in help.
- Breadcrumbs are useful in the BL detail view (Home > Shipments > BL XXXX), and not needed on flat pages.

### Gaps
- No NN/g or Baymard study measuring the benefit of command palettes (adoption, time saved) was found. This is evidence from practice, not from research.
- The NN/g utility navigation article was not fetched, so there is no specific detail on the order of language or notifications beyond Carbon.

## Accessibility requirements for navigation and mega menus (ARIA APG, WCAG 2.2)

### Takeaway
Use the **disclosure navigation** pattern (buttons with `aria-expanded` + link lists), not `role="menu"/"menubar"`. Keep navigation and help in a consistent order (3.2.3, 3.2.6). Mark the current page with `aria-current="page"`, open menus by click, close them with Escape, and meet the 24×24 CSS px minimum target size (2.5.8).

### Cited Findings
- [S] APG Disclosure Navigation Menu: buttons with `aria-controls` and `aria-expanded`. `aria-current="page"` on the current link. Tab and Shift+Tab move through buttons and links. Enter or Space toggles. Escape closes. Arrow keys, Home and End are optional. It "does not use the WAI-ARIA menu role" because site navigation lacks the complex functionality that assistive technologies expect from a menu. — [W3C APG, Example Disclosure Navigation Menu](https://www.w3.org/WAI/ARIA/apg/patterns/disclosure/examples/disclosure-navigation/)
- [S] 3.2.3 Consistent Navigation (AA): repeated navigation keeps the same relative order. — [W3C Understanding 3.2.3](https://www.w3.org/WAI/WCAG22/Understanding/consistent-navigation.html)
- [S] 3.2.6 Consistent Help (A): see the previous section. — [W3C Understanding 3.2.6](https://www.w3.org/WAI/WCAG22/Understanding/consistent-help.html)
- [S] 2.5.8 Target Size (Minimum) (AA, WCAG 2.2): targets of at least 24×24 CSS px, or enough spacing, with exceptions (inline links, among others). — [W3C Understanding 2.5.8](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html) (from known knowledge of the standard; the page was not fetched in this session)
- [G] Mega menu accessibility: keep it simple with no embedded widgets, a clickable top level leading to an accessible page, strong borders for magnifier users, and adequate touch targets. — [NN/g Mega Menus](https://www.nngroup.com/articles/mega-menus-work-well/)
- [G] Click-activated submenus, not hover-only, for keyboard and touch. — [NN/g Menu-Design Checklist](https://www.nngroup.com/articles/menu-design/)
- [G] Fluent: secondary actions always in the DOM (not hover-only). Truncated labels with a tooltip. — [Fluent 2 Nav](https://fluent2.microsoft.design/components/web/react/core/nav/usage)

### Inferences
- Implementation for the portal: the sidebar is a `<nav aria-label="Main">` with `<ul>` lists. Groups are `<button aria-expanded aria-controls>`, never `role="menu"`. Collapsing to icons needs visible text or an `aria-label` plus a tooltip. Persist expanded or collapsed state per user (a user-initiated change, compatible with 3.2.3). Add a "Skip to main content" link before the sidebar and breadcrumbs.
- Also relevant: 2.4.11 Focus Not Obscured (AA, WCAG 2.2). Sticky headers and flyouts must not cover the focused element (not verified in this session).

### Gaps
- 2.5.8 and 2.4.11 were not fetched in this session. The requirements come from knowledge of the WCAG 2.2 standard and should be verified at w3.org before quoting exact text.
