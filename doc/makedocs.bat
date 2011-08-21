@echo off
call mdoc update -o xml -L "c:\Program Files\Unity\Editor\Data\Managed" ..\Library\ScriptAssemblies\Assembly-CSharp.dll
docfixinator.py xml
echo "Make changes to XML files in [out] and press any key when done"
pause
call mdoc export-html --template doctemplate.xsl -o html out

