---
name: saas-dashboard-blueprint
description: >
  Blueprint on how to design YC level SAAS dashboards with implementation guide in shadcn
---

Objektive Grundstruktur eines YC SaaS Dashboards. Abgeleitet aus [[LinearDesignGuide]], Linear (YC W20), Retool (YC W17), Notion, Figma, sowie YC Library Artikeln und SaaS UI Pattern Datenbanken. Validiert gegen Linear, Retool, Notion, Figma.

Jede Komponente hat einen klaren Job. Das Macro-Layout ist immer gleich. Die kreative Freiheit liegt nicht in der Struktur sondern in WIE man jede Komponente ausfuellt — genau wie bei [[YcWebsiteBlueprint]].

Wichtiger Unterschied zu Landing Pages: Ein Dashboard hat MEHR Freiheit in den Content-Bereichen, aber WENIGER Freiheit im Shell/Navigation-Bereich. Bei Landing Pages ist es umgekehrt.

# Das Macro-Layout: Inverted-L Shell

Jedes ernsthafte SaaS Produkt benutzt das gleiche Layout:

```
+--[ Header / Toolbar ]-----------------------------+
|          |                                         |
| Sidebar  |         Main Content Area               |
|          |                                         |
|          |                                         |
|          |                                         |
+----------+-----------------------------------------+
```

Sidebar + Header bilden ein "L" um den Content. Content bekommt die meiste visuelle Prioritaet. Das ist nicht verhandelbar — horizontale Top-Navs allein skalieren nicht fuer komplexe Multi-Modul Produkte.

---

# Die Struktur

## 1. Sidebar Navigation

**Job:** Globale Orientierung + schneller Zugang zu allem
**Feste Elemente (von oben nach unten):**

1. Workspace Switcher (Dropdown oben-links: Org-Name + Logo)
2. Primaere Navigation (Inbox, My Issues, Favorites — die 20% die 80% genutzt werden)
3. Team/Projekt Sektionen (kollabierbare Baumhierarchie)
4. Custom Views (nutzererstelle gefilterte Ansichten)
5. Bottom Section (Settings Zahnrad, Help, User Avatar)

**Verhalten:**

- Kollabierbar zu Icon-Only Rail fuer mehr Content-Platz
- Dimmed Chrome: Sidebar visuell zurueckgesetzt (gedaempfte Farben), damit Content heraussticht — Linear dimmt die Sidebar explizit "a few notches" unter Content
- Single-Click Access: Jedes Ziel in 1 Klick erreichbar, keine tiefen Submenues
- Icons + Labels: Jeder Nav-Item hat ein Icon. Bei Collapse nur Icons
- Active State: Hervorgehobener Hintergrund fuer aktuelle Seite
- Mobile: Sidebar wird Hamburger-Drawer (links, Overlay)

**Freiheit:** KEINE bei der Struktur. Etwas bei der visuellen Gestaltung (Farben, Icons).

**Linear:** Workspace > Teams > Issues, mit Cycles + Projects + Initiatives als Organisationsebenen
**Notion:** Workspace > Pages als Baumhierarchie, frei verschachtelbar
**Retool:** Apps-Liste als Hauptnavigation, Ordner fuer Organisation
**Figma:** Files als Baumhierarchie, Team > Projekt > File

---

## 2. Header / Toolbar

**Job:** Kontextuelle Kontrolle fuer die aktuelle Ansicht
**Feste Elemente:**

- Breadcrumb / Page Title (links)
- View Switcher (List / Board / Timeline / Split)
- Filter Bar (Dropdowns, Search Input, aktive Filter Chips)
- Display Options (Gruppierung, Sortierung, Spalten)
- Primary Action Button (rechts, z.B. "Create Issue")
- Search / Cmd+K Trigger (rechts)
- Notifications Bell (rechts)
- User Avatar (ganz rechts, Dropdown zu Profil/Settings/Logout)

**Freiheit:** NIEDRIG. Die Elemente sind standardisiert, nur die Reihenfolge und Details variieren.

**Linear:** Kompakte Tabs mit abgerundeten Ecken, Filter + Display Options persistent pro View
**Notion:** Minimaler Header, View Switcher als Tabs, Filter als Chips
**Figma:** Toolbar kontextuell zum ausgewaehlten Element (Selection-basiert)

---

