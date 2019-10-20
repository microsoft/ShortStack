# ShortStack
# Copyright Microsoft 2017
# Created by Eric Jorgensen
#
# This is a powershell module to enable a stacked pull request worflow.  Stacked pull
# requests are  useful because they allow you to break up a big change
# into easier-to-digest chunks that will get more useful review comments. 
#
# Note:  this is a work in progress.  My goal is to automate most of this with
#        command-line tools and possibly in a VS extension.  
#
# =====================================================================================

# Strict mode helps us locate syntax errors that would normally be silent
Set-StrictMode -version Latest     

# For messing with rest calls and urls
Add-Type -AssemblyName System.Web

#-----------------------------------------------------------------------------
# Find the origin for the current branch
#-----------------------------------------------------------------------------
function get_origin($branch)
{
     $lines = git remote show origin -n 
     $regex = (new-object System.Text.RegularExpressions.Regex "$branch +merges with remote (.*)")
     foreach($line in $lines)
     {
        $match = $regex.Match($line)  
        if($match.Success) {
            return $match.Groups[1].Value.Trim()
        }  
    }
    return $null
}

#-----------------------------------------------------------------------------
# what branch am I in right now?
#-----------------------------------------------------------------------------
function get_current_branch
{
     $branch = git rev-parse --abbrev-ref HEAD
     return $branch
}

#-----------------------------------------------------------------------------
# Get the parameters for the current stack
#-----------------------------------------------------------------------------
function get_stack_info($stackName)
{
    $output = @{}
    $output.IsStacked = $true

    if($stackName -eq $null)
    {
        $currentBranch = get_current_branch
    }
    else
    {
        $currentBranch = make_stackbranch_name $stackName 0
    }

    $match = (new-object System.Text.RegularExpressions.Regex '(.+?)/(.+?)/(.+)_(\d+)').Match($currentBranch)
    if($match.Success -ne $true)
    {
        $output.IsStacked = $false
        return $output
    }
    
    $output.Name = $match.Groups[3].ToString()
    $output.Number = ([int]($match.Groups[4].ToString()))
    $output.Origin = get_origin $currentBranch
    $output.Branch = $currentBranch
    $template = make_stackbranch_name $output.Name 99
    $template = $template.Replace("_99", "_");
    $output.Template = $template

    $localBranches = git branch
    $pattern = $template + "([0-9]+)$"
    $regex = (new-object System.Text.RegularExpressions.Regex $pattern)

    $highestBranchNumber = -1
    foreach($localBranch in $localBranches)
    {
        $match = $regex.Match($localBranch)  
        if($match.Success) {
            $branchNumber = ([int]($match.Groups[1].ToString()))
            if($highestBranchNumber -lt $branchNumber)
            {
                $highestBranchNumber = $branchNumber
            }
        }      
    }
    $output.LastBranchNumber = $highestBranchNumber

    return $output
}

#-----------------------------------------------------------------------------
# Generic function for making a rest GET call
#-----------------------------------------------------------------------------
function show_vsts_error
{
    $webUrl = git config --get remote.origin.url

    write-host -ForegroundColor Red "******* ERROR *********"
    write-host -ForegroundColor Red 'Could not access VSTS. Please make sure that $VSTSPersonalAccessToken'
    write-host -ForegroundColor Red 'is set to the correct value in your powershell config.'
    write-host -ForegroundColor White 'To get a current access token:'
    write-host "    1) Visit $webUrl"
    write-host "    2) Click on your user name on the VSTS page and click on 'My profile'"
    write-host "    3) From there, click on the 'Manage Security' link on the right"
    write-host "    4) Click on 'Add' to add a new token with these properties"
    write-host "         Description: Powershell access"
    write-host "         Expires In:  1 Year"
    write-host "         Accounts:    All accessible"
    write-host "         Description: Powershell access"
    write-host "         Scopes:      All"
    write-host "    5) Click 'Create Token', then immediately copy the value displayed."
}

#-----------------------------------------------------------------------------
# Generic function for making a rest GET call
#-----------------------------------------------------------------------------
function rest_get($uri)
{
    $error.Clear()
    $output = Invoke-RestMethod -Uri $uri -Method Get -ContentType "application/json" -Headers @{Authorization=(get_vsts_auth_header)}
    
    if($error.Count -gt 0)
    {
        show_vsts_error
        return ,@()      
    }

    return ,$output
}

#-----------------------------------------------------------------------------
# Generic function for making a rest GET call
#-----------------------------------------------------------------------------
function rest_patch($uri, $jsonDocument)
{
    $error.Clear()
    $output = Invoke-RestMethod -Uri $uri -Method PATCH -Body $jsonDocument -ContentType "application/json" -Headers @{Authorization=(get_vsts_auth_header)}
    
    if($error.Count -gt 0)
    {
        show_vsts_error
        return ,@()      
    }

    return ,$output
}

#-----------------------------------------------------------------------------
# Get the url for the specified api;  eg: get_api_url("pullRequests")
# Note: the url will have ?api-version already specified
#-----------------------------------------------------------------------------
function get_remote_url_parts
{
    $output = @{}
    $webUrl = git config --get remote.origin.url
    $partMatch = [regex]::Match($webUrl, "^https://(.+?)\.([^/]+)/([^/]+)?(/[^/]+)?/_git(/[^/]+)?")
    $output.FullUrl = $webUrl
    $output.BadUrl = $false
    if($partMatch.Success)
    {
        $output.Server = $partMatch.Groups[1].Value
        $output.Host = $partMatch.Groups[2].Value
        $output.Collection = $partMatch.Groups[3].Value.Trim('/')
        $output.Project = $partMatch.Groups[4].Value.Trim('/')
        $output.Repository = $partMatch.Groups[5].Value.Trim('/')
        if($output.Collection -eq "")
        {
            # 'server/_git/Name' is a repository with the same name as the project
            $output.Collection = "DefaultCollection"
            $output.Project = $output.Repository
        }
        elseif($output.Project -eq "")
        {
            # 'server/Project/_git/Name' is a repository in a single-project collection
            $output.Project = $output.Collection
            $output.Collection = "DefaultCollection"
        }
    }
    else
    {
        $output.BadUrl = $true
    }
    return $output
}

#-----------------------------------------------------------------------------
# Get the url for the specified api;  eg: get_api_url("pullRequests")
# Note: the url will have ?api-version already specified
#-----------------------------------------------------------------------------
function get_api_url($api, $query)
{
    $parts = get_remote_url_parts
    if($parts.BadUrl)
    {
        write-host -ForegroundColor Red "ERROR: the web url is in an unrecognized format:" $parts.FullUrl
        return ""
    }

    $server = $parts.Server
    $host = $parts.Host
    $collection = $parts.Collection
    $project = $parts.Project
    $repository = $parts.Repository

    $apiUrl = "https://$server.$host/$collection/$project/_apis/git/repositories/$repository"

    return $apiUrl + "/$api" +"?api-version=3.0" + $query
}


