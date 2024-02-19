#pragma once

__cli_import("Cesium.Runtime.CTypeFunctions::IsAlnum")
int isalnum(int ch);

__cli_import("Cesium.Runtime.CTypeFunctions::IsAlpha")
int isalpha(int ch);

__cli_import("Cesium.Runtime.CTypeFunctions::IsLower")
int islower(int ch);

__cli_import("Cesium.Runtime.CTypeFunctions::IsUpper")
int isupper(int ch);

__cli_import("Cesium.Runtime.CTypeFunctions::IsDigit")
int isdigit(int ch);

__cli_import("Cesium.Runtime.CTypeFunctions::IsXDigit")
int isxdigit(int ch);

__cli_import("Cesium.Runtime.CTypeFunctions::IsCntrl")
int iscntrl(int ch);

__cli_import("Cesium.Runtime.CTypeFunctions::IsGraph")
int isgraph(int ch);

__cli_import("Cesium.Runtime.CTypeFunctions::IsSpace")
int isspace(int ch);

__cli_import("Cesium.Runtime.CTypeFunctions::IsBlank")
int isblank(int ch);

__cli_import("Cesium.Runtime.CTypeFunctions::IsPrint")
int isprint(int ch);

__cli_import("Cesium.Runtime.CTypeFunctions::IsPunct")
int ispunct(int ch);

__cli_import("Cesium.Runtime.CTypeFunctions::ToUpper")
int toupper(int ch);

__cli_import("Cesium.Runtime.CTypeFunctions::ToLower")
int tolower(int ch);
