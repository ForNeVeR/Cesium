#!/bin/bash

if [ -z "$SONAR_TOKEN" ]; then
  echo "SONAR_TOKEN is not set"
  exit 1
fi

if [ -z "$GITHUB_TOKEN" ]; then
  echo "$GITHUB_TOKEN is not set"
  exit 1
fi

./.sonar/scanner/dotnet-sonarscanner begin /k:"ForNeVeR_Cesium" /o:"fornever" /d:sonar.login="$SONAR_TOKEN" /d:sonar.host.url="https://sonarcloud.io"
dotnet build
./.sonar/scanner/dotnet-sonarscanner end /d:sonar.login="$SONAR_TOKEN"
