#!/bin/sh

set -ev
dotnet build
cp ModSource/bin/net48/DowsedInSalt.dll DowsedInSalt/