#-----------------------------------------------------------------------------
# Get all the pull requests that match the query
#-----------------------------------------------------------------------------
function get_pull_requests($query) 
{    
    $getPullRequestsUri = get_api_url "pullRequests" $query
    return ,(rest_get($getPullRequestsUri))."value";
}

#-----------------------------------------------------------------------------
# Get the pull request associated with this stack branch
#-----------------------------------------------------------------------------
function get_current_pull_request() 
{    
    $stackInfo = get_stack_info
    if(!$stackInfo.IsStacked)
    {
        return $null
    }
    $query="&status=Active"
    $query += "&sourceRefName=" + [System.Web.HttpUtility]::UrlEncode("refs/heads/" + $stackInfo.Branch)
    $pullRequests = get_pull_requests($query)
    return $pullRequests[0]
}

#-----------------------------------------------------------------------------
# Get all the commits that match the query
#-----------------------------------------------------------------------------
function get_commits($remoteBranch, $numberToGet) 
{    
    if($numberToGet -eq $null)
    {
        $numberToGet = 5
    }
    if($remoteBranch.StartsWith("refs/heads/"))
    {
        $remoteBranch = $remoteBranch.SubString(11)
    }
    $query = "&branch=" + [System.Web.HttpUtility]::UrlEncode($remoteBranch)
    $query += '&$top=' + $numberToGet
    $getPullRequestsUri = get_api_url "commits" $query
    
    return @((rest_get($getPullRequestsUri))."value");
}


#-----------------------------------------------------------------------------
# Get a summary of all commit comments
#-----------------------------------------------------------------------------
function get_unpushed_commit_summary($stackLevel)
{   
    $stackInfo = get_stack_info
    if(!$stackInfo.IsStacked)
    {
        $output = @{}
        $output.CommitCount = 0
        return $output
    }

    if($stackLevel -eq $null)
    {
        $stackLevel = $stackInfo.Number
    }

    $sourceBranch = $stackInfo.template + $stackLevel.ToString("00")  
    $destinationBranch = "origin/" + $sourceBranch

    #use cherry to find the commits not in the destination branch
    $commitLines =  git cherry -v  $destinationBranch $sourceBranch 
    $output = @{}
    $commitCount = 0
    [System.Collections.ArrayList]$commentLines = @()
    foreach($line in $commitLines)
    {
        $partMatch = [regex]::Match($line, '^\+ ([0-9a-z]+) ')
        if($partMatch.Success)
        {
            $commitCount++
            # Use git show to get the actual commit lines
            $showLines = git show $partMatch.Groups[1].Value
            for($i=4; $i -lt $showLines.Count; $i++)
            {
                if($showLines[$i] -eq "") 
                {
                    break
                }
                [void]$commentLines.Add($showLines[$i].Trim()) 
            }
        }
    }
    $output.CommitCount = $commitCount
    $output.Lines = $commentLines
    return $output
}

#-----------------------------------------------------------------------------
# Standard way to name a stack branch
#-----------------------------------------------------------------------------
function make_stackbranch_name($name, $number)
{
    return "users/$env:username/" + $name + "_" + $number.ToString("00") 
}

#-----------------------------------------------------------------------------
# Check git for unpushed changes
#-----------------------------------------------------------------------------
function environment_has_unpushed_changes($stackLevel)
{
    $unpushedCommits = get_unpushed_commit_summary $stackLevel

    return $unpushedCommits.CommitCount -gt 0
}

#-----------------------------------------------------------------------------
# Check git for uncommitted changes
#-----------------------------------------------------------------------------
function environment_has_uncommitted_changes
{
    $statusLines = git status;

    $hasUnstagedChanges = lineset_has_regex_match $statusLines  "^Changes not staged for commit"
    $hasStagedChanges = lineset_has_regex_match $statusLines  "^Changes to be committed"
    $hasUncommittedChanges = ($hasUnstagedChanges -or $hasStagedChanges)

    return $hasUncommittedChanges
}

#-----------------------------------------------------------------------------
# helper for checking a set of lines for a patter
#-----------------------------------------------------------------------------
function lineset_has_regex_match($lines, $regexPattern)
{
    $regex = new-object System.Text.RegularExpressions.Regex $regexPattern
    foreach($line in $lines)
    {
        if($regex.Match($line).Success) {
            return $true
        }  
    }
    return $false 
    
}

#-----------------------------------------------------------------------------
# Check git for uncommitted changes
#-----------------------------------------------------------------------------
function current_branch_has_diverged
{
    git fetch
    $lines =  git status
    $hasDiverged = lineset_has_regex_match $lines  "have diverged,"
    if($hasDiverged) { return $true }
    $hasDiverged = lineset_has_regex_match $lines  "branch is behind"
    if($hasDiverged) { return $true }
    return $false
}

#-----------------------------------------------------------------------------
# Do a git pull and report back the status
#-----------------------------------------------------------------------------
function git_pull($originBranch)
{
    $output = @{}
    $output.gitCommand = "pull"
    $output.IsSuccessful = $true
    $output.AlreadyUpToDate = $false
    [System.Collections.ArrayList]$conflicts = @()
    [System.Collections.ArrayList]$errors = @()

    $originBranch = $originBranch -ireplace '^origin/', ''

    git pull origin $originBranch *>&1 | %{
        $match = (new-object System.Text.RegularExpressions.Regex "^CONFLICT").Match($_)  
        if($match.Success) {
            [void]$conflicts.Add($_)
            $output.IsSuccessful = $false
        }  
        $match = (new-object System.Text.RegularExpressions.Regex "^error:").Match($_)  
        if($match.Success) {
            [void]$errors.Add($_)
            $output.IsSuccessful = $false
        }  
        $match = (new-object System.Text.RegularExpressions.Regex "Already up to date").Match($_)  
        if($match.Success) {
            $output.AlreadyUpToDate = $true
        }  
    }

    $output.Conflicts = $conflicts
    $output.Errors = $errors

    return $output
}

#-----------------------------------------------------------------------------
# Do a git push and report back the status
#-----------------------------------------------------------------------------
function git_push($dest, $source)
{
    $output = @{}
    $output.gitCommand = "push"
    $output.IsSuccessful = $true
    $output.AlreadyUpToDate = $false
    [System.Collections.ArrayList]$conflicts = @()
    [System.Collections.ArrayList]$errors = @()

    # git push is weird is returns everything in stderr
    $commandOutput = git push $dest $source *>&1 | Out-String 
    $lineOutput =(-Join $commandOutput).Split("`n")

    foreach($line in $lineOutput)
    {
        $match = (new-object System.Text.RegularExpressions.Regex "^(error|hint|git : fatal:):").Match($line)  
        if($match.Success) {
            [void]$errors.Add($line)
            $output.IsSuccessful = $false
        }  
        $match = (new-object System.Text.RegularExpressions.Regex "up-to-date").Match($line)  
        if($match.Success) {
            $output.AlreadyUpToDate = $true
        }         
    }

    $output.Conflicts = $conflicts
    $output.Errors = $errors

    return $output
}


