type: internal

Regenerated .github/requirements/semgrep.txt so the OpenTelemetry packages move to 1.45.2 (GHSA-mf5v-w3w3-f6cg), and restored the mcp==1.30.0 override: Dependabot had bumped it to 2.3.0 in semgrep-overrides.txt, but mcp 2.x removed mcp.server.fastmcp, which semgrep imports, so semgrep crashes under it. CI tooling only; no library change.
