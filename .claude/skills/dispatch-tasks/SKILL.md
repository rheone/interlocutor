---
name: dispatch-tasks
description: Dispatch work to one or more sub-agents - serially as a dependent chain, or in parallel as an independent batch - and relay each one's status into this thread as it finishes. Use when the user says "dispatch", asks to do something "for each" item in a list, wants work run "in parallel", wants steps done "serially" one after another, or wants work handed off to a background "thread".
argument-hint: "What should be dispatched, and serially or in parallel?"
---

Run work on sub-agents instead of doing it inline, and keep this thread posted as each piece lands. Every run is either a **chain** (serial, dependent, ordered) or a **batch** (parallel, independent, unordered) - never both in the same run.

1. **Gather the tasks.** If the invocation already lists or enumerates the work, use that list as given - it's unambiguous. If it doesn't (e.g. "dispatch these"), derive a numbered task list from the recent conversation. Each task must be self-contained enough to hand to a sub-agent as its whole prompt.
   Done when: a numbered task list exists.

2. **Confirm only what you inferred.** Skip this step entirely when the list came from step 1's explicit path - re-confirming the user's own words back to them is a no-op. When the list was derived from context, show it and get a yes before continuing; don't proceed on "no" or silence.
   Done when: the path was explicit, or the user affirmed the shown list.

3. **Pick chain or batch.** If the invocation's own wording already settled it ("in parallel", "serially", "for each") don't ask again. Otherwise ask directly: chain (dependent, ordered) or batch (independent, parallel)?
   Done when: exactly one mode is chosen, before anything is dispatched.

4. **Size the run.**
    - _Chain_: the task list's order **is** the chain order. Nothing else to decide here.
    - _Batch_: decide how many run concurrently. Default cap is 3 - at or under that, proceed without asking. Above 3, name the default and ask the user to confirm going higher or cap it down.
    - _Batch only_ - check for conflicts: scan the task descriptions for overlapping file/directory mentions. A likely collision is a heads-up and a go/no-go question, never a silent block - the user decides.
      Done when: chain order is set, or batch concurrency is fixed and any flagged conflict has been answered.

5. **Pick agent and model, once, for the whole run.** If the user already named a `subagent_type` or `model` in their request, use it. Otherwise default to the current model and the general-purpose `claude` agent type, and just say so rather than asking a question whose answer is already "use the default." Pick from whatever agent roster is visible in this session right now - don't hardcode a roster here, it'll go stale.
   Done when: one agent type and one model apply to every dispatch in this run.

6. **Dispatch.**
    - _Chain_: dispatch one `Agent` call at a time with `run_in_background: false` - the next step's prompt needs this step's actual result, so blocking is correct here, not a compromise. Feed each step's prompt the prior step's full result as context. Post a status line to the thread the moment each step returns, before starting the next. If a step fails, don't reflexively abort the rest: check each remaining step's description for whether it actually depends on the failed step's output or subject - abort only that dependent tail, keep dispatching the independent remainder.
    - _Batch_: fire all the tasks (up to the concurrency cap from step 4) as `Agent` calls in one message with `run_in_background: true`, `isolation: "worktree"` on any task that writes to the repo (omit it for read-only/research tasks). If the task count exceeds the cap, treat the rest as a queue - when a running slot's completion notification lands, dispatch the next queued task immediately rather than waiting for the whole wave to drain. Batch siblings are independent by construction (step 4's conflict check saw to that), so one task failing never stops the others.
      Done when: every gathered task has been dispatched, is queued, or was explicitly aborted with a stated reason.

7. **Keep a live checklist.** Post one running checklist (☐/☑ per task) and update it in place as each real outcome lands - a completion notification for batch tasks, or the immediate return of a chain step. Never sit on several finished tasks and post them as one delayed batch.
   For a **batch** run specifically, arm a 5-minute repeating heartbeat so a task that's taking a while gets a "still working on: `<task>`" ping instead of silence - see [heartbeat.md](heartbeat.md) for the exact mechanism and why chain mode structurally can't have one.
   Done when: every checklist row is either checked with an outcome, or marked aborted.

8. **Close out.** Once the chain has ended (finished or aborted-tail) or every batch/queued task has resolved: cancel the heartbeat job if step 7 armed one, then post one consolidated summary - each task, done or failed, with a one-line outcome.
   Done when: the heartbeat job (if any) is deleted, and the summary accounts for every task from step 1, aborted ones included.
