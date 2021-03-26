"""
File which saves the script lines from development environment to
actual script destination

Params: 
- SRCPATH = path to the script file
- DESTPATH = Name of the file (write the value of script path to .env file)
"""

import os

# Setting the path variables for text files.
#  -  !! Create an .env file and set the DESTPATH there. !!  -
SRCPATH = "Sfscript\\Program.cs"

# 'Cooler Daniel' way to do it, but needs pip installing..
# from dotenv import load_dotenv
# load_dotenv() 
# DESTPATH = os.getenv('DESTPATH')

# 'Cool Daniel'
abort = False
try:
    envFile = open(".env", "r")
    DESTPATH = envFile.read()
    envFile.close()
except:
    print("*** File reading error. Check that .env file exists in the root directory. *** ")
    abort = True


def main():
    srcfile = open(SRCPATH, "r")

    rows = srcfile.readlines()

    hasScriptBegun = False
    hasScriptEnded = False

    resultString = ""

    # Loop through the source file. The script lines are specified between the marks '//hahaYES' and '//hahaNO'
    for row in rows:
        if ("//hahaNO" in row):
            hasScriptEnded = True
        if (not hasScriptEnded and not hasScriptEnded):
            if (hasScriptBegun):
                resultString = resultString + row
            else:
                # Check if script has started so reading can begin
                if ("//hahaYES" in row):
                    hasScriptBegun = True
            # Check if script has ended so reading can stop
    srcfile.close()

    # In the endsave the resulting string to the new file
    destfile = open(DESTPATH, "w")
    destfile.write(resultString)
    destfile.close()

    print("Done!")


if(not abort):
    main()
