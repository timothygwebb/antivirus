"""
agents package — AI agent modules for the antivirus tool.

Each agent wraps a specific antivirus capability and exposes a simple
`run()` method returning a plain dict, suitable for use as an AI tool function.

Modules:
    scan_agent    — virus scanning agent
    update_agent  — virus definition update agent
    repair_agent  — browser repair / detection agent
"""