#-----------------------------------------------------------------------------
# Do a git checkout and report back the status
#-----------------------------------------------------------------------------
function git_checkout($branch)
{
    $output = @{}
    $output.gitCommand = "checkout"
    $output.IsSuccessful = $true
    [System.Collections.ArrayList]$conflicts = @()
    [System.Collections.ArrayList]$errors = @()

    git checkout $branch *>&1 | %{
        $match = (new-object System.Text.RegularExpressions.Regex "^error:").Match($_)  
        if($match.Success) {
            $output.IsSuccessful = $false
            [void]$errors.Add($_)
            return $output
        }  
    }

    return $output
}


#-----------------------------------------------------------------------------
# Get the value we should pass in to vsts rest calls authorization header
#-----------------------------------------------------------------------------
function get_vsts_auth_header
{
    $parts = get_remote_url_parts

    if($parts.BadUrl -ne $true)
    {
        try
        {
            $null = Get-Variable -Scope Global -Name VSTSTokens -ErrorAction Stop
            $tokensExist = $true
        }
        catch
        {
            $tokensExist = $false
        }

        if($tokensExist)
        {
             if( $VSTSTokens[$parts.Server] -ne $null)
             {
                 $authBytes = [Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f "",$VSTSTokens[$parts.Server]))
             }
             else
             {
                 write-host -ForegroundColor red "Error: No token for repo '"$parts.Server"' found in VSTSTokens"
                 return ""
             }
        }
        else
        {
            if($VSTSPersonalAccessToken -eq $null)
            {
                write-host -ForegroundColor red "Error: No VSTS access token found. "
                return ""
            }

            $authBytes = [Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f "",$VSTSPersonalAccessToken))
        }
        return "Basic " + [Convert]::ToBase64String($authBytes)
    }
    return ""
}

#-----------------------------------------------------------------------------
# Open up the current git project in VSTS
#-----------------------------------------------------------------------------
function Get-VSTSUserGuids 
{
    $response = get_pull_requests("") 

    $output = [ordered]@{}
    foreach($request in $response)
    {
        foreach($reviewer in $response."reviewers")
        {
            $output[$reviewer."displayName"] = $reviewer."id"
        }
    }

    foreach($item in $output.GetEnumerator())
    {
        write-host $item.Name ": " $item.Value
    }
}

#-----------------------------------------------------------------------------
# Open up the current git project in VSTS
#-----------------------------------------------------------------------------
function govsts 
{
    $url = git config --get remote.origin.url
    start microsoft-edge:$url
}

#-----------------------------------------------------------------------------
# Look in the environment for default reviewers
#-----------------------------------------------------------------------------
function get_default_reviewers
{
    $rootFolder = git rev-parse --show-toplevel
    $lines = get-content "$rootFolder\.git\stackprefs.txt" 2>  $null
    [System.Collections.ArrayList]$reviewers = @()
    foreach($line in $lines)
    {
        $parts = $line.Trim().Split("=",2)
        if($parts.Count -eq 2)
        {
            $name = $parts[0].ToLower()
            if($parts[0].ToLower() -eq "reviewerid")
            {
                $newReviewer = @{}
                $newReviewer.id = $parts[1]
                $reviewers.Add($newReviewer) > $null
            }
        }
    }
    return ,$reviewers
}

#-----------------------------------------------------------------------------
# Warn the user if some work is dangling
#-----------------------------------------------------------------------------
function warn_on_dangling_work
{
    $command = $((Get-PSCallStack)[1].Command)
    $currentBranch = get_current_branch
    if(environment_has_uncommitted_changes)
    {
        write-host -ForegroundColor Red "ERROR: The current environment has uncommitted changes."
        write-host -ForegroundColor Red "(Current branch is $currentBranch)"
        write-host -ForegroundColor Red "Please 'git stash' or commit changes before running '$command'."
        return $true
    }

    if(environment_has_unpushed_changes)
    {
        write-host -ForegroundColor Yellow "WARNING: There are unpushed commits"
        write-host -ForegroundColor Yellow "(Current branch is $currentBranch)" 
    }
    return $false
}

#-----------------------------------------------------------------------------
# quick error
#-----------------------------------------------------------------------------
function error($message)
{
    write-host ForegroundColor Red $message
    write-error $message -ErrorAction:SilentlyContinue
}

#-----------------------------------------------------------------------------
#  Helper to create easily spottable label text
#-----------------------------------------------------------------------------
function bar($message)
{
    $spaces = "                                                              ";
    $width = 80
    $left = [int](($width - $message.Length) / 2)
    $right = $width - $message.Length - $left
    write-host -NoNewLine -ForegroundColor White -BackGroundColor DarkCyan "##"
    write-host -NoNewLine -ForegroundColor White -BackGroundColor DarkCyan $spaces.SubString(0,$left)
    write-host -NoNewLine -ForegroundColor Black -BackGroundColor DarkCyan $message
    write-host -NoNewLine -ForegroundColor White -BackGroundColor DarkCyan $spaces.SubString(0,$right)
    write-host -ForegroundColor White -BackGroundColor DarkCyan "##"
}

#-----------------------------------------------------------------------------
# Make a string safe for json
#-----------------------------------------------------------------------------
function json_encode($text)
{
    return '"' + [System.Web.HttpUtility]::JavaScriptStringEncode($text) + '"'
}


#-----------------------------------------------------------------------------
# helper to patch a pull request
#-----------------------------------------------------------------------------
function patch_pull_request($pullRequest, $patchDictionary)
{
    $jsonPatch = "{"
    $separator = ""
    foreach ($item in $patchDictionary.GetEnumerator()) 
    {
        $jsonPatch += $separator + '"' + $item.Name + '":' + $item.Value
        $separator = ","
    }
    $jsonPatch += "}"

    #write-host "PATCH: $jsonPatch"
    $urlSuffix = "pullrequests/" + $pullRequest."pullrequestId"
    $apiUrl = get_api_url $urlSuffix
    $result = rest_patch $apiUrl $jsonPatch
}

#-----------------------------------------------------------------------------
# Look at this result from a git call.  Print any problems.  Return true
# if the command succeeded.
#-----------------------------------------------------------------------------
function check_for_good_git_result($result)
{
    if($result.IsSuccessful -ne $true)
    {
        write-host -ForegroundColor Red "Error: There were problems with git "$result.gitCommand 
        foreach($error in $result.Errors)
        {
            write-host -ForegroundColor Red  "  $error"
        }
        foreach($conflict in $result.Conflicts)
        {
            write-host -ForegroundColor Red  "  $conflict"
        }
    }

    return $result.IsSuccessful
}

