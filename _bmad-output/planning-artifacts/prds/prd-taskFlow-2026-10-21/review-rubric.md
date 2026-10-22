# PRD Quality Review — TaskFlow

## Overall verdict
This is a tight, honest PRD for its size. The thesis ("*how much is left?*" plus a portfolio piece someone else can run) carries through to the features, the binding §6 non-goals and a cut order in §8. All seven release checks in §10 trace to FRs that can be tested. The risk is in a few places where the PRD names a known trap but leaves the deciding detail out. It does not say whether *defined* numeric or differently-cased enum values pass AC-3. It does not say what an edit does with omitted fields. It does not say how AC-4 (survives a restart) is shown when the integration tests are in-memory. And the "project's task view" is never defined. Settle those in an hour of edits and the PRD is ready for architecture and stories.

## Decision-readiness — strong
Decisions are stated as decisions, not as "considerations". Examples: "Project names do not need to be unique" (FR-1), "the last save wins" (§6), "Total always equals completed + pending" (FR-10) and the FR-7 due-date rule for unchanged past dates. §8 names what gets given up under pressure: "Cut UI polish first, then filters inside a project's task view (FR-9, bullet 3). Never cut validation or tests." That is a real trade-off with a named victim. §12 "None blocking" is believable for this scope, and it points to the addendum for what was deliberately deferred (routes, folder layout, migrations vs `EnsureCreated`).

The one buried decision is enum strictness (see Done-ness). The §8 risk row says ".NET binds numbers to enums without checking them". The mitigation says to reject "undefined values", but neither place decides whether *defined* numbers (`2`) or other casings (`"high"`) are valid.

### Findings
- **low** Addendum says "not final decisions" but holds binding conventions (addendum.md, intro vs "Conventions carried from CLAUDE.md"). The header "These notes are a starting point, not final decisions" also covers `TaskItem` naming, string enums, DB-level cascade and SQLite in-memory tests, which CLAUDE.md makes mandatory. An architect could read them as open to change. *Fix:* label the Stack and Conventions sections "binding (from CLAUDE.md)" and keep "to confirm" for the API surface and run-command sections only.

## Substance over theater — strong
There is no furniture. Two personas (Aisha, Sam), and both drive decisions: Aisha's journey sets the filter, the dashboard and the past-date behaviour, and Sam's sets NFR-3, SM-2 and AC-7. The Vision is specific to this product ("clone the repository, start the app with the documented commands, and see a clean, tested full-stack design"). Most NFRs have product-specific limits: NFR-7 "up to 500 tasks … under 1 second", SM-2 "≤ 10 minutes", NFR-8 named browsers plus keyboard access. NFR-5 is a real, stack-specific constraint, not boilerplate.

### Findings
- **low** NFR-9 is an adjective, not a limit (§5 NFR-9). "Code a beginner can read" can't be tested. That's acceptable for a learning project, and SM-C1 partly holds it in place. *Fix:* optionally add one checkable proxy, e.g. "controllers contain no EF Core queries; no repository or mediator layer unless a story justifies it".

## Strategic coherence — strong
The thesis is clear and two-sided: a user who can't answer "how much is left?" (§1), and a builder who must ship a runnable, tested portfolio piece in 7 × 3–4 h (§7). Every FR serves one of those. The dashboard (FR-10) is the payoff of the problem statement. Strict validation (FR-5, FR-11) serves "bad input is caught, not silently saved" (§2.1). Persistence and cascade serve "trust that data is still there tomorrow". The success metrics measure delivery and runnability rather than user adoption. That fits a learning and portfolio MVP. SM-C1 is a real counter-metric aimed at the right failure (feature and coverage chasing). The MVP is clearly a problem-solving MVP, and the scope logic matches.

No findings.

## Done-ness clarity — adequate
Most FRs have verifiable consequences: explicit lengths, trimming, defaults, 400 vs 404, "nothing is saved", the dashboard formula and an empty state of 0/0/0. For the seven release checks:

| Check | Status |
|---|---|
| AC-1 valid title | Testable (FR-5 bullet 1) |
| AC-2 valid project | Testable (FR-5 bullet 2) |
| AC-3 invalid status/priority | **Ambiguous** at the edges |
| AC-4 persists after restart | **Verification method unspecified** |
| AC-5 cascade delete | Testable at DB level (FR-4 bullet 3) |
| AC-6 dashboard totals | Testable (FR-10) |
| AC-7 local run | Testable (NFR-3, SM-2) |

The gaps below would each cause a story or test author to guess.

