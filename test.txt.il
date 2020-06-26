.assembly extern mscorlib { }
.assembly MINICompiler { }
.method public static void Main()
{
.entrypoint
.locals init (
[0] float64 V_a,
[1] float64 V_b
)

ldc.r8 0
stloc.s 0
ldc.r8 0
stloc.s 1
ldc.i4 2
ldc.i4 2
mul
ldc.i4 2
add
conv.r8
stloc.s 1
ldloc.s 1
stloc.s 0
call class [mscorlib]System.Globalization.CultureInfo [mscorlib]System.Globalization.CultureInfo::get_InvariantCulture()
ldstr "{0:0.000000}"
ldloc.s 0
box [mscorlib]System.Double
call string [mscorlib]System.String::Format(class [mscorlib]System.IFormatProvider, string, object)
call void [mscorlib]System.Console::Write(string)
ret
}