#-----------------------------------------------------------------------------
# For quick tests
#-----------------------------------------------------------------------------
function zz($a, $b, $c)
{
    $stackInfo = get_stack_info
    write-host "Current Stack Level is "$stackInfo.Number

    $unpushedCommits = get_unpushed_commit_summary $stackInfo.Number
    write-host "unpushed count "$unpushedCommits.CommitCount

}


##############################################################################
##############################################################################
###########################    SS API functions     ##########################
##############################################################################
##############################################################################

#-----------------------------------------------------------------------------
# Show quick help
#-----------------------------------------------------------------------------
function sshelp_main
{
    write-host "ShortStack is a tool for handling a stacked pull request workflow."
    write-host 
    write-host "for more information:"
    write-host -NoNewLine -ForegroundColor white "    ss help commands      "
    write-host "Show available commands"
    write-host -NoNewLine -ForegroundColor white "    ss help workflow      "
    write-host "Describe the ss workflow"
    write-host -NoNewLine -ForegroundColor white "    ss help setup         "
    write-host "Instructions on how to set up your environment for stacked PRs"
}

#-----------------------------------------------------------------------------
# Show quick help
#-----------------------------------------------------------------------------
function sshelp_commands
{
    write-host "Commands available in the ShortStack module:"
    write-host -ForegroundColor white "    ss abandon"
    write-host "         Throw away all work in the current stack"
    write-host -ForegroundColor white "    ss go (number) [(name)]"
    write-host "         Go to the specified branch in a stack"
    write-host -ForegroundColor white "    ss finish"
    write-host "         Finish the stack and create a final pull request with all changes"
    write-host -ForegroundColor white "    ss help"
    write-host "         Show this information"
    write-host -ForegroundColor white  "    ss list"
    write-host "         Show all the stacks in the current repository"
    write-host -ForegroundColor white  "    ss push"
    write-host "         Push current commits up to the server, and create a PR if one doesn't exist."
    write-host -ForegroundColor white  "    ss new [(name)] [(origin)]"
    write-host "         Create a new stack with (name) tracking (origin).  If arguments are ommited, a new stack level is created."
    write-host -ForegroundColor white "    ss update"
    write-host "         Starting at the remote origin at the top of the stack, resolve all diverging changes."
    write-host -ForegroundColor white "    ss status"
    write-host "         Show the status of the current stack"
    write-host -ForegroundColor white "    govsts"
    write-host "         Open the VSTS web page for this repository"
    write-host -ForegroundColor white "    Get-VSTSUserGuids"
    write-host "         Show the Vsts user ids that are active in this repository (usefule for default reviewer file)"

}

#-----------------------------------------------------------------------------
# Show quick help
#-----------------------------------------------------------------------------
function sshelp_workflow
{
    write-host "#########################################################################"
    write-host "#  SHORTSTACK WORKFLOW                                                  #"
    write-host "#########################################################################"
    write-host "Stacked pull requests are a way to divide up a large code change so that:"
    write-host "    1) All sub-changes are single-thesis, thus easier to code review"
    write-host "    2) Comments and fixes are tracked in the context of the logical change,"
    write-host "       creating more thorough code reviews."
    write-host "    3) The developer can quickly move on with new changes without having to"
    write-host "       wait for dependencies to be accepted and merged."
    write-host
    write-host "Basic Workflow:"
    write-host "    I have a new project that I'm calling 'superfoo' that is based on 'dev' branch,"
    write-host "    so I start a stack and make some changes like this:"
    write-host "        1) at an up-to-date repository prompt, run:  ss new superfoo dev"
    write-host "        2) make one or more commits"
    write-host "        3) Push changes and create a new pull request:  ss push"
    write-host "           (code review is automatically posted)"
    write-host ""
    write-host "    Ready for next thesis:"
    write-host "        1) Automatically create next branch in the stack:  ss new"
    write-host "        2) make one or more commits"
    write-host "        3) Push changes and create a new pull request:  ss push"
    write-host "        (repeat 1-3 for as many logical changes you want to make)"
    write-host ""
    write-host "    Go back to make fixes that address a code review on branch 2"
    write-host "        1) Go to the right branch:   ss go 2"
    write-host "        2) Make one or more commits"
    write-host "        3) Push changes to the existing pull request:  ss push"
    write-host "        4) Sync up the now diverged branches (including dev):   ss update"
    write-host ""
    write-host "    If you want to see the status of your stack:      ss status"
    write-host "    When you are ready to check in the whole stack:   ss finish "
    write-host "    If you want to give up and throw it all away:     ss abandon"
    write-host ""
}

#-----------------------------------------------------------------------------
# Show quick help
#-----------------------------------------------------------------------------
function sshelp_setup
{
    $myPath = (Get-Module ShortStack).path
    if($myPath -eq $null)
    {
        $myPath = "(The path to this module)"
    }
    $webUrl = git config --get remote.origin.url

    write-host "Setup for stacked pull request workflow:"
    write-host "    1) Install posh-git by running: PowerShellGet\Install-Module posh-git -Scope CurrentUser"
    write-host "       start up posh git in your environment: Import-module posh-git"
    write-host "       (you will see git information in your prompt if posh-git is loaded)"
    write-host
    write-host '    2) Get a current VSTS access token:'
    write-host "           1) Visit $webUrl"
    write-host "           2) Click on your user name on the VSTS page and click on 'My profile'"
    write-host "           3) From there, click on the 'Manage Security' link on the right"
    write-host "           4) Click on 'Add' to add a new token with these properties"
    write-host "              Description: Powershell access"
    write-host "              Expires In:  1 Year"
    write-host "              Accounts:    All accessible"
    write-host "              Description: Powershell access"
    write-host "              Scopes:      All"
    write-host "           5) Click 'Create Token', then immediately copy the value displayed."    
    write-host
    write-host "    3) Include the posh-git module in your environment by loading it from your profile:"
    write-host '           mkdir C:\Users\$env:username\Documents\WindowsPowerShell'
    write-host '           notepad $profile'
    write-host ""
    write-host "        Add these lines to include posh-git and the SS module in your environment:"
    write-host "           Import-Module Posh-Git -Force"
    write-host "           Import-Module $myPath -Force"
    write-host '           $VSTSTokens = @{}'
    write-host '           $VSTSTokens.Add("(your vsts server, eg: mscodehub)", "(your VSTS personal access key)")'
    write-host
    write-host '    4) Reload your profile: '
    write-host '            . $profile'
    write-host ""
    write-host "    5) Create a default reviewers file:" 
    write-host '        - Create the file "stackprefs.txt" in your .git folder'
    write-host "        - run 'Get-VSTSUserGuids' to see the user id's" 
    write-host '        - for each reviewer you want to include, add this line:'
    write-host '            reviewerid=(vsts_guid_of_user)'
    write-host "          (lines starting with '#' are treated as comments)"
}

