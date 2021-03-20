"""
File which saves the script lines from development environment to
actual script destination

Params:
- SRCPATH = path to the script file
- DESTPATH = Name of the file (set to be)
"""

import sys

# Set arguments to constant variables0
SRCPATH = "Sfscript\\Program.cs"
DESTPATH = "C:\\Users\\samus\\Documents\\Superfighters Deluxe\\Scripts\\hale.txt"

def main():
    srcfile = open(SRCPATH,"r")
    
    rows = srcfile.readlines()
    
    hasScriptBegun = False
    hasScriptEnded = False

    resultString = ""
    
    # Loop through the source file. The script lines are specified between the marks '//hahaYES' and '//hahaNO'
    for row in rows:
        if ( "//hahaNO" in row ):
                hasScriptEnded = True
        if (  not hasScriptEnded and not hasScriptEnded):
            if ( hasScriptBegun ):
                resultString = resultString + row
            else:
                # Check if script has started so reading can begin
                if ( "//hahaYES" in row ):
                    hasScriptBegun = True
            # Check if script has ended so reading can stop
    srcfile.close()

    # In the endsave the resulting string to the new file
    destfile = open(DESTPATH, "w")
    destfile.write(resultString)

main()