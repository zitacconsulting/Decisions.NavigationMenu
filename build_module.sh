#!/bin/bash
set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${GREEN}Building Decisions.NavigationMenu Module${NC}"

echo -e "${YELLOW}Compiling main project...${NC}"
dotnet publish ./Decisions.NavigationMenu.csproj --self-contained false --output ./obj-main -c Release

echo -e "${YELLOW}Compiling Razor views project...${NC}"
dotnet restore ./Decisions.NavigationMenu.Views.csproj
dotnet publish ./Decisions.NavigationMenu.Views.csproj --self-contained false --output ./obj-views -c Release --no-restore

echo -e "${YELLOW}Creating Decisions module package...${NC}"
dotnet msbuild build.proj -t:build_module

echo -e "${GREEN}Module built successfully!${NC}"
echo -e "${CYAN}Output: Decisions.NavigationMenu.zip${NC}"
