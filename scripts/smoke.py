"""LEGACY CONTRACT: incompatible with the current cookie/CSRF API.

Do not use this as the active smoke check. See docs/runbooks/TESTING.md
and BE-07 for porting/retirement; current checks are SQL tests and Playwright.
Original usage: python scripts/smoke.py [http://localhost:8080].

Creates uniquely named practice accounts and tests actual SQL Server concurrency.
Uses only Python's standard library. Does not delete or modify existing accounts.
"""
import concurrent.futures
import json
import sys
import urllib.error
import urllib.request
import uuid

BASE = (sys.argv[1] if len(sys.argv) > 1 else "http://localhost:8080").rstrip("/")


def request(path, data=None, token=None, expected=200):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    payload = None if data is None else json.dumps(data).encode()
    req = urllib.request.Request(BASE + path, data=payload, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=60) as response:
            status, body = response.status, response.read()
    except urllib.error.HTTPError as error:
        status, body = error.code, error.read()
    assert status == expected, (path, status, body.decode())
    return json.loads(body) if body else None


def register(name):
    return request("/api/auth/register", {
        "email": f"smoke-{uuid.uuid4().hex}@example.com",
        "password": "PracticeSmoke123!", "nickname": name,
    })


request("/health")
request("/api/profiles/me", expected=401)
for attempt in range(3):
    first, second = register("Smoke Alice"), register("Smoke Bob")
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        futures = [pool.submit(request, "/api/matches/swipes",
            {"targetId": target["userId"], "direction": "like"}, actor["token"])
            for actor, target in [(first, second), (second, first)]]
        results = [future.result() for future in futures]
    assert any(result["matched"] for result in results), "Concurrent reciprocal likes lost their match"
    matches = request("/api/matches", token=first["token"])
    assert len(matches) == 1, "Expected exactly one match"
    match_id = matches[0]["id"]
    for _ in range(3):
        request("/api/matches/swipes", {"targetId": second["userId"], "direction": "like"}, first["token"])
    assert len(request("/api/matches", token=first["token"])) == 1
    request(f"/api/matches/{match_id}/messages", {"content": "Concurrency test passed"}, first["token"])
    messages = request(f"/api/matches/{match_id}/messages", token=second["token"])
    assert messages[-1]["content"] == "Concurrency test passed"
    print(f"PASS: reciprocal like race {attempt + 1}, duplicate protection, and persisted chat")
print("Stack smoke checks passed. Six uniquely named practice accounts were created.")
