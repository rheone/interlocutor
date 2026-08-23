# Sample App Walkthrough

> [!NOTE]
> **TBD**: This section provides a guided tour of the runnable sample application under [`samples/`](../samples/) - `Interlocutor.Sample.Handlers` and `Interlocutor.Sample.WebApi`. The sample itself exists and is verified working end to end; this walkthrough doc hasn't been written yet. Write it from the actual sample code, not from a hypothetical one.

## Expected content

- Overview of sample architecture
- Project structure (domain, application, API layers)
- Query handler example (read request, no transaction)
- Command handler example (write request, transaction)
- Generic handler example (serves multiple request types)
- Cross-assembly handler discovery
- Explicitly composed pipeline (non-default behaviors)
- Pipeline stage with short-circuit
- Multi-subscriber notification
- Transport conversion at the edge (ASP.NET Core integration)
- Running and testing the sample

## Links

- [builder-and-registration.md](builder-and-registration.md) - assembly scanning setup
- [pipeline-and-behaviors.md](pipeline-and-behaviors.md) - explicit behavior composition
- Ticket 18 (Sample application) - implementation in progress
