$files = $args -join ','
if (-not $files) { exit 0 }

dotnet format style Interlocutor.slnx --include $files
exit $LASTEXITCODE