## 3. Command Palette (Cmd+K) ⭐

**Job:** Power-User Effizienz. Jede Aktion ohne Maus erreichbar.
**Feste Elemente:**

- Modal Overlay, zentriert, abgedunkelter Hintergrund
- Search Input oben mit Placeholder
- Ergebnis-Liste darunter, scrollbar, nach Kategorie gruppiert
- Zuletzt genutzte Items vor dem Tippen angezeigt
- Keyboard Shortcut Hints neben jeder Aktion

**Verhalten:**

- Cmd+K (Mac) / Ctrl+K (Windows) — universeller Standard
- Fuzzy Search, Echtzeit-Filterung beim Tippen
- Pfeiltasten navigieren, Enter ausfuehrt, Escape schliesst
- Kontextbewusst: zeigt relevante Aktionen fuer aktuelle Ansicht
- Nicht nur Suche — es MACHT Dinge (navigieren, erstellen, Status aendern, zuweisen)
- VS Code Pattern: Prefix-Modifier aendern Modus (">" fuer Commands)

**Ergebnis-Kategorien:**

- Navigation (zu Seite/View gehen)
- Actions (Issue erstellen, Status aendern)
- Recent Items
- Settings
- Help/Docs

**Freiheit:** NIEDRIG. Das Pattern ist standardisiert. Freiheit nur in den spezifischen Aktionen.

**Nicht verhandelbar fuer YC-Grade:** "A secret keyboard shortcut is usually not a way to go. Hint about the palette in the UI."

---

## 4. Main Content Area ⭐⭐

**Job:** Die eigentliche Arbeit. Hier verbringt der User 90% seiner Zeit.

### View Types (jedes SaaS Tool braucht mindestens 3)

1. **List View** — vertikale Reihen, sortierbare Spalten, Standard fuer daten-lastige Tools
2. **Board/Kanban View** — Spalten nach Status/Kategorie, Drag-and-Drop Karten
3. **Timeline/Gantt View** — horizontale zeitbasierte Balken
4. **Split View** — Liste links, Detail-Panel rechts (Master-Detail)
5. **Detail/Fullscreen View** — einzelnes Item auf volle Content Area expandiert

### Tabellen/Listen Konventionen

- Sticky Headers (bleiben beim Scrollen sichtbar)
- Subtiler Hover-Highlight fuer Zeilen-Tracking
- Rechtsbuendige Zahlen mit Dezimal-Alignment
- Sortierbare Spaltenheader (Klick fuer asc/desc)
- Bulk Selection Checkboxes + Bulk Action Toolbar die bei Auswahl erscheint
- Pagination oder Infinite Scroll mit klaren Indikatoren
- Inline Editing wo angemessen (Klick auf Zelle zum Editieren)

### Dashboard-spezifisch (F-Pattern Layout)

- Oben-links = wichtigste Metrik (North Star)
- Obere Reihe = primaere KPIs horizontal
- Linke Spalte = sekundaere Metriken absteigend
- Bar Charts > Pie Charts (immer)
- Sparklines fuer Inline-Trend-Indikatoren
- Wenn der User hovern muss um ein Chart zu verstehen, hat die Visualisierung versagt

**Freiheit:** HOCH — das ist die Zone wo sich SaaS Produkte am staerksten unterscheiden. Die Form folgt dem Produkt.

**Linear:** Issues als List + Board + Timeline. Split View fuer Detail. Interaktiver Kanban mit Sub-Grouping.
**Notion:** Frei konfigurierbare Datenbanken als Table/Board/Gallery/Calendar/Timeline
**Retool:** Drag-and-Drop Builder fuer Custom Dashboards mit Components
**Figma:** Canvas als Haupt-View, Properties Panel rechts, Layers Panel links

---

## 5. Detail View / Side Panel

**Job:** Einzelnes Item anzeigen und editieren ohne den Kontext zu verlieren
**Feste Elemente:**

- Titel/Name prominent oben
- Status/Metadata als Tags/Chips unter dem Titel
- Beschreibung/Body als Rich-Text Editor
- Properties Panel (rechte Seite oder unter dem Body): Assignee, Priority, Labels, Due Date
- Activity Feed / Kommentare unten
- Sub-Items / Linked Items
- Action Buttons: Status aendern, zuweisen, archivieren

**Pattern-Varianten:**

