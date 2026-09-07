# Batch heartbeat

A batch's tasks run detached (`run_in_background: true`), so between dispatch and the next real completion notification the session sits idle - and idle is exactly when a scheduled prompt can land. That's the mechanism:

At the moment of a batch dispatch, call:

```
CronCreate({
  cron: "*/5 * * * *",
  recurring: true,
  prompt: "dispatch-tasks heartbeat: check the running checklist for this batch; for every task still unchecked, post 'still working on: <task>' to the thread."
})
```

Keep the returned job id with the rest of the run's state. Each time it fires (or each time a genuine completion notification re-invokes the session, whichever comes first), re-check the checklist and ping whatever's still open - `recurring: true` already covers "repeating," so there's no extra bookkeeping to do between ticks.

The instant the last task resolves, call `CronDelete` on that job id as part of step 8's close-out - even if the cron never fired once. An armed job left behind is silent waste for the rest of the session (it self-expires after 7 days regardless, but there's no reason to let it ride that long).

## Why chain mode gets no heartbeat

A chain step is dispatched with `run_in_background: false` - the tool call blocks until that one sub-agent returns. `CronCreate` jobs "only fire while the REPL is idle (not mid-query)," and a blocking call is, definitionally, mid-query. There is no channel to interleave a ping into a wait that hasn't returned control yet. This isn't a missing feature to build around - it's a direct consequence of chain mode needing each step's real output before it can build the next one (see `SKILL.md` step 6). The nearest available signal is the step-boundary status line that step 7 already posts; a slow chain step is silent until it finishes, then reports.

## Why not `ScheduleWakeup` or `Monitor`

- `ScheduleWakeup` is scoped to `/loop`'s dynamic-pacing mode - it re-invokes `/loop` itself with a `/loop`-shaped prompt or sentinel. Borrowing it here would depend on re-invocation semantics that belong to a different skill and could change out from under this one.
- `Monitor` streams stdout lines from a shell command or a WebSocket. A dispatched `Agent` task isn't a process, a log file, or a socket - there's no shell-visible signal for "this background agent is still running" to attach a filter to.

`CronCreate` is the one primitive here with no coupling to another skill and a documented idle-fire contract that matches what a batch actually needs.
