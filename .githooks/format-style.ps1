$files = $args -join ','
if (-not $files) { exit 0 }

dotnet format style Mediance.slnx --include $files
exit $LASTEXITCODE
