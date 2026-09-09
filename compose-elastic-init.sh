#!/bin/sh

set -e
echo "\nConfiguring Elasticsearch..."

curl -fsS \
  -u "elastic:${ELASTIC_PASSWORD}" \
  -X PUT \
  "http://elasticsearch:9200/_security/user/kibana_system/_password" \
  -H "Content-Type: application/json" \
  -d "{\"password\":\"${KIBANA_SYSTEM_PASSWORD}\"}"

curl -fsS \
  -u "elastic:${ELASTIC_PASSWORD}" \
  -X PUT \
  "http://elasticsearch:9200/_security/role/httploggingsink" \
  -H "Content-Type: application/json" \
  -d '{
    "cluster": ["manage"],
    "indices": [
      {
        "names": ["logs-http-log-*"],
        "privileges": ["create_index", "create", "index", "write"]
      }
    ]
  }'

curl -fsS \
-u "elastic:${ELASTIC_PASSWORD}" \
-X PUT \
"http://elasticsearch:9200/_security/user/httploggingsink" \
-H "Content-Type: application/json" \
-d "{\"password\":\"${HTTPLOGGINGSINK_PASSWORD}\",\"roles\":[\"httploggingsink\"]}"

echo "\nElasticsearch configuration complete."
