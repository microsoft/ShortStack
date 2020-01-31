# ShortStack

A tool for creating and managing stacked pull requests

## How to Debug

Setup

1. Install latest [Powershell Core](https://github.com/PowerShell/PowerShell).
2. Open ShortStackLib Properties and go to the "Debug" Tab
3. Use these settings:
   1. Launch: `Executable`
   2. Executable: `pwsh`
   3. Application Arguments:
      `-NoExit -Command "Import-Module ./ShortStackLib.dll; Import-Module posh-git; cd [some test repo]"`
      (The test repo should be a place where you can freely run git activities without disrupting anything)

Running the code

1. Lauch ShortStackLib.  This should bring up a powershell window and give you a posh-git prompt.
2. Run whatever Shortstack commands you want.  They will run under the debugger.



