type: internal

Regenerated .github/requirements/semgrep.txt so its pins match semgrep-overrides.txt: opentelemetry-api and the other OpenTelemetry packages move to 1.45.2 (GHSA-mf5v-w3w3-f6cg). The lock was behind the overrides file after Dependabot bumped the overrides without regenerating it, so mcp also moves 1.30.0 to 2.3.0 as the overrides already require. CI tooling only; no library change.
