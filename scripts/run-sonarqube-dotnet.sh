#!/bin/bash

if [ -z "$SONAR_TOKEN" ]; then
  echo "SONAR_TOKEN is not set"
  exit 1
fi

./.sonar/scanner/dotnet-sonarscanner begin /k:"$PROJECT_KEY" /o:"$ORGANIZATION" /d:sonar.login="$SONAR_TOKEN" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.c.file.suffixes=-
dotnet build
./.sonar/scanner/dotnet-sonarscanner end /d:sonar.login="$SONAR_TOKEN"
