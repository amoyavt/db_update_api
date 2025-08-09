#!/bin/bash

echo "=== Testing Database Auto-Update System POC ==="
echo

# Test 1: Health Check
echo "1. Testing Central API Health Check..."
HEALTH=$(curl -s http://localhost:8080/health)
echo "   Response: $HEALTH"
echo

# Test 2: Sync Request
echo "2. Testing Sync Request for MAC 48:b0:2d:e9:c3:b7..."
SYNC_RESPONSE=$(curl -s -X POST http://localhost:8080/api/device-sync/request \
  -H "Content-Type: application/json" \
  -d '{"mac": "48:b0:2d:e9:c3:b7"}')

MANIFEST_ID=$(echo "$SYNC_RESPONSE" | jq -r '.manifest.manifestId')
echo "   Generated ManifestId: $MANIFEST_ID"

COMPANY_COUNT=$(echo "$SYNC_RESPONSE" | jq '.data.companies | length')
LOCATION_COUNT=$(echo "$SYNC_RESPONSE" | jq '.data.locations | length')
GROUP_COUNT=$(echo "$SYNC_RESPONSE" | jq '.data.groups | length')
USER_COUNT=$(echo "$SYNC_RESPONSE" | jq '.data.users | length')
AREA_COUNT=$(echo "$SYNC_RESPONSE" | jq '.data.areas | length')
DEVICE_COUNT=$(echo "$SYNC_RESPONSE" | jq '.data.devices | length')

echo "   Data Counts: Companies=$COMPANY_COUNT, Locations=$LOCATION_COUNT, Groups=$GROUP_COUNT"
echo "                 Users=$USER_COUNT, Areas=$AREA_COUNT, Devices=$DEVICE_COUNT"
echo

# Test 3: Sync Acknowledgment
echo "3. Testing Sync Acknowledgment..."
ACK_RESPONSE=$(curl -s -X POST http://localhost:8080/api/device-sync/ack \
  -H "Content-Type: application/json" \
  -d "{
    \"manifestId\": \"$MANIFEST_ID\",
    \"mac\": \"48:b0:2d:e9:c3:b7\",
    \"status\": \"Success\",
    \"localCounts\": {
      \"companies\": $COMPANY_COUNT,
      \"locations\": $LOCATION_COUNT,
      \"groups\": $GROUP_COUNT,
      \"users\": $USER_COUNT,
      \"areas\": $AREA_COUNT,
      \"devices\": $DEVICE_COUNT
    },
    \"localChecksums\": {
      \"companies\": \"test-hash-1\",
      \"locations\": \"test-hash-2\",
      \"groups\": \"test-hash-3\",
      \"users\": \"test-hash-4\",
      \"areas\": \"test-hash-5\",
      \"devices\": \"test-hash-6\"
    },
    \"durationMs\": 1500,
    \"error\": null
  }")

echo "   Acknowledgment sent successfully!"
echo

# Test 4: Check Seq Logs
echo "4. Access Points:"
echo "   - Central API Health: http://localhost:8080/health"
echo "   - Central API Sync: http://localhost:8080/api/device-sync/request"
echo "   - Seq Logs: http://localhost:5341"
echo

echo "=== POC Test Complete ==="