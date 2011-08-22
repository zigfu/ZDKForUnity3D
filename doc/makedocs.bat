@echo off
call mdoc update -o xml -L "c:\Program Files\Unity\Editor\Data\Managed" ..\Library\ScriptAssemblies\Assembly-CSharp.dll
docfixinator.py xml
call mdoc export-html --template doctemplate.xsl -o html out

