INCLUDE conf.ld


OUTPUT_ARCH( mips )
OUTPUT_FORMAT( "elf32-bigmips", "elf32-bigmips", "elf32-littlemips" )

ENTRY( ENTRY_POINT )


SECTIONS
{
	. = ADDRESS_START;

	.text ALIGN( 4 ):
	{
		FILL( 0 );

		__text_start = . ;

		*(.init);
		*(.text);
		*(.ctors);
        	*(.dtors);
        	*(.fini);
		
		. = ALIGN( 16 );
		__text_end = . ;
		__text_size = __text_end - __text_start;
	}

        .data ALIGN( 16 ): 
	{ 
		FILL( 0 );
		
		__data_start = . ;
		
		*(.data);
		*(.data.*);

		. = ALIGN( 8 );
		_gp = . ;
		*(.sdata);
		
		. = ALIGN( 16 );
		__data_end = . ;
		__data_size = __data_end - __data_start;
	} 
	
	.rodata ALIGN( 16 ):
	{
		FILL( 0 );
		
		__rodata_start = . ;
		
		/* We manually include this later */
		*(EXCLUDE_FILE (*relocations.o) .rodata .rodata.*)
		*(.eh_frame);

		__rodata_end = .;
		__rodata_size = __rodata_end - __rodata_start;
		
		__header_start = .;
		
		/* Zelda 64 overlay header constructed here */
		LONG(__text_end - __text_start);
		LONG(__data_end - __data_start);
		LONG(__rodata_end - __rodata_start);
		LONG(__bss_end - __bss_start);
		LONG((__relocs_end - __relocs_start) / 4);

		__relocs_start = .;
			
		KEEP( relocations.o(.rodata) );

		__relocs_end = .;
		
		
		/* Address to end of rodata section from the end of the file 
		   This is what points to the overlay header. */
		. = (. & 15) ? ALIGN(16) - 4 : . + 16 - 4;
		LONG(__overlay_end - __header_start);	
		__overlay_end = .;	
		
	}

	
        .bss ALIGN( 16 ) : 
	{
		__bss_start = . ;
			
		*(.scommon);
		*(.sbss);
		*(.bss);
		
		. = ALIGN( 16 );
		__bss_end = . ;
		__bss_size = __bss_end - __bss_start;
	}

	"/DISCARD/":
	{
		*(.MIPS.abiflags);
	}
	
	. = ALIGN( 8 );
	end = . ;
}