#-----------------------------------------------------------------------------
# Show quick help
#-----------------------------------------------------------------------------
function sshelp($subHelp)
{
    write-host
    switch ($subHelp)
    {
        commands    { sshelp_commands }
        workflow    { sshelp_workflow }
        setup       { sshelp_setup }
        $null       { sshelp_main }
        default     { sshelp_main }
    }
    write-host
    write-host "Question and/or comments?  Please contact Eric.Jorgensen@microsoft.com"
    write-host
}


#-----------------------------------------------------------------------------
# Show a quick status for the stack
#-----------------------------------------------------------------------------
function ssstatus
{
    $localBranches = git branch
    $defaultColor = (get-host).ui.rawui.ForegroundColor
    $stackInfo = get_stack_info
    $currentBranch = get_current_branch

    if(!$stackInfo.IsStacked)
    {
        write-host "Current branch is not in a stacked configuration."
        return
    }

    write-host -ForegroundColor White "============== STACK STATUS ================== "    

    for($i = 1; $i -lt 1000; $i++)
    {
        $branchName = make_stackbranch_name $stackInfo.Name $i
        $isABranch = $false
        if($localBranches.Contains("  $branchName") -Or $localBranches.Contains("* $branchName"))
        {
            $isABranch = $true
        }
        if(!$isABranch)
        {
            return
        }

        $query="&status=Active"
        $query += "&sourceRefName=" + [System.Web.HttpUtility]::UrlEncode("refs/heads/$branchName")
        $pullRequests = get_pull_requests($query)
        $representativePullRequest = $null
        if($pullRequests.Count -gt 0)
        {
            $representativePullRequest = $pullRequests[0]
        }
        else
        {
            $query="&status=Completed"
            $query += "&sourceRefName=" + [System.Web.HttpUtility]::UrlEncode("refs/heads/$branchName")
            $pullRequests = get_pull_requests($query)
            if($pullRequests.Count -gt 0)
            {
                $representativePullRequest = $pullRequests[0]
            }
        }

        $color = $defaultColor
        if($branchName -eq $currentBranch)
        {
            $color = "Green"
        }

        $unpushedStatus = "";
        if(environment_has_unpushed_changes $i)
        {
             $unpushedStatus = "(Has unpushed commits) ";
        }
        write-host -NoNewLine -ForegroundColor $color "  "$i": "$branchName
        if($representativePullRequest -eq $null)
        {
            write-host -ForegroundColor DarkGray "    $unpushedStatus *** NO Pull Request ***"
        }
        else
        {
            $color = $defaultColor
            $status = $representativePullRequest."status" 
            if($status -eq "Active")
            {
                $color = "Yellow"
            }
            write-host -ForegroundColor $color "   $unpushedStatus "$representativePullRequest."title"
        }
    }   
}


#-----------------------------------------------------------------------------
# This will abandon all the PR's associated with the current stack
#-----------------------------------------------------------------------------
function ssabandon
{
    $stackInfo = get_stack_info
    if($stackInfo.IsStacked -ne $true)
    {
        Write-host "Current branch is not a stacked branch.  To start a stack: ss new (name) (origin branch to track)"
        return
    }

    write-host -ForegroundColor Yellow "WARNING: This will delete all local branches for this stack"
    write-host -ForegroundColor Yellow "and abandon related pull requests.   Are you sure?"

    $reply = Read-Host -Prompt "Type 'YES' to continue: "
    if($reply -ne "YES")
    {
        write-host "OK, the bits will live to fight another day."
        return
    }

    $safeDirectory = GET-gitDirectory
    cd $safeDirectory
    cd ..

    $localBranches = git branch
    $defaultBranch= $localBranches[0].Trim()
    if($defaultBranch.StartsWith("* "))
    {
        $defaultBranch = $defaultBranch.SubString(2)
    }
    write-host "Switching to branch" $defaultBranch
    git checkout $defaultBranch

    $query = "&status=active"
    $pullRequests = get_pull_requests($query)

    $branchRoot = "refs/heads/" + $stackInfo.Branch.SubString(0, $stackInfo.Branch.Length-2)

    foreach($pullRequest in $pullRequests)
    {
        if(($pullRequest."sourceRefName").StartsWith($branchRoot))
        {
            write-host "Abandoning pull request" $pullRequest."pullrequestId" $pullRequest."title" 
            patch_pull_request $pullRequest @{"description" =  json_encode "abandoned"}
        }
    }


    for($i = 100; $i -gt -1; $i--)
    {
        $branchName = make_stackbranch_name $stackInfo.Name $i
        if($localBranches.Contains("  $branchName") -Or $localBranches.Contains("* $branchName"))
        {
            write-host "Deleting branch $branchName" 
            git branch -D $branchName
        }
    }
    
    write-host "DONE"
}

#-----------------------------------------------------------------------------
# This will create a stacked pull request.
#
# $name     The human-readable part of the branch name
# $origin   The name of the branch to track on the server (default: master)
#
# You only need to provide the arguments when starting a stack.  If no
# arguments are provided, then a new branch is created to stack on top of
# the current branch.  
#-----------------------------------------------------------------------------
function ssgo($number, $stackName)
{
    $currentBranch = get_current_branch

    switch -Regex ($number)
    {
        $null { $number = -1 }
        '^[0-9]+$' { } 
        'last' { $number = -1; }
        'first' { $number = 0 }
        default { 
            $temp = $stackName
            $stackName = $number; 
            $number = $temp;
        } 
    }

    $stackInfo = get_stack_info $stackName
    if($stackInfo.IsStacked -ne $true)
    {
        Write-host -ForegroundColor Red "Current branch is not a stacked branch.  To start a stack:  ss new (name) (origin branch to track)"
        return
    }


    if($number -eq "last" -or $number -eq -1 -or $number -eq $null)
    {
        $number = $stackInfo.LastBranchNumber
    }

    if($stackInfo.Origin -eq $null)
    {
        Write-host -ForegroundColor Red "The stack '$stackName' does not exist.  To start a stack:  ss new (name) (origin branch to track)"
        return;
    }

    if($number -gt $stackInfo.LastBranchNumber)
    {
        Write-host -ForegroundColor Red "The stack '"$stackInfo.Name"' only goes up to "$stackInfo.LastBranchNumber
        Write-host -ForegroundColor Red "Use 'ss go last' to go to the last branch."
        return;
    }


    $targetBranch = make_stackbranch_name $stackInfo.Name $number
    if($currentBranch -eq $targetBranch)
    {
        write-host "You are already on this branch."
        return
    }

    if(warn_on_dangling_work)
    {
        write-host -ForegroundColor DarkYellow "To force the branch change anyway, type:   git checkout $targetBranch"
        return       
    }

    $null = git checkout $targetBranch 
}


