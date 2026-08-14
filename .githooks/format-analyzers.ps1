$files = $args -join ','
if (-not $files) { exit 0 }

dotnet format analyzers Interlocutor.slnx --verify-no-changes --include $files
exit $LASTEXITCODE
