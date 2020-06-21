.assembly extern mscorlib { }
.assembly MINICompiler { }
.method public static void Main()
{
.entrypoint
.locals init (
[0] int32 V_i,
[1] bool V_b
)

ldc.i4 0
stloc.s 0
ldc.i4 0
stloc.s 1
ldc.i4.1
stloc.s 1
ldc.i4 0
stloc.s 0

br WC_4

WB_5: nop
ldloc.s 0
not
ldc.i4 10
clt
brfalse ELSE_2

br WC_0

WB_1: nop
ldc.i4.0
stloc.s 1
ldloc.s 0
ldc.i4 1
add
stloc.s 0

WC_0: nop
ldloc.s 1
brtrue WB_1
br AFIF_3
ELSE_2: nop
ldc.i4.0
stloc.s 1
ldloc.s 1
brfalse ELSE_0
br AFIF_1
ELSE_0: nop
ldc.i4.1
stloc.s 1
AFIF_1: nop
AFIF_3: nop

br WC_2

WB_3: nop
ldc.i4.1
stloc.s 1
ldloc.s 0
ldc.i4 1
add
stloc.s 0

WC_2: nop
ldloc.s 1
ldc.i4.0
ceq
brtrue WB_3

WC_4: nop
ldloc.s 1
brfalse S_0
ldloc.s 0
not
ldc.i4 10
clt
brfalse S_0
ldc.i4.1
br O_1
S_0: ldc.i4.0
br O_1
O_1: nop
brtrue WB_5
ldloc.s 0
call void [mscorlib]System.Console::Write(int32)
ret
}