### Findings
- **high** It is unclear which enum inputs are "defined values" (§4.2 FR-5 bullet 4; §8 row 2; addendum "Conventions" bullet 2). FR-5 says status and priority "accept only their defined values (§3)", but the examples it rejects are "undefined numbers (e.g. `7`), misspellings and empty strings". That leaves open whether a *defined* number (`"priority": 2`) or a different case (`"high"`, `"inprogress"`) is accepted. `JsonStringEnumConverter` accepts both by default. Query-string binding for FR-9 filters also parses numbers and ignores case. The addendum's "Undefined values (including numbers) are rejected" can be read either way. AC-3 is a release check, so its tests need a single answer. *Fix:* add one sentence to FR-5 (and point FR-9 to it), e.g. "Only the exact names in §3 are accepted, matched case-insensitively; numbers of any kind are rejected (body and query string)." Then mirror it in the addendum (`allowIntegerValues: false`, plus a check for query parameters).
- **medium** It is unclear how AC-4 "persists after restart" is shown (§10 AC-4; §7 SM-1; §5 NFR-5). SM-1 allows "an automated test or a written manual check" but doesn't say which applies to which check. NFR-5 puts integration tests on in-memory SQLite, and that database cannot show data surviving a backend restart. Without a decision, AC-4 risks being "covered" by a test that never touches the file on disk. *Fix:* in §10, add a "Verified by" column. For AC-4, choose either (a) a written manual check: create/edit a task → stop `dotnet run` → start → GET shows the change, or (b) one integration test against a temporary SQLite *file* that rebuilds the host. Also mark AC-7 as manual.
- **medium** FR-7 doesn't say what an edit does with omitted fields (§4.2 FR-7 bullets 1–2). FR-7 says "the … status, priority … rules from FR-5 apply", and FR-5 includes "Status defaults to `Todo`. Priority defaults to `Medium`." For an edit this is ambiguous: an edit that omits priority could silently reset it to `Medium`. The same edit omitting the due date *clears* it ("Clearing the due date is allowed"). Is the edit a full replacement with required fields, or a partial update? That decides the DTO shape and the AC-4 test. *Fix:* add to FR-7: "An edit sends the full task; title, project, status and priority are required on edit (no defaults); an absent or null due date clears it."
- **medium** "Today" doesn't fix the time zone, so date tests may be non-deterministic (§3 "Today"; §8 row 4; §11 §3 entry). The glossary says "the current calendar date on the machine running the backend". The index adds "local date", and the risk row names "time zones" as the hazard. The authoritative definition doesn't settle local vs UTC. Near midnight the two give different answers to "yesterday/today/tomorrow". The PRD requires those tests (§8) but doesn't require a controllable clock. *Fix:* change the glossary to "the current date in the backend machine's local time zone". Add to NFR-5 or the addendum: "date rules read the current date through an injectable clock (e.g. `TimeProvider`) so tests are deterministic."
- **low** FR-7 has no 404 case (§4.2 FR-7). FR-3 and FR-8 both state "does not exist returns HTTP 404"; FR-7 doesn't. *Fix:* add "Editing a task that does not exist returns HTTP 404."
- **low** It is unclear whether stored values are trimmed (§4.1 FR-1 bullet 1; §4.2 FR-5 bullet 1). Length is measured "after trimming spaces", but the PRD doesn't say whether the *saved* name or title is the trimmed value. It also doesn't say whether a whitespace-only description is stored or treated as empty. *Fix:* "Names and titles are saved trimmed; a blank description is saved as empty (null)."
- **low** The response to an unknown `projectId` filter is unspecified (§4.3 FR-9; addendum "Likely API surface"). The addendum adds a `projectId` query parameter, but nothing says whether an unknown id returns 400, 404 or an empty list. *Fix:* choose one (an empty list is simplest; 404 matches FR-2) and state it in FR-9 or the addendum.
- **low** It is unclear when the dashboard refreshes (§4.4 FR-10 bullet 2). The refresh rule is "the next load shows the new numbers", but the PRD doesn't say whether the dashboard is its own page or sits beside the task list. If it sits beside the list, UJ-2's "the dashboard totals drop to match" implies an automatic refresh that FR-10 doesn't require. *Fix:* state where the dashboard lives, or add "If the dashboard is visible while data changes, it refreshes after the change."

## Scope honesty — strong
§6 does real work. It names the tempting adjacent features (search, sorting, Kanban, overdue counts, undo, soft delete). It settles concurrency explicitly ("last save wins"), and it says what comes first in v2 (per-project counts). "§6 is binding" (§8) gives it teeth. The open-items count (13 inline `[ASSUMPTION]` tags, 0 `[NOTE FOR PM]`, 0 open questions) suits a low-stakes solo MVP. Every assumption is a sensible default, not a hidden product bet.

