$s = Get-WmiObject Win32_Process -Filter "Name='i-do-everything.exe'"
if ($s) {
"`nKill previous running IDO Listener application..."
$s.Terminate()
}

