; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
INTERLOC002 | Interlocutor.Discovery | Warning | Multiple handlers registered for request type
INTERLOC003 | Interlocutor.Discovery | Info | Catch-all notification handler
INTERLOC004 | Interlocutor.Discovery | Error | Notification handler ordering cycle
INTERLOC005 | Interlocutor.Discovery | Error | Notification handler ordering reference is not a handler
INTERLOC006 | Interlocutor.Discovery | Error | Ambiguous open generic handlers for request type
INTERLOC007 | Interlocutor.Discovery | Error | Handler override reference is not a handler
INTERLOC008 | Interlocutor.Discovery | Warning | Unresolved generic request usage
