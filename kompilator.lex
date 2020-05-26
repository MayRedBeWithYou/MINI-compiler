
%namespace GardensPoint
%{
%}

number [0-9]+

%%

{number}  { yylval = int.Parse(yytext); return (int)Tokens.NUMBER; }
"+"       { return (int)Tokens.PLUS; }
"-"       { return (int)Tokens.MINUS; }
"*"       { return (int)Tokens.MULTIPLIES; }
"/"       { return (int)Tokens.DIVIDES; }
"("       { return (int)Tokens.OPEN_PAR; }
")"       { return (int)Tokens.CLOSE_PAR; }
"\n"      { return (int)Tokens.ENDL; }
<<EOF>>   { return (int)Tokens.ENDL; }
" "       { }
"\t"      { }
.         { return (int)Tokens.ERROR; }

%%