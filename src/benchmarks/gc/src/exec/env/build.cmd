@echo off
if exist out\ (
    rd /s /q out
)
mkdir out
cd out
cmake ..
devenv /build Debug env.sln
cd ..
