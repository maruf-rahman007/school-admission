#!/bin/sh
# Bonus check: two backend processes on one database never overbook (README "Bonus: two backends").
# Creates a 5-seat class, fires 50 parallel applications (25 to each backend) and expects exactly 5 holds.
# POSIX sh only (the curl image has busybox sh and no jq). Exit 0 = PASS, 1 = FAIL.
# Run: docker compose --profile multi run --rm multi-check
set -eu

A="${BACKEND_A:-http://backend:8080}"
B="${BACKEND_B:-http://backend2:8080}"
SEATS=5
REQUESTS=50
TIMEOUT=20

fail() {
  echo "FAIL: $*"
  exit 1
}

work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

# 1. A fresh class (names need not be unique; the timestamp makes it easy to spot in the UI).
stamp=$(date +%s)
code=$(curl -s -o "$work/class.json" -w '%{http_code}' --max-time "$TIMEOUT" \
  -H 'Content-Type: application/json' \
  -d "{\"name\":\"multi-check $stamp\",\"totalSeats\":$SEATS}" \
  "$A/classes") || fail "could not reach $A"
[ "$code" = "201" ] || fail "POST /classes returned $code"
class_id=$(sed -n 's/.*"id":"\([^"]*\)".*/\1/p' "$work/class.json")
[ -n "$class_id" ] || fail "no class id in: $(cat "$work/class.json")"
echo "class: $class_id ($SEATS seats), $REQUESTS parallel applications split over $A and $B"

# 2. Unique 17-digit student IDs per run: '900' + epoch seconds (10) + 2 random digits + 2-digit index.
rnd=$(( $(od -An -N1 -tu1 /dev/urandom | tr -d ' ') % 100 ))
run_id=$(printf '900%s%02d' "$stamp" "$rnd")

i=0
while [ "$i" -lt "$REQUESTS" ]; do
  if [ $((i % 2)) -eq 0 ]; then url="$A"; tag=a; else url="$B"; tag=b; fi
  student=$(printf '%s%02d' "$run_id" "$i")
  (
    c=$(curl -s -o "$work/$i.body" -w '%{http_code}' --max-time "$TIMEOUT" -X POST \
      -H "X-Student-Id: $student" "$url/classes/$class_id/holds") || c=000
    echo "$tag $c" > "$work/$i.code"
  ) &
  i=$((i + 1))
done
wait

# 3. Count the answers per backend.
count() { cat "$work"/*.code | grep -c "^$1 $2\$" || true; }
a201=$(count a 201); a409=$(count a 409)
b201=$(count b 201); b409=$(count b 409)
total201=$((a201 + b201)); total409=$((a409 + b409))
other=$((REQUESTS - total201 - total409))
echo "backend  (a): 201=$a201 409=$a409"
echo "backend2 (b): 201=$b201 409=$b409"
echo "total:        201=$total201 409=$total409 other=$other"

[ "$other" -eq 0 ] || fail "$other responses were neither 201 nor 409: $(cat "$work"/*.code | grep -v ' 20[1]$' | grep -v ' 409$' | tr '\n' ' ')"
[ "$total201" -eq "$SEATS" ] || fail "expected $SEATS holds, got $total201"
[ "$total409" -eq $((REQUESTS - SEATS)) ] || fail "expected $((REQUESTS - SEATS)) rejections, got $total409"
for f in "$work"/*.code; do
  n=$(basename "$f" .code)
  if grep -q ' 409$' "$f" && ! grep -q 'NO_SEATS_AVAILABLE' "$work/$n.body"; then
    fail "request $n got 409 without NO_SEATS_AVAILABLE: $(cat "$work/$n.body")"
  fi
done

# 4. Both backends must report the same, correct seat counts.
for url in "$A" "$B"; do
  code=$(curl -s -o "$work/counts.json" -w '%{http_code}' --max-time "$TIMEOUT" "$url/classes/$class_id") \
    || fail "could not reach $url"
  [ "$code" = "200" ] || fail "GET /classes/{id} on $url returned $code"
  held=$(sed -n 's/.*"heldSeats":\([0-9-]*\).*/\1/p' "$work/counts.json")
  available=$(sed -n 's/.*"availableSeats":\([0-9-]*\).*/\1/p' "$work/counts.json")
  echo "$url reports heldSeats=$held availableSeats=$available"
  [ "$held" = "$SEATS" ] || fail "$url: heldSeats=$held, expected $SEATS"
  [ "$available" = "0" ] || fail "$url: availableSeats=$available, expected 0"
done

echo "PASS: no overbooking with two backends ($SEATS of $REQUESTS applications held)"
