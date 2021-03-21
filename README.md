# HaleScript 

This ez script package will implement the famous HALE mod for Superfighters Deluxe.

## Usage

1. Visual Studio Code should download needed packages automatically.
2. The saveScript.py is a python script file to help with the script file creation. It copies the necessary files from the development environment file Program.cs and copies them to the actual Script destination. The path parameters need to be specified in the saveScript.py file. Path e.g. "C:\\users\\name\\Documents\\source.txt" :
  - SRCPATH = Absolute path to source file
  - DESTPATH = Absolute path to destination file. This should be located according to [Superfighters Script manual](http://sfdmaps.at.ua/index/how_to_use_scripts/0-6).
3. Run the Python script and it saves the specified script lines from "Program.cs" to the script file.