- **Side Drawer** (von rechts reinschieben, non-blocking) — fuer schnelle Einblicke
- **Full-Page** — fuer komplexes Editing
- **Inline Expand** — Accordion innerhalb der Liste

**Freiheit:** MITTEL. Das Layout ist standardisiert, aber die Properties und Actions sind produktspezifisch.

**Linear:** Side Panel mit Properties rechts, Rich-Text Body, Activity Feed, Sub-Issues
**Notion:** Full-Page mit verschachtelten Blocks, Properties als Datenbank-Felder oben

---

## 6. Empty / Loading / Error States

**Job:** Nie den User in einer Sackgasse lassen

### Empty States

- Headline ("No issues yet")
- Description (warum leer + was als naechstes tun)
- CTA (Secondary Button oder Text-Link zum Befuellen)
- Optional: einfache, monochrome Illustration
- Empty States sind Onboarding-Gelegenheiten

### Loading States

- Skeleton Screens > Spinners (Layout-Form zeigen waehrend Daten laden)
- Progressive Loading: Content zeigen sobald er ankommt
- Optimistic UI: Aktion sofort zeigen, im Hintergrund synchen
- Loading-Indikatoren auf einzelnen Komponenten, NIE full-page Blocker

### Error States

- Inline Errors fuer Formularfelder (roter Rahmen + Nachricht)
- Toast fuer nicht-kritische Fehler (auto-dismiss)
- Banner fuer systemweite Issues (oben, dismissible)
- Modal nur fuer kritische/blockierende Fehler
- Immer: Klartext, was passiert ist + wie man es fixt
- Nie: Technischer Jargon, Error Codes ohne Erklaerung, Sackgassen ohne Recovery

**Freiheit:** KEINE. Diese Patterns sind standardisiert und nicht verhandelbar.

---

## 7. Modals / Overlays

**Job:** Fokussierte Interaktionen die temporaer den Hauptflow unterbrechen

### Wann Modals

- Destruktive Bestaetigungen ("Delete this project?")
- Fokussierte Creation Flows (schnelle Formulare)
- Dringende Unterbrechungen (Subscription abgelaufen)
- Preview vor Submit bei High-Impact Aktionen

### Wann KEINE Modals

- Editierbare Formulare mit vielen Feldern → eigene Seite
- Content der viel Scrollen erfordert → eigene Seite
- Verschachtelte Modals (Modal-in-Modal = IMMER schlecht)
- Alles was inline oder als Drawer geht

### Confirmation Dialog Pattern

- Klare Headline: "Delete project?"
- Beschreibung der Konsequenzen
- Zwei Buttons: Cancel (secondary) + Confirm (primary, rot fuer destruktiv)
- Type-to-Confirm fuer irreversible Aktionen (Projektname eintippen)

**Freiheit:** KEINE. Modal-Patterns sind UX-Konventionen, keine Designentscheidungen.

---

## 8. Notifications

**Job:** User informieren ohne den Flow zu unterbrechen

**Tier 1 — Toast/Snackbar (ephemeral):**

- 3-5 Sekunden sichtbar, auto-dismiss
- Bottom-left oder top-right
- Bestaetigt User-Aktionen ("Issue created", "Saved")
- Snackbar-Variante mit Action Button ("Undo")

**Tier 2 — Notification Center / Inbox (persistent):**

- Bell Icon im Header, Badge Count fuer ungelesen
- Dropdown oder dedizierte Seite
- Real-time Updates via WebSocket/SSE
- Deep-Link: Klick geht zum relevanten Kontext

**Tier 3 — Banner/Alert (systemweit):**

- Volle Breite oben in der App
- Maintenance, Outages, Subscription Warnings
- Farbkodiert: gelb=warning, rot=critical, blau=info

**Freiheit:** KEINE. Drei-Tier System ist Standard.

---

## 9. Settings

**Job:** Konfiguration ohne die Hauptarbeit zu stoeren
**Feste Elemente:**

- Eigene Sub-Sidebar oder vertikale Tabs (getrennt von Haupt-Sidebar)
- Gruppiert nach: Account, Workspace, Billing, Notifications, Integrations, Appearance
- Auto-Save mit Toast ODER Section-Level Save Buttons
- Toggle Switches fuer On/Off Preferences
- Danger Zone ganz unten (Delete Workspace, Transfer Ownership)
- Invite Members per Email, Role-Based Access (Owner/Admin/Member/Guest)

