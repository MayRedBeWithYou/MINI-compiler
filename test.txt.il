.assembly extern mscorlib { }
.assembly MINICompiler { }
.method public static void Main()
{
.entrypoint
.locals init (
[0] int32 V_a,
[1] int32 V_b,
[2] int32 V_c
)

ldc.i4 0
stloc.s 0
ldc.i4 0
stloc.s 1
ldc.i4 0
stloc.s 2
ldc.i4 3
stloc.s 1
ldloc.s 1
ldc.i4 4
stloc.s 2
ldloc.s 2
add
stloc.s 0
ldloc.s 0
call void [mscorlib]System.Console::Write(int32)
ldstr "\n"
call void [mscorlib]System.Console::Write(string)
ldloc.s 1
call void [mscorlib]System.Console::Write(int32)
ldstr "\n"
call void [mscorlib]System.Console::Write(string)
ldloc.s 2
call void [mscorlib]System.Console::Write(int32)
ldstr "\n"
call void [mscorlib]System.Console::Write(string)
ret
}