#-----------------------------------------------------------------------------
# This will create a stacked pull request.
#
# $name     The human-readable part of the branch name
# $origin   The name of the branch to track on the server (default: master)
#
# You only need to provide the arguments when starting a stack.  If no
# arguments are provided, then a new branch is created to stack on top of
# the current branch.  
#-----------------------------------------------------------------------------
function ssnew($name, $desiredOrigin)
{
    if(warn_on_dangling_work)
    {
        return       
    }

    $number = 0
    $stackInfo = get_stack_info $name
    $origin = $desiredOrigin

    # if no name, then figure it out from the current branch name
    if($stackInfo.IsStacked -ne $true)
    {
        Write-host "Current branch is not a stacked branch.  To start a stack:  ss new (name) (origin branch to track)"
        return
    }
    
    # if this is an active stack, then override the desired origin with the last branch
    if($stackInfo.Origin -ne $null)
    {
        $lastBranch = make_stackbranch_name $stackInfo.Name $stackInfo.LastBranchNumber
        if($lastBranch -ne $stackInfo.Branch)
        {
            $null = git checkout $lastBranch
            $stackInfo = get_stack_info
        }
        $origin = $stackInfo.Branch
    }

    $name = $stackInfo.Name
    $number = $stackInfo.LastBranchNumber + 1      
    
    # If we still don't have an origin, default to master
    if($origin -eq $null)
    {
        $origin = "master"
    }
    
    # Make sure we are tracking something from the server
    if($origin.StartsWith("origin/") -eq $false)
    {
        $origin = "origin/$origin"
    }
    
    $newBranch = make_stackbranch_name $name $number 

    # for new stacks, we need to create a "zero" branch to isolate the
    # stack from the tracked branch.  This is because the tracked 
    # branch usually has policies that prevent us from automatically
    # completing and commiting pull requests when we are finished.  
    if($number -eq 0)
    {
        write-host -ForegroundColor Gray "Creating branch 'zero' branch to track $origin ..."
        git branch $newBranch --track $origin *> $null
        git checkout $newBranch   *> $null
        $pullResult = git_pull $origin
        [void](check_for_good_git_result $pullResult)
        $pushResult = git_push origin $newBranch
        [void](check_for_good_git_result $pushResult)

        $origin = "origin/$newbranch"
        $number++
        $newBranch = make_stackbranch_name $name $number 
    }
    
    write-host -ForegroundColor White "Creating branch $newBranch to track $origin ..."
    git branch $newBranch --track $origin *> $null
    git checkout $newBranch   *> $null
   
    write-host "Synchronizing with tracking branch..."
    $pullResult = git_pull $origin
    [void](check_for_good_git_result $pullResult)
   

    #create the upstream branch
    write-host "Creating remote version of $newBranch"
    $pushResult = git_push origin $newBranch
    [void](check_for_good_git_result $pushResult)

    write-host -ForegroundColor Green "==================================="
    write-host -ForegroundColor Green "---  Your new branch is ready!  ---"
    write-host -ForegroundColor Green "==================================="
    write-host "Next steps:"
    write-host "    1) Keep to a 'single-thesis' change for this branch"
    write-host "    2) make as many commits as you want"
    write-host "    3) When your change is finished:"
    write-host "       ss push   <== pushes your changes up and creates a pull request."
    write-host "       ss new    <== creates the next branch for a new change.`n`n"
}

#-----------------------------------------------------------------------------
# Show a list of available stacks
#-----------------------------------------------------------------------------
function sslist($name, $desiredOrigin)
{
    [System.Collections.ArrayList]$output = @()
    $lines = git branch
    $regex = new-object System.Text.RegularExpressions.Regex "..(.+?)/(.+?)/(.+)_(\d+)"
    foreach($line in $lines)
    {
        $match = $regex.Match($line)
        if($match.Success) {
            $name = $match.Groups[3].Value
            if(!$output.Contains($name))
            {
                $null = $output.Add($name);
            }
        }  
    }
    
    if($output.Count -eq 0)
    {
        write-host "There are no stacks in this repository"
        return
    }
    
    write-host "Found these stacks:"
    foreach($name in $output)
    {
        write-host "  $name"
    }
}

