.assembly extern mscorlib { }
.assembly MINICompiler { }
.method public static void Main()
{
.entrypoint
.locals init (
[0] float64 V_a,
[1] int32 V_b
)

ldc.r8 0
stloc.s 0
ldc.i4 0
stloc.s 1
ldc.i4 5
stloc.s 1
ldc.i4 2
ldc.i4 2
add
stloc.s 1
ldloc.s 1
conv.r8
stloc.s 0
call class [mscorlib]System.Globalization.CultureInfo [mscorlib]System.Globalization.CultureInfo::get_InvariantCulture()
ldstr "{0:0.000000}"
ldloc.s 0
box [mscorlib]System.Double
call string [mscorlib]System.String::Format(class [mscorlib]System.IFormatProvider, string, object)
call void [mscorlib]System.Console::Write(string)
ldstr "\n"
call void [mscorlib]System.Console::Write(string)
ldloc.s 1
call void [mscorlib]System.Console::Write(int32)
ldstr "\n"
call void [mscorlib]System.Console::Write(string)
ldc.i4 2
ldc.i4 2
ldc.i4 5
mul
add
ldc.i4 3
add
call void [mscorlib]System.Console::Write(int32)
ldstr "\n"
call void [mscorlib]System.Console::Write(string)
ldc.i4 12
ldc.i4 3
div
ldc.i4 2
div
ldc.i4 2
div
call void [mscorlib]System.Console::Write(int32)
ldstr "\n"
call void [mscorlib]System.Console::Write(string)
ldc.i4 12
ldc.i4 3
div
ldc.i4 2
ldc.i4 2
div
div
call void [mscorlib]System.Console::Write(int32)
ldstr "\n"
call void [mscorlib]System.Console::Write(string)
ret
}
