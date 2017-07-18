#!/bin/bash 
#
# Usage: ./dist/verify-rom.sh "rom.z64" "output-directory"
#
# This will disassemble all of the overlays in the ROM, build them, and run the
# lite-diff target on all of them checking for differences.
#
# Uses GNU parallel to speed up building.
#
MIPSSHARP_ROOT="`readlink -f "$(dirname $0)/.."`"
ROM_PATH="$1"
TARGET_FOLDER="$2"

pushd .

rm -fr "$TARGET_FOLDER"
mkdir -p "$TARGET_FOLDER"
cd "$TARGET_FOLDER"

dotnet exec "$MIPSSHARP_ROOT"/MipsSharp/bin/Release/netcoreapp1.1/MipsSharp.dll --zelda64 -A "$ROM_PATH"

ls -1 | parallel make -C '{}' lite-diff

for i in * 
do 
	if [[ "$(make -sC "$i" lite-diff | wc -l)" -ne "0" ]]
	then 
		echo " - $i"
	fi
done

popd 
