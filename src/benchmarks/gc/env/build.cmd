@echo off
if exist out\ (
    rd /s /q out
)
mkdir out
cd out
cmake ..
devenv /build Release env.sln
cd ..
