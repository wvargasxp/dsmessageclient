#!/bin/sh
xbuild TestsHeadless.csproj
nunit-console4 TestsHeadless.csproj
