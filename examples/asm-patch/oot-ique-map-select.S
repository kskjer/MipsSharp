#
# Patch for Ocarina of Time on the iQue to enable map select
# ----------------------------------------------------------
# Input ROM MD5:  a475e9f8615513666a265c464708ae8f
# Input ROM CRCs: 0x3D81FB3E 0xBD843E34
#
# Assembling using MipsSharp:
#   MipsSharp               \
#       --asm-patch         \
#       -i 002022f5.z64     \
#       -o dest.z64         \
#       -p patch.bsdiff     \
#       -s map-patch.S
#

#include <mips.h>
        .set    noreorder
        .set    noat


        .ram_origin     0x800027BC
        .rom_origin     0x000033BC

        jal             hook


        .ram_origin     0x800064AC
        .rom_origin     0x000070AC

        .global         hook

hook:
        lui             v0,0x800f
        ori             v0,v0,0x8324
        addiu           v1,v0,28
$L0:        
        lw              a1,0(v0)
        addiu           v0,v0,4
        bne             v0,v1,$L0
        sw              a1,188(v0)

        j               0x0026330 
       