#!/bin/sh
xbuild /target:Clean ../EMXamarin/EMXamarin.csproj
xbuild /target:Clean TestsHeadless.csproj
xbuild TestsHeadless.csproj
nunit-console4 -run:TestsHeadless.AdditionTest TestsHeadless.csproj
