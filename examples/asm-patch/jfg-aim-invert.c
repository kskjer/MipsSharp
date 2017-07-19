asm("#include <mips.h>            \n\t"
    "                             \n\t"
    ".ram_origin     0x800431B0   \n\t"
    ".rom_origin     0x00043db0   \n\t"
    "                             \n\t"
    ".set            noreorder    \n\t"
    ".set            noat         \n\t"
    "                             \n\t"
    "lui             a0,0x8010    \n\t"
    "jal             interceptor  \n\t"
    "addiu           a0,a0,-20288 \n\t");

typedef struct
{
        unsigned short button;
        signed char    stick_x;
        signed char    stick_y;
}
OSContPad;


asm(".ram_origin     0x800AC440                   \n\t"
    ".rom_origin     0x000ad040                   \n\t"
    "                                             \n\t"
    ".equ            osContGetReadData,0x80097DD4 \n\t");

extern void osContGetReadData( OSContPad *);

void
interceptor ( OSContPad *pad )
{
        osContGetReadData(pad);

        if (pad->button & 0x10)
                pad->stick_y = -pad->stick_y;
}
