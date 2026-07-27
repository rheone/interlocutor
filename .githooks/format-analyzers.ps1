$files = $args -join ','
if (-not $files) { exit 0 }

dotnet format analyzers Mediance.slnx --verify-no-changes --include $files
exit $LASTEXITCODE