**Freiheit:** NIEDRIG. Struktur ist standardisiert, nur die spezifischen Settings variieren.

---

## 10. Design System Grundlagen

**Job:** Visuelle Konsistenz ueber das gesamte Produkt

**Spacing:** 8px Base Grid — alle Abstande sind Vielfache von 8
**Typography:** Inter oder aehnliche neutrale Sans-Serif. Monospace fuer Code/IDs.
**Colors:** Semantic Tokens (text-primary/secondary/tertiary, status-Farben nur fuer Bedeutung)
**Theming:** Dark Mode ist Pflicht fuer Developer/Power-User Tools. LCH Color Space fuer perceptually uniform Themes.
**Breakpoints:** 1280px, 1024px, 768px, 640px

**Freiheit:** MITTEL. Farbpalette und Branding sind frei, aber das System (8px Grid, semantische Tokens, Dark Mode) ist Standard.

---

# Zusammenfassung: Wo liegt die Freiheit?

| #   | Komponente                 | Freiheit | Warum                                                 |
| --- | -------------------------- | -------- | ----------------------------------------------------- |
| 1   | Sidebar Navigation         | Keine    | Muss sofort funktionieren, universelles Pattern       |
| 2   | Header / Toolbar           | Niedrig  | Kontextuelle Kontrolle, standardisierte Elemente      |
| 3   | Command Palette            | Keine    | UX-Konvention, nicht verhandelbar fuer YC-Grade       |
| 4   | **Main Content Area**      | **Hoch** | **Hier zeigt sich was das Produkt einzigartig macht** |
| 5   | Detail View / Side Panel   | Mittel   | Layout standardisiert, Properties produktspezifisch   |
| 6   | Empty/Loading/Error States | Keine    | UX-Hygiene, nicht verhandelbar                        |
| 7   | Modals / Overlays          | Keine    | UX-Konventionen                                       |
| 8   | Notifications              | Keine    | Drei-Tier System ist Standard                         |
| 9   | Settings                   | Niedrig  | Struktur standardisiert                               |
| 10  | Design System              | Mittel   | System ist Standard, Branding ist frei                |

**Die Main Content Area (Section 4) macht 80% des einzigartigen Charakters eines SaaS Dashboards aus.** Alles drumherum ist ein festes Geruest — noch fester als bei Landing Pages.

Der Unterschied zu Landing Pages: Bei einer Landing Page sind Product in Action + Capabilities die Freiheitszonen (WAS man zeigt). Bei einem Dashboard ist die Main Content Area die Freiheitszone (WIE man arbeitet). Alles andere — Sidebar, Header, Command Palette, Modals, Notifications, Settings — folgt strengen Konventionen.

# YC No-Gos

- Full-Page Loading Spinners die allen Content blockieren
- Sackgassen-Fehlerseiten ohne Recovery-Pfad
- Verschachtelte Modals (Modal-in-Modal)
- Pie Charts fuer Datenvergleiche
- Keine Keyboard Navigation
- Mehr als 2 Klicks zu irgendeinem Ziel
- Inkonsistentes Spacing oder Alignment
- Flash of Unstyled Content
- Browser alert()/confirm() statt Custom Modals
- Horizontale Top-Nav allein fuer komplexe Multi-Modul Tools
- Settings ohne Suchfunktion

# YC Must-Haves

- Command Palette (Cmd+K)
- Keyboard Shortcuts fuer alle primaeren Aktionen
- Dark Mode
- Responsive Design
- Sub-200ms Interaktions-Antwortzeiten
- Skeleton Loading States
- Empty States mit klaren CTAs
- Undo fuer destruktive Aktionen
- Real-time Updates (WebSocket)
- Konsistentes 8px Spacing Grid

# Wiederkehrende Workflow Patterns

1. **List > Detail Drawer** — Klick auf Zeile, Side Panel oeffnet mit Details
2. **Search + Filter > Table > Bulk Actions** — Standard Datenmanagement Flow
3. **Inline Create > Modal Confirm > Return** — schneller "+" Button, leichtgewichtige Erstellung
4. **Full-Screen Modal > Form > Return** — fuer komplexe Erstellung (viele Felder)
5. **Multi-Step Wizard** — Onboarding, Setup Flows, komplexe Konfiguration
6. **Auto-Save > Toast Confirmation** — Inline editieren, auto-persist, Toast bestaetigt
7. **Action > Undo Snackbar** — Delete/Archive triggert Snackbar mit Undo
8. **Kanban Board > Card Click > Side Panel** — Board Item oeffnet Drawer
9. **Notification > Deep Link > Inline Action** — Klick auf Notification, landet im Kontext
10. **Form Builder > Live Preview** — Split Pane, links editieren, rechts Vorschau

