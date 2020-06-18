.method private hidebysig static void  Main(string[] args) cil managed
{
.entrypoint
.locals init (
[0] int32 i,
[1] float64 d,
[2] bool b
)

ldc.i4.s 5
stloc.s 0
ldc.r8 123.456
stloc.s 1
ldc.i4.1
stloc.s 2
ldloc.s 0
call	void [mscorlib]System.Console::Write(int32)
ldstr "\n"
call	void [mscorlib]System.Console::Write(string)
call class [mscorlib]System.Globalization.CultureInfo [mscorlib]System.Globalization.CultureInfo::get_InvariantCulture()
ldstr "{ 0:0.000000}"
ldloc.s 1
box [mscorlib]System.Double
call string [mscorlib]System.String::Format(class [mscorlib]System.IFormatProvider, string, object)
call void [mscorlib]System.Console::Write(string)
ldstr "\n"
call	void [mscorlib]System.Console::Write(string)
ldloc.s 2
call	void [mscorlib]System.Console::Write(bool)
ldstr "\n"
call	void [mscorlib]System.Console::Write(string)
ret
}
