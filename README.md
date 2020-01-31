# Introduction
ShortStack is a tool to transform the way you check in code so that you get more and better code reviews.  
 The typical developer will check in something large and painful at the end of a period of private feature
 development, but with ShortStack, it becomes easy to create small pull requests for each atomic piece of
 work you do to advance the feature.   This allows you to get feedback EARLY, because you won't have to wait for 
 code reviews, and it will make code reviews MORE EFFECTIVE because you can isolate logic changes from 
 each other and from trivial changes such as boiler plate code.

# Getting Started
To get started with the powershell script:
1. Clone short stack to you local drive:  ```git clone https://github.com/microsoft/ShortStack.git [local folder path]```
2. Enable custom scripts on your machine with this powershell command:  ```Set-ExecutionPolicy Unrestricted```
3. Install posh-git:  ```PowerShellGet\Install-Module posh-git -Scope CurrentUser```
4. Get a VSTS access token:
    1. Visit https://mscodehub.visualstudio.com/ShortStack/_git/ShortStack
    2. Click your user icon and click 'Security' 
    3. Click on 'Add' to add a new token (make it active for a year)
    4. Click 'Create Token', then immediately copy the value displayed.
5. Edit your powershell profile with this command: ```notepad $profile```. If prompted, create the file if it doesn't already exist and then add these lines:
```
Import-Module Posh-Git -Force
Import-Module c:\tools\shortstack\scripts\ShortStack.psm1 -Force
$VSTSPersonalAccessToken="(your VSTS personal access key)"
```
6. Close and re-open powershell to make sure your profile works.  You should be able to type ```ss``` on the command line and see ShortStack Help.
7. In each repository, Create a default reviewers file:
    1. Create the file "stackprefs.txt" in your .git folder
    2. run ```Get-VSTSUserGuids``` to see the user id's of users in your repository
    3. for each reviewer you want to include, add this line: ```reviewerid=(vsts_guid_of_user)``` (lines starting with '#' are treated as comments)


# Build and Test
(Coming soon)

# Contribute
Please feel free to fork your own repo and submit your work as a PR.  We will code review and bring in changes that meet a high quality bar.