# Validierung

Blueprint getestet gegen 4 YC SaaS Produkte:

| Produkt | Branche            | Sidebar                   | Header                   | Cmd+K            | Main Content ⭐              | Detail View          | States      | Modals | Notifications | Settings       |
| ------- | ------------------ | ------------------------- | ------------------------ | ---------------- | ---------------------------- | -------------------- | ----------- | ------ | ------------- | -------------- |
| Linear  | Project Management | ✅ Workspace>Teams>Issues | ✅ View Switcher+Filters | ✅ Gold Standard | ✅ List+Board+Timeline+Split | ✅ Side Panel        | ✅ Skeleton | ✅     | ✅ 3-Tier     | ✅ Sub-Sidebar |
| Notion  | Knowledge/Docs     | ✅ Page Tree              | ✅ Minimal, View Tabs    | ✅               | ✅ Frei konfigurierbare DBs  | ✅ Full-Page Blocks  | ✅          | ✅     | ✅            | ✅             |
| Retool  | Internal Tools     | ✅ App-Liste+Ordner       | ✅ Builder Toolbar       | ✅               | ✅ Drag-Drop Components      | ✅ Component Config  | ✅          | ✅     | ✅            | ✅             |
| Figma   | Design             | ✅ File Tree              | ✅ Kontext-Toolbar       | ✅               | ✅ Canvas+Layers+Properties  | ✅ Selection-basiert | ✅          | ✅     | ✅            | ✅             |
|         |                    |                           |                          |                  |                              |                      |             |        |               |                |

**Ergebnis:** 10-Komponenten Struktur passt auf alle 4 Produkte. Die Inverted-L Shell ist universell. Command Palette ist bei allen vorhanden. Die Main Content Area ist die einzige Zone mit hoher Freiheit — und genau dort unterscheiden sich die Produkte fundamental (Issues vs. Pages vs. Components vs. Canvas).

---

# Implementation Guide: shadcn/ui

## Landing Page ↔ Dashboard Styling Regeln

Das Dashboard teilt Brand-DNA mit der Landing Page, ist aber ein anderes Medium. Die Regel:

**Was sich TEILT (Brand Continuity):**

- Akzentfarbe (Teal fuer OfficeOS) → Primary Buttons, Active States, Links
- Font Family → gleiche Schrift ueberall
- Logo → identisch, oben-links in Sidebar
- Border Radius → gleiche Rundung auf Buttons, Cards, Inputs
- Brand Voice → Tonalitaet in Empty States, Tooltips, Onboarding

**Was sich AENDERT (zwangslaeufig):**

