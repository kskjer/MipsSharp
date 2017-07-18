AS  = mips-elf-as
CC  = mips-elf-gcc
CPP = mips-elf-cpp
LD  = mips-elf-ld
OBJCOPY = mips-elf-objcopy

MIPSSHARP = dotnet exec $(MIPSSHARP_PATH)/MipsSharp.dll

DIST_DIR = $(MIPSSHARP_PATH)/dist

CPPFLAGS = -I$(DIST_DIR)
CFLAGS   = -march=vr4300 -mabi=32 -G 0
LDFLAGS  = -L$(DIST_DIR)

-include $(DIST_DIR)/z64-ovl.local.mk

default: $(OVL_NAME).ovl

%.o: %.S
	$(CPP) $(CPPFLAGS) $^ | $(AS) $(CFLAGS) -o $@

%.elf: %.o
	echo | $(CC) -x c -c $(CFLAGS) -o relocations.o -
	# First we have to link without the OVL relocations, so that MipsSharp can extract is
	$(LD) $(LDFLAGS) -T z64-ovl.ld --emit-relocs -o $@.tmp $^ relocations.o
	# This is the actual link
	$(MIPSSHARP) --zelda64 -O $@.tmp | $(CC) -x c -c $(CFLAGS) -o relocations.o -
	$(LD) $(LDFLAGS) -T z64-ovl.ld --emit-relocs -o $@ $^ relocations.o 

%.ovl: %.elf
	$(OBJCOPY) -O binary $^ $@

disasm-new.asm: $(OVL_NAME).ovl
	$(MIPSSHARP) --zelda64 -D $(OVL_NAME).ovl $(OVL_ADDR) > $@

diff: disasm-new.asm $(OVL_NAME).S
	vimdiff $^

lite-diff: disasm-new.asm $(OVL_NAME).S
	-diff $^; true

bin-diff: $(OVL_NAME).ovl
	xxd $(OVL_NAME).ovl > hex-new.hex
	xxd $(OVL_NAME).ovl.orig > hex-orig.hex
	vimdiff hex-new.hex hex-orig.hex

disasm-without-ep.asm: 
	$(MIPSSHARP) --zelda64 -D $(OVL_NAME).ovl > $@

test-ep-infer: disasm-without-ep.asm
	diff disasm-without-ep.asm $(OVL_NAME).S

# Don't delete intermediate files
.PRECIOUS: %.o %.elf
	

clean:
	rm -fv *.o *.elf *.ovl *.asm *.hex *.tmp
