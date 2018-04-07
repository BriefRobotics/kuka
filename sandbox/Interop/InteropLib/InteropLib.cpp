// This is the main DLL file.

#include "stdafx.h"
#include <msclr\auto_gcroot.h>

_declspec(dllexport) char* Hello()
{
	return "Hello from Interop Land!";
}