| Eigenschaft      | Landing Page               | Dashboard                         |
| ---------------- | -------------------------- | --------------------------------- |
| Akzentfarbe      | Flaechig, dekorativ        | Sparsam, nur interaktiv           |
| Whitespace       | Grosszuegig (Storytelling) | Dicht (Informationsdichte)        |
| Typography Scale | 14-72px                    | 12-24px                           |
| Background       | Gradient/Textur erlaubt    | Flat, neutral (#fafafa / #0a0a0a) |
| Decoration       | Illustrationen, Glows      | Nichts, rein funktional           |
| Dark Mode        | Optional                   | Pflicht                           |

Die Formel: **Gleiche Gene, anderer Koerper.** Font, Farbe, Radius = DNA. Spacing, Dichte, Dekoration = Medium.

---

## Blueprint → shadcn Mapping

Jede Blueprint-Komponente mapped auf konkrete shadcn Komponenten und Blocks:

### 1. Sidebar Navigation → `Sidebar` Block

shadcn hat die Sidebar als vollstaendige Loesung. Block `sidebar-07` ist der beste Startpunkt (collapses to icons).

```
SidebarProvider
├── Sidebar (side="left", variant="inset", collapsible="icon")
│   ├── SidebarHeader          → Workspace Switcher (Dropdown)
│   ├── SidebarContent         → scrollbarer Navigationsbereich
│   │   ├── SidebarGroup       → "Primaer" (Inbox, My Issues, Favorites)
│   │   ├── SidebarGroup       → "Teams" (kollabierbar via Collapsible)
│   │   │   └── SidebarMenuSub → Projekt-Unternavigation
│   │   └── SidebarGroup       → "Custom Views"
│   ├── SidebarFooter          → Settings, Help, User Avatar
│   └── SidebarRail            → Resize Handle
├── SidebarInset               → wrapped den Main Content
└── SidebarTrigger             → Toggle Button (Cmd+B)
```

**CSS Variables fuer Brand:**

```css
--sidebar-background: /* gedaempfter als Content-BG */ --sidebar-foreground:
  /* text-secondary */
  --sidebar-primary: /* dein Teal */ --sidebar-accent: /* hover/active state */;
```

**Wichtig:** `group-data-[collapsible=icon]:hidden` auf Labels damit bei Collapse nur Icons bleiben.

### 2. Header / Toolbar → `Breadcrumb` + `DropdownMenu` + `Button`

Kein eigener shadcn Block — selbst zusammenbauen:

```
<header>
  <Breadcrumb />                    → Seitenposition
  <Tabs />                          → View Switcher (List/Board/Timeline)
  <div className="ml-auto flex">
    <DropdownMenu />                → Filter + Display Options
    <Button variant="default" />    → Primary Action ("Create Issue")
    <Button variant="ghost" />      → Search/Cmd+K Trigger
    <DropdownMenu />                → Notifications Bell
    <DropdownMenu />                → User Avatar
  </div>
</header>
```

### 3. Command Palette → `CommandDialog`

shadcn `Command` basiert auf cmdk. Direkt nutzbar:

```
CommandDialog (Cmd+K oeffnet Modal)
├── CommandInput              → Fuzzy Search
├── CommandList
│   ├── CommandEmpty           → "No results found"
│   ├── CommandGroup heading="Navigation"
│   │   └── CommandItem        → mit CommandShortcut ("G then I")
│   ├── CommandSeparator
│   ├── CommandGroup heading="Actions"
│   │   └── CommandItem        → "Create Issue" + Shortcut "C"
│   └── CommandGroup heading="Recent"
│       └── CommandItem
```

**Erweitern mit:** Context-Awareness (aktuelle View bestimmt welche Actions erscheinen), Prefix-Modifier (VS Code Pattern).

### 4. Main Content Area → Block `dashboard-01` als Startpunkt

Das ist die Freiheitszone — hier baut man produktspezifisch. shadcn liefert die Bausteine:

**Fuer List Views:**

- `DataTable` (basiert auf TanStack Table) → Sortierung, Filterung, Pagination, Bulk Selection
- `Table` fuer einfachere Listen ohne volle Interaktivitaet

**Fuer Board/Kanban:**

- Kein shadcn Block — nutze `dnd-kit` + `Card` Komponenten
- Jede Spalte ist ein Container, Karten sind draggable Cards

**Fuer Detail/Split View:**

- `ResizablePanelGroup` + `ResizablePanel` → Master-Detail Split
- Links Liste, rechts Detail Panel, mit Drag-Resize Handle

**Fuer Dashboard/Analytics:**

- `ChartContainer` + Recharts (shadcn Charts sind Recharts-Wrapper)
- `Card` fuer KPI Karten mit Trend-Indikatoren
- Kein Pie Chart — `BarChart` oder `AreaChart`

### 5. Detail View / Side Panel → `Sheet`

```
Sheet (side="right")
├── SheetHeader
│   ├── SheetTitle          → Item-Name
│   └── SheetDescription    → Status Badges
├── SheetContent
│   ├── Textarea            → Rich-Text Body (oder Tiptap/ProseMirror)
│   ├── Separator
│   ├── Properties Grid     → Assignee, Priority, Labels (Select, Badge)
│   ├── Separator
│   └── Activity Feed       → Kommentare (Avatar + Text + Timestamp)
└── SheetFooter
    └── Action Buttons      → Status aendern, Archivieren
```

Alternativ: `Dialog` fuer Full-Page Detail, `Sheet` fuer Side Drawer.

### 6. Empty / Loading / Error States

**Empty States:** Kein shadcn Block — selbst bauen als wiederverwendbare Komponente:

```
<div className="flex flex-col items-center justify-center py-16">
  <Icon />
  <h3>Headline</h3>
  <p className="text-muted-foreground">Description</p>
  <Button variant="secondary">CTA</Button>
</div>
```

**Loading:** `Skeleton` Komponente — fuer jede View eine Skeleton-Variante bauen (TableSkeleton, CardSkeleton, etc.)

**Errors:**

- Inline: Formular-Felder haben eingebaute Error States
- Toast: `Sonner` (shadcn Default Toast)
- Banner: selbst bauen mit `Alert` Komponente

### 7. Modals / Overlays

| Pattern                  | shadcn Komponente                        |
| ------------------------ | ---------------------------------------- |
| Destructive Confirmation | `AlertDialog` (Cancel + Confirm rot)     |
| Quick Creation           | `Dialog`                                 |
| Side Drawer              | `Sheet`                                  |
| Dropdown Actions         | `DropdownMenu`                           |
| Context Menu             | `ContextMenu` (Rechtsklick)              |
| Type-to-Confirm          | `AlertDialog` + `Input` (Name eintippen) |

**Nie:** Dialog in Dialog verschachteln.

### 8. Notifications

| Tier                | shadcn Komponente                                  |
| ------------------- | -------------------------------------------------- |
| Toast (ephemeral)   | `Sonner` — auto-dismiss, mit Undo Action           |
| Notification Center | `Popover` am Bell Icon + eigene Liste              |
| System Banner       | `Alert` (variant destructive/warning) oben fixiert |

### 9. Settings → Eigene Route mit Sub-Navigation

```
/settings
├── /settings/profile        → Form mit Input, Avatar Upload
├── /settings/workspace      → Form mit Input, Member Table
├── /settings/billing        → Card mit Plan Info, Button "Upgrade"
├── /settings/notifications  → Switch Toggles Grid
├── /settings/integrations   → Card Grid mit Connect Buttons
└── /settings/appearance     → Theme Toggle, Select fuer Language
```

Navigation: `Tabs` (vertikal) oder eigene Mini-Sidebar. Danger Zone ganz unten mit `Button variant="destructive"`.

### 10. Design System Setup

In `globals.css` die shadcn CSS Variables ueberschreiben:

```css
:root {
  /* OfficeOS Brand Tokens */
  --primary: /* dein Teal HSL */;
  --primary-foreground: /* weiss */;
  --radius: /* dein Button Radius */;

  /* Dashboard-spezifisch: dichter als Landing Page */
  --sidebar-background: /* leicht gedaempft */;
  --sidebar-foreground: /* text-secondary */;
  --sidebar-primary: /* Teal */;
}

.dark {
  /* Dark Mode Tokens — Pflicht */
  --background: 0 0% 4%;
  --foreground: 0 0% 95%;
  --primary: /* Teal angepasst fuer Dark */;
}
```

**8px Grid:** Tailwind `space-2` = 8px, `p-2` = 8px. Alles in 2er-Schritten.

---

## Installations-Reihenfolge

Pragmatische Reihenfolge um schnell ein funktionierendes Dashboard zu haben:

| Schritt | Was             | shadcn Befehl                                                   |
| ------- | --------------- | --------------------------------------------------------------- |
| 1       | Projekt Setup   | `npx shadcn@latest init`                                        |
| 2       | Sidebar Shell   | `npx shadcn@latest add sidebar` + Block `sidebar-07`            |
| 3       | Command Palette | `npx shadcn@latest add command` + `dialog`                      |
| 4       | Basic Content   | `npx shadcn@latest add table data-table card`                   |
| 5       | Detail View     | `npx shadcn@latest add sheet tabs separator badge`              |
| 6       | Interactions    | `npx shadcn@latest add alert-dialog dropdown-menu context-menu` |
| 7       | Feedback        | `npx shadcn@latest add sonner skeleton alert`                   |
| 8       | Forms           | `npx shadcn@latest add input select switch textarea`            |
| 9       | Charts          | `npx shadcn@latest add chart`                                   |
| 10      | Brand Tokens    | CSS Variables in `globals.css` ueberschreiben                   |

Nach Schritt 3 hat man bereits eine funktionierende Inverted-L Shell mit Command Palette. Der Rest ist inkrementell.
