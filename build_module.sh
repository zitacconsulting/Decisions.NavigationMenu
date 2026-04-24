#!/bin/bash
set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${GREEN}Building Decisions.NavigationMenu Module${NC}"

# ── Version bump ─────────────────────────────────────────────────────────────
CURRENT_VERSION=$(grep 'Current = "' src/ModuleVersion.cs | sed 's/.*Current = "\(.*\)".*/\1/')
echo -e "${CYAN}Current version: ${CURRENT_VERSION}${NC}"
read -p "New version (leave blank to keep ${CURRENT_VERSION}): " NEW_VERSION

if [ -n "$NEW_VERSION" ]; then
    sed -i "s/Current = \".*\"/Current = \"${NEW_VERSION}\"/" src/ModuleVersion.cs
    sed -i "s/\"Version\": \".*\"/\"Version\": \"${NEW_VERSION}\"/" Module.Build.json
    echo -e "${GREEN}Version bumped to ${NEW_VERSION}${NC}"
fi

echo -e "${YELLOW}Compiling main project...${NC}"
dotnet publish ./src/Decisions.NavigationMenu.csproj --self-contained false --output ./obj-main -c Release

echo -e "${YELLOW}Compiling Razor views project...${NC}"
dotnet restore ./src/Decisions.NavigationMenu.Views.csproj
dotnet publish ./src/Decisions.NavigationMenu.Views.csproj --self-contained false --output ./obj-views -c Release --no-restore

echo -e "${YELLOW}Creating Decisions module package...${NC}"
dotnet msbuild build.proj -t:build_module

echo -e "${GREEN}Module built successfully!${NC}"
echo -e "${CYAN}Output: Decisions.NavigationMenu.zip${NC}"