#-----------------------------------------------------------------------------
# This will wrap up the entire stack and prepare a final pull request 
# with all the changes in it
#-----------------------------------------------------------------------------
function ssfinish($name, $desiredOrigin, $deleteFlag)
{
    # make sure this is a stack
    $stackInfo = get_stack_info
    if($stackInfo.IsStacked -ne $true)
    {
        Write-host "Current branch is not a stacked branch."
        write-host "  To start a stack:         ss new (name) (origin branch to track)"
        write-host "  To got to a stack:        ss go last (name)"
        write-host "  To see available stacks:  ss list"
        return
    }

    # don't finish if there are uncommitted changes
    if(warn_on_dangling_work)
    {
        return       
    }

    # ask the user for confirmation
    write-host -ForegroundColor Yellow "WARNING: This command will delete local stacked branches and mark all pull requests as complete."
    write-host "It is assumed you have run the following:"
    write-host -NoNewLine -ForegroundColor White "SS UPDATE 0" 
    write-host "   - to make sure everything has been pushed"
    write-host -NoNewLine -ForegroundColor White "SS STATUS" 
    write-host "   - to make sure you have pull requests in place."
    write-host -ForegroundColor DarkYellow "Do you wish to continue?"

    $reply = Read-Host -Prompt "Type 'YES' to continue: "
    if($reply -ne "YES")
    {
        write-host "Way to keep a cool head.  Come back when you are ready."
        return
    }

    # create a new branch for finishing
    write-host "Creating 'finish' branch..."
    $zeroBranch = $stackInfo.Template + "00"
    $null = git checkout $zeroBranch
    $stackInfo = get_stack_info
    $rootOrigin = $stackInfo.Origin
    $finishBranch = $stackInfo.Template + "FINISH"
    $null = git branch $finishBranch --track "origin/$rootOrigin"
    $null = git checkout $finishBranch

    $lastBranch = $stackInfo.Template + $stackInfo.LastBranchNumber.ToString("00")
    $error.Clear()
    $null = git merge $lastBranch
    if($error.Count -ne 0)
    {
        write-host -ForegroundColor Red "There were errors while attempted to merge to the finish branch."
        write-host "Complete the merge manually and run this command again."
        return
    }
    $pushResult = git_push origin $finishBranch
    $successfulPush = check_for_good_git_result $pushResult
    if(!$successfulPush)
    {
        write-host -ForegroundColor Red "Please resolve above reported errors and run this command again."
        return
    }

    $description = ""

    # Mark active pull requests as completed
    $query = "&status=active"
    $pullRequests = get_pull_requests($query)
    if($pullRequests.Count -eq 0)
    {
        write-host -ForegroundColor DarkYellow "No active pull requests for this stack, may indicate an issue with this operation."
        write-host -ForegroundColor DarkYellow "This may be expected, if there truly are no active pull requests for this stack."
        write-host -ForegroundColor DarkYellow "Do you wish to continue?"

        $reply = Read-Host -Prompt "Type 'YES' to continue: "
        if($reply -ne "YES")
        {
            write-host "Sounds good to me. Best of luck!"
            return
        }
    }
    $branchRoot = "refs/heads/" + $stackInfo.Template
    $pullRequestLinks = "### Links to Stacked Pull Requests: `n"
    foreach($pullRequest in $pullRequests)
    {
        $description += $pullRequest."title" + "`n"
        $sourceBranch = $pullRequest."sourceRefName"
        $commits = get_commits $sourceBranch 2
        $lastCommitId = $commits[0]."commitId"

        if(($pullRequest."targetRefName").StartsWith($branchRoot))
        {
            write-host "Completing pull request" $pullRequest."pullrequestId" $pullRequest."title"
            $jsonPatch = @{}
            $jsonPatch.Add('status', '"completed"')
            $jsonPatch.Add('lastMergeSourceCommit', '{ "commitId": "' + $lastCommitId + '" } ')
            patch_pull_request $pullRequest $jsonPatch

            # VSTS markdown will automatically add links to Pull Requests if the IDs
            # are prefixed with an exclamation point, e.g.: !123456
            $pullRequestLinks += "!{0}`n" -f $pullRequest."pullRequestId"

            # Sleep to let the commits from the completed PR catch up
            Start-Sleep -Milliseconds 2000
       }
    }

    # Create a pull request for the final branch
    $postData = @{}
    $postData.sourceRefName = "refs/heads/$finishBranch"
    $postData.targetRefName = "refs/heads/$rootOrigin"
    $postData.title = "Completion of " +$stackInfo.Name
    $postData.description = $description + "`n`n" + $pullRequestLinks   
    $postData.reviewers = get_default_reviewers

    write-host "Creating new pull request '"$postData.title"'"
    $createPullReqeustUri = get_api_url "pullRequests"
    $jsonText = ConvertTo-Json -InputObject  $postData 
    $error.Clear()
    $result = Invoke-RestMethod -Uri $createPullReqeustUri -Method POST -Body $jsonText -ContentType "application/json" -Headers @{Authorization=(get_vsts_auth_header)}
    if($error.Count -eq 0)
    {
        write-host "Created pull request with id: "$result."pullRequestId"
        $pullRequest = rest_get($result."url")
        $remoteWebUrl = $pullRequest."repository"."remoteUrl"
        $remoteWebUrl = $remoteWebUrl + "/pullrequest/" + $result."pullRequestId"
        [System.Diagnostics.Process]::Start($remoteWebUrl) > $null
    }
    else
    {
        write-host -ForegroundColor Red "There were problems trying to create the final pull request"
        write-host $result
        write-host -ForegroundColor Yellow "The local branches for this stack will NOT be deleted"
        return
    }

    if ($deleteFlag -eq "-d")
    {
        # Delete the local branches
        $localBranches = git branch
        for($i = $stackInfo.LastBranchNumber; $i -gt -1; $i--)
        {
            $branchName = make_stackbranch_name $stackInfo.Name $i
            if($localBranches.Contains("  $branchName") -Or $localBranches.Contains("* $branchName"))
            {
                write-host "Deleting branch $branchName" 
                $null = git branch -D $branchName
            }
        }
    }
    
    write-host -ForegroundColor Green "DONE"
}



#-----------------------------------------------------------------------------
# Starting from the top origin branch, move all pending changes down
# the list of stacked branches
#-----------------------------------------------------------------------------
function ssupdate($startNumber)
{
    $stackInfo = get_stack_info
    if($startNumber -eq $null)
    {
        $startNumber = 1
    }

    if($stackInfo.IsStacked -ne $true)
    {
        error "Error: the current branch is not stacked."
        return
    }

    for($stackNumber = $startNumber; $stackNumber -lt $stackInfo.LastBranchNumber + 1 ; $stackNumber++)
    {
        if(warn_on_dangling_work)
        {
            return       
        }
       
        write-host "Checking stack level $stackNumber"
        $branchName = make_stackbranch_name $stackInfo.Name $stackNumber
        $checkoutResult = git_checkout $branchName
        if($checkoutResult.IsSuccessful -ne $true)
        {
            write-host  -ForegroundColor Red "ERROR: could not check out $branchName"
            return
        }
        $currentStackInfo = get_stack_info

        if(current_branch_has_diverged)
        {
            $originBranch = "origin/" + $currentStackInfo.Origin
            write-host "  Pulling changes from " $originBranch 
            $pullResult = git_pull $originBranch 
            if(!(check_for_good_git_result $pullResult))
            {
                return;
            }

            if($pullResult.AlreadyUpToDate -ne $true)
            {
                $pushResult = git_push origin $currentStackInfo.Branch
                if(!$pushResult.AlreadyUpToDate)
                {
                    write-host "  Pushed local changes to "$currentStackInfo.Branch
                }
            }
        }
    }
    $result = git_checkout $stackInfo.Branch

    Write-host -ForegroundColor Green "DONE"
}

