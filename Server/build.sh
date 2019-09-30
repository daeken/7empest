#!/bin/bash

docker build -t daeken/tempest-server:v1 .
docker push daeken/tempest-server:v1
