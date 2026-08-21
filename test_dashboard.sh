#!/bin/bash
echo "Registering user..."
RESPONSE=$(curl -s -X POST http://localhost:5078/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email": "manager@test.com", "password": "Password123!", "firstName": "John", "lastName": "Doe", "role": "SalesManager"}')

TOKEN=$(echo $RESPONSE | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

echo "Received Token: $TOKEN"

echo "Fetching Dashboard..."
curl -s -i http://localhost:5078/api/crm/dashboard \
  -H "Authorization: Bearer $TOKEN"
