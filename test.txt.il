.assembly extern mscorlib { }
.assembly MINICompiler { }
.method public static void Main()
{
.entrypoint
.locals init (
[0] int32 a
)

call string [mscorlib]System.Console::ReadLine()
call int32 [mscorlib]System.Int32::Parse(string)
stloc.0

br WC_0

WB_1: nop
ldloc.s 0
ldc.i4.s 1
sub
stloc.s 0
ldloc.s 0
call	void [mscorlib]System.Console::Write(int32)
ldstr "\n"
call	void [mscorlib]System.Console::Write(string)

WC_0: nop
ldloc.s 0
ldc.i4.s 5
cgt
brtrue WB_1
ret
}
