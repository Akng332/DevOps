#!/usr/bin/env python3
"""Check the lab's container configuration before changing the environment."""

import json
import re
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parent.parent
config = json.loads(subprocess.check_output(
    ["docker", "compose", "config", "--format", "json"], cwd=root, text=True
))
services = config["services"]

for name, service in services.items():
    image = service.get("image", "")
    assert image and not image.endswith(":latest") and ":" in image, (name, image)
    assert service.get("healthcheck", {}).get("disable") is not True, name
    assert service.get("mem_limit") and service.get("cpus"), name
    assert not service.get("privileged", False), name
    assert not service.get("network_mode") == "host", name
    if name != "app":
        assert not service.get("ports"), name

app = services["app"]
assert app.get("user") == "1000:1000"
assert app.get("read_only") is True
assert app.get("healthcheck", {}).get("test")
assert len(app.get("ports", [])) == 1
assert app["ports"][0]["target"] == 8080
assert {item["source"] for item in app.get("secrets", [])} == {
    "app_db_password", "jwt_key"
}
assert {"backend", "frontend"} == set(app["networks"])
assert set(services["db"]["networks"]) == {"backend"}
assert services["db"].get("healthcheck", {}).get("test")
assert config["networks"]["backend"].get("internal") is True
assert any(v["type"] == "volume" for v in services["db"]["volumes"])
assert all("PASSWORD=" not in str(s) for s in services.values())

dockerfile = (root / "Dockerfile").read_text()
assert re.search(r"(?m)^USER\s+1000:1000\s*$", dockerfile)
assert re.search(r"(?m)^HEALTHCHECK\s", dockerfile)
assert not re.search(r"(?mi)^\s*(ENV|ARG)\s+.*(PASSWORD|JWT__KEY)\s*=", dockerfile)
assert not re.search(r"(?mi)^FROM\s+\S+:latest\b", dockerfile)
assert (root / ".dockerignore").exists()
print("Container configuration passed static checks")