#-----------------------------------------------------------------------------
# Push currently created work and bring up VSTS do you can create a PR
#-----------------------------------------------------------------------------
function sspush($force)
{
    $stackInfo = get_stack_info
    if($stackInfo.IsStacked -ne $true)
    {
        error "Error: the current branch is not stacked."
        return
    }

    $currentBranch = get_current_branch
    $unpushedCommits = get_unpushed_commit_summary $stackInfo.Number
    if($unpushedCommits.CommitCount -eq 0)
    {
        throw "Could not find any commits not already on the target branch."
    }
    $firstLine = $unpushedCommits.Lines[0];
    $commitDescription = "";
    for ($i=1; $i -lt $unpushedCommits.Lines.Count; $i++)
    {
        $commitDescription = $commitDescription +  $unpushedCommits.Lines[$i] + "`n"
    }
  
    # see if the pull request exists
    $query = "&status=active"
    $query += "&sourceRefName=" + [System.Web.HttpUtility]::UrlEncode("refs/heads/$currentBranch")
    $pullRequests = get_pull_requests($query)
            
    if($pullRequests.Count -gt 1)
    {
        write-host -ForegroundColor Red "******* ERROR *********"
        write-host -ForegroundColor Red 'There is more than one pull request sourced from ' + $currentBranch
        write-host -ForegroundColor Red 'Type "govsts" to open vsts and resolve your activie pull reqeusts.'
        return
    }
    elseif($pullRequests.Count -eq 1)
    {
        if($force -ne "--force")
        {
            throw "A commit has already been pushed to this level in the stack.  Use --force if you really want to add another commit at this level."
        }

        write-host "Pushing changes up to $currentBranch..."
        $pushresult = git_push origin $currentBranch 
        if(!(check_for_good_git_result $pushResult))
        {
            throw "Git push failed."
        }

        $newDescription = $firstLine + "`n" + $commitDescription
        if(Get-Member -inputobject $pullRequests[0] -name "description" -Membertype Properties)
        { 
            $newDescription =  $pullRequests[0]."description" + $newDescription
        }
        
        patch_pull_request $pullRequests[0] @{"description" = json_encode $newDescription}

        write-host "Updated existing pull request for $currentBranch"
        return
    }

    write-host "Pushing changes up to $currentBranch..."
    $pushresult = git_push origin $currentBranch 
    if(!(check_for_good_git_result $pushResult))
    {
        throw "Git push failed."
    }

    # No pull request exists at this point, so let's create one
    $branchinfo = git status -sb
    $targetBranch = [regex]::Match($branchInfo, "origin/(.+?) ").Groups[1].Value
    if($targetBranch -eq "" -or $targetBranch -eq $null)
    {
        throw "Uh-oh.  Target branch could not be determined. Have you committed any changes?"    
    }

    $createPullReqeustUri = get_api_url "pullRequests"
   
    # Use the commit comments to create a title and description.  
    # First line is the title, the rest is the description
    $description = $commitDescription
    
    $title = $firstLine
    if($title -eq "")
    {
        $title = "*Please edit this title*"
    }
    $postData = @{}
    $postData.sourceRefName = "refs/heads/$currentBranch"
    $postData.targetRefName = "refs/heads/$targetBranch"
    $postData.title = $stackInfo.Name + "_" + $stackInfo.Number.ToString("00") + ": " + $title
    $postData.description = $description   
    $postData.reviewers = get_default_reviewers
    
    if ($postData.reviewers.Count -eq 0) 
    {
        write-host -ForegroundColor Yellow "WARNING:  No default reviewers specified."
        write-host -ForegroundColor White 'How to specify default reviewers:'
        write-host '    1) Create the file "stackprefs.txt" in your .git folder'
        write-host '    2) for each reviewer you want to include, add this line:'
        write-host '       reviewerid=(vsts_guid_of_user)'
        write-host '       (lines starting with '#' are treated as comments)
        write-host 'To see a list of availble user guids, type "Get-VSTSUserGuids"'
    }
   
    write-host "Creating new pull request '"$postData.title"'"
    $jsonText = ConvertTo-Json -InputObject  $postData 

    $error.Clear()
    try 
    { 
        $result = Invoke-RestMethod -Uri $createPullReqeustUri -Method POST -Body $jsonText -ContentType "application/json" -Headers @{Authorization=(get_vsts_auth_header)}
        if($error.Count -eq 0)
        {
            write-host "Created pull request with id: "$result."pullRequestId"
            $pullRequest = rest_get($result."url")
            $remoteWebUrl = $pullRequest."repository"."remoteUrl"
            $remoteWebUrl = $remoteWebUrl + "/pullrequest/" + $result."pullRequestId"
            [System.Diagnostics.Process]::Start($remoteWebUrl) > $null
        }
        else
        {
            Write-Host $error
        }
    } 
    catch
    { 
        write-host -ForegroundColor Red "Push Error " $_
        write-host "Push data: " $jsonText
    }
   
}

#-----------------------------------------------------------------------------
# Unit test helper
#-----------------------------------------------------------------------------
function assert_equals($expectedValue, $actualValue, $message)
{
    if($expectedValue -ne $actualValue)
    {
        $error = $message + "`n" +  "Expected: '$expectedValue'`nGot:      '$actualValue'"
        throw $error
    }
}

#-----------------------------------------------------------------------------
# A little sanity check to make sure we haven't broken the flow
#-----------------------------------------------------------------------------
function sstest()
{
    $gitDirectory = Get-gitDirectory
    cd $gitDirectory
    cd ..

    bar "Move to Master"
    git checkout master
    
    bar "Start New Stack"
    $name = (Get-Date).ToString("yyyyMMdd_HHmmss") + "_TEST"
    ssnew $name master

    bar "Make a couple of commits and push"
    mkdir $name
    cd $name
    "void Poof() {}" > test.cs
    git add *
    git commit -am "A first change"
    "// a comment" >> test.cs
    git commit -am "aaa"
    "// another comment" >> test.cs
    git commit -am "bbb"
    Start-Sleep -Milliseconds 1000
    sspush
    $pullRequest = get_current_pull_request
    assert_equals "$($name)_01: A first change" $pullRequest."title"
    assert_equals "aaa`nbbb`n" $pullRequest."description"
   
    bar "Additional commits should expand description"
    "// third comment" >> test.cs
    git commit -am "ccc"
    Start-Sleep -Milliseconds 1000
    sspush "--force"
    $pullRequest = get_current_pull_request
    assert_equals "$($name)_01: A first change" $pullRequest."title"
    assert_equals "aaa`nbbb`nccc`n" $pullRequest."description"
 
    bar "ss new to create a new stack"
    ssnew
    "// line1" > test2.cs
    git add *
    git commit -am "Test2"   
    Start-Sleep -Milliseconds 1000
    sspush
    $pullRequest = get_current_pull_request
    assert_equals "$($name)_02: Test2" $pullRequest."title"

    #TODO:
    # [ ] Go back to 1 and push up a change, then ss update to push it down to 2
    # [ ] ss finish to make sure the final branch with all changes is completed

    write-host -ForegroundColor Green "SUCCESS!"
}

#-----------------------------------------------------------------------------
# Push currently created work and bring up VSTS do you can create a PR
#-----------------------------------------------------------------------------
function ss()
{
    Param(
        $command,
        $option1,
        $option2,
        $option3,
        $option4
    ) 

    Try
    {
        switch -Regex ( $command )
        {
            '^abandon$'         { ssabandon $option1 $option2 $option3 $option4 }
            '^go$'              { ssgo $option1 $option2 $option3 $option4 }
            '^finish$'          { ssfinish $option1 $option2 $option3 $option4 }
            '^list$'            { sslist $option1 $option2 $option3 $option4 }
            '^push$'            { sspush $option1 $option2 $option3 $option4 }
            '^new$'             { ssnew $option1 $option2 $option3 $option4 }
            '^update$'          { ssupdate $option1 $option2 $option3 $option4 }
            '^test$'            { sstest $option1 $option2 $option3 $option4 }
            '^status$'          { ssstatus $option1 $option2 $option3 $option4 }
            '^.*(help|\?)'      { sshelp $option1 $option2 $option3 $option4 }
            default         { sshelp $option1 $option2 $option3 $option4 }
        }
    }
    Catch
    {
        write-host -ForegroundColor red  "ERROR: "$_.Exception.Message
    }   
}

# Functions specifically for stacked pull requests
export-modulemember -function ss

#some extra helper functions
export-modulemember -function govsts
export-modulemember -function Get-VSTSUserGuids
export-modulemember -function zz

write-host -ForegroundColor Magenta "SHORTSTACK module loaded.  Type 'ss help' for more info."