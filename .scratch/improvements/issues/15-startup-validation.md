## Labels

ready-for-agent

## What to build

Startup validation exists as a free-standing class that nothing calls, so misconfiguration is discovered at first dispatch rather than at startup. It also only inspects the generic query and command contracts, silently skipping void commands, which the requirements explicitly say must be covered.

Wire it into the startup path via the configuration object, and close its coverage gaps. Assembly scanning should also fail gracefully rather than throwing an opaque loader error when an assembly has unloadable references.

## Acceptance criteria

- [ ] Validation runs at startup when enabled through configuration, and the failure names the offending request type
- [ ] Void commands are validated alongside the generic query and command contracts
- [ ] Notifications with no registered handler are reported
- [ ] Duplicate behavior registrations are reported
- [ ] An assembly with unloadable references produces a clear diagnostic rather than a raw loader exception
- [ ] Tests are red before green

## Blocked by

- 13-registration-options