### Findings
- **low** Pagination is omitted silently (§6). With "up to 500 tasks" (NFR-7) and "newest first" (FR-6), a reader could assume either pagination or none. *Fix:* add "Pagination — the task list returns all matching tasks" to §6.
- **low** Two `[ASSUMPTION]` tags are placed ambiguously (§2 Primary user; §2.2 UJ-1). In §2 the tag follows "There is no team and no sharing", which reads as if single-user were assumed. In fact it is a confirmed constraint (CLAUDE.md "Single-user MVP"); the index says the assumption is the task volume. In UJ-1 the tag follows the edge-case sentence, but the index says the whole journey is PM-drafted. *Fix:* move the §2 tag to after "(tens to a few hundred tasks)". Put the UJ-1 tag at the end of the title line or the first sentence.

## Downstream usability — adequate
The glossary is present and used consistently: "Completed/Pending/Done", "validation error" and "Today" all hold across the FRs, UJs, SMs and ACs. "Task" vs `TaskItem` is handled explicitly. IDs are contiguous and unique (FR-1–11, NFR-1–9, SM-1–4 + SM-C1, AC-1–7, UJ-1–3). Every UJ has a named protagonist. The weak point is one concept that is used but never defined. There is also one bad cross-reference.

### Findings
- **medium** "A project's task view" is referenced but never specified (§4.1 description "Opening a project shows its tasks (see §4.3)"; FR-2 story "open one so that I can find its tasks"; FR-9 bullet 3; §8 cut-order row). No acceptance criterion says what opening a project shows. FR-2's criteria cover only the list and the 404. FR-6 covers the global list. The §4.1 cross-reference points to §4.3 (Filtering), not to a definition. Story creation will have to invent this screen, and the §8 cut plan refers to something with no scope. *Fix:* add an FR-2 bullet: "Opening a project shows its name, description and its tasks (same columns and order as FR-6), with an empty state when it has none." Retarget the §4.1 cross-reference to FR-2/FR-6. Optionally add "Task list" and "Project task view" to §3.
- **low** "AC" is overloaded (§0 vs §10). §0 says each FR "has testable acceptance criteria" (unnumbered bullets), while §10 uses AC-1–AC-7 for *release* checks. Stories will contain both. *Fix:* rename the §10 IDs to RC-1–RC-7 (release checks), or number the FR bullets (FR-5.1, FR-5.2 …). Numbering also replaces "FR-9, bullet 3" in §8.

## Shape fit — strong
This is a solo, hobby-scale product at the top of a planning chain (architecture → epics/stories), and the shape matches. There are three short UJs with named protagonists, a capability-style FR list, and limits on the few NFRs that matter. No enterprise sections were forced in. The technical material is in the addendum, which keeps the PRD readable. A few HTTP status codes and `dotnet test`/`npm run build` appear in the PRD itself, despite §0's "Technical decisions … are in addendum.md, not here". That's fine at this scale, because the stack is fixed by constraints and the release checks need those codes.

No findings.

## Mechanical notes
- **Glossary drift:** minimal. "Today" (glossary) vs "local date" (§11) is the only real drift; it's covered under Done-ness. "renames or edits" in the §4.1 description is redundant wording ("edits" covers it). "task list", "project list" and "project's task view" are used without glossary entries (see Downstream).
- **ID continuity:** clean; no gaps or duplicates. One bad cross-reference: the §4.1 "see §4.3" should point to FR-2/FR-6. SM-1 says "Validates FR-1–FR-11", but the §10 checks trace only to FR-4, 5, 7, 9 and 10. FR-1–3, 6, 8 and 11 are validated through the per-story DoD (§9), not through SM-1. *Fix:* change SM-1 to "Validates the release checks; per-FR coverage is via §9 DoD."
- **Addendum consistency:** the addendum's "404 for unknown ids" could be read as contradicting FR-5's "A missing or unknown project is rejected with a validation error" (400). The intended split is route id → 404, body reference → 400. *Fix:* say "404 for unknown ids in the route; an unknown `projectId` in a request body is a 400 validation error."
- **Assumptions Index roundtrip:** complete. All 13 inline tags are indexed and every index entry appears inline. Two tags are placed ambiguously (see Scope honesty).
- **UJ protagonists:** all named (Aisha ×2, Sam) with context inline.
- **Required sections:** all present for the agreed stakes: problem/vision, users, glossary, FRs, NFRs, non-goals, metrics with counter-metric, risks, DoD, release checks, assumptions, open questions.
- **Ordering field:** FR-6 "newest first" implies a creation timestamp (or relying on id order) that is in neither the glossary nor the addendum, and project-list order is unspecified. *Fix:* note "ordered by creation time, newest first" for both lists, or state that id order is acceptable.
